#requires -Version 5.1
<#
.SYNOPSIS
Builds and publishes one mod to the Dyson Sphere Program Thunderstore community.
Resolves its Team from the service-account token before publishing.
.PARAMETER EnvFile
Optional token file. Defaults to .env in the caller's current directory when present,
otherwise TCLI_AUTH_TOKEN is read from the process environment.
.EXAMPLE
.\Publish-Mod.ps1 -Project UXAssist -DryRun
.EXAMPLE
.\Publish-Mod.ps1 -Project Dustbin -EnvFile C:\Secrets\thunderstore.env
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9_][A-Za-z0-9_.-]*\z')]
    [string]$Project,

    [string]$EnvFile,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-PublishDotNet {
    param([string[]]$Arguments, [switch]$Capture, [string]$Secret, [int[]]$SuccessExitCodes = @(0))

    $previousOutputEncoding = [Console]::OutputEncoding
    try {
        # Localized .NET SDK output is UTF-8. PowerShell decodes native output
        # with Console.OutputEncoding, which can still be a legacy code page.
        [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)

        # Windows PowerShell turns native stderr into error records. Check the exit
        # code ourselves and redact credentials before forwarding any tcli output.
        $ErrorActionPreference = 'Continue'
        $lines = New-Object 'System.Collections.Generic.List[string]'
        & dotnet @Arguments 2>&1 | ForEach-Object {
            $line = $_.ToString()
            if (-not [string]::IsNullOrEmpty($Secret)) { $line = $line.Replace($Secret, '[REDACTED]') }
            if ($Capture) { $lines.Add($line) } else { Write-Host $line }
        }
        $commandExit = $LASTEXITCODE
        if ($commandExit -notin $SuccessExitCodes) {
            if ($Capture) { foreach ($line in $lines) { Write-Host $line } }
            throw "dotnet $($Arguments[0]) failed (exit $commandExit). No further publishing steps were run."
        }
        if ($Capture) { return ($lines -join [Environment]::NewLine) }
    } finally { [Console]::OutputEncoding = $previousOutputEncoding }
}

function Assert-PublishTcli {
    try {
        # tcli 0.2.4 returns 1 for --version because its parser treats it as an
        # unparsed command. Accept it only here, with a recognizable version.
        $versionOutput = Invoke-PublishDotNet -Capture -Arguments @('tool', 'run', 'tcli', '--', '--version') -SuccessExitCodes @(0, 1)
        if ($versionOutput -notmatch '(?m)^tcli [0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z.-]+)?\s*$') {
            throw 'The version check did not return a recognizable tcli version.'
        }
        Write-Host $versionOutput
    } catch {
        throw "Cannot verify the pinned tcli tool. Run dotnet tool restore in the repository root, then dotnet tool run tcli -- --version to inspect startup diagnostics. $($_.Exception.Message)"
    }
}

function Get-PublishJsonProperty {
    param($Object, [string]$Name)
    if ($null -ne $Object) {
        $property = $Object.PSObject.Properties[$Name]
        if ($null -ne $property) { return $property.Value }
    }
    return $null
}

function Read-PublishJson {
    param([string]$Path)
    try { return ([IO.File]::ReadAllText($Path) | ConvertFrom-Json -ErrorAction Stop) }
    catch { throw "Cannot read JSON from '$Path'. Check that it is a valid JSON object." }
}

function Get-PublishProject {
    param([string]$Path)
    $query = Invoke-PublishDotNet -Capture -Arguments @(
        'msbuild', $Path, '-nologo', '-verbosity:quiet', '-property:Configuration=Release',
        '-getProperty:Version,ProjectName,TargetPath,PackHasChangelog,PackUsePluginsLayout,PackPreloaderTargetDir,ProjectAssetsFile',
        '-getItem:ProjectReference'
    )
    try { $result = $query | ConvertFrom-Json -ErrorAction Stop }
    catch { throw "Cannot evaluate '$Path'. MSBuild 17.8 or newer is required; check 'dotnet msbuild -version'." }
    if ($null -eq (Get-PublishJsonProperty $result 'Properties')) { throw "MSBuild returned no properties for '$Path'." }
    return $result
}

function Test-PublishPathWithin {
    param([string]$Path, [string]$Directory)
    $fullPath = [IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
    $fullDirectory = [IO.Path]::GetFullPath($Directory).TrimEnd('\', '/')
    return ($fullPath.Equals($fullDirectory, [StringComparison]::OrdinalIgnoreCase) -or
        $fullPath.StartsWith($fullDirectory + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase))
}

function Get-PublishPhysicalPath {
    param([string]$Path)
    # Resolve parent junctions as well as file symlinks on .NET Framework 4.x.
    if (-not ('DSPModPublish.PhysicalPath' -as [type])) {
        Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
namespace DSPModPublish {
    public static class PhysicalPath {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string name, uint access, uint share,
            IntPtr security, uint disposition, uint flags, IntPtr template);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetFinalPathNameByHandle(SafeFileHandle file,
            StringBuilder path, uint length, uint flags);
        public static string Resolve(string path) {
            using (var handle = CreateFile(path, 0, 7, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero)) {
                if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
                var buffer = new StringBuilder(32768);
                var length = GetFinalPathNameByHandle(handle, buffer, (uint)buffer.Capacity, 0);
                if (length == 0 || length >= buffer.Capacity)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                var result = buffer.ToString();
                if (result.StartsWith(@"\\?\UNC\")) return @"\\" + result.Substring(8);
                return result.StartsWith(@"\\?\") ? result.Substring(4) : result;
            }
        }
    }
}
'@
    }
    return [DSPModPublish.PhysicalPath]::Resolve([IO.Path]::GetFullPath($Path))
}

function Get-PublishToken {
    param([string]$EnvironmentFile)
    if ([string]::IsNullOrWhiteSpace($EnvironmentFile)) {
        $defaultFile = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath('.env')
        if ([IO.File]::Exists($defaultFile)) { $EnvironmentFile = $defaultFile }
    }
    if (-not [string]::IsNullOrWhiteSpace($EnvironmentFile)) {
        $filePath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($EnvironmentFile)
        if (-not [IO.File]::Exists($filePath)) { throw 'The environment file does not exist. Supply a UTF-8 file with TCLI_AUTH_TOKEN.' }
        $token = $null
        foreach ($line in [IO.File]::ReadAllLines($filePath, [Text.UTF8Encoding]::new($false, $true))) {
            if ($line -match '^\s*(#.*)?$') { continue }
            if ($line -notmatch '^\s*TCLI_AUTH_TOKEN\s*=\s*(.*?)\s*$' -or $null -ne $token) {
                throw 'Invalid environment file. Use one TCLI_AUTH_TOKEN assignment, optional blank lines, and full-line comments.'
            }
            $token = $Matches[1]
            if ($token.StartsWith('"') -or $token.StartsWith("'")) {
                if ($token.Length -lt 2 -or $token[0] -ne $token[$token.Length - 1]) { throw 'The token has unmatched quotes in the environment file.' }
                $token = $token.Substring(1, $token.Length - 2)
            }
        }
    } else {
        $token = [Environment]::GetEnvironmentVariable('TCLI_AUTH_TOKEN', 'Process')
    }
    if ([string]::IsNullOrWhiteSpace($token) -or $token -match '\s') {
        throw 'Set a non-empty TCLI_AUTH_TOKEN without whitespace, create .env in the current directory, or supply -EnvFile.'
    }
    # tcli 0.2.4 reads the persisted Windows User value before the process value.
    $userToken = Get-PublishUserToken
    if (-not [string]::IsNullOrWhiteSpace($userToken) -and $userToken -cne $token) {
        throw 'A different User-scoped TCLI_AUTH_TOKEN overrides the selected token in tcli. Remove or reconcile that setting before retrying.'
    }
    return $token
}

function Get-PublishUserToken {
    return [Environment]::GetEnvironmentVariable('TCLI_AUTH_TOKEN', 'User')
}

function Get-PublishNamespace {
    param([string]$Token)
    $endpoint = 'https://thunderstore.io/api/experimental/current-user/'
    try {
        # Keep credentials on the fixed HTTPS endpoint and out of diagnostics.
        $profile = Invoke-RestMethod -Uri $endpoint -Method Get -Headers @{ Authorization = "Bearer $Token" } -TimeoutSec 30 -MaximumRedirection 0 -ErrorAction Stop
    } catch {
        throw "Cannot query the token's Team at $endpoint. Check the service-account token, connectivity, and server availability before retrying; nothing was uploaded."
    }
    $username = Get-PublishJsonProperty $profile 'username'
    $teamsProperty = if ($null -ne $profile) { $profile.PSObject.Properties['teams'] }
    if ($username -isnot [string] -or [string]::IsNullOrWhiteSpace($username) -or
        $null -eq $teamsProperty -or $teamsProperty.Value -isnot [array]) {
        throw 'The current-user endpoint returned an unauthenticated or malformed response. Check the service-account token before publishing.'
    }
    $teams = $teamsProperty.Value
    if ($teams.Count -eq 0) { throw 'The service-account token has no active Team. Check its Team membership before publishing.' }
    if ($teams.Count -ne 1) { throw 'The token belongs to multiple Teams; publication requires exactly one Team.' }
    if ($teams[0] -isnot [string] -or $teams[0] -notmatch '^[A-Za-z0-9_]+\z') {
        throw 'The current-user endpoint returned an invalid Team name. Nothing was uploaded.'
    }
    return $teams[0]
}

function Assert-PublishChangelog {
    param([string]$Text, [string]$Version)
    $sections = @([regex]::Matches($Text, '(?is)<details\b[^>]*>(.*?)</details>') | ForEach-Object { $_.Groups[1].Value })
    if ($sections.Count -eq 0) { $sections = @($Text) }
    foreach ($section in $sections) {
        $latest = [regex]::Match($section, '(?m)^\s*(?:[*-]\s+|#{1,6}\s+)(\d+\.\d+\.\d+)(?=\s|$)')
        if (-not $latest.Success -or $latest.Groups[1].Value -cne $Version) {
            throw "The latest changelog entry in every language must match project version $Version. Update the release notes before publishing."
        }
    }
}

function Get-PublishStreamHash {
    param([IO.Stream]$Stream)
    $hash = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($hash.ComputeHash($Stream)) }
    finally { $hash.Dispose() }
}

function Assert-PublishArchive {
    param([string]$ZipPath, [string]$PackageName, [string]$Version, [hashtable]$Files, [bool]$HasChangelog)
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    Add-Type -AssemblyName System.Drawing
    $archive = [IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        $entries = @{}
        foreach ($entry in $archive.Entries) {
            $entryName = $entry.FullName.Replace('\', '/')
            if ($entryName -match '(^/|:|(^|/)\.\.(/|$))' -or $entries.ContainsKey($entryName)) {
                throw 'The ZIP contains an unsafe path or duplicate entry. Rebuild the package.'
            }
            if ($entryName -match '(^|/)(\.env(?:\..*)?|thunderstore\.toml)$') { throw 'The ZIP contains a local publishing or credential file.' }
            $entries[$entryName] = $entry
        }
        foreach ($entryName in $Files.Keys) {
            if (-not $entries.ContainsKey($entryName) -or $entries[$entryName].FullName -cne $entryName) {
                throw "The ZIP is missing the expected file '$entryName'. Rebuild the package."
            }
            $zipStream = $entries[$entryName].Open()
            $fileStream = [IO.File]::OpenRead($Files[$entryName])
            try {
                if ((Get-PublishStreamHash $zipStream) -cne (Get-PublishStreamHash $fileStream)) {
                    throw "The ZIP file '$entryName' differs from its current source or build output. Rebuild the package."
                }
            } finally { $zipStream.Dispose(); $fileStream.Dispose() }
        }
        $reader = [IO.StreamReader]::new($entries['manifest.json'].Open(), [Text.Encoding]::UTF8, $true)
        try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json -ErrorAction Stop } finally { $reader.Dispose() }
        if ((Get-PublishJsonProperty $manifest 'name') -cne $PackageName -or
            (Get-PublishJsonProperty $manifest 'version_number') -cne $Version) {
            throw 'The ZIP manifest does not match the selected package name and project version.'
        }
        $iconStream = $entries['icon.png'].Open()
        try {
            $icon = [Drawing.Image]::FromStream($iconStream)
            try {
                if ($icon.RawFormat.Guid -ne [Drawing.Imaging.ImageFormat]::Png.Guid -or $icon.Width -ne 256 -or $icon.Height -ne 256) {
                    throw 'The package icon must be a 256x256 PNG.'
                }
            } finally { $icon.Dispose() }
        } finally { $iconStream.Dispose() }
        if ($HasChangelog) {
            $reader = [IO.StreamReader]::new($entries['CHANGELOG.md'].Open(), [Text.Encoding]::UTF8, $true)
            try { Assert-PublishChangelog $reader.ReadToEnd() $Version } finally { $reader.Dispose() }
        }
    } finally { $archive.Dispose() }
}

function Assert-PublishVersionAvailable {
    param([string]$Namespace, [string]$PackageName, [string]$Version)
    $endpoint = "https://thunderstore.io/api/experimental/package/$Namespace/$PackageName/$Version/"
    try {
        $response = Invoke-RestMethod -Uri $endpoint -Method Get -TimeoutSec 30 -ErrorAction Stop
    } catch {
        $httpResponse = Get-PublishJsonProperty $_.Exception 'Response'
        $status = Get-PublishJsonProperty $httpResponse 'StatusCode'
        if ($null -ne $status -and [int]$status -eq 404) { return }
        throw "Cannot check the remote version at $endpoint. Check connectivity and the server response before retrying; nothing was uploaded."
    }
    if ((Get-PublishJsonProperty $response 'namespace') -cne $Namespace -or
        (Get-PublishJsonProperty $response 'name') -cne $PackageName -or
        (Get-PublishJsonProperty $response 'version_number') -cne $Version) {
        throw 'The package-version endpoint returned an unexpected response. Nothing was uploaded.'
    }
    throw "Version $Namespace-$PackageName-$Version already exists. Select an unpublished project version; this script does not bump versions."
}

function Invoke-ModPublish {
    param([string]$RepositoryRoot, [string]$Project, [string]$EnvFile, [switch]$DryRun)
    if ($Project -notmatch '^[A-Za-z0-9_][A-Za-z0-9_.-]*\z') {
        throw 'Supply one top-level -Project directory name.'
    }
    $projectDir = Join-Path $RepositoryRoot $Project
    $projectPath = Join-Path $projectDir "$Project.csproj"
    $packageDir = Join-Path $projectDir 'package'
    foreach ($required in @($projectPath, (Join-Path $packageDir 'manifest.json'), (Join-Path $packageDir 'icon.png'), (Join-Path $projectDir 'README.md'))) {
        if (-not [IO.File]::Exists($required)) {
            if ($Project.EndsWith('Preloader', [StringComparison]::OrdinalIgnoreCase)) {
                throw "Select the main mod with -Project $($Project.Substring(0, $Project.Length - 9)); preloaders are bundled, not published separately."
            }
            throw "Required project/package file not found: $required. Select a packaged main mod, not a helper project."
        }
    }
    $manifestPath = Join-Path $packageDir 'manifest.json'
    $packageName = Get-PublishJsonProperty (Read-PublishJson $manifestPath) 'name'
    if ($packageName -isnot [string] -or $packageName -notmatch '^[A-Za-z0-9_]+\z') { throw 'The manifest must have a valid, non-empty string name.' }
    $token = $null
    $namespace = $null
    # Resolve token files before changing to the repository's build directory.
    if (-not $DryRun) { $token = Get-PublishToken $EnvFile }
    $oldToken = [Environment]::GetEnvironmentVariable('TCLI_AUTH_TOKEN', 'Process')
    $configDir = $null
    $configPath = $null
    $configBase = Join-Path $projectDir 'obj\thunderstore-publish'
    Push-Location -LiteralPath $RepositoryRoot
    try {
        $null = Get-Command dotnet -CommandType Application -ErrorAction Stop
        if (-not $DryRun) {
            Assert-PublishTcli
            $namespace = Get-PublishNamespace $token
        }
        $metadata = Get-PublishProject $projectPath
        $properties = $metadata.Properties
        if (-not [string]::IsNullOrEmpty($properties.PackPreloaderTargetDir)) { throw 'Select a main mod; this project is a preloader or helper.' }
        $version = $properties.Version
        if ($version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+\z') { throw 'The evaluated project Version must have the Thunderstore format major.minor.patch.' }
        $bundled = @()
        $needsRestore = -not [IO.File]::Exists($properties.ProjectAssetsFile)
        foreach ($reference in @($metadata.Items.ProjectReference)) {
            $referenceMetadata = Get-PublishProject $reference.FullPath
            $referenceProperties = $referenceMetadata.Properties
            if (-not [IO.File]::Exists($referenceProperties.ProjectAssetsFile)) { $needsRestore = $true }
            if ([string]::IsNullOrEmpty($referenceProperties.PackPreloaderTargetDir)) { continue }
            $targetDir = $referenceProperties.PackPreloaderTargetDir
            if (-not [IO.Path]::IsPathRooted($targetDir)) { $targetDir = Join-Path ([IO.Path]::GetDirectoryName($reference.FullPath)) $targetDir }
            $targetDir = [IO.Path]::GetFullPath($targetDir)
            if (Test-PublishPathWithin $targetDir $packageDir) {
                $bundled += [pscustomobject]@{ Path = $reference.FullPath; TargetDir = $targetDir; Output = $referenceProperties.TargetPath }
            }
        }
        if ($needsRestore) { Invoke-PublishDotNet -Arguments @('restore', $projectPath) }
        foreach ($preloader in $bundled) {
            Invoke-PublishDotNet -Arguments @('build', $preloader.Path, '-t:CopyToParentPackage', '-c', 'Release', '--no-restore')
        }
        Invoke-PublishDotNet -Arguments @('build', $projectPath, '-t:ZipMod', '-c', 'Release', '--no-restore')
        $zipPath = Join-Path $packageDir "$($properties.ProjectName)-$version.zip"
        if (-not [IO.File]::Exists($zipPath)) { throw "The build did not produce the expected ZIP: $zipPath" }
        $files = @{
            'manifest.json' = $manifestPath
            'icon.png' = Join-Path $packageDir 'icon.png'
            'README.md' = Join-Path $projectDir 'README.md'
        }
        $dllName = [IO.Path]::GetFileName($properties.TargetPath)
        $hasChangelog = $properties.PackHasChangelog -eq 'true'
        if ($properties.PackUsePluginsLayout -eq 'true') { $files["plugins/$dllName"] = $properties.TargetPath }
        else { $files[$dllName] = $properties.TargetPath }
        foreach ($preloader in $bundled) {
            $relativeDir = $preloader.TargetDir.Substring($packageDir.Length).TrimStart('\', '/').Replace('\', '/')
            $entryName = ($relativeDir + '/' + [IO.Path]::GetFileName($preloader.Output)).TrimStart('/')
            $files[$entryName] = $preloader.Output
        }
        if ($hasChangelog) { $files['CHANGELOG.md'] = Join-Path $projectDir 'CHANGELOG.md' }
        Assert-PublishArchive $zipPath $packageName $version $files $hasChangelog

        $teamLabel = if ($DryRun) { 'not queried (resolved from token when publishing)' } else { $namespace }
        Write-Host "Project: $Project | Team: $teamLabel | Package: $packageName | Version: $version"
        Write-Host 'Community: dyson-sphere-program | Categories: empty (retain existing listing categories)'
        Write-Host "ZIP: $zipPath"
        if ($DryRun) { Write-Host 'Dry run complete. Package validated; Team lookup and upload were skipped.'; return }

        $configDir = Join-Path $configBase ([Guid]::NewGuid().ToString('N'))
        $null = [IO.Directory]::CreateDirectory($configDir)
        $configPath = Join-Path $configDir 'thunderstore.toml'
        $configText = @"
[config]
schemaVersion = "0.0.1"

[package]
namespace = "$namespace"
name = "$packageName"
versionNumber = "$version"

[build]
outdir = "."

[publish]
repository = "https://thunderstore.io"
communities = ["dyson-sphere-program"]
categories = []
"@
        [IO.File]::WriteAllText($configPath, $configText + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
        Write-Host ('dotnet tool run tcli -- publish --config-path "{0}" --file "{1}"' -f $configPath, $zipPath)

        Assert-PublishVersionAvailable $namespace $packageName $version
        [Environment]::SetEnvironmentVariable('TCLI_AUTH_TOKEN', $token, 'Process')
        try { Invoke-PublishDotNet -Arguments @('tool', 'run', 'tcli', '--', 'publish', '--config-path', $configPath, '--file', $zipPath) -Secret $token }
        catch { throw "Publication did not finish successfully. Inspect https://thunderstore.io/c/dyson-sphere-program/p/$namespace/$packageName/v/$version/ before retrying; a submission may already have succeeded." }
        Write-Host "Published: https://thunderstore.io/c/dyson-sphere-program/p/$namespace/$packageName/"
    } finally {
        # PowerShell converts $null to an empty string for this .NET call.
        # Modern runtimes preserve empty environment values; use a true null.
        if ($null -eq $oldToken) { [Environment]::SetEnvironmentVariable('TCLI_AUTH_TOKEN', [NullString]::Value, 'Process') }
        else { [Environment]::SetEnvironmentVariable('TCLI_AUTH_TOKEN', $oldToken, 'Process') }
        Pop-Location
        if ($null -ne $configDir -and (Test-PublishPathWithin $configDir $configBase) -and
            [IO.Directory]::Exists($configDir) -and
            (Test-PublishPathWithin (Get-PublishPhysicalPath $configDir) (Get-PublishPhysicalPath $configBase))) {
            try {
                if ([IO.File]::Exists($configPath)) { [IO.File]::Delete($configPath) }
                [IO.Directory]::Delete($configDir, $false)
            } catch { Write-Warning "Could not remove generated configuration directory: $configDir. Remove it when no longer needed." }
        }
    }
}

try {
    Invoke-ModPublish -RepositoryRoot $PSScriptRoot -Project $Project -EnvFile $EnvFile -DryRun:$DryRun
} catch {
    Write-Error -Message $_.Exception.Message -ErrorAction Continue
    exit 1
}
