using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatPacingConfigTests
    {
        [Test]
        public void OpeningAndMainFightTicks_MatchDemoSpec()
        {
            Assert.AreEqual(50, CombatPacingConfig.OpeningTicks);
            Assert.AreEqual(300, CombatPacingConfig.MainFightTicks);
            Assert.AreEqual(50, CombatPacingConfig.BriefPushTicks);
            Assert.AreEqual(10, CombatPacingConfig.TicksPerSecond);
        }
    }
}
