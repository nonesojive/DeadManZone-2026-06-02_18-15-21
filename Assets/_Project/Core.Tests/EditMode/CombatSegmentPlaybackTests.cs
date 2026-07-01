using System.Collections.Generic;
using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatSegmentPlaybackTests
    {
        [Test]
        public void ResolveFirstTick_EmptySegment_ReturnsNegativeOne()
        {
            var events = new List<CombatEvent>
            {
                new CombatEvent { Segment = 0, Tick = 10, ActionType = "move" }
            };

            Assert.AreEqual(-1, CombatSegmentPlayback.ResolveFirstTick(1, events));
        }

        [Test]
        public void GroupEventsByTick_SkipsTicksWithoutPresentationEvents()
        {
            var events = new List<CombatEvent>
            {
                new CombatEvent { Segment = 1, Tick = 100, ActionType = "move" },
                new CombatEvent { Segment = 1, Tick = 130, ActionType = "damage" }
            };

            var grouped = CombatSegmentPlayback.GroupEventsByTick(1, events);

            Assert.AreEqual(2, grouped.Count);
            Assert.IsFalse(grouped.ContainsKey(101));
            Assert.IsFalse(grouped.ContainsKey(129));
        }
    }
}
