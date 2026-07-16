using System.Collections.Generic;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    /// <summary>2026-07-15 faction-roster-v1 §2.1 Trench Works: "no attack; adjacent enemy
    /// units' movement charge slowed while it stands." Mirrors CombatStealthRules — a small,
    /// directly-testable pure-rules seam, kept out of TickCombatRun's tick loop.
    ///
    /// Deliberately NOT the Suppression tag: §1.8's border rule reserves Suppression for
    /// Crimson (attack-speed tier step-down + movement slow, applied on hit, N-tick duration).
    /// This is narrower — a permanent-while-adjacent movement-only debuff, no duration/on-hit
    /// trigger — checked live against the tick sim's shared cross-side coordinate space (unlike
    /// PieceAbilityEngine's pre-fight AdjacentAura, which only ever sees one side's own board).</summary>
    public static class MovementSlowRules
    {
        // PROVISIONAL — tune in playtest.
        public const int MovementSlowPercent = 50;

        public static bool IsSlowed(CombatantState mover, IReadOnlyList<CombatantState> opponents)
        {
            if (mover == null || opponents == null)
                return false;

            foreach (var opponent in opponents)
            {
                if (opponent == null || !opponent.IsActive)
                    continue;

                if (!PieceTagQueries.HasAbilityTag(opponent.Definition, GameTagIds.MovementSlowAura))
                    continue;

                if (CombatRange.Distance(mover.AnchorPosition, opponent.AnchorPosition) <= 1)
                    return true;
            }

            return false;
        }

        public static int ApplyMovementSlow(int moveChargePerTick, bool isSlowed) =>
            isSlowed ? moveChargePerTick * (100 - MovementSlowPercent) / 100 : moveChargePerTick;
    }
}
