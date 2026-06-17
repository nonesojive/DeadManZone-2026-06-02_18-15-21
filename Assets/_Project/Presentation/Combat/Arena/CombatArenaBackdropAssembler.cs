using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Assembles modular backdrop rings from profile ScriptableObjects or legacy catalog fallbacks.</summary>
    public static class CombatArenaBackdropAssembler
    {
        public static IReadOnlyList<ICombatArenaBackdropRing> ResolveRings(
            CombatArenaAtmosphereProfileSO profile)
        {
            if (profile?.backdropRings == null || profile.backdropRings.Length == 0)
                return CreateLegacyRings();

            var rings = new List<ICombatArenaBackdropRing>(profile.backdropRings.Length);
            foreach (var asset in profile.backdropRings)
            {
                if (asset == null || !asset.enabled)
                    continue;

                rings.Add(new ScriptableCombatArenaBackdropRing(asset));
            }

            return rings;
        }

        public static CombatArenaBackdropCatalogLengths ResolveCatalogLengths(
            CombatArenaAtmosphereProfileSO profile)
        {
            if (profile?.backdropRings == null || profile.backdropRings.Length == 0)
                return CombatArenaBackdropCatalogLengths.FromLegacyCatalog();

            var rings = ResolveRings(profile);
            return new CombatArenaBackdropCatalogLengths(
                GetRingPrefabCount(rings, CombatArenaBackdropRing.TrenchDressing),
                GetRingPrefabCount(rings, CombatArenaBackdropRing.Skyline),
                GetRingPrefabCount(rings, CombatArenaBackdropRing.AtmosphereFx));
        }

        public static IReadOnlyList<CombatArenaBackdropSpawnPoint> FilterPointsForRing(
            IReadOnlyList<CombatArenaBackdropSpawnPoint> points,
            CombatArenaBackdropRing ring) =>
            points.Where(p => p.Ring == ring).ToList();

        public static void Populate(
            Transform backdropRoot,
            BattlefieldLayout layout,
            CombatArenaConfigSO config,
            CombatArenaAtmosphereProfileSO profile)
        {
            if (backdropRoot == null || layout == null || config == null || profile == null)
                return;

            float halfWidth = layout.TotalWidth * config.cellWidth * 0.5f;
            float halfDepth = layout.Height * config.cellDepth * 0.5f;
            var catalogLengths = ResolveCatalogLengths(profile);
            var points = CombatArenaBackdropLayout.BuildLayout(
                halfWidth,
                halfDepth,
                profile.backdropSeed,
                catalogLengths,
                profile.enableAtmosphereFx);

            foreach (var ring in ResolveRings(profile))
            {
                if (!ring.IsEnabled)
                    continue;

                var parent = EnsureChildRoot(backdropRoot, ring.ChildRootName);
                int fxCount = 0;
                foreach (var point in points)
                {
                    if (point.Ring != ring.RingType)
                        continue;

                    if (point.Ring == CombatArenaBackdropRing.AtmosphereFx)
                    {
                        if (!profile.enableAtmosphereFx || fxCount >= profile.maxAtmosphereFxCount)
                            continue;

                        fxCount++;
                    }

                    CombatArenaBackdropSpawner.SpawnPoint(ring, parent, point);
                }
            }
        }

        private static int GetRingPrefabCount(
            IReadOnlyList<ICombatArenaBackdropRing> rings,
            CombatArenaBackdropRing ringType)
        {
            foreach (var ring in rings)
            {
                if (ring.RingType == ringType)
                    return ring.PrefabCount;
            }

            return 0;
        }

        private static IReadOnlyList<ICombatArenaBackdropRing> CreateLegacyRings() =>
            new ICombatArenaBackdropRing[]
            {
                new LegacyCatalogCombatArenaBackdropRing(CombatArenaBackdropRing.TrenchDressing),
                new LegacyCatalogCombatArenaBackdropRing(CombatArenaBackdropRing.Skyline),
                new LegacyCatalogCombatArenaBackdropRing(CombatArenaBackdropRing.AtmosphereFx)
            };

        private static Transform EnsureChildRoot(Transform backdropRoot, string name)
        {
            var existing = backdropRoot.Find(name);
            if (existing != null)
                return existing;

            var child = new GameObject(name).transform;
            child.SetParent(backdropRoot, false);
            return child;
        }
    }
}
