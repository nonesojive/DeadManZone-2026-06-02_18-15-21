using DeadManZone.Core.Board;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class PieceCombatRulesTests
    {
        [Test]
        public void ParticipatesInCombat_UnitAndHybridAlwaysTrue()
        {
            Assert.IsTrue(PieceCombatRules.ParticipatesInCombat(new PieceDefinition { Category = PieceCategory.Unit }));
            Assert.IsTrue(PieceCombatRules.ParticipatesInCombat(new PieceDefinition { Category = PieceCategory.Hybrid }));
        }

        [Test]
        public void ParticipatesInCombat_BuildingDependsOnDamage()
        {
            Assert.IsTrue(PieceCombatRules.ParticipatesInCombat(new PieceDefinition
            {
                Category = PieceCategory.Building,
                BaseDamage = 2
            }));

            Assert.IsFalse(PieceCombatRules.ParticipatesInCombat(new PieceDefinition
            {
                Category = PieceCategory.Building,
                BaseDamage = 0
            }));
        }

        [Test]
        public void IsDeprioritizedTarget_NonDamagingBuildings()
        {
            Assert.IsTrue(PieceCombatRules.IsDeprioritizedTarget(new PieceDefinition
            {
                Category = PieceCategory.Building,
                BaseDamage = 0
            }));
        }
    }
}
