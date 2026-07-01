using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
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
        public void IsDeprioritizedTarget_NonDamagingPieces()
        {
            Assert.IsTrue(PieceCombatRules.IsDeprioritizedTarget(new PieceDefinition
            {
                Category = PieceCategory.Building,
                BaseDamage = 0
            }));

            Assert.IsTrue(PieceCombatRules.IsDeprioritizedTarget(new PieceDefinition
            {
                CombatRole = GameTagIds.Utility,
                BaseDamage = 0,
                Category = PieceCategory.Unit
            }));

            Assert.IsFalse(PieceCombatRules.IsDeprioritizedTarget(new PieceDefinition
            {
                CombatRole = GameTagIds.Utility,
                BaseDamage = 3,
                Category = PieceCategory.Unit
            }));
        }
    }
}
