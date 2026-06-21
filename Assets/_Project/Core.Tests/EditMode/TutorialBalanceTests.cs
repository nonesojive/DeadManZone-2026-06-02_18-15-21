using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class TutorialBalanceTests
    {
        private ContentDatabase _database;

        [SetUp]
        public void SetUp() => _database = ContentDatabase.Load();

        [Test]
        public void Fight1_ReferenceBoard_SurvivedWithoutLossDuringGrind_On90PercentOfSeeds()
        {
            AssertReachRate(
                1,
                TutorialBalanceFixtures.MeasureSurvivalRate(1, _database),
                "survival without grind loss");
        }

        [Test]
        public void Fight2_ReferenceBoard_SurvivedWithoutLossDuringGrind_On90PercentOfSeeds()
        {
            AssertReachRate(
                2,
                TutorialBalanceFixtures.MeasureSurvivalRate(2, _database),
                "survival without grind loss");
        }

        [Test]
        public void Fight3_ReferenceBoard_SurvivedWithoutLossDuringGrind_On90PercentOfSeeds()
        {
            AssertReachRate(
                3,
                TutorialBalanceFixtures.MeasureSurvivalRate(3, _database),
                "survival without grind loss");
        }

        [Test]
        public void Fight1_ReferenceBoard_ReachesPauseTwo_On90PercentOfSeeds()
        {
            AssertReachRate(
                1,
                TutorialBalanceFixtures.MeasurePauseTwoReachRate(1, _database),
                "pause #2");
        }

        [Test]
        public void Fight2_ReferenceBoard_ReachesPauseTwo_On90PercentOfSeeds()
        {
            AssertReachRate(
                2,
                TutorialBalanceFixtures.MeasurePauseTwoReachRate(2, _database),
                "pause #2");
        }

        [Test]
        public void Fight3_ReferenceBoard_ReachesPauseTwo_On90PercentOfSeeds()
        {
            AssertReachRate(
                3,
                TutorialBalanceFixtures.MeasurePauseTwoReachRate(3, _database),
                "pause #2");
        }

        private static void AssertReachRate(int fight, float rate, string metric)
        {
            float required = metric == "pause #2"
                ? fight switch
                {
                    1 => TutorialBalanceFixtures.MinFight1PauseTwoReachRate,
                    2 => TutorialBalanceFixtures.MinFight2PauseTwoReachRate,
                    _ => TutorialBalanceFixtures.MinReachRate
                }
                : TutorialBalanceFixtures.MinReachRate;

            Assert.GreaterOrEqual(
                rate,
                required,
                $"Fight {fight} {metric} rate was {rate:P0} (need {required:P0}).");
        }
    }
}
