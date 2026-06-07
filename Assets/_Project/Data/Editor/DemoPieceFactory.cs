using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
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
            DemoContentGenerator.SavePiece("hq_command", "Command HQ", PieceCategory.Building, ShopLane.Defensive,
                Double, new[] { GameTags.Hq }, "iron_vanguard", maxHp: 25, goldCost: 0, manpowerCost: 0),
            DemoContentGenerator.SavePiece("rifle_squad", "Rifle Squad", PieceCategory.Unit, ShopLane.Offensive,
                Single, new[] { GameKeywords.Infantry, GameTags.Vanguard, GameTags.Combatant }, "iron_vanguard",
                maxHp: 10, baseDamage: 2, goldCost: 5),
            DemoContentGenerator.SavePiece("diesel_walker", "Diesel Walker", PieceCategory.Unit, ShopLane.Offensive,
                Walker, new[] { GameKeywords.Mechanical, GameTags.Vanguard, GameTags.Combatant }, "iron_vanguard",
                maxHp: 25, baseDamage: 4, cooldownTicks: 5, goldCost: 12),
            DemoContentGenerator.SavePiece("radio_array", "Radio Array", PieceCategory.Building, ShopLane.Defensive,
                Single, new[] { GameTags.Command }, "iron_vanguard", maxHp: 12, goldCost: 7,
                shopModifiers: ShopModifierFlags.EnemyTagPreview),
            DemoContentGenerator.SavePiece("mg_team", "MG Team", PieceCategory.Unit, ShopLane.Offensive,
                Double, new[] { GameKeywords.Infantry, GameTags.Combatant }, "iron_vanguard",
                maxHp: 14, baseDamage: 3, cooldownTicks: 4, goldCost: 8),
            DemoContentGenerator.SavePiece("field_gun_nest", "Field Gun Nest", PieceCategory.Building, ShopLane.Defensive,
                new[] { Vector2Int.zero, new Vector2Int(0, 1) },
                new[] { GameKeywords.Artillery, GameTags.Combatant }, "iron_vanguard",
                maxHp: 18, baseDamage: 3, goldCost: 9),
            DemoContentGenerator.SavePiece("supply_depot", "Supply Depot", PieceCategory.Building, ShopLane.Defensive,
                Single, new[] { GameKeywords.Supply }, "iron_vanguard", maxHp: 15, goldCost: 6,
                shopModifiers: ShopModifierFlags.GoldDiscount10),
            DemoContentGenerator.SavePiece("mobile_artillery", "Mobile Artillery", PieceCategory.Hybrid, ShopLane.Specialty,
                Double, new[] { GameKeywords.Artillery, GameKeywords.Mechanical }, "iron_vanguard",
                maxHp: 16, baseDamage: 5, goldCost: 10, requisitionCost: 2)
        };

        private static PieceDefinitionSO[] CreateNeutralPieces() => new[]
        {
            DemoContentGenerator.SavePiece("conscript_rifleman", "Conscript Rifleman", PieceCategory.Unit, ShopLane.Offensive,
                Single, new[] { GameKeywords.Infantry, GameTags.Combatant }, "neutral",
                maxHp: 8, baseDamage: 2, goldCost: 4),
            DemoContentGenerator.SavePiece("grenade_thrower", "Grenade Thrower", PieceCategory.Unit, ShopLane.Offensive,
                Single, new[] { GameKeywords.Infantry, GameTags.Combatant }, "neutral",
                maxHp: 7, baseDamage: 3, cooldownTicks: 4, goldCost: 5),
            DemoContentGenerator.SavePiece("field_medic", "Field Medic", PieceCategory.Unit, ShopLane.Defensive,
                Single, new[] { GameKeywords.Medic, GameTags.Combatant }, "neutral",
                maxHp: 6, baseDamage: 0, goldCost: 5),
            DemoContentGenerator.SavePiece("armored_transport", "Armored Transport", PieceCategory.Unit, ShopLane.Offensive,
                Double, new[] { GameKeywords.Vehicle, GameTags.Combatant }, "neutral",
                maxHp: 18, baseDamage: 1, goldCost: 8),
            DemoContentGenerator.SavePiece("mobile_cannon", "Mobile Cannon", PieceCategory.Hybrid, ShopLane.Specialty,
                Double, new[] { GameKeywords.Artillery, GameKeywords.Vehicle }, "neutral",
                maxHp: 14, baseDamage: 4, goldCost: 9, requisitionCost: 1)
        };

        private static PieceDefinitionSO[] CreateDustScourgePieces() => new[]
        {
            DemoContentGenerator.SavePiece("dust_hq", "Nomad Command", PieceCategory.Building, ShopLane.Defensive,
                Double, new[] { GameTags.Hq }, "dust_scourge", maxHp: 22, goldCost: 0, manpowerCost: 0),
            DemoContentGenerator.SavePiece("sand_raider", "Sand Raider", PieceCategory.Unit, ShopLane.Offensive,
                Single, new[] { GameKeywords.Infantry, GameKeywords.Gas, GameTags.Combatant }, "dust_scourge",
                maxHp: 9, baseDamage: 3, cooldownTicks: 2, goldCost: 6),
            DemoContentGenerator.SavePiece("scrap_rig", "Scrap Rig", PieceCategory.Unit, ShopLane.Offensive,
                Double, new[] { GameKeywords.Vehicle, GameTags.Combatant }, "dust_scourge",
                maxHp: 16, baseDamage: 2, goldCost: 7),
            DemoContentGenerator.SavePiece("toxin_launcher", "Toxin Launcher", PieceCategory.Hybrid, ShopLane.Specialty,
                Single, new[] { GameKeywords.Gas, GameKeywords.Artillery }, "dust_scourge",
                maxHp: 10, baseDamage: 4, goldCost: 9, requisitionCost: 2,
                grantedAbility: GrantedAbility.GrenadeLob)
        };

        private static PieceDefinitionSO[] CreateCartelPieces() => new[]
        {
            DemoContentGenerator.SavePiece("echo_hq", "Echo Nexus", PieceCategory.Building, ShopLane.Defensive,
                Double, new[] { GameTags.Hq }, "cartel_of_echoes", maxHp: 20, goldCost: 0, manpowerCost: 0),
            DemoContentGenerator.SavePiece("phantom_agent", "Phantom Agent", PieceCategory.Unit, ShopLane.Offensive,
                Single, new[] { GameKeywords.Stealth, GameKeywords.Echo, GameTags.Combatant }, "cartel_of_echoes",
                maxHp: 7, baseDamage: 3, cooldownTicks: 2, goldCost: 7),
            DemoContentGenerator.SavePiece("signal_relay", "Signal Relay", PieceCategory.Building, ShopLane.Defensive,
                Single, new[] { GameTags.Command, GameKeywords.Echo }, "cartel_of_echoes",
                maxHp: 11, goldCost: 6, shopModifiers: ShopModifierFlags.EnemyTagPreview),
            DemoContentGenerator.SavePiece("resonance_cannon", "Resonance Cannon", PieceCategory.Hybrid, ShopLane.Specialty,
                Double, new[] { GameKeywords.Artillery, GameKeywords.Echo }, "cartel_of_echoes",
                maxHp: 13, baseDamage: 5, goldCost: 10, requisitionCost: 2)
        };

        private static PieceDefinitionSO[] CreateCrimsonLegionPieces() => new[]
        {
            DemoContentGenerator.SavePiece("crimson_elite", "Crimson Elite", PieceCategory.Unit, ShopLane.Offensive,
                Single, new[] { GameKeywords.Infantry, GameTags.Combatant }, "crimson_legion",
                maxHp: 12, baseDamage: 3, goldCost: 0),
            DemoContentGenerator.SavePiece("crimson_tank", "Crimson Tank", PieceCategory.Unit, ShopLane.Offensive,
                Walker, new[] { GameKeywords.Vehicle, GameTags.Combatant }, "crimson_legion",
                maxHp: 28, baseDamage: 5, goldCost: 0),
            DemoContentGenerator.SavePiece("crimson_artillery", "Crimson Battery", PieceCategory.Building, ShopLane.Defensive,
                Double, new[] { GameKeywords.Artillery, GameTags.Combatant }, "crimson_legion",
                maxHp: 20, baseDamage: 4, goldCost: 0)
        };

        private static PieceDefinitionSO[] CreateAshWraithPieces() => new[]
        {
            DemoContentGenerator.SavePiece("wraith_stalker", "Wraith Stalker", PieceCategory.Unit, ShopLane.Offensive,
                Single, new[] { GameKeywords.Stealth, GameKeywords.Gas, GameTags.Combatant }, "ash_wraiths",
                maxHp: 8, baseDamage: 4, goldCost: 0),
            DemoContentGenerator.SavePiece("wraith_phantom", "Ash Phantom", PieceCategory.Unit, ShopLane.Offensive,
                Single, new[] { GameKeywords.Infantry, GameKeywords.Gas, GameTags.Combatant }, "ash_wraiths",
                maxHp: 10, baseDamage: 3, goldCost: 0),
            DemoContentGenerator.SavePiece("wraith_bombard", "Grave Bombard", PieceCategory.Hybrid, ShopLane.Specialty,
                Double, new[] { GameKeywords.Artillery, GameKeywords.Gas }, "ash_wraiths",
                maxHp: 15, baseDamage: 5, goldCost: 0)
        };
    }
}
