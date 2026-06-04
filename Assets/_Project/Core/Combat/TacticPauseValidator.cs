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
            CombatPhase pauseAfterPhase,
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

            int cost = GetTacticCost(selected, previous, pauseAfterPhase);
            if (authority < cost)
            {
                reason = "Insufficient Authority";
                return false;
            }

            authority -= cost;
            return true;
        }

        public static int GetTacticCost(TacticType selected, TacticType previous, CombatPhase pauseAfterPhase)
        {
            int cost = selected == TacticType.ProtectSupport ? 1 : 0;
            if (pauseAfterPhase != CombatPhase.Grind || selected == previous)
                return cost;

            if (selected == TacticType.ProtectSupport)
                return cost + 1;

            return cost + 1;
        }

        public static int GetTotalPauseCost(
            TacticType selected,
            TacticType previous,
            CombatPhase pauseAfterPhase,
            IEnumerable<GrantedAbility> abilities)
        {
            int cost = GetTacticCost(selected, previous, pauseAfterPhase);
            if (abilities == null)
                return cost;

            foreach (var ability in abilities)
                cost += CombatAbilityExecutor.GetAuthorityCost(ability, pauseAfterPhase);

            return cost;
        }

        public bool ValidatePause(
            TacticType selected,
            TacticType previous,
            bool hqAlive,
            bool hasCommandPiece,
            CombatPhase pauseAfterPhase,
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

            int cost = GetTotalPauseCost(selected, previous, pauseAfterPhase, abilities);
            if (authority < cost)
            {
                reason = "Insufficient Authority";
                return false;
            }

            return true;
        }
    }
}
