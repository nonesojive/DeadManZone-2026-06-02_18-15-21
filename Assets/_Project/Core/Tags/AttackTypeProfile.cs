using DeadManZone.Core.Board;

namespace DeadManZone.Core.Tags
{
    public sealed class AttackTypeProfile
    {
        public AttackType AttackType { get; init; }
        public string TagId { get; init; }
        public string DisplayName { get; init; }
        public string Tooltip { get; init; }
        public float StrongMultiplier { get; init; } = 1.25f;
        public float WeakMultiplier { get; init; } = 0.85f;
        public float NeutralMultiplier { get; init; } = 1.0f;
        public ArmorType? StrongArmor { get; init; }
        public ArmorType? WeakArmor { get; init; }
        public string StrongPrimaryTagId { get; init; }
        public string WeakPrimaryTagId { get; init; }
        public bool StrongVsStructures { get; init; }
    }
}
