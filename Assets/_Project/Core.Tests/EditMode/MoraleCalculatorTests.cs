using DeadManZone.Core.Run;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class MoraleCalculatorTests
    {
        [Test]
        public void LossMorale_HigherOnLaterFightsAndWorseSeverity()
        {
            int early = MoraleCalculator.ComputeLoss(fightIndex: 2, combatantsLost: 1, totalCombatants: 5, hqDamage: false);
            int late = MoraleCalculator.ComputeLoss(fightIndex: 9, combatantsLost: 4, totalCombatants: 5, hqDamage: true);
            Assert.Greater(late, early);
        }

        [Test]
        public void ComputeLoss_UsesFullSeverityWhenNoCombatants()
        {
            int loss = MoraleCalculator.ComputeLoss(fightIndex: 1, combatantsLost: 0, totalCombatants: 0, hqDamage: false);
            Assert.AreEqual(6, loss);
        }
    }
}
