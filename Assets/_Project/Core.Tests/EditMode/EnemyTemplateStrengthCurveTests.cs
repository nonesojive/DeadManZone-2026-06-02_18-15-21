using DeadManZone.Core;
using DeadManZone.Core.Combat;
using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class EnemyTemplateStrengthCurveTests
    {
        private ContentDatabase _database;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _database = ContentDatabase.Load();

        [Test]
        public void AllEnemyTemplates_HavePositiveEffectiveStrength()
        {
            var faction = _database.GetFaction(FactionIds.IronVanguard);
            var registry = _database.BuildRegistry();

            for (int fight = 1; fight <= 10; fight++)
            {
                var template = _database.GetEnemyTemplate(fight);
                var board = template.BuildBoard(faction, registry);
                var snapshot = ArmyStrengthCalculator.Evaluate(board);
                Assert.Greater(snapshot.EffectiveTotal, 0, $"Fight {fight} should have combat strength.");
            }
        }

        [Test]
        public void EnemyStrength_GrowsRoughlyWithFightIndex()
        {
            var faction = _database.GetFaction(FactionIds.IronVanguard);
            var registry = _database.BuildRegistry();

            int fight1 = ArmyStrengthCalculator.Evaluate(
                _database.GetEnemyTemplate(1).BuildBoard(faction, registry)).EffectiveTotal;
            int fight10 = ArmyStrengthCalculator.Evaluate(
                _database.GetEnemyTemplate(10).BuildBoard(faction, registry)).EffectiveTotal;

            Assert.Greater(fight10, fight1, "Late gauntlet fights should rate stronger than fight 1.");
        }

        [Test]
        public void ReferencePlayerBoard_WithinSoftBandOfMidFightEnemy()
        {
            var player = TutorialBalanceFixtures.BuildReferencePlayerBoard(_database, fightIndex: 5);
            var faction = _database.GetFaction(FactionIds.IronVanguard);
            var registry = _database.BuildRegistry();
            var enemy = ArmyStrengthCalculator.Evaluate(
                _database.GetEnemyTemplate(5).BuildBoard(faction, registry));

            var playerStrength = ArmyStrengthCalculator.Evaluate(player);
            float ratio = playerStrength.EffectiveTotal / (float)System.Math.Max(1, enemy.EffectiveTotal);

            Assert.Greater(ratio, 0.4f, "Reference board should not be trivial vs fight 5.");
            Assert.Less(ratio, 2.5f, "Reference board should not dominate fight 5 on paper.");
        }
    }
}
