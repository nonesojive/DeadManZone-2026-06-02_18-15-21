using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>M3 rarity weight table + pity rules. The table values are "initial,
    /// tune in playtest" — these tests lock the INVARIANTS (sums, monotonicity,
    /// clamps, force conditions), plus the two endpoint rows so a retune is a
    /// conscious edit here too.</summary>
    public sealed class RarityWeightsTests
    {
        [Test]
        public void Rows_SumTo100_AcrossTheClock()
        {
            for (int fe = 0; fe <= 20; fe++)
            {
                var row = RarityWeights.WeightsFor(fe);
                Assert.AreEqual(
                    100,
                    row.CommonPercent + row.UncommonPercent + row.RarePercent,
                    $"fightEquivalent={fe}");
                Assert.That(row.CommonPercent, Is.GreaterThan(0), $"fe={fe} common");
                Assert.That(row.UncommonPercent, Is.GreaterThan(0), $"fe={fe} uncommon");
                Assert.That(row.RarePercent, Is.GreaterThan(0), $"fe={fe} rare");
            }
        }

        [Test]
        public void RareShare_NeverDecreases_AsTheClockClimbs()
        {
            int previousRare = -1;
            int previousCommon = int.MaxValue;
            for (int fe = 1; fe <= 20; fe++)
            {
                var row = RarityWeights.WeightsFor(fe);
                Assert.That(row.RarePercent, Is.GreaterThanOrEqualTo(previousRare), $"fe={fe}");
                Assert.That(row.CommonPercent, Is.LessThanOrEqualTo(previousCommon), $"fe={fe}");
                previousRare = row.RarePercent;
                previousCommon = row.CommonPercent;
            }
        }

        [Test]
        public void Endpoints_MatchTheAuthoredCurve()
        {
            var early = RarityWeights.WeightsFor(1);
            Assert.AreEqual(80, early.CommonPercent);
            Assert.AreEqual(18, early.UncommonPercent);
            Assert.AreEqual(2, early.RarePercent);

            var late = RarityWeights.WeightsFor(9);
            Assert.AreEqual(55, late.CommonPercent);
            Assert.AreEqual(30, late.UncommonPercent);
            Assert.AreEqual(15, late.RarePercent);

            // Clamps: below the table start and past its end.
            Assert.AreEqual(2, RarityWeights.WeightsFor(0).RarePercent);
            Assert.AreEqual(15, RarityWeights.WeightsFor(99).RarePercent);
        }

        [Test]
        public void PityBoost_StepsRareOdds_AndClampsAtTheCap()
        {
            Assert.AreEqual(2, RarityWeights.RareChancePercent(1, 0));
            Assert.AreEqual(
                2 + RarityWeights.PityStepPercent * 3,
                RarityWeights.RareChancePercent(1, 3));
            Assert.AreEqual(
                RarityWeights.RareOddsCapPercent,
                RarityWeights.RareChancePercent(9, 100));
            // Negative pity is treated as zero (defensive).
            Assert.AreEqual(2, RarityWeights.RareChancePercent(1, -3));
        }

        [Test]
        public void ForcesRare_AtTheGuaranteeCount_OrAtTheOddsCap()
        {
            Assert.IsFalse(RarityWeights.ForcesRare(1, 0));
            Assert.IsFalse(RarityWeights.ForcesRare(1, RarityWeights.PityGuaranteeBatches - 1));
            Assert.IsTrue(RarityWeights.ForcesRare(1, RarityWeights.PityGuaranteeBatches));

            // Late game the odds cap (base 15 + 4/batch) arrives before the batch count.
            Assert.IsFalse(RarityWeights.ForcesRare(10, 6)); // 15 + 24 = 39 < 40
            Assert.IsTrue(RarityWeights.ForcesRare(10, 7));  // 15 + 28 = 43 → clamped ≥ cap
        }

        [Test]
        public void RollTier_TracksTheTable_DeterministicallyAtAFixedSeed()
        {
            int CountRares(int fightEquivalent)
            {
                var rng = new Rng(1234);
                int rares = 0;
                for (int i = 0; i < 10_000; i++)
                {
                    if (RarityWeights.RollTier(rng, fightEquivalent, 0) == Rarity.Rare)
                        rares++;
                }

                return rares;
            }

            int early = CountRares(1);
            int late = CountRares(10);

            // ~2% vs ~15% of 10k rolls; generous bounds, fixed seed → deterministic.
            Assert.That(early, Is.InRange(100, 350), "early rare share");
            Assert.That(late, Is.InRange(1200, 1800), "late rare share");
            Assert.That(late, Is.GreaterThan(early * 3));
        }

        [Test]
        public void RollTier_PityFeedsRareFromCommonFirst()
        {
            int Count(Rarity tier, int pity)
            {
                var rng = new Rng(777);
                int count = 0;
                for (int i = 0; i < 10_000; i++)
                {
                    if (RarityWeights.RollTier(rng, 1, pity) == tier)
                        count++;
                }

                return count;
            }

            // At pity 5 (rare 2 + 20 = 22%) the uncommon share must hold ~18% —
            // the boost comes out of the common share.
            Assert.That(Count(Rarity.Rare, 5), Is.InRange(1800, 2600));
            Assert.That(Count(Rarity.Uncommon, 5), Is.InRange(1400, 2200));
        }
    }
}
