using System.Reflection;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;

namespace QuickDiscard
{
    internal static class ItemViewAccess
    {
        private static readonly FieldInfo ItemUiContextField =
            AccessTools.Field(typeof(ItemView), "ItemUiContext");

        internal static ItemUiContext GetItemUiContext(ItemView itemView)
        {
            if (itemView == null || ItemUiContextField == null)
            {
                return null;
            }

            return ItemUiContextField.GetValue(itemView) as ItemUiContext;
        }
    }
}

