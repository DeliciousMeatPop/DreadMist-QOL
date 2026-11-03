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
        public const string Version = "0.1.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> EnableCheatPanel;
        internal static ConfigEntry<KeyboardShortcut> CheatPanelHotkey;
        internal static ConfigEntry<bool> FilterDialogue;
        internal static ConfigEntry<bool> DialogueDebug;
        internal static ConfigEntry<bool> GoldWeighsNothing;
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

            _cheats = new CheatPanel();
            var harmony = new Harmony(Guid);

            try { Tweaks.Install(harmony); }
            catch (Exception e) { Log.LogError("Could not install tweaks: " + e); }

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
                if (FilterDialogue.Value && Event.current != null)
                    DialogueFilter.UpdateReveal(RevealHeld(Event.current));

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

        private bool RevealHeld(Event e)
        {
            KeyCode left, right;
            bool fromEvent;
            switch (DialogueRevealKey.Value)
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
