using System.Collections.Generic;
using System.Linq;

namespace DeadManZone.Core.Combat
{
    /// <summary>2026-07-15 faction-roster-v1 §1.8/§2.5 tentpole (Oathborn's Armored Ark):
    /// "load pieces in Build. At the opening window the player targets a cell; the transport
    /// drives there and unloads on arrival (choice of WHERE, not when). If destroyed in
    /// transit, cargo spills out at the wreck with a morale shock — never dies inside."
    ///
    /// This class is the pure data-manipulation seam (mirrors MovementSlowRules/CombatStealthRules):
    /// resolving which combatants are a transport's live cargo, and disembarking one. Logging,
    /// occupancy-grid bookkeeping, and the morale-shock roll stay in TickCombatRun (same split
    /// as ApplyDeathShock/LogDestroyed) since those need the tick's log/occupancy state.</summary>
    public static class TransportRules
    {
        /// <summary>Morale damage each spilled cargo piece takes when its carrier is destroyed
        /// in transit — the "shock" of the wreck, not a second death-shock-radius pulse to
        /// nearby allies. PROVISIONAL, mirrors MoraleRules.DeathShockDamage.</summary>
        public const int SpillMoraleShock = 6;

        /// <summary>Live, still-embarked cargo carried by <paramref name="transport"/>.</summary>
        public static IReadOnlyList<CombatantState> ResolveCargo(
            CombatantState transport,
            IEnumerable<CombatantState> combatants)
        {
            if (transport == null || transport.EmbarkedCargoIds == null || transport.EmbarkedCargoIds.Count == 0 || combatants == null)
                return System.Array.Empty<CombatantState>();

            var cargoIds = new HashSet<string>(transport.EmbarkedCargoIds);
            return combatants
                .Where(c => c != null && c.IsEmbarked && cargoIds.Contains(c.InstanceId))
                .ToList();
        }

        /// <summary>Places <paramref name="cargo"/> onto the field at its carrier's current
        /// anchor and clears its embarked flag. Caller (TickCombatRun) still owns re-placing it
        /// in the occupancy grid and logging the move/event.</summary>
        public static void Disembark(CombatantState cargo, CombatantState transport)
        {
            if (cargo == null || transport == null)
                return;

            cargo.IsEmbarked = false;
            cargo.AnchorPosition = transport.AnchorPosition;
            cargo.RecomputeOccupiedCells();
        }
    }
}
