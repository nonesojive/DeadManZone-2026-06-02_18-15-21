using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    internal static class DemoPieceFactory
    {
        public static PieceDefinitionSO[] CreateAll()
        {
            var list = new System.Collections.Generic.List<PieceDefinitionSO>();
            list.AddRange(CreateIronmarchPieces());
            list.AddRange(CreateNeutralPieces());
            list.AddRange(CreateDustScourgePieces());
            list.AddRange(CreateCartelPieces());
            list.AddRange(CreateCrimsonLegionPieces());
            list.AddRange(CreateAshWraithPieces());
            return list.ToArray();
        }

        private static PieceDefinitionSO[] CreateIronmarchPieces() => new[]
        {
            SaveMappedPiece("rifle_squad", "Rifle Squad", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Single, FactionIds.IronmarchUnion,
                maxHp: 100, baseDamage: 20, manpowerCost: 10, goldCost: 5,
                attackType: AttackType.Ballistic, armorType: ArmorType.Light),
            SaveMappedPiece("diesel_walker", "Diesel Walker", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Walker, FactionIds.IronmarchUnion,
                maxHp: 250, baseDamage: 32, cooldownTicks: 5, goldCost: 12,
                attackType: AttackType.Piercing, armorType: ArmorType.Heavy, movementSpeed: 1),
            SaveMappedPiece("radio_array", "Radio Array", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.Single, FactionIds.IronmarchUnion, maxHp: 120, goldCost: 7,
                shopModifiers: ShopModifierFlags.EnemyTagPreview),
            SaveMappedPiece("mg_team", "MG Team", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.HorizontalPair, FactionIds.IronmarchUnion,
                maxHp: 120, baseDamage: 24, manpowerCost: 12, cooldownTicks: 4, goldCost: 8,
                attackType: AttackType.Shredding, armorType: ArmorType.Medium, attackSpeed: AttackSpeedTier.Fast),
            SaveMappedPiece("field_gun_nest", "Field Gun Nest", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.VerticalPair, FactionIds.IronmarchUnion,
                maxHp: 180, baseDamage: 24, goldCost: 9,
                attackType: AttackType.Explosive, armorType: ArmorType.Medium, attackRange: AttackRangeTier.Long),
            SaveMappedPiece("supply_depot", "Supply Depot", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.Single, FactionIds.IronmarchUnion, maxHp: 50, goldCost: 6, manpowerCost: 0, musterPerShop: 3,
                shopModifiers: ShopModifierFlags.GoldDiscount10),
            SaveMappedPiece("field_workshop", "Field Workshop", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.Single, FactionIds.IronmarchUnion, maxHp: 120, goldCost: 7, musterPerShop: 2,
                shopModifiers: ShopModifierFlags.GuaranteeEngineerOffer),
            SaveMappedPiece("mobile_artillery", "Mobile Artillery", PieceCategory.Hybrid, ShopLane.Specialty,
                DemoSandboxShapes.HorizontalPair, FactionIds.IronmarchUnion,
                maxHp: 160, baseDamage: 40, goldCost: 10, requisitionCost: 2,
                attackType: AttackType.Explosive, armorType: ArmorType.Medium, attackRange: AttackRangeTier.Long),
            SaveMappedPiece("ironmarch_heavy_tank", "IronMarch Heavy Tank", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Square2x2, FactionIds.IronmarchUnion,
                maxHp: 320, baseDamage: 36, cooldownTicks: 6, goldCost: 14, manpowerCost: 14,
                attackType: AttackType.Piercing, armorType: ArmorType.Heavy, movementSpeed: 1),
            SaveMappedPiece("ironmarch_mortar", "IronMarch Mortar", PieceCategory.Unit, ShopLane.Specialty,
                DemoSandboxShapes.VerticalPair, FactionIds.IronmarchUnion,
                maxHp: 90, baseDamage: 34, cooldownTicks: 5, goldCost: 8, requisitionCost: 1,
                attackType: AttackType.Explosive, armorType: ArmorType.Light, attackRange: AttackRangeTier.Long,
                attackSpeed: AttackSpeedTier.Slow),
            SaveMappedPiece("ironmarch_engineer", "Combat Engineer", PieceCategory.Unit, ShopLane.Defensive,
                DemoSandboxShapes.Single, FactionIds.IronmarchUnion,
                maxHp: 80, baseDamage: 8, goldCost: 6, manpowerCost: 6,
                synergyTags: new[] { GameTagIds.Mechanic },
                attackType: AttackType.Melee, armorType: ArmorType.Medium),
            SaveMappedPiece("ironmarch_breacher", "Assault Breacher", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Single, FactionIds.IronmarchUnion,
                maxHp: 110, baseDamage: 26, goldCost: 7, manpowerCost: 8,
                abilityTags: new[] { GameTagIds.Flamethrower },
                attackType: AttackType.Fire, armorType: ArmorType.Medium, movementSpeed: 4),
            SaveMappedPiece("ironmarch_sniper", "Marksman Detachment", PieceCategory.Unit, ShopLane.Specialty,
                DemoSandboxShapes.Single, FactionIds.IronmarchUnion,
                maxHp: 70, baseDamage: 28, goldCost: 7, manpowerCost: 6, cooldownTicks: 4,
                attackType: AttackType.Ballistic, armorType: ArmorType.Light,
                attackRange: AttackRangeTier.Long, attackSpeed: AttackSpeedTier.Slow,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Sniper, string.Empty)),
            SaveMappedPiece("ironmarch_defender", "Bulwark Squad", PieceCategory.Unit, ShopLane.Defensive,
                DemoSandboxShapes.HorizontalPair, FactionIds.IronmarchUnion,
                maxHp: 140, baseDamage: 14, goldCost: 6, manpowerCost: 10,
                attackType: AttackType.Melee, armorType: ArmorType.Heavy, movementSpeed: 1,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Defender, string.Empty))
        };

        private static PieceDefinitionSO[] CreateNeutralPieces() => new[]
        {
            SaveMappedPiece("conscript_rifleman", "Conscript Rifleman", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Single, "neutral",
                maxHp: 60, baseDamage: 12, manpowerCost: 6, goldCost: 4,
                attackType: AttackType.Ballistic, armorType: ArmorType.Light),
            SaveMappedPiece("grenade_thrower", "Grenade Thrower", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.VerticalPair, "neutral",
                maxHp: 70, baseDamage: 24, cooldownTicks: 4, goldCost: 5, manpowerCost: 8,
                grantedAbility: GrantedAbility.MortarShot,
                abilityTags: new[] { GameTagIds.Grenadier },
                attackType: AttackType.Explosive, armorType: ArmorType.Light),
            SaveMappedPiece("armored_transport", "Armored Transport", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.TransportL, "neutral",
                maxHp: 120, baseDamage: 8, manpowerCost: 8, goldCost: 8,
                grantedAbility: GrantedAbility.ShieldAllies,
                attackType: AttackType.Ballistic, armorType: ArmorType.Medium, movementSpeed: 2),
            SaveMappedPiece("mobile_cannon", "Mobile Cannon", PieceCategory.Hybrid, ShopLane.Specialty,
                DemoSandboxShapes.SiegePlate, "neutral",
                maxHp: 180, baseDamage: 36, goldCost: 10, requisitionCost: 2, manpowerCost: 10,
                grantedAbility: GrantedAbility.CannonBlast,
                attackType: AttackType.Explosive, armorType: ArmorType.Heavy, attackRange: AttackRangeTier.Long,
                attackSpeed: AttackSpeedTier.Slow, movementSpeed: 1),
            SaveMappedPiece("field_medic", "Field Medic", PieceCategory.Unit, ShopLane.Defensive,
                DemoSandboxShapes.Single, "neutral",
                maxHp: 40, baseDamage: 0, manpowerCost: 4, goldCost: 5,
                grantedAbility: GrantedAbility.ShieldAllies,
                synergyTags: new[] { GameTagIds.Medic },
                attackType: AttackType.None, armorType: ArmorType.Light),
            SaveMappedPiece("neutral_supply_depot", "Neutral Supply Depot", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.Single, "neutral", maxHp: 55, goldCost: 5, manpowerCost: 0, musterPerShop: 2,
                shopModifiers: ShopModifierFlags.GoldDiscount10, salvageChanceBonus: 5),
            SaveMappedPiece("neutral_field_gun", "Neutral Field Gun", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.VerticalPair, "neutral",
                maxHp: 120, baseDamage: 22, goldCost: 7,
                attackType: AttackType.Explosive, armorType: ArmorType.Medium, attackRange: AttackRangeTier.Medium),
            SaveMappedPiece("shock_trooper", "Shock Trooper", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Single, "neutral",
                maxHp: 85, baseDamage: 22, goldCost: 6, manpowerCost: 7,
                abilityTags: new[] { GameTagIds.Flamethrower },
                attackType: AttackType.Fire, armorType: ArmorType.Medium, movementSpeed: 4),
            SaveMappedPiece("neutral_mortar_team", "Mortar Team", PieceCategory.Unit, ShopLane.Specialty,
                DemoSandboxShapes.HorizontalPair, "neutral",
                maxHp: 75, baseDamage: 30, goldCost: 6, requisitionCost: 1, manpowerCost: 8,
                attackType: AttackType.Explosive, armorType: ArmorType.Light, attackRange: AttackRangeTier.Long,
                attackSpeed: AttackSpeedTier.Slow,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Artillery, string.Empty)),
            SaveMappedPiece("marksman_squad", "Marksman Squad", PieceCategory.Unit, ShopLane.Specialty,
                DemoSandboxShapes.Single, "neutral",
                maxHp: 55, baseDamage: 26, goldCost: 6, manpowerCost: 5, cooldownTicks: 4,
                attackType: AttackType.Ballistic, armorType: ArmorType.Light,
                attackRange: AttackRangeTier.Long, attackSpeed: AttackSpeedTier.Slow,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Sniper, string.Empty))
        };

        private static PieceDefinitionSO[] CreateDustScourgePieces() => new[]
        {
            SaveMappedPiece("sand_raider", "Sand Raider", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Single, FactionIds.DustScourge,
                maxHp: 90, baseDamage: 24, cooldownTicks: 2, goldCost: 6,
                attackType: AttackType.Melee, armorType: ArmorType.Light, movementSpeed: 4),
            SaveMappedPiece("scrap_rig", "Scrap Rig", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.HorizontalPair, FactionIds.DustScourge,
                maxHp: 160, baseDamage: 16, goldCost: 7,
                attackType: AttackType.Shredding, armorType: ArmorType.Medium),
            SaveMappedPiece("toxin_launcher", "Toxin Launcher", PieceCategory.Hybrid, ShopLane.Specialty,
                DemoSandboxShapes.Single, FactionIds.DustScourge,
                maxHp: 100, baseDamage: 32, goldCost: 9, requisitionCost: 2,
                grantedAbility: GrantedAbility.MortarShot,
                attackType: AttackType.Gas, armorType: ArmorType.Light)
        };

        private static PieceDefinitionSO[] CreateCartelPieces() => new[]
        {
            SaveMappedPiece("phantom_agent", "Phantom Agent", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Single, FactionIds.CartelOfEchoes,
                maxHp: 70, baseDamage: 24, cooldownTicks: 2, goldCost: 7,
                abilityTags: new[] { GameTagIds.Stealth },
                attackType: AttackType.Piercing, armorType: ArmorType.Light,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Sniper, string.Empty)),
            SaveMappedPiece("signal_relay", "Signal Relay", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.Single, FactionIds.CartelOfEchoes,
                maxHp: 110, goldCost: 6, musterPerShop: 1, shopModifiers: ShopModifierFlags.EnemyTagPreview),
            SaveMappedPiece("resonance_cannon", "Resonance Cannon", PieceCategory.Hybrid, ShopLane.Specialty,
                DemoSandboxShapes.HorizontalPair, FactionIds.CartelOfEchoes,
                maxHp: 130, baseDamage: 40, goldCost: 10, requisitionCost: 2,
                attackType: AttackType.Explosive, armorType: ArmorType.Medium, attackRange: AttackRangeTier.Long)
        };

        private static PieceDefinitionSO[] CreateCrimsonLegionPieces() => new[]
        {
            SaveMappedPiece("crimson_elite", "Crimson Elite", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Single, "crimson_legion",
                maxHp: 120, baseDamage: 24, goldCost: 0,
                attackType: AttackType.Melee, armorType: ArmorType.Medium),
            SaveMappedPiece("crimson_tank", "Crimson Tank", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Walker, "crimson_legion",
                maxHp: 280, baseDamage: 40, goldCost: 0,
                attackType: AttackType.Piercing, armorType: ArmorType.Heavy),
            SaveMappedPiece("crimson_artillery", "Crimson Battery", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.HorizontalPair, "crimson_legion",
                maxHp: 200, baseDamage: 32, goldCost: 0,
                attackType: AttackType.Explosive, armorType: ArmorType.Heavy, attackRange: AttackRangeTier.Long)
        };

        private static PieceDefinitionSO[] CreateAshWraithPieces() => new[]
        {
            SaveMappedPiece("wraith_stalker", "Wraith Stalker", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Single, "ash_wraiths",
                maxHp: 80, baseDamage: 32, goldCost: 0,
                abilityTags: new[] { GameTagIds.Stealth },
                attackType: AttackType.Piercing, armorType: ArmorType.Light,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Sniper, string.Empty)),
            SaveMappedPiece("wraith_phantom", "Ash Phantom", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Single, "ash_wraiths",
                maxHp: 100, baseDamage: 24, goldCost: 0,
                attackType: AttackType.Shredding, armorType: ArmorType.Light),
            SaveMappedPiece("wraith_bombard", "Grave Bombard", PieceCategory.Hybrid, ShopLane.Specialty,
                DemoSandboxShapes.HorizontalPair, "ash_wraiths",
                maxHp: 150, baseDamage: 40, goldCost: 0,
                attackType: AttackType.Explosive, armorType: ArmorType.Medium, attackRange: AttackRangeTier.Long)
        };

        private static PieceDefinitionSO SaveMappedPiece(
            string id,
            string displayName,
            PieceCategory category,
            ShopLane lane,
            Vector2Int[] shape,
            string factionId = "neutral",
            int maxHp = 10,
            int baseDamage = 0,
            int cooldownTicks = 3,
            int goldCost = 5,
            int requisitionCost = 0,
            int manpowerCost = 1,
            int musterPerShop = 0,
            ShopModifierFlags shopModifiers = ShopModifierFlags.None,
            CommandActionFlags commandActions = CommandActionFlags.None,
            GrantedAbility grantedAbility = GrantedAbility.None,
            string[] synergyTags = null,
            string[] abilityTags = null,
            int salvageChanceBonus = 0,
            AttackType attackType = AttackType.Ballistic,
            ArmorType armorType = ArmorType.Light,
            AttackSpeedTier attackSpeed = AttackSpeedTier.Medium,
            AttackRangeTier attackRange = AttackRangeTier.Medium,
            int movementSpeed = 2,
            bool includeInShopPool = true,
            TagContentMigrator.PieceTagMapping? mappingOverride = null)
        {
            var mapping = mappingOverride ?? TagContentMigrator.GetMappingOrThrow(id);
            var piece = DemoContentGenerator.SavePiece(
                id,
                displayName,
                category,
                lane,
                shape,
                primary: mapping.Primary,
                combatRole: mapping.CombatRole,
                systemTag: mapping.SystemTag,
                synergyTags: synergyTags ?? System.Array.Empty<string>(),
                factionId: factionId,
                maxHp: maxHp,
                baseDamage: baseDamage,
                cooldownTicks: cooldownTicks,
                goldCost: goldCost,
                requisitionCost: requisitionCost,
                manpowerCost: manpowerCost,
                musterPerShop: musterPerShop,
                shopModifiers: shopModifiers,
                commandActions: commandActions,
                grantedAbility: grantedAbility,
                abilityTags: abilityTags,
                salvageChanceBonus: salvageChanceBonus,
                attackType: attackType,
                armorType: armorType,
                attackSpeed: attackSpeed,
                attackRange: attackRange,
                movementSpeed: movementSpeed);

            piece.includeInShopPool = includeInShopPool;
            EditorUtility.SetDirty(piece);
            return piece;
        }
    }
}
