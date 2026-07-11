using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>Pause-command regressions: event segment alignment (F1), occupancy release
    /// on ability kills (F2), and pause-granted armor buff lifetime (F6).</summary>
    public sealed class TickCombatRunCommandTests
    {
        [Test]
        public void OpeningPauseAbilityKill_LogsKillAndFightEndInSameSegment()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(
                TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.GrenadeLob),
                TestBoards.FrontLineAnchor(3),
                "player_grenadier");
            var enemy = TestBoards.WeakEnemyOnly();

            var run = TickCombatRun.Start(player, enemy, seed: 7, authority: 2);
            var weakAnchor = run.EnemyCombatantsForTests.Single().AnchorPosition;

            var result = run.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.GrenadeLob,
                    SourcePieceId = "player_grenadier",
                    TargetCell = weakAnchor
                }
            });

            Assert.AreEqual(CombatAdvanceStatus.Completed, result.Status);
            var destroyed = run.Log.Events.Single(e => e.ActionType == "destroyed");
            var fightEnd = run.Log.Events.Single(e => e.ActionType == "fight_end");
            Assert.AreEqual(result.SegmentIndex, destroyed.Segment,
                "pause-ability kill must land in the segment the playback plays");
            Assert.AreEqual(destroyed.Segment, fightEnd.Segment,
                "fight_end must share the kill's segment");
            Assert.LessOrEqual(run.Log.Events.Max(e => e.Segment), run.CheckpointsFired,
                "no event may exceed the max playable segment");
        }

        [Test]
        public void MidPauseAbilityKill_LogsInSegmentThatPlaysNext()
        {
            var run = StartMatchedFightWithRearConscript(authority: 3);

            var first = run.Continue(System.Array.Empty<PhaseCommand>());
            Assert.AreEqual(CombatAdvanceStatus.AwaitingCommand, first.Status);
            Assert.AreEqual(1, run.CheckpointsFired);

            var weak = run.EnemyCombatantsForTests.Single(c => c.InstanceId == "enemy_weak");
            Assert.IsTrue(weak.IsAlive, "rear conscript should survive to the mid pause");

            var second = run.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 1,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.GrenadeLob,
                    SourcePieceId = "player_grenadier",
                    TargetCell = weak.AnchorPosition
                }
            });

            var destroyed = run.Log.Events.Single(e =>
                e.ActionType == "destroyed" && e.ActorId == "enemy_weak");
            Assert.AreEqual(1, destroyed.Segment,
                "mid-pause ability kill must land in segment 1 (the one playing next)");
            Assert.AreEqual(second.SegmentIndex, destroyed.Segment);
            Assert.LessOrEqual(run.Log.Events.Max(e => e.Segment), run.CheckpointsFired,
                "no event may exceed the max playable segment");
        }

        [Test]
        public void MidPauseAbilityKill_ReleasesOccupancyGrid()
        {
            var run = StartMatchedFightWithRearConscript(authority: 3);

            var first = run.Continue(System.Array.Empty<PhaseCommand>());
            Assert.AreEqual(CombatAdvanceStatus.AwaitingCommand, first.Status);

            var weak = run.EnemyCombatantsForTests.Single(c => c.InstanceId == "enemy_weak");
            Assert.IsTrue(weak.IsAlive);

            run.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 1,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.GrenadeLob,
                    SourcePieceId = "player_grenadier",
                    TargetCell = weak.AnchorPosition
                }
            });

            Assert.IsFalse(weak.IsAlive, "grenade should kill the 3hp conscript");
            Assert.IsFalse(
                run.OccupancySnapshotForTests.Values.Contains("enemy_weak"),
                "ability kill must free the victim's cells for pathfinding");
        }

        [Test]
        public void ShieldAllies_GrantedAtPause_SurvivesTheCommandBatch()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(
                TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.ShieldAllies),
                TestBoards.FrontLineAnchor(4),
                "player_shield");
            player.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(5), "player_rifle");
            var enemy = TestBoards.WeakEnemyOnly();

            var run = TickCombatRun.Start(player, enemy, seed: 11, authority: 2);

            run.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.UseAbility,
                    Ability = GrantedAbility.ShieldAllies,
                    SourcePieceId = "player_shield"
                }
            });

            Assert.IsTrue(
                run.Log.Events.Any(e => e.ActionType == "shield_allies" && e.TargetId == "player_rifle"),
                "ShieldAllies should log its grant to the adjacent infantry ally");
            var buffed = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "player_rifle");
            Assert.AreEqual(1, buffed.ArmorBuffSteps,
                "armor granted during the pause batch must survive until the next pause");
        }

        /// <summary>Matched rifle lines (fires the 60% mid pause) plus an immobile 3hp
        /// conscript parked deep in the enemy support zone (the deepest zone that accepts
        /// Units) — away from the front through segment 0, a deterministic grenade victim
        /// at the mid pause (GrenadeLob costs 3 there).</summary>
        private static TickCombatRun StartMatchedFightWithRearConscript(int authority)
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(
                TestPieces.With(TestPieces.RifleSquad(), grantedAbility: GrantedAbility.GrenadeLob),
                TestBoards.FrontLineAnchor(3),
                "player_grenadier");
            player.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(6), "player_rifle_2");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(3), "enemy_rifle_1");
            enemy.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(6), "enemy_rifle_2");
            Assert.IsTrue(
                enemy.TryPlace(
                    TestPieces.With(TestPieces.WeakConscript(), movementSpeed: 0),
                    TestBoards.SupportLineAnchor(1, 0),
                    "enemy_weak").Success,
                "rear conscript must be placed (support zone is the deepest zone that accepts Units)");

            return TickCombatRun.Start(player, enemy, seed: 42, authority: authority);
        }
    }
}
