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
        Faction,
        /// <summary>2026-07-15 faction-roster-v1 §1.9/§3 Dust Scourge's salvage-count inversion:
        /// CountTagId is the PLAYER's own faction id; counts board pieces where
        /// OffFactionRules.IsSalvage(piece, CountTagId) is true (off-faction, non-neutral,
        /// non-mercenary) instead of pieces belonging to that faction.</summary>
        SalvageForFaction
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
