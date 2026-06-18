namespace DeadManZone.Core.Combat
{
    public static class CombatAccuracyConfig
    {
        public const float InnerRangeFraction = 0.6f;
        public const float AccuracyFloorFraction = 0.5f;
        public const int AbsoluteAccuracyFloor = 40;
        public const int GrazeBandBaseline = 12;
        public const int GrazeBandAtPointBlank = 2;
        public const float GrazeBandMaxMultiplier = 2f;
        public const float GrazeDamageFraction = 0.33f;
    }
}
