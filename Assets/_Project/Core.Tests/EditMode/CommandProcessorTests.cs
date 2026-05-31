using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public class CommandProcessorTests
    {
        [Test]
        public void ChangeStance_RequiresCommandBuilding()
        {
            var board = TestBoards.WithCommandBunker();
            var processor = new CommandProcessor();
            var available = processor.GetAvailableCommands(board, requisition: 2, CombatPhase.Deployment);

            Assert.That(available.Any(c => c.Type == CommandType.ChangeStance), Is.True);
        }

        [Test]
        public void SpendRequisitionBuff_FailsWithoutRequisition()
        {
            var board = new BoardState(TestBoards.Layout);
            board.TryPlace(TestPieces.SupplyDepot(), new GridCoord(0, 0));

            var processor = new CommandProcessor();
            var stances = new StanceState();
            int requisition = 0;
            var command = new PhaseCommand
            {
                AfterPhase = CombatPhase.Deployment,
                Type = CommandType.SpendRequisitionBuff,
                Cost = 1,
                SourcePieceId = board.Pieces.First().InstanceId
            };

            var result = processor.TryApply(
                command,
                board,
                ref requisition,
                stances,
                playerCombatants: new System.Collections.Generic.List<CombatantState>(),
                enemyCombatants: new System.Collections.Generic.List<CombatantState>(),
                log: new CombatEventLog(),
                completedPhase: CombatPhase.Deployment);

            Assert.IsFalse(result.Success);
            Assert.That(result.Reason, Does.Contain("requisition").IgnoreCase);
        }

        [Test]
        public void CommandOnSpecialTile_GrantsBonusActionSlot()
        {
            var board = new BoardState(TestBoards.Layout);
            board.TryPlace(TestPieces.CommandBunker(), new GridCoord(1, 2));

            var processor = new CommandProcessor();
            Assert.That(processor.GetBonusActionSlots(board), Is.EqualTo(1));
        }
    }
}
