namespace DeadManZone.Core.Combat
{
    public static class CombatPacingConfig
    {
        public const int TicksPerSecond = 10;
        public const int OpeningTicks = 50;
        public const int MainFightTicks = 200;
        public const int BriefPushTicks = 50;
        public const int MaxGasTicks = 10_000;
        public const int GasRampReferenceTicks = 200;
    }
}
