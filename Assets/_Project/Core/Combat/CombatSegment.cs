namespace DeadManZone.Core.Combat
{
    public enum CombatSegment
    {
        Opening = 1,
        MainFight = 2,
        BriefPush = 3,
        GasFinal = 4
    }

    public static class SegmentTickBudget
    {
        public const int Opening = CombatPacingConfig.OpeningTicks;
        public const int MainFight = CombatPacingConfig.MainFightTicks;
        public const int BriefPush = CombatPacingConfig.BriefPushTicks;
        public const int GasFinal = CombatPacingConfig.GasRampReferenceTicks;
    }
}
