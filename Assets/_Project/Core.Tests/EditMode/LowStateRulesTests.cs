using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>2026-07-15 faction-roster-v1 §2.9/§4 Ashen low-state triggers: "below 50% HP or
    /// morale → piece-defined bonuses activate", evaluated live, per-unit.</summary>
    public sealed class LowStateRulesTests
    {
        private static PieceDefinition Ashen(int lowStateDamageBonus = 5, int lowStateAttackSpeedSteps = 1) => new()
        {
            Id = "ash_acolyte",
            DisplayName = "Ash Acolyte",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 20,
            MaxMorale = 20,
            LowStateDamageBonus = lowStateDamageBonus,
            LowStateAttackSpeedSteps = lowStateAttackSpeedSteps
        };

        private static CombatantState MakeCombatant(int hp, int morale, PieceDefinition def = null) => new()
        {
            InstanceId = "unit",
            Side = CombatSide.Player,
            Definition = def ?? Ashen(),
            AnchorPosition = new GridCoord(0, 0),
            CurrentHp = hp,
            CurrentMorale = morale
        };

        [Test]
        public void IsLowState_ExactlyHalfHp_IsTrue()
        {
            Assert.IsTrue(LowStateRules.IsLowState(MakeCombatant(hp: 10, morale: 20)),
                "the universal threshold is <=50%, not <50%");
        }

        [Test]
        public void IsLowState_AboveHalfHpAndMorale_IsFalse()
        {
            Assert.IsFalse(LowStateRules.IsLowState(MakeCombatant(hp: 11, morale: 20)));
        }

        [Test]
        public void IsLowState_LowMoraleOnly_IsTrue()
        {
            Assert.IsTrue(LowStateRules.IsLowState(MakeCombatant(hp: 20, morale: 5)));
        }

        [Test]
        public void IsLowState_MoraleImmunePiece_OnlyChecksHp()
        {
            var moraleImmune = new PieceDefinition
            {
                Id = "structure",
                Category = PieceCategory.Building,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                MaxHp = 20,
                MaxMorale = 0,
                LowStateDamageBonus = 5
            };

            Assert.IsFalse(LowStateRules.IsLowState(MakeCombatant(hp: 20, morale: 0, def: moraleImmune)),
                "MaxMorale 0 pieces are morale-immune (CanBreak false) — 0 current morale must not read as low-state");
        }

        [Test]
        public void IsLowState_Dead_IsFalse()
        {
            var dead = MakeCombatant(hp: 0, morale: 0);
            Assert.IsFalse(LowStateRules.IsLowState(dead));
        }

        [Test]
        public void GetDamageBonus_WhenLowState_ReturnsDefinitionValue()
        {
            var unit = MakeCombatant(hp: 5, morale: 20);
            Assert.AreEqual(5, LowStateRules.GetDamageBonus(unit));
        }

        [Test]
        public void GetDamageBonus_WhenNotLowState_ReturnsZero()
        {
            var unit = MakeCombatant(hp: 20, morale: 20);
            Assert.AreEqual(0, LowStateRules.GetDamageBonus(unit));
        }

        [Test]
        public void GetAttackSpeedSteps_WhenLowState_ReturnsDefinitionValue()
        {
            var unit = MakeCombatant(hp: 5, morale: 20);
            Assert.AreEqual(1, LowStateRules.GetAttackSpeedSteps(unit));
        }
    }
}
