using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Data.UnitCreation
{
    public enum UnitCreatorMode
    {
        Create,
        Edit
    }

    public sealed class UnitCreationDraft
    {
        public UnitCreatorMode Mode = UnitCreatorMode.Create;
        public string SourceAssetPath;

        public string id = "new_unit";
        public string displayName = "New Unit";
        public string factionId = "neutral";
        public PieceCategory category = PieceCategory.Unit;

        public Vector2Int[] shapeCells = { Vector2Int.zero };

        public string primary = GameTagIds.Infantry;
        public string combatRole = GameTagIds.Assault;
        public List<string> synergyTags = new();
        public List<string> abilityTags = new();
        public List<string> flavorTags = new();

        public int maxHp = 10;
        public int baseDamage;
        public int cooldownTicks = 3;
        public int goldCost = 5;
        public int requisitionCost;
        public int manpowerCost = 1;
        public int musterPerShop;

        public ShopModifierFlags shopModifiers;
        public int salvageChanceBonus;
        public CommandActionFlags commandActions;
        public AttackSpeedTier attackSpeed = AttackSpeedTier.Medium;
        public AttackRangeTier attackRange = AttackRangeTier.Medium;
        public int movementSpeed = 2;
        public ArmorType armorType = ArmorType.Light;
        public AttackType attackType = AttackType.Ballistic;
        public GrantedAbility grantedAbility = GrantedAbility.None;
        public int accuracyOverride;

        public Sprite icon;
        public Color categoryTint = Color.white;
        public PieceCellSprite[] cellSprites = Array.Empty<PieceCellSprite>();

        public bool addToContentDatabase = true;
        public bool includeInShopPool = true;

        public ShopLane ComputedShopLane =>
            ShopLaneResolver.ResolveLane(combatRole);

        public ShopLaneResolveResult ComputedShopLaneDetail =>
            ShopLaneResolver.Resolve(combatRole);

        public static UnitCreationDraft CreateDefault() => new();

        public static UnitCreationDraft FromPiece(PieceDefinitionSO piece, bool editMode)
        {
            if (piece == null)
                throw new ArgumentNullException(nameof(piece));

            return new UnitCreationDraft
            {
                Mode = editMode ? UnitCreatorMode.Edit : UnitCreatorMode.Create,
                id = piece.id,
                displayName = piece.displayName,
                factionId = piece.factionId,
                category = piece.category,
                shapeCells = piece.shapeCells != null && piece.shapeCells.Length > 0
                    ? (Vector2Int[])piece.shapeCells.Clone()
                    : new[] { Vector2Int.zero },
                primary = piece.primary,
                combatRole = piece.combatRole,
                synergyTags = new List<string>(piece.synergyTags ?? Array.Empty<string>()),
                abilityTags = new List<string>(piece.abilityTags ?? Array.Empty<string>()),
                flavorTags = new List<string>(piece.flavorTags ?? Array.Empty<string>()),
                maxHp = piece.maxHp,
                baseDamage = piece.baseDamage,
                cooldownTicks = piece.cooldownTicks,
                goldCost = piece.goldCost,
                requisitionCost = piece.requisitionCost,
                manpowerCost = piece.manpowerCost,
                musterPerShop = piece.musterPerShop,
                shopModifiers = piece.shopModifiers,
                salvageChanceBonus = piece.salvageChanceBonus,
                commandActions = piece.commandActions,
                attackSpeed = piece.attackSpeed,
                attackRange = piece.attackRange,
                movementSpeed = piece.movementSpeed,
                armorType = piece.armorType,
                attackType = piece.attackType,
                grantedAbility = piece.grantedAbility,
                accuracyOverride = piece.accuracyOverride,
                icon = piece.icon,
                categoryTint = piece.categoryTint,
                cellSprites = piece.cellSprites != null
                    ? (PieceCellSprite[])piece.cellSprites.Clone()
                    : Array.Empty<PieceCellSprite>(),
                includeInShopPool = piece.includeInShopPool,
                addToContentDatabase = true
            };
        }

        public UnitCreationDraft CloneForDuplicate()
        {
            var copy = FromPiece(ToTemporaryPiece(), editMode: false);
            copy.id = string.IsNullOrEmpty(id) ? "unit_copy" : $"{id}_copy";
            copy.displayName = string.IsNullOrEmpty(displayName) ? "Unit Copy" : $"{displayName} Copy";
            copy.Mode = UnitCreatorMode.Create;
            copy.SourceAssetPath = null;
            return copy;
        }

        public PieceDefinitionSO ToTemporaryPiece()
        {
            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            ApplyTo(piece, writeRegistration: false);
            return piece;
        }

        public void ApplyTo(PieceDefinitionSO piece, bool writeRegistration)
        {
            piece.id = id?.Trim();
            piece.displayName = displayName?.Trim();
            piece.factionId = factionId;
            piece.category = category;
            piece.shapeCells = shapeCells != null && shapeCells.Length > 0
                ? shapeCells
                : new[] { Vector2Int.zero };
            piece.primary = primary;
            piece.combatRole = combatRole;
            piece.systemTag = ResolveSystemTag();
            piece.synergyTags = synergyTags?.ToArray() ?? Array.Empty<string>();
            piece.abilityTags = abilityTags?.ToArray() ?? Array.Empty<string>();
            piece.flavorTags = flavorTags?.ToArray() ?? Array.Empty<string>();
            piece.maxHp = maxHp;
            piece.baseDamage = baseDamage;
            piece.cooldownTicks = cooldownTicks;
            piece.goldCost = goldCost;
            piece.requisitionCost = requisitionCost;
            piece.manpowerCost = manpowerCost;
            piece.musterPerShop = musterPerShop;
            piece.shopModifiers = shopModifiers;
            piece.salvageChanceBonus = salvageChanceBonus;
            piece.commandActions = commandActions;
            piece.attackSpeed = attackSpeed;
            piece.attackRange = attackRange;
            piece.movementSpeed = movementSpeed;
            piece.armorType = armorType;
            piece.attackType = attackType;
            piece.grantedAbility = grantedAbility;
            piece.accuracyOverride = accuracyOverride;
            piece.icon = icon;
            piece.categoryTint = categoryTint;
            piece.cellSprites = cellSprites ?? Array.Empty<PieceCellSprite>();
            piece.shopLane = ComputedShopLane;
            piece.tags = PieceTagQueries.BuildLegacyTags(
                category,
                baseDamage,
                primary,
                combatRole,
                piece.systemTag,
                piece.synergyTags,
                piece.abilityTags,
                piece.flavorTags);

            if (writeRegistration)
                piece.includeInShopPool = includeInShopPool;
        }

        private string ResolveSystemTag() => string.Empty;
    }
}
