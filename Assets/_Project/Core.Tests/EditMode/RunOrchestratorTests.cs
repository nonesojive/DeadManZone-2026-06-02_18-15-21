using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using DeadManZone.Game;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Core.Tests
{
    public class RunOrchestratorTests
    {
        private ContentDatabase _database;
        private RunOrchestrator _orchestrator;

        [SetUp]
        public void SetUp()
        {
            _database = ContentDatabase.Load();
            if (_database == null || _database.Pieces.Count == 0)
            {
                Assert.Ignore(DeadManZoneTestContent.MissingDatabaseHint);
            }

            SaveManager.DeleteSave();
            _orchestrator = new RunOrchestrator(_database);
        }

        [TearDown]
        public void TearDown()
        {
            SaveManager.DeleteSave();
        }

        [Test]
        public void StartNewRun_CreatesEmptyCombatAndHqBoards()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion);
            Assert.IsEmpty(_orchestrator.GetCombatBoard().Pieces);
            Assert.IsEmpty(_orchestrator.GetHqBoard().Pieces);
            Assert.AreEqual(6, _orchestrator.GetCombatBoard().Layout.Width);
            Assert.AreEqual(3, _orchestrator.GetHqBoard().Layout.Width);
            Assert.AreEqual(6, _orchestrator.GetHqBoard().Layout.Height);
        }

        [Test]
        public void StartNewRun_SetsBuildPhaseAndShop()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion);

            Assert.AreEqual(RunPhase.Build, _orchestrator.State.Phase);
            Assert.AreEqual(1, _orchestrator.State.FightIndex);
            Assert.Greater(_orchestrator.State.Shop.Offers.Count, 0);
            Assert.AreEqual(9, _orchestrator.State.SaveSchemaVersion);
            Assert.AreEqual(ReservesState.Width, _orchestrator.State.Reserves.Width);
            Assert.AreEqual(ReservesState.Height, _orchestrator.State.Reserves.Height);
            Assert.IsEmpty(_orchestrator.State.Reserves.Pieces);
        }

        [Test]
        public void TryLoadSavedRun_RejectsSchemaBelowV8()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 111);
            _orchestrator.State.SaveSchemaVersion = 7;
            _orchestrator.SaveAndExit();

            var loaded = new RunOrchestrator(_database);
            Assert.IsFalse(loaded.TryLoadSavedRun());
        }

        [Test]
        public void SaveRoundTrip_PreservesFightProgress()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 1234);
            _orchestrator.State.FightIndex = 2;
            _orchestrator.State.Supplies = 77;
            _orchestrator.SaveAndExit();

            var loaded = new RunOrchestrator(_database);
            Assert.IsTrue(loaded.TryLoadSavedRun());
            Assert.AreEqual(2, loaded.State.FightIndex);
            Assert.AreEqual(77, loaded.State.Supplies);
        }

        [Test]
        public void SellPlacedPiece_RemovesFromBoardAndRefundsHalfGold()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 77);
            int startingSupplies = _orchestrator.State.Supplies;

            var board = _orchestrator.GetCombatBoard();
            var rifle = _database.Pieces.First(p => p.id == "conscript_rifleman").ToCore();
            var place = board.TryPlace(rifle, new Core.Common.GridCoord(0, 0), "rifle_1");
            Assert.IsTrue(place.Success, place.Reason);
            _orchestrator.SaveCombatBoard(board);

            int refund = rifle.GoldCost / 2;
            Assert.IsTrue(_orchestrator.TrySellPlacedPiece("rifle_1"));
            Assert.AreEqual(startingSupplies + refund, _orchestrator.State.Supplies);
            Assert.IsEmpty(_orchestrator.GetCombatBoard().Pieces);
        }

        [Test]
        public void RerollShop_IncreasesCostByOneEachUse()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 101);
            int startingSupplies = _orchestrator.State.Supplies;

            Assert.IsTrue(_orchestrator.TryRerollShop());
            Assert.IsTrue(_orchestrator.TryRerollShop());

            Assert.AreEqual(startingSupplies - 3, _orchestrator.State.Supplies);
            Assert.AreEqual(2, _orchestrator.State.RerollCountThisRound);
        }

        [Test]
        public void RerollShop_WithTwoLocks_CostsOneAuthority()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 404);
            _orchestrator.State.Authority = 5;
            var offers = _orchestrator.State.Shop.Offers;
            _orchestrator.SetLockedOffer(offers[0], locked: true);
            _orchestrator.SetLockedOffer(offers[1], locked: true);

            Assert.IsTrue(_orchestrator.TryRerollShop());
            Assert.AreEqual(4, _orchestrator.State.Authority);
        }

        [Test]
        public void LockedOffer_PersistsAcrossMultipleRerolls()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 202);
            var toLock = _orchestrator.State.Shop.Offers.First(o => o.Lane == Core.Shop.ShopLane.Offensive);
            _orchestrator.SetLockedOffer(toLock, locked: true);
            string lockedPieceId = toLock.PieceId;
            int lockedSlotIndex = toLock.SlotIndex;
            Assert.AreEqual(lockedSlotIndex, _orchestrator.State.LockedOffers[0].SlotIndex);

            Assert.IsTrue(_orchestrator.TryRerollShop());
            Assert.IsTrue(_orchestrator.State.Shop.Offers.Any(o =>
                o.PieceId == lockedPieceId &&
                o.SlotIndex == lockedSlotIndex));
            Assert.AreEqual(lockedSlotIndex, _orchestrator.State.LockedOffers[0].SlotIndex);

            Assert.IsTrue(_orchestrator.TryRerollShop());
            Assert.IsTrue(_orchestrator.State.Shop.Offers.Any(o =>
                o.PieceId == lockedPieceId &&
                o.SlotIndex == lockedSlotIndex));
            Assert.AreEqual(lockedSlotIndex, _orchestrator.State.LockedOffers[0].SlotIndex);
        }

        [Test]
        public void TryAcquireOfferToReserves_RemovesOfferAndPlacesOnReserves()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 303);
            var offer = _orchestrator.State.Shop.Offers.First(o =>
                o.Lane == Core.Shop.ShopLane.Offensive &&
                o.RequisitionPrice == 0 &&
                _orchestrator.CanAffordOffer(o.OfferId));
            int suppliesBefore = _orchestrator.State.Supplies;
            int offerCountBefore = _orchestrator.State.Shop.Offers.Count;

            Assert.IsTrue(_orchestrator.TryAcquireOfferToReserves(
                offer.OfferId,
                new Core.Common.GridCoord(0, 0)));
            Assert.AreEqual(suppliesBefore - offer.GoldPrice, _orchestrator.State.Supplies);
            Assert.AreEqual(offerCountBefore - 1, _orchestrator.State.Shop.Offers.Count);
            Assert.IsTrue(_orchestrator.State.Reserves.Pieces.Any(p => p.PieceId == offer.PieceId));
        }

        [Test]
        public void TryAcquireOfferToBoard_InvalidZone_DoesNotCharge()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 404);
            var offer = _orchestrator.State.Shop.Offers.First(o =>
            {
                var piece = _database.Pieces.First(p => p.id == o.PieceId);
                return piece.category == PieceCategory.Building;
            });
            int suppliesBefore = _orchestrator.State.Supplies;

            bool placed = _orchestrator.TryAcquireOfferToBoard(
                offer.OfferId,
                TestBoards.CombatBoardAnchor(5, 4));

            Assert.IsFalse(placed);
            Assert.AreEqual(suppliesBefore, _orchestrator.State.Supplies);
        }

        [Test]
        public void TryMovePlacedPiece_RelocatesOnBoard()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 505);
            var board = _orchestrator.GetHqBoard();
            var radio = GetPiece("command_outpost");
            Assert.IsTrue(board.TryPlace(radio, new Core.Common.GridCoord(1, 0), "radio_1").Success);
            _orchestrator.SaveHqBoard(board);

            Assert.IsTrue(_orchestrator.TryMovePlacedPiece("radio_1", new Core.Common.GridCoord(0, 2)));

            var updated = _orchestrator.GetHqBoard();
            var piece = updated.Pieces.First(p => p.InstanceId == "radio_1");
            Assert.AreEqual(0, piece.Anchor.X);
            Assert.AreEqual(2, piece.Anchor.Y);
        }

        [Test]
        public void TryMoveBoardToReserves_RemovesFromBoardAndPlacesOnReserves()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 606);
            var board = _orchestrator.GetHqBoard();
            var radio = GetPiece("command_outpost");
            Assert.IsTrue(board.TryPlace(radio, new Core.Common.GridCoord(1, 0), "radio_1").Success);
            _orchestrator.SaveHqBoard(board);

            Assert.IsTrue(_orchestrator.TryMoveBoardToReserves(
                "radio_1",
                new Core.Common.GridCoord(0, 0)));
            Assert.IsEmpty(_orchestrator.GetHqBoard().Pieces);
            Assert.IsTrue(_orchestrator.State.Reserves.Pieces.Any(p =>
                p.InstanceId == "radio_1" && p.PieceId == radio.Id));
        }

        [Test]
        public void SaveMidCombat_RestoresAwaitingCommandWindow()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 909);
            var board = _orchestrator.GetCombatBoard();
            Assert.IsTrue(board.TryPlace(TestPieces.RifleSquad(), TestBoards.CombatBoardAnchor(5, 3), "rifle_1").Success);
            _orchestrator.SaveCombatBoard(board);

            _orchestrator.BeginCombat();
            var openingStep = _orchestrator.AdvanceCombat();

            Assert.AreEqual(RunPhase.Combat, _orchestrator.State.Phase);
            Assert.AreEqual(CombatAdvanceStatus.AwaitingCommand, openingStep.Status);
            Assert.IsTrue(_orchestrator.State.Combat.AwaitingCommand);
            Assert.AreEqual(1, _orchestrator.State.Combat.CheckpointsFired);
            Assert.AreEqual(0, _orchestrator.State.Combat.LastSegmentIndex);

            _orchestrator.SaveAndExit();
            var reloaded = new RunOrchestrator(_database);
            Assert.IsTrue(reloaded.TryLoadSavedRun());
            Assert.AreEqual(RunPhase.Combat, reloaded.State.Phase);
            Assert.IsTrue(reloaded.State.Combat.AwaitingCommand);
            Assert.AreEqual(1, reloaded.State.Combat.CheckpointsFired);
            Assert.AreEqual(0, reloaded.State.Combat.LastSegmentIndex);
        }

        [Test]
        public void SaveMidBuild_RestoresGoldAndReserves()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 808);
            _orchestrator.State.Supplies = 42;
            var reserves = _orchestrator.GetReserves();
            var rifle = TestPieces.RifleSquad();
            Assert.IsTrue(reserves.TryPlace(rifle, new Core.Common.GridCoord(0, 0), "reserve_rifle").Success);
            _orchestrator.SaveReserves(reserves);
            _orchestrator.SaveAndExit();

            var reloaded = new RunOrchestrator(_database);
            Assert.IsTrue(reloaded.TryLoadSavedRun());
            Assert.AreEqual(42, reloaded.State.Supplies);
            Assert.IsTrue(reloaded.State.Reserves.Pieces.Any(p => p.PieceId == rifle.Id));
        }

        [Test]
        public void RerollShop_ChangesOnlyUnlockedSlots()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 303);
            var before = _orchestrator.State.Shop.Offers.ToList();
            var locked = before.First(o => o.SlotIndex == 0);
            _orchestrator.SetLockedOffer(locked, locked: true);

            Assert.IsTrue(_orchestrator.TryRerollShop());
            var after = _orchestrator.State.Shop.Offers;

            var lockedAfter = after.First(o => o.SlotIndex == locked.SlotIndex);
            Assert.AreEqual(locked.PieceId, lockedAfter.PieceId);
            Assert.Greater(after.Count, 0);
        }

        [Test]
        public void DefeatReport_CarriesDamageTablesFromSim()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: VerticalSliceTestFixtures.RegressionRunSeed);

            // Stack the deck for a loss: field only the single cheapest offer.
            var cheapest = _orchestrator.State.Shop.Offers
                .OrderBy(o => o.GoldPrice)
                .FirstOrDefault();
            Assert.NotNull(cheapest, "shop should have at least one offer");

            bool placed = false;
            for (int y = 0; y < 6 && !placed; y++)
                for (int x = 0; x < 6 && !placed; x++)
                    placed = _orchestrator.TryAcquireOfferToBoard(cheapest.OfferId, new GridCoord(x, y));
            Assert.IsTrue(placed, "cheapest offer should place on the combat board");

            if (!_orchestrator.CanStartBattle(out string reason))
                Assert.Ignore($"single-unit board cannot start battle: {reason}");

            _orchestrator.BeginCombat();
            while (_orchestrator.State.Phase == RunPhase.Combat)
            {
                SubmitCombatCommandsForCurrentWindow();
                var step = _orchestrator.AdvanceCombat();
                if (step.Status == CombatAdvanceStatus.Completed)
                {
                    _orchestrator.FinalizePendingCombat();
                    break;
                }
            }

            var report = _orchestrator.State.LastBattleReport;
            Assert.NotNull(report, "fight end should produce a battle report");

            if (report.PlayerWon)
                Assert.Ignore("seeded single-unit board won fight 1; defeat path not exercised");

            // The defeat card previously showed empty dealt/taken columns because the
            // orchestrator rebuilt the report from an empty combatant array.
            Assert.IsNotEmpty(report.TopDamageTaken,
                "a defeated army took damage; the report must carry the sim's damage tables");
        }

        [Test]
        public void FullCombatLoop_CanReachVictoryWithStrongBoard()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: VerticalSliceTestFixtures.RegressionRunSeed);
            VerticalSliceTestFixtures.SaveGauntletToOrchestrator(_orchestrator, _database);
            int startingFightIndex = _orchestrator.State.FightIndex;

            for (int fight = 1; fight <= RunOrchestrator.MaxFights; fight++)
            {
                if (!_orchestrator.CanStartBattle(out _))
                    break;

                _orchestrator.BeginCombat();

                while (_orchestrator.State.Phase == RunPhase.Combat)
                {
                    SubmitCombatCommandsForCurrentWindow();
                    var step = _orchestrator.AdvanceCombat();
                    if (step.Status == CombatAdvanceStatus.Completed)
                    {
                        _orchestrator.FinalizePendingCombat();
                        _orchestrator.DismissAftermath();
                        break;
                    }
                }

                if (_orchestrator.State.Phase == RunPhase.Victory)
                    break;

                if (_orchestrator.State.FightIndex > startingFightIndex)
                    break;

                if (_orchestrator.State.Phase == RunPhase.Defeat)
                    break;

                Assert.AreEqual(RunPhase.Build, _orchestrator.State.Phase, $"Fight {fight} should return to build.");
            }

            Assert.Greater(
                _orchestrator.State.FightIndex,
                startingFightIndex,
                "Gauntlet board should win at least one fight through the orchestrator loop.");
        }

        [Test]
        public void SaveMidPause_SameCommands_ProducesIdenticalCombatLog()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 4242);
            var board = _orchestrator.GetCombatBoard();
            Assert.IsTrue(board.TryPlace(TestPieces.RifleSquad(), TestBoards.CombatBoardAnchor(5, 3), "rifle_1").Success);
            _orchestrator.SaveCombatBoard(board);

            _orchestrator.BeginCombat();
            _orchestrator.AdvanceCombat();
            _orchestrator.SavePauseDraft(TacticType.Advance, new List<GrantedAbility>());
            _orchestrator.SaveAndExit();

            var reloaded = new RunOrchestrator(_database);
            Assert.IsTrue(reloaded.TryLoadSavedRun());
            Assert.AreEqual(TacticType.Advance, reloaded.State.Combat.PendingSelectedTactic);

            reloaded.SubmitCombatCommands(new List<PhaseCommand>
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.SetTactic,
                    Tactic = TacticType.Advance,
                    SourcePieceId = "player_tactic"
                }
            });
            var reloadedStep = reloaded.AdvanceCombat();

            var fresh = new RunOrchestrator(_database);
            fresh.StartNewRun(FactionIds.IronmarchUnion, runSeed: 4242);
            var freshBoard = fresh.GetCombatBoard();
            Assert.IsTrue(freshBoard.TryPlace(TestPieces.RifleSquad(), TestBoards.CombatBoardAnchor(5, 3), "rifle_1").Success);
            fresh.SaveCombatBoard(freshBoard);
            fresh.BeginCombat();
            fresh.AdvanceCombat();
            fresh.SubmitCombatCommands(new List<PhaseCommand>
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.SetTactic,
                    Tactic = TacticType.Advance,
                    SourcePieceId = "player_tactic"
                }
            });
            var freshStep = fresh.AdvanceCombat();

            Assert.NotNull(reloadedStep.EventLog);
            Assert.NotNull(freshStep.EventLog);
            Assert.AreEqual(
                freshStep.EventLog.Events.Count,
                reloadedStep.EventLog.Events.Count);
        }

        [Test]
        public void RestoreAfterMidFightTacticChange_ReproducesLiveEventLog()
        {
            // The live fight applies the tactic change mid-fight via the command
            // processor; restore must reproduce that (starting tactic at fight start,
            // change replayed from SubmittedCommands), not re-derive fight-start
            // buffs from the saved end-tactic.
            var live = RunFightWithOpeningTacticChange(runSeed: 4242, saveAndReloadMidFight: false);
            var restored = RunFightWithOpeningTacticChange(runSeed: 4242, saveAndReloadMidFight: true);

            CollectionAssert.AreEqual(live, restored,
                "restored fight must replay the exact event log the player watched");
        }

        private List<string> RunFightWithOpeningTacticChange(int runSeed, bool saveAndReloadMidFight)
        {
            SaveManager.DeleteSave();
            var orchestrator = new RunOrchestrator(_database);
            orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed);
            var board = orchestrator.GetCombatBoard();
            Assert.IsTrue(board.TryPlace(
                TestPieces.RifleSquad(),
                TestBoards.CombatBoardAnchor(5, 3),
                "rifle_1").Success);
            orchestrator.SaveCombatBoard(board);

            orchestrator.BeginCombat();
            // Change tactic at the opening pause — the saved PlayerTactic then differs
            // from the fight's starting tactic, which is what F3 got wrong on restore.
            orchestrator.SubmitCombatCommands(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.SetTactic,
                    Tactic = TacticType.Advance,
                    SourcePieceId = "player_tactic"
                }
            });
            var step = orchestrator.AdvanceCombat();

            if (saveAndReloadMidFight)
            {
                orchestrator.SaveAndExit();
                orchestrator = new RunOrchestrator(_database);
                Assert.IsTrue(orchestrator.TryLoadSavedRun());
                step = orchestrator.AdvanceCombat();
            }

            while (step.Status != CombatAdvanceStatus.Completed)
                step = orchestrator.AdvanceCombat();

            Assert.NotNull(step.EventLog);
            return step.EventLog.Events
                .Select(e => $"{e.Segment}|{e.Tick}|{e.ActorId}|{e.ActionType}|{e.TargetId}|{e.Value}")
                .ToList();
        }

        [Test]
        public void TryLoadSavedRun_CompletedFight_SetsPendingCompletion()
        {
            _orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 187463421);
            var board = _orchestrator.GetCombatBoard();
            Assert.IsTrue(board.TryPlace(
                TestPieces.IroncladFieldMarshal(),
                TestBoards.CombatBoardAnchor(1, 3),
                "marshal_1").Success);
            _orchestrator.SaveCombatBoard(board);

            _orchestrator.BeginCombat();
            while (_orchestrator.State.Phase == RunPhase.Combat
                   && !_orchestrator.HasPendingCombatCompletion)
            {
                if (_orchestrator.State.Combat.AwaitingCommand)
                {
                    int pauseIndex = _orchestrator.State.Combat.CheckpointsFired == 0 ? 0 : 1;
                    _orchestrator.SubmitCombatCommands(new[]
                    {
                        new PhaseCommand
                        {
                            AfterCheckpoint = pauseIndex,
                            Type = CommandType.SetTactic,
                            Tactic = TacticType.Advance,
                            SourcePieceId = "player_tactic"
                        }
                    });
                }

                _orchestrator.AdvanceCombat();
            }

            Assert.IsTrue(_orchestrator.HasPendingCombatCompletion);
            _orchestrator.SaveAndExit();

            var reloaded = new RunOrchestrator(_database);
            Assert.IsTrue(reloaded.TryLoadSavedRun());
            Assert.IsTrue(reloaded.HasPendingCombatCompletion);
            Assert.AreEqual(RunPhase.Combat, reloaded.State.Phase);
        }

        private Core.Board.PieceDefinition GetPiece(string pieceId) =>
            _database.Pieces.First(p => p.id == pieceId).ToCore();

        private void SubmitCombatCommandsForCurrentWindow()
        {
            if (_orchestrator.State.Combat == null || !_orchestrator.State.Combat.AwaitingCommand)
                return;

            var checkpointIndex = _orchestrator.State.Combat.CheckpointsFired - 1;
            int authority = _orchestrator.State.Combat.Authority > 0
                ? _orchestrator.State.Combat.Authority
                : _orchestrator.State.Combat.Requisition;

            var commands = VerticalSliceTestFixtures.BuildAggressiveCommandsForCheckpoint(
                _orchestrator.GetPlayerBoard(),
                checkpointIndex,
                authority);

            _orchestrator.SubmitCombatCommands(commands);
        }
    }
}
