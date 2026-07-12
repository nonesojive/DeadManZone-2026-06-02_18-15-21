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

        /// <summary>True if an additive arena scene (2D or 3D) is actually loaded, regardless
        /// of any per-loader flag — catches the defeat path where the flag desyncs. Checks
        /// both names rather than resolving the config: a config switch mid-session must not
        /// strand the previously loaded arena.</summary>
        public static bool IsSceneLoaded =>
            SceneManager.GetSceneByName(GameScenes.CombatArena2D).isLoaded ||
            SceneManager.GetSceneByName(GameScenes.CombatArena3D).isLoaded;

        /// <summary>Guarantee the arena is gone. Safe to call whenever we enter the shop.</summary>
        public static void RequestUnload()
        {
            if (_loader != null)
            {
                _loader.RequestUnload();
                return;
            }

            if (SceneManager.GetSceneByName(GameScenes.CombatArena2D).isLoaded)
                SceneManager.UnloadSceneAsync(GameScenes.CombatArena2D);
            if (SceneManager.GetSceneByName(GameScenes.CombatArena3D).isLoaded)
                SceneManager.UnloadSceneAsync(GameScenes.CombatArena3D);
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
