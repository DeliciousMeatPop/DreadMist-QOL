using Dreadmist;

namespace DreadMistQOL
{
    /// <summary>
    /// The only file that does "using Dreadmist;" (the game's namespace), so a name clash can't leak into the rest of the plugin.
    /// </summary>
    internal static class GameApi
    {
        public static bool HasClock { get { return WorldManager.Exists; } }

        /// <summary>The in-game day number (same value the game's own day conditions use).</summary>
        /// <summary>The game clock the plants' respawn timers use (same units as InteractableResourceDestructibleController.RespawnTime).</summary>
        public static float GameTime { get { return WorldManager.Instance.GameTime; } }

        public static int Day { get { return WorldManager.Instance.CurrentDay; } }
    }
}
