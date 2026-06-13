using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class SynergyEngineTests
    {
        private static BoardState CreateAdjacentBoard(PieceDefinition source, string sourceId, PieceDefinition neighbor, string neighborId)
        {
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            Assert.IsTrue(board.TryPlace(source, TestBoards.SupportLineAnchor(0), sourceId).Success);
            Assert.IsTrue(board.TryPlace(neighbor, TestBoards.SupportLineAnchor(1), neighborId).Success);
            return board;
        }

        [Test]
        public void PieceWithNoSynergyTags_ProducesZeroBonuses()
        {
            var rifle = TestPieces.CreateUnit(
                "rifle",
                primary: GameTagIds.Infantry,
                systemTag: GameTagIds.Combatant);
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            Assert.IsTrue(board.TryPlace(rifle, TestBoards.SupportLineAnchor(0), "rifle_1").Success);

            var snapshot = SynergyEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("rifle_1", out var result));
            Assert.AreEqual(0, result.DamageBonus);
            Assert.AreEqual(0, result.ArmorBuffSteps);
            Assert.AreEqual(0, result.MoveChargeBonus);
        }

        [Test]
        public void MedicAdjacentInfantry_GrantsArmorBuff()
        {
            var medic = TestPieces.CreateUnit(
                "medic",
                systemTag: GameTagIds.Combatant,
                synergyTags: new[] { GameTagIds.Medic });
            var infantry = TestPieces.CreateUnit(
                "infantry",
                primary: GameTagIds.Infantry,
                systemTag: GameTagIds.Combatant);
            var board = CreateAdjacentBoard(medic, "medic_1", infantry, "infantry_1");

            var snapshot = SynergyEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("infantry_1", out var result));
            Assert.AreEqual(1, result.ArmorBuffSteps);
        }

        [Test]
        public void CommandAdjacentArtillery_GrantsDamageBonus()
        {
            var command = TestPieces.CreateUnit(
                "command",
                systemTag: GameTagIds.Combatant,
                synergyTags: new[] { GameTagIds.Command });
            var artillery = TestPieces.CreateUnit(
                "artillery",
                combatRole: GameTagIds.Artillery,
                systemTag: GameTagIds.Combatant);
            var board = CreateAdjacentBoard(command, "command_1", artillery, "artillery_1");

            var snapshot = SynergyEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("artillery_1", out var result));
            Assert.AreEqual(2, result.DamageBonus);
        }

        [Test]
        public void InspiringAdjacentAny_GrantsMoveCharge()
        {
            var inspiring = TestPieces.CreateUnit(
                "inspiring",
                systemTag: GameTagIds.Combatant,
                synergyTags: new[] { GameTagIds.Inspiring });
            var neighbor = TestPieces.CreateUnit(
                "neighbor",
                primary: GameTagIds.Infantry,
                systemTag: GameTagIds.Combatant);
            var board = CreateAdjacentBoard(inspiring, "inspiring_1", neighbor, "neighbor_1");

            var snapshot = SynergyEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("neighbor_1", out var result));
            Assert.AreEqual(5, result.MoveChargeBonus);
        }

        [Test]
        public void FightStartSnapshot_DoesNotChangeAfterRelocate()
        {
            var rifle = TestPieces.CreateUnit(
                "rifle",
                primary: GameTagIds.Infantry,
                systemTag: GameTagIds.Combatant);
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            Assert.IsTrue(board.TryPlace(rifle, TestBoards.SupportLineAnchor(0), "rifle_1").Success);

            var initialSnapshot = SynergyEngine.EvaluateFightStart(board);
            Assert.IsFalse(initialSnapshot.TryGet("rifle_1", out var initialRifleResult) && initialRifleResult.DamageBonus > 0);

            var moved = board.TryRelocate("rifle_1", TestBoards.FrontLineAnchor(0), PieceRotation.R0);
            Assert.IsTrue(moved.Success, moved.Reason);

            var movedSnapshot = SynergyEngine.EvaluateFightStart(board);
            Assert.IsFalse(movedSnapshot.TryGet("rifle_1", out var movedRifleResult) && movedRifleResult.DamageBonus > 0);
        }
    }
}
