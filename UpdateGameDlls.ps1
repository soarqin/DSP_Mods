# UpdateGameDlls.ps1
# Finds the Dyson Sphere Program installation via Steam registry and libraryfolders.vdf,
# then uses assembly-publicizer to refresh AssemblyFromGame/ if the game DLLs are newer.

param(
    [string]$ProjectRoot = $PSScriptRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$DSP_APPID  = '1366540'
$DSP_DLLS   = @('Assembly-CSharp.dll', 'UnityEngine.UI.dll')
$MANAGED_SUBPATH = 'DSPGAME_Data\Managed'
$OUTPUT_DIR = Join-Path $ProjectRoot 'AssemblyFromGame'

# ---------------------------------------------------------------------------
# 1. Locate Steam installation via registry
# ---------------------------------------------------------------------------
$steamPath = $null
foreach ($regPath in @(
    'HKCU:\Software\Valve\Steam',
    'HKLM:\Software\Valve\Steam',
    'HKLM:\Software\Wow6432Node\Valve\Steam'
)) {
    try {
        $val = (Get-ItemProperty -LiteralPath $regPath -Name SteamPath -ErrorAction Stop).SteamPath
        if ($val -and (Test-Path $val)) {
            $steamPath = $val
            break
        }
    } catch { }
}

if (-not $steamPath) {
    Write-Warning 'UpdateGameDlls: Steam installation not found in registry; skipping DLL update.'
    exit 0
}

# ---------------------------------------------------------------------------
# 2. Parse libraryfolders.vdf to find all Steam library paths
# ---------------------------------------------------------------------------
$vdfPath = Join-Path $steamPath 'steamapps\libraryfolders.vdf'
if (-not (Test-Path $vdfPath)) {
    Write-Warning "UpdateGameDlls: $vdfPath not found; skipping DLL update."
    exit 0
}

$vdfContent = Get-Content $vdfPath -Raw -Encoding UTF8

# Extract all library folder paths that contain the DSP app id.
# VDF structure:  "path"  "<library_path>"  followed (somewhere) by  "<appid>"
# Strategy: split into per-library-entry blocks, then check each block.
$libraryPaths = @()

# Match each top-level numbered entry block
$blockPattern  = '"(?:\d+)"\s*\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}'
$pathPattern   = '"path"\s+"([^"]+)"'
$appPattern    = '"' + $DSP_APPID + '"\s+"[^"]+"'

$blockMatches = [regex]::Matches($vdfContent, $blockPattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
foreach ($block in $blockMatches) {
    $blockText = $block.Groups[1].Value
    if ($blockText -match $appPattern) {
        $pathMatch = [regex]::Match($blockText, $pathPattern)
        if ($pathMatch.Success) {
            # VDF uses forward slashes and may double-escape backslashes
            $libPath = $pathMatch.Groups[1].Value -replace '\\\\', '\' -replace '/', '\'
            $libraryPaths += $libPath
        }
    }
}

if ($libraryPaths.Count -eq 0) {
    Write-Warning "UpdateGameDlls: DSP (AppID $DSP_APPID) not found in any Steam library; skipping DLL update."
    exit 0
}

# ---------------------------------------------------------------------------
# 3. Find the game's Managed directory
# ---------------------------------------------------------------------------
$managedDir = $null
foreach ($lib in $libraryPaths) {
    $candidate = Join-Path $lib "steamapps\common\Dyson Sphere Program\$MANAGED_SUBPATH"
    if (Test-Path $candidate) {
        $managedDir = $candidate
        break
    }
}

if (-not $managedDir) {
    Write-Warning 'UpdateGameDlls: Dyson Sphere Program Managed directory not found; skipping DLL update.'
    exit 0
}

Write-Host "UpdateGameDlls: Game Managed directory: $managedDir"

# ---------------------------------------------------------------------------
# 4. Locate assembly-publicizer
# ---------------------------------------------------------------------------
$publicizer = $null
foreach ($candidate in @(
    'assembly-publicizer',                             # on PATH
    "$env:USERPROFILE\.dotnet\tools\assembly-publicizer.exe",
    "$env:ProgramFiles\dotnet\tools\assembly-publicizer.exe"
)) {
    try {
        $resolved = (Get-Command $candidate -ErrorAction Stop).Source
        $publicizer = $resolved
        break
    } catch { }
}

if (-not $publicizer) {
    Write-Host 'UpdateGameDlls: assembly-publicizer not found; installing via dotnet tool install -g BepInEx.AssemblyPublicizer.Cli ...'
    dotnet tool install -g BepInEx.AssemblyPublicizer.Cli
    if ($LASTEXITCODE -ne 0) {
        Write-Warning 'UpdateGameDlls: Failed to install assembly-publicizer; skipping DLL update.'
        exit 0
    }
    # Refresh PATH for the current process so the newly installed tool is found
    $env:PATH = [System.Environment]::GetEnvironmentVariable('PATH', 'User') + ';' + $env:PATH
    try {
        $publicizer = (Get-Command 'assembly-publicizer' -ErrorAction Stop).Source
    } catch {
        Write-Warning 'UpdateGameDlls: assembly-publicizer still not found after install; skipping DLL update.'
        exit 0
    }
    Write-Host "UpdateGameDlls: assembly-publicizer installed at $publicizer"
}

# ---------------------------------------------------------------------------
# 5. Acquire an exclusive file lock so that concurrent MSBuild nodes (triggered
#    by the !Exists fallback condition) cannot write the DLLs simultaneously.
#    The lock is released automatically in the finally block.
# ---------------------------------------------------------------------------
$lockPath   = Join-Path $OUTPUT_DIR '.update.lock'
$lockStream = $null
try {
    # Retry loop: wait up to 60 s for the lock (another node may be updating)
    $deadline = [DateTime]::UtcNow.AddSeconds(60)
    while ($true) {
        try {
            $lockStream = [System.IO.File]::Open(
                $lockPath,
                [System.IO.FileMode]::OpenOrCreate,
                [System.IO.FileAccess]::ReadWrite,
                [System.IO.FileShare]::None)
            break   # lock acquired
        } catch [System.IO.IOException] {
            if ([DateTime]::UtcNow -ge $deadline) {
                Write-Warning 'UpdateGameDlls: Timed out waiting for lock; skipping DLL update.'
                exit 0
            }
            Start-Sleep -Milliseconds 200
        }
    }

# ---------------------------------------------------------------------------
# 6. For each DLL: compare timestamps, publicize if game copy is newer
# ---------------------------------------------------------------------------
$updated = 0
foreach ($dll in $DSP_DLLS) {
    $srcFile = Join-Path $managedDir $dll
    $dstFile = Join-Path $OUTPUT_DIR $dll

    if (-not (Test-Path $srcFile)) {
        Write-Warning "UpdateGameDlls: Source DLL not found: $srcFile"
        continue
    }

    $srcTime = (Get-Item $srcFile).LastWriteTimeUtc
    $needsUpdate = $true

    if (Test-Path $dstFile) {
        $dstTime = (Get-Item $dstFile).LastWriteTimeUtc
        if ($srcTime -le $dstTime) {
            Write-Host "UpdateGameDlls: $dll is up-to-date (game: $srcTime, local: $dstTime)"
            $needsUpdate = $false
        }
    }

    if ($needsUpdate) {
        Write-Host "UpdateGameDlls: Publicizing $dll (game: $srcTime) ..."
        # --overwrite writes directly to <output>/<dll> (no -publicized postfix)
        & $publicizer $srcFile --strip --overwrite --output $OUTPUT_DIR
        if ($LASTEXITCODE -ne 0) {
            Write-Error "UpdateGameDlls: assembly-publicizer failed for $dll (exit code $LASTEXITCODE)"
            exit 1
        }
        # Preserve the source timestamp on the output so future comparisons are stable
        $outFile = Join-Path $OUTPUT_DIR $dll
        if (Test-Path $outFile) {
            (Get-Item $outFile).LastWriteTimeUtc = $srcTime
        }
        $updated++
        Write-Host "UpdateGameDlls: $dll updated."
    }
}

if ($updated -gt 0) {
    Write-Host "UpdateGameDlls: $updated DLL(s) refreshed."
} else {
    Write-Host 'UpdateGameDlls: All DLLs are up-to-date.'
}

} finally {
    # Release the exclusive lock regardless of success or failure
    if ($lockStream) { $lockStream.Close() }
}
