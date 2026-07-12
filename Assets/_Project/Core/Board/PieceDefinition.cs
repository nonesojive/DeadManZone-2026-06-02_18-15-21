using System.Collections.Generic;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Board
{
    public sealed class PieceDefinition
    {
        public string Id { get; init; }
        public string DisplayName { get; init; }
        public PieceCategory Category { get; init; }
        public PieceShape Shape { get; init; }
        public string Primary { get; init; }
        public string CombatRole { get; init; }
        public string SystemTag { get; init; }
        public IReadOnlyList<string> SynergyTags { get; init; } = System.Array.Empty<string>();
        public IReadOnlyList<string> AbilityTags { get; init; } = System.Array.Empty<string>();
        public IReadOnlyList<string> FlavorTags { get; init; } = System.Array.Empty<string>();
        public IReadOnlyList<string> Tags { get; init; } = System.Array.Empty<string>();
        public IReadOnlyList<PieceAbilityDefinition> Abilities { get; init; }
            = System.Array.Empty<PieceAbilityDefinition>();
        public int MaxHp { get; init; }
        public int BaseDamage { get; init; }
        public int CooldownTicks { get; init; }
        public int GoldCost { get; init; }
        public int RequisitionCost { get; init; }
        public int ManpowerCost { get; init; }
        public int MusterPerShop { get; init; }
        public ShopModifierFlags ShopModifiers { get; init; }
        public int SalvageChanceBonus { get; init; }
        public CommandActionFlags CommandActions { get; init; }
        public AttackSpeedTier AttackSpeed { get; init; } = AttackSpeedTier.Medium;
        public AttackRangeTier AttackRange { get; init; } = AttackRangeTier.Medium;
        public int MovementSpeed { get; init; } = 2;
        public ArmorType ArmorType { get; init; } = ArmorType.Light;
        public AttackType AttackType { get; init; } = AttackType.Ballistic;
        public GrantedAbility GrantedAbility { get; init; } = GrantedAbility.None;
        public int? AccuracyOverride { get; init; }
        public string FactionId { get; init; } = "neutral";

        /// <summary>Design role, not raw power (M3). Defaults Common so pieces from
        /// assets generated before the rarity pass stay valid.</summary>
        public Rarity Rarity { get; init; } = Rarity.Common;
    }

    [System.Flags]
    public enum ShopModifierFlags
    {
        None = 0,
        ExtraGeneralSlot = 1 << 0,
        GoldDiscount10 = 1 << 1,
        EnemyTagPreview = 1 << 2,
        GuaranteeEngineerOffer = 1 << 3,
        SalvageChanceBoost5 = 1 << 4
    }

    [System.Flags]
    public enum CommandActionFlags
    {
        None = 0,
        ChangeStance = 1 << 0,
        SpendRequisitionBuff = 1 << 1,
        CallStrike = 1 << 2
    }
}
