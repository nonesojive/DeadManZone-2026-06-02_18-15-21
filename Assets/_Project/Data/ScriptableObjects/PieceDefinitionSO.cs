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

        [Header("Combat Stats")]
        public AttackSpeedTier attackSpeed = AttackSpeedTier.Medium;
        public AttackRangeTier attackRange = AttackRangeTier.Medium;
        public MovementSpeedTier movementSpeed = MovementSpeedTier.Medium;
        public ArmorType armorType = ArmorType.Light;
        public AttackType attackType = AttackType.Ballistic;
        public GrantedAbility grantedAbility = GrantedAbility.None;
        public string factionId = "neutral";

        [Header("Visuals")]
        public Sprite icon;
        public Color categoryTint = Color.white;
        [Tooltip("Optional per-cell board sprites keyed by local shape offset (pre-rotation).")]
        public PieceCellSprite[] cellSprites;

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
                Tags = PieceTagQueries.BuildLegacyTags(
                    category,
                    baseDamage,
                    primary,
                    combatRole,
                    systemTag,
                    synergyTags ?? System.Array.Empty<string>(),
                    abilityTags ?? System.Array.Empty<string>(),
                    tags ?? System.Array.Empty<string>()),
                MaxHp = maxHp,
                BaseDamage = baseDamage,
                CooldownTicks = cooldownTicks,
                GoldCost = goldCost,
                RequisitionCost = requisitionCost,
                ManpowerCost = manpowerCost,
                MusterPerShop = musterPerShop,
                ShopModifiers = shopModifiers,
                CommandActions = commandActions,
                AttackSpeed = attackSpeed,
                AttackRange = attackRange,
                MovementSpeed = movementSpeed,
                ArmorType = armorType,
                AttackType = attackType,
                GrantedAbility = grantedAbility,
                FactionId = factionId
            };
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
