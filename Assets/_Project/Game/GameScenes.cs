using DeadManZone.Data;
using UnityEngine.SceneManagement;

namespace DeadManZone.Game
{
    public static class GameScenes
    {
        public const string MainMenu = "MainMenu";
        public const string Run = "Run";
        public const string CombatArena = "CombatArena";
        public const string CombatArena2D = "CombatArena2D";

        public static string ResolveCombatArenaScene(CombatArenaConfigSO config) =>
            config != null && config.visualMode == CombatArenaVisualMode.TopTroops2D
                ? CombatArena2D
                : CombatArena;

        public static void LoadMainMenu() => SceneManager.LoadScene(MainMenu);

        public static void LoadRun() => SceneManager.LoadScene(Run);
    }
}
