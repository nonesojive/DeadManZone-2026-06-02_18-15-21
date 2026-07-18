using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Combat
{
    public sealed class TacticPauseValidator
    {
        public bool CanContinue(
            TacticType selected,
            TacticType previous,
            bool hasCommandPiece,
            int checkpointIndex,
            ref int authority,
            out string reason,
            TacticType[] startingTactics = null)
        {
            reason = null;

            if (!IsTacticUnlocked(startingTactics, selected))
            {
                reason = "Tactic not unlocked";
                return false;
            }

            if (selected == TacticType.ProtectSupport && !hasCommandPiece)
            {
                reason = "No Command piece on board";
                return false;
            }

            int cost = GetTacticCost(selected, previous, checkpointIndex);
            if (authority < cost)
            {
                reason = "Insufficient Authority";
                return false;
            }

            authority -= cost;
            return true;
        }

        public static int GetTacticCost(TacticType selected, TacticType previous, int checkpointIndex)
        {
            int cost = selected == TacticType.ProtectSupport ? 1 : 0;
            // checkpointIndex 0 (opening) is always a free pick; any pause after that (1, or 2
            // with a third window fielded — 2026-07-15 faction-roster-v1 §1.7 The Second Hand)
            // costs an extra Authority to switch away from the standing tactic.
            if (checkpointIndex < 1 || selected == previous)
                return cost;

            return cost + 1;
        }

        public static int GetTotalPauseCost(
            TacticType selected,
            TacticType previous,
            int checkpointIndex,
            IEnumerable<GrantedAbility> abilities)
        {
            int cost = GetTacticCost(selected, previous, checkpointIndex);
            if (abilities == null)
                return cost;

            foreach (var ability in abilities)
                cost += CombatAbilityExecutor.GetAuthorityCost(ability, checkpointIndex);

            return cost;
        }

        public bool ValidatePause(
            TacticType selected,
            TacticType previous,
            bool hasCommandPiece,
            int checkpointIndex,
            int authority,
            IEnumerable<GrantedAbility> abilities,
            out string reason,
            TacticType[] startingTactics = null)
        {
            reason = null;

            if (!IsTacticUnlocked(startingTactics, selected))
            {
                reason = "Tactic not unlocked";
                return false;
            }

            if (selected == TacticType.ProtectSupport && !hasCommandPiece)
            {
                reason = "No Command piece on board";
                return false;
            }

            int cost = GetTotalPauseCost(selected, previous, checkpointIndex, abilities);
            if (authority < cost)
            {
                reason = "Insufficient Authority";
                return false;
            }

            return true;
        }

        /// <summary>Owner rule (2026-07-17): Advance and StandGround (Hold The Line) are the two
        /// universal default doctrines — always available to every faction, every fight, from
        /// fight 1, regardless of authored <c>startingTactics</c>. Faction-specific tactics
        /// (DisciplinedFire, ProtectSupport, ...) unlock on top of these two, per faction data.
        /// Public so callers outside Core (e.g. the orders window's LOCKED label) share this one
        /// verdict instead of re-deriving it.</summary>
        public static bool IsTacticUnlocked(TacticType[] startingTactics, TacticType tactic)
        {
            if (tactic == TacticType.Advance || tactic == TacticType.StandGround)
                return true;

            if (startingTactics == null || startingTactics.Length == 0)
                return true; // ponytail: no list = all unlocked
            return Array.IndexOf(startingTactics, tactic) >= 0;
        }
    }
}
