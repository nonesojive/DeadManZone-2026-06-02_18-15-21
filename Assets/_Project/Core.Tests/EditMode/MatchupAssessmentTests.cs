using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class MatchupAssessmentTests
    {
        [Test]
        public void ResolveLabel_FavorableAtOrAboveThreshold()
        {
            Assert.AreEqual(MatchupLabel.Favorable, MatchupAssessment.ResolveLabel(1.15f));
            Assert.AreEqual(MatchupLabel.Favorable, MatchupAssessment.ResolveLabel(1.5f));
        }

        [Test]
        public void ResolveLabel_EvenInMiddleBand()
        {
            Assert.AreEqual(MatchupLabel.Even, MatchupAssessment.ResolveLabel(1.0f));
            Assert.AreEqual(MatchupLabel.Even, MatchupAssessment.ResolveLabel(0.85f));
            Assert.AreEqual(MatchupLabel.Even, MatchupAssessment.ResolveLabel(1.14f));
        }

        [Test]
        public void ResolveLabel_DangerousBelowThreshold()
        {
            Assert.AreEqual(MatchupLabel.Dangerous, MatchupAssessment.ResolveLabel(0.84f));
            Assert.AreEqual(MatchupLabel.Dangerous, MatchupAssessment.ResolveLabel(0.5f));
        }

        [Test]
        public void Compare_UsesEffectiveTotals()
        {
            var player = new ArmyStrengthSnapshot { BaseTotal = 100, EffectiveTotal = 120 };
            var enemy = new ArmyStrengthSnapshot { BaseTotal = 90, EffectiveTotal = 100 };

            var assessment = MatchupAssessment.Compare(player, enemy);
            Assert.AreEqual(1.2f, assessment.Ratio, 0.001f);
            Assert.AreEqual(MatchupLabel.Favorable, assessment.Label);
        }

        [Test]
        public void Compare_ZeroEnemyStrength_DefaultsEvenRatio()
        {
            var player = new ArmyStrengthSnapshot { BaseTotal = 100, EffectiveTotal = 100 };
            var enemy = default(ArmyStrengthSnapshot);

            var assessment = MatchupAssessment.Compare(player, enemy);
            Assert.AreEqual(1f, assessment.Ratio, 0.001f);
            Assert.AreEqual(MatchupLabel.Even, assessment.Label);
        }
    }
}
