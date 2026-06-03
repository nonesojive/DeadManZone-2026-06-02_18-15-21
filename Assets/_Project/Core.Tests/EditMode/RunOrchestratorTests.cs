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
            var place = board.TryPlace(bunker, new Core.Common.GridCoord(1, 2), "bunker_1");
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

            Assert.IsTrue(_orchestrator.TryRerollLane(Core.Shop.ShopLane.General));
            Assert.IsTrue(_orchestrator.TryRerollLane(Core.Shop.ShopLane.Engineers));

            Assert.AreEqual(startingSupplies - 3, _orchestrator.State.Supplies);
            Assert.AreEqual(2, _orchestrator.State.RerollCountThisRound);
        }

        [Test]
        public void LockedOffer_PersistsAcrossMultipleRerolls()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 202);
            var toLock = _orchestrator.State.Shop.Offers.First(o => o.Lane == Core.Shop.ShopLane.General);
            _orchestrator.SetLockedOffer(toLock, locked: true);
            string lockedPieceId = toLock.PieceId;

            Assert.IsTrue(_orchestrator.TryRerollLane(Core.Shop.ShopLane.General));
            Assert.IsTrue(_orchestrator.State.Shop.Offers.Any(o =>
                o.Lane == Core.Shop.ShopLane.General && o.PieceId == lockedPieceId));

            Assert.IsTrue(_orchestrator.TryRerollLane(Core.Shop.ShopLane.General));
            Assert.IsTrue(_orchestrator.State.Shop.Offers.Any(o =>
                o.Lane == Core.Shop.ShopLane.General && o.PieceId == lockedPieceId));
        }

        [Test]
        public void TryAcquireOfferToBench_RemovesOfferAndAddsToBench()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 303);
            var offer = _orchestrator.State.Shop.Offers.First();
            int suppliesBefore = _orchestrator.State.Supplies;
            int offerCountBefore = _orchestrator.State.Shop.Offers.Count;

            Assert.IsTrue(_orchestrator.TryAcquireOfferToBench(offer.OfferId));
            Assert.AreEqual(suppliesBefore - offer.GoldPrice, _orchestrator.State.Supplies);
            Assert.AreEqual(offerCountBefore - 1, _orchestrator.State.Shop.Offers.Count);
            Assert.Contains(offer.PieceId, _orchestrator.State.BenchPieceIds);
        }

        [Test]
        public void TryAcquireOfferToBoard_InvalidZone_DoesNotCharge()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 404);
            var offer = _orchestrator.State.Shop.Offers.First(o => o.PieceId == "rifle_squad");
            int suppliesBefore = _orchestrator.State.Supplies;

            bool placed = _orchestrator.TryAcquireOfferToBoard(
                offer.OfferId,
                new Core.Common.GridCoord(0, 0));

            Assert.IsFalse(placed);
            Assert.AreEqual(suppliesBefore, _orchestrator.State.Supplies);
        }

        [Test]
        public void TryMovePlacedPiece_RelocatesOnBoard()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 505);
            var board = _orchestrator.GetPlayerBoard();
            var bunker = TestPieces.CommandBunker();
            Assert.IsTrue(board.TryPlace(bunker, new Core.Common.GridCoord(1, 2), "bunker_1").Success);
            _orchestrator.SavePlayerBoard(board);

            Assert.IsTrue(_orchestrator.TryMovePlacedPiece("bunker_1", new Core.Common.GridCoord(0, 2)));

            var updated = _orchestrator.GetPlayerBoard();
            var piece = updated.Pieces.First(p => p.InstanceId == "bunker_1");
            Assert.AreEqual(0, piece.Anchor.X);
            Assert.AreEqual(2, piece.Anchor.Y);
        }

        [Test]
        public void TryMoveBoardToBench_RemovesFromBoardAndFillsBench()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 606);
            var board = _orchestrator.GetPlayerBoard();
            var bunker = TestPieces.CommandBunker();
            Assert.IsTrue(board.TryPlace(bunker, new Core.Common.GridCoord(1, 2), "bunker_1").Success);
            _orchestrator.SavePlayerBoard(board);

            Assert.IsTrue(_orchestrator.TryMoveBoardToBench("bunker_1", 0));
            Assert.AreEqual(0, _orchestrator.GetPlayerBoard().Pieces.Count());
            Assert.Contains(bunker.Id, _orchestrator.State.BenchPieceIds);
        }

        [Test]
        public void SaveMidCombat_RestoresAwaitingCommandWindow()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 909);
            var board = _orchestrator.GetPlayerBoard();
            Assert.IsTrue(board.TryPlace(TestPieces.CommandBunker(), new Core.Common.GridCoord(1, 2), "bunker_1").Success);
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
        public void SaveMidBuild_RestoresGoldAndBench()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 808);
            _orchestrator.State.Supplies = 42;
            _orchestrator.State.BenchPieceIds.Add("rifle_squad");
            _orchestrator.SaveAndExit();

            var reloaded = new RunOrchestrator(_database);
            Assert.IsTrue(reloaded.TryLoadSavedRun());
            Assert.AreEqual(42, reloaded.State.Supplies);
            Assert.Contains("rifle_squad", reloaded.State.BenchPieceIds);
        }

        [Test]
        public void RerollLane_ChangesOnlySelectedLaneOffers()
        {
            _orchestrator.StartNewRun("iron_vanguard", runSeed: 303);
            var before = _orchestrator.State.Shop.Offers.ToList();

            Assert.IsTrue(_orchestrator.TryRerollLane(Core.Shop.ShopLane.General));
            var after = _orchestrator.State.Shop.Offers;

            var beforeEngineers = before.Where(o => o.Lane == Core.Shop.ShopLane.Engineers)
                .Select(o => o.OfferId)
                .OrderBy(id => id)
                .ToArray();
            var afterEngineers = after.Where(o => o.Lane == Core.Shop.ShopLane.Engineers)
                .Select(o => o.OfferId)
                .OrderBy(id => id)
                .ToArray();
            CollectionAssert.AreEquivalent(beforeEngineers, afterEngineers);
        }

        [Test]
        public void FullCombatLoop_CanReachVictoryWithStrongBoard()
        {
            var board = BuildGauntletTestBoard();
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

        private BoardState BuildGauntletTestBoard()
        {
            var faction = _database.GetFaction("iron_vanguard");
            var board = new BoardState(faction.CreateBoardLayout());

            TryPlacePiece(board, "field_gun_nest", new Core.Common.GridCoord(0, 0), "gun_1");
            TryPlacePiece(board, "supply_depot", new Core.Common.GridCoord(2, 0), "depot_1");
            TryPlacePiece(board, "command_bunker", new Core.Common.GridCoord(1, 2), "bunker_1");
            TryPlacePiece(board, "mortar_crew", new Core.Common.GridCoord(4, 3), "mortar_1");
            TryPlacePiece(board, "rifle_squad", new Core.Common.GridCoord(0, 4), "rifle_1");
            TryPlacePiece(board, "diesel_walker", new Core.Common.GridCoord(2, 4), "walker_1");
            TryPlacePiece(board, "mg_team", new Core.Common.GridCoord(5, 4), "mg_1");
            TryPlacePiece(board, "mobile_artillery", new Core.Common.GridCoord(6, 3), "artillery_1");

            return board;
        }

        private void TryPlacePiece(BoardState board, string pieceId, Core.Common.GridCoord anchor, string instanceId)
        {
            var piece = _database.Pieces.First(p => p.id == pieceId).ToCore();
            var result = board.TryPlace(piece, anchor, instanceId);
            Assert.IsTrue(result.Success, $"Failed to place {pieceId} at {anchor}: {result.Reason}");
        }

        private int FindWinningSeed(BoardState board, int startSeed)
        {
            for (int seed = startSeed; seed < startSeed + 100; seed++)
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

            for (int fight = 1; fight <= RunOrchestrator.MaxFights; fight++)
            {
                var enemy = _database.GetEnemyTemplate(fight).BuildBoard(faction, registry);
                var commands = BuildAggressiveCommands(board);
                var result = new CombatResolver().Resolve(
                    board,
                    enemy,
                    runSeed + fight * 1000,
                    commands,
                    requisition: 8);

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
                    Type = CommandType.ChangeStance,
                    Stance = StanceType.AllOutAssault,
                    SourcePieceId = bunkerId
                });
                commands.Add(new PhaseCommand
                {
                    AfterPhase = CombatPhase.Grind,
                    Type = CommandType.ChangeStance,
                    Stance = StanceType.AllOutAssault,
                    SourcePieceId = bunkerId
                });
            }

            if (depotId != null)
            {
                commands.Add(new PhaseCommand
                {
                    AfterPhase = CombatPhase.Deployment,
                    Type = CommandType.SpendRequisitionBuff,
                    Stance = StanceType.AllOutAssault,
                    SourcePieceId = depotId,
                    Cost = 1
                });
                commands.Add(new PhaseCommand
                {
                    AfterPhase = CombatPhase.Grind,
                    Type = CommandType.SpendRequisitionBuff,
                    Stance = StanceType.AllOutAssault,
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

                if (cmd.Type == CommandType.SpendRequisitionBuff &&
                    _orchestrator.State.Combat.Requisition < cmd.RequisitionCost)
                    continue;

                _orchestrator.SubmitCombatCommand(new PhaseCommand
                {
                    AfterPhase = completedPhase,
                    Type = cmd.Type,
                    Stance = StanceType.AllOutAssault,
                    SourcePieceId = cmd.SourcePieceId,
                    Cost = cmd.RequisitionCost
                });
                submitted++;
            }
        }
    }
}
