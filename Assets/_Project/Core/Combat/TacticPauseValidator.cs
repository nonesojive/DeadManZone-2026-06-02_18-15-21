using System.Collections.Generic;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Combat
{
    public sealed class TacticPauseValidator
    {
        public bool CanContinue(
            TacticType selected,
            TacticType previous,
            bool hqAlive,
            bool hasCommandPiece,
            int checkpointIndex,
            ref int authority,
            out string reason)
        {
            reason = null;

            if (selected == TacticType.DisciplinedFire && !hqAlive)
            {
                reason = "HQ destroyed";
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
            if (checkpointIndex != 1 || selected == previous)
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
            bool hqAlive,
            bool hasCommandPiece,
            int checkpointIndex,
            int authority,
            IEnumerable<GrantedAbility> abilities,
            out string reason)
        {
            reason = null;

            if (selected == TacticType.DisciplinedFire && !hqAlive)
            {
                reason = "HQ destroyed";
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
    }
}
