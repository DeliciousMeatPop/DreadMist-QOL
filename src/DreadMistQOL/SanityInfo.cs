using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace DreadMistQOL
{
    /// <summary>
    /// Adds a "Sanity details" section to the stats window text (EntitySentientPlayerData.ActiveValuesDisplay).
    /// Information only: nothing about how sanity works is changed. Formulas below are copied from the game's code.
    /// </summary>
    internal static class SanityInfo
    {
        private static MethodInfo _overTime;
        private static bool _overTimeLooked;

        public static void Install(Harmony harmony)
        {
            harmony.PatchAll(typeof(DisplayPatch));
            Plugin.Log.LogInfo("Sanity breakdown installed.");
        }

        // The game's own formatter for "per hour" style numbers, found by name so we match its units exactly.
        private static string OverTime(float v)
        {
            if (!_overTimeLooked)
            {
                _overTimeLooked = true;
                try
                {
                    Type t = AccessTools.TypeByName("StaticHelper");
                    if (t != null) _overTime = t.GetMethod("GetValueOverTimeDisplay", BindingFlags.Public | BindingFlags.Static);
                }
                catch { }
            }
            try
            {
                if (_overTime != null) return (string)_overTime.Invoke(null, new object[] { v });
            }
            catch { }
            return v.ToString("0.###");
        }

        private static string Signed(float v, string text)
        {
            return (v > 0f ? "<color=#9fd39f>+" : v < 0f ? "<color=red>" : "<color=white>") + text + "</color>";
        }

        [HarmonyPatch(typeof(Dreadmist.EntitySentientPlayerData), "get_ActiveValuesDisplay")]
        private static class DisplayPatch
        {
            private static void Postfix(Dreadmist.EntitySentientPlayerData __instance, ref string __result)
            {
                try
                {
                    if (Plugin.ShowSanityDetails.Value) __result += Build(__instance);
                }
                catch (Exception e) { Plugin.Log.LogError("SanityInfo: " + e); }
            }
        }

        private static string Build(Dreadmist.EntitySentientPlayerData d)
        {
            var sm = d.StatModifiers;
            float sanity = d.Sanity, max = d.SanityMax;
            int courage = d.CourageLevelEffective;

            // Stage thresholds exactly as the game applies them: >50 none, 25-50 Confused, 10-25 Irrational, <10 Deranged.
            string stage; string next;
            if (sanity > 50f) { stage = "Stable"; next = "Confused below 50"; }
            else if (sanity >= 25f) { stage = "Confused"; next = "Irrational below 25"; }
            else if (sanity >= 10f) { stage = "Irrational"; next = "Deranged below 10"; }
            else { stage = "Deranged"; next = "lowest stage"; }

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("<color=orange>[Sanity details]</color>");
            sb.AppendLine("Sanity " + sanity.ToString("0.#") + " / " + max.ToString("0.#") + "  -  " + stage + " (" + next + ")");
            sb.AppendLine("Maximum = 100 base + " + courage + " Courage" + (sm.Sanity != 0f ? " " + (sm.Sanity > 0f ? "+" : "") + sm.Sanity.ToString("0.#") + " from effects/gear" : ""));

            // Constant drain
            float baseDrain = 0.00347222f * 4f;
            float courageCut = Math.Min(courage, 100) * 0.5f;
            float effectDrain = sm.SanityDrain * 100f;
            float cfg = Dreadmist.ResourceManager.Instance.GameplayConfig.SanityDrainMultiplier;

            var pc = Dreadmist.PlayerManager.Exists ? Dreadmist.PlayerManager.Instance.FirstSelectedEntity : null;
            bool resting = pc != null && pc.SentientPlayerData == d && pc.IsUsingFurnature;

            sb.AppendLine("Constant drain: " + Signed(-1f, OverTime(-d.SanityDrain * 4f)) + (resting ? "  (paused while using furniture)" : ""));
            sb.AppendLine("   starts at " + OverTime(-baseDrain) + ", reduced " + courageCut.ToString("0.#") + "% by Courage" +
                          (effectDrain != 0f ? ", " + (effectDrain > 0f ? "raised " : "lowered ") + Math.Abs(effectDrain).ToString("0.#") + "% by effects/gear" : "") +
                          (Math.Abs(cfg - 1f) > 0.001f ? ", x" + cfg.ToString("0.##") + " game setting" : ""));
            if (resting && pc.FurnitureSanityRestore > 0f)
                sb.AppendLine("Furniture restores: " + Signed(1f, OverTime(pc.FurnitureSanityRestore)));

            // Named causes from active status effects
            var changes = new List<string>(); var drains = new List<string>(); var maxes = new List<string>();
            float listedChange = 0f;
            var container = d.SentientStatEffectContainer;
            if (container != null)
            {
                foreach (var inst in container.StatEffectsAll)
                {
                    if (inst == null || !inst.HasDefinition) continue;
                    string name = inst.StatEffectDefinition.DisplayNameLocalized;
                    foreach (var v in inst.StatEffectDefinition.StatEffectValues)
                        Collect(name, v.StatEffectType.ToString(), v.Value, changes, drains, maxes, ref listedChange);
                    if (inst.HasPotionEffects)
                        foreach (var v in inst.PotionStatEffects)
                            Collect(name, v.StatEffectType.ToString(), v.Value, changes, drains, maxes, ref listedChange);
                }
            }

            if (sm.SanityChange != 0f || changes.Count > 0)
            {
                sb.AppendLine("Effects changing sanity: " + Signed(sm.SanityChange, OverTime(sm.SanityChange)));
                foreach (var c in changes) sb.AppendLine("   " + c);
                float other = sm.SanityChange - listedChange;
                if (Math.Abs(other) > 0.0001f) sb.AppendLine("   other (gear, furniture, etc.): " + Signed(other, OverTime(other)));
            }
            foreach (var c in drains) sb.AppendLine("   drain modifier: " + c);
            foreach (var c in maxes) sb.AppendLine("   max sanity: " + c);

            sb.AppendLine("Also: some dialogue answers, food/potions and sleeping change sanity (dialogue shows [Sanity +/-x]).");
            return sb.ToString();
        }

        private static void Collect(string name, string type, float value, List<string> changes, List<string> drains, List<string> maxes, ref float listedChange)
        {
            if (type == "SanityChange")
            {
                changes.Add(name + ": " + Signed(value, OverTime(value)));
                listedChange += value;
            }
            else if (type == "SanityDrain")
                drains.Add(name + ": " + Signed(-value, (value > 0 ? "+" : "") + (value * 100f).ToString("0.#") + "% drain"));
            else if (type == "Sanity")
                maxes.Add(name + ": " + Signed(value, (value > 0 ? "+" : "") + value.ToString("0.#")));
        }
    }
}
