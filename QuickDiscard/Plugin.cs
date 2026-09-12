using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace QuickDiscard
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class QuickDiscardPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.dawncloud.quickdiscard";
        public const string PluginName = "Quick Discard";
        public const string PluginVersion = "0.3.0";

        internal static QuickDiscardPlugin Instance;
        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> EnableDropZone;
        internal static ConfigEntry<bool> RaidOnly;
        internal static ConfigEntry<bool> SkipConfirmation;
        internal static ConfigEntry<KeyboardShortcut> DiscardHotkey;
        internal static ConfigEntry<float> DropZoneAnchorMinX;
        internal static ConfigEntry<float> DropZoneAnchorMinY;
        internal static ConfigEntry<float> DropZoneAnchorMaxX;
        internal static ConfigEntry<float> DropZoneAnchorMaxY;
        internal static ConfigEntry<float> DropZoneMargin;
        internal static ConfigEntry<string> DropZoneLabel;
        internal static ConfigEntry<bool> DumpInventoryLayout;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            EnableDropZone = Config.Bind(
                "General",
                "EnableDropZone",
                true,
                "Show the drag-and-drop discard zone on the in-raid inventory screen.");

            RaidOnly = Config.Bind(
                "General",
                "RaidOnly",
                true,
                "Only show and activate the discard zone while in raid.");

            SkipConfirmation = Config.Bind(
                "General",
                "SkipConfirmation",
                false,
                "Skip the vanilla destroy confirmation dialog. Keep this false for vanilla behavior.");

            DiscardHotkey = Config.Bind(
                "Hotkey",
                "Discard",
                KeyboardShortcut.Empty,
                "Optional hotkey. Discards the item currently under the mouse while the inventory screen is open.");

            DropZoneAnchorMinX = Config.Bind(
                "UI",
                "AnchorMinX",
                0.006f,
                "Left edge of the discard zone, as a fraction of the inventory screen width.");

            DropZoneAnchorMinY = Config.Bind(
                "UI",
                "AnchorMinY",
                0.02f,
                "Bottom edge of the discard zone, as a fraction of the inventory screen height.");

            DropZoneAnchorMaxX = Config.Bind(
                "UI",
                "AnchorMaxX",
                0.271f,
                "Right edge of the discard zone, as a fraction of the inventory screen width.");

            DropZoneAnchorMaxY = Config.Bind(
                "UI",
                "AnchorMaxY",
                0.13f,
                "Top edge of the discard zone, as a fraction of the inventory screen height.");

            DropZoneMargin = Config.Bind(
                "UI",
                "Margin",
                0f,
                "Inset in UI pixels applied inside the anchor rectangle. 0 fills the rectangle completely.");

            DropZoneLabel = Config.Bind(
                "UI",
                "Label",
                "DROP",
                "Text shown in the middle of the discard zone.");

            DumpInventoryLayout = Config.Bind(
                "Debug",
                "DumpInventoryLayout",
                false,
                "Write the inventory screen layout (normalized rects of each panel) to the BepInEx log (up to 4 dumps per session).");

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(QuickDiscardPlugin).Assembly);

            Logger.LogInfo("Quick Discard loaded.");
        }

        private void Update()
        {
            KeyboardShortcut shortcut = DiscardHotkey.Value;
            if (shortcut.MainKey == KeyCode.None)
            {
                return;
            }

            if (!shortcut.IsDown())
            {
                return;
            }

            DropZoneController zone = DropZoneController.Instance;
            if (zone != null)
            {
                zone.TryDiscardHoveredItem();
            }
        }

        private void OnDestroy()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                _harmony = null;
            }
        }

        internal static void RequestDiscard(ItemUiContext itemUiContext, Item item)
        {
            if (itemUiContext == null || item == null)
            {
                return;
            }

            try
            {
                Task task = itemUiContext.ThrowItem(item);
                if (task != null)
                {
                    task.ContinueWith(
                        delegate(Task completedTask)
                        {
                            if (Log != null && completedTask.Exception != null)
                            {
                                Log.LogError("Quick Discard transaction failed: " + completedTask.Exception);
                            }
                        },
                        TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            catch (Exception exception)
            {
                if (Log != null)
                {
                    Log.LogError("Quick Discard failed to start discard transaction: " + exception);
                }
            }
        }
    }
}
