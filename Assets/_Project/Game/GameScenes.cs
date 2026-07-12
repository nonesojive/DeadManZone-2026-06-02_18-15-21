using UnityEngine.SceneManagement;

namespace DeadManZone.Game
{
    public static class GameScenes
    {
        public const string MainMenu = "MainMenu";
        public const string Run = "Run";
        public const string CombatArena3D = "CombatArena3D";

        /// <summary>ToonInk3D is the only combat renderer — the 2D sprite arena was deleted.
        /// (Future battlefield themes: branch here on fight/encounter data — each theme is
        /// its own scene so it owns its lighting/fog RenderSettings.)</summary>
        public static string ResolveCombatArenaScene() => CombatArena3D;

        public static void LoadMainMenu() => SceneManager.LoadScene(MainMenu);

        public static void LoadRun() => SceneManager.LoadScene(Run);
    }
}
