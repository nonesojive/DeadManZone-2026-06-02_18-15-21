using System;
using DeadManZone.Core.Combat;

namespace DeadManZone.Data
{
    public static class TacticUnlockRules
    {
        public static bool IsUnlocked(FactionSO faction, TacticType tactic) =>
            IsUnlockedForList(faction?.startingTactics, tactic);

        /// <summary>Owner rule (2026-07-17): Advance and StandGround (Hold The Line) are the two
        /// universal default doctrines — always available to every faction, every fight, from
        /// fight 1, regardless of authored startingTactics. Faction-specific tactics unlock on
        /// top of these two. Mirrors <see cref="TacticPauseValidator.IsTacticUnlocked"/>; kept as
        /// a separate copy because Core cannot reference this Data-layer FactionSO wrapper.</summary>
        public static bool IsUnlockedForList(TacticType[] startingTactics, TacticType tactic)
        {
            if (tactic == TacticType.Advance || tactic == TacticType.StandGround)
                return true;

            if (startingTactics == null || startingTactics.Length == 0)
                return true; // ponytail: no list = all unlocked
            return Array.IndexOf(startingTactics, tactic) >= 0;
        }
    }
}
