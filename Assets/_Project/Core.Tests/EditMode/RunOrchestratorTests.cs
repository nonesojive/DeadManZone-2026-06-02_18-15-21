using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
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
                Assert.Ignore("Generated ContentDatabase not found. Run DeadManZone/Generate Vertical Slice Content first.");
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
        public void StartNewRun_SetsBuildPhaseAndShop()
        {
            _orchestrator.StartNewRun("iron_vanguard");

            Assert.AreEqual(RunPhase.Build, _orchestrator.State.Phase);
            Assert.AreEqual(1, _orchestrator.State.FightIndex);
            Assert.Greater(_orchestrator.State.Shop.Offers.Count, 0);
            Assert.AreEqual(3, _orchestrator.State.SaveSchemaVersion);
            Assert.AreEqual(ReservesState.Width, _orchestrator.State.Reserves.Width);
            Assert.AreEqual(ReservesState.Height, _orchestrator.State.Reserves.Height);
            Assert.IsEmpty(_orchestrator.State.Reserves.Pieces);
        }

        [Test]
        public void TryLoadSavedRun_RejectsSchemaBelowV3()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 111);
            _orchestrator.State.SaveSchemaVersion = 2;
            _orchestrator.SaveAndExit();

            var loaded = new RunOrchestrator(_database);
            Assert.IsFalse(loaded.TryLoadSavedRun());
        }

        [Test]
        public void SaveRoundTrip_PreservesFightProgress()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 1234);
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
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 77);
            int startingSupplies = _orchestrator.State.Supplies;

            var board = _orchestrator.GetPlayerBoard();
            var bunker = _database.Pieces.First(p => p.id == "command_bunker").ToCore();
            var place = board.TryPlace(bunker, new Core.Common.GridCoord(1, 4), "bunker_1");
            Assert.IsTrue(place.Success, place.Reason);
            _orchestrator.SavePlayerBoard(board);

            int refund = bunker.GoldCost / 2;
            Assert.IsTrue(_orchestrator.TrySellPlacedPiece("bunker_1"));
            Assert.AreEqual(startingSupplies + refund, _orchestrator.State.Supplies);
            Assert.IsEmpty(_orchestrator.GetPlayerBoard().Pieces);
        }

        [Test]
        public void RerollLane_IncreasesCostByOneEachUse()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 101);
            int startingSupplies = _orchestrator.State.Supplies;

            Assert.IsTrue(_orchestrator.TryRerollLane(Core.Shop.ShopLane.Offensive));
            Assert.IsTrue(_orchestrator.TryRerollLane(Core.Shop.ShopLane.Defensive));

            Assert.AreEqual(startingSupplies - 3, _orchestrator.State.Supplies);
            Assert.AreEqual(2, _orchestrator.State.RerollCountThisRound);
        }

        [Test]
        public void LockedOffer_PersistsAcrossMultipleRerolls()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 202);
            var toLock = _orchestrator.State.Shop.Offers.First(o => o.Lane == Core.Shop.ShopLane.Offensive);
            _orchestrator.SetLockedOffer(toLock, locked: true);
            string lockedPieceId = toLock.PieceId;
            int lockedSlotIndex = toLock.SlotIndex;
            Assert.AreEqual(lockedSlotIndex, _orchestrator.State.LockedOffer.SlotIndex);

            Assert.IsTrue(_orchestrator.TryRerollLane(Core.Shop.ShopLane.Offensive));
            Assert.IsTrue(_orchestrator.State.Shop.Offers.Any(o =>
                o.Lane == Core.Shop.ShopLane.Offensive &&
                o.PieceId == lockedPieceId &&
                o.SlotIndex == lockedSlotIndex));
            Assert.AreEqual(lockedSlotIndex, _orchestrator.State.LockedOffer.SlotIndex);

            Assert.IsTrue(_orchestrator.TryRerollLane(Core.Shop.ShopLane.Offensive));
            Assert.IsTrue(_orchestrator.State.Shop.Offers.Any(o =>
                o.Lane == Core.Shop.ShopLane.Offensive &&
                o.PieceId == lockedPieceId &&
                o.SlotIndex == lockedSlotIndex));
            Assert.AreEqual(lockedSlotIndex, _orchestrator.State.LockedOffer.SlotIndex);
        }

        [Test]
        public void TryAcquireOfferToReserves_RemovesOfferAndPlacesOnReserves()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 303);
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
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 404);
            var offer = _orchestrator.State.Shop.Offers.First(o =>
            {
                var piece = _database.Pieces.First(p => p.id == o.PieceId);
                return piece.category == PieceCategory.Building;
            });
            int suppliesBefore = _orchestrator.State.Supplies;

            bool placed = _orchestrator.TryAcquireOfferToBoard(
                offer.OfferId,
                TestBoards.FrontLineAnchor(4));

            Assert.IsFalse(placed);
            Assert.AreEqual(suppliesBefore, _orchestrator.State.Supplies);
        }

        [Test]
        public void TryMovePlacedPiece_RelocatesOnBoard()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 505);
            var board = _orchestrator.GetPlayerBoard();
            var bunker = TestPieces.CommandBunker();
            Assert.IsTrue(board.TryPlace(bunker, new Core.Common.GridCoord(1, 4), "bunker_1").Success);
            _orchestrator.SavePlayerBoard(board);

            Assert.IsTrue(_orchestrator.TryMovePlacedPiece("bunker_1", new Core.Common.GridCoord(0, 2)));

            var updated = _orchestrator.GetPlayerBoard();
            var piece = updated.Pieces.First(p => p.InstanceId == "bunker_1");
            Assert.AreEqual(0, piece.Anchor.X);
            Assert.AreEqual(2, piece.Anchor.Y);
        }

        [Test]
        public void TryMoveBoardToReserves_RemovesFromBoardAndPlacesOnReserves()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 606);
            var board = _orchestrator.GetPlayerBoard();
            var bunker = TestPieces.CommandBunker();
            Assert.IsTrue(board.TryPlace(bunker, new Core.Common.GridCoord(1, 4), "bunker_1").Success);
            _orchestrator.SavePlayerBoard(board);

            Assert.IsTrue(_orchestrator.TryMoveBoardToReserves(
                "bunker_1",
                new Core.Common.GridCoord(0, 0)));
            Assert.AreEqual(0, _orchestrator.GetPlayerBoard().Pieces.Count());
            Assert.IsTrue(_orchestrator.State.Reserves.Pieces.Any(p =>
                p.InstanceId == "bunker_1" && p.PieceId == bunker.Id));
        }

        [Test]
        public void SaveMidCombat_RestoresAwaitingCommandWindow()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 909);
            var board = _orchestrator.GetPlayerBoard();
            Assert.IsTrue(board.TryPlace(TestPieces.CommandBunker(), new Core.Common.GridCoord(1, 4), "bunker_1").Success);
            Assert.IsTrue(board.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(), "rifle_1").Success);
            _orchestrator.SavePlayerBoard(board);

            _orchestrator.BeginCombat();

            Assert.AreEqual(RunPhase.Combat, _orchestrator.State.Phase);
            Assert.IsTrue(_orchestrator.State.Combat.AwaitingCommand);
            Assert.Greater(_orchestrator.GetAvailableCommands().Count, 0);

            _orchestrator.SaveAndExit();
            var reloaded = new RunOrchestrator(_database);
            Assert.IsTrue(reloaded.TryLoadSavedRun());
            Assert.AreEqual(RunPhase.Combat, reloaded.State.Phase);
            Assert.IsTrue(reloaded.State.Combat.AwaitingCommand);
            Assert.Greater(reloaded.GetAvailableCommands().Count, 0);
        }

        [Test]
        public void SaveMidBuild_RestoresGoldAndReserves()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 808);
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
        public void RerollLane_ChangesOnlySelectedLaneOffers()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 303);
            var before = _orchestrator.State.Shop.Offers.ToList();

            Assert.IsTrue(_orchestrator.TryRerollLane(Core.Shop.ShopLane.Offensive));
            var after = _orchestrator.State.Shop.Offers;

            var beforeDefensive = before.Where(o => o.Lane == Core.Shop.ShopLane.Defensive)
                .Select(o => o.OfferId)
                .OrderBy(id => id)
                .ToArray();
            var afterDefensive = after.Where(o => o.Lane == Core.Shop.ShopLane.Defensive)
                .Select(o => o.OfferId)
                .OrderBy(id => id)
                .ToArray();
            CollectionAssert.AreEquivalent(beforeDefensive, afterDefensive);
        }

        [Test]
        public void FullCombatLoop_CanReachVictoryWithStrongBoard()
        {
            var board = VerticalSliceTestFixtures.BuildGauntletBoard(_database);
            int runSeed = FindWinningSeed(board, startSeed: 42);

            _orchestrator.StartNewRun("iron_vanguard", runSeed: runSeed);
            _orchestrator.SavePlayerBoard(board);

            for (int fight = 1; fight <= RunOrchestrator.MaxFights; fight++)
            {
                _orchestrator.BeginCombat();

                while (_orchestrator.State.Phase == RunPhase.Combat)
                {
                    SubmitCombatCommandsForCurrentWindow();

                    var step = _orchestrator.AdvanceCombat();
                    if (step.Status == CombatAdvanceStatus.Completed)
                        break;
                }

                if (_orchestrator.State.Phase == RunPhase.Victory)
                    break;

                Assert.AreEqual(RunPhase.Build, _orchestrator.State.Phase, $"Fight {fight} should return to build.");
            }

            Assert.AreEqual(RunPhase.Victory, _orchestrator.State.Phase);
        }

        private int FindWinningSeed(BoardState board, int startSeed)
        {
            for (int seed = startSeed; seed < startSeed + 1000; seed++)
            {
                if (BoardBeatsGauntlet(board, seed))
                    return seed;
            }

            Assert.Fail("Gauntlet test board did not win all fights on any seed in range.");
            return startSeed;
        }

        private bool BoardBeatsGauntlet(BoardState board, int runSeed)
        {
            var faction = _database.GetFaction("iron_vanguard");
            var registry = _database.BuildRegistry();
            int authority = AuthorityCalculator.ComputeRoundPool(board);

            for (int fight = 1; fight <= RunOrchestrator.MaxFights; fight++)
            {
                var enemy = _database.GetEnemyTemplate(fight).BuildBoard(faction, registry);
                var commands = BuildAggressiveCommands(board);
                var result = new CombatResolver().Resolve(
                    board,
                    enemy,
                    runSeed + fight * 1000,
                    commands,
                    requisition: authority);

                if (!result.PlayerWon)
                    return false;
            }

            return true;
        }

        private static List<PhaseCommand> BuildAggressiveCommands(BoardState board)
        {
            var commands = new List<PhaseCommand>();
            string bunkerId = board.Pieces.FirstOrDefault(p => p.Definition.Id == "command_bunker")?.InstanceId;
            string depotId = board.Pieces.FirstOrDefault(p => p.Definition.Id == "supply_depot")?.InstanceId;

            if (bunkerId != null)
            {
                commands.Add(new PhaseCommand
                {
                    AfterPhase = CombatPhase.Deployment,
                    Type = CommandType.SetTactic,
                    Tactic = TacticType.Advance,
                    SourcePieceId = bunkerId
                });
                commands.Add(new PhaseCommand
                {
                    AfterPhase = CombatPhase.Grind,
                    Type = CommandType.SetTactic,
                    Tactic = TacticType.Advance,
                    SourcePieceId = bunkerId
                });
            }

            if (depotId != null)
            {
                commands.Add(new PhaseCommand
                {
                    AfterPhase = CombatPhase.Deployment,
                    Type = CommandType.SpendRequisitionBuff,
                    Tactic = TacticType.Advance,
                    SourcePieceId = depotId,
                    Cost = 1
                });
                commands.Add(new PhaseCommand
                {
                    AfterPhase = CombatPhase.Grind,
                    Type = CommandType.SpendRequisitionBuff,
                    Tactic = TacticType.Advance,
                    SourcePieceId = depotId,
                    Cost = 1
                });
            }

            return commands;
        }

        private void SubmitCombatCommandsForCurrentWindow()
        {
            var available = _orchestrator.GetAvailableCommands();
            if (available.Count == 0)
                return;

            int budget = _orchestrator.GetPrimaryActionBudget();
            int submitted = 0;
            var completedPhase = _orchestrator.State.Combat.CompletedPhase;

            foreach (var cmd in available)
            {
                if (submitted >= budget)
                    break;

                if (cmd.Type == CommandType.SpendRequisitionBuff)
                {
                    int pool = _orchestrator.State.Combat.Authority > 0
                        ? _orchestrator.State.Combat.Authority
                        : _orchestrator.State.Combat.Requisition;
                    if (pool < cmd.RequisitionCost)
                        continue;
                }

                _orchestrator.SubmitCombatCommand(new PhaseCommand
                {
                    AfterPhase = completedPhase,
                    Type = cmd.Type,
                    Tactic = TacticType.Advance,
                    SourcePieceId = cmd.SourcePieceId,
                    Cost = cmd.RequisitionCost
                });
                submitted++;
            }
        }
    }
}
