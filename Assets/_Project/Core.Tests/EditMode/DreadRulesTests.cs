using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>Pure Dread-clock rules, boss-order derivation, and twist application —
    /// no ContentDatabase required.</summary>
    public sealed class DreadRulesTests
    {
        [Test]
        public void FightEquivalent_PreservesFightIndexedCurve()
        {
            // N normal wins at DreadPerWin land exactly on old fight N+1.
            Assert.AreEqual(1, DreadRules.FightEquivalent(0));
            Assert.AreEqual(2, DreadRules.FightEquivalent(DreadRules.DreadPerWin));
            Assert.AreEqual(4, DreadRules.FightEquivalent(6));
            Assert.AreEqual(7, DreadRules.FightEquivalent(12));
            Assert.AreEqual(10, DreadRules.FightEquivalent(18));
            // M2 odd values (easy/hard options) floor onto the same clock.
            Assert.AreEqual(1, DreadRules.FightEquivalent(1));
            Assert.AreEqual(2, DreadRules.FightEquivalent(3));
        }

        [Test]
        public void NextThreshold_WalksTheAuthoredLadder()
        {
            Assert.AreEqual(6, DreadRules.NextThreshold(0));
            Assert.AreEqual(12, DreadRules.NextThreshold(1));
            Assert.AreEqual(18, DreadRules.NextThreshold(2));
        }

        [Test]
        public void IsBossPending_TriggersAtThreshold_NotBefore()
        {
            Assert.IsFalse(DreadRules.IsBossPending(0, 0));
            Assert.IsFalse(DreadRules.IsBossPending(5, 0));
            Assert.IsTrue(DreadRules.IsBossPending(6, 0));
            Assert.IsTrue(DreadRules.IsBossPending(7, 0));

            // Defeating a boss clears the pending state until the next threshold.
            Assert.IsFalse(DreadRules.IsBossPending(6, 1));
            Assert.IsFalse(DreadRules.IsBossPending(11, 1));
            Assert.IsTrue(DreadRules.IsBossPending(12, 1));
            Assert.IsTrue(DreadRules.IsBossPending(18, 2));

            // After the third boss the run is won — nothing is ever pending.
            Assert.IsFalse(DreadRules.IsBossPending(99, 3));
        }

        [Test]
        public void GetBossOrder_IsDeterministicPermutationPerSeed()
        {
            var allIds = BossRoster.All.Select(b => b.BossId).ToArray();
            var seeds = new[] { 1, 42, 1234, 24_680, 987_654, -5, 777, 31_337 };
            var distinctOrders = new System.Collections.Generic.HashSet<string>();

            foreach (int seed in seeds)
            {
                var order = BossRoster.GetBossOrder(seed);
                CollectionAssert.AreEqual(order, BossRoster.GetBossOrder(seed),
                    $"seed {seed} must reproduce the same order");
                CollectionAssert.AreEquivalent(allIds, order,
                    $"seed {seed} must be a permutation of all boss ids");
                distinctOrders.Add(string.Join(",", order));
            }

            Assert.GreaterOrEqual(distinctOrders.Count, 2,
                "boss order should differ across a handful of seeds");
        }

        [Test]
        public void IronDiscipline_GrantsEnemiesOnePermanentArmorStep()
        {
            var control = StartTwoEnemyFight(modifier: null);
            var twisted = StartTwoEnemyFight(TwistCatalog.Resolve(TwistCatalog.IronDiscipline));

            Assert.IsTrue(control.EnemyCombatantsForTests.All(e => e.ArmorBuffSteps == 0));
            Assert.IsTrue(twisted.EnemyCombatantsForTests.All(e => e.ArmorBuffSteps == 1));
            Assert.IsTrue(twisted.PlayerCombatantsForTests.All(p => p.ArmorBuffSteps == 0),
                "twist must not touch the player side");
        }

        [Test]
        public void EndlessMuster_RaisesAllEnemyHpByThirtyPercent()
        {
            var control = StartTwoEnemyFight(modifier: null);
            var twisted = StartTwoEnemyFight(TwistCatalog.Resolve(TwistCatalog.EndlessMuster));

            foreach (var enemy in twisted.EnemyCombatantsForTests)
            {
                var baseline = control.EnemyCombatantsForTests
                    .First(e => e.InstanceId == enemy.InstanceId);
                Assert.AreEqual(baseline.CurrentHp * 130 / 100, enemy.CurrentHp,
                    $"{enemy.InstanceId} should start with +30% HP");
            }
        }

        [Test]
        public void DeathlessCold_BoostsOnlyTheEnemyFrontRank()
        {
            var twisted = StartTwoEnemyFight(TwistCatalog.Resolve(TwistCatalog.DeathlessCold));
            var enemies = twisted.EnemyCombatantsForTests;
            int frontX = enemies.Min(e => e.AnchorPosition.X);

            var front = enemies.Where(e => e.AnchorPosition.X == frontX).ToList();
            var rear = enemies.Where(e => e.AnchorPosition.X != frontX).ToList();
            Assert.IsNotEmpty(front);
            Assert.IsNotEmpty(rear, "fixture must have a non-front enemy to prove scoping");
            // e.MaxHp = stored durability-scaled fight max; the twist multiplies scaled spawn HP.
            Assert.IsTrue(front.All(e => e.CurrentHp == e.MaxHp * 160 / 100));
            Assert.IsTrue(rear.All(e => e.CurrentHp == e.MaxHp));
        }

        [Test]
        public void TwistCatalog_ResolvesEveryAuthoredBossTwist()
        {
            foreach (var boss in BossRoster.All)
            {
                var modifier = TwistCatalog.Resolve(boss.TwistId);
                Assert.AreEqual(boss.TwistId, modifier.Id);
            }

            Assert.Throws<System.InvalidOperationException>(
                () => TwistCatalog.Resolve("no_such_twist"));
        }

        private static TickCombatRun StartTwoEnemyFight(ICombatRuleModifier modifier)
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(), "player_rifle");

            var enemy = new BoardState(TestBoards.Layout);
            // Two DISTINCT columns, both in the unit-legal zone — SupportLineAnchor(0) is
            // the Rear zone, which silently rejects Units (audit gotcha; the no-op left
            // this fixture with a single enemy and no rear rank to prove scoping against).
            Assert.IsTrue(
                enemy.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(3), "enemy_front").Success,
                "front enemy must place");
            Assert.IsTrue(
                enemy.TryPlace(TestPieces.RifleSquad(), new GridCoord(5, 3), "enemy_rear").Success,
                "rear enemy must place");

            return TickCombatRun.Start(
                player,
                enemy,
                seed: 42,
                modifiers: modifier == null ? null : new[] { modifier });
        }
    }
}
