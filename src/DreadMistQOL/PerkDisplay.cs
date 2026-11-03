using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace DreadMistQOL
{
    /// <summary>
    /// Clearer perk points. Presentation only; the game's own rules are untouched.
    /// Verified rules: each attribute/skill has its OWN pool. You earn 1 point per 5 levels (max 20 per attribute/skill),
    /// and every perk level costs exactly 1 point from that pool.
    /// </summary>
    internal static class PerkDisplay
    {
        private static readonly FieldInfo CountTextField = AccessTools.Field(typeof(Dreadmist.UI.PerksWindowUI), "perkCountText");
        private static readonly FieldInfo AttributeField = AccessTools.Field(typeof(Dreadmist.UI.PerksWindowUI), "attributeData");
        private static string _lastLog;

        public static void Install(Harmony harmony)
        {
            harmony.PatchAll(typeof(WindowPatch));
            harmony.PatchAll(typeof(TooltipPatch));
            Plugin.Log.LogInfo("Perk display improvements installed.");
        }

        [HarmonyPatch(typeof(Dreadmist.UI.PerksWindowUI), "Refresh")]
        private static class WindowPatch
        {
            private static void Postfix(Dreadmist.UI.PerksWindowUI __instance)
            {
                try
                {
                    var attr = AttributeField.GetValue(__instance) as Dreadmist.AttributeData;
                    var text = CountTextField.GetValue(__instance) as UnityEngine.UI.Text;
                    if (attr == null || text == null) return;

                    int unspent = attr.PerkPointsCurrent, earned = attr.PerkPointsUnlocked, level = attr.Level;
                    text.horizontalOverflow = HorizontalWrapMode.Overflow;
                    text.verticalOverflow = VerticalWrapMode.Overflow;
                    text.text = "Perk points: " + unspent + " unspent / " + earned + " earned";

                    // Diagnostic: lets us see whether the numbers the game stores match its own earning rule.
                    string key = attr.AttributeSkillType + "|" + level + "|" + earned + "|" + unspent;
                    if (key != _lastLog)
                    {
                        _lastLog = key;
                        Plugin.Log.LogInfo("Perks window: " + attr.AttributeSkillType + " level " + level + ", earned " + earned +
                                           ", unspent " + unspent + " (level alone allows " + (Math.Min(level, 100) / 5) + ").");
                    }
                }
                catch (Exception e) { Plugin.Log.LogError("PerkDisplay (window): " + e); }
            }
        }

        private static Dreadmist.AttributeData CurrentAttribute(Dreadmist.Definitions.PerkDefinition def)
        {
            if (!Dreadmist.PlayerManager.Exists) return null;
            var player = Dreadmist.PlayerManager.Instance.FirstSelectedEntity;
            if (player == null) return null;
            return player.SentientPlayerData.LevelData.GetAttributeSkill(def.Type);
        }

        [HarmonyPatch(typeof(Dreadmist.Definitions.PerkDefinition), "GetDisplay")]
        private static class TooltipPatch
        {
            private static void Postfix(Dreadmist.Definitions.PerkDefinition __instance, int perkLevel, int parentLevel, ref string __result)
            {
                try
                {
                    if (!Plugin.ClearPerkDisplay.Value || !__instance.HasLevels) return;

                    int next = perkLevel + 1;
                    var sb = new StringBuilder(__result);
                    sb.AppendLine().AppendLine();

                    if (next > __instance.LevelMax)
                    {
                        sb.Append("<color=#9fd39f>Maxed out.</color>");
                    }
                    else
                    {
                        var attr = CurrentAttribute(__instance);
                        string name = Dreadmist.Localizer.GetAttributeSkillLocalized(__instance.Type);
                        int required = __instance.Levels[next].RequireLevel;

                        sb.AppendLine("<color=orange>Next level (" + (next + 1) + ") costs 1 perk point.</color>");
                        if (attr != null)
                            sb.AppendLine("You have " + attr.PerkPointsCurrent + " unspent (" + attr.PerkPointsUnlocked + " earned) for " + name + ".");

                        var problems = new List<string>();
                        if (required > parentLevel)
                            problems.Add("Needs " + name + " level " + required + " (you have " + parentLevel + ").");
                        if (attr != null && attr.PerkPointsCurrent <= 0)
                        {
                            problems.Add("No unspent points. You earn 1 point for every 5 " + name + " levels" +
                                         (parentLevel < 100 ? " (next one at level " + ((parentLevel / 5 + 1) * 5) + ")." : " (all earned)."));
                        }

                        if (problems.Count == 0) sb.Append("<color=#9fd39f>Ready: you can buy this now.</color>");
                        else sb.Append("<color=red>" + string.Join("\n", problems.ToArray()) + "</color>");
                    }
                    __result = sb.ToString();
                }
                catch (Exception e) { Plugin.Log.LogError("PerkDisplay (tooltip): " + e); }
            }
        }
    }
}
