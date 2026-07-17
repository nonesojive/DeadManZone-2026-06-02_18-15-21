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
            return list.ToArray();
        }

        // 2026-07-15 faction-roster-v1-design.md §2.1 (Neutral) + §2.2 (IronMarch Union).
        // This "5 Factions" demo pipeline is superseded for IronMarch/Neutral by
        // IronmarchUnionContentFactory (which deletes and regenerates the same ids with
        // richer abilities) — kept in sync here only so this menu command still compiles
        // and produces a self-consistent roster if run on its own. Stats are PROVISIONAL
        // (balance pass pending), mirroring IronmarchUnionContentFactory.Pieces.cs; no
        // custom abilities here since SaveMappedPiece/DemoContentGenerator.SavePiece never
        // wired customAbilities for this pipeline (pre-existing constraint, not new).
        private static PieceDefinitionSO[] CreateIronmarchPieces() => new[]
        {
            SaveMappedPiece("conscript_rifles", "Conscript Rifles", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Single, FactionIds.IronmarchUnion,
                maxHp: 50, baseDamage: 5, manpowerCost: 1,
                attackType: AttackType.Ballistic, armorType: ArmorType.None,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Assault, string.Empty)),
            SaveMappedPiece("line_grenadiers", "Line Grenadiers", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.HorizontalPair, FactionIds.IronmarchUnion,
                maxHp: 45, baseDamage: 8, manpowerCost: 1,
                attackType: AttackType.Explosive, armorType: ArmorType.None,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Assault, string.Empty)),
            SaveMappedPiece("field_mortar_team", "Field Mortar Team", PieceCategory.Unit, ShopLane.Specialty,
                DemoSandboxShapes.VerticalPair, FactionIds.IronmarchUnion,
                maxHp: 30, baseDamage: 7, manpowerCost: 2,
                attackType: AttackType.Explosive, armorType: ArmorType.None, attackRange: AttackRangeTier.Long,
                attackSpeed: AttackSpeedTier.Slow, movementSpeed: 1,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Artillery, string.Empty)),
            SaveMappedPiece("sharpshooter", "Sharpshooter", PieceCategory.Unit, ShopLane.Specialty,
                DemoSandboxShapes.Single, FactionIds.IronmarchUnion,
                maxHp: 30, baseDamage: 6, manpowerCost: 1,
                attackType: AttackType.Piercing, armorType: ArmorType.None,
                attackRange: AttackRangeTier.Long, attackSpeed: AttackSpeedTier.Slow,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Sniper, string.Empty)),
            SaveMappedPiece("iron_guard", "Iron Guard", PieceCategory.Unit, ShopLane.Defensive,
                DemoSandboxShapes.Single, FactionIds.IronmarchUnion,
                maxHp: 70, baseDamage: 4, manpowerCost: 2,
                attackType: AttackType.Ballistic, armorType: ArmorType.Medium, movementSpeed: 1,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Defender, string.Empty)),
            SaveMappedPiece("command_outpost", "Command Outpost", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.HorizontalPair, FactionIds.IronmarchUnion, maxHp: 40, manpowerCost: 0,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Building, GameTagIds.Utility, string.Empty)),
            SaveMappedPiece("forward_observer", "Forward Observer", PieceCategory.Unit, ShopLane.Defensive,
                DemoSandboxShapes.Single, FactionIds.IronmarchUnion,
                maxHp: 25, baseDamage: 0, manpowerCost: 1, attackType: AttackType.None, armorType: ArmorType.None,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Support, string.Empty),
                rarity: Rarity.Uncommon),
            SaveMappedPiece("shock_sergeant", "Shock Sergeant", PieceCategory.Unit, ShopLane.Defensive,
                DemoSandboxShapes.Single, FactionIds.IronmarchUnion,
                maxHp: 35, baseDamage: 5, manpowerCost: 1,
                attackType: AttackType.Ballistic, armorType: ArmorType.Light,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Utility, string.Empty),
                rarity: Rarity.Uncommon),
            SaveMappedPiece("artillery_park", "Artillery Park", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.TransportL, FactionIds.IronmarchUnion, maxHp: 90, manpowerCost: 0,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Building, GameTagIds.Utility, string.Empty),
                rarity: Rarity.Uncommon),
            SaveMappedPiece("breakthrough_tank", "Breakthrough Tank", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Square2x2, FactionIds.IronmarchUnion,
                maxHp: 90, baseDamage: 8, manpowerCost: 4,
                attackType: AttackType.Ballistic, armorType: ArmorType.Heavy, movementSpeed: 1,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Vehicle, GameTagIds.Tank, string.Empty),
                rarity: Rarity.Rare),
            SaveMappedPiece("grand_battery", "Grand Battery", PieceCategory.Unit, ShopLane.Specialty,
                DemoSandboxShapes.Square2x2, FactionIds.IronmarchUnion,
                maxHp: 110, baseDamage: 10, manpowerCost: 3,
                grantedAbility: GrantedAbility.MortarShot,
                attackType: AttackType.Explosive, armorType: ArmorType.Light, attackRange: AttackRangeTier.Long,
                attackSpeed: AttackSpeedTier.Slow,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Structure, GameTagIds.Artillery, string.Empty),
                rarity: Rarity.Rare),
            SaveMappedPiece("marksman_doctrine_officer", "Marksman-Doctrine Officer", PieceCategory.Unit, ShopLane.Specialty,
                DemoSandboxShapes.Single, FactionIds.IronmarchUnion,
                maxHp: 35, baseDamage: 6, manpowerCost: 2, cooldownTicks: 4,
                abilityTags: new[] { GameTagIds.Stealth },
                attackType: AttackType.Piercing, armorType: ArmorType.None,
                attackRange: AttackRangeTier.Long, attackSpeed: AttackSpeedTier.Slow, movementSpeed: 3,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Sniper, string.Empty),
                rarity: Rarity.Rare)
        };

        private static PieceDefinitionSO[] CreateNeutralPieces() => new[]
        {
            SaveMappedPiece("militia_squad", "Militia Squad", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.HorizontalPair, "neutral",
                maxHp: 45, baseDamage: 5, manpowerCost: 1,
                attackType: AttackType.Ballistic, armorType: ArmorType.None,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Assault, string.Empty)),
            SaveMappedPiece("field_medic", "Field Medic", PieceCategory.Unit, ShopLane.Defensive,
                DemoSandboxShapes.Single, "neutral",
                maxHp: 30, baseDamage: 3, manpowerCost: 1,
                synergyTags: new[] { GameTagIds.Medic },
                attackType: AttackType.Ballistic, armorType: ArmorType.None,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Support, string.Empty,
                    synergyTags: new[] { GameTagIds.Medic })),
            SaveMappedPiece("supply_depot", "Supply Depot", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.HorizontalPair, "neutral", maxHp: 50, manpowerCost: 0,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Building, GameTagIds.Utility, string.Empty)),
            SaveMappedPiece("recruitment_office", "Recruitment Office", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.HorizontalPair, "neutral", maxHp: 35, manpowerCost: 0, musterPerShop: 1,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Building, GameTagIds.Utility, string.Empty)),
            SaveMappedPiece("machine_gun_nest", "Machine Gun Nest", PieceCategory.Unit, ShopLane.Defensive,
                DemoSandboxShapes.HorizontalPair, "neutral",
                maxHp: 100, baseDamage: 2, manpowerCost: 2,
                attackType: AttackType.Ballistic, armorType: ArmorType.Light,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Structure, GameTagIds.Defender, string.Empty),
                rarity: Rarity.Uncommon),
            SaveMappedPiece("trench_works", "Trench Works", PieceCategory.Unit, ShopLane.Defensive,
                DemoSandboxShapes.TransportL, "neutral",
                maxHp: 140, baseDamage: 0, manpowerCost: 2, attackType: AttackType.None, armorType: ArmorType.Light,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Structure, GameTagIds.Defender, string.Empty),
                rarity: Rarity.Uncommon),
            SaveMappedPiece("field_hospital", "Field Hospital", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.TransportL, "neutral", maxHp: 60, manpowerCost: 0,
                synergyTags: new[] { GameTagIds.Medic },
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Building, GameTagIds.Support, string.Empty),
                rarity: Rarity.Uncommon)
        };

        private static PieceDefinitionSO[] CreateDustScourgePieces() => new[]
        {
            SaveMappedPiece("sand_raider", "Sand Raider", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Single, FactionIds.DustScourge,
                maxHp: 90, baseDamage: 24, cooldownTicks: 2, 
                attackType: AttackType.Melee, armorType: ArmorType.Light, movementSpeed: 4),
            SaveMappedPiece("scrap_rig", "Scrap Rig", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.HorizontalPair, FactionIds.DustScourge,
                maxHp: 160, baseDamage: 16, 
                attackType: AttackType.Shredding, armorType: ArmorType.Medium,
                rarity: Rarity.Uncommon),
            SaveMappedPiece("toxin_launcher", "Toxin Launcher", PieceCategory.Hybrid, ShopLane.Specialty,
                DemoSandboxShapes.Single, FactionIds.DustScourge,
                maxHp: 100, baseDamage: 32, requisitionCost: 2,
                grantedAbility: GrantedAbility.MortarShot,
                attackType: AttackType.Gas, armorType: ArmorType.Light,
                rarity: Rarity.Rare)
        };

        private static PieceDefinitionSO[] CreateCartelPieces() => new[]
        {
            SaveMappedPiece("phantom_agent", "Phantom Agent", PieceCategory.Unit, ShopLane.Offensive,
                DemoSandboxShapes.Single, FactionIds.CartelOfEchoes,
                maxHp: 70, baseDamage: 24, cooldownTicks: 2, 
                abilityTags: new[] { GameTagIds.Stealth },
                attackType: AttackType.Piercing, armorType: ArmorType.Light,
                mappingOverride: new TagContentMigrator.PieceTagMapping(GameTagIds.Infantry, GameTagIds.Sniper, string.Empty),
                rarity: Rarity.Uncommon),
            SaveMappedPiece("signal_relay", "Signal Relay", PieceCategory.Building, ShopLane.Defensive,
                DemoSandboxShapes.Single, FactionIds.CartelOfEchoes,
                maxHp: 110, musterPerShop: 1, shopModifiers: ShopModifierFlags.EnemyTagPreview,
                rarity: Rarity.Uncommon),
            SaveMappedPiece("resonance_cannon", "Resonance Cannon", PieceCategory.Hybrid, ShopLane.Specialty,
                DemoSandboxShapes.HorizontalPair, FactionIds.CartelOfEchoes,
                maxHp: 130, baseDamage: 40, requisitionCost: 2,
                attackType: AttackType.Explosive, armorType: ArmorType.Medium, attackRange: AttackRangeTier.Long,
                rarity: Rarity.Rare)
        };

        // 2026-07-15 faction-roster-v1 Wave 2: CreateCrimsonLegionPieces()/CreateAshWraithPieces()
        // deleted — crimson_legion/ash_wraiths were enemy-only pools with no playable identity;
        // Crimson Assembly and Ashen Covenant replace them with full 12-piece rosters authored in
        // CrimsonAssemblyContentFactory/AshenCovenantContentFactory (see AllFactionsContentFactory).
        // This "5 Factions" legacy demo pipeline never produced those two pools' pieces under the
        // new ids, so nothing else needs remapping here.

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
            TagContentMigrator.PieceTagMapping? mappingOverride = null,
            Rarity rarity = Rarity.Common)
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
                movementSpeed: movementSpeed,
                rarity: rarity);

            piece.includeInShopPool = includeInShopPool;
            EditorUtility.SetDirty(piece);
            return piece;
        }
    }
}
