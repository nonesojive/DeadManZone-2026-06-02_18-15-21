using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    /// <summary>Board-wide tag thresholds that grant team-wide combat bonuses.</summary>
    public static class CriticalMassRules
    {
        public const int InfantryThreshold = 3;
        public const int VehicleThreshold = 2;
        public const int ArtilleryThreshold = 2;

        public readonly struct CriticalMassBonus
        {
            public int DamageBonus { get; init; }
            public int ArmorShredSteps { get; init; }
        }

        public static CriticalMassBonus Evaluate(BoardState board)
        {
            var tagCounts = CountTags(board);
            int damageBonus = 0;
            int armorShred = 0;

            if (tagCounts.GetValueOrDefault(GameKeywords.Infantry) >= InfantryThreshold)
                damageBonus += 2;

            if (tagCounts.GetValueOrDefault(GameKeywords.Vehicle) >= VehicleThreshold)
                armorShred += 1;

            if (tagCounts.GetValueOrDefault(GameKeywords.Artillery) >= ArtilleryThreshold)
                damageBonus += 3;

            return new CriticalMassBonus
            {
                DamageBonus = damageBonus,
                ArmorShredSteps = armorShred
            };
        }

        public static void ApplyToCombatants(BoardState board, IList<CombatantState> combatants)
        {
            var bonus = Evaluate(board);
            if (bonus.DamageBonus == 0 && bonus.ArmorShredSteps == 0)
                return;

            foreach (var combatant in combatants.Where(c => c.IsAlive))
            {
                combatant.DamageBonus += bonus.DamageBonus;
                // Armor shred applies as bonus damage against enemies — stored on attacker side.
            }
        }

        private static Dictionary<string, int> CountTags(BoardState board)
        {
            var counts = new Dictionary<string, int>();
            foreach (var piece in board.Pieces)
            {
                foreach (var tag in piece.Definition.Tags)
                {
                    if (!counts.ContainsKey(tag))
                        counts[tag] = 0;
                    counts[tag]++;
                }
            }

            return counts;
        }
    }
}
