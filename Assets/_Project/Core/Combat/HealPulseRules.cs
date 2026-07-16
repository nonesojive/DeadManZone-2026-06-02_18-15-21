using System.Collections.Generic;
using System.Linq;

namespace DeadManZone.Core.Combat
{
    /// <summary>2026-07-15 faction-roster-v1 §4 (🟡 in-combat healing): "the sim has no
    /// HP-restoration path today." Adds one: a piece with HealPulseAmount/Radius/IntervalTicks
    /// restores HP to nearby active allies on a tick cadence, capped at MaxHp. Consumers (later
    /// content wave): Mercy Sister, Field Chirurgeon, Hospitaller-General. Mirrors
    /// MovementSlowRules — pure, testable, kept out of TickCombatRun's tick loop.</summary>
    public static class HealPulseRules
    {
        public static bool IsPulseTick(CombatantState healer, int globalTick)
        {
            int interval = healer?.Definition?.HealPulseIntervalTicks ?? 0;
            return interval > 0 && globalTick % interval == 0;
        }

        public static IReadOnlyList<CombatantState> GetHealTargets(
            CombatantState healer,
            IEnumerable<CombatantState> allies)
        {
            if (healer == null || allies == null)
                return System.Array.Empty<CombatantState>();

            return allies
                .Where(a => a != null
                    && a.InstanceId != healer.InstanceId
                    && a.IsActive
                    && a.Definition.MaxHp > 0
                    && a.CurrentHp < a.Definition.MaxHp
                    && CombatRange.Distance(healer.AnchorPosition, a.AnchorPosition) <= healer.Definition.HealPulseRadius)
                .ToList();
        }

        /// <summary>Amount actually applied — the pulse amount, capped so it never overheals.</summary>
        public static int GetHealAmount(CombatantState healer, CombatantState target)
        {
            if (healer == null || target == null)
                return 0;

            int missing = target.Definition.MaxHp - target.CurrentHp;
            return System.Math.Max(0, System.Math.Min(healer.Definition.HealPulseAmount, missing));
        }
    }
}
