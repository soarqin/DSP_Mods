param(
    [Parameter(Mandatory)]
    [string] $TargetDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$TargetDir = $TargetDir.Trim().Trim('"').TrimEnd('\', '/')

$steamPath = $null
foreach ($regPath in @(
    'HKCU:\Software\Valve\Steam',
    'HKLM:\Software\Valve\Steam',
    'HKLM:\Software\Wow6432Node\Valve\Steam'
)) {
    try {
        $val = (Get-ItemProperty -LiteralPath $regPath -Name SteamPath -ErrorAction Stop).SteamPath
        if ($val -and (Test-Path $val)) { $steamPath = $val; break }
    } catch { }
}

if (-not $steamPath) { throw 'Steam installation not found; cannot copy game facades for ReflectionReaderCheck.' }

$vdfPath = Join-Path $steamPath 'steamapps\libraryfolders.vdf'
$vdfContent = Get-Content $vdfPath -Raw -Encoding UTF8
$blockPattern = '"(?:\d+)"\s*\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}'
$managed = $null
foreach ($block in [regex]::Matches($vdfContent, $blockPattern, 'Singleline')) {
    $text = $block.Groups[1].Value
    if ($text -notmatch '"1366540"\s+"[^"]+"') { continue }
    $pathMatch = [regex]::Match($text, '"path"\s+"([^"]+)"')
    if (-not $pathMatch.Success) { continue }
    $libPath = $pathMatch.Groups[1].Value -replace '\\\\', '\' -replace '/', '\'
    $candidate = Join-Path $libPath 'steamapps\common\Dyson Sphere Program\DSPGAME_Data\Managed'
    if (Test-Path $candidate) { $managed = $candidate; break }
}

if (-not $managed) { throw 'DSP Managed directory not found; cannot copy game facades for ReflectionReaderCheck.' }

foreach ($name in @('netstandard.dll', 'UnityEngine.dll', 'UnityEngine.CoreModule.dll', 'UnityEngine.SharedInternalsModule.dll')) {
    $src = Join-Path $managed $name
    if (-not (Test-Path $src)) { throw "Missing $src" }
    Copy-Item -LiteralPath $src -Destination (Join-Path $TargetDir $name) -Force
}
