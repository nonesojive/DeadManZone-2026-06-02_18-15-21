using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;

namespace DeadManZone.Core.Tags
{
    public sealed class PieceCardViewModel
    {
        public string DisplayName { get; init; } = string.Empty;
        public int Hp { get; init; }
        public int BaseDamage { get; init; }
        public MovementSpeedTier MovementSpeed { get; init; } = MovementSpeedTier.None;
        public AttackSpeedTier AttackSpeed { get; init; } = AttackSpeedTier.Medium;
        public AttackType AttackType { get; init; } = AttackType.None;
        public ArmorType ArmorType { get; init; } = ArmorType.None;
        public IReadOnlyList<TagDefinition> IdentityTags { get; init; } = Array.Empty<TagDefinition>();
        public IReadOnlyList<TagDefinition> OptionalTags { get; init; } = Array.Empty<TagDefinition>();
        public int OverflowCount { get; init; }
        public int SynergyDamageBonus { get; init; }
        public int SynergyArmorBuffSteps { get; init; }
    }
}
