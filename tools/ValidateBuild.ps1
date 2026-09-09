param(
    [string]$GameDir = "D:\free games\EFT_0821",
    [string]$PluginPath = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
if ([string]::IsNullOrWhiteSpace($PluginPath)) {
    $PluginPath = Join-Path $repoRoot "dist\BepInEx\plugins\QuickDiscard\QuickDiscard.dll"
}

$managedDir = Join-Path $GameDir "EscapeFromTarkov_Data\Managed"
$coreDir = Join-Path $GameDir "BepInEx\core"
$pluginDir = Split-Path -Parent $PluginPath

$searchDirs = @($pluginDir, $coreDir, $managedDir)
$assemblyResolve = {
    param($sender, $args)
    $assemblyName = New-Object Reflection.AssemblyName($args.Name)
    foreach ($directory in $searchDirs) {
        $candidate = Join-Path $directory ($assemblyName.Name + ".dll")
        if (Test-Path -LiteralPath $candidate) {
            return [Reflection.Assembly]::LoadFrom($candidate)
        }
    }
    return $null
}

[AppDomain]::CurrentDomain.add_AssemblyResolve($assemblyResolve)

$gameAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $managedDir "Assembly-CSharp.dll"))
$pluginAssembly = [Reflection.Assembly]::LoadFrom($PluginPath)
[void][Reflection.Assembly]::LoadFrom((Join-Path $coreDir "0Harmony.dll"))

$itemViewType = $gameAssembly.GetType("EFT.UI.DragAndDrop.ItemView", $true)
$inventoryScreenType = $gameAssembly.GetType("EFT.UI.InventoryScreen", $true)
$itemUiContextType = $gameAssembly.GetType("EFT.UI.ItemUiContext", $true)
$dialogContextType = $gameAssembly.GetType("EFT.UI.DialogWindowContext", $true)
$screenControllerType = $inventoryScreenType.GetNestedType("InventoryScreenController", "Public,NonPublic")

$checks = @()
$checks += @{
    Name = "InventoryScreen.Show(InventoryScreenController)"
    Value = $inventoryScreenType.GetMethod(
        "Show",
        [Reflection.BindingFlags]"Public,Instance",
        $null,
        @($screenControllerType),
        $null)
}
$checks += @{
    Name = "ItemView.OnDrag(PointerEventData)"
    Value = $itemViewType.GetMethod("OnDrag", [Reflection.BindingFlags]"Public,Instance")
}
$checks += @{
    Name = "ItemView.OnEndDrag(PointerEventData)"
    Value = $itemViewType.GetMethod("OnEndDrag", [Reflection.BindingFlags]"Public,Instance")
}
$checks += @{
    Name = "ItemView.ItemUiContext field"
    Value = $itemViewType.GetField(
        "ItemUiContext",
        [Reflection.BindingFlags]"Public,NonPublic,Instance")
}

$showMessageWindow = $itemUiContextType.GetMethod(
    "ShowMessageWindow",
    [Reflection.BindingFlags]"Public,Instance",
    $null,
    @(
        $dialogContextType.MakeByRefType(),
        [string],
        [string],
        [bool]
    ),
    $null)

$checks += @{
    Name = "ItemUiContext.ShowMessageWindow(ref DialogWindowContext, ...)"
    Value = $showMessageWindow
}

$failed = $false
foreach ($check in $checks) {
    if ($null -eq $check.Value) {
        Write-Output ("MISSING: " + $check.Name)
        $failed = $true
    }
    else {
        Write-Output ("OK: " + $check.Name)
    }
}

$skipPatchType = $pluginAssembly.GetType("QuickDiscard.Patches.SkipConfirmationPatch", $true)
$targetMethod = $skipPatchType.GetMethod(
    "TargetMethod",
    [Reflection.BindingFlags]"NonPublic,Static")

if ($null -eq $targetMethod) {
    Write-Output "MISSING: SkipConfirmationPatch.TargetMethod"
    $failed = $true
}
else {
    Write-Output "OK: SkipConfirmationPatch.TargetMethod"
}

$itemViewAccessType = $pluginAssembly.GetType("QuickDiscard.ItemViewAccess", $true)
$itemUiContextField = $itemViewAccessType.GetField(
    "ItemUiContextField",
    [Reflection.BindingFlags]"NonPublic,Static")

if ($null -eq $itemUiContextField) {
    Write-Output "MISSING: ItemViewAccess.ItemUiContextField"
    $failed = $true
}
else {
    Write-Output "OK: ItemViewAccess.ItemUiContextField"
}

if ($failed) {
    throw "QuickDiscard build validation failed."
}

Write-Output "QuickDiscard build validation passed."
