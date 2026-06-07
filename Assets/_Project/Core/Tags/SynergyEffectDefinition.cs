namespace DeadManZone.Core.Tags
{
    public readonly struct SynergyEffectDefinition
    {
        public string SourceSynergyTagId { get; init; }
        public SynergyDirection Direction { get; init; }
        public NeighborFilter NeighborFilter { get; init; }
        public SynergyStat Stat { get; init; }
        public SynergyModType ModType { get; init; }
        public int Magnitude { get; init; }
    }
}
