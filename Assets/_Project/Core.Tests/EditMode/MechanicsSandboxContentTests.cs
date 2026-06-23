using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class MechanicsSandboxContentTests
    {
        [Test]
        public void ContentDatabase_HasSandboxNeutralAndIronMarchRosters()
        {
            var database = ContentDatabase.Load();
            Assert.NotNull(database, "ContentDatabase asset missing from Resources.");

            var neutral = database.Pieces.Where(p => p != null && p.factionId == "neutral").ToList();
            var ironMarch = database.Pieces.Where(p => p != null && p.factionId == FactionIds.IronVanguard).ToList();

            Assert.GreaterOrEqual(neutral.Count, 10, "Expected at least 10 neutral sandbox pieces.");
            Assert.GreaterOrEqual(ironMarch.Count, 15, "Expected at least 15 IronMarch Union pieces.");
        }

        [Test]
        public void NeutralSandboxPieces_CoverMechanicFootprintsAndRoles()
        {
            var database = ContentDatabase.Load();
            Assert.NotNull(database);

            AssertPieceShape(database, "conscript_rifleman", 1);
            AssertPieceShape(database, "grenade_thrower", 2);
            AssertPieceShape(database, "armored_transport", 3);
            AssertPieceShape(database, "mobile_cannon", 6);
            AssertPieceRole(database, "shock_trooper", GameTagIds.Assault);
            AssertPieceRole(database, "neutral_mortar_team", GameTagIds.Artillery);
            AssertPieceRole(database, "marksman_squad", GameTagIds.Sniper);
            AssertPieceHasSynergyTag(database, "field_medic", GameTagIds.Medic);
        }

        [Test]
        public void NeutralSupplyDepot_HasSalvageBoost()
        {
            var database = ContentDatabase.Load();
            Assert.NotNull(database);

            var depot = database.Pieces.FirstOrDefault(p => p != null && p.id == "neutral_supply_depot");
            Assert.NotNull(depot, "neutral_supply_depot piece is required for salvage boost testing.");
            Assert.GreaterOrEqual(depot.salvageChanceBonus, 5);
        }

        [Test]
        public void IronMarchSandboxPieces_IncludeNewRoleVariants()
        {
            var database = ContentDatabase.Load();
            Assert.NotNull(database);

            Assert.NotNull(FindPiece(database, "ironmarch_heavy_tank"));
            Assert.NotNull(FindPiece(database, "ironmarch_mortar"));
            Assert.NotNull(FindPiece(database, "ironmarch_engineer"));
            Assert.NotNull(FindPiece(database, "ironmarch_breacher"));
            Assert.NotNull(FindPiece(database, "ironmarch_sniper"));
            Assert.NotNull(FindPiece(database, "ironmarch_defender"));
        }

        private static PieceDefinitionSO FindPiece(ContentDatabase database, string id) =>
            database.Pieces.FirstOrDefault(p => p != null && p.id == id);

        private static void AssertPieceShape(ContentDatabase database, string id, int cellCount)
        {
            var piece = FindPiece(database, id);
            Assert.NotNull(piece, $"Missing piece '{id}'.");
            Assert.AreEqual(cellCount, piece.shapeCells?.Length ?? 0, $"Unexpected footprint for '{id}'.");
        }

        private static void AssertPieceRole(ContentDatabase database, string id, string combatRole)
        {
            var piece = FindPiece(database, id);
            Assert.NotNull(piece, $"Missing piece '{id}'.");
            Assert.AreEqual(combatRole, piece.combatRole);
        }

        private static void AssertPieceHasSynergyTag(ContentDatabase database, string id, string synergyTag)
        {
            var piece = FindPiece(database, id);
            Assert.NotNull(piece, $"Missing piece '{id}'.");
            Assert.IsTrue(piece.synergyTags != null && piece.synergyTags.Contains(synergyTag));
        }
    }
}
