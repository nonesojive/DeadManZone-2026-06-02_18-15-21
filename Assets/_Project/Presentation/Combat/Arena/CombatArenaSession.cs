using DeadManZone.Game;
using UnityEngine.SceneManagement;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Read-only view of whether the additive combat arena scene is loaded.
    /// Only <see cref="CombatArenaSceneLoader"/> mutates the bound loader.
    /// </summary>
    public static class CombatArenaSession
    {
        private static CombatArenaSceneLoader _loader;

        public static bool IsActive => _loader != null && _loader.IsLoaded;

        /// <summary>True if ANY arena scene is actually loaded, regardless of any
        /// per-loader flag — catches the defeat path where the flag desyncs.
        /// Checks every theme scene (M4): checking only CombatArena3D let a themed
        /// arena survive behind the shop, and the next fight stacked a second arena
        /// on top of it (two environments, two audio listeners, frozen wiring).</summary>
        public static bool IsSceneLoaded
        {
            get
            {
                foreach (var sceneName in GameScenes.AllCombatArenaScenes)
                {
                    if (SceneManager.GetSceneByName(sceneName).isLoaded)
                        return true;
                }

                return false;
            }
        }

        /// <summary>Guarantee the arena is gone. Safe to call whenever we enter the shop.</summary>
        public static void RequestUnload()
        {
            if (_loader != null && _loader.isActiveAndEnabled)
            {
                _loader.RequestUnload();
                return;
            }

            // Loader missing OR inactive (the loader lives on CombatPanel, which the
            // Build phase deactivates — its coroutine can't run then): unload directly.
            foreach (var sceneName in GameScenes.AllCombatArenaScenes)
            {
                if (SceneManager.GetSceneByName(sceneName).isLoaded)
                    SceneManager.UnloadSceneAsync(sceneName);
            }
        }

        internal static void Bind(CombatArenaSceneLoader loader) => _loader = loader;

        internal static void Unbind(CombatArenaSceneLoader loader)
        {
            if (_loader == loader)
                _loader = null;
        }

        public static void ResetForTests() => _loader = null;
    }
}
