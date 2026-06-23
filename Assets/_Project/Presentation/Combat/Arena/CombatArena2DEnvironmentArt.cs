using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Loads Combat2D environment sprites from Resources; falls back to placeholders when unset.</summary>
    public static class CombatArena2DEnvironmentArt
    {
        private const string ResourcePath = "DeadManZone/CombatArena2DEnvironmentArt";

        private static CombatArena2DEnvironmentArtSO _cached;

        public static CombatArena2DEnvironmentArtSO Load()
        {
            if (_cached == null)
                _cached = Resources.Load<CombatArena2DEnvironmentArtSO>(ResourcePath);
            return _cached;
        }

        public static bool HasGridArt =>
            Load()?.gridCellLight != null && Load()?.gridCellDark != null;

        public static Sprite GridCellLight => Load()?.gridCellLight;

        public static Sprite GridCellDark => Load()?.gridCellDark;

        public static Sprite GridBackdrop => Load()?.gridBackdrop;

        public static Sprite SkyGradient => Load()?.skyGradient;

        public static Sprite UnitShadow =>
            Load()?.shadowUnit ?? CombatArena2DPlaceholderSprites.Shadow;

        public static Sprite BuildingShadow =>
            Load()?.shadowBuilding ?? UnitShadow;

        internal static void ClearCacheForTests() => _cached = null;
    }
}
