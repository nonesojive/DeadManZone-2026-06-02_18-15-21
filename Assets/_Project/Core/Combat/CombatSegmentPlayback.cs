using System.Collections.Generic;
using System.Linq;

namespace DeadManZone.Core.Combat
{
    /// <summary>Tick-paced segment replay helpers (segment = playback chunk between pauses).</summary>
    public static class CombatSegmentPlayback
    {
        public static float SecondsPerTick => 1f / CombatPacingConfig.TicksPerSecond;

        /// <summary>First global tick of a segment's events, or -1 when the segment is empty.</summary>
        public static int ResolveFirstTick(int segment, IEnumerable<CombatEvent> events) =>
            events?
                .Where(e => e.Segment == segment)
                .Select(e => e.Tick)
                .DefaultIfEmpty(-1)
                .Min() ?? -1;

        /// <summary>Last global tick of a segment's events, or -1 when the segment is empty.</summary>
        public static int ResolveLastTick(int segment, IEnumerable<CombatEvent> events) =>
            events?
                .Where(e => e.Segment == segment)
                .Select(e => e.Tick)
                .DefaultIfEmpty(-1)
                .Max() ?? -1;

        public static bool SegmentContainsFightEnd(IEnumerable<CombatEvent> events, int segment) =>
            events?.Any(e => e.Segment == segment && e.ActionType == "fight_end") == true;

        public static Dictionary<int, List<CombatEvent>> GroupEventsByTick(
            int segment,
            IEnumerable<CombatEvent> events)
        {
            var grouped = new Dictionary<int, List<CombatEvent>>();
            if (events == null)
                return grouped;

            foreach (var combatEvent in events.Where(e => e.Segment == segment).OrderBy(e => e.Tick))
            {
                if (!grouped.TryGetValue(combatEvent.Tick, out var list))
                {
                    list = new List<CombatEvent>();
                    grouped[combatEvent.Tick] = list;
                }

                list.Add(combatEvent);
            }

            return grouped;
        }
    }
}
