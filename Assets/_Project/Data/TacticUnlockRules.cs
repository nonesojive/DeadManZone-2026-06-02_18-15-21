using System;
using DeadManZone.Core.Combat;

namespace DeadManZone.Data
{
    public static class TacticUnlockRules
    {
        public static bool IsUnlocked(FactionSO faction, TacticType tactic)
        {
            if (faction?.startingTactics == null || faction.startingTactics.Length == 0)
                return true; // ponytail: no list = all unlocked
            return Array.IndexOf(faction.startingTactics, tactic) >= 0;
        }

        public static bool IsUnlockedForList(TacticType[] startingTactics, TacticType tactic)
        {
            if (startingTactics == null || startingTactics.Length == 0)
                return true; // ponytail: no list = all unlocked
            return Array.IndexOf(startingTactics, tactic) >= 0;
        }
    }
}
