using System.Linq;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatArenaBackdropAssemblerTests
    {
        [Test]
        public void ResolveRings_WhenProfileIsNull_ReturnsLegacyCatalogRings()
        {
            var rings = CombatArenaBackdropAssembler.ResolveRings(null);

            Assert.AreEqual(3, rings.Count);
            Assert.IsTrue(rings.Any(r => r.RingType == CombatArenaBackdropRing.TrenchDressing));
            Assert.IsTrue(rings.Any(r => r.RingType == CombatArenaBackdropRing.Skyline));
            Assert.IsTrue(rings.Any(r => r.RingType == CombatArenaBackdropRing.AtmosphereFx));
        }

        [Test]
        public void ResolveRings_FiltersDisabledRingAssets()
        {
            var profile = ScriptableObject.CreateInstance<CombatArenaAtmosphereProfileSO>();
            var dressing = ScriptableObject.CreateInstance<CombatArenaBackdropRingSO>();
            dressing.ring = CombatArenaBackdropRing.TrenchDressing;
            dressing.enabled = true;
            dressing.childRootName = "TrenchDressing";
            dressing.prefabPaths = new[] { "Assets/Test/Trench.prefab" };

            var skyline = ScriptableObject.CreateInstance<CombatArenaBackdropRingSO>();
            skyline.ring = CombatArenaBackdropRing.Skyline;
            skyline.enabled = false;
            skyline.childRootName = "SkylineBackdrop";

            profile.backdropRings = new[] { dressing, skyline };

            try
            {
                var rings = CombatArenaBackdropAssembler.ResolveRings(profile);

                Assert.IsTrue(rings.Any(r => r.RingType == CombatArenaBackdropRing.TrenchDressing));
                Assert.IsFalse(rings.Any(r => r.RingType == CombatArenaBackdropRing.Skyline));
            }
            finally
            {
                Object.DestroyImmediate(skyline);
                Object.DestroyImmediate(dressing);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void ResolveCatalogLengths_UsesRingAssetPrefabCounts()
        {
            var profile = ScriptableObject.CreateInstance<CombatArenaAtmosphereProfileSO>();
            var dressing = ScriptableObject.CreateInstance<CombatArenaBackdropRingSO>();
            dressing.ring = CombatArenaBackdropRing.TrenchDressing;
            dressing.prefabPaths = new[] { "a.prefab", "b.prefab", "c.prefab" };

            var skyline = ScriptableObject.CreateInstance<CombatArenaBackdropRingSO>();
            skyline.ring = CombatArenaBackdropRing.Skyline;
            skyline.prefabPaths = new[] { "x.prefab" };

            profile.backdropRings = new[] { dressing, skyline };

            try
            {
                var lengths = CombatArenaBackdropAssembler.ResolveCatalogLengths(profile);

                Assert.AreEqual(3, lengths.TrenchDressing);
                Assert.AreEqual(1, lengths.Skyline);
                Assert.AreEqual(0, lengths.AtmosphereFx);
            }
            finally
            {
                Object.DestroyImmediate(skyline);
                Object.DestroyImmediate(dressing);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void FilterPointsForRing_ReturnsOnlyMatchingRingPoints()
        {
            var points = CombatArenaBackdropLayout.BuildLayout(14.4f, 5.4f, 424242);

            var dressing = CombatArenaBackdropAssembler.FilterPointsForRing(
                points,
                CombatArenaBackdropRing.TrenchDressing);

            Assert.IsNotEmpty(dressing);
            Assert.IsTrue(dressing.All(p => p.Ring == CombatArenaBackdropRing.TrenchDressing));
        }
    }
}
