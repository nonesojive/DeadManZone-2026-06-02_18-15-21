using System.Linq;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatArenaBackdropLayoutTests
    {
        private const float HalfWidth = 14.4f;
        private const float HalfDepth = 5.4f;

        [Test]
        public void BuildLayout_TrenchDressing_SitsOutsidePlayableBounds()
        {
            var points = CombatArenaBackdropLayout.BuildLayout(HalfWidth, HalfDepth, 424242);

            var dressing = points.Where(p => p.Ring == CombatArenaBackdropRing.TrenchDressing).ToList();
            Assert.IsNotEmpty(dressing, "Expected trench dressing spawn points.");

            foreach (var point in dressing)
            {
                bool outsideX = Mathf.Abs(point.LocalPosition.x) > HalfWidth + 1.5f;
                bool outsideZ = Mathf.Abs(point.LocalPosition.z) > HalfDepth + 1.5f;
                Assert.IsTrue(outsideX || outsideZ,
                    $"Dressing point {point.LocalPosition} should sit outside the playable board.");
            }
        }

        [Test]
        public void BuildLayout_Skyline_IsFartherThanDressing()
        {
            var points = CombatArenaBackdropLayout.BuildLayout(HalfWidth, HalfDepth, 424242);

            float dressingMax = points
                .Where(p => p.Ring == CombatArenaBackdropRing.TrenchDressing)
                .Max(p => p.LocalPosition.magnitude);

            float skylineMin = points
                .Where(p => p.Ring == CombatArenaBackdropRing.Skyline)
                .Min(p => p.LocalPosition.magnitude);

            Assert.Greater(skylineMin, dressingMax + 4f,
                "Skyline silhouettes should sit beyond the trench dressing ring.");
        }

        [Test]
        public void BuildLayout_WithAtmosphereFxDisabled_DoesNotEmitFxPoints()
        {
            Assert.DoesNotThrow(() =>
                CombatArenaBackdropLayout.BuildLayout(HalfWidth, HalfDepth, 424242, includeAtmosphereFx: false));

            var points = CombatArenaBackdropLayout.BuildLayout(HalfWidth, HalfDepth, 424242, includeAtmosphereFx: false);
            Assert.IsFalse(points.Any(p => p.Ring == CombatArenaBackdropRing.AtmosphereFx),
                "Combat layout should omit atmosphere FX when disabled.");
        }

        [Test]
        public void BuildLayout_WithAtmosphereFxEnabledAndEmptyCatalog_DoesNotThrowOrEmitFxPoints()
        {
            Assert.DoesNotThrow(() =>
                CombatArenaBackdropLayout.BuildLayout(HalfWidth, HalfDepth, 424242, includeAtmosphereFx: true));

            var points = CombatArenaBackdropLayout.BuildLayout(HalfWidth, HalfDepth, 424242, includeAtmosphereFx: true);
            Assert.IsFalse(points.Any(p => p.Ring == CombatArenaBackdropRing.AtmosphereFx),
                "Empty atmosphere FX catalog must not emit spawn points or divide by zero.");
        }

        [Test]
        public void BuildLayout_IsDeterministicForSeed()
        {
            var first = CombatArenaBackdropLayout.BuildLayout(HalfWidth, HalfDepth, 424242);
            var second = CombatArenaBackdropLayout.BuildLayout(HalfWidth, HalfDepth, 424242);

            Assert.AreEqual(first.Count, second.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].Ring, second[i].Ring);
                Assert.That(first[i].LocalPosition, Is.EqualTo(second[i].LocalPosition).Within(0.001f));
            }
        }
    }
}
