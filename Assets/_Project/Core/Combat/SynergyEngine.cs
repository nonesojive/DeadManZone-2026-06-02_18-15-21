using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    /// <summary>Applies adjacency and tag-based combat buffs at fight start.</summary>
    public static class SynergyEngine
    {
        public readonly struct SynergyResult
        {
            public int DamageBonus { get; init; }
            public int ArmorBuffSteps { get; init; }
            public int MoveChargeBonus { get; init; }
        }

        public static SynergyResult ComputeForCombatant(
            BoardState board,
            PlacedPiece placedPiece,
            IReadOnlyList<PlacedPiece> allPieces)
        {
            int damageBonus = 0;
            int armorSteps = 0;
            int moveCharge = 0;
            var tags = placedPiece.Definition.Tags;

            foreach (var adjacentId in board.GetAdjacentInstanceIds(placedPiece.InstanceId))
            {
                var adjacent = allPieces.First(p => p.InstanceId == adjacentId);
                var adjTags = adjacent.Definition.Tags;

                if (adjTags.Contains(GameKeywords.Supply))
                    damageBonus += 1;

                if (adjTags.Contains(GameKeywords.Medic) && tags.Contains(GameKeywords.Infantry))
                    armorSteps += 1;

                if (adjTags.Contains(GameKeywords.Command) && tags.Contains(GameKeywords.Artillery))
                    damageBonus += 2;

                if (adjTags.Contains(GameKeywords.Echo) && tags.Contains(GameKeywords.Stealth))
                    damageBonus += 1;
            }

            return new SynergyResult
            {
                DamageBonus = damageBonus,
                ArmorBuffSteps = armorSteps,
                MoveChargeBonus = moveCharge
            };
        }

        public static void ApplyToCombatants(BoardState board, IList<CombatantState> combatants)
        {
            var pieces = board.Pieces.ToList();
            foreach (var combatant in combatants)
            {
                var placed = pieces.FirstOrDefault(p => p.InstanceId == combatant.InstanceId);
                if (placed == null)
                    continue;

                var synergy = ComputeForCombatant(board, placed, pieces);
                combatant.DamageBonus += synergy.DamageBonus;
                combatant.ArmorBuffSteps += synergy.ArmorBuffSteps;
                combatant.MoveCharge += synergy.MoveChargeBonus;
            }
        }
    }
}
