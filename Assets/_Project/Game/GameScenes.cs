using DeadManZone.Data;
using UnityEngine.SceneManagement;

namespace DeadManZone.Game
{
    public static class GameScenes
    {
        public const string MainMenu = "MainMenu";
        public const string Run = "Run";
        public const string CombatArena2D = "CombatArena2D";

        // Combat is locked to the 2D arena; the legacy 3D scene was removed.
        public static string ResolveCombatArenaScene(CombatArenaConfigSO config) => CombatArena2D;

        public static void LoadMainMenu() => SceneManager.LoadScene(MainMenu);

        public static void LoadRun() => SceneManager.LoadScene(Run);
    }
}
