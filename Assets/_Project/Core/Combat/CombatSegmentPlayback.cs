using System.Collections.Generic;
using System.Linq;

namespace DeadManZone.Core.Combat
{
    /// <summary>Tick-paced segment replay helpers aligned with <see cref="CombatPacingConfig"/>.</summary>
    public static class CombatSegmentPlayback
    {
        public static float SecondsPerTick => 1f / CombatPacingConfig.TicksPerSecond;

        public static int GetTickBudget(CombatPhase phase) =>
            phase switch
            {
                CombatPhase.Deployment => CombatPacingConfig.OpeningTicks,
                CombatPhase.Grind => CombatPacingConfig.MainFightTicks,
                CombatPhase.FinalPush => CombatPacingConfig.BriefPushTicks,
                _ => 0
            };

        public static int ResolveLastTick(
            CombatPhase phase,
            IEnumerable<CombatEvent> events,
            bool segmentEndsFight = false)
        {
            int eventMax = events?
                .Where(e => e.Phase == phase)
                .Select(e => e.Tick)
                .DefaultIfEmpty(-1)
                .Max() ?? -1;

            if (segmentEndsFight || phase == CombatPhase.FinalPush)
                return eventMax < 0 ? 0 : eventMax;

            int budgetLast = GetTickBudget(phase) - 1;
            return eventMax < 0 ? budgetLast : System.Math.Max(eventMax, budgetLast);
        }

        public static bool SegmentContainsFightEnd(IEnumerable<CombatEvent> events, CombatPhase phase) =>
            events?.Any(e => e.Phase == phase && e.ActionType == "fight_end") == true;

        public static Dictionary<int, List<CombatEvent>> GroupEventsByTick(
            CombatPhase phase,
            IEnumerable<CombatEvent> events)
        {
            var grouped = new Dictionary<int, List<CombatEvent>>();
            if (events == null)
                return grouped;

            foreach (var combatEvent in events.Where(e => e.Phase == phase).OrderBy(e => e.Tick))
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
