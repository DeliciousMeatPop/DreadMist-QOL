using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

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

        private static string Clean(string t)
        {
            if (t == null) return "";
            var sb = new System.Text.StringBuilder();
            bool tag = false;
            foreach (char c in t) { if (c == '<') tag = true; else if (c == '>') tag = false; else if (!tag) sb.Append(c); }
            string r = sb.ToString().Replace('\n', ' ');
            return r.Length > 80 ? r.Substring(0, 80) : r;
        }

        private static void Rebuilt(Dreadmist.UI.DialogueWindowUI window, string where)
        {
            Unavailable.Clear();
            var buttons = ButtonsField.GetValue(window) as List<Dreadmist.UI.IntButtonTypedController>;
            if (buttons == null) return;

            // Buttons are reused (and cloned from button 0), so always start from a clean, visible state.
            foreach (var b in buttons) { if (b != null) SetHidden(b, false); }

            int available = 0;
            var red = new List<Dreadmist.UI.IntButtonTypedController>();
            foreach (var b in buttons)
            {
                if (b == null || !b.gameObject.activeSelf) continue;
                string text;
                if (ConditionText.TryGetValue(b, out text) && text != null && text.Contains(RedMarker)) red.Add(b);
                else available++;
            }

            if (Plugin.DialogueDebug.Value)
            {
                Plugin.Log.LogInfo("Dialogue " + where + ": " + (available + red.Count) + " choices, " + red.Count + " unavailable.");
                foreach (var b in red)
                {
                    string t; ConditionText.TryGetValue(b, out t);
                    Plugin.Log.LogInfo("   unavailable: [" + Clean(t) + "]");
                }
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
                if (b != null) SetHidden(b, hide);
            }
        }

        /// <summary>
        /// Hides a button WITHOUT deactivating it. Deactivating stops the game's text-typing effect part-way, and the
        /// button only becomes clickable when typing finishes, so a deactivated choice could stay dead afterwards.
        /// </summary>
        private static void SetHidden(Dreadmist.UI.IntButtonTypedController b, bool hidden)
        {
            var cg = b.GetComponent<CanvasGroup>();
            var le = b.GetComponent<LayoutElement>();
            if (!hidden && cg == null && le == null) return; // nothing of ours to undo

            if (cg == null) cg = b.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = hidden ? 0f : 1f;
            cg.blocksRaycasts = !hidden;
            cg.interactable = !hidden;

            if (le == null) le = b.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = hidden;
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
                try { Rebuilt(__instance, "root"); }
                catch (Exception e) { Plugin.Log.LogError("Dialogue filter (root): " + e); }
            }
        }

        [HarmonyPatch(typeof(Dreadmist.UI.DialogueWindowUI), "SetCurrentDialogueNode")]
        private static class NodePatch
        {
            private static void Postfix(Dreadmist.UI.DialogueWindowUI __instance)
            {
                try { Rebuilt(__instance, "node"); }
                catch (Exception e) { Plugin.Log.LogError("Dialogue filter (node): " + e); }
            }
        }
    }
}
