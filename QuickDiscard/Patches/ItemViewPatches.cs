using EFT.UI.DragAndDrop;
using HarmonyLib;
using UnityEngine.EventSystems;

namespace QuickDiscard.Patches
{
    [HarmonyPatch(typeof(ItemView), "OnDrag")]
    internal static class ItemViewOnDragPatch
    {
        [HarmonyPostfix]
        private static void Postfix(PointerEventData eventData)
        {
            DropZoneController zone = DropZoneController.Instance;
            if (zone != null)
            {
                zone.UpdateHover(eventData);
            }
        }
    }

    [HarmonyPatch(typeof(ItemView), "OnEndDrag")]
    internal static class ItemViewOnEndDragPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ItemView __instance, PointerEventData eventData)
        {
            DropZoneController zone = DropZoneController.Instance;
            if (zone == null || __instance == null || eventData == null)
            {
                return;
            }

            if (!zone.ContainsScreenPoint(eventData.position, eventData.pressEventCamera))
            {
                return;
            }

            if (__instance.Item == null)
            {
                return;
            }

            EFT.UI.ItemUiContext itemUiContext = ItemViewAccess.GetItemUiContext(__instance);
            if (itemUiContext == null)
            {
                return;
            }

            QuickDiscardPlugin.RequestDiscard(itemUiContext, __instance.Item);
        }
    }
}
