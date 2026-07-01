namespace DeadManZone.Core.Tags
{
    public readonly struct PieceAbilityDefinition
    {
        public string Id { get; init; }
        public string CardDescription { get; init; }
        public PieceAbilityTrigger Trigger { get; init; }
        public NeighborFilter NeighborFilter { get; init; }
        public SynergyStat Stat { get; init; }
        public SynergyModType ModType { get; init; }
        public int Magnitude { get; init; }
        public string CountTagId { get; init; }
        /// <summary>When true, each matching adjacent piece buffs the aura source instead of the neighbor.</summary>
        public bool ApplyToSelf { get; init; }
    }
}
