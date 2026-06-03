namespace DeadManZone.Core.Combat
{
    public enum CombatSegment
    {
        Opening = 1,
        MainFight = 2,
        GasFinal = 3
    }

    public static class SegmentTickBudget
    {
        public const int Opening = 100;
        public const int MainFight = 500;
        public const int GasFinal = 200;
    }
}
