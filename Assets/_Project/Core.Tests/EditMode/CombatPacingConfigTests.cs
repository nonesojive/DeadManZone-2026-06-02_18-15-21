using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatPacingConfigTests
    {
        [Test]
        public void PauseThresholdsAndGasTiming_MatchSpec()
        {
            Assert.AreEqual(2, CombatPacingConfig.PauseThresholds.Length);
            Assert.AreEqual(0.75f, CombatPacingConfig.PauseThresholds[0], 0.0001f);
            Assert.AreEqual(0.30f, CombatPacingConfig.PauseThresholds[1], 0.0001f);
            Assert.AreEqual(300, CombatPacingConfig.GasStartTick);
            Assert.AreEqual(10_000, CombatPacingConfig.MaxFightTicks);
            Assert.AreEqual(10, CombatPacingConfig.TicksPerSecond);
        }
    }
}
