using System;
using DeadManZone.Core.Tags;

namespace DeadManZone.Data
{
    [Serializable]
    public sealed class PieceAbilityInlineEntry
    {
        public string id;
        public string cardDescription;
        public PieceAbilityTrigger trigger;
        public NeighborFilter neighborFilter;
        public SynergyStat stat;
        public SynergyModType modType;
        public int magnitude;
        public string countTagId;
        public bool applyToSelf;
        /// <summary>AdjacentAura hop count. 0/1 = literal board-touching adjacency (default).</summary>
        public int radius = 1;

        public PieceAbilityDefinition ToCore() => new()
        {
            Id = id,
            CardDescription = cardDescription,
            Trigger = trigger,
            NeighborFilter = neighborFilter,
            Stat = stat,
            ModType = modType,
            Magnitude = magnitude,
            CountTagId = countTagId,
            ApplyToSelf = applyToSelf,
            Radius = radius
        };
    }
}
