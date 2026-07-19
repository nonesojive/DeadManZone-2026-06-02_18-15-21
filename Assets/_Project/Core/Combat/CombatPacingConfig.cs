namespace DeadManZone.Core.Combat
{
    public static class CombatPacingConfig
    {
        public const int TicksPerSecond = 10;

        /// <summary>Army HP fraction that fires the mid-fight command pause.</summary>
        public static readonly float[] PauseThresholds = { 0.60f };

        /// <summary>2026-07-15 faction-roster-v1 §1.7/§2.6/§4 Paradox's The Second Hand: the
        /// extra threshold appended to a fight's pause-threshold list when a fielded piece has
        /// PieceDefinition.AddsPauseWindow (TickCombatRun instance thresholds). PROVISIONAL.</summary>
        public const float ThirdPauseWindowThreshold = 0.30f;

        /// <summary>Global tick at which anti-stall gas starts ramping. Anti-stall ONLY — this
        /// must sit ABOVE the late-fight duration target band (450-600 ticks), not inside it
        /// (2026-07-19 iteration 2, was 300, which put the ramp exactly where the post-HP-scale
        /// late fights live and throttled them all to identical gas-forced lengths).</summary>
        public const int GasStartTick = 500;

        /// <summary>Absolute fight-length bound; reaching it forces a draw.</summary>
        public const int MaxFightTicks = 10_000;

        public const int GasRampReferenceTicks = 200;

        // PROVISIONAL balance dials 2026-07-19 iter2: durability grows with total units
        // fielded so bigger late-run armies fight longer (owner: ~30s wall early, 45-60s
        // late). Symmetric both sides; strength previews intentionally unscaled. Measured at
        // flat 2.0: fight-1 262 ticks / fight-8 336 (CombatDurationBenchmarkTests).
        public const float BaseDurabilityScale = 2.4f;
        public const float DurabilityPerExtraUnit = 0.18f;
        public const int DurabilityFreeUnits = 8;
        public const float MaxDurabilityScale = 4.4f;

        /// <summary>Army-size-scaled durability: totalSpawnedUnits is BOTH sides' spawning
        /// unit count (the same MaxHp &gt; 0 filter TickCombatRun.SpawnCombatants applies),
        /// so one fight has exactly one scale and player/enemy stay symmetric.</summary>
        public static float DurabilityScaleFor(int totalSpawnedUnits) =>
            System.Math.Min(
                MaxDurabilityScale,
                BaseDurabilityScale
                    + DurabilityPerExtraUnit * System.Math.Max(0, totalSpawnedUnits - DurabilityFreeUnits));

        /// <summary>Overload for callers holding a BattlefieldState: TickCombatRun itself, and
        /// the Presentation HP-bar registration paths (ArmyHealthBarPresenter /
        /// CombatArenaPresenter), which rebuild the battlefield from the same two boards.
        /// BattlefieldState.Cells is an unfiltered pass-through of both boards' pieces, so
        /// counting MaxHp &gt; 0 cells reproduces SpawnCombatants' spawn count exactly —
        /// keeping the presenters' registered maxes identical to the sim's without plumbing
        /// the TickCombatRun instance through the replay layer.</summary>
        public static float DurabilityScaleFor(Board.BattlefieldState battlefield)
        {
            int totalSpawnedUnits = 0;
            foreach (var cell in battlefield.Cells)
            {
                if (cell?.Definition != null && cell.Definition.MaxHp > 0)
                    totalSpawnedUnits++;
            }

            return DurabilityScaleFor(totalSpawnedUnits);
        }

        /// <summary>The single implementation of the durability scale: deterministic integer
        /// math via System.Math.Round with MidpointRounding.AwayFromZero (round-half-away, not
        /// banker's rounding), floored at 1 so no combat unit ever scales to zero HP. Callers:
        /// TickCombatRun.SpawnCombatants (the one seam where combat max HP is read off a
        /// PieceDefinition — both sides spawn through it), ManpowerCalculator.ComputeCasualties
        /// (via CombatantState.MaxHp, converting scaled combat damage back to run-state bodies),
        /// and the replay HP-bar RegisterUnit calls in Presentation. The scale itself comes
        /// from DurabilityScaleFor above.</summary>
        public static int ScaleUnitMaxHp(int definitionMaxHp, float durabilityScale) =>
            System.Math.Max(1, (int)System.Math.Round(
                definitionMaxHp * durabilityScale, System.MidpointRounding.AwayFromZero));
    }
}
