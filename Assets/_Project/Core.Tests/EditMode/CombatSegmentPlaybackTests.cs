using System.Collections.Generic;
using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatSegmentPlaybackTests
    {
        [Test]
        public void ResolveLastTick_WhenFightEndsInSegment_UsesLastEventTickNotBudget()
        {
            var events = new List<CombatEvent>
            {
                new() { Phase = CombatPhase.Grind, Tick = 12, ActorId = "a", ActionType = "damage", TargetId = "b", Value = 5 },
                new() { Phase = CombatPhase.Grind, Tick = 40, ActorId = "combat", ActionType = "fight_end", TargetId = "victory", Value = 0 }
            };

            int lastTick = CombatSegmentPlayback.ResolveLastTick(CombatPhase.Grind, events, segmentEndsFight: true);

            Assert.AreEqual(40, lastTick);
            Assert.Less(lastTick, CombatPacingConfig.MainFightTicks - 1);
        }

        [Test]
        public void ResolveLastTick_WhenPauseExpected_UsesFullSegmentBudget()
        {
            var events = new List<CombatEvent>
            {
                new() { Phase = CombatPhase.Grind, Tick = 5, ActorId = "a", ActionType = "damage", TargetId = "b", Value = 2 }
            };

            int lastTick = CombatSegmentPlayback.ResolveLastTick(CombatPhase.Grind, events, segmentEndsFight: false);

            Assert.AreEqual(CombatPacingConfig.MainFightTicks - 1, lastTick);
        }
    }
}
