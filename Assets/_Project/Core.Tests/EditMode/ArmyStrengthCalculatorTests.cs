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

        /// <summary>
        /// Five infantry trips the `infantry` critical-mass rule (5 -> +10 Max HP each). ARMY
        /// STRENGTH must SEE that. It previously did not: the rating only read synergy's damage and
        /// armor, so every HP threshold and every HP aura in the game was invisible to the preview —
        /// it understated exactly the compositions the game wants the player to build.
        /// </summary>
        [Test]
        public void CriticalMass_RaisesEffectiveTotal_AboveBase()
        {
            var below = ArmyStrengthCalculator.Evaluate(BoardWithInfantry(4));
            var above = ArmyStrengthCalculator.Evaluate(BoardWithInfantry(5));

            // Below the threshold nothing fires: effective == base.
            Assert.AreEqual(below.BaseTotal, below.EffectiveTotal,
                "no rule active at 4 infantry, so effective must equal base");

            // At the threshold every infantry gains HP, so effective must exceed base.
            Assert.Greater(above.EffectiveTotal, above.BaseTotal,
                "the infantry critical-mass rule (5 -> +10 HP) must be visible to ARMY STRENGTH");
        }

        /// <summary>
        /// The Easy front fields a GREEN enemy: TickCombatRun suppresses that side's fight-start
        /// engines. The preview has to model the same suppression or it over-states what the player
        /// will actually face — and the three fronts stop being measured on the same basis.
        /// </summary>
        [Test]
        public void SuppressedFightStartEngines_RateAsRawStatLine()
        {
            var board = BoardWithInfantry(5);

            var buffed = ArmyStrengthCalculator.Evaluate(board);
            var green = ArmyStrengthCalculator.Evaluate(
                board, buildBoards: null, includeFightStartEngines: false);

            Assert.AreEqual(green.BaseTotal, green.EffectiveTotal,
                "engines off: the army rates at its raw stat line");
            Assert.Less(green.EffectiveTotal, buffed.EffectiveTotal,
                "a green force must preview weaker than the same board fighting with its engines on");
        }

        /// <summary>The real combat board: unzoned 6x6, same as BoardLayout.CreateCombatBoard().</summary>
        private static BoardState BoardWithInfantry(int count)
        {
            var board = new BoardState(TestBoards.CombatLayout);
            for (int i = 0; i < count; i++)
            {
                var infantry = TestPieces.CreateUnit($"infantry_{i}", primary: GameTagIds.Infantry);
                Assert.IsTrue(
                    board.TryPlace(infantry, new GridCoord(i, 0), $"infantry_{i}").Success,
                    $"failed to place infantry_{i}");
            }

            return board;
        }
    }
}
