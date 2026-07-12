using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class MoraleRoutTests
    {
        private static readonly string[] AttackActions = { "damage", "graze", "miss" };

        [Test]
        public void TerrorHit_ReducesMoraleAndLogsMoraleDamage()
        {
            var run = StartTerrorVsSingleMoraleTarget();
            RunToCompletion(run);

            var moraleEvents = run.Log.Events.Where(e => e.ActionType == "morale_damage").ToList();
            Assert.IsNotEmpty(moraleEvents);
            foreach (var moraleEvent in moraleEvents)
            {
                Assert.AreEqual("player_terror", moraleEvent.ActorId);
                Assert.AreEqual("enemy_mor", moraleEvent.TargetId);
                Assert.AreEqual(5, moraleEvent.Value);
            }

            var enemy = run.EnemyCombatantsForTests.Single(c => c.InstanceId == "enemy_mor");
            Assert.AreEqual(20 - 5 * moraleEvents.Count, enemy.CurrentMorale);
        }

        [Test]
        public void MoraleZero_RoutsUnitAndFreesOccupancy()
        {
            var run = StartTerrorVsSingleMoraleTarget();
            RunToCompletion(run);

            var rout = run.Log.Events.Single(e => e.ActionType == "rout");
            Assert.AreEqual("enemy_mor", rout.ActorId);
            Assert.AreEqual("player_terror", rout.TargetId);
            Assert.AreEqual(0, rout.Value);

            var enemy = run.EnemyCombatantsForTests.Single(c => c.InstanceId == "enemy_mor");
            Assert.IsTrue(enemy.IsAlive, "Routed is not dead");
            Assert.IsTrue(enemy.IsBroken);
            Assert.IsFalse(enemy.IsActive);
            CollectionAssert.DoesNotContain(run.OccupancySnapshotForTests.Values, "enemy_mor");
        }

        [Test]
        public void SideWithOnlyBrokenSurvivors_LosesFight()
        {
            var run = StartTerrorVsSingleMoraleTarget();
            var result = RunToCompletion(run);

            Assert.IsTrue(run.IsFightOver);
            Assert.IsTrue(run.PlayerWon);
            Assert.IsFalse(run.IsDraw);
            Assert.AreEqual(0, result.EnemyKilled);
            Assert.AreEqual(1, result.EnemyRouted);
        }

        [Test]
        public void RoutedUnit_StopsAttackingAndBeingTargeted()
        {
            var player = new BoardState(TestBoards.Layout);
            Place(player, TerrorRifle(baseDamage: 4), new GridCoord(8, 4), "player_terror");

            var enemy = new BoardState(TestBoards.Layout);
            Place(enemy, MoraleTarget(maxMorale: 20, maxHp: 100, baseDamage: 2, attackRange: AttackRangeTier.Long),
                new GridCoord(8, 4), "enemy_a_target");
            Place(enemy, MoraleTarget(maxMorale: 0, maxHp: 40), new GridCoord(8, 6), "enemy_b_immune");

            var run = TickCombatRun.Start(player, enemy, seed: 42);
            var result = RunToCompletion(run);

            var events = run.Log.Events;
            int routIndex = events.FindIndex(e => e.ActionType == "rout" && e.ActorId == "enemy_a_target");
            Assert.GreaterOrEqual(routIndex, 0, "enemy_a_target should rout");

            Assert.IsTrue(
                events.Take(routIndex).Any(e => AttackActions.Contains(e.ActionType) && e.ActorId == "enemy_a_target"),
                "Routed unit should have attacked before breaking");
            Assert.IsFalse(
                events.Skip(routIndex + 1).Any(e => AttackActions.Contains(e.ActionType) && e.ActorId == "enemy_a_target"),
                "Routed unit must stop attacking");
            Assert.IsFalse(
                events.Skip(routIndex + 1).Any(e => AttackActions.Contains(e.ActionType) && e.TargetId == "enemy_a_target"),
                "Routed unit must stop being targeted");
            Assert.IsFalse(
                events.Skip(routIndex + 1).Any(e => e.ActionType == "gas_damage" && e.TargetId == "enemy_a_target"),
                "Routed unit fled the field, so gas skips it");

            CollectionAssert.DoesNotContain(run.OccupancySnapshotForTests.Values, "enemy_a_target");
            Assert.IsTrue(run.PlayerWon);
            Assert.AreEqual(1, result.EnemyKilled);
            Assert.AreEqual(1, result.EnemyRouted);
        }

        [Test]
        public void DeathShock_HitsOnlyAlliesWithinRadius_AndCanCascadeRouts()
        {
            var run = StartKillerVsDeathShockPack();
            RunToCompletion(run);

            var shocks = run.Log.Events.Where(e => e.ActionType == "morale_damage").ToList();
            Assert.AreEqual(2, shocks.Count, "Only the death shocks nearby allies; shock-routs don't shock again");
            foreach (var shock in shocks)
            {
                Assert.AreEqual("enemy_a_dies", shock.ActorId);
                Assert.AreEqual(MoraleRules.DeathShockDamage, shock.Value);
            }

            CollectionAssert.AreEquivalent(
                new[] { "enemy_b_near", "enemy_c_near" },
                shocks.Select(e => e.TargetId).ToList());

            var routs = run.Log.Events.Where(e => e.ActionType == "rout").ToList();
            CollectionAssert.AreEquivalent(
                new[] { "enemy_b_near", "enemy_c_near" },
                routs.Select(e => e.ActorId).ToList());
            Assert.IsTrue(routs.All(e => e.TargetId == "enemy_a_dies"));

            var far = run.EnemyCombatantsForTests.Single(c => c.InstanceId == "enemy_d_far");
            Assert.AreEqual(50, far.CurrentMorale, "Ally outside DeathShockRadius takes no shock");
            Assert.IsFalse(far.IsBroken);
        }

        [Test]
        public void EnemyKilledAndRoutedCounts_SplitDeadFromBroken()
        {
            var run = StartKillerVsDeathShockPack();
            var result = RunToCompletion(run);

            Assert.IsTrue(run.PlayerWon);
            Assert.AreEqual(2, result.EnemyKilled);
            Assert.AreEqual(2, result.EnemyRouted);
        }

        [Test]
        public void BattleReport_CarriesRoutCounts()
        {
            var run = StartTerrorVsSingleMoraleTarget();
            var result = RunToCompletion(run);

            Assert.AreEqual(1, result.BattleReport.EnemyRouted);
            Assert.AreEqual(0, result.BattleReport.EnemyKilled);
            Assert.AreEqual(0, result.BattleReport.PlayerRouted, "No player unit broke in this duel");
        }

        [Test]
        public void ReplayState_RoutRemovesUnitAnchor_LikeDestroyed()
        {
            var player = new BoardState(TestBoards.Layout);
            Place(player, TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(), "p1");
            var enemy = new BoardState(TestBoards.Layout);
            var battlefield = BattlefieldState.FromBoards(player, enemy);

            var replay = new CombatReplayState();
            replay.ResetFromBattlefield(battlefield);
            Assert.IsTrue(replay.TryGetAnchor("p1", out _));

            replay.ApplyEvent(new CombatEvent { ActionType = "rout", ActorId = "p1", TargetId = "enemy_x" });

            Assert.IsFalse(replay.TryGetAnchor("p1", out _), "Routed unit must not re-anchor on save/resume");
        }

        [Test]
        public void MaxMoraleZero_UnitIsImmuneToMoraleDamage()
        {
            var player = new BoardState(TestBoards.Layout);
            Place(player, TerrorRifle(), new GridCoord(8, 4), "player_terror");

            var enemy = new BoardState(TestBoards.Layout);
            Place(enemy, MoraleTarget(maxMorale: 0, maxHp: 40), new GridCoord(8, 4), "enemy_immune");

            var run = TickCombatRun.Start(player, enemy, seed: 42);
            var result = RunToCompletion(run);

            Assert.IsFalse(run.Log.Events.Any(e => e.ActionType == "morale_damage"));
            Assert.IsFalse(run.Log.Events.Any(e => e.ActionType == "rout"));
            Assert.IsTrue(run.PlayerWon, "Immune unit fights until killed");
            Assert.AreEqual(1, result.EnemyKilled);
            Assert.AreEqual(0, result.EnemyRouted);
        }

        [Test]
        public void ZeroTerrorContent_ProducesNoMoraleEvents()
        {
            var player = new BoardState(TestBoards.Layout);
            Place(player, TestPieces.MultiCellRearBlocker(), new GridCoord(0, 4), "player_blocker");
            Place(player, TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(3), "player_rifle_1");
            Place(player, TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(6), "player_rifle_2");

            var enemy = new BoardState(TestBoards.Layout);
            Place(enemy, TestPieces.MultiCellRearBlocker(), new GridCoord(0, 4), "enemy_blocker");
            Place(enemy, TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(3), "enemy_rifle_1");
            Place(enemy, TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(6), "enemy_rifle_2");

            var run = TickCombatRun.Start(player, enemy, seed: 99);
            var result = RunToCompletion(run);

            Assert.IsFalse(run.Log.Events.Any(e => e.ActionType == "morale_damage" || e.ActionType == "rout"));
            foreach (var combatant in run.PlayerCombatantsForTests.Concat(run.EnemyCombatantsForTests))
            {
                Assert.IsFalse(combatant.IsBroken);
                Assert.AreEqual(0, combatant.CurrentMorale);
            }

            Assert.AreEqual(0, result.EnemyRouted);
        }

        /// <summary>Static terror duel: player terror rifle vs a single morale-20 enemy that
        /// can't shoot back. Routs after exactly four terror hits (4 × 5 morale).</summary>
        private static TickCombatRun StartTerrorVsSingleMoraleTarget()
        {
            var player = new BoardState(TestBoards.Layout);
            Place(player, TerrorRifle(), new GridCoord(8, 4), "player_terror");

            var enemy = new BoardState(TestBoards.Layout);
            Place(enemy, MoraleTarget(maxMorale: 20), new GridCoord(8, 4), "enemy_mor");

            return TickCombatRun.Start(player, enemy, seed: 42);
        }

        /// <summary>One-shot killer vs a pack: killing enemy_a_dies shocks the two allies one
        /// cell away (morale == DeathShockDamage, so one shock routs both) but not the ally
        /// four cells away.</summary>
        private static TickCombatRun StartKillerVsDeathShockPack()
        {
            var player = new BoardState(TestBoards.Layout);
            Place(player, TerrorRifle(baseDamage: 200, terrorDamage: 0), new GridCoord(8, 4), "player_killer");

            var enemy = new BoardState(TestBoards.Layout);
            Place(enemy, MoraleTarget(maxMorale: 50, maxHp: 50), new GridCoord(8, 4), "enemy_a_dies");
            Place(enemy, MoraleTarget(maxMorale: MoraleRules.DeathShockDamage, maxHp: 400), new GridCoord(8, 5), "enemy_b_near");
            Place(enemy, MoraleTarget(maxMorale: MoraleRules.DeathShockDamage, maxHp: 400), new GridCoord(8, 3), "enemy_c_near");
            Place(enemy, MoraleTarget(maxMorale: 50, maxHp: 400), new GridCoord(8, 8), "enemy_d_far");

            return TickCombatRun.Start(player, enemy, seed: 42);
        }

        private static PieceDefinition TerrorRifle(int baseDamage = 2, int terrorDamage = 5) => TestPieces.With(
            TestPieces.RifleSquad(),
            baseDamage: baseDamage,
            maxHp: 500,
            attackRange: AttackRangeTier.Long,
            movementSpeed: 0,
            accuracyOverride: 100,
            terrorDamage: terrorDamage);

        private static PieceDefinition MoraleTarget(
            int maxMorale,
            int maxHp = 500,
            int baseDamage = 0,
            AttackRangeTier attackRange = AttackRangeTier.Medium) => TestPieces.With(
            TestPieces.RifleSquad(),
            baseDamage: baseDamage,
            maxHp: maxHp,
            attackRange: attackRange,
            movementSpeed: 0,
            accuracyOverride: 100,
            maxMorale: maxMorale);

        private static void Place(BoardState board, PieceDefinition piece, GridCoord anchor, string instanceId) =>
            Assert.IsTrue(
                board.TryPlace(piece, anchor, instanceId).Success,
                $"Failed to place {instanceId} at ({anchor.X},{anchor.Y})");

        private static CombatAdvanceResult RunToCompletion(TickCombatRun run)
        {
            var result = run.Continue(System.Array.Empty<PhaseCommand>());
            while (result.Status == CombatAdvanceStatus.AwaitingCommand)
                result = run.Continue(System.Array.Empty<PhaseCommand>());
            return result;
        }
    }
}
