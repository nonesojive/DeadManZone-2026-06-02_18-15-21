using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Ability Definition")]
    public sealed class AbilityDefinitionSO : ScriptableObject
    {
        public string id;
        [TextArea] public string cardDescription;
        public PieceAbilityTrigger trigger;
        public NeighborFilter neighborFilter;
        public SynergyStat stat;
        public SynergyModType modType;
        public int magnitude;
        public string countTagId;

        public PieceAbilityDefinition ToCore() => new()
        {
            Id = id,
            CardDescription = cardDescription,
            Trigger = trigger,
            NeighborFilter = neighborFilter,
            Stat = stat,
            ModType = modType,
            Magnitude = magnitude,
            CountTagId = countTagId
        };
    }
}
