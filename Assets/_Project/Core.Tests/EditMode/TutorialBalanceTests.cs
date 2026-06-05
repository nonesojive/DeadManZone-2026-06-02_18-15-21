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
        public void Fight1_ReferenceBoard_ReachesPauseTwoOn90PercentOfSeeds()
        {
            float rate = TutorialBalanceFixtures.MeasurePauseTwoReachRate(1, _database);
            Assert.GreaterOrEqual(rate, TutorialBalanceFixtures.MinPauseTwoReachRate,
                $"Fight 1 pause #2 reach rate was {rate:P0}");
        }

        [Test]
        public void Fight2_ReferenceBoard_ReachesPauseTwoOn90PercentOfSeeds()
        {
            float rate = TutorialBalanceFixtures.MeasurePauseTwoReachRate(2, _database);
            Assert.GreaterOrEqual(rate, TutorialBalanceFixtures.MinPauseTwoReachRate,
                $"Fight 2 pause #2 reach rate was {rate:P0}");
        }

        [Test]
        public void Fight3_ReferenceBoard_ReachesPauseTwoOn90PercentOfSeeds()
        {
            float rate = TutorialBalanceFixtures.MeasurePauseTwoReachRate(3, _database);
            Assert.GreaterOrEqual(rate, TutorialBalanceFixtures.MinPauseTwoReachRate,
                $"Fight 3 pause #2 reach rate was {rate:P0}");
        }
    }
}
