using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>2026-07-15 faction-roster-v1 §1.8 Suppression tentpole (Crimson), wired end to
    /// end through TickCombatRun.ResolveAttacks/TryMoveSide. Pure-rules coverage lives in
    /// SuppressionRulesTests.cs.</summary>
    public sealed class TickCombatRunSuppressionTests
    {
        private static PieceDefinition SuppressingAttacker() => new()
        {
            Id = "suppression_team",
            DisplayName = "Suppression Team",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 60,
            BaseDamage = 3,
            CooldownTicks = 2,
            AccuracyOverride = 100,
            AttackRange = AttackRangeTier.Long,
            AppliesSuppressionOnHit = true
        };

        private static PieceDefinition DurableTarget() => new()
        {
            Id = "durable_target",
            DisplayName = "Durable Target",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 500,
            MaxMorale = 500,
            BaseDamage = 0
        };

        [Test]
        public void AppliesSuppressionOnHit_NonLethalHit_LogsSuppressAndSetsDuration()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(SuppressingAttacker(), TestBoards.FrontLineAnchor(), "player_suppressor");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(DurableTarget(), TestBoards.FrontLineAnchor(), "enemy_durable");

            var run = TickCombatRun.Start(player, enemy, seed: 5, authority: 0);
            run.Continue(System.Array.Empty<PhaseCommand>());

            Assert.IsTrue(
                run.Log.Events.Any(e => e.ActionType == "suppress"
                    && e.ActorId == "player_suppressor"
                    && e.TargetId == "enemy_durable"
                    && e.Value == SuppressionRules.SuppressionDurationTicks),
                "an AppliesSuppressionOnHit attacker must log a suppress event with the full duration on a non-lethal hit");
        }

        [Test]
        public void WithoutSuppressionFlag_NeverLogsSuppressEvent()
        {
            var plainAttacker = TestPieces.With(TestPieces.RifleSquad(), baseDamage: 3, cooldownTicks: 2, accuracyOverride: 100);
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(plainAttacker, TestBoards.FrontLineAnchor(), "player_plain");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(DurableTarget(), TestBoards.FrontLineAnchor(), "enemy_durable");

            var run = TickCombatRun.Start(player, enemy, seed: 5, authority: 0);
            run.Continue(System.Array.Empty<PhaseCommand>());

            Assert.IsFalse(run.Log.Events.Any(e => e.ActionType == "suppress"));
        }

        [Test]
        public void SuppressedTarget_KilledOnLethalHit_DoesNotLogSuppress()
        {
            var lethalAttacker = TestPieces.With(TestPieces.RifleSquad(), baseDamage: 999, cooldownTicks: 1, accuracyOverride: 100);
            // Reuse the shape/def but flag suppression via a fresh definition (With() doesn't
            // forward the new fields — see PieceDefinition's ledger fields note).
            var lethalSuppressor = new PieceDefinition
            {
                Id = lethalAttacker.Id,
                DisplayName = lethalAttacker.DisplayName,
                Category = lethalAttacker.Category,
                Shape = lethalAttacker.Shape,
                Tags = lethalAttacker.Tags,
                MaxHp = lethalAttacker.MaxHp,
                BaseDamage = lethalAttacker.BaseDamage,
                CooldownTicks = lethalAttacker.CooldownTicks,
                AccuracyOverride = lethalAttacker.AccuracyOverride,
                AppliesSuppressionOnHit = true
            };

            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(lethalSuppressor, TestBoards.FrontLineAnchor(), "player_lethal");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(TestPieces.WeakConscript(), TestBoards.FrontLineAnchor(), "enemy_weak");

            var run = TickCombatRun.Start(player, enemy, seed: 5, authority: 0);
            run.Continue(System.Array.Empty<PhaseCommand>());

            Assert.IsTrue(run.Log.Events.Any(e => e.ActionType == "destroyed" && e.ActorId == "enemy_weak"));
            Assert.IsFalse(run.Log.Events.Any(e => e.ActionType == "suppress"),
                "a kill shot must not also suppress a target that's already dead");
        }
    }
}
