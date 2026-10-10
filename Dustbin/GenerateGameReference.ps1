param(
    [Parameter(Mandatory = $true)][string]$InputAssembly,
    [Parameter(Mandatory = $true)][string]$OutputAssembly,
    [Parameter(Mandatory = $true)][string]$PreloaderAssembly,
    [Parameter(Mandatory = $true)][string]$CecilAssembly,
    [Parameter(Mandatory = $true)][string]$BepInExAssembly,
    [Parameter(Mandatory = $true)][string]$UnityAssembly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

[void][Reflection.Assembly]::LoadFrom($CecilAssembly)
[void][Reflection.Assembly]::LoadFrom($BepInExAssembly)
$preloader = [Reflection.Assembly]::LoadFrom($PreloaderAssembly)
$resolver = [Mono.Cecil.DefaultAssemblyResolver]::new()
foreach ($assemblyPath in @($InputAssembly, $UnityAssembly, $BepInExAssembly, $CecilAssembly)) {
    $resolver.AddSearchDirectory([IO.Path]::GetDirectoryName($assemblyPath))
}
$parameters = [Mono.Cecil.ReaderParameters]::new()
$parameters.AssemblyResolver = $resolver
$reference = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($InputAssembly, $parameters)
try {
    $preloader.GetType('DustbinPreloader.Preloader').GetMethod('Patch').Invoke($null, @($reference)) | Out-Null
    foreach ($typeName in @('StorageComponent', 'TankComponent')) {
        $fields = $reference.MainModule.GetType($typeName).Fields
        if (-not ($fields | Where-Object { $_.Name -eq 'IsDustbin' -and $_.FieldType.FullName -eq 'System.Boolean' })) {
            throw "Dustbin preloader did not inject $typeName.IsDustbin."
        }
    }
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($OutputAssembly))
    $reference.Write($OutputAssembly)
} finally {
    $reference.Dispose()
    $resolver.Dispose()
}
