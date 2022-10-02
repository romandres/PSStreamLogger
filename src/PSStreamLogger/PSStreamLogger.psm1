Set-StrictMode -Version Latest

$isCore = ($PSVersionTable.Keys -contains "PSEdition") -and ($PSVersionTable.PSEdition -ne "Desktop")

$binarySubPath = ""
if ($isCore) {
    $binarySubPath = "coreclr"
} else {
    $binarySubPath = "fullclr"
}

$script:PSModule = $ExecutionContext.SessionState.Module
$script:PSModuleRoot = $script:PSModule.ModuleBase

$script:ModuleAssembly = "PSStreamLogger.dll"

$binaryModuleRoot = Join-Path -Path $script:PSModuleRoot -ChildPath $binarySubPath
$modulePath = Join-Path -Path $binaryModuleRoot -ChildPath $script:ModuleAssembly

$module = Import-Module -Name $modulePath -PassThru

# When the module is unloaded, remove the nested binary module that was loaded with it
if($module)
{
    $script:PSModule.OnRemove = {
        Remove-Module -ModuleInfo $module
    }
}
