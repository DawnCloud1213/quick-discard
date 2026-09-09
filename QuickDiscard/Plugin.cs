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
        public const string PluginVersion = "0.1.0";

        internal static QuickDiscardPlugin Instance;
        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> EnableDropZone;
        internal static ConfigEntry<bool> RaidOnly;
        internal static ConfigEntry<bool> SkipConfirmation;
        internal static ConfigEntry<KeyboardShortcut> DiscardHotkey;
        internal static ConfigEntry<float> DropZoneWidth;
        internal static ConfigEntry<float> DropZoneHeight;
        internal static ConfigEntry<float> DropZoneMargin;

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

            DropZoneWidth = Config.Bind(
                "UI",
                "Width",
                168f,
                "Drop zone width in UI pixels.");

            DropZoneHeight = Config.Bind(
                "UI",
                "Height",
                88f,
                "Drop zone height in UI pixels.");

            DropZoneMargin = Config.Bind(
                "UI",
                "Margin",
                24f,
                "Distance from the lower-right corner of the inventory screen.");

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

