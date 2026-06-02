using UnityEngine.SceneManagement;

namespace DeadManZone.Game
{
    public static class GameScenes
    {
        public const string MainMenu = "MainMenu";
        public const string Run = "Run";

        public static void LoadMainMenu() => SceneManager.LoadScene(MainMenu);

        public static void LoadRun() => SceneManager.LoadScene(Run);
    }
}
