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
        public int MovementSpeed { get; init; }
        public AttackSpeedTier AttackSpeed { get; init; } = AttackSpeedTier.Medium;
        public int AttackSpeedValue { get; init; }
        public int MovementSpeedValue { get; init; }
        public AttackType AttackType { get; init; } = AttackType.None;
        public ArmorType ArmorType { get; init; } = ArmorType.None;
        public TagDefinition PrimaryTag { get; init; }
        public TagDefinition CombatRoleTag { get; init; }
        public IReadOnlyList<TagDefinition> IdentityTags { get; init; } = Array.Empty<TagDefinition>();
        public IReadOnlyList<TagDefinition> OptionalTags { get; init; } = Array.Empty<TagDefinition>();
        public IReadOnlyList<TagDefinition> ChipTags { get; init; } = Array.Empty<TagDefinition>();
        public int OverflowCount { get; init; }
        public int SynergyDamageBonus { get; init; }
        public int SynergyArmorBuffSteps { get; init; }
        public int SynergyMoveChargeBonus { get; init; }
        public IReadOnlyList<string> SynergyLines { get; init; } = Array.Empty<string>();
        public string CriticalMassHint { get; init; } = string.Empty;
        public string SalvageContext { get; init; } = string.Empty;
        public string AttackTypeTooltip { get; init; } = string.Empty;
        public string ArmorTypeTooltip { get; init; } = string.Empty;
        public IReadOnlyList<string> AbilityLines { get; init; } = Array.Empty<string>();
        public string AbilityText { get; init; } = string.Empty;
    }
}
