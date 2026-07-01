using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class PieceCombatRatingTests
    {
        private static PieceDefinition Conscript() => new()
        {
            Id = "conscript",
            DisplayName = "Conscript",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            Tags = new[] { GameTagIds.Infantry },
            MaxHp = 70,
            BaseDamage = 12,
            CooldownTicks = 4,
            ManpowerCost = 6
        };

        private static PieceDefinition MgTeam() => new()
        {
            Id = "mg_team",
            DisplayName = "MG Team",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            Tags = new[] { GameTagIds.Infantry },
            MaxHp = 120,
            BaseDamage = 24,
            CooldownTicks = 4,
            AttackSpeed = AttackSpeedTier.Fast,
            ArmorType = ArmorType.Medium,
            AttackType = AttackType.Shredding,
            AccuracyOverride = 70,
            ManpowerCost = 12
        };

        [Test]
        public void MgTeam_RatesHigherThanConscript()
        {
            int mg = PieceCombatRating.ComputeBase(MgTeam());
            int conscript = PieceCombatRating.ComputeBase(Conscript());
            Assert.Greater(mg, conscript);
        }

        [Test]
        public void CombatUnit_HasPositiveRating()
        {
            Assert.Greater(PieceCombatRating.ComputeBase(TestPieces.BulwarkSquad()), 0);
        }

        [Test]
        public void NonFieldingBuilding_ReturnsZero()
        {
            Assert.AreEqual(0, PieceCombatRating.ComputeBase(TestPieces.CommandBunker()));
            Assert.AreEqual(0, PieceCombatRating.ComputeBase(TestPieces.SupplyDepot()));
        }

        [Test]
        public void SynergyDamageBonus_IncreasesRating()
        {
            var piece = MgTeam();
            int baseRating = PieceCombatRating.ComputeBase(piece);
            int buffed = PieceCombatRating.Compute(piece, new PieceAbilityEngine.SynergyResult { DamageBonus = 4 });
            Assert.Greater(buffed, baseRating);
        }
    }
}
