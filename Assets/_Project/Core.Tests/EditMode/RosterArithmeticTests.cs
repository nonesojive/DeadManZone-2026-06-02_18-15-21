using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>2026-07-15 faction-roster-v1: §1.1 roster arithmetic (Neutral 4C/3U/0R,
    /// IronMarch 6C/3U/3R) and the §3 sniper Critical Mass rule (≈2/4/6 → +accuracy, then
    /// +damage%). The roster counts read the shipped ContentDatabase, so they only reflect
    /// the new roster after "DeadManZone → Content → Generate IronMarch Union Content Pass"
    /// has been run in-editor; self-ignore otherwise, same pattern as BalancePassTests.</summary>
    public sealed class RosterArithmeticTests
    {
        private ContentDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _database = ContentDatabase.Load();
            if (_database == null || _database.Pieces.Count == 0)
                Assert.Ignore(DeadManZoneTestContent.MissingDatabaseHint);
        }

        [Test]
        public void Neutral_Roster_Is4Common3Uncommon0Rare()
        {
            var neutral = _database.Pieces.Where(p => p.factionId == "neutral").ToList();
            if (neutral.Count == 0)
                Assert.Ignore("Neutral roster not yet regenerated — run the IronMarch content pass.");

            AssertRarityCounts(neutral, expectedCommon: 4, expectedUncommon: 3, expectedRare: 0, factionLabel: "Neutral");
        }

        [Test]
        public void IronmarchUnion_Roster_Is6Common3Uncommon3Rare()
        {
            var ironmarch = _database.Pieces.Where(p => p.factionId == FactionIds.IronmarchUnion).ToList();
            if (ironmarch.Count == 0)
                Assert.Ignore("IronMarch roster not yet regenerated — run the IronMarch content pass.");

            AssertRarityCounts(ironmarch, expectedCommon: 6, expectedUncommon: 3, expectedRare: 3, factionLabel: "IronMarch Union");
        }

        private static void AssertRarityCounts(
            List<PieceDefinitionSO> pieces,
            int expectedCommon,
            int expectedUncommon,
            int expectedRare,
            string factionLabel)
        {
            int common = pieces.Count(p => p.rarity == Rarity.Common);
            int uncommon = pieces.Count(p => p.rarity == Rarity.Uncommon);
            int rare = pieces.Count(p => p.rarity == Rarity.Rare);

            Assert.AreEqual(expectedCommon, common, $"{factionLabel}: expected {expectedCommon} commons");
            Assert.AreEqual(expectedUncommon, uncommon, $"{factionLabel}: expected {expectedUncommon} uncommons");
            Assert.AreEqual(expectedRare, rare, $"{factionLabel}: expected {expectedRare} rares");
            Assert.AreEqual(expectedCommon + expectedUncommon + expectedRare, pieces.Count,
                $"{factionLabel}: roster size must equal the commons+uncommons+rares total");
        }

        [Test]
        public void SniperCriticalMassRule_ThresholdsApproximate2_4_6()
        {
            var rules = CriticalMassDefaultRules.Build();

            var accuracyRule = rules.FirstOrDefault(r => r.Id == "sniper_accuracy");
            Assert.IsNotNull(accuracyRule.Tiers, "sniper_accuracy rule must exist");
            Assert.AreEqual(GameTagIds.Sniper, accuracyRule.CountTagId);
            Assert.AreEqual(CriticalMassStat.Accuracy, accuracyRule.Stat);
            Assert.AreEqual(2, accuracyRule.Tiers[0].Threshold, "sniper accuracy kicks in at ~2 snipers");
            Assert.Greater(accuracyRule.Tiers[0].Magnitude, 0, "the low threshold must actually grant +accuracy");

            var damageRule = rules.FirstOrDefault(r => r.Id == "sniper_damage");
            Assert.IsNotNull(damageRule.Tiers, "sniper_damage rule must exist");
            Assert.AreEqual(GameTagIds.Sniper, damageRule.CountTagId);
            Assert.AreEqual(CriticalMassStat.Damage, damageRule.Stat);
            Assert.AreEqual(SynergyModType.Percent, damageRule.ModType, "spec calls for +damage%, not flat damage");
            Assert.AreEqual(4, damageRule.Tiers[1].Threshold, "damage% ramp lands at ~4 snipers");
            Assert.AreEqual(6, damageRule.Tiers[2].Threshold, "damage% ramp tops out at ~6 snipers");
            Assert.Greater(damageRule.Tiers[2].Magnitude, damageRule.Tiers[1].Magnitude,
                "damage% must keep climbing across the later tiers");
        }
    }
}
