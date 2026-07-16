using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>2026-07-15 faction-roster-v1 §4 (🟡 in-combat healing): "the sim has no
    /// HP-restoration path today." Consumers (later wave): Mercy Sister, Field Chirurgeon,
    /// Hospitaller-General.</summary>
    public sealed class HealPulseRulesTests
    {
        private static PieceDefinition Healer(int amount = 10, int radius = 2, int interval = 20) => new()
        {
            Id = "mercy_sister",
            DisplayName = "Mercy Sister",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 20,
            HealPulseAmount = amount,
            HealPulseRadius = radius,
            HealPulseIntervalTicks = interval
        };

        private static PieceDefinition Ally() => new()
        {
            Id = "ally",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 100
        };

        private static CombatantState MakeCombatant(string id, PieceDefinition def, GridCoord pos, int hp) => new()
        {
            InstanceId = id,
            Side = CombatSide.Player,
            Definition = def,
            AnchorPosition = pos,
            CurrentHp = hp
        };

        [Test]
        public void IsPulseTick_OnCadence_ReturnsTrue()
        {
            var healer = MakeCombatant("healer", Healer(interval: 20), new GridCoord(0, 0), 20);
            Assert.IsTrue(HealPulseRules.IsPulseTick(healer, 40));
            Assert.IsTrue(HealPulseRules.IsPulseTick(healer, 0));
        }

        [Test]
        public void IsPulseTick_OffCadence_ReturnsFalse()
        {
            var healer = MakeCombatant("healer", Healer(interval: 20), new GridCoord(0, 0), 20);
            Assert.IsFalse(HealPulseRules.IsPulseTick(healer, 5));
        }

        [Test]
        public void IsPulseTick_ZeroInterval_NeverPulses()
        {
            var healer = MakeCombatant("healer", Healer(interval: 0), new GridCoord(0, 0), 20);
            Assert.IsFalse(HealPulseRules.IsPulseTick(healer, 0));
        }

        [Test]
        public void GetHealTargets_ExcludesFullHpAndSelfAndOutOfRadius()
        {
            var healer = MakeCombatant("healer", Healer(radius: 1), new GridCoord(5, 5), 20);
            var wounded = MakeCombatant("wounded", Ally(), new GridCoord(5, 6), 50);
            var fullHp = MakeCombatant("full", Ally(), new GridCoord(6, 5), 100);
            var farAway = MakeCombatant("far", Ally(), new GridCoord(9, 9), 10);

            var targets = HealPulseRules.GetHealTargets(healer, new List<CombatantState> { healer, wounded, fullHp, farAway });

            CollectionAssert.AreEquivalent(new[] { "wounded" }, new List<string> { targets[0].InstanceId });
        }

        [Test]
        public void GetHealAmount_CapsAtMissingHp()
        {
            var healer = MakeCombatant("healer", Healer(amount: 30), new GridCoord(0, 0), 20);
            var nearlyFull = MakeCombatant("nearly_full", Ally(), new GridCoord(0, 1), 95);

            Assert.AreEqual(5, HealPulseRules.GetHealAmount(healer, nearlyFull),
                "heal must never overheal past MaxHp");
        }

        [Test]
        public void GetHealAmount_WoundedEnough_ReturnsFullPulseAmount()
        {
            var healer = MakeCombatant("healer", Healer(amount: 10), new GridCoord(0, 0), 20);
            var wounded = MakeCombatant("wounded", Ally(), new GridCoord(0, 1), 50);

            Assert.AreEqual(10, HealPulseRules.GetHealAmount(healer, wounded));
        }
    }
}
