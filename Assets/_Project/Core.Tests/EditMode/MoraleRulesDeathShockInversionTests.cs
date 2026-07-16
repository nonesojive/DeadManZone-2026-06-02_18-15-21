using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>2026-07-15 faction-roster-v1 §2.9/§4 Ashen death-shock inversion: "an Ashen
    /// death GRANTS morale to allies within 2 cells instead of draining it." Keyed off
    /// PieceDefinition.FactionId directly (MoraleRules.IsDeathShockInverted) — the smaller
    /// change vs. a new per-piece flag for a whole-faction passive.</summary>
    public sealed class MoraleRulesDeathShockInversionTests
    {
        [Test]
        public void IsDeathShockInverted_AshenCovenant_ReturnsTrue()
        {
            Assert.IsTrue(MoraleRules.IsDeathShockInverted(FactionIds.AshenCovenant));
        }

        [Test]
        public void IsDeathShockInverted_OtherFaction_ReturnsFalse()
        {
            Assert.IsFalse(MoraleRules.IsDeathShockInverted(FactionIds.IronmarchUnion));
        }

        [Test]
        public void IsDeathShockInverted_Null_ReturnsFalse()
        {
            Assert.IsFalse(MoraleRules.IsDeathShockInverted(null));
        }

        private static PieceDefinition AshenFragile() => new()
        {
            Id = "zealot_mob",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 5,
            MaxMorale = 40,
            BaseDamage = 0,
            FactionId = FactionIds.AshenCovenant
        };

        private static PieceDefinition AshenAlly() => new()
        {
            Id = "ash_acolyte",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 200,
            MaxMorale = 40,
            BaseDamage = 0,
            FactionId = FactionIds.AshenCovenant
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
        public void AshenDeath_GrantsMoraleToNearbyAllies_InsteadOfDraining()
        {
            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(Striker(), TestBoards.FrontLineAnchor(), "enemy_striker");

            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(AshenFragile(), TestBoards.FrontLineAnchor(), "player_fragile");
            player.TryPlace(AshenAlly(), new GridCoord(TestBoards.FrontLineAnchor().X, TestBoards.FrontLineAnchor().Y + 1), "player_ally");

            var run = TickCombatRun.Start(player, enemy, seed: 1);
            var ally = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "player_ally");
            ally.CurrentMorale = 20;

            run.Continue(System.Array.Empty<PhaseCommand>());

            Assert.IsTrue(run.Log.Events.Any(e => e.ActionType == "destroyed" && e.ActorId == "player_fragile"));
            Assert.IsTrue(
                run.Log.Events.Any(e => e.ActionType == "morale_gain" && e.ActorId == "player_fragile" && e.TargetId == "player_ally"),
                "an Ashen death must GRANT morale to nearby allies, not damage them");
            Assert.IsFalse(
                run.Log.Events.Any(e => e.ActionType == "morale_damage" && e.ActorId == "player_fragile"),
                "inversion must fully replace the drain — no morale_damage from this death");
        }

        private static PieceDefinition NonAshenFragile() => new()
        {
            Id = "conscript",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 5,
            MaxMorale = 40,
            BaseDamage = 0,
            FactionId = FactionIds.IronmarchUnion
        };

        [Test]
        public void NonAshenDeath_StillDrainsMoraleAsBefore()
        {
            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(Striker(), TestBoards.FrontLineAnchor(), "enemy_striker");

            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(NonAshenFragile(), TestBoards.FrontLineAnchor(), "player_fragile");
            player.TryPlace(AshenAlly(), new GridCoord(TestBoards.FrontLineAnchor().X, TestBoards.FrontLineAnchor().Y + 1), "player_ally");

            var run = TickCombatRun.Start(player, enemy, seed: 1);

            run.Continue(System.Array.Empty<PhaseCommand>());

            Assert.IsTrue(run.Log.Events.Any(e => e.ActionType == "destroyed" && e.ActorId == "player_fragile"));
            Assert.IsTrue(
                run.Log.Events.Any(e => e.ActionType == "morale_damage" && e.ActorId == "player_fragile" && e.TargetId == "player_ally"),
                "a non-Ashen death must keep draining morale as before (ADR-0005)");
            Assert.IsFalse(run.Log.Events.Any(e => e.ActionType == "morale_gain" && e.ActorId == "player_fragile"));
        }
    }
}
