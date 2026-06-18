using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatLogFormatterTests
    {
        [Test]
        public void Format_Graze_IncludesGrazeLabel()
        {
            var line = CombatLogFormatter.Format(new CombatEvent
            {
                Segment = 0,
                Tick = 1,
                ActorId = "a",
                ActionType = "graze",
                TargetId = "b",
                Value = 7
            });

            StringAssert.Contains("graze", line);
            StringAssert.Contains("7", line);
        }

        [Test]
        public void Format_Miss_ShowsMissed()
        {
            var line = CombatLogFormatter.Format(new CombatEvent
            {
                Segment = 0,
                Tick = 1,
                ActorId = "a",
                ActionType = "miss",
                TargetId = "b",
                Value = 0
            });

            StringAssert.Contains("miss", line.ToLowerInvariant());
        }
    }
}
