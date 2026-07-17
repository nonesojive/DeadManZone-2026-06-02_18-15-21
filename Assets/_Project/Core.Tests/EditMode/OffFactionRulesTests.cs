using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>2026-07-15 faction-roster-v1 §1.4 off-faction ruleset: `salvage` is derived
    /// (never stored), `mercenary` is acquisition-based and permanent and suppresses
    /// salvage, neutral is neither.</summary>
    public sealed class OffFactionRulesTests
    {
        private static PlacedPiece Piece(string factionId, bool isMercenary = false) =>
            new()
            {
                InstanceId = "probe",
                Definition = new PieceDefinition
                {
                    Id = "probe_def",
                    Category = PieceCategory.Unit,
                    Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                    FactionId = factionId
                },
                Anchor = new GridCoord(0, 0),
                IsMercenary = isMercenary
            };

        [Test]
        public void IsSalvage_OffFactionNonMercenary_ReturnsTrue()
        {
            Assert.IsTrue(OffFactionRules.IsSalvage(
                Piece(FactionIds.DustScourge), FactionIds.IronmarchUnion));
        }

        [Test]
        public void IsSalvage_OwnFaction_ReturnsFalse()
        {
            Assert.IsFalse(OffFactionRules.IsSalvage(
                Piece(FactionIds.IronmarchUnion), FactionIds.IronmarchUnion));
        }

        [Test]
        public void IsSalvage_Neutral_ReturnsFalse()
        {
            Assert.IsFalse(OffFactionRules.IsSalvage(
                Piece(OffFactionRules.NeutralFactionId), FactionIds.IronmarchUnion));
        }

        [Test]
        public void IsSalvage_Mercenary_ReturnsFalse_EvenThoughOffFaction()
        {
            Assert.IsFalse(OffFactionRules.IsSalvage(
                Piece(FactionIds.DustScourge, isMercenary: true), FactionIds.IronmarchUnion));
        }

        [Test]
        public void IsSalvage_NullPieceOrDefinition_ReturnsFalse()
        {
            Assert.IsFalse(OffFactionRules.IsSalvage(null, FactionIds.IronmarchUnion));
            Assert.IsFalse(OffFactionRules.IsSalvage(
                new PlacedPiece { Definition = null }, FactionIds.IronmarchUnion));
        }

        [Test]
        public void IsMercenary_ReflectsThePlacedPieceFlag()
        {
            Assert.IsTrue(OffFactionRules.IsMercenary(Piece(FactionIds.CartelOfEchoes, isMercenary: true)));
            Assert.IsFalse(OffFactionRules.IsMercenary(Piece(FactionIds.CartelOfEchoes)));
            Assert.IsFalse(OffFactionRules.IsMercenary(null));
        }

        [Test]
        public void IsFighter_ExcludesBuildingsAndStructures()
        {
            var building = new PieceDefinition
            {
                Id = "building",
                Category = PieceCategory.Building,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) })
            };
            var structurePrimary = new PieceDefinition
            {
                Id = "mg_nest",
                Category = PieceCategory.Unit,
                Primary = DeadManZone.Core.Tags.GameTagIds.Structure,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) })
            };
            var infantry = new PieceDefinition
            {
                Id = "rifleman",
                Category = PieceCategory.Unit,
                Primary = DeadManZone.Core.Tags.GameTagIds.Infantry,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) })
            };
            var vehicle = new PieceDefinition
            {
                Id = "tank",
                Category = PieceCategory.Unit,
                Primary = DeadManZone.Core.Tags.GameTagIds.Vehicle,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) })
            };

            Assert.IsFalse(OffFactionRules.IsFighter(building));
            Assert.IsFalse(OffFactionRules.IsFighter(structurePrimary),
                "primary=structure excludes even a Category.Unit piece (machine_gun_nest gotcha)");
            Assert.IsTrue(OffFactionRules.IsFighter(infantry));
            Assert.IsTrue(OffFactionRules.IsFighter(vehicle));
            Assert.IsFalse(OffFactionRules.IsFighter(null));
        }
    }
}
