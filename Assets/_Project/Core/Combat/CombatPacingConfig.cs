namespace DeadManZone.Core.Combat
{
    public static class CombatPacingConfig
    {
        public const int TicksPerSecond = 10;

        /// <summary>Army HP fraction that fires the mid-fight command pause.</summary>
        public static readonly float[] PauseThresholds = { 0.60f };

        /// <summary>Global tick at which anti-stall gas starts ramping (~30s of fight).</summary>
        public const int GasStartTick = 300;

        /// <summary>Absolute fight-length bound; reaching it forces a draw.</summary>
        public const int MaxFightTicks = 10_000;

        public const int GasRampReferenceTicks = 200;
    }
}
