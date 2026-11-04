using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace DreadMistQOL
{
    /// <summary>
    /// Entry point. Quality-of-life features (added in later steps) live in their own classes and have
    /// their own on/off switches. The optional cheat panel is separate and OFF by default.
    /// </summary>
    [BepInPlugin(Guid, Name, Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.deliciousmeatpop.dreadmistqol";
        public const string Name = "DreadMist QOL";
        public const string Version = "0.2.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> EnableCheatPanel;
        internal static ConfigEntry<KeyboardShortcut> CheatPanelHotkey;
        internal static ConfigEntry<bool> FilterDialogue;
        internal static ConfigEntry<bool> DialogueDebug;
        internal static ConfigEntry<bool> GoldWeighsNothing;
        internal static ConfigEntry<bool> ClearPerkDisplay;
        internal static ConfigEntry<bool> ShowSanityDetails;
        internal static ConfigEntry<bool> ProtectLastHarvest;
        internal static ConfigEntry<RevealModifier> ForageOverrideKey;
        internal static ConfigEntry<int> RegrowDays;
        internal static ConfigEntry<bool> RespawnOnlyWhenDue;
        internal static ConfigEntry<RevealModifier> DialogueRevealKey;

        internal enum RevealModifier { Shift, Control, Alt }

        private bool _legacyInputOk = true;

        private CheatPanel _cheats;
        private float _noticeUntil;
        private bool _drawErrorLogged;

        private void Awake()
        {
            Log = Logger;

            EnableCheatPanel = Config.Bind(
                "Cheats", "EnableCheatPanel", false,
                "Master switch for the optional console-command panel (items, XP, perks, world). " +
                "Leave false to use only the normal quality-of-life features.");

            CheatPanelHotkey = Config.Bind(
                "Hotkeys", "ToggleCheatPanel", new KeyboardShortcut(KeyCode.F8),
                "Key that shows/hides the cheat panel. Examples: F8, LeftControl + F8. " +
                "Only works when EnableCheatPanel = true.");

            FilterDialogue = Config.Bind(
                "Dialogue", "HideUnavailableChoices", true,
                "When true, conversations only show choices you can do right now. " +
                "Hold the reveal key to show every choice together with its requirements. " +
                "Presentation only: requirements are never bypassed.");

            DialogueRevealKey = Config.Bind(
                "Dialogue", "RevealKey", RevealModifier.Shift,
                "Key to hold to reveal unavailable choices. One of: Shift, Control, Alt.");

            DialogueDebug = Config.Bind(
                "Dialogue", "DebugLog", true,
                "Write one log line per dialogue screen (how many choices were hidden and why) to BepInEx\\LogOutput.log. Helps troubleshooting.");

            GoldWeighsNothing = Config.Bind(
                "Tweaks", "GoldWeighsNothing", false,
                "When true, coins have zero weight (they no longer reduce how much else you can carry). " +
                "This is a gameplay tweak, off by default. Reopen your inventory after changing it.");

            ProtectLastHarvest = Config.Bind(
                "Foraging", "ProtectLastHarvest", true,
                "Bushes, flowers and similar foraged plants stop at 1 harvest left, because the last harvest destroys the plant. " +
                "Hover text warns you. Hold the override key while interacting to harvest the last one anyway. Trees are not affected.");

            ForageOverrideKey = Config.Bind(
                "Foraging", "OverrideKey", RevealModifier.Control,
                "Key to hold to harvest a protected plant's last harvest (and destroy it). One of: Shift, Control, Alt.");

            RegrowDays = Config.Bind(
                "Foraging", "RegrowDays", 2,
                "A partly-harvested foraged plant that is left alone regains 1 harvest every this many in-game days. 0 = off.");

            ClearPerkDisplay = Config.Bind(
                "Perks", "ClearPointDisplay", true,
                "Perks window shows 'unspent / earned' perk points, and each perk's tooltip shows its cost (1 point) and exactly what is missing.");

            ShowSanityDetails = Config.Bind(
                "Sanity", "ShowBreakdown", true,
                "Adds a 'Sanity details' section to the stats window: stage thresholds, constant drain and what reduces it, and effects raising or lowering sanity.");

            RespawnOnlyWhenDue = Config.Bind(
                "Foraging", "RespawnOnlyWhenDue", true,
                "The game resets harvested bushes/flowers whenever an area reloads (for example after you walk far away and come back), " +
                "even if no game time has passed. When true, they only reset once their normal respawn time has really passed.");

            _cheats = new CheatPanel();
            var harmony = new Harmony(Guid);

            try { Tweaks.Install(harmony); }
            catch (Exception e) { Log.LogError("Could not install tweaks: " + e); }

            if (ClearPerkDisplay.Value)
            {
                try { PerkDisplay.Install(harmony); }
                catch (Exception e) { Log.LogError("Could not install perk display: " + e); }
            }

            if (ShowSanityDetails.Value)
            {
                try { SanityInfo.Install(harmony); }
                catch (Exception e) { Log.LogError("Could not install sanity details: " + e); }
            }

            try { Foraging.Install(harmony); }
            catch (Exception e) { Log.LogError("Could not install foraging rules: " + e); }

            if (FilterDialogue.Value)
            {
                try { DialogueFilter.Install(harmony); }
                catch (Exception e) { Log.LogError("Could not install the dialogue filter: " + e); }
            }

            Log.LogInfo(Name + " " + Version + " loaded. Unity " + Application.unityVersion +
                        ". Config file: " + Config.ConfigFilePath);
            Log.LogInfo("Cheat panel enabled: " + EnableCheatPanel.Value +
                        ", hotkey: " + CheatPanelHotkey.Value);
        }

        // IMGUI is used on purpose: it needs no game UI prefabs and works with either Unity input backend.
        private void OnGUI()
        {
            try
            {
                HandleHotkey();
                if (Event.current != null)
                {
                    if (FilterDialogue.Value) DialogueFilter.UpdateReveal(ModifierDown(Event.current, DialogueRevealKey.Value));
                    Foraging.OverrideHeld = ProtectLastHarvest.Value && ModifierDown(Event.current, ForageOverrideKey.Value);
                }

                if (_cheats.Visible && EnableCheatPanel.Value)
                    _cheats.Draw();

                if (Time.realtimeSinceStartup < _noticeUntil)
                {
                    GUI.Box(new Rect(20, 20, 520, 52),
                        "Cheat panel is disabled.\nSet EnableCheatPanel = true in:\n" + Config.ConfigFilePath);
                }
            }
            catch (Exception e)
            {
                if (!_drawErrorLogged)
                {
                    _drawErrorLogged = true; // log once, not every frame
                    Log.LogError("OnGUI failed: " + e);
                }
            }
        }

        private void Update()
        {
            try { Foraging.Tick(); }
            catch (Exception e) { if (!_updateErrorLogged) { _updateErrorLogged = true; Log.LogError("Update failed: " + e); } }
        }

        private bool _updateErrorLogged;

        private void HandleHotkey()
        {
            Event e = Event.current;
            // KeyUp (not KeyDown) so holding the key doesn't flip the panel repeatedly.
            if (e == null || e.type != EventType.KeyUp) return;

            KeyboardShortcut s = CheatPanelHotkey.Value;
            if (e.keyCode != s.MainKey) return;
            if (!s.Modifiers.All(m => ModifierHeld(e, m))) return;

            if (!EnableCheatPanel.Value)
            {
                _noticeUntil = Time.realtimeSinceStartup + 6f;
                Log.LogInfo("Cheat hotkey pressed but EnableCheatPanel is false.");
            }
            else
            {
                _cheats.Visible = !_cheats.Visible;
                if (_cheats.Visible) _cheats.Expand();
                Log.LogInfo("Cheat panel " + (_cheats.Visible ? "opened" : "closed"));
            }
            e.Use();
        }

        private bool ModifierDown(Event e, RevealModifier which)
        {
            KeyCode left, right;
            bool fromEvent;
            switch (which)
            {
                case RevealModifier.Control: left = KeyCode.LeftControl; right = KeyCode.RightControl; fromEvent = e.control; break;
                case RevealModifier.Alt: left = KeyCode.LeftAlt; right = KeyCode.RightAlt; fromEvent = e.alt; break;
                default: left = KeyCode.LeftShift; right = KeyCode.RightShift; fromEvent = e.shift; break;
            }
            if (fromEvent) return true;

            // Fallback in case the event doesn't carry modifiers; disabled if the legacy Input API isn't available.
            if (_legacyInputOk)
            {
                try { return Input.GetKey(left) || Input.GetKey(right); }
                catch { _legacyInputOk = false; Log.LogInfo("Legacy Input API unavailable; using GUI events for the reveal key."); }
            }
            return false;
        }

        private static bool ModifierHeld(Event e, KeyCode m)
        {
            switch (m)
            {
                case KeyCode.LeftShift:
                case KeyCode.RightShift: return e.shift;
                case KeyCode.LeftControl:
                case KeyCode.RightControl: return e.control;
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt: return e.alt;
                default: return false;
            }
        }
    }
}
