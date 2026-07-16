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
            var mortarPiece = TestPieces.With(
                TestPieces.RifleSquad(),
                grantedAbility: GrantedAbility.MortarShot);
            board.TryPlace(mortarPiece, TestBoards.FrontLineAnchor());

            var processor = new CommandProcessor();
            var available = processor.GetAvailableCommands(board, requisition: 2, checkpointIndex: 0);

            Assert.That(available.Any(c => c.Type == CommandType.UseAbility && c.Ability == GrantedAbility.MortarShot), Is.True);
        }

        // 2026-07-15 faction-roster-v1 §4 (🟡 ledger, now wired): HQ-board buildings
        // (Artillery Park) can grant pause-window abilities too.
        [Test]
        public void GrantedAbility_OnHqBoard_AppearsAlongsideCombatBoard()
        {
            var combatBoard = new BoardState(TestBoards.Layout);
            var hqBoard = new BoardState(TestBoards.IronMarchHqLayout);
            var artilleryPark = TestPieces.With(TestPieces.CommandOutpost(), grantedAbility: GrantedAbility.MortarShot);
            hqBoard.TryPlace(artilleryPark, new GridCoord(0, 0), "artillery_park_1");

            var processor = new CommandProcessor();
            var available = processor.GetAvailableCommands(combatBoard, requisition: 2, checkpointIndex: 0, hqBoard);

            Assert.That(available.Any(c => c.Type == CommandType.UseAbility
                && c.Ability == GrantedAbility.MortarShot
                && c.SourcePieceId == "artillery_park_1"), Is.True);
        }

        [Test]
        public void GrantedAbility_NoHqBoardProvided_StillWorksForCombatBoard()
        {
            var board = new BoardState(TestBoards.Layout);
            var mortarPiece = TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.MortarShot);
            board.TryPlace(mortarPiece, TestBoards.FrontLineAnchor());

            var processor = new CommandProcessor();
            var available = processor.GetAvailableCommands(board, requisition: 2, checkpointIndex: 0);

            Assert.That(available.Any(c => c.Type == CommandType.UseAbility && c.Ability == GrantedAbility.MortarShot), Is.True);
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

            var result = processor.TryApplyBatch(
                new[] { command },
                board,
                ref requisition,
                tactics,
                playerCombatants: new System.Collections.Generic.List<CombatantState>(),
                enemyCombatants: new System.Collections.Generic.List<CombatantState>(),
                log: new CombatEventLog(),
                checkpointIndex: 0,
                logSegment: 0,
                globalTick: 0);

            Assert.IsFalse(result.Success);
            Assert.That(result.Reason, Does.Contain("requisition").IgnoreCase);
        }
    }
}
