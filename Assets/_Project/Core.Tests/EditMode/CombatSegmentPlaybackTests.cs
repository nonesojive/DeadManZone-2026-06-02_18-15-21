using System.Collections.Generic;
using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatSegmentPlaybackTests
    {
        [Test]
        public void ResolveLastTick_WhenFightEndsInSegment_UsesLastEventTick()
        {
            var events = new List<CombatEvent>
            {
                new() { Segment = 1, Tick = 12, ActorId = "a", ActionType = "damage", TargetId = "b", Value = 5 },
                new() { Segment = 1, Tick = 40, ActorId = "combat", ActionType = "fight_end", TargetId = "victory", Value = 0 }
            };

            int lastTick = CombatSegmentPlayback.ResolveLastTick(1, events);

            Assert.AreEqual(40, lastTick);
        }

        [Test]
        public void ResolveLastTick_IgnoresEventsFromOtherSegments()
        {
            var events = new List<CombatEvent>
            {
                new() { Segment = 0, Tick = 5, ActorId = "a", ActionType = "damage", TargetId = "b", Value = 2 },
                new() { Segment = 1, Tick = 12, ActorId = "a", ActionType = "damage", TargetId = "b", Value = 2 }
            };

            Assert.AreEqual(5, CombatSegmentPlayback.ResolveLastTick(0, events));
            Assert.AreEqual(12, CombatSegmentPlayback.ResolveLastTick(1, events));
        }

        [Test]
        public void ResolveLastTick_WhenSegmentEmpty_ReturnsNegativeOne()
        {
            var events = new List<CombatEvent>
            {
                new() { Segment = 0, Tick = 5, ActorId = "a", ActionType = "damage", TargetId = "b", Value = 2 }
            };

            Assert.AreEqual(-1, CombatSegmentPlayback.ResolveLastTick(1, events));
        }
    }
}
