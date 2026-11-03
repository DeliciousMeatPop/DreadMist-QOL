using HarmonyLib;

namespace DreadMistQOL
{
    /// <summary>Optional gameplay tweaks. Each one has its own switch in the config file and is OFF by default.</summary>
    internal static class Tweaks
    {
        public static void Install(Harmony harmony)
        {
            harmony.PatchAll(typeof(GoldWeightPatch));
        }

        /// <summary>
        /// ItemData.Weight is the single source of an item's weight; stack weight and the inventory total are built from it.
        /// Returning 0 for coins makes gold weightless without touching any other item.
        /// </summary>
        [HarmonyPatch(typeof(Dreadmist.ItemData), "get_Weight")]
        private static class GoldWeightPatch
        {
            private static void Postfix(Dreadmist.ItemData __instance, ref float __result)
            {
                if (Plugin.GoldWeighsNothing.Value && __instance.IsCoin) __result = 0f;
            }
        }
    }
}
