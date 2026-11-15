#requires -Version 5.1
# Runs the auto-cruise obstacle avoidance of a built UXAssist.dll against the original game assemblies, without Unity.
[CmdletBinding()]
param([string]$GameRoot, [string]$UXAssistDll)

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
if (-not $UXAssistDll) {
    & dotnet build (Join-Path $repoRoot 'UXAssist\UXAssist.csproj') -c Release --no-restore -v quiet -nologo
    if ($LASTEXITCODE -ne 0) { throw "UXAssist build failed (exit $LASTEXITCODE)." }
    $UXAssistDll = Join-Path $repoRoot 'UXAssist\bin\Release\net472\UXAssist.dll'
}
$modDll = (Resolve-Path -LiteralPath $UXAssistDll).Path
$testRoot = Join-Path $repoRoot 'UXAssist\obj\auto-cruise-tests'
$null = [IO.Directory]::CreateDirectory($testRoot)
# Isolate the temporary console project from the mod's SDK props, packages, and packaging targets.
Set-Content -LiteralPath (Join-Path $testRoot 'Directory.Build.props') -Value '<Project />' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $testRoot 'Directory.Build.targets') -Value '<Project />' -Encoding UTF8
$tests = [Security.SecurityElement]::Escape((Join-Path $PSScriptRoot 'AutoCruiseAvoidanceTests.cs'))
$originalDll = [Security.SecurityElement]::Escape((Join-Path $managed 'Assembly-CSharp.dll'))
$unityDll = [Security.SecurityElement]::Escape((Join-Path $managed 'UnityEngine.CoreModule.dll'))
$project = Join-Path $testRoot 'AutoCruiseTests.csproj'
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
    <Compile Include="$tests" />
    <Reference Include="Assembly-CSharp" HintPath="$originalDll" />
    <Reference Include="UnityEngine.CoreModule" HintPath="$unityDll" />
  </ItemGroup>
</Project>
"@
& dotnet run --project $project -c Release -- $modDll
if ($LASTEXITCODE -ne 0) { throw "Auto-cruise avoidance checks failed (exit $LASTEXITCODE)." }
