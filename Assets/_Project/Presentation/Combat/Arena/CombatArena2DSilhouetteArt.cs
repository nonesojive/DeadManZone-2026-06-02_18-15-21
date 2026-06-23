using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Loads role silhouette sprites from Resources; falls back to procedural placeholders.</summary>
    public static class CombatArena2DSilhouetteArt
    {
        private const string ResourcePath = "DeadManZone/CombatArena2DSilhouetteArt";

        private static CombatArena2DSilhouetteArtSO _cached;

        public static CombatArena2DSilhouetteArtSO Load()
        {
            if (_cached == null)
                _cached = Resources.Load<CombatArena2DSilhouetteArtSO>(ResourcePath);
            return _cached;
        }

        public static Sprite ForRole(CombatArena2DSilhouetteRole role)
        {
            var art = Load();
            if (art == null)
                return null;

            return role switch
            {
                CombatArena2DSilhouetteRole.Assault => art.assault,
                CombatArena2DSilhouetteRole.Ranged => art.ranged,
                CombatArena2DSilhouetteRole.Artillery => art.artillery,
                CombatArena2DSilhouetteRole.Vehicle => art.vehicle,
                _ => art.generic
            };
        }

        internal static void ClearCacheForTests() => _cached = null;
    }
}
