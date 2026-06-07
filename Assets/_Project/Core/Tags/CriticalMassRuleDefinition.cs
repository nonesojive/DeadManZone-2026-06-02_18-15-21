namespace DeadManZone.Core.Tags
{
    public enum CriticalMassCountCategory
    {
        Primary,
        CombatRole,
        Synergy
    }

    public readonly struct CriticalMassRuleDefinition
    {
        public string TagId { get; init; }
        public CriticalMassCountCategory CountCategory { get; init; }
        public int Threshold { get; init; }
        public int DamageBonus { get; init; }
        public int ArmorShredSteps { get; init; }
        public int MoveChargePercentBonus { get; init; }
    }
}
