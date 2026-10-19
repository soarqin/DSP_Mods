#requires -Version 5.1
# Runs the production geometry and solver against the original game assemblies, without Unity.
[CmdletBinding()]
param([string]$GameRoot, [string]$Snapshot)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if (-not $GameRoot) {
    $steamRoot = (Get-ItemProperty -LiteralPath 'HKCU:\Software\Valve\Steam' -Name SteamPath).SteamPath
    $vdf = Get-Content -LiteralPath (Join-Path $steamRoot 'steamapps\libraryfolders.vdf') -Raw
    foreach ($entry in [regex]::Matches($vdf, '"path"\s+"([^"]+)"')) {
        $library = $entry.Groups[1].Value.Replace('\\', '\')
        $candidate = Join-Path $library 'steamapps\common\Dyson Sphere Program'
        if (Test-Path -LiteralPath (Join-Path $candidate 'DSPGAME_Data\Managed\Assembly-CSharp.dll')) {
            $GameRoot = $candidate
            break
        }
    }
}
if (-not $GameRoot) { throw 'DSP installation not found. Pass -GameRoot with the installed game directory.' }
$managed = Join-Path ([IO.Path]::GetFullPath($GameRoot)) 'DSPGAME_Data\Managed'
foreach ($name in @('Assembly-CSharp.dll', 'UnityEngine.CoreModule.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $managed $name))) { throw "Missing original game assembly: $name" }
}
$testRoot = Join-Path $repoRoot 'UXAssist\obj\quick-miner-tests'
$null = [IO.Directory]::CreateDirectory($testRoot)
# Isolate the temporary console project from the mod's SDK props, packages, and packaging targets.
Set-Content -LiteralPath (Join-Path $testRoot 'Directory.Build.props') -Value '<Project />' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $testRoot 'Directory.Build.targets') -Value '<Project />' -Encoding UTF8
$geometry = [Security.SecurityElement]::Escape((Join-Path $repoRoot 'UXAssist\Functions\Factory\MinerPlacementGeometry.cs'))
$solver = [Security.SecurityElement]::Escape((Join-Path $repoRoot 'UXAssist\Functions\Factory\MinerPlacementSolver.cs'))
$debugData = [Security.SecurityElement]::Escape((Join-Path $repoRoot 'UXAssist\Functions\Factory\MinerPlacementDebug.cs'))
$tests = [Security.SecurityElement]::Escape((Join-Path $PSScriptRoot 'QuickMinerPlacementTests.cs'))
$originalDll = [Security.SecurityElement]::Escape((Join-Path $managed 'Assembly-CSharp.dll'))
$unityDll = [Security.SecurityElement]::Escape((Join-Path $managed 'UnityEngine.CoreModule.dll'))
$project = Join-Path $testRoot 'QuickMinerTests.csproj'
Set-Content -LiteralPath $project -Encoding UTF8 -Value @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$geometry" />
    <Compile Include="$solver" />
    <Compile Include="$debugData" />
    <Compile Include="$tests" />
    <Reference Include="Assembly-CSharp" HintPath="$originalDll" />
    <Reference Include="UnityEngine.CoreModule" HintPath="$unityDll" />
  </ItemGroup>
</Project>
"@
if ($Snapshot) {
    $snapshotPath = (Resolve-Path -LiteralPath $Snapshot).Path
    & dotnet run --project $project -c Release -- $snapshotPath
} else {
    & dotnet run --project $project -c Release
}
if ($LASTEXITCODE -ne 0) { throw "Quick miner placement checks failed (exit $LASTEXITCODE)." }
