using System.Linq;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
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
                SaveSchemaVersion = 2
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
            const string legacyJson = """
                {
                  "FightIndex": 2,
                  "Gold": 88,
                  "Requisition": 5,
                  "RunSeed": 42,
                  "FactionId": "iron_vanguard",
                  "Phase": "Build"
                }
                """;

            var loaded = RunSaveSerializer.Deserialize(legacyJson);

            Assert.AreEqual(2, loaded.SaveSchemaVersion);
            Assert.AreEqual(88, loaded.Supplies);
            Assert.AreEqual(5, loaded.Authority);
            Assert.AreEqual(10, loaded.Manpower);
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
                FactionId = "iron_vanguard",
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
                            Lane = ShopLane.General,
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
                    CompletedPhase = CombatPhase.Deployment,
                    AwaitingCommand = true,
                    Requisition = 3,
                    SubmittedCommands = new System.Collections.Generic.List<PhaseCommand>
                    {
                        new PhaseCommand
                        {
                            AfterPhase = CombatPhase.Deployment,
                            Type = CommandType.ChangeStance,
                            Stance = StanceType.AllOutAssault,
                            SourcePieceId = "bunker_1"
                        }
                    },
                    EventLog = new System.Collections.Generic.List<CombatEventRecord>
                    {
                        new CombatEventRecord
                        {
                            Phase = CombatPhase.Deployment,
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
            Assert.AreEqual(CombatPhase.Deployment, loaded.Combat.CompletedPhase);
            Assert.AreEqual(1, loaded.Combat.SubmittedCommands.Count);
            Assert.AreEqual(StanceType.AllOutAssault, loaded.Combat.SubmittedCommands[0].Stance);
            Assert.AreEqual(1, loaded.Combat.EventLog.Count);
        }

        [Test]
        public void BoardSnapshot_RoundTripsThroughRegistry()
        {
            var registry = TestContentRegistry.Create();
            var source = TestBoards.WithCommandBunker();
            var snapshot = BoardSnapshotMapper.FromBoard(source, rearRows: 2, supportRows: 2);
            var restored = BoardSnapshotMapper.ToBoard(snapshot, registry);

            Assert.AreEqual(source.Pieces.Count, restored.Pieces.Count);
            Assert.IsTrue(restored.Pieces.Any(p => p.Definition.Id == "command_bunker"));
        }

        [Test]
        public void TryFromJson_ReturnsFalseOnCorruptData()
        {
            Assert.IsFalse(RunSaveSerializer.TryFromJson("{ not valid json", out _));
        }
    }
}
