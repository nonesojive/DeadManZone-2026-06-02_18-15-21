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
        public void Deserialize_RejectsLegacyGoldAndRequisition()
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

            Assert.Throws<System.InvalidOperationException>(() => RunSaveSerializer.Deserialize(legacyJson));
        }

        [Test]
        public void SerializeDeserialize_PreservesCombatAndHqBoards()
        {
            var state = RunState.CreateNew(FactionIds.IronVanguard, 42, 100, 100, 2, 100);
            state.CombatBoard = new BoardSnapshot
            {
                BoardKind = BoardKind.Combat.ToString(),
                Width = 6,
                Height = 6
            };
            state.HqBoard = new BoardSnapshot
            {
                BoardKind = BoardKind.Hq.ToString(),
                Width = 3,
                Height = 6
            };

            var loaded = RunSaveSerializer.FromJson(RunSaveSerializer.ToJson(state));
            Assert.AreEqual(8, loaded.SaveSchemaVersion);
            Assert.NotNull(loaded.CombatBoard);
            Assert.NotNull(loaded.HqBoard);
            Assert.AreEqual(6, loaded.CombatBoard.Width);
            Assert.AreEqual(3, loaded.HqBoard.Width);
            Assert.AreEqual(6, loaded.HqBoard.Height);
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
            var snapshot = BoardSnapshotMapper.FromBoard(source);
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
                SaveSchemaVersion = 8
            };

            var loaded = RunSaveSerializer.FromJson(RunSaveSerializer.ToJson(state));

            Assert.AreEqual(FactionIds.DustScourge, loaded.LastEnemyFactionId);
            Assert.AreEqual(23, loaded.SalvageChancePercent);
            Assert.AreEqual(8, loaded.SaveSchemaVersion);
        }

        [Test]
        public void TryFromJson_ReturnsFalseOnCorruptData()
        {
            Assert.IsFalse(RunSaveSerializer.TryFromJson("{ not valid json", out _));
        }
    }
}
