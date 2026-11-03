using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
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

        private CheatPanel _cheats;
        private bool _cheatsOpen;
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

            _cheats = new CheatPanel();

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

                if (_cheatsOpen && EnableCheatPanel.Value)
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
                _cheatsOpen = !_cheatsOpen;
                Log.LogInfo("Cheat panel " + (_cheatsOpen ? "opened" : "closed"));
            }
            e.Use();
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
