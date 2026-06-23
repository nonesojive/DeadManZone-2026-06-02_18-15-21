using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public class RunSaveSerializerTests
    {
        [Test]
        public void SerializeDeserialize_PreservesFourResources()
        {
            var state = new RunState
            {
                Supplies = 120,
                Manpower = 8,
                Authority = 3,
                Morale = 45,
                SaveSchemaVersion = 3
            };
            var json = RunSaveSerializer.Serialize(state);
            var loaded = RunSaveSerializer.Deserialize(json);
            Assert.AreEqual(120, loaded.Supplies);
            Assert.AreEqual(8, loaded.Manpower);
            Assert.AreEqual(3, loaded.Authority);
            Assert.AreEqual(45, loaded.Morale);
        }

        [Test]
        public void Deserialize_MigratesLegacyGoldAndRequisition()
        {
            const string legacyJson =
                "{\n" +
                "  \"FightIndex\": 2,\n" +
                "  \"Gold\": 88,\n" +
                "  \"Requisition\": 5,\n" +
                "  \"RunSeed\": 42,\n" +
                "  \"FactionId\": \"iron_vanguard\",\n" +
                "  \"Phase\": \"Build\"\n" +
                "}";

            var loaded = RunSaveSerializer.Deserialize(legacyJson);

            Assert.AreEqual(2, loaded.SaveSchemaVersion);
            Assert.AreEqual(88, loaded.Supplies);
            Assert.AreEqual(5, loaded.Authority);
            Assert.AreEqual(100, loaded.Manpower);
            Assert.AreEqual(100, loaded.Morale);
        }

        [Test]
        public void SerializeDeserialize_PreservesSuppliesAndFightIndex()
        {
            var state = new RunState
            {
                FightIndex = 3,
                Supplies = 120,
                Authority = 4,
                Manpower = 10,
                Morale = 100,
                RunSeed = 777,
                FactionId = FactionIds.IronVanguard,
                Phase = RunPhase.Build
            };

            var json = RunSaveSerializer.ToJson(state);
            var loaded = RunSaveSerializer.FromJson(json);

            Assert.AreEqual(3, loaded.FightIndex);
            Assert.AreEqual(120, loaded.Supplies);
            Assert.AreEqual(4, loaded.Authority);
            Assert.AreEqual(RunPhase.Build, loaded.Phase);
        }

        [Test]
        public void SerializeDeserialize_PreservesShopOffers()
        {
            var state = new RunState
            {
                Shop = new ShopState
                {
                    Seed = 42,
                    Offers = new System.Collections.Generic.List<ShopOffer>
                    {
                        new ShopOffer
                        {
                            OfferId = "general_rifle_0",
                            Lane = ShopLane.Offensive,
                            PieceId = "rifle_squad",
                            GoldPrice = 10,
                            RequisitionPrice = 0
                        }
                    }
                },
                FrozenOfferId = "general_rifle_0"
            };

            var loaded = RunSaveSerializer.FromJson(RunSaveSerializer.ToJson(state));

            Assert.AreEqual(42, loaded.Shop.Seed);
            Assert.AreEqual(1, loaded.Shop.Offers.Count);
            Assert.AreEqual("rifle_squad", loaded.Shop.Offers.First().PieceId);
            Assert.AreEqual("general_rifle_0", loaded.FrozenOfferId);
        }

        [Test]
        public void SerializeDeserialize_PreservesMidCombatState()
        {
            var state = new RunState
            {
                Phase = RunPhase.Combat,
                FightIndex = 2,
                Combat = new CombatSaveState
                {
                    CombatSeed = 999,
                    CheckpointsFired = 1,
                    LastSegmentIndex = 0,
                    GlobalTick = 0,
                    AwaitingCommand = true,
                    Requisition = 3,
                    PlayerTactic = TacticType.StandGround,
                    PendingSelectedTactic = TacticType.Advance,
                    PendingSelectedAbilities = new System.Collections.Generic.List<GrantedAbility>
                    {
                        GrantedAbility.GrenadeLob
                    },
                    SubmittedCommands = new System.Collections.Generic.List<PhaseCommand>
                    {
                        new PhaseCommand
                        {
                            AfterCheckpoint = 0,
                            Type = CommandType.SetTactic,
                            Tactic = TacticType.Advance,
                            SourcePieceId = "bunker_1"
                        }
                    },
                    EventLog = new System.Collections.Generic.List<CombatEventRecord>
                    {
                        new CombatEventRecord
                        {
                            Segment = 0,
                            Tick = 0,
                            ActorId = "rifle_1",
                            ActionType = "move",
                            Value = 0
                        }
                    }
                }
            };

            var loaded = RunSaveSerializer.FromJson(RunSaveSerializer.ToJson(state));

            Assert.AreEqual(RunPhase.Combat, loaded.Phase);
            Assert.IsTrue(loaded.Combat.AwaitingCommand);
            Assert.AreEqual(1, loaded.Combat.CheckpointsFired);
            Assert.AreEqual(0, loaded.Combat.LastSegmentIndex);
            Assert.AreEqual(TacticType.StandGround, loaded.Combat.PlayerTactic);
            Assert.AreEqual(TacticType.Advance, loaded.Combat.PendingSelectedTactic);
            Assert.AreEqual(GrantedAbility.GrenadeLob, loaded.Combat.PendingSelectedAbilities[0]);
            Assert.AreEqual(1, loaded.Combat.SubmittedCommands.Count);
            Assert.AreEqual(TacticType.Advance, loaded.Combat.SubmittedCommands[0].Tactic);
            Assert.AreEqual(1, loaded.Combat.EventLog.Count);
        }

        [Test]
        public void BoardSnapshot_RoundTripsThroughRegistry()
        {
            var registry = TestContentRegistry.Create();
            var source = TestBoards.WithCommandBunker();
            var snapshot = BoardSnapshotMapper.FromBoard(
                source,
                TestBoards.DefaultRearCols,
                TestBoards.DefaultSupportCols);
            var restored = BoardSnapshotMapper.ToBoard(snapshot, registry);

            Assert.AreEqual(source.Pieces.Count, restored.Pieces.Count);
            Assert.IsTrue(restored.Pieces.Any(p => p.Definition.Id == "command_bunker"));
            Assert.IsTrue(restored.Pieces.Any(p => PieceTagQueries.HasTag(p.Definition, GameTagIds.Hq)));
        }

        [Test]
        public void SerializeDeserialize_PreservesSalvageState()
        {
            var state = new RunState
            {
                LastEnemyFactionId = FactionIds.DustScourge,
                SalvageChancePercent = 23,
                SaveSchemaVersion = 6
            };

            var loaded = RunSaveSerializer.FromJson(RunSaveSerializer.ToJson(state));

            Assert.AreEqual(FactionIds.DustScourge, loaded.LastEnemyFactionId);
            Assert.AreEqual(23, loaded.SalvageChancePercent);
            Assert.AreEqual(6, loaded.SaveSchemaVersion);
        }

        [Test]
        public void TryFromJson_ReturnsFalseOnCorruptData()
        {
            Assert.IsFalse(RunSaveSerializer.TryFromJson("{ not valid json", out _));
        }
    }
}
