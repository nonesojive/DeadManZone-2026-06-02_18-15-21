using System;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using System.Collections.Generic;
using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Piece Definition")]
    public class PieceDefinitionSO : ScriptableObject
    {
        public string id;
        public string displayName;
        public PieceCategory category;
        public Vector2Int[] shapeCells = { Vector2Int.zero };
        public string[] tags;
        public string primary;
        public string combatRole;
        public string systemTag;
        public string[] synergyTags = System.Array.Empty<string>();
        public string[] abilityTags = System.Array.Empty<string>();
        public string[] flavorTags = System.Array.Empty<string>();
        public int maxHp = 10;
        public int baseDamage;
        public int cooldownTicks = 3;
        public int goldCost = 5;
        public int requisitionCost;
        public int manpowerCost = 1;

        [Header("Manpower")]
        public int musterPerShop;
        public ShopModifierFlags shopModifiers;
        public CommandActionFlags commandActions;
        public ShopLane shopLane = ShopLane.Offensive;

        [Header("Shop")]
        public bool includeInShopPool = true;

        [Header("Salvage")]
        public int salvageChanceBonus;

        [Header("Combat Stats")]
        public AttackSpeedTier attackSpeed = AttackSpeedTier.Medium;
        public AttackRangeTier attackRange = AttackRangeTier.Medium;
        public MovementSpeedTier movementSpeed = MovementSpeedTier.Medium;
        public ArmorType armorType = ArmorType.Light;
        public AttackType attackType = AttackType.Ballistic;
        public GrantedAbility grantedAbility = GrantedAbility.None;
        [Tooltip("Optional 0-100. Leave at 0 to use attack type / role defaults.")]
        public int accuracyOverride;
        public string factionId = "neutral";

        [Header("Visuals")]
        public Sprite icon;
        public Color categoryTint = Color.white;
        [Tooltip("Optional per-cell board sprites keyed by local shape offset (pre-rotation).")]
        public PieceCellSprite[] cellSprites;

        [Header("Abilities")]
        public AbilityDefinitionSO[] catalogAbilities = Array.Empty<AbilityDefinitionSO>();
        public PieceAbilityInlineEntry[] customAbilities = Array.Empty<PieceAbilityInlineEntry>();

        [Header("Combat Arena (3D)")]
        [Tooltip("Optional 3D prefab for combat arena presentation. Board/shop still use icon + cellSprites.")]
        public GameObject combatArenaPrefab;
        [Tooltip("Uniform scale applied before height fitting. 1 = asset default.")]
        public float combatArenaModelScale = 1f;
        [Tooltip("Target model height in meters. 0 uses combat arena default (~1.6).")]
        public float combatArenaModelHeight;

        public bool HasCellArt()
        {
            if (cellSprites == null)
                return false;

            foreach (var entry in cellSprites)
            {
                if (entry.sprite != null)
                    return true;
            }

            return false;
        }

        public Sprite TryGetCellSprite(Vector2Int localCell)
        {
            if (cellSprites == null)
                return null;

            foreach (var entry in cellSprites)
            {
                if (entry.localCell == localCell && entry.sprite != null)
                    return entry.sprite;
            }

            return null;
        }

        public PieceDefinition ToCore()
        {
            var cells = new List<GridCoord>();
            foreach (var cell in shapeCells)
                cells.Add(new GridCoord(cell.x, cell.y));

            return new PieceDefinition
            {
                Id = id,
                DisplayName = displayName,
                Category = category,
                Shape = new PieceShape(cells),
                Primary = primary,
                CombatRole = combatRole,
                SystemTag = systemTag,
                SynergyTags = synergyTags ?? System.Array.Empty<string>(),
                AbilityTags = abilityTags ?? System.Array.Empty<string>(),
                FlavorTags = flavorTags ?? System.Array.Empty<string>(),
                Tags = PieceTagQueries.BuildLegacyTags(
                    category,
                    baseDamage,
                    primary,
                    combatRole,
                    systemTag,
                    synergyTags ?? System.Array.Empty<string>(),
                    abilityTags ?? System.Array.Empty<string>(),
                    flavorTags ?? System.Array.Empty<string>(),
                    tags ?? System.Array.Empty<string>()),
                MaxHp = maxHp,
                BaseDamage = baseDamage,
                CooldownTicks = cooldownTicks,
                GoldCost = goldCost,
                RequisitionCost = requisitionCost,
                ManpowerCost = manpowerCost,
                MusterPerShop = musterPerShop,
                ShopModifiers = shopModifiers,
                SalvageChanceBonus = salvageChanceBonus,
                CommandActions = commandActions,
                AttackSpeed = attackSpeed,
                AttackRange = attackRange,
                MovementSpeed = movementSpeed,
                ArmorType = armorType,
                AttackType = attackType,
                GrantedAbility = grantedAbility,
                AccuracyOverride = accuracyOverride <= 0 ? null : accuracyOverride,
                FactionId = factionId,
                Abilities = ResolveAbilities()
            };
        }

        private PieceAbilityDefinition[] ResolveAbilities()
        {
            var abilities = new List<PieceAbilityDefinition>();

            if (catalogAbilities != null)
            {
                foreach (var ability in catalogAbilities)
                {
                    if (ability != null)
                        abilities.Add(ability.ToCore());
                }
            }

            if (customAbilities != null)
            {
                foreach (var entry in customAbilities)
                {
                    if (entry != null)
                        abilities.Add(entry.ToCore());
                }
            }

            return abilities.ToArray();
        }

        private void OnValidate()
        {
            void Warn(string message)
            {
                Debug.LogWarning($"[PieceDefinitionSO:{name}] {message}", this);
            }

            bool IsUnknownTag(string tagId)
            {
                return !TagRegistry.TryGet(tagId, out _);
            }

            if (string.IsNullOrWhiteSpace(primary))
            {
                Warn("Primary tag is empty.");
            }
            else if (IsUnknownTag(primary))
            {
                Warn($"Unknown primary tag '{primary}'.");
            }

            if (!string.IsNullOrWhiteSpace(combatRole) && IsUnknownTag(combatRole))
            {
                Warn($"Unknown combat role tag '{combatRole}'.");
            }

            if (!string.IsNullOrWhiteSpace(systemTag) && IsUnknownTag(systemTag))
            {
                Warn($"Unknown system tag '{systemTag}'.");
            }

            WarnUnknownTagEntries("synergyTags", synergyTags);
            WarnUnknownTagEntries("abilityTags", abilityTags);
            WarnUnknownTagEntries("flavorTags", flavorTags);

            void WarnUnknownTagEntries(string fieldName, string[] tagIds)
            {
                if (tagIds == null)
                    return;

                for (int i = 0; i < tagIds.Length; i++)
                {
                    string tagId = tagIds[i];
                    if (string.IsNullOrWhiteSpace(tagId))
                        continue;

                    if (IsUnknownTag(tagId))
                    {
                        Warn($"Unknown {fieldName} entry at index {i}: '{tagId}'.");
                    }
                }
            }
        }
    }
}
