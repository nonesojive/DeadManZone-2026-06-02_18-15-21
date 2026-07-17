using DeadManZone.Core;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>2026-07-15 faction-roster-v1 §1.9/§4: FactionPassives is the single home for
    /// the per-faction economy/shop passives. Every query must be a no-op (false/0/unchanged)
    /// for a faction that doesn't own the passive, including factions with no FactionSO
    /// content yet (Paradox/Blightborn/Crimson) — this is what keeps existing seeded-run
    /// tests for IronMarch/other factions from drifting.</summary>
    public sealed class FactionPassivesTests
    {
        [Test]
        public void HasMercenarySlot_OnlyCartel()
        {
            Assert.IsTrue(FactionPassives.HasMercenarySlot(FactionIds.CartelOfEchoes));
            Assert.IsFalse(FactionPassives.HasMercenarySlot(FactionIds.IronmarchUnion));
            Assert.IsFalse(FactionPassives.HasMercenarySlot(FactionIds.DustScourge));
            Assert.IsFalse(FactionPassives.HasMercenarySlot(null));
        }

        [Test]
        public void MercenarySurchargeFor_CartelOnly_ReturnsPercent()
        {
            Assert.AreEqual(FactionPassives.MercenarySurchargePercent,
                FactionPassives.MercenarySurchargeFor(FactionIds.CartelOfEchoes));
            Assert.AreEqual(0, FactionPassives.MercenarySurchargeFor(FactionIds.IronmarchUnion));
        }

        [Test]
        public void SalvagePityDryBatchThreshold_DustScourgeTightens_OthersDefault()
        {
            Assert.AreEqual(FactionPassives.SalvagePityDryBatchThresholdDustScourge,
                FactionPassives.SalvagePityDryBatchThreshold(FactionIds.DustScourge));
            Assert.AreEqual(FactionPassives.SalvagePityDryBatchThresholdDefault,
                FactionPassives.SalvagePityDryBatchThreshold(FactionIds.IronmarchUnion));
            Assert.AreEqual(FactionPassives.SalvagePityDryBatchThresholdDefault,
                FactionPassives.SalvagePityDryBatchThreshold(FactionIds.CartelOfEchoes));
        }

        [Test]
        public void RarityOddsFightEquivalent_CrimsonAddsOne_OthersUnchanged()
        {
            Assert.AreEqual(6, FactionPassives.RarityOddsFightEquivalent(FactionIds.CrimsonAssembly, 5));
            Assert.AreEqual(5, FactionPassives.RarityOddsFightEquivalent(FactionIds.IronmarchUnion, 5));
            Assert.AreEqual(5, FactionPassives.RarityOddsFightEquivalent(FactionIds.DustScourge, 5));
        }

        [Test]
        public void DespairDividendSupplies_BlightbornOnly_ScalesWithRoutCount()
        {
            Assert.AreEqual(3, FactionPassives.DespairDividendSupplies(FactionIds.BlightbornPact, 3));
            Assert.AreEqual(0, FactionPassives.DespairDividendSupplies(FactionIds.BlightbornPact, 0));
            Assert.AreEqual(0, FactionPassives.DespairDividendSupplies(FactionIds.IronmarchUnion, 3));
        }

        [Test]
        public void HasFreeFirstReroll_OnlyParadox()
        {
            Assert.IsTrue(FactionPassives.HasFreeFirstReroll(FactionIds.ParadoxEngine));
            Assert.IsFalse(FactionPassives.HasFreeFirstReroll(FactionIds.IronmarchUnion));
            Assert.IsFalse(FactionPassives.HasFreeFirstReroll(null));
        }
    }
}
