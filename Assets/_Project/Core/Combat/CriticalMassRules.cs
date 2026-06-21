using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    /// <summary>Backward-compatible facade over <see cref="CriticalMassEngine"/>.</summary>
    public static class CriticalMassRules
    {
        public static CriticalMassSnapshot EvaluateFightStart(BoardState board) =>
            CriticalMassEngine.Evaluate(board);

        public static CriticalMassSnapshot Evaluate(BoardState board) =>
            CriticalMassEngine.Evaluate(board);

        public static void ApplyToCombatants(
            CriticalMassSnapshot snapshot,
            IList<CombatantState> combatants) =>
            CriticalMassEngine.ApplyToCombatants(snapshot, combatants);

        public static void ApplyToCombatants(
            BoardState board,
            IList<CombatantState> combatants,
            CriticalMassSnapshot snapshot)
        {
            ApplyToCombatants(snapshot, combatants);
        }

        public static void ApplyToCombatants(BoardState board, IList<CombatantState> combatants) =>
            ApplyToCombatants(board, combatants, EvaluateFightStart(board));
    }
}
