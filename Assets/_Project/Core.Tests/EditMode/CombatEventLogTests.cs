using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public class CombatEventLogTests
    {
        [Test]
        public void EventLog_Append_AddsEvent()
        {
            var log = new CombatEventLog();
            log.Append(CombatPhase.Deployment, 0, "a1", "move", null, 0);
            Assert.AreEqual(1, log.Events.Count);
            Assert.AreEqual("move", log.Events[0].ActionType);
        }
    }
}
