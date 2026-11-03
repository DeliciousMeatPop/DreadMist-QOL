using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace DreadMistQOL
{
    /// <summary>
    /// Hides dialogue choices whose requirements you don't currently meet, unless the reveal key (Shift) is held.
    /// Presentation only: it just turns response buttons off/on. It never changes conditions, never clicks anything,
    /// and the game's own checks still run when a choice is selected.
    ///
    /// How "can't do it now" is decided: the game builds a requirement text for every choice, and wraps each unmet
    /// requirement in &lt;color=red&gt;. A choice whose text contains red is treated as unavailable.
    /// </summary>
    internal static class DialogueFilter
    {
        private const string RedMarker = "<color=red>";

        // Requirement text the game passed to each button (the first argument of IntButtonTypedController.SetText).
        private static readonly ConditionalWeakTable<Dreadmist.UI.IntButtonTypedController, string> ConditionText =
            new ConditionalWeakTable<Dreadmist.UI.IntButtonTypedController, string>();

        private static readonly FieldInfo ButtonsField = AccessTools.Field(typeof(Dreadmist.UI.DialogueWindowUI), "responseButtons");

        // Buttons found to be unavailable the last time the dialogue was (re)built.
        private static readonly List<Dreadmist.UI.IntButtonTypedController> Unavailable = new List<Dreadmist.UI.IntButtonTypedController>();

        private static bool _revealHeld;

        public static void Install(Harmony harmony)
        {
            harmony.PatchAll(typeof(SetTextPatch));
            harmony.PatchAll(typeof(RootPatch));
            harmony.PatchAll(typeof(NodePatch));
            Plugin.Log.LogInfo("Dialogue filter installed (hold " + Plugin.DialogueRevealKey.Value + " to show everything).");
        }

        /// <summary>Called every OnGUI with the current state of the reveal key.</summary>
        public static void UpdateReveal(bool held)
        {
            if (held == _revealHeld) return;
            _revealHeld = held;
            ApplyVisibility();
        }

        private static bool ShouldHide
        {
            get { return Plugin.FilterDialogue.Value && !_revealHeld; }
        }

        private static void Rebuilt(Dreadmist.UI.DialogueWindowUI window)
        {
            Unavailable.Clear();
            var buttons = ButtonsField.GetValue(window) as List<Dreadmist.UI.IntButtonTypedController>;
            if (buttons == null) return;

            int available = 0;
            var red = new List<Dreadmist.UI.IntButtonTypedController>();
            foreach (var b in buttons)
            {
                if (b == null || !b.gameObject.activeSelf) continue;
                string text;
                if (ConditionText.TryGetValue(b, out text) && text != null && text.Contains(RedMarker)) red.Add(b);
                else available++;
            }

            // Safety: never leave the player with no choices at all.
            if (available == 0)
            {
                if (red.Count > 0) Plugin.Log.LogInfo("Dialogue filter: every choice has an unmet requirement; showing all.");
                return;
            }
            Unavailable.AddRange(red);
            ApplyVisibility();
        }

        private static void ApplyVisibility()
        {
            bool hide = ShouldHide;
            foreach (var b in Unavailable)
            {
                if (b != null) b.gameObject.SetActive(!hide);
            }
        }

        [HarmonyPatch(typeof(Dreadmist.UI.IntButtonTypedController), "SetText")]
        private static class SetTextPatch
        {
            private static void Prefix(Dreadmist.UI.IntButtonTypedController __instance, string startingText)
            {
                try
                {
                    ConditionText.Remove(__instance);
                    ConditionText.Add(__instance, startingText ?? "");
                }
                catch (Exception e) { Plugin.Log.LogError("Dialogue filter (SetText): " + e); }
            }
        }

        [HarmonyPatch(typeof(Dreadmist.UI.DialogueWindowUI), "GoToDialogueRoot")]
        private static class RootPatch
        {
            private static void Postfix(Dreadmist.UI.DialogueWindowUI __instance)
            {
                try { Rebuilt(__instance); }
                catch (Exception e) { Plugin.Log.LogError("Dialogue filter (root): " + e); }
            }
        }

        [HarmonyPatch(typeof(Dreadmist.UI.DialogueWindowUI), "SetCurrentDialogueNode")]
        private static class NodePatch
        {
            private static void Postfix(Dreadmist.UI.DialogueWindowUI __instance)
            {
                try { Rebuilt(__instance); }
                catch (Exception e) { Plugin.Log.LogError("Dialogue filter (node): " + e); }
            }
        }
    }
}
