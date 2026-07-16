using DeadManZone.Core.Board;
using DeadManZone.Core.Run;

namespace DeadManZone.Core.Combat
{
    /// <summary>
    /// ARMY STRENGTH — the number the player compares their board against a front with.
    ///
    /// It must rate the army **as it will actually fight**, which means running the same two
    /// fight-start engines the sim runs: <see cref="CriticalMassEngine"/>, then
    /// <see cref="PieceAbilityEngine"/>. Previously this applied only PART of ONE of them
    /// (synergy's damage and armor), so an army whose value came from HP auras and Critical Mass
    /// thresholds previewed as if none of it existed — the preview understated exactly the
    /// compositions the game most wants the player to build.
    ///
    /// <para><b>BaseTotal</b> = raw stat line, engines off. <b>EffectiveTotal</b> = what marches.</para>
    /// </summary>
    public static class ArmyStrengthCalculator
    {
        /// <param name="buildBoards">
        /// The player's full board set. Combat evaluates synergy with the HQ board in scope
        /// (<c>TickCombatRun</c> → <c>EvaluateFightStart(playerBoard, playerBuildBoards)</c>), so
        /// omitting it under-rates HQ-fed auras. Enemies have no HQ board — pass null.
        /// </param>
        /// <param name="includeFightStartEngines">
        /// False ONLY for an army that will fight with its engines suppressed — i.e. the enemy on
        /// an EASY front, which fields a green force (<c>suppressEnemyFightStartEngines</c>).
        /// Passing false there is what keeps the Easy preview honest without spelling the mechanic
        /// out in the UI: the number is simply, correctly, lower.
        /// </param>
        public static ArmyStrengthSnapshot Evaluate(
            BoardState board,
            BuildBoardSet buildBoards = null,
            bool includeFightStartEngines = true)
        {
            if (board == null || board.Pieces.Count == 0)
                return default;

            var synergySnapshot = PieceAbilityEngine.FightStartSynergySnapshot.Empty;
            var criticalMassSnapshot = CriticalMassSnapshot.Empty;

            if (includeFightStartEngines)
            {
                synergySnapshot = buildBoards != null
                    ? PieceAbilityEngine.EvaluateFightStart(board, buildBoards)
                    : PieceAbilityEngine.EvaluateFightStart(board);

                // Combat board only — the same scope TickCombatRun uses. An HQ building cannot
                // tip a combat threshold.
                criticalMassSnapshot = CriticalMassEngine.Evaluate(board);
            }

            int baseTotal = 0;
            int effectiveTotal = 0;

            foreach (var placed in board.Pieces)
            {
                if (!ManpowerCalculator.CountsTowardFielding(placed.Definition))
                    continue;

                baseTotal += PieceCombatRating.ComputeBase(placed.Definition);

                synergySnapshot.TryGet(placed.InstanceId, out var synergy);
                criticalMassSnapshot.ModifiersByInstanceId.TryGetValue(
                    placed.InstanceId, out var criticalMass);

                effectiveTotal += PieceCombatRating.Compute(placed.Definition, synergy, criticalMass);
            }

            return new ArmyStrengthSnapshot
            {
                BaseTotal = baseTotal,
                EffectiveTotal = effectiveTotal
            };
        }
    }
}
