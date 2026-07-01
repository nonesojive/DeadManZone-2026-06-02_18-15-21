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
            ApplyToSelf = applyToSelf
        };
    }
}
