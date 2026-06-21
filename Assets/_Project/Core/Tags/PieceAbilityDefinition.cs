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
    }
}
