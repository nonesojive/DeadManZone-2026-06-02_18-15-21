using System.Collections.Generic;
using System.Linq;

namespace DeadManZone.Core.Combat
{
    public static class BattleReportBuilder
    {
        public static BattleReport Build(
            IEnumerable<CombatantState> playerCombatants,
            bool playerWon,
            bool isDraw,
            int manpowerRefunded,
            int suppliesEarned,
            int moraleDelta,
            int topCount = 3)
        {
            var playerSide = playerCombatants.Where(c => c.Side == CombatSide.Player).ToList();
            return new BattleReport
            {
                PlayerWon = playerWon,
                IsDraw = isDraw,
                ManpowerRefunded = manpowerRefunded,
                SuppliesEarned = suppliesEarned,
                MoraleDelta = moraleDelta,
                TopDamageDealt = RankByDamage(playerSide, c => c.DamageDealtThisFight, topCount),
                TopDamageTaken = RankByDamage(playerSide, c => c.DamageTakenThisFight, topCount)
            };
        }

        private static List<BattleReportEntry> RankByDamage(
            IReadOnlyList<CombatantState> combatants,
            System.Func<CombatantState, int> selector,
            int topCount) =>
            combatants
                .Where(c => selector(c) > 0)
                .OrderByDescending(selector)
                .ThenBy(c => c.InstanceId)
                .Take(topCount)
                .Select(c => new BattleReportEntry
                {
                    InstanceId = c.InstanceId,
                    DisplayName = c.Definition.DisplayName,
                    Damage = selector(c)
                })
                .ToList();
    }
}
