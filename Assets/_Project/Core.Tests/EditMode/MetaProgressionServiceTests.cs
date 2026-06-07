using DeadManZone.Core.Meta;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class MetaProgressionServiceTests
    {
        [SetUp]
        public void SetUp() => MetaProgressionService.ResetCache();

        [Test]
        public void TryUnlockAchievement_AddsToSave()
        {
            Assert.IsTrue(MetaProgressionService.TryUnlockAchievement(AchievementIds.ClearGauntlet));
            Assert.IsFalse(MetaProgressionService.TryUnlockAchievement(AchievementIds.ClearGauntlet));
            var data = MetaProgressionService.Load();
            Assert.IsTrue(data.UnlockedAchievements.Contains(AchievementIds.ClearGauntlet));
        }

        [Test]
        public void IronVanguard_UnlockedByDefault()
        {
            Assert.IsTrue(MetaProgressionService.IsFactionUnlocked("iron_vanguard"));
        }
    }
}
