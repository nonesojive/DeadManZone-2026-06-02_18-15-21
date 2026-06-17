using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    /// <summary>
    /// Deterministic engagement goals for presentation chase movement.
    /// Uses the same RoleEngagement rules as the sim so replays stay truthful for async PvP.
    /// </summary>
    public static class CombatPresentationEngagement
    {
        public static GridCoord ComputeGoal(
            BattlefieldCell unit,
            GridCoord unitAnchor,
            IReadOnlyList<BattlefieldCell> allCells,
            IReadOnlyDictionary<string, GridCoord> aliveAnchors,
            BattlefieldLayout layout)
        {
            if (unit?.Definition == null || layout == null)
                return unitAnchor;

            var combatant = ToCombatant(unit, unitAnchor);
            var allies = BuildCombatants(unit.Side, allCells, aliveAnchors);
            var enemies = BuildCombatants(OppositeSide(unit.Side), allCells, aliveAnchors);

            return RoleEngagement.ComputeGoal(combatant, allies, enemies, layout);
        }

        /// <summary>
        /// Presentation-only march target: a short lead ahead of the sim anchor toward the engagement goal.
        /// Prevents visuals from sprinting to the enemy's cell on the far side of the field.
        /// </summary>
        public static GridCoord ComputeChaseAnchor(
            BattlefieldCell unit,
            GridCoord unitAnchor,
            IReadOnlyList<BattlefieldCell> allCells,
            IReadOnlyDictionary<string, GridCoord> aliveAnchors,
            BattlefieldLayout layout,
            float maxLeadCells)
        {
            if (unit?.Definition == null || layout == null)
                return unitAnchor;

            var combatant = ToCombatant(unit, unitAnchor);
            var allies = BuildCombatants(unit.Side, allCells, aliveAnchors);
            var enemies = BuildCombatants(OppositeSide(unit.Side), allCells, aliveAnchors);

            if (CombatMovementRules.HasEnemyInRange(combatant, enemies))
                return unitAnchor;

            var goal = RoleEngagement.ComputeGoal(combatant, allies, enemies, layout);
            if (!CombatMovementRules.ShouldAttemptMove(combatant, enemies, goal))
                return unitAnchor;

            return ClampLeadTowardGoal(unitAnchor, goal, maxLeadCells);
        }

        public static bool ShouldChase(
            BattlefieldCell unit,
            GridCoord unitAnchor,
            IReadOnlyList<BattlefieldCell> allCells,
            IReadOnlyDictionary<string, GridCoord> aliveAnchors,
            BattlefieldLayout layout)
        {
            if (unit?.Definition == null || layout == null)
                return false;

            if (!PieceTagQueries.HasTag(unit.Definition, GameTagIds.Combatant))
                return false;

            var combatant = ToCombatant(unit, unitAnchor);
            var enemies = BuildCombatants(OppositeSide(unit.Side), allCells, aliveAnchors);

            if (CombatMovementRules.HasEnemyInRange(combatant, enemies))
                return false;

            var goal = RoleEngagement.ComputeGoal(
                combatant,
                BuildCombatants(unit.Side, allCells, aliveAnchors),
                enemies,
                layout);

            return CombatMovementRules.ShouldAttemptMove(combatant, enemies, goal);
        }

        private static GridCoord ClampLeadTowardGoal(GridCoord from, GridCoord to, float maxLeadCells)
        {
            int maxLead = System.Math.Max(1, (int)System.MathF.Round(maxLeadCells));
            if (from.Equals(to))
                return from;

            int dx = to.X - from.X;
            int dy = to.Y - from.Y;
            int manhattan = System.Math.Abs(dx) + System.Math.Abs(dy);
            if (manhattan <= maxLead)
                return to;

            int x = from.X;
            int y = from.Y;
            int remaining = maxLead;
            int stepX = System.Math.Sign(dx);
            int stepY = System.Math.Sign(dy);

            while (remaining > 0 && (x != to.X || y != to.Y))
            {
                if (x != to.X && remaining > 0)
                {
                    x += stepX;
                    remaining--;
                }

                if (y != to.Y && remaining > 0)
                {
                    y += stepY;
                    remaining--;
                }
            }

            return new GridCoord(x, y);
        }

        private static List<CombatantState> BuildCombatants(
            CombatSide side,
            IReadOnlyList<BattlefieldCell> allCells,
            IReadOnlyDictionary<string, GridCoord> aliveAnchors)
        {
            var combatants = new List<CombatantState>();
            if (allCells == null || aliveAnchors == null)
                return combatants;

            foreach (var cell in allCells.Where(c => c != null && c.Side == side))
            {
                if (!aliveAnchors.TryGetValue(cell.InstanceId, out var anchor))
                    continue;

                if (!PieceTagQueries.HasTag(cell.Definition, GameTagIds.Combatant))
                    continue;

                combatants.Add(ToCombatant(cell, anchor));
            }

            combatants.Sort((left, right) =>
                string.CompareOrdinal(left.InstanceId, right.InstanceId));

            return combatants;
        }

        private static CombatantState ToCombatant(BattlefieldCell cell, GridCoord anchor) =>
            new()
            {
                InstanceId = cell.InstanceId,
                Side = cell.Side,
                Definition = cell.Definition,
                CurrentHp = 1,
                AnchorPosition = anchor,
                SpawnAnchorY = anchor.Y,
                ShapeOffsets = System.Array.Empty<GridCoord>()
            };

        private static CombatSide OppositeSide(CombatSide side) =>
            side == CombatSide.Player ? CombatSide.Enemy : CombatSide.Player;
    }
}
