using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using DeadManZone.Game;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>Regression guards for vertical-slice determinism and save integrity.</summary>
    public sealed class VerticalSliceRegressionTests
    {
        private ContentDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _database = ContentDatabase.Load();
            if (_database == null || _database.Pieces.Count == 0)
            {
                Assert.Ignore("ContentDatabase not found. Run DeadManZone → Generate Vertical Slice Content first.");
            }

            SaveManager.DeleteSave();
        }

        [TearDown]
        public void TearDown() => SaveManager.DeleteSave();

        [Test]
        public void Content_HasEnemyTemplatesForAllFights()
        {
            for (int fight = 1; fight <= RunOrchestrator.MaxFights; fight++)
            {
                var template = _database.GetEnemyTemplate(fight);
                Assert.NotNull(template, $"Missing enemy template for fight {fight}.");
                Assert.AreEqual(fight, template.fightNumber,
                    $"Expected dedicated enemy template for fight {fight}. Regenerate content via DeadManZone → Generate Vertical Slice Content.");
            }
        }

        [Test]
        public void AllEnemyTemplates_FixedSeedCombat_IsDeterministic()
        {
            var player = VerticalSliceTestFixtures.BuildGauntletBoard(_database);
            var faction = _database.GetFaction("iron_vanguard");
            var registry = _database.BuildRegistry();
            var commands = VerticalSliceTestFixtures.BuildAggressiveCommands(player);
            var resolver = new CombatResolver();

            for (int fight = 1; fight <= RunOrchestrator.MaxFights; fight++)
            {
                var enemy = _database.GetEnemyTemplate(fight).BuildBoard(faction, registry);
                int combatSeed = VerticalSliceTestFixtures.RegressionRunSeed + fight * 1000;

                var first = resolver.Resolve(
                    player,
                    enemy,
                    combatSeed,
                    commands,
                    VerticalSliceTestFixtures.RegressionRequisition);
                var second = resolver.Resolve(
                    player,
                    enemy,
                    combatSeed,
                    commands,
                    VerticalSliceTestFixtures.RegressionRequisition);

                Assert.AreEqual(first.PlayerWon, second.PlayerWon, $"Fight {fight} win flag differed between runs.");
                AssertCombatLogsEqual(first.EventLog, second.EventLog, fight);
            }
        }

        [Test]
        public void AllEnemyTemplates_FixedSeedCombat_ProducesEventLog()
        {
            var player = VerticalSliceTestFixtures.BuildGauntletBoard(_database);
            var faction = _database.GetFaction("iron_vanguard");
            var registry = _database.BuildRegistry();
            var commands = VerticalSliceTestFixtures.BuildAggressiveCommands(player);
            var resolver = new CombatResolver();

            for (int fight = 1; fight <= RunOrchestrator.MaxFights; fight++)
            {
                var enemy = _database.GetEnemyTemplate(fight).BuildBoard(faction, registry);
                int combatSeed = VerticalSliceTestFixtures.RegressionRunSeed + fight * 1000;

                var result = resolver.Resolve(
                    player,
                    enemy,
                    combatSeed,
                    commands,
                    VerticalSliceTestFixtures.RegressionRequisition);

                Assert.Greater(result.EventLog.Events.Count, 0, $"Fight {fight} produced no combat events.");
            }
        }

        [Test]
        public void Serialize_RoundTripsEveryRunPhase()
        {
            foreach (RunPhase phase in Enum.GetValues(typeof(RunPhase)))
            {
                var original = CreateRepresentativeState(phase);
                var loaded = RunSaveSerializer.FromJson(RunSaveSerializer.ToJson(original));

                Assert.AreEqual(phase, loaded.Phase, $"Phase {phase} did not round-trip.");
                AssertRepresentativeStatePreserved(original, loaded, phase);
            }
        }

        [Test]
        public void SaveManager_RoundTripsEveryRunPhase()
        {
            foreach (RunPhase phase in Enum.GetValues(typeof(RunPhase)))
            {
                var state = CreateRepresentativeState(phase);
                SaveManager.Save(state);

                var orchestrator = new RunOrchestrator(_database);
                Assert.IsTrue(orchestrator.TryLoadSavedRun(), $"Failed to load save for phase {phase}.");
                AssertRepresentativeStatePreserved(state, orchestrator.State, phase);
            }
        }

        [Test]
        public void SaveManager_BuildPhase_RestoresShopAndBoard()
        {
            var orchestrator = new RunOrchestrator(_database);
            orchestrator.StartNewRun("iron_vanguard", runSeed: VerticalSliceTestFixtures.RegressionRunSeed);
            orchestrator.SavePlayerBoard(VerticalSliceTestFixtures.BuildGauntletBoard(_database));
            int supplies = orchestrator.State.Supplies;
            int offerCount = orchestrator.State.Shop.Offers.Count;

            orchestrator.SaveAndExit();

            var reloaded = new RunOrchestrator(_database);
            Assert.IsTrue(reloaded.TryLoadSavedRun());
            Assert.AreEqual(RunPhase.Build, reloaded.State.Phase);
            Assert.AreEqual(supplies, reloaded.State.Supplies);
            Assert.AreEqual(offerCount, reloaded.State.Shop.Offers.Count);
            Assert.AreEqual(8, reloaded.GetPlayerBoard().Pieces.Count);
        }

        private RunState CreateRepresentativeState(RunPhase phase)
        {
            var faction = _database.GetFaction("iron_vanguard");
            var board = VerticalSliceTestFixtures.BuildGauntletBoard(_database);

            var state = RunState.CreateNew(
                "iron_vanguard",
                VerticalSliceTestFixtures.RegressionRunSeed,
                faction.startingSupplies,
                faction.startingManpower,
                faction.startingAuthority,
                faction.startingMorale);
            state.PlayerBoard = BoardSnapshotMapper.FromBoard(board, faction.rearCols, faction.supportCols);
            state.Phase = phase;
            state.FightIndex = 2;
            state.Supplies = 37;
            state.Authority = 5;
            state.SaveSchemaVersion = 3;
            state.Reserves = new ReservesSnapshot
            {
                Width = ReservesState.Width,
                Height = ReservesState.Height,
                Pieces = new List<PlacedPieceRecord>
                {
                    new()
                    {
                        InstanceId = "reserve_rifle",
                        PieceId = "rifle_squad",
                        AnchorX = 0,
                        AnchorY = 0,
                        RotationDegrees = 0
                    }
                }
            };

            switch (phase)
            {
                case RunPhase.Build:
                    state.Shop = new Core.Shop.ShopState
                    {
                        Seed = 99,
                        Offers = new List<Core.Shop.ShopOffer>
                        {
                            new()
                            {
                                OfferId = "general_test_0",
                                Lane = Core.Shop.ShopLane.Offensive,
                                PieceId = "rifle_squad",
                                GoldPrice = 5,
                                RequisitionPrice = 0
                            }
                        }
                    };
                    state.LockedOffer = new ShopOfferRecord
                    {
                        OfferId = "general_test_0",
                        Lane = Core.Shop.ShopLane.Offensive,
                        PieceId = "rifle_squad",
                        GoldPrice = 5,
                        RequisitionPrice = 0
                    };
                    break;

                case RunPhase.Combat:
                    var enemyTemplate = _database.GetEnemyTemplate(state.FightIndex);
                    state.Combat = new CombatSaveState
                    {
                        CombatSeed = VerticalSliceTestFixtures.RegressionRunSeed + state.FightIndex * 1000,
                        EnemyBoard = enemyTemplate.ToBoardSnapshot(),
                        CompletedPhase = CombatPhase.Deployment,
                        AwaitingCommand = true,
                        Authority = 4,
                        Requisition = 4,
                        SubmittedCommands = new List<PhaseCommand>
                        {
                            new()
                            {
                                AfterPhase = CombatPhase.Deployment,
                                Type = CommandType.ChangeStance,
                                Stance = StanceType.AllOutAssault,
                                SourcePieceId = "bunker_1"
                            }
                        },
                        EventLog = new List<CombatEventRecord>
                        {
                            new()
                            {
                                Phase = CombatPhase.Deployment,
                                Tick = 0,
                                ActorId = "rifle_1",
                                ActionType = "damage",
                                TargetId = "enemy_1",
                                Value = 2
                            }
                        }
                    };
                    break;

                case RunPhase.Aftermath:
                    state.Shop = new Core.Shop.ShopState { Seed = 12, Offers = new List<Core.Shop.ShopOffer>() };
                    break;

                case RunPhase.Victory:
                    state.FightIndex = RunOrchestrator.MaxFights;
                    break;

                case RunPhase.Defeat:
                    state.Supplies = 0;
                    break;
            }

            return state;
        }

        private static void AssertRepresentativeStatePreserved(RunState expected, RunState actual, RunPhase phase)
        {
            Assert.AreEqual(expected.FightIndex, actual.FightIndex);
            Assert.AreEqual(expected.Supplies, actual.Supplies);
            Assert.AreEqual(expected.Manpower, actual.Manpower);
            Assert.AreEqual(expected.Authority, actual.Authority);
            Assert.AreEqual(expected.Morale, actual.Morale);
            Assert.AreEqual(expected.RunSeed, actual.RunSeed);
            Assert.AreEqual(expected.FactionId, actual.FactionId);
            Assert.AreEqual(expected.Reserves.Pieces.Count, actual.Reserves.Pieces.Count);
            Assert.AreEqual(
                expected.Reserves.Pieces[0].PieceId,
                actual.Reserves.Pieces[0].PieceId);

            Assert.NotNull(actual.PlayerBoard);
            Assert.AreEqual(expected.PlayerBoard.Pieces.Count, actual.PlayerBoard.Pieces.Count);

            switch (phase)
            {
                case RunPhase.Build:
                    Assert.NotNull(actual.Shop);
                    Assert.AreEqual(expected.Shop.Offers.Count, actual.Shop.Offers.Count);
                    Assert.NotNull(actual.LockedOffer);
                    Assert.AreEqual(expected.LockedOffer.PieceId, actual.LockedOffer.PieceId);
                    break;

                case RunPhase.Combat:
                    Assert.NotNull(actual.Combat);
                    Assert.AreEqual(expected.Combat.CombatSeed, actual.Combat.CombatSeed);
                    Assert.AreEqual(expected.Combat.AwaitingCommand, actual.Combat.AwaitingCommand);
                    Assert.AreEqual(expected.Combat.CompletedPhase, actual.Combat.CompletedPhase);
                    Assert.AreEqual(
                        expected.Combat.Authority > 0 ? expected.Combat.Authority : expected.Combat.Requisition,
                        actual.Combat.Authority > 0 ? actual.Combat.Authority : actual.Combat.Requisition);
                    Assert.AreEqual(expected.Combat.SubmittedCommands.Count, actual.Combat.SubmittedCommands.Count);
                    Assert.AreEqual(expected.Combat.EventLog.Count, actual.Combat.EventLog.Count);
                    break;

                case RunPhase.Aftermath:
                    Assert.NotNull(actual.Shop);
                    break;
            }
        }

        private static void AssertCombatLogsEqual(CombatEventLog first, CombatEventLog second, int fightNumber)
        {
            Assert.AreEqual(
                first.Events.Count,
                second.Events.Count,
                $"Fight {fightNumber}: event count mismatch.");

            for (int i = 0; i < first.Events.Count; i++)
            {
                var a = first.Events[i];
                var b = second.Events[i];
                Assert.AreEqual(a.Phase, b.Phase, $"Fight {fightNumber} event {i} phase.");
                Assert.AreEqual(a.Tick, b.Tick, $"Fight {fightNumber} event {i} tick.");
                Assert.AreEqual(a.ActionType, b.ActionType, $"Fight {fightNumber} event {i} action.");
                Assert.AreEqual(a.ActorId, b.ActorId, $"Fight {fightNumber} event {i} actor.");
                Assert.AreEqual(a.TargetId, b.TargetId, $"Fight {fightNumber} event {i} target.");
                Assert.AreEqual(a.Value, b.Value, $"Fight {fightNumber} event {i} value.");
            }
        }
    }
}
