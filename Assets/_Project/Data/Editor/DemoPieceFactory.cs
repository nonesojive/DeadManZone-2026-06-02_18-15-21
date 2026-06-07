using DeadManZone.Core.Board;
using DeadManZone.Core.Shop;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    internal static class DemoPieceFactory
    {
        private static readonly Vector2Int[] Single = { Vector2Int.zero };
        private static readonly Vector2Int[] Double = { Vector2Int.zero, Vector2Int.right };
        private static readonly Vector2Int[] Walker = { Vector2Int.zero, Vector2Int.right, new(0, 1), new(1, 1) };

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
            SaveMappedPiece("hq_command", "Command HQ", PieceCategory.Building, ShopLane.Defensive,
                Double, "iron_vanguard", maxHp: 25, goldCost: 0, manpowerCost: 0),
            SaveMappedPiece("rifle_squad", "Rifle Squad", PieceCategory.Unit, ShopLane.Offensive,
                Single, "iron_vanguard",
                maxHp: 10, baseDamage: 2, goldCost: 5),
            SaveMappedPiece("diesel_walker", "Diesel Walker", PieceCategory.Unit, ShopLane.Offensive,
                Walker, "iron_vanguard",
                maxHp: 25, baseDamage: 4, cooldownTicks: 5, goldCost: 12),
            SaveMappedPiece("radio_array", "Radio Array", PieceCategory.Building, ShopLane.Defensive,
                Single, "iron_vanguard", maxHp: 12, goldCost: 7,
                shopModifiers: ShopModifierFlags.EnemyTagPreview),
            SaveMappedPiece("mg_team", "MG Team", PieceCategory.Unit, ShopLane.Offensive,
                Double, "iron_vanguard",
                maxHp: 14, baseDamage: 3, cooldownTicks: 4, goldCost: 8),
            SaveMappedPiece("field_gun_nest", "Field Gun Nest", PieceCategory.Building, ShopLane.Defensive,
                new[] { Vector2Int.zero, new Vector2Int(0, 1) },
                "iron_vanguard",
                maxHp: 18, baseDamage: 3, goldCost: 9),
            SaveMappedPiece("supply_depot", "Supply Depot", PieceCategory.Building, ShopLane.Defensive,
                Single, "iron_vanguard", maxHp: 15, goldCost: 6,
                shopModifiers: ShopModifierFlags.GoldDiscount10),
            SaveMappedPiece("mobile_artillery", "Mobile Artillery", PieceCategory.Hybrid, ShopLane.Specialty,
                Double, "iron_vanguard",
                maxHp: 16, baseDamage: 5, goldCost: 10, requisitionCost: 2)
        };

        private static PieceDefinitionSO[] CreateNeutralPieces() => new[]
        {
            SaveMappedPiece("conscript_rifleman", "Conscript Rifleman", PieceCategory.Unit, ShopLane.Offensive,
                Single, "neutral",
                maxHp: 8, baseDamage: 2, goldCost: 4),
            SaveMappedPiece("grenade_thrower", "Grenade Thrower", PieceCategory.Unit, ShopLane.Offensive,
                Single, "neutral",
                maxHp: 7, baseDamage: 3, cooldownTicks: 4, goldCost: 5),
            SaveMappedPiece("field_medic", "Field Medic", PieceCategory.Unit, ShopLane.Defensive,
                Single, "neutral",
                maxHp: 6, baseDamage: 0, goldCost: 5),
            SaveMappedPiece("armored_transport", "Armored Transport", PieceCategory.Unit, ShopLane.Offensive,
                Double, "neutral",
                maxHp: 18, baseDamage: 1, goldCost: 8),
            SaveMappedPiece("mobile_cannon", "Mobile Cannon", PieceCategory.Hybrid, ShopLane.Specialty,
                Double, "neutral",
                maxHp: 14, baseDamage: 4, goldCost: 9, requisitionCost: 1)
        };

        private static PieceDefinitionSO[] CreateDustScourgePieces() => new[]
        {
            SaveMappedPiece("dust_hq", "Nomad Command", PieceCategory.Building, ShopLane.Defensive,
                Double, "dust_scourge", maxHp: 22, goldCost: 0, manpowerCost: 0),
            SaveMappedPiece("sand_raider", "Sand Raider", PieceCategory.Unit, ShopLane.Offensive,
                Single, "dust_scourge",
                maxHp: 9, baseDamage: 3, cooldownTicks: 2, goldCost: 6),
            SaveMappedPiece("scrap_rig", "Scrap Rig", PieceCategory.Unit, ShopLane.Offensive,
                Double, "dust_scourge",
                maxHp: 16, baseDamage: 2, goldCost: 7),
            SaveMappedPiece("toxin_launcher", "Toxin Launcher", PieceCategory.Hybrid, ShopLane.Specialty,
                Single, "dust_scourge",
                maxHp: 10, baseDamage: 4, goldCost: 9, requisitionCost: 2,
                grantedAbility: GrantedAbility.GrenadeLob)
        };

        private static PieceDefinitionSO[] CreateCartelPieces() => new[]
        {
            SaveMappedPiece("echo_hq", "Echo Nexus", PieceCategory.Building, ShopLane.Defensive,
                Double, "cartel_of_echoes", maxHp: 20, goldCost: 0, manpowerCost: 0),
            SaveMappedPiece("phantom_agent", "Phantom Agent", PieceCategory.Unit, ShopLane.Offensive,
                Single, "cartel_of_echoes",
                maxHp: 7, baseDamage: 3, cooldownTicks: 2, goldCost: 7),
            SaveMappedPiece("signal_relay", "Signal Relay", PieceCategory.Building, ShopLane.Defensive,
                Single, "cartel_of_echoes",
                maxHp: 11, goldCost: 6, shopModifiers: ShopModifierFlags.EnemyTagPreview),
            SaveMappedPiece("resonance_cannon", "Resonance Cannon", PieceCategory.Hybrid, ShopLane.Specialty,
                Double, "cartel_of_echoes",
                maxHp: 13, baseDamage: 5, goldCost: 10, requisitionCost: 2)
        };

        private static PieceDefinitionSO[] CreateCrimsonLegionPieces() => new[]
        {
            SaveMappedPiece("crimson_elite", "Crimson Elite", PieceCategory.Unit, ShopLane.Offensive,
                Single, "crimson_legion",
                maxHp: 12, baseDamage: 3, goldCost: 0),
            SaveMappedPiece("crimson_tank", "Crimson Tank", PieceCategory.Unit, ShopLane.Offensive,
                Walker, "crimson_legion",
                maxHp: 28, baseDamage: 5, goldCost: 0),
            SaveMappedPiece("crimson_artillery", "Crimson Battery", PieceCategory.Building, ShopLane.Defensive,
                Double, "crimson_legion",
                maxHp: 20, baseDamage: 4, goldCost: 0)
        };

        private static PieceDefinitionSO[] CreateAshWraithPieces() => new[]
        {
            SaveMappedPiece("wraith_stalker", "Wraith Stalker", PieceCategory.Unit, ShopLane.Offensive,
                Single, "ash_wraiths",
                maxHp: 8, baseDamage: 4, goldCost: 0),
            SaveMappedPiece("wraith_phantom", "Ash Phantom", PieceCategory.Unit, ShopLane.Offensive,
                Single, "ash_wraiths",
                maxHp: 10, baseDamage: 3, goldCost: 0),
            SaveMappedPiece("wraith_bombard", "Grave Bombard", PieceCategory.Hybrid, ShopLane.Specialty,
                Double, "ash_wraiths",
                maxHp: 15, baseDamage: 5, goldCost: 0)
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
            ShopModifierFlags shopModifiers = ShopModifierFlags.None,
            CommandActionFlags commandActions = CommandActionFlags.None,
            GrantedAbility grantedAbility = GrantedAbility.None)
        {
            var mapping = TagContentMigrator.GetMappingOrThrow(id);
            return DemoContentGenerator.SavePiece(
                id,
                displayName,
                category,
                lane,
                shape,
                primary: mapping.Primary,
                combatRole: mapping.CombatRole,
                systemTag: mapping.SystemTag,
                synergyTags: mapping.SynergyTags,
                factionId: factionId,
                maxHp: maxHp,
                baseDamage: baseDamage,
                cooldownTicks: cooldownTicks,
                goldCost: goldCost,
                requisitionCost: requisitionCost,
                manpowerCost: manpowerCost,
                shopModifiers: shopModifiers,
                commandActions: commandActions,
                grantedAbility: grantedAbility);
        }
    }
}
