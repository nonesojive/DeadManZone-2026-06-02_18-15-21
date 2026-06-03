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
        // Sim ticks per segment (~10 tps presentation pacing in CombatDirector).
        public const int Opening = 10;
        public const int MainFight = 50;
        public const int GasFinal = 20;
    }
}
