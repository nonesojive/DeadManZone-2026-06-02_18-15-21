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
            SaveMappedPiece("ironmarch_hq", "IronMarch High Command", PieceCategory.Building, ShopLane.Defensive,
                new[]
                {
                    new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3),
                    new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(2, 0)
                },
                "iron_vanguard", maxHp: 80, goldCost: 0, manpowerCost: 8, includeInShopPool: false),
            SaveMappedPiece("rifle_squad", "Rifle Squad", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Single, "iron_vanguard",
                maxHp: 100, baseDamage: 20, manpowerCost: 10, goldCost: 5,
                attackType: AttackType.Ballistic, armorType: ArmorType.Light),
            SaveMappedPiece("diesel_walker", "Diesel Walker", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Walker, "iron_vanguard",
                maxHp: 250, baseDamage: 32, cooldownTicks: 5, goldCost: 12,
                attackType: AttackType.Piercing, armorType: ArmorType.Heavy, movementSpeed: MovementSpeedTier.Low),
            SaveMappedPiece("radio_array", "Radio Array", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.Single, "iron_vanguard", maxHp: 120, goldCost: 7,
                shopModifiers: ShopModifierFlags.EnemyTagPreview),
            SaveMappedPiece("mg_team", "MG Team", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.HorizontalPair, "iron_vanguard",
                maxHp: 120, baseDamage: 24, manpowerCost: 12, cooldownTicks: 4, goldCost: 8,
                attackType: AttackType.Shredding, armorType: ArmorType.Medium, attackSpeed: AttackSpeedTier.Fast),
            SaveMappedPiece("field_gun_nest", "Field Gun Nest", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.VerticalPair, "iron_vanguard",
                maxHp: 180, baseDamage: 24, goldCost: 9,
                attackType: AttackType.Explosive, armorType: ArmorType.Medium, attackRange: AttackRangeTier.Long),
            SaveMappedPiece("supply_depot", "Supply Depot", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.Single, "iron_vanguard", maxHp: 50, goldCost: 6, manpowerCost: 0, musterPerShop: 3,
                shopModifiers: ShopModifierFlags.GoldDiscount10),
            SaveMappedPiece("field_workshop", "Field Workshop", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.Single, "iron_vanguard", maxHp: 120, goldCost: 7, musterPerShop: 2,
                shopModifiers: ShopModifierFlags.GuaranteeEngineerOffer),
            SaveMappedPiece("mobile_artillery", "Mobile Artillery", PieceCategory.Hybrid, ShopLane.Specialty,
                DemoSandboxShapes.HorizontalPair, "iron_vanguard",
                maxHp: 160, baseDamage: 40, goldCost: 10, requisitionCost: 2,
                attackType: AttackType.Explosive, armorType: ArmorType.Medium, attackRange: AttackRangeTier.Long),
            SaveMappedPiece("ironmarch_heavy_tank", "IronMarch Heavy Tank", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Square2x2, "iron_vanguard",
                maxHp: 320, baseDamage: 36, cooldownTicks: 6, goldCost: 14, manpowerCost: 14,
                attackType: AttackType.Piercing, armorType: ArmorType.Heavy, movementSpeed: MovementSpeedTier.Low),
            SaveMappedPiece("ironmarch_mortar", "IronMarch Mortar", PieceCategory.Unit, ShopLane.Specialty,
                DemoSandboxShapes.VerticalPair, "iron_vanguard",
                maxHp: 90, baseDamage: 34, cooldownTicks: 5, goldCost: 8, requisitionCost: 1,
                attackType: AttackType.Explosive, armorType: ArmorType.Light, attackRange: AttackRangeTier.Long,
                attackSpeed: AttackSpeedTier.Slow),
            SaveMappedPiece("ironmarch_engineer", "Combat Engineer", PieceCategory.Unit, ShopLane.Defensive,
                DemoSandboxShapes.Single, "iron_vanguard",
                maxHp: 80, baseDamage: 8, goldCost: 6, manpowerCost: 6,
                synergyTags: new[] { GameTagIds.Mechanic },
                attackType: AttackType.Melee, armorType: ArmorType.Medium),
            SaveMappedPiece("ironmarch_breacher", "Assault Breacher", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Single, "iron_vanguard",
                maxHp: 110, baseDamage: 26, goldCost: 7, manpowerCost: 8,
                abilityTags: new[] { GameTagIds.Flamethrower },
                attackType: AttackType.Fire, armorType: ArmorType.Medium, movementSpeed: MovementSpeedTier.High),
            SaveMappedPiece("ironmarch_sniper", "Marksman Detachment", PieceCategory.Unit, ShopLane.Specialty,
                DemoSandboxShapes.Single, "iron_vanguard",
                maxHp: 70, baseDamage: 28, goldCost: 7, manpowerCost: 6, cooldownTicks: 4,
                attackType: AttackType.Ballistic, armorType: ArmorType.Light,
                attackRange: AttackRangeTier.Long, attackSpeed: AttackSpeedTier.Slow,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Sniper, GameTagIds.Combatant)),
            SaveMappedPiece("ironmarch_defender", "Bulwark Squad", PieceCategory.Unit, ShopLane.Defensive,
                DemoSandboxShapes.HorizontalPair, "iron_vanguard",
                maxHp: 140, baseDamage: 14, goldCost: 6, manpowerCost: 10,
                attackType: AttackType.Melee, armorType: ArmorType.Heavy, movementSpeed: MovementSpeedTier.Low,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Defender, GameTagIds.Combatant))
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
                grantedAbility: GrantedAbility.GrenadeLob,
                abilityTags: new[] { GameTagIds.Grenadier },
                attackType: AttackType.Explosive, armorType: ArmorType.Light),
            SaveMappedPiece("armored_transport", "Armored Transport", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.TransportL, "neutral",
                maxHp: 120, baseDamage: 8, manpowerCost: 8, goldCost: 8,
                grantedAbility: GrantedAbility.ShieldAllies,
                attackType: AttackType.Ballistic, armorType: ArmorType.Medium, movementSpeed: MovementSpeedTier.Medium),
            SaveMappedPiece("mobile_cannon", "Mobile Cannon", PieceCategory.Hybrid, ShopLane.Specialty,
                DemoSandboxShapes.SiegePlate, "neutral",
                maxHp: 180, baseDamage: 36, goldCost: 10, requisitionCost: 2, manpowerCost: 10,
                grantedAbility: GrantedAbility.CannonBlast,
                attackType: AttackType.Explosive, armorType: ArmorType.Heavy, attackRange: AttackRangeTier.Long,
                attackSpeed: AttackSpeedTier.Slow, movementSpeed: MovementSpeedTier.Low),
            SaveMappedPiece("field_medic", "Field Medic", PieceCategory.Unit, ShopLane.Defensive,
                DemoSandboxShapes.Single, "neutral",
                maxHp: 40, baseDamage: 0, manpowerCost: 4, goldCost: 5,
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
                attackType: AttackType.Fire, armorType: ArmorType.Medium, movementSpeed: MovementSpeedTier.High),
            SaveMappedPiece("neutral_mortar_team", "Mortar Team", PieceCategory.Unit, ShopLane.Specialty,
                DemoSandboxShapes.HorizontalPair, "neutral",
                maxHp: 75, baseDamage: 30, goldCost: 6, requisitionCost: 1, manpowerCost: 8,
                attackType: AttackType.Explosive, armorType: ArmorType.Light, attackRange: AttackRangeTier.Long,
                attackSpeed: AttackSpeedTier.Slow,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Artillery, GameTagIds.Combatant)),
            SaveMappedPiece("marksman_squad", "Marksman Squad", PieceCategory.Unit, ShopLane.Specialty,
                DemoSandboxShapes.Single, "neutral",
                maxHp: 55, baseDamage: 26, goldCost: 6, manpowerCost: 5, cooldownTicks: 4,
                attackType: AttackType.Ballistic, armorType: ArmorType.Light,
                attackRange: AttackRangeTier.Long, attackSpeed: AttackSpeedTier.Slow,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Sniper, GameTagIds.Combatant))
        };

        private static PieceDefinitionSO[] CreateDustScourgePieces() => new[]
        {
            SaveMappedPiece("dust_hq", "Nomad Command", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.HorizontalPair, "dust_scourge", maxHp: 220, goldCost: 0, manpowerCost: 0,
                includeInShopPool: false),
            SaveMappedPiece("sand_raider", "Sand Raider", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Single, "dust_scourge",
                maxHp: 90, baseDamage: 24, cooldownTicks: 2, goldCost: 6,
                attackType: AttackType.Melee, armorType: ArmorType.Light, movementSpeed: MovementSpeedTier.High),
            SaveMappedPiece("scrap_rig", "Scrap Rig", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.HorizontalPair, "dust_scourge",
                maxHp: 160, baseDamage: 16, goldCost: 7,
                attackType: AttackType.Shredding, armorType: ArmorType.Medium),
            SaveMappedPiece("toxin_launcher", "Toxin Launcher", PieceCategory.Hybrid, ShopLane.Specialty,
                DemoSandboxShapes.Single, "dust_scourge",
                maxHp: 100, baseDamage: 32, goldCost: 9, requisitionCost: 2,
                grantedAbility: GrantedAbility.GrenadeLob,
                attackType: AttackType.Gas, armorType: ArmorType.Light)
        };

        private static PieceDefinitionSO[] CreateCartelPieces() => new[]
        {
            SaveMappedPiece("echo_hq", "Echo Nexus", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.HorizontalPair, "cartel_of_echoes", maxHp: 200, goldCost: 0, manpowerCost: 0,
                includeInShopPool: false),
            SaveMappedPiece("phantom_agent", "Phantom Agent", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Single, "cartel_of_echoes",
                maxHp: 70, baseDamage: 24, cooldownTicks: 2, goldCost: 7,
                abilityTags: new[] { GameTagIds.Stealth },
                attackType: AttackType.Piercing, armorType: ArmorType.Light,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Sniper, GameTagIds.Combatant)),
            SaveMappedPiece("signal_relay", "Signal Relay", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.Single, "cartel_of_echoes",
                maxHp: 110, goldCost: 6, musterPerShop: 1, shopModifiers: ShopModifierFlags.EnemyTagPreview),
            SaveMappedPiece("resonance_cannon", "Resonance Cannon", PieceCategory.Hybrid, ShopLane.Specialty,
                DemoSandboxShapes.HorizontalPair, "cartel_of_echoes",
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
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Sniper, GameTagIds.Combatant)),
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
            MovementSpeedTier movementSpeed = MovementSpeedTier.Medium,
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
