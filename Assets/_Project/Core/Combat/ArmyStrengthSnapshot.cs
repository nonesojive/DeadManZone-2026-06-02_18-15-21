namespace DeadManZone.Core.Combat
{
    public readonly struct ArmyStrengthSnapshot
    {
        public int BaseTotal { get; init; }
        public int EffectiveTotal { get; init; }
        public int SynergyBonus => EffectiveTotal - BaseTotal;
    }
}
