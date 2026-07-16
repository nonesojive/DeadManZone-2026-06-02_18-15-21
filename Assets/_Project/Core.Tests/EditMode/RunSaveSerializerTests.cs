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
                "  \"FactionId\": \"ironmarch_union\",\n" +
                "  \"Phase\": \"Build\"\n" +
                "}";

            Assert.Throws<System.InvalidOperationException>(() => RunSaveSerializer.Deserialize(legacyJson));
        }

        [Test]
        public void SerializeDeserialize_PreservesCombatAndHqBoards()
        {
            var state = RunState.CreateNew(FactionIds.IronmarchUnion, 42, 100, 100, 2);
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
            Assert.AreEqual(10, loaded.SaveSchemaVersion);
            Assert.NotNull(loaded.CombatBoard);
            Assert.NotNull(loaded.HqBoard);
            Assert.AreEqual(6, loaded.CombatBoard.Width);
            Assert.AreEqual(3, loaded.HqBoard.Width);
            Assert.AreEqual(6, loaded.HqBoard.Height);
        }

        /// <summary>
        /// ShopV2 flip validation gate: "save/continue roundtrip mid-shop (locked offer +
        /// chosen front persist)". The V2 shop reads BOTH of these straight out of RunState
        /// to paint its slots and fight cards, so if either is dropped by the save the player
        /// reloads into a shop that silently forgot what they locked and which front they
        /// committed to.
        /// </summary>
        [Test]
        public void SerializeDeserialize_PreservesLockedOffersAndChosenFront()
        {
            var state = new RunState
            {
                FightIndex = 2,
                Supplies = 60,
                Authority = 3,
                Manpower = 90,
                RunSeed = 1234,
                FactionId = FactionIds.IronmarchUnion,
                Phase = RunPhase.Build,
                FightOptions =
                {
                    new FightOptionRecord
                    {
                        Tier = FightOptionTier.Easy,
                        EnemyFactionId = "neutral",
                        TemplateFightNumber = 2
                    },
                    new FightOptionRecord
                    {
                        Tier = FightOptionTier.Hard,
                        EnemyFactionId = "ash_wraiths",
                        TemplateFightNumber = 2,
                        ConditionId = "gas_drift"
                    }
                },
                ChosenFightOption = 1,
                LockedOffers =
                {
                    new ShopOfferRecord
                    {
                        OfferId = "offer-abc",
                        SlotIndex = 3,
                        PieceId = "marksman_doctrine_officer",
                        GoldPrice = 20
                    }
                }
            };

            var loaded = RunSaveSerializer.FromJson(RunSaveSerializer.ToJson(state));

            Assert.AreEqual(1, loaded.ChosenFightOption, "chosen front must survive the roundtrip");

            Assert.AreEqual(2, loaded.FightOptions.Count);
            Assert.AreEqual(FightOptionTier.Hard, loaded.FightOptions[1].Tier);
            Assert.AreEqual("gas_drift", loaded.FightOptions[1].ConditionId,
                "hard-tier battle condition is shown up front — consent, not gotcha");

            Assert.AreEqual(1, loaded.LockedOffers.Count, "locked offer must survive the roundtrip");
            var locked = loaded.LockedOffers[0];
            Assert.AreEqual("offer-abc", locked.OfferId);
            Assert.AreEqual(3, locked.SlotIndex,
                "SlotIndex is what ShopV2ShopBandPresenter matches on to paint the lock");
            Assert.AreEqual("marksman_doctrine_officer", locked.PieceId);
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
                RunSeed = 777,
                FactionId = FactionIds.IronmarchUnion,
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
                            PieceId = "conscript_rifles",
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
            Assert.AreEqual("conscript_rifles", loaded.Shop.Offers.First().PieceId);
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
                        GrantedAbility.MortarShot
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
            Assert.AreEqual(GrantedAbility.MortarShot, loaded.Combat.PendingSelectedAbilities[0]);
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
            Assert.IsTrue(restored.Pieces.Any(p => p.Definition.Id == "command_outpost"));
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
            Assert.AreEqual(10, loaded.SaveSchemaVersion, "v8 input is stamped to the current schema on load");
        }

        [Test]
        public void FromJson_MigratesV8SingularLockedOfferAndIgnoresPlayerBoard()
        {
            // v8 save carrying both retired members: the singular LockedOffer (pre-list)
            // and the obsolete PlayerBoard snapshot. v9 removed both properties — the
            // offer folds into LockedOffers, the stale board key is simply ignored.
            const string oldJson =
                "{\n" +
                "  \"SaveSchemaVersion\": 8,\n" +
                "  \"Phase\": \"Build\",\n" +
                "  \"LockedOffer\": { \"SlotIndex\": 2, \"PieceId\": \"conscript_rifles\", \"GoldPrice\": 13 },\n" +
                "  \"PlayerBoard\": { \"Width\": 8, \"Height\": 6 }\n" +
                "}";

            var loaded = RunSaveSerializer.FromJson(oldJson);

            Assert.AreEqual(1, loaded.LockedOffers.Count, "singular LockedOffer folds into the list");
            Assert.AreEqual(2, loaded.LockedOffers[0].SlotIndex);
            Assert.AreEqual("conscript_rifles", loaded.LockedOffers[0].PieceId);
            Assert.AreEqual(10, loaded.SaveSchemaVersion);
        }

        [Test]
        public void FromJson_MigratesV9ByIgnoringRetiredMoraleKey()
        {
            // v9 save carrying the retired run-level Morale resource (ADR-0005, M5).
            // v10 removed the member — the stray key is simply ignored on load.
            const string v9Json =
                "{\n" +
                "  \"SaveSchemaVersion\": 9,\n" +
                "  \"Phase\": \"Build\",\n" +
                "  \"Morale\": 42,\n" +
                "  \"Manpower\": 17\n" +
                "}";

            var loaded = RunSaveSerializer.FromJson(v9Json);

            Assert.AreEqual(10, loaded.SaveSchemaVersion);
            Assert.AreEqual(17, loaded.Manpower);
            Assert.AreEqual(100, loaded.LastFightSalvageKillPercent,
                "additive field defaults to the neutral kill share on older saves");
        }

        [Test]
        public void TryFromJson_ReturnsFalseOnCorruptData()
        {
            Assert.IsFalse(RunSaveSerializer.TryFromJson("{ not valid json", out _));
        }

        [Test]
        public void FromJson_MigratesGrenadeLobSavesToMortarShot()
        {
            // Pre-rename v8 save: string enums ("GrenadeLob") + replay event strings ("grenade_lob").
            const string oldJson =
                "{\n" +
                "  \"SaveSchemaVersion\": 8,\n" +
                "  \"Phase\": \"Combat\",\n" +
                "  \"Combat\": {\n" +
                "    \"AwaitingCommand\": true,\n" +
                "    \"PendingSelectedAbilities\": [\"GrenadeLob\", \"ShieldAllies\"],\n" +
                "    \"SubmittedCommands\": [{ \"AfterCheckpoint\": 0, \"Type\": \"UseAbility\", \"Ability\": \"GrenadeLob\", \"SourcePieceId\": \"p1\" }],\n" +
                "    \"EventLog\": [{ \"Segment\": 0, \"Tick\": 3, \"ActorId\": \"p1\", \"ActionType\": \"grenade_lob\", \"TargetId\": \"e1\", \"Value\": 30 }]\n" +
                "  }\n" +
                "}";

            var loaded = RunSaveSerializer.FromJson(oldJson);

            Assert.AreEqual(GrantedAbility.MortarShot, loaded.Combat.PendingSelectedAbilities[0]);
            Assert.AreEqual(GrantedAbility.ShieldAllies, loaded.Combat.PendingSelectedAbilities[1]);
            Assert.AreEqual(GrantedAbility.MortarShot, loaded.Combat.SubmittedCommands[0].Ability);
            Assert.AreEqual("mortar_shot", loaded.Combat.EventLog[0].ActionType);
        }
    }
}
