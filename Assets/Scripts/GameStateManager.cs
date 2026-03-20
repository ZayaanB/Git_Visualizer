using UnityEngine;

namespace GitVisualizer
{
    /// <summary>
    /// Static game state passed between scenes (e.g., Main Menu -> Main Game).
    /// </summary>
    public static class GameStateManager
    {
        public enum GameMode
        {
            Solo,
            Coop
        }

        public static GameMode CurrentMode { get; private set; } = GameMode.Solo;

        public static bool IsCoopMode => CurrentMode == GameMode.Coop;

        public static void SetSoloMode() => CurrentMode = GameMode.Solo;

        public static void SetCoopMode() => CurrentMode = GameMode.Coop;
    }
}
