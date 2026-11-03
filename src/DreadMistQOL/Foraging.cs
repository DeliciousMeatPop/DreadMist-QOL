using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace DreadMistQOL
{
    /// <summary>
    /// Optional foraging rules for bushes, flowers and similar "Foraging" plants (never trees or fruit trees):
    ///  1) PROTECT: the last harvest destroys the plant, so by default harvesting stops when 1 is left. A warning is
    ///     added to the hover text. Hold the override key while interacting to harvest the last one anyway.
    ///  2) REGROW: a plant you left partly harvested slowly recovers one harvest every few in-game days.
    /// How the game works (verified from its code): each plant counts harvests used (HarvestCurrent) against a limit
    /// (HarvestLimit); hover text shows "Harvests: remaining/limit"; reaching the limit destroys it.
    /// </summary>
    internal static class Foraging
    {
        private static readonly FieldInfo HarvestCurrentField =
            AccessTools.Field(typeof(InteractableResourceDestructibleController), "harvestCurrent");

        private sealed class State { public int Seen; public int LastChangeDay; }
        private static readonly Dictionary<int, State> States = new Dictionary<int, State>();
        private static int _lastDay = int.MinValue;
        private static float _nextCheck;

        /// <summary>True while the player holds the override key (updated from OnGUI by the plugin).</summary>
        public static bool OverrideHeld;

        public static void Install(Harmony harmony)
        {
            harmony.PatchAll(typeof(CanHarvestPatch));
            harmony.PatchAll(typeof(HoverPatch));
            Plugin.Log.LogInfo("Foraging rules installed (protect last harvest: " + Plugin.ProtectLastHarvest.Value +
                               ", regrow every " + Plugin.RegrowDays.Value + " day(s)).");
        }

        /// <summary>True for foraged plants only (skill Foraging, not a tree).</summary>
        private static bool IsForagedPlant(InteractableResourceDestructibleController c)
        {
            try
            {
                return c != null && !c.IsTree && c.SkillUsed.ToString() == "Foraging";
            }
            catch { return false; }
        }

        private static int Remaining(InteractableResourceDestructibleController c)
        {
            return c.HarvestLimit - c.HarvestCurrent;
        }

        /// <summary>Plants with a limit of 1 are single-use by design, so they are never protected.</summary>
        private static bool IsProtected(InteractableResourceDestructibleController c)
        {
            return Plugin.ProtectLastHarvest.Value && !OverrideHeld && IsForagedPlant(c)
                   && c.HarvestLimit >= 2 && Remaining(c) <= 1;
        }

        [HarmonyPatch(typeof(InteractableResourceDestructibleController), "get_CanHarvest")]
        private static class CanHarvestPatch
        {
            private static void Postfix(InteractableResourceDestructibleController __instance, ref bool __result)
            {
                try
                {
                    if (__result && IsProtected(__instance)) __result = false;
                }
                catch (Exception e) { Plugin.Log.LogError("Foraging (CanHarvest): " + e); }
            }
        }

        [HarmonyPatch(typeof(InteractableResourceDestructibleController), "get_HoveredDisplay")]
        private static class HoverPatch
        {
            private static void Postfix(InteractableResourceDestructibleController __instance, ref string __result)
            {
                try
                {
                    if (!IsForagedPlant(__instance)) return;
                    if (Plugin.ProtectLastHarvest.Value && __instance.HarvestLimit >= 2 && Remaining(__instance) == 1)
                    {
                        __result += "\n<color=orange>Protected: harvesting the last one destroys this plant for good.\n" +
                                    "Hold " + Plugin.ForageOverrideKey.Value + " while interacting to harvest it anyway.</color>";
                    }
                    else if (Plugin.RegrowDays.Value > 0 && __instance.HarvestCurrent > 0 && Remaining(__instance) > 0)
                    {
                        __result += "\n<color=#9fd39f>Regrows 1 harvest every " + Plugin.RegrowDays.Value + " day(s) if left alone.</color>";
                    }
                }
                catch (Exception e) { Plugin.Log.LogError("Foraging (hover): " + e); }
            }
        }

        // ------------------------------------------------------------------ regrowth

        /// <summary>Called from Plugin.Update. Does work only when the in-game day changes.</summary>
        public static void Tick()
        {
            int every = Plugin.RegrowDays.Value;
            if (every <= 0) return;
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 5f;

            if (!GameApi.HasClock) return;
            int day = GameApi.Day;
            if (day == _lastDay) return;
            _lastDay = day;

            try { RegrowPass(day, every); }
            catch (Exception e) { Plugin.Log.LogError("Foraging regrowth failed: " + e); }
        }

        private static void RegrowPass(int day, int every)
        {
            var seenNow = new HashSet<int>();
            int regrown = 0;

            foreach (var c in UnityEngine.Object.FindObjectsByType<InteractableResourceDestructibleController>(FindObjectsSortMode.None))
            {
                if (!IsForagedPlant(c)) continue;
                int id = c.GetInstanceID();
                seenNow.Add(id);
                int cur = c.HarvestCurrent;

                State st;
                if (!States.TryGetValue(id, out st))
                {
                    States[id] = new State { Seen = cur, LastChangeDay = day };
                    continue;
                }
                if (cur != st.Seen) { st.Seen = cur; st.LastChangeDay = day; continue; } // someone harvested it

                if (cur > 0 && cur < c.HarvestLimit && day - st.LastChangeDay >= every)
                {
                    HarvestCurrentField.SetValue(c, cur - 1);
                    RefreshStage(c, cur - 1);
                    st.Seen = cur - 1;
                    st.LastChangeDay = day;
                    regrown++;
                }
            }

            // forget plants that no longer exist
            var gone = new List<int>();
            foreach (var k in States.Keys) { if (!seenNow.Contains(k)) gone.Add(k); }
            foreach (var k in gone) States.Remove(k);

            if (regrown > 0) Plugin.Log.LogInfo("Foraging: day " + day + ", " + regrown + " plant(s) regrew a harvest.");
        }

        /// <summary>Some plants swap meshes as they are used up; keep the visual in step with the count.</summary>
        private static void RefreshStage(InteractableResourceDestructibleController c, int harvestCurrent)
        {
            try
            {
                if (!(c is InteractableResourceDestructibleStagesController)) return;
                Type t = typeof(InteractableResourceDestructibleStagesController);
                Array stages = AccessTools.Field(t, "stages").GetValue(c) as Array;
                if (stages == null || stages.Length == 0) return;

                int index = 0;
                for (int i = stages.Length - 1; i >= 0; i--)
                {
                    object stage = stages.GetValue(i);
                    int need = (int)stage.GetType().GetProperty("HarvestCount").GetValue(stage, null);
                    if (need <= harvestCurrent) { index = i; break; }
                }
                AccessTools.Method(t, "SetStage").Invoke(c, new object[] { index });
            }
            catch (Exception e) { Plugin.Log.LogError("Foraging (stage refresh): " + e); }
        }
    }
}
