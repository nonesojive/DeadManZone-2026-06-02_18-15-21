using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>2026-07-15 faction-roster-v1 §4 (🟡 in-combat healing): "the sim has no
    /// HP-restoration path today." Wired end to end through TickCombatRun.ApplyHealPulses.
    /// Pure-rules coverage lives in HealPulseRulesTests.cs.</summary>
    public sealed class TickCombatRunHealPulseTests
    {
        private static PieceDefinition Healer() => new()
        {
            Id = "mercy_sister",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 20,
            HealPulseAmount = 8,
            HealPulseRadius = 3,
            HealPulseIntervalTicks = 5,
            // Stationary — stays put next to the wounded ally in the support zone so the
            // radius check stays satisfied for the whole (short) fight, regardless of how the
            // unrelated front-line duel resolves.
            MovementSpeed = 0
        };

        private static PieceDefinition WoundedAlly() => new()
        {
            Id = "wounded_ally",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 100,
            BaseDamage = 0,
            MovementSpeed = 0
        };

        // Small and killable — the fight must resolve well before tick 300 (anti-stall gas),
        // otherwise thousands of un-fought ticks of gas attrition would confound the HP
        // assertions below. A quick, deterministic win keeps this test fast and stable.
        private static PieceDefinition FragileEnemy() => new()
        {
            Id = "fragile",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 50,
            BaseDamage = 0
        };

        private static PieceDefinition Striker() => new()
        {
            Id = "striker",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 50,
            BaseDamage = 999,
            CooldownTicks = 1,
            AccuracyOverride = 100,
            AttackRange = AttackRangeTier.Long
        };

        [Test]
        public void HealPulse_RestoresWoundedAllyHp_CappedAtMaxHp()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(Healer(), TestBoards.SupportLineAnchor(1, 0), "healer_1");
            player.TryPlace(WoundedAlly(), TestBoards.SupportLineAnchor(2, 0), "wounded_1");
            player.TryPlace(Striker(), TestBoards.FrontLineAnchor(), "striker_1");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(FragileEnemy(), TestBoards.FrontLineAnchor(), "enemy_fragile");

            var run = TickCombatRun.Start(player, enemy, seed: 1);
            var wounded = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "wounded_1");
            wounded.CurrentHp = 95;

            var result = run.Continue(System.Array.Empty<PhaseCommand>());
            Assert.AreEqual(CombatAdvanceStatus.Completed, result.Status,
                "the striker must end the fight quickly, well before anti-stall gas at tick 300");

            Assert.IsTrue(
                run.Log.Events.Any(e => e.ActionType == "heal" && e.ActorId == "healer_1" && e.TargetId == "wounded_1"),
                "the healer must pulse HP to the wounded ally within radius");
            // wounded.MaxHp is the stored durability-scaled fight max (army-size
            // DurabilityScaleFor; 4 spawned units here, so BaseDurabilityScale) — the heal
            // clamp caps there, not at raw Definition.MaxHp.
            Assert.LessOrEqual(wounded.CurrentHp, wounded.MaxHp, "heal must never overheal past MaxHp");
            Assert.Greater(wounded.CurrentHp, 95, "at least one pulse must have landed by the time the fight resolves");
        }

        [Test]
        public void HealPulse_FullHpAlly_NeverLogsHealEvent()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(Healer(), TestBoards.SupportLineAnchor(1, 0), "healer_1");
            player.TryPlace(WoundedAlly(), TestBoards.SupportLineAnchor(2, 0), "full_hp_1");
            player.TryPlace(Striker(), TestBoards.FrontLineAnchor(), "striker_1");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(FragileEnemy(), TestBoards.FrontLineAnchor(), "enemy_fragile");

            var run = TickCombatRun.Start(player, enemy, seed: 1);
            run.Continue(System.Array.Empty<PhaseCommand>());

            Assert.IsFalse(run.Log.Events.Any(e => e.ActionType == "heal"),
                "a full-HP ally must never receive a heal pulse");
        }
    }
}
