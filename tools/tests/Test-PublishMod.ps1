#requires -Version 5.1
# Run from any directory with Windows PowerShell 5.1 or PowerShell 7.
# All uploads and remote checks use stubs; credentials are synthetic.
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$scriptPath = Join-Path $repoRoot 'Publish-Mod.ps1'
$parseTokens = $null
$parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$parseTokens, [ref]$parseErrors)
if ($parseErrors.Count -ne 0) { throw 'Publish-Mod.ps1 has parse errors.' }
# Load helpers without running the publishing entry point or touching real tokens.
foreach ($statement in $ast.EndBlock.Statements) {
    if ($statement -is [Management.Automation.Language.FunctionDefinitionAst]) {
        . ([scriptblock]::Create($statement.Extent.Text))
    }
}
$script:realInvokeDotNet = ${function:Invoke-PublishDotNet}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -TypeDefinition @'
using System;
public sealed class PublishCheckHttpResponse {
    public int StatusCode { get; set; }
}
public sealed class PublishCheckHttpException : Exception {
    public PublishCheckHttpResponse Response { get; private set; }
    public PublishCheckHttpException(int status) : base("Synthetic HTTP error") {
        Response = new PublishCheckHttpResponse { StatusCode = status };
    }
}
'@
$script:checkCount = 0
$script:userToken = $null
function Get-PublishUserToken { return $script:userToken }
function Assert-Check([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:checkCount++
}
function Assert-CheckThrows([scriptblock]$Action, [string]$MessagePattern) {
    $caught = $false
    try { & $Action 6>$null } catch {
        $caught = $true
        if ($_.Exception.Message -notlike $MessagePattern) { throw }
    }
    Assert-Check $caught "Expected failure: $MessagePattern"
}

$checkBase = Join-Path $repoRoot 'UXAssist\obj\publish-mod-check'
$sandbox = Join-Path $checkBase ([Guid]::NewGuid().ToString('N'))
$null = [IO.Directory]::CreateDirectory($sandbox)
$savedToken = [Environment]::GetEnvironmentVariable('TCLI_AUTH_TOKEN', 'Process')
$savedLocation = (Get-Location).Path
$script:syntheticToken = 'synthetic-publish-check-only'
$script:callerToken = 'synthetic-caller-value'

function New-PublishFixture {
    $script:fixtureRoot = Join-Path $sandbox (([Guid]::NewGuid().ToString('N')) + ' repo with spaces')
    $script:fixtureDir = Join-Path $script:fixtureRoot 'Fixture'
    $script:fixturePackage = Join-Path $script:fixtureDir 'package'
    $script:fixtureOutput = Join-Path $script:fixtureDir 'bin\Release\Fixture.dll'
    $script:fixtureAssets = Join-Path $script:fixtureDir 'obj\project.assets.json'
    $script:fixtureZip = Join-Path $script:fixturePackage 'Fixture-1.2.3.zip'
    foreach ($dir in @($script:fixturePackage, [IO.Path]::GetDirectoryName($script:fixtureOutput), [IO.Path]::GetDirectoryName($script:fixtureAssets))) {
        $null = [IO.Directory]::CreateDirectory($dir)
    }
    [IO.File]::WriteAllText((Join-Path $script:fixtureDir 'Fixture.csproj'), '<Project/>')
    [IO.File]::WriteAllText($script:fixtureAssets, '{}')
    [IO.File]::WriteAllText((Join-Path $script:fixturePackage 'manifest.json'), '{"name":"ManifestPackage","version_number":"1.2.2"}')
    [IO.File]::WriteAllText((Join-Path $script:fixtureDir 'README.md'), '# Fixture')
    [IO.File]::Copy((Join-Path $repoRoot 'UXAssist\package\icon.png'), (Join-Path $script:fixturePackage 'icon.png'))
    [IO.File]::WriteAllText((Join-Path $script:fixtureDir 'CHANGELOG.md'), '<details><summary>English</summary>' + "`n* 1.2.3`n</details><details><summary>Second language</summary>`n* 1.2.3`n</details>")
    $script:calls = New-Object 'System.Collections.Generic.List[string]'
    $script:requests = New-Object 'System.Collections.Generic.List[string]'
    $script:layout = $false
    $script:failStep = ''
    $script:tcliVersionOutput = 'tcli 0.2.4+synthetic'
    $script:badArchive = ''
    $script:remoteState = 'available'
    $script:expectedNamespace = 'AnotherTeam'
    $script:published = $false
    $script:capturedConfig = $null
    $script:capturedConfigPath = $null
}

function Invoke-PublishDotNet {
    param([string[]]$Arguments, [switch]$Capture, [string]$Secret, [int[]]$SuccessExitCodes = @(0))
    $script:calls.Add(($Arguments -join '|'))
    if ($Arguments -contains '--version') {
        Assert-Check ($Capture -and $SuccessExitCodes.Count -eq 2 -and 0 -in $SuccessExitCodes -and 1 -in $SuccessExitCodes) 'The tcli version check did not account for its nonstandard exit code.'
        if ($script:failStep -eq 'tool') { throw 'Synthetic tool startup failure' }
        return $script:tcliVersionOutput
    }
    if ($Arguments[0] -eq 'msbuild') {
        if ($script:failStep -eq 'evaluate') { return 'not JSON' }
        $isPreloader = $Arguments[1] -like '*FixturePreloader.csproj'
        $properties = @{
            Version = '1.2.3'; ProjectName = 'Fixture'; TargetPath = $script:fixtureOutput
            PackHasChangelog = 'true'; PackUsePluginsLayout = $script:layout.ToString().ToLowerInvariant()
            PackPreloaderTargetDir = ''; ProjectAssetsFile = $script:fixtureAssets
        }
        $references = @()
        if ($isPreloader) {
            $properties.TargetPath = Join-Path $script:fixtureRoot 'FixturePreloader\bin\FixturePreloader.dll'
            $properties.PackPreloaderTargetDir = '..\Fixture\package\patchers'
        } elseif ($script:layout) {
            $references = @(@{ FullPath = Join-Path $script:fixtureRoot 'FixturePreloader\FixturePreloader.csproj' })
        }
        return (@{ Properties = $properties; Items = @{ ProjectReference = $references } } | ConvertTo-Json -Depth 6 -Compress)
    }
    if ($Arguments[0] -eq 'restore') {
        if ($script:failStep -eq 'restore') { throw 'Synthetic restore failure' }
        [IO.File]::WriteAllText($script:fixtureAssets, '{}')
        return
    }
    if ($Arguments[0] -eq 'build') {
        if ($Arguments -contains '-t:CopyToParentPackage') {
            if ($script:failStep -eq 'preloader') { throw 'Synthetic preloader failure' }
            $preloaderOutput = Join-Path $script:fixtureRoot 'FixturePreloader\bin\FixturePreloader.dll'
            $null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($preloaderOutput))
            $null = [IO.Directory]::CreateDirectory((Join-Path $script:fixturePackage 'patchers'))
            [IO.File]::WriteAllText($preloaderOutput, 'fresh preloader')
            [IO.File]::Copy($preloaderOutput, (Join-Path $script:fixturePackage 'patchers\FixturePreloader.dll'), $true)
            return
        }
        if ($script:failStep -eq 'build') { throw 'Synthetic build failure' }
        [IO.File]::WriteAllText($script:fixtureOutput, 'fresh main DLL')
        [IO.File]::WriteAllText((Join-Path $script:fixturePackage 'manifest.json'), '{"name":"ManifestPackage","version_number":"1.2.3"}')
        if ($script:badArchive -eq 'wrong version') {
            [IO.File]::WriteAllText((Join-Path $script:fixturePackage 'manifest.json'), '{"name":"ManifestPackage","version_number":"1.2.2"}')
        }
        if ([IO.File]::Exists($script:fixtureZip)) { [IO.File]::Delete($script:fixtureZip) }
        $zip = [IO.Compression.ZipFile]::Open($script:fixtureZip, [IO.Compression.ZipArchiveMode]::Create)
        try {
            $files = @{
                'manifest.json' = Join-Path $script:fixturePackage 'manifest.json'
                'icon.png' = Join-Path $script:fixturePackage 'icon.png'
                'README.md' = Join-Path $script:fixtureDir 'README.md'
                'CHANGELOG.md' = Join-Path $script:fixtureDir 'CHANGELOG.md'
            }
            if ($script:layout) {
                $files['plugins/Fixture.dll'] = $script:fixtureOutput
                $files['patchers/FixturePreloader.dll'] = Join-Path $script:fixturePackage 'patchers\FixturePreloader.dll'
            } else { $files['Fixture.dll'] = $script:fixtureOutput }
            foreach ($entryName in $files.Keys) {
                if ($script:badArchive -eq 'missing DLL' -and $entryName -eq 'Fixture.dll') { continue }
                $null = [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $files[$entryName], $entryName)
            }
            if ($script:badArchive -eq 'unsafe') { $null = $zip.CreateEntry('../bad.dll') }
            if ($script:badArchive -eq 'duplicate') { $null = $zip.CreateEntry('README.md') }
            if ($script:badArchive -eq 'credential') { $null = $zip.CreateEntry('plugins/.env') }
        } finally { $zip.Dispose() }
        if ($script:badArchive -eq 'stale DLL') { [IO.File]::WriteAllText($script:fixtureOutput, 'different DLL') }
        return
    }
    if ($Arguments -contains 'publish') {
        Assert-Check ($SuccessExitCodes.Count -eq 1 -and $SuccessExitCodes[0] -eq 0) 'Publication accepted a nonzero exit code.'
        Assert-Check ($Secret -ceq $script:syntheticToken) 'The output-redaction token is incorrect.'
        Assert-Check ([Environment]::GetEnvironmentVariable('TCLI_AUTH_TOKEN', 'Process') -ceq $script:syntheticToken) 'The child process token is incorrect.'
        Assert-Check (-not ($Arguments -contains '--token')) 'A token was put in the command line.'
        $script:capturedConfigPath = $Arguments[[Array]::IndexOf($Arguments, '--config-path') + 1]
        $script:capturedConfig = [IO.File]::ReadAllText($script:capturedConfigPath)
        Assert-Check ($script:capturedConfig -match ('namespace = "' + $script:expectedNamespace + '"')) 'The supplied namespace was not used.'
        Assert-Check ($script:capturedConfig -match 'name = "ManifestPackage"') 'Package Name was not read from the manifest.'
        Assert-Check ($script:capturedConfig -match 'communities = \["dyson-sphere-program"\]' -and $script:capturedConfig -match 'categories = \[\]') 'Community/categories are incorrect.'
        Assert-Check (-not $script:capturedConfig.Contains($script:syntheticToken)) 'A token was written to TOML.'
        Assert-Check ($Arguments[[Array]::IndexOf($Arguments, '--file') + 1] -ceq $script:fixtureZip) 'The exact ZIP was not used.'
        if ($script:failStep -eq 'publish') { throw 'Synthetic submission timeout' }
        $script:published = $true
    }
}

function Invoke-RestMethod {
    param([string]$Uri, [string]$Method, [int]$TimeoutSec, [string]$ErrorAction)
    $script:requests.Add($Uri)
    if ($script:remoteState -eq 'available') { throw [PublishCheckHttpException]::new(404) }
    if ($script:remoteState -eq 'server error') { throw [PublishCheckHttpException]::new(503) }
    if ($script:remoteState -eq 'offline') { throw 'Synthetic connection failure' }
    if ($script:remoteState -eq 'malformed') { return [pscustomobject]@{ unexpected = 'response' } }
    return [pscustomobject]@{ namespace = $script:expectedNamespace; name = 'ManifestPackage'; version_number = '1.2.3' }
}

try {
    New-PublishFixture
    $externalTokenFile = Join-Path $sandbox 'synthetic.env'
    [IO.File]::WriteAllText($externalTokenFile, "# Synthetic credentials only`nTCLI_AUTH_TOKEN='$script:syntheticToken'`n")
    [Environment]::SetEnvironmentVariable('TCLI_AUTH_TOKEN', $script:callerToken, 'Process')
    Push-Location -LiteralPath $script:fixtureRoot
    try {
        Assert-Check ((Get-PublishToken $externalTokenFile) -ceq $script:syntheticToken) 'Explicit environment file did not take precedence.'
        Assert-Check ((Get-PublishToken) -ceq $script:callerToken) 'Inherited process token was not used when .env was absent.'
        foreach ($invalid in @('TCLI_AUTH_TOKEN=', 'OTHER_KEY=value', "TCLI_AUTH_TOKEN=one`nTCLI_AUTH_TOKEN=two", 'TCLI_AUTH_TOKEN="unclosed', 'TCLI_AUTH_TOKEN=two words')) {
            [IO.File]::WriteAllText($externalTokenFile, $invalid)
            Assert-CheckThrows { Get-PublishToken $externalTokenFile } '*'
        }
        [IO.File]::WriteAllText($externalTokenFile, 'TCLI_AUTH_TOKEN=$env:SHOULD_NOT_EXPAND')
        Assert-Check ((Get-PublishToken $externalTokenFile) -ceq '$env:SHOULD_NOT_EXPAND') 'Environment file content was expanded.'
        [IO.File]::WriteAllText($externalTokenFile, "TCLI_AUTH_TOKEN=$script:syntheticToken")
        $script:userToken = 'synthetic-stale-user-value'
        Assert-CheckThrows { Get-PublishToken $externalTokenFile } '*User-scoped*'
        $script:userToken = $null
        [Environment]::SetEnvironmentVariable('TCLI_AUTH_TOKEN', [NullString]::Value, 'Process')
        Assert-CheckThrows { Get-PublishToken } '*Set a non-empty*'
        [Environment]::SetEnvironmentVariable('TCLI_AUTH_TOKEN', $script:callerToken, 'Process')
        $defaultTokenFile = Join-Path $script:fixtureRoot '.env'
        [IO.File]::WriteAllText($defaultTokenFile, 'TCLI_AUTH_TOKEN=synthetic-current-directory-token')
        Assert-Check ((Get-PublishToken) -ceq 'synthetic-current-directory-token') 'The current directory .env did not override the process token.'
        Assert-Check ((Get-PublishToken $externalTokenFile) -ceq $script:syntheticToken) 'Explicit -EnvFile did not override the default .env.'
        Assert-Check ((Get-PublishToken '.env') -ceq 'synthetic-current-directory-token') 'An explicit in-checkout .env was rejected.'
        Assert-CheckThrows { Get-PublishToken 'missing.env' } '*does not exist*'
        [IO.File]::WriteAllText($defaultTokenFile, 'TCLI_AUTH_TOKEN=')
        Assert-CheckThrows { Get-PublishToken } '*Set a non-empty*'
    } finally { Pop-Location }

    Invoke-ModPublish -RepositoryRoot $script:fixtureRoot -Project Fixture -Namespace AnotherTeam -DryRun -EnvFile 'does-not-exist' 6>$null
    Assert-Check (-not $script:published -and $script:requests.Count -eq 0) 'DryRun attempted publication or remote checks.'
    Assert-Check (@($script:calls | Where-Object { $_ -like 'tool|*' }).Count -eq 0) 'DryRun required tcli.'
    foreach ($invalidNamespace in @('', 'Bad Team', "Team`n")) {
        Assert-CheckThrows { Invoke-ModPublish $script:fixtureRoot Fixture $invalidNamespace -DryRun } '*-Namespace*'
    }
    Assert-CheckThrows { Invoke-ModPublish $script:fixtureRoot '..\Fixture' AnotherTeam -DryRun } '*-Project*'
    Assert-CheckThrows { Invoke-ModPublish $script:fixtureRoot FixturePreloader AnotherTeam -DryRun } '*main mod*'

    New-PublishFixture
    $script:layout = $true
    $null = [IO.Directory]::CreateDirectory((Join-Path $script:fixturePackage 'patchers'))
    [IO.File]::WriteAllText((Join-Path $script:fixturePackage 'patchers\FixturePreloader.dll'), 'stale preloader')
    Invoke-ModPublish $script:fixtureRoot Fixture AnotherTeam -DryRun 6>$null
    Assert-Check (($script:calls | Where-Object { $_ -like 'build|*' } | Select-Object -First 1) -like '*CopyToParentPackage*') 'The preloader was not copied before the main mod was packaged.'
    Assert-Check ([IO.File]::ReadAllText((Join-Path $script:fixturePackage 'patchers\FixturePreloader.dll')) -ceq 'fresh preloader') 'A stale preloader was packaged.'

    foreach ($badZip in @('unsafe', 'duplicate', 'credential', 'missing DLL', 'stale DLL', 'wrong version')) {
        New-PublishFixture
        $script:badArchive = $badZip
        Assert-CheckThrows { Invoke-ModPublish $script:fixtureRoot Fixture AnotherTeam -DryRun } '*ZIP*'
        Assert-Check (-not $script:published) 'An invalid ZIP was published.'
    }
    New-PublishFixture
    [IO.File]::WriteAllText((Join-Path $script:fixtureDir 'CHANGELOG.md'), "<details>`n* 1.2.3`n</details><details>`n* 1.2.2`n</details>")
    Assert-CheckThrows { Invoke-ModPublish $script:fixtureRoot Fixture AnotherTeam -DryRun } '*every language*'
    New-PublishFixture
    $smallIcon = New-Object Drawing.Bitmap(128, 128)
    try { $smallIcon.Save((Join-Path $script:fixturePackage 'icon.png'), [Drawing.Imaging.ImageFormat]::Png) } finally { $smallIcon.Dispose() }
    Assert-CheckThrows { Invoke-ModPublish $script:fixtureRoot Fixture AnotherTeam -DryRun } '*256x256 PNG*'

    New-PublishFixture
    $script:tcliVersionOutput = 'Synthetic tool startup error without a version'
    Assert-CheckThrows { Invoke-ModPublish $script:fixtureRoot Fixture AnotherTeam $externalTokenFile } '*recognizable tcli version*'
    Assert-Check (-not $script:published -and $script:requests.Count -eq 0 -and $script:calls.Count -eq 1) 'An invalid tool version fell through to the build or publication.'

    foreach ($failure in @('tool', 'evaluate', 'restore', 'preloader', 'build')) {
        New-PublishFixture
        $script:failStep = $failure
        if ($failure -eq 'restore') { [IO.File]::Delete($script:fixtureAssets) }
        if ($failure -eq 'preloader') { $script:layout = $true }
        # Leave an older ZIP to ensure a failed build never publishes it.
        [IO.File]::WriteAllText($script:fixtureZip, 'old zip')
        Assert-CheckThrows { Invoke-ModPublish $script:fixtureRoot Fixture AnotherTeam $externalTokenFile } '*'
        Assert-Check (-not $script:published -and $script:requests.Count -eq 0) 'A build failure fell through to publication.'
    }
    foreach ($remote in @('existing', 'server error', 'offline', 'malformed')) {
        New-PublishFixture
        $script:remoteState = $remote
        Assert-CheckThrows { Invoke-ModPublish $script:fixtureRoot Fixture AnotherTeam $externalTokenFile } '*'
        Assert-Check (-not $script:published) 'An existing/unverifiable version was published.'
        Assert-Check ($script:requests[0] -like '*/AnotherTeam/ManifestPackage/1.2.3/') 'The version lookup did not use the selected Team and manifest name.'
    }

    foreach ($failure in @('', 'publish')) {
        New-PublishFixture
        $script:failStep = $failure
        if ($failure) { Assert-CheckThrows { Invoke-ModPublish $script:fixtureRoot Fixture AnotherTeam $externalTokenFile } '*Inspect https://*before retrying*' }
        else { Invoke-ModPublish $script:fixtureRoot Fixture AnotherTeam $externalTokenFile 6>$null }
        Assert-Check ([Environment]::GetEnvironmentVariable('TCLI_AUTH_TOKEN', 'Process') -ceq $script:callerToken) 'The process token was not restored.'
        Assert-Check ((Get-Location).Path -ceq $savedLocation) 'The caller directory was not restored.'
        Assert-Check (-not [IO.Directory]::Exists([IO.Path]::GetDirectoryName($script:capturedConfigPath))) 'Generated configuration was not cleaned up.'
        Assert-Check ([IO.File]::Exists($script:fixtureZip)) 'The prepared ZIP was removed.'
    }
    [IO.File]::WriteAllText((Join-Path $checkBase 'verified-thunderstore.toml'), $script:capturedConfig)

    New-PublishFixture
    Push-Location -LiteralPath $sandbox
    try {
        Invoke-ModPublish $script:fixtureRoot Fixture AnotherTeam 'synthetic.env' 6>$null
        Assert-Check ((Get-Location).Path -ceq $sandbox) 'The external caller directory was not restored.'
        [IO.File]::WriteAllText((Join-Path $sandbox '.env'), "TCLI_AUTH_TOKEN=$script:syntheticToken")
        [IO.File]::WriteAllText((Join-Path $script:fixtureRoot '.env'), 'TCLI_AUTH_TOKEN=synthetic-wrong-repository-token')
        $script:published = $false
        Invoke-ModPublish -RepositoryRoot $script:fixtureRoot -Project Fixture -Namespace AnotherTeam 6>$null
        Assert-Check $script:published 'Publication did not use .env from the caller directory.'
        Assert-Check ((Get-Location).Path -ceq $sandbox) 'Default .env lookup did not restore the caller directory.'
    } finally { Pop-Location }
    [Environment]::SetEnvironmentVariable('TCLI_AUTH_TOKEN', [NullString]::Value, 'Process')
    New-PublishFixture
    Invoke-ModPublish $script:fixtureRoot Fixture AnotherTeam $externalTokenFile 6>$null
    Assert-Check ($null -eq [Environment]::GetEnvironmentVariable('TCLI_AUTH_TOKEN', 'Process')) 'An originally absent process token was not removed.'

    # Use real MSBuild output: string stubs cannot reproduce native decoding bugs.
    $encodingProject = Join-Path $sandbox 'Encoding.proj'
    [IO.File]::WriteAllText($encodingProject, @'
<Project DefaultTargets="Build">
  <Target Name="Build">
    <Message Text="&#x4E2D;&#x6587;&#x8F93;&#x51FA;" Importance="high" />
  </Target>
  <Target Name="Fail">
    <Error Text="&#x4E2D;&#x6587;&#x8F93;&#x51FA;" />
  </Target>
</Project>
'@)
    $expectedNativeText = -join ([char[]]@(0x4e2d, 0x6587, 0x8f93, 0x51fa))
    $previousOutputEncoding = [Console]::OutputEncoding
    $previousPipelineEncoding = $OutputEncoding
    try {
        foreach ($codePage in @(936, 65001)) {
            [Console]::OutputEncoding = [Text.Encoding]::GetEncoding($codePage)
            $nativeArguments = @('msbuild', $encodingProject, '-nologo', '-verbosity:minimal', '-nodeReuse:false')
            $captured = & $script:realInvokeDotNet -Capture -Arguments $nativeArguments
            Assert-Check ($captured.Contains($expectedNativeText)) "Captured MSBuild output was corrupted with code page $codePage."
            Assert-Check ([Console]::OutputEncoding.CodePage -eq $codePage) 'Capture did not restore the console encoding.'
            $displayed = & $script:realInvokeDotNet -Arguments $nativeArguments 6>&1
            Assert-Check (($displayed -join "`n").Contains($expectedNativeText)) "Displayed MSBuild output was corrupted with code page $codePage."
            Assert-Check ([Console]::OutputEncoding.CodePage -eq $codePage) 'Display did not restore the console encoding.'
            $failureOutput = New-Object 'System.Collections.Generic.List[string]'
            $failed = $false
            try {
                & $script:realInvokeDotNet -Capture -Arguments ($nativeArguments + '-target:Fail') 6>&1 |
                    ForEach-Object { $failureOutput.Add($_.ToString()) }
            } catch {
                $failed = $true
                Assert-Check ($_.Exception.Message -like '*exit 1*') 'MSBuild returned an unexpected failure.'
            }
            Assert-Check ($failed -and ($failureOutput -join "`n").Contains($expectedNativeText)) 'MSBuild failure output was lost or corrupted.'
            Assert-Check ([Console]::OutputEncoding.CodePage -eq $codePage) 'Failure did not restore the console encoding.'
        }
        Assert-Check ($OutputEncoding -eq $previousPipelineEncoding) 'Native output handling changed the input pipeline encoding.'
    } finally { [Console]::OutputEncoding = $previousOutputEncoding }

    # Exercise redaction and nonzero exits without starting another process.
    $script:nativeExit = 0
    $script:nativeOutput = "Synthetic tool output: $script:syntheticToken"
    function dotnet {
        param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
        $global:LASTEXITCODE = $script:nativeExit
        return $script:nativeOutput
    }
    try {
        $redacted = & $script:realInvokeDotNet -Capture -Arguments @('stub') -Secret $script:syntheticToken
        Assert-Check ($redacted.Contains('[REDACTED]') -and -not $redacted.Contains($script:syntheticToken)) 'Native output exposed the token.'
        # Exercise the actual preflight helper through the real exit-code wrapper.
        $stubInvokeDotNet = ${function:Invoke-PublishDotNet}
        try {
            Set-Item -LiteralPath Function:Invoke-PublishDotNet -Value $script:realInvokeDotNet
            $script:nativeOutput = 'tcli 0.2.4+synthetic'
            foreach ($versionExit in @(0, 1)) {
                $script:nativeExit = $versionExit
                Assert-PublishTcli 6>$null
                Assert-Check ($true) "The version check rejected tcli exit code $versionExit."
            }
            $script:nativeExit = 1
            $script:nativeOutput = 'Synthetic tool restore failure'
            Assert-CheckThrows { Assert-PublishTcli } '*recognizable tcli version*'
            $script:nativeOutput = 'tcli 0.2.4+synthetic'
            $script:nativeExit = 150
            Assert-CheckThrows { Assert-PublishTcli } '*exit 150*'
            $script:nativeExit = 1
            Assert-CheckThrows { & $script:realInvokeDotNet -Capture -Arguments @('tool', 'run', 'tcli', '--', 'publish') } '*exit 1*'
        } finally { Set-Item -LiteralPath Function:Invoke-PublishDotNet -Value $stubInvokeDotNet }
        $script:nativeOutput = "Synthetic tool output: $script:syntheticToken"
        $script:nativeExit = 17
        Assert-CheckThrows { & $script:realInvokeDotNet -Arguments @('stub') -Secret $script:syntheticToken } '*exit 17*'
        Assert-CheckThrows { & $script:realInvokeDotNet -Capture -Arguments @('stub') -Secret $script:syntheticToken } '*exit 17*'
    } finally { Remove-Item -LiteralPath Function:dotnet }
    Write-Host "Publish-Mod checks passed: $script:checkCount assertions. No real upload or remote lookup was made."
} finally {
    if ($null -eq $savedToken) { [Environment]::SetEnvironmentVariable('TCLI_AUTH_TOKEN', [NullString]::Value, 'Process') }
    else { [Environment]::SetEnvironmentVariable('TCLI_AUTH_TOKEN', $savedToken, 'Process') }
    Set-Location -LiteralPath $savedLocation
}
