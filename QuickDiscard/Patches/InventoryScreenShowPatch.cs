using System;
using EFT.UI;
using HarmonyLib;

namespace QuickDiscard.Patches
{
    [HarmonyPatch(
        typeof(InventoryScreen),
        "Show",
        new Type[] { typeof(InventoryScreen.InventoryScreenController) })]
    internal static class InventoryScreenShowPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            InventoryScreen __instance,
            InventoryScreen.InventoryScreenController controller)
        {
            DropZoneController.EnsureFor(__instance, controller);
        }
    }
}

