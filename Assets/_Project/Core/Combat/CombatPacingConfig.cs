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

        /// <summary>Global tick at which anti-stall gas starts ramping (~30s of fight).</summary>
        public const int GasStartTick = 300;

        /// <summary>Absolute fight-length bound; reaching it forces a draw.</summary>
        public const int MaxFightTicks = 10_000;

        public const int GasRampReferenceTicks = 200;
    }
}
