using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadManZone.Game
{
    /// <summary>Ensures Play Mode always opens MainMenu, even when Run scene is active in the editor.</summary>
    public static class GamePlayBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureMainMenuOnPlay()
        {
            var active = SceneManager.GetActiveScene();
            if (active.name != GameScenes.Run)
                return;

            SceneManager.LoadScene(GameScenes.MainMenu);
        }
    }
}
