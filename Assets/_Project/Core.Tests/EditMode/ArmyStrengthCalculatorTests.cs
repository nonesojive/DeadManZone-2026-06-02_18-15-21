using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class ArmyStrengthCalculatorTests
    {
        private static PieceAbilityDefinition CreateCommandDamageAbility() => new()
        {
            Id = "command_adjacent_artillery_damage_plus_two",
            Trigger = PieceAbilityTrigger.AdjacentAura,
            NeighborFilter = new NeighborFilter { CombatRoleTagId = GameTagIds.Artillery },
            Stat = SynergyStat.Damage,
            ModType = SynergyModType.Flat,
            Magnitude = 2
        };

        [Test]
        public void EmptyBoard_ReturnsZeroTotals()
        {
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);

            var snapshot = ArmyStrengthCalculator.Evaluate(board);
            Assert.AreEqual(0, snapshot.BaseTotal);
            Assert.AreEqual(0, snapshot.EffectiveTotal);
        }

        [Test]
        public void FieldingPieces_SumRatings()
        {
            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var combat = new BoardState(layout);
            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            var rifle = TestPieces.RifleSquad();
            Assert.IsTrue(combat.TryPlace(rifle, TestBoards.SupportLineAnchor(0), "rifle_1").Success);
            Assert.IsTrue(hq.TryPlace(TestPieces.CommandBunker(), new GridCoord(0, 2), "bunker_1").Success);
            var board = new BuildBoardSet { Combat = combat, Hq = hq }.ToAggregateBoard();

            var snapshot = ArmyStrengthCalculator.Evaluate(board);
            int expected = PieceCombatRating.ComputeBase(rifle)
                + PieceCombatRating.ComputeBase(TestPieces.CommandBunker());
            Assert.AreEqual(expected, snapshot.BaseTotal);
            Assert.AreEqual(expected, snapshot.EffectiveTotal);
        }

        [Test]
        public void AdjacentSynergy_IncreasesEffectiveTotal()
        {
            var command = TestPieces.CreateUnit(
                "command",
                synergyTags: new[] { GameTagIds.Command });
            command = TestPieces.With(command, abilities: new[] { CreateCommandDamageAbility() });
            var artillery = TestPieces.CreateUnit(
                "artillery",
                combatRole: GameTagIds.Artillery);

            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            Assert.IsTrue(board.TryPlace(command, TestBoards.SupportLineAnchor(0), "command_1").Success);
            Assert.IsTrue(board.TryPlace(artillery, TestBoards.SupportLineAnchor(1), "artillery_1").Success);

            var snapshot = ArmyStrengthCalculator.Evaluate(board);
            Assert.Greater(snapshot.EffectiveTotal, snapshot.BaseTotal);
            Assert.Greater(snapshot.SynergyBonus, 0);
        }

        [Test]
        public void BuildingsExcluded_FromTotals()
        {
            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            Assert.IsTrue(hq.TryPlace(TestPieces.CommandBunker(), new GridCoord(0, 0), "bunker_1").Success);
            var board = new BuildBoardSet
            {
                Combat = new BoardState(BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>())),
                Hq = hq
            }.ToAggregateBoard();

            var snapshot = ArmyStrengthCalculator.Evaluate(board);
            Assert.AreEqual(0, snapshot.BaseTotal);
        }
    }
}
