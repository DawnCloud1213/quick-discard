using System;
using System.Reflection;
using System.Threading.Tasks;
using EFT.UI;
using HarmonyLib;

namespace QuickDiscard.Patches
{
    [HarmonyPatch]
    internal static class SkipConfirmationPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(ItemUiContext),
                "ShowMessageWindow",
                new Type[]
                {
                    typeof(DialogWindowContext).MakeByRefType(),
                    typeof(string),
                    typeof(string),
                    typeof(bool)
                });
        }

        [HarmonyPrefix]
        private static bool Prefix(string description, ref Task<bool> __result)
        {
            if (QuickDiscardPlugin.SkipConfirmation == null ||
                !QuickDiscardPlugin.SkipConfirmation.Value ||
                string.IsNullOrEmpty(description))
            {
                return true;
            }

            if (IsDestroyConfirmation(description))
            {
                __result = Task.FromResult(true);
                return false;
            }

            return true;
        }

        private static bool IsDestroyConfirmation(string description)
        {
            if (description.IndexOf("destroy", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (description.IndexOf("discard", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (description.IndexOf("\u9500\u6BC1", StringComparison.Ordinal) >= 0)
            {
                return true;
            }

            if (description.IndexOf("\u4E22\u5F03", StringComparison.Ordinal) >= 0)
            {
                return true;
            }

            return false;
        }
    }
}

