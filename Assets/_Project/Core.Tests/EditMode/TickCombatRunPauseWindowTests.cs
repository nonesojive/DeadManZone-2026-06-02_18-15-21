using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>2026-07-15 faction-roster-v1 §1.7/§2.6/§4 Paradox's The Second Hand:
    /// "PauseThresholds is already a list — allow a fielded piece (HQ building) to add one
    /// window." Only one piece in the game does this (§1.7).</summary>
    public sealed class TickCombatRunPauseWindowTests
    {
        private static PieceDefinition SlowGrinder() => new()
        {
            Id = "slow_grinder",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 200,
            BaseDamage = 5,
            CooldownTicks = 5,
            AccuracyOverride = 100,
            AttackRange = AttackRangeTier.Long,
            ArmorType = ArmorType.Heavy
        };

        private static PieceDefinition DurableHarmlessEnemy() => new()
        {
            Id = "durable_harmless",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 100,
            ArmorType = ArmorType.None,
            BaseDamage = 0
        };

        private static PieceDefinition TheSecondHand() => new()
        {
            Id = "the_second_hand",
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 0,
            AddsPauseWindow = true
        };

        [Test]
        public void WithoutThirdWindowPiece_OnlyOnePauseBeforeCompletion()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(SlowGrinder(), TestBoards.FrontLineAnchor(), "player_grinder");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(DurableHarmlessEnemy(), TestBoards.FrontLineAnchor(), "enemy_durable");

            var run = TickCombatRun.Start(player, enemy, seed: 1);

            var first = run.Continue(System.Array.Empty<PhaseCommand>());
            Assert.AreEqual(CombatAdvanceStatus.AwaitingCommand, first.Status);
            Assert.AreEqual(1, run.CheckpointsFired);

            var second = run.Continue(System.Array.Empty<PhaseCommand>());
            Assert.AreEqual(CombatAdvanceStatus.Completed, second.Status,
                "without a fielded AddsPauseWindow piece there is only the one mid-fight pause");
        }

        [Test]
        public void WithThirdWindowPiece_OnHqBoard_FiresASecondMidFightPause()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(SlowGrinder(), TestBoards.FrontLineAnchor(), "player_grinder");
            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            hq.TryPlace(TheSecondHand(), new GridCoord(0, 0), "second_hand_1");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(DurableHarmlessEnemy(), TestBoards.FrontLineAnchor(), "enemy_durable");

            var run = TickCombatRun.Start(
                player, enemy, seed: 1, authority: 0,
                playerBuildBoards: new BuildBoardSet { Combat = player, Hq = hq });

            var first = run.Continue(System.Array.Empty<PhaseCommand>());
            Assert.AreEqual(CombatAdvanceStatus.AwaitingCommand, first.Status);
            Assert.AreEqual(1, run.CheckpointsFired);
            Assert.AreEqual(1, run.CurrentPauseIndex);

            var second = run.Continue(System.Array.Empty<PhaseCommand>());
            Assert.AreEqual(CombatAdvanceStatus.AwaitingCommand, second.Status,
                "The Second Hand must add a third pause window before the fight can complete");
            Assert.AreEqual(2, run.CheckpointsFired);
            Assert.AreEqual(2, run.CurrentPauseIndex);
        }
    }
}
