namespace DreadMistQOL
{
    /// <summary>Names accepted by the game's SetLevel / AddExperience console commands (verified from the game code).</summary>
    internal static class Names
    {
        public static readonly string[] Attributes =
        {
            "Strength", "Agility", "Endurance", "Vitality", "Knowledge", "Courage", "Personality"
        };

        public static readonly string[] Skills =
        {
            "MeleeCombat", "Blocking", "Marksmanship", "HandToHand", "Dodging", "WeaponBlade", "WeaponAxe",
            "WeaponBlunt", "WeaponPolearm", "WeaponBow", "WeaponCrossbow", "WeaponThrown", "ArmorLight",
            "ArmorMedium", "ArmorHeavy", "ArcaneControl", "Smithing", "WeaponSmithing", "Armorer",
            "JewelSmithing", "Tailoring", "Bowyery", "Alchemy", "Cooking", "Brewing", "WoodWorking", "Tanning",
            "Enchanting", "RunicEngineering", "Sneaking", "LockPicking", "Stealing", "Takedowns", "Mining",
            "Logging", "Farming", "Fishing", "Athletics", "Swimming", "Medical", "Butchering", "Speechcraft",
            "Bartering", "Reading", "Gambling", "Herbalism", "Necromancy", "Construction", "Musician",
            "Foraging", "Tinkering", "Chymistry"
        };

        public static readonly string[] AttributesAndSkills = Combine();

        private static string[] Combine()
        {
            var all = new string[Attributes.Length + Skills.Length];
            Attributes.CopyTo(all, 0);
            Skills.CopyTo(all, Attributes.Length);
            return all;
        }
    }
}
