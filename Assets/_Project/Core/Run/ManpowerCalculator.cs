using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Content;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Run
{
    public static class ManpowerCalculator
    {
        public static int HpPerBody(PieceDefinition definition)
        {
            if (definition == null || definition.ManpowerCost <= 0)
                return definition?.MaxHp ?? 1;
            return definition.MaxHp / definition.ManpowerCost;
        }

        public static int ComputeFieldingRequirement(BoardState board, ContentRegistry content)
        {
            if (board == null)
                return 0;

            return board.Pieces
                .Where(p => CountsTowardFielding(p.Definition))
                .Sum(p => p.Definition.ManpowerCost);
        }

        /// <summary>2026-07-15 faction-roster-v1 §2.1 Field Hospital: post-fight, reduces
        /// Manpower lost to damaged-but-surviving units. "Priced painfully — insurance costs
        /// tempo" per the design spec; PROVISIONAL magnitude, tune in playtest.</summary>
        public const int FieldHospitalSurvivorCasualtyReductionPercent = 50;

        private const string FieldHospitalPieceId = "field_hospital";

        /// <param name="hqBoard">Optional HQ board to check for Field Hospital. field_hospital
        /// is Building-primary, so it always resolves to the HQ board
        /// (BoardPlacementRules.ResolveTargetBoard) — never the combat board.</param>
        public static int ComputeCasualties(IReadOnlyList<CombatantState> playerCombatants, BoardState hqBoard = null)
        {
            if (playerCombatants == null || playerCombatants.Count == 0)
                return 0;

            bool hasFieldHospital = HasFieldHospital(hqBoard);
            int total = 0;
            foreach (var c in playerCombatants)
            {
                if (c?.Definition == null || c.Definition.ManpowerCost <= 0)
                    continue;
                // Routed units fled the field intact (ADR-0005 mercy mechanic): no death
                // cost AND no damage-taken attrition — they stand again next round.
                if (c.IsBroken)
                    continue;
                if (c.DamageTakenThisFight <= 0 && c.IsAlive)
                    continue;

                if (!c.IsAlive)
                {
                    total += c.Definition.ManpowerCost;
                    continue;
                }

                // DamageTakenThisFight is in durability-scaled combat-HP units (the fight's
                // army-size CombatPacingConfig.DurabilityScaleFor), so the per-body divisor must be the
                // combatant's stored scaled MaxHp — dividing scaled damage by raw definition HP
                // would double survivor attrition. Bodies-lost fraction (and thus run-state
                // Manpower outcomes) is identical to pre-scale. ManpowerCost > 0 is guaranteed
                // by the guard above.
                int hpPerBody = c.MaxHp / c.Definition.ManpowerCost;
                int bodies = hpPerBody > 0 ? c.DamageTakenThisFight / hpPerBody : 0;
                if (hasFieldHospital)
                    bodies = bodies * (100 - FieldHospitalSurvivorCasualtyReductionPercent) / 100;
                total += Math.Min(c.Definition.ManpowerCost, bodies);
            }

            return total;
        }

        private static bool HasFieldHospital(BoardState hqBoard) =>
            hqBoard != null && hqBoard.Pieces.Any(p => p.Definition.Id == FieldHospitalPieceId);

        public static int ComputeUpkeep(BoardState board, ContentRegistry content) =>
            ComputeFieldingRequirement(board, content);

        public static bool CountsTowardFielding(PieceDefinition definition) =>
            definition != null && definition.ManpowerCost > 0;
    }
}
