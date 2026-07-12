using DeadManZone.Core.Run;
using UnityEngine.SceneManagement;

namespace DeadManZone.Game
{
    public static class GameScenes
    {
        public const string MainMenu = "MainMenu";
        public const string Run = "Run";

        // Scene-per-theme (M4): each arena scene owns its lighting/fog RenderSettings,
        // which are per-scene and only apply while it is the active scene. The original
        // CombatArena3D scene IS the Trenchline theme — it keeps its name so pre-M4
        // build settings, saves and tools stay valid.
        public const string CombatArena3D = "CombatArena3D";
        public const string CombatArenaFogField = "CombatArena3D_FogField";
        public const string CombatArenaRavagedTown = "CombatArena3D_RavagedTown";
        public const string CombatArenaWartornForest = "CombatArena3D_WartornForest";

        /// <summary>Every arena scene, for callers that must find whichever one lingers
        /// loaded (e.g. the defeat-path unload safety net).</summary>
        public static readonly string[] AllCombatArenaScenes =
        {
            CombatArena3D, CombatArenaFogField, CombatArenaRavagedTown, CombatArenaWartornForest
        };

        /// <summary>Arena scene for a theme. Null/unknown ids normalize to the default
        /// theme (legacy saves, unshipped themes) — the Trenchline arena.</summary>
        public static string ResolveCombatArenaScene(string arenaThemeId = null) =>
            ArenaThemes.Normalize(arenaThemeId) switch
            {
                ArenaThemes.FogField => CombatArenaFogField,
                ArenaThemes.RavagedTown => CombatArenaRavagedTown,
                ArenaThemes.WartornForest => CombatArenaWartornForest,
                _ => CombatArena3D
            };

        public static void LoadMainMenu() => SceneManager.LoadScene(MainMenu);

        public static void LoadRun() => SceneManager.LoadScene(Run);
    }
}
