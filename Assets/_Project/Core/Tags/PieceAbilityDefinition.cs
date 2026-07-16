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
        /// <summary>AdjacentAura hop count. 0 or 1 (default) = literal board-touching adjacency,
        /// the original behavior. 2026-07-15 faction-roster-v1: Breakthrough Tank's "within 2
        /// cells" aura reuses the same board-adjacency topology at 2 hops rather than introducing
        /// raw grid-distance geometry.</summary>
        public int Radius { get; init; }
    }
}
