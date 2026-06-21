namespace DeadManZone.Core.Tags
{
    public enum CriticalMassCountCategory
    {
        Primary,
        CombatRole,
        Synergy,
        Ability,
        Flavor,
        AttackType,
        Faction
    }

    public readonly struct CriticalMassRuleDefinition
    {
        public string Id { get; init; }
        public string CountTagId { get; init; }
        public CriticalMassCountCategory CountCategory { get; init; }
        public CriticalMassTier[] Tiers { get; init; }
        public CriticalMassStat Stat { get; init; }
        public SynergyModType ModType { get; init; }
        public CriticalMassScope Scope { get; init; }
        public CriticalMassTargetFilter Target { get; init; }
    }
}
