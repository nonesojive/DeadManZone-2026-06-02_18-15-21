using DeadManZone.Data;
using UnityEngine.SceneManagement;

namespace DeadManZone.Game
{
    public static class GameScenes
    {
        public const string MainMenu = "MainMenu";
        public const string Run = "Run";
        public const string CombatArena2D = "CombatArena2D";
        public const string CombatArena3D = "CombatArena3D";

        /// <summary>The shared CombatArenaConfig's visualMode is THE 2D/3D switch: ToonInk3D
        /// loads the 3D arena scene, everything else keeps the proven 2D path byte-identical.
        /// (Future battlefield themes: branch here on fight/encounter data — each theme is
        /// its own scene so it owns its lighting/fog RenderSettings.)</summary>
        public static string ResolveCombatArenaScene(CombatArenaConfigSO config) =>
            config != null && config.visualMode == CombatArenaVisualMode.ToonInk3D
                ? CombatArena3D
                : CombatArena2D;

        public static void LoadMainMenu() => SceneManager.LoadScene(MainMenu);

        public static void LoadRun() => SceneManager.LoadScene(Run);
    }
}
