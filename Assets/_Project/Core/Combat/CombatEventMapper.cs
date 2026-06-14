using System.Collections.Generic;
using DeadManZone.Core.Run;

namespace DeadManZone.Core.Combat
{
    public static class CombatEventMapper
    {
        public static CombatEvent ToCombatEvent(CombatEventRecord record)
        {
            if (record == null)
                return null;

            return new CombatEvent
            {
                Segment = record.Segment,
                Tick = record.Tick,
                ActorId = record.ActorId,
                ActionType = record.ActionType,
                TargetId = record.TargetId,
                Value = record.Value
            };
        }

        public static IEnumerable<CombatEvent> FromRecords(IReadOnlyList<CombatEventRecord> records)
        {
            if (records == null)
                yield break;

            foreach (var record in records)
            {
                var combatEvent = ToCombatEvent(record);
                if (combatEvent != null)
                    yield return combatEvent;
            }
        }
    }
}
