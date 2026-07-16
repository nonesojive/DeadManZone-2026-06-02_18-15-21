using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>2026-07-15 faction-roster-v1 §2.1 Trench Works. Mirrors
    /// CombatStealthRulesTests.cs — the analogous precedent for this kind of pure-rules seam.</summary>
    public sealed class MovementSlowRulesTests
    {
        private static PieceDefinition TrenchWorks() => new()
        {
            Id = "trench_works",
            DisplayName = "Trench Works",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 140,
            AbilityTags = new[] { GameTagIds.MovementSlowAura }
        };

        private static CombatantState MakeCombatant(string id, PieceDefinition definition, GridCoord position, int hp = 100) => new()
        {
            InstanceId = id,
            Side = CombatSide.Enemy,
            Definition = definition,
            AnchorPosition = position,
            CurrentHp = hp
        };

        [Test]
        public void IsSlowed_AdjacentToMovementSlowAuraPiece_ReturnsTrue()
        {
            var mover = MakeCombatant("mover", TestPieces.RifleSquad(), new GridCoord(5, 5));
            var trench = MakeCombatant("trench_1", TrenchWorks(), new GridCoord(5, 6));

            Assert.IsTrue(MovementSlowRules.IsSlowed(mover, new List<CombatantState> { trench }));
        }

        [Test]
        public void IsSlowed_NotAdjacent_ReturnsFalse()
        {
            var mover = MakeCombatant("mover", TestPieces.RifleSquad(), new GridCoord(5, 5));
            var trench = MakeCombatant("trench_1", TrenchWorks(), new GridCoord(5, 8));

            Assert.IsFalse(MovementSlowRules.IsSlowed(mover, new List<CombatantState> { trench }));
        }

        [Test]
        public void IsSlowed_AdjacentButNotAuraPiece_ReturnsFalse()
        {
            var mover = MakeCombatant("mover", TestPieces.RifleSquad(), new GridCoord(5, 5));
            var plainRifle = MakeCombatant("enemy_rifle", TestPieces.RifleSquad(), new GridCoord(5, 6));

            Assert.IsFalse(MovementSlowRules.IsSlowed(mover, new List<CombatantState> { plainRifle }));
        }

        [Test]
        public void IsSlowed_AuraPieceRouted_ReturnsFalse()
        {
            var mover = MakeCombatant("mover", TestPieces.RifleSquad(), new GridCoord(5, 5));
            var trench = MakeCombatant("trench_1", TrenchWorks(), new GridCoord(5, 6));
            trench.IsBroken = true;

            Assert.IsFalse(MovementSlowRules.IsSlowed(mover, new List<CombatantState> { trench }), "routed/inactive pieces cannot project the aura");
        }

        [Test]
        public void ApplyMovementSlow_WhenSlowed_HalvesCharge()
        {
            Assert.AreEqual(50, MovementSlowRules.ApplyMovementSlow(100, isSlowed: true));
        }

        [Test]
        public void ApplyMovementSlow_WhenNotSlowed_Unchanged()
        {
            Assert.AreEqual(100, MovementSlowRules.ApplyMovementSlow(100, isSlowed: false));
        }
    }
}
