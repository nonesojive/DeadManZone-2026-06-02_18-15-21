using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>2026-07-15 faction-roster-v1 §1.8/§2.6 Paradox: Doctor Recursion's "your
    /// pause-window abilities each fire twice" and Resonance Coil's Echo ("repeat the last
    /// [ability] issued this fight, free"). Scoped to abilities only this wave — see
    /// CommandProcessor.TryApplyBatch header note on TacticState.LastAbilityCommand.
    /// Deterministic, zero randomness (border rule) — no RNG involved in either path.</summary>
    public sealed class CommandProcessorRepeatActivationTests
    {
        private static PieceDefinition DoctorRecursion() => new()
        {
            Id = "doctor_recursion",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 20,
            RepeatsPauseAbilities = true
        };

        private static CombatantState ActiveCombatant(string id, PieceDefinition def) => new()
        {
            InstanceId = id,
            Side = CombatSide.Player,
            Definition = def,
            AnchorPosition = new GridCoord(0, 0),
            CurrentHp = def.MaxHp
        };

        private static CombatantState MakeEnemy(string id, int hp) => new()
        {
            InstanceId = id,
            Side = CombatSide.Enemy,
            Definition = TestPieces.WeakConscript(),
            AnchorPosition = new GridCoord(1, 0),
            CurrentHp = hp
        };

        [Test]
        public void DoctorRecursion_Fielded_MortarShotFiresTwice_ForOneAuthorityCost()
        {
            var board = new BoardState(TestBoards.Layout);
            var mortarPiece = TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.MortarShot);
            var mortarPlacement = board.TryPlace(mortarPiece, TestBoards.FrontLineAnchor(), "mortar_1");
            Assert.IsTrue(mortarPlacement.Success);

            var playerCombatants = new List<CombatantState>
            {
                ActiveCombatant("mortar_1", mortarPiece),
                ActiveCombatant("recursion_1", DoctorRecursion())
            };
            var enemy = MakeEnemy("enemy_1", hp: 200);
            var enemyCombatants = new List<CombatantState> { enemy };

            var processor = new CommandProcessor();
            var tactics = new TacticState();
            int authority = 2;
            var command = new PhaseCommand
            {
                AfterCheckpoint = 0,
                Type = CommandType.UseAbility,
                Ability = GrantedAbility.MortarShot,
                SourcePieceId = "mortar_1",
                TargetCell = enemy.AnchorPosition
            };

            var log = new CombatEventLog();
            var result = processor.TryApplyBatch(
                new[] { command }, board, ref authority, tactics,
                playerCombatants, enemyCombatants, log,
                checkpointIndex: 0, logSegment: 0, globalTick: 0);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(0, authority, "Doctor Recursion's second cast is free — only the base cost (2) is charged");
            Assert.AreEqual(2, log.Events.Count(e => e.ActionType == "mortar_shot"),
                "the ability must resolve exactly twice, deterministically");
        }

        [Test]
        public void WithoutDoctorRecursion_MortarShotFiresOnce()
        {
            var board = new BoardState(TestBoards.Layout);
            var mortarPiece = TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.MortarShot);
            board.TryPlace(mortarPiece, TestBoards.FrontLineAnchor(), "mortar_1");

            var playerCombatants = new List<CombatantState> { ActiveCombatant("mortar_1", mortarPiece) };
            var enemy = MakeEnemy("enemy_1", hp: 200);
            var enemyCombatants = new List<CombatantState> { enemy };

            var processor = new CommandProcessor();
            var tactics = new TacticState();
            int authority = 2;
            var command = new PhaseCommand
            {
                AfterCheckpoint = 0,
                Type = CommandType.UseAbility,
                Ability = GrantedAbility.MortarShot,
                SourcePieceId = "mortar_1",
                TargetCell = enemy.AnchorPosition
            };

            var log = new CombatEventLog();
            processor.TryApplyBatch(
                new[] { command }, board, ref authority, tactics,
                playerCombatants, enemyCombatants, log,
                checkpointIndex: 0, logSegment: 0, globalTick: 0);

            Assert.AreEqual(1, log.Events.Count(e => e.ActionType == "mortar_shot"));
        }

        [Test]
        public void Echo_RepeatsLastAbilityCommand_ForFree()
        {
            var board = new BoardState(TestBoards.Layout);
            var mortarPiece = TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.MortarShot);
            board.TryPlace(mortarPiece, TestBoards.FrontLineAnchor(), "mortar_1");
            var resonanceCoil = new PieceDefinition
            {
                Id = "resonance_coil",
                // Unit, not Building: this test places it directly on a Combat-kind board
                // (TestBoards.Layout) — a Building-category piece there would fail placement
                // (Buildings resolve to the HQ board per BoardPlacementRules).
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                MaxHp = 30,
                GrantedAbility = GrantedAbility.Echo
            };
            board.TryPlace(resonanceCoil, TestBoards.SupportLineAnchor(1, 1), "coil_1");

            var playerCombatants = new List<CombatantState> { ActiveCombatant("mortar_1", mortarPiece) };
            var enemy = MakeEnemy("enemy_1", hp: 200);
            var enemyCombatants = new List<CombatantState> { enemy };

            var processor = new CommandProcessor();
            var tactics = new TacticState();
            int authority = 5;
            var log = new CombatEventLog();

            var mortarCommand = new PhaseCommand
            {
                AfterCheckpoint = 0,
                Type = CommandType.UseAbility,
                Ability = GrantedAbility.MortarShot,
                SourcePieceId = "mortar_1",
                TargetCell = enemy.AnchorPosition
            };
            processor.TryApplyBatch(
                new[] { mortarCommand }, board, ref authority, tactics,
                playerCombatants, enemyCombatants, log,
                checkpointIndex: 0, logSegment: 0, globalTick: 0);
            int authorityAfterMortar = authority;

            var echoCommand = new PhaseCommand
            {
                AfterCheckpoint = 1,
                Type = CommandType.UseAbility,
                Ability = GrantedAbility.Echo,
                SourcePieceId = "coil_1"
            };
            var echoResult = processor.TryApplyBatch(
                new[] { echoCommand }, board, ref authority, tactics,
                playerCombatants, enemyCombatants, log,
                checkpointIndex: 1, logSegment: 1, globalTick: 50);

            Assert.IsTrue(echoResult.Success);
            Assert.AreEqual(authorityAfterMortar, authority, "Echo is free — no Authority is spent replaying it");
            Assert.AreEqual(2, log.Events.Count(e => e.ActionType == "mortar_shot"),
                "Echo must replay the mortar shot a second time");
            Assert.IsTrue(log.Events.Any(e => e.ActionType == "echo" && e.ActorId == "coil_1"));
        }

        [Test]
        public void Echo_NoPriorAbility_Fails()
        {
            var board = new BoardState(TestBoards.Layout);
            var resonanceCoil = new PieceDefinition
            {
                Id = "resonance_coil",
                // Unit, not Building: this test places it directly on a Combat-kind board
                // (TestBoards.Layout) — a Building-category piece there would fail placement
                // (Buildings resolve to the HQ board per BoardPlacementRules).
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                MaxHp = 30,
                GrantedAbility = GrantedAbility.Echo
            };
            board.TryPlace(resonanceCoil, TestBoards.SupportLineAnchor(1, 1), "coil_1");

            var processor = new CommandProcessor();
            var tactics = new TacticState();
            int authority = 5;
            var command = new PhaseCommand
            {
                AfterCheckpoint = 0,
                Type = CommandType.UseAbility,
                Ability = GrantedAbility.Echo,
                SourcePieceId = "coil_1"
            };

            var result = processor.TryApplyBatch(
                new[] { command }, board, ref authority, tactics,
                new List<CombatantState>(), new List<CombatantState>(), new CombatEventLog(),
                checkpointIndex: 0, logSegment: 0, globalTick: 0);

            Assert.IsFalse(result.Success);
        }

        [Test]
        public void TransportTarget_OnlyAllowedAtOpeningWindow()
        {
            var board = new BoardState(TestBoards.Layout);
            var transportDef = new PieceDefinition
            {
                Id = "armored_ark",
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                MaxHp = 100,
                IsTransport = true,
                TransportCapacity = 1
            };
            board.TryPlace(transportDef, TestBoards.FrontLineAnchor(), "ark_1");
            var playerCombatants = new List<CombatantState>
            {
                new()
                {
                    InstanceId = "ark_1",
                    Side = CombatSide.Player,
                    Definition = transportDef,
                    AnchorPosition = TestBoards.FrontLineAnchor(),
                    CurrentHp = 100,
                    IsTransport = true
                }
            };

            var processor = new CommandProcessor();
            var tactics = new TacticState();
            int authority = 0;
            var sink = new Dictionary<string, GridCoord>();
            var command = new PhaseCommand
            {
                AfterCheckpoint = 1,
                Type = CommandType.TransportTarget,
                SourcePieceId = "ark_1",
                TargetCell = new GridCoord(0, 0)
            };

            var result = processor.TryApplyBatch(
                new[] { command }, board, ref authority, tactics,
                playerCombatants, new List<CombatantState>(), new CombatEventLog(),
                checkpointIndex: 1, logSegment: 1, globalTick: 100,
                transportTargetSink: sink);

            Assert.IsFalse(result.Success, "§2.5 Armored Ark: transport targeting is opening-window only");
            Assert.IsEmpty(sink);
        }

        [Test]
        public void TransportTarget_AtOpeningWindow_RecordsTargetCell()
        {
            var board = new BoardState(TestBoards.Layout);
            var transportDef = new PieceDefinition
            {
                Id = "armored_ark",
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                MaxHp = 100,
                IsTransport = true,
                TransportCapacity = 1
            };
            board.TryPlace(transportDef, TestBoards.FrontLineAnchor(), "ark_1");
            var target = new GridCoord(2, 2);
            var playerCombatants = new List<CombatantState>
            {
                new()
                {
                    InstanceId = "ark_1",
                    Side = CombatSide.Player,
                    Definition = transportDef,
                    AnchorPosition = TestBoards.FrontLineAnchor(),
                    CurrentHp = 100,
                    IsTransport = true
                }
            };

            var processor = new CommandProcessor();
            var tactics = new TacticState();
            int authority = 0;
            var sink = new Dictionary<string, GridCoord>();
            var command = new PhaseCommand
            {
                AfterCheckpoint = 0,
                Type = CommandType.TransportTarget,
                SourcePieceId = "ark_1",
                TargetCell = target
            };

            var result = processor.TryApplyBatch(
                new[] { command }, board, ref authority, tactics,
                playerCombatants, new List<CombatantState>(), new CombatEventLog(),
                checkpointIndex: 0, logSegment: 0, globalTick: 0,
                transportTargetSink: sink);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(target, sink["ark_1"]);
            Assert.AreEqual(0, authority, "PROVISIONAL: transport targeting is free (choice of WHERE, not when)");
        }
    }
}
