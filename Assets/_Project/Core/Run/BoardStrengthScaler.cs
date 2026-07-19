using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;

namespace DeadManZone.Core.Run
{
    /// <summary>
    /// PROVISIONAL 2026-07-19 owner spec (fight-option strength ratios): solves the uniform
    /// per-piece <see cref="PlacedPiece.StatScale"/> that brings a board's
    /// <see cref="ArmyStrengthCalculator"/> EffectiveTotal to a target — used by
    /// <see cref="FightOptionGenerator"/> to pin Easy at 0.85x and Hard at 1.30x of the
    /// Normal draw. Binary search: the rating is monotone non-decreasing in the scale
    /// (each piece's rating ~ sqrt(scaledHp * scaledDps), both factors non-decreasing in s;
    /// per-piece integer rounding makes it a step function, so the solver keeps the
    /// best-seen scale rather than assuming an exact crossing exists). Deterministic —
    /// pure float/int math, no rng; the loop allocates nothing beyond what Evaluate needs.
    ///
    /// NOTE: the owner-spec signature carried a ContentRegistry parameter; it was dropped —
    /// Evaluate reads everything off the board's own PlacedPiece.Definition references, and
    /// the one production caller (FightOptionGenerator.Generate) has no registry in scope.
    /// </summary>
    public static class BoardStrengthScaler
    {
        public const float MinScale = 0.4f;
        public const float MaxScale = 2.5f;
        public const int MaxIterations = 24;

        /// <summary>Relative-error convergence target: 0.5%.</summary>
        public const float ConvergenceTolerance = 0.005f;

        /// <param name="includeFightStartEngines">False ONLY when solving an army that will
        /// fight (and preview) with its engines suppressed — the enemy on an EASY front.
        /// Solving Easy on the suppressed basis is what makes its DISPLAYED number (and its
        /// actual green-force fight strength) land on the 0.85 ratio.</param>
        /// <returns>The best scale found in [<see cref="MinScale"/>, <see cref="MaxScale"/>];
        /// the board is left with that scale APPLIED. Returns 1 (board untouched) for a
        /// null/empty board or a non-positive target.</returns>
        public static float SolveScale(
            BoardState board,
            int targetEffectiveTotal,
            bool includeFightStartEngines = true,
            float maxScale = MaxScale)
        {
            if (board == null || board.Pieces.Count == 0 || targetEffectiveTotal <= 0)
                return 1f;

            float lo = MinScale;
            float hi = maxScale;
            float best = 1f;
            float bestError = float.MaxValue;

            for (int i = 0; i < MaxIterations; i++)
            {
                float mid = (lo + hi) * 0.5f;
                ApplyScale(board, mid);
                int rating = ArmyStrengthCalculator.Evaluate(
                    board, buildBoards: null, includeFightStartEngines).EffectiveTotal;
                float error = System.Math.Abs(rating - targetEffectiveTotal)
                    / (float)targetEffectiveTotal;

                if (error < bestError)
                {
                    bestError = error;
                    best = mid;
                }

                if (error <= ConvergenceTolerance)
                    break;

                if (rating < targetEffectiveTotal)
                    lo = mid;
                else
                    hi = mid;
            }

            ApplyScale(board, best);
            return best;
        }

        /// <summary>Sets every piece's StatScale to <paramref name="scale"/> (uniform).</summary>
        public static void ApplyScale(BoardState board, float scale)
        {
            if (board == null)
                return;

            foreach (var piece in board.Pieces)
                piece.StatScale = scale;
        }
    }
}
