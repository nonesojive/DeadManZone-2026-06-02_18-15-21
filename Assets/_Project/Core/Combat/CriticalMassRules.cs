using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    /// <summary>Board-wide tag thresholds that grant team-wide combat bonuses.</summary>
    public static class CriticalMassRules
    {
        public readonly struct FightStartCriticalMassBonus
        {
            public int DamageBonus { get; init; }
            public int ArmorShredSteps { get; init; }
            public int MoveChargePercentBonus { get; init; }
        }

        public static FightStartCriticalMassBonus EvaluateFightStart(BoardState board)
        {
            int damageBonus = 0;
            int armorShred = 0;
            int moveChargePercentBonus = 0;

            if (board != null)
            {
                var rules = CriticalMassRuleCatalog.GetRules();
                for (int i = 0; i < rules.Count; i++)
                {
                    var rule = rules[i];
                    if (rule.Threshold <= 0 || string.IsNullOrWhiteSpace(rule.TagId))
                        continue;

                    if (CountMatchingPieces(board, rule.TagId, rule.CountCategory) < rule.Threshold)
                        continue;

                    damageBonus += rule.DamageBonus;
                    armorShred += rule.ArmorShredSteps;
                    moveChargePercentBonus += rule.MoveChargePercentBonus;
                }
            }

            return new FightStartCriticalMassBonus
            {
                DamageBonus = damageBonus,
                ArmorShredSteps = armorShred,
                MoveChargePercentBonus = moveChargePercentBonus
            };
        }

        public static FightStartCriticalMassBonus Evaluate(BoardState board) => EvaluateFightStart(board);

        public static void ApplyToCombatants(
            BoardState board,
            IList<CombatantState> combatants,
            FightStartCriticalMassBonus snapshot)
        {
            if (combatants == null)
                return;

            if (snapshot.DamageBonus == 0
                && snapshot.ArmorShredSteps == 0
                && snapshot.MoveChargePercentBonus == 0)
            {
                return;
            }

            // Armor shred is represented as extra offense on the attacker side in this combat model.
            int combinedDamageBonus = snapshot.DamageBonus + snapshot.ArmorShredSteps;
            for (int i = 0; i < combatants.Count; i++)
            {
                var combatant = combatants[i];
                if (combatant == null || !combatant.IsAlive)
                    continue;

                combatant.DamageBonus += combinedDamageBonus;
                combatant.MoveChargePercentBonus += snapshot.MoveChargePercentBonus;
            }
        }

        public static void ApplyToCombatants(BoardState board, IList<CombatantState> combatants)
        {
            ApplyToCombatants(board, combatants, EvaluateFightStart(board));
        }

        private static int CountMatchingPieces(
            BoardState board,
            string tagId,
            CriticalMassCountCategory countCategory)
        {
            int count = 0;
            foreach (var piece in board.Pieces)
            {
                if (piece.Definition == null)
                    continue;

                if (MatchesCategoryTag(piece.Definition, tagId, countCategory))
                    count++;
            }

            return count;
        }

        private static bool MatchesCategoryTag(
            PieceDefinition definition,
            string tagId,
            CriticalMassCountCategory countCategory)
        {
            return countCategory switch
            {
                CriticalMassCountCategory.Primary => PieceTagQueries.HasPrimaryTag(definition, tagId),
                CriticalMassCountCategory.CombatRole => PieceTagQueries.HasCombatRoleTag(definition, tagId),
                CriticalMassCountCategory.Synergy => PieceTagQueries.HasSynergyTag(definition, tagId),
                _ => false
            };
        }
    }
}
