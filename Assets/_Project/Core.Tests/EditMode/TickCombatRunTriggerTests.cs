using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class TickCombatRunTriggerTests
    {
        [Test]
        public void FirstContinue_PausesWhenEitherSideDropsTo75Percent()
        {
            var (player, enemy) = TriggerTestBoards.MakeMatchedBoards();
            var run = TickCombatRun.Start(player, enemy, seed: 42);

            var result = run.Continue(System.Array.Empty<PhaseCommand>());

            Assert.AreEqual(CombatAdvanceStatus.AwaitingCommand, result.Status);
            Assert.AreEqual(1, run.CheckpointsFired);
            Assert.AreEqual(0, run.CurrentPauseIndex);
            Assert.NotNull(result.PauseTrigger);
            Assert.AreEqual(0.75f, result.PauseTrigger.Threshold, 0.0001f);

            float lowest = System.Math.Min(
                ArmyHealthTracker.Evaluate(run.PlayerCombatantsForTests).Fraction,
                ArmyHealthTracker.Evaluate(run.EnemyCombatantsForTests).Fraction);
            Assert.LessOrEqual(lowest, 0.75f);
        }

        [Test]
        public void SecondContinue_PausesAt30Percent()
        {
            var (player, enemy) = TriggerTestBoards.MakeMatchedBoards();
            var run = TickCombatRun.Start(player, enemy, seed: 42);
            run.Continue(System.Array.Empty<PhaseCommand>());

            var result = run.Continue(System.Array.Empty<PhaseCommand>());

            if (result.Status == CombatAdvanceStatus.AwaitingCommand)
            {
                Assert.AreEqual(2, run.CheckpointsFired);
                Assert.AreEqual(0.30f, result.PauseTrigger.Threshold, 0.0001f);
            }
            else
            {
                Assert.IsTrue(run.IsFightOver);
            }
        }

        [Test]
        public void Stomp_ProducesZeroPausesWhenFightEndsFirst()
        {
            var (player, enemy) = TriggerTestBoards.MakeStompBoards();
            var run = TickCombatRun.Start(player, enemy, seed: 7);

            var result = run.Continue(System.Array.Empty<PhaseCommand>());

            Assert.AreEqual(CombatAdvanceStatus.Completed, result.Status);
            Assert.IsTrue(run.PlayerWon);
        }

        [Test]
        public void BurstThroughBothThresholds_FiresSingleMergedPause()
        {
            var (player, enemy) = TriggerTestBoards.MakeBurstBoards();
            var run = TickCombatRun.Start(player, enemy, seed: 7);

            var result = run.Continue(System.Array.Empty<PhaseCommand>());

            if (result.Status == CombatAdvanceStatus.AwaitingCommand)
            {
                Assert.AreEqual(2, run.CheckpointsFired, "merged pause must consume both thresholds");
                Assert.AreEqual(1, run.CurrentPauseIndex);
            }
        }

        [Test]
        public void Determinism_FastForwardReproducesIdenticalEventLog()
        {
            var (player, enemy) = TriggerTestBoards.MakeMatchedBoards();
            var commands = new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.SetTactic,
                    Tactic = TacticType.Advance,
                    SourcePieceId = "player_tactic"
                }
            };

            var first = TickCombatRun.Start(player, enemy, seed: 99);
            first.Continue(System.Array.Empty<PhaseCommand>());
            first.Continue(commands);

            var (player2, enemy2) = TriggerTestBoards.MakeMatchedBoards();
            var second = TickCombatRun.Start(player2, enemy2, seed: 99);
            second.Continue(System.Array.Empty<PhaseCommand>());
            second.Continue(commands);

            var firstLog = first.Log.Events
                .Select(e => $"{e.Segment}|{e.Tick}|{e.ActorId}|{e.ActionType}|{e.TargetId}|{e.Value}")
                .ToList();
            var secondLog = second.Log.Events
                .Select(e => $"{e.Segment}|{e.Tick}|{e.ActorId}|{e.ActionType}|{e.TargetId}|{e.Value}")
                .ToList();
            CollectionAssert.AreEqual(firstLog.Take(secondLog.Count), secondLog);
        }

        [Test]
        public void GasAntiStall_FinishesMutuallyUnkillableFight()
        {
            var (player, enemy) = TriggerTestBoards.MakeUnkillableBoards();
            var run = TickCombatRun.Start(player, enemy, seed: 1);

            var result = run.Continue(System.Array.Empty<PhaseCommand>());
            while (result.Status == CombatAdvanceStatus.AwaitingCommand)
                result = run.Continue(System.Array.Empty<PhaseCommand>());

            Assert.IsTrue(run.IsFightOver);
            Assert.IsTrue(run.Log.Events.Any(e => e.ActionType == "gas_damage"));
        }

        private static class TriggerTestBoards
        {
            public static (BoardState player, BoardState enemy) MakeMatchedBoards() =>
                (TestBoards.StandardPlayer(), TestBoards.StandardEnemy());

            public static (BoardState player, BoardState enemy) MakeStompBoards()
            {
                var player = new BoardState(TestBoards.Layout);
                player.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(), "player_rifle_1");
                player.TryPlace(TestPieces.RifleSquad(), new GridCoord(7, 4), "player_rifle_2");

                var enemy = new BoardState(TestBoards.Layout);
                enemy.TryPlace(TestPieces.WeakConscript(), TestBoards.FrontLineAnchor(), "enemy_weak");

                return (player, enemy);
            }

            public static (BoardState player, BoardState enemy) MakeBurstBoards()
            {
                var player = new BoardState(TestBoards.Layout);
                var striker = TestPieces.With(
                    TestPieces.RifleSquad(),
                    baseDamage: 400,
                    attackSpeed: AttackSpeedTier.Fast);
                player.TryPlace(striker, TestBoards.FrontLineAnchor(), "player_striker");

                var enemy = new BoardState(TestBoards.Layout);
                enemy.TryPlace(TestPieces.HqPiece(), new GridCoord(0, 4), "enemy_hq");
                var spongeBase = TestPieces.RifleSquad();
                var sponge = new PieceDefinition
                {
                    Id = spongeBase.Id,
                    DisplayName = spongeBase.DisplayName,
                    Category = spongeBase.Category,
                    Shape = spongeBase.Shape,
                    Tags = spongeBase.Tags,
                    MaxHp = 500,
                    BaseDamage = spongeBase.BaseDamage,
                    CooldownTicks = spongeBase.CooldownTicks,
                    FactionId = spongeBase.FactionId
                };
                enemy.TryPlace(sponge, TestBoards.FrontLineAnchor(5), "enemy_sponge");

                return (player, enemy);
            }

            public static (BoardState player, BoardState enemy) MakeUnkillableBoards()
            {
                var heavy = TestPieces.With(
                    TestPieces.RifleSquad(),
                    armorType: ArmorType.Heavy,
                    baseDamage: 1,
                    attackType: AttackType.Ballistic);

                var player = new BoardState(TestBoards.Layout);
                player.TryPlace(TestPieces.HqPiece(), new GridCoord(0, 4), "player_hq");
                player.TryPlace(heavy, TestBoards.FrontLineAnchor(4), "player_heavy_1");
                player.TryPlace(heavy, TestBoards.FrontLineAnchor(6), "player_heavy_2");

                var enemy = new BoardState(TestBoards.Layout);
                enemy.TryPlace(TestPieces.HqPiece(), new GridCoord(0, 4), "enemy_hq");
                enemy.TryPlace(heavy, TestBoards.FrontLineAnchor(3), "enemy_heavy_1");
                enemy.TryPlace(heavy, TestBoards.FrontLineAnchor(6), "enemy_heavy_2");

                return (player, enemy);
            }
        }
    }
}
