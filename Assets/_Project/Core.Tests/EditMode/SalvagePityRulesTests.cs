using System.Collections.Generic;
using DeadManZone.Core;
using DeadManZone.Core.Content;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>2026-07-15 faction-roster-v1 §1.5: salvage pity timer, mirroring
    /// RarityWeights' rare pity architecture (state-derived, appear-reset).</summary>
    public sealed class SalvagePityRulesTests
    {
        [Test]
        public void ForcesSalvage_AtGlobalThreshold_ForIronMarch()
        {
            Assert.IsFalse(SalvagePityRules.ForcesSalvage(FactionIds.IronmarchUnion, 3));
            Assert.IsTrue(SalvagePityRules.ForcesSalvage(FactionIds.IronmarchUnion, 4));
            Assert.IsTrue(SalvagePityRules.ForcesSalvage(FactionIds.IronmarchUnion, 10));
        }

        [Test]
        public void ForcesSalvage_DustScourgeTightensToTwo()
        {
            Assert.IsFalse(SalvagePityRules.ForcesSalvage(FactionIds.DustScourge, 1));
            Assert.IsTrue(SalvagePityRules.ForcesSalvage(FactionIds.DustScourge, 2));
        }

        [Test]
        public void ContainsSalvageOffer_DetectsAnySalvagedOffer()
        {
            var offers = new List<ShopOffer>
            {
                new() { PieceId = "a", IsSalvaged = false },
                new() { PieceId = "b", IsSalvaged = true }
            };

            Assert.IsTrue(SalvagePityRules.ContainsSalvageOffer(offers));
            Assert.IsFalse(SalvagePityRules.ContainsSalvageOffer(new List<ShopOffer>
            {
                new() { PieceId = "a", IsSalvaged = false }
            }));
            Assert.IsFalse(SalvagePityRules.ContainsSalvageOffer(null));
        }

        [Test]
        public void SalvagePoolAvailability_EmptyWhenNoEnemyFactionYet()
        {
            var registry = new ContentRegistry();
            Assert.IsTrue(SalvagePoolAvailability.IsEmpty(registry, null, FactionIds.IronmarchUnion));
            Assert.IsTrue(SalvagePoolAvailability.IsEmpty(registry, "", FactionIds.IronmarchUnion));
        }

        [Test]
        public void SalvagePoolAvailability_EmptyWhenEnemyFactionHasNoPieces()
        {
            var registry = new ContentRegistry();
            registry.Register(TestPieces.RifleSquad(), ShopLane.Offensive); // FactionId = IronmarchUnion

            Assert.IsTrue(SalvagePoolAvailability.IsEmpty(registry, "crimson_legion", FactionIds.IronmarchUnion));
        }

        [Test]
        public void SalvagePoolAvailability_NotEmptyWhenEnemyFactionHasPieces()
        {
            var registry = new ContentRegistry();
            var enemyPiece = TestPieces.RifleSquad(); // FactionId = IronmarchUnion
            registry.Register(enemyPiece, ShopLane.Offensive);

            Assert.IsFalse(SalvagePoolAvailability.IsEmpty(registry, enemyPiece.FactionId, FactionIds.DustScourge));
        }

        [Test]
        public void SalvagePoolAvailability_EmptyWhenEnemyFactionMirrorsThePlayer()
        {
            // Current demo content's placeholder fights pit IronMarch against IronMarch
            // (EnemyTemplateSO.enemyFactionId == ironmarch_union) — ShopOfferPoolBuilder
            // excludes a Salvage candidate that also matches the player's own faction, so
            // this must read as empty too, or the pity counter would climb on content that
            // can never actually show a salvage offer.
            var registry = new ContentRegistry();
            registry.Register(TestPieces.RifleSquad(), ShopLane.Offensive); // FactionId = IronmarchUnion

            Assert.IsTrue(SalvagePoolAvailability.IsEmpty(
                registry, FactionIds.IronmarchUnion, FactionIds.IronmarchUnion));
        }
    }
}
