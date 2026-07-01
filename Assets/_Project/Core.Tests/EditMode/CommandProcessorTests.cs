using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public class CommandProcessorTests
    {
        [Test]
        public void GrantedAbility_AppearsWhenPieceHasAbility()
        {
            var board = new BoardState(TestBoards.Layout);
            var grenadeThrower = TestPieces.With(
                TestPieces.RifleSquad(),
                grantedAbility: GrantedAbility.GrenadeLob);
            board.TryPlace(grenadeThrower, TestBoards.FrontLineAnchor());

            var processor = new CommandProcessor();
            var available = processor.GetAvailableCommands(board, requisition: 2, checkpointIndex: 0);

            Assert.That(available.Any(c => c.Type == CommandType.UseAbility && c.Ability == GrantedAbility.GrenadeLob), Is.True);
        }

        [Test]
        public void SpendRequisitionBuff_FailsWithoutRequisition()
        {
            var board = TestBoards.WithSupplyDepot();

            var processor = new CommandProcessor();
            var tactics = new TacticState();
            int requisition = 0;
            var command = new PhaseCommand
            {
                AfterCheckpoint = 0,
                Type = CommandType.SpendRequisitionBuff,
                Cost = 1,
                SourcePieceId = board.Pieces.First().InstanceId
            };

            var result = processor.TryApply(
                command,
                board,
                ref requisition,
                tactics,
                playerCombatants: new System.Collections.Generic.List<CombatantState>(),
                enemyCombatants: new System.Collections.Generic.List<CombatantState>(),
                log: new CombatEventLog(),
                checkpointIndex: 0,
                globalTick: 0);

            Assert.IsFalse(result.Success);
            Assert.That(result.Reason, Does.Contain("requisition").IgnoreCase);
        }

        [Test]
        public void BonusActionSlots_AreDisabledInDemo()
        {
            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            hq.TryPlace(TestPieces.CommandBunker(), new GridCoord(0, 0));
            var board = new BuildBoardSet
            {
                Combat = new BoardState(TestBoards.Layout),
                Hq = hq
            }.ToAggregateBoard();

            var processor = new CommandProcessor();
            Assert.That(processor.GetBonusActionSlots(board), Is.EqualTo(0));
        }
    }
}
