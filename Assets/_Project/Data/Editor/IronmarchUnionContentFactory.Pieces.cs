using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static partial class IronmarchUnionContentFactory
    {
        private static readonly Vector2Int[] IronHorseShape =
        {
            Vector2Int.zero,
            new Vector2Int(1, 0),
            new Vector2Int(2, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(2, 1)
        };

        private static PieceDefinitionSO[] CreatePieces() => new[]
        {
            SavePiece("supply_depot", "Supply Depot", PieceCategory.Building, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Building, GameTagIds.Utility, "neutral", 15, 50, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, synergyTags: new[] { GameTagIds.SupplyLine }, flavorTags: new[] { GameTagIds.Logistics },
                rarity: Rarity.Uncommon),
            SavePiece("field_hospital", "Field Hospital", PieceCategory.Building, DemoSandboxShapes.Square2x2,
                GameTagIds.Building, GameTagIds.Support, "neutral", 20, 60, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, synergyTags: new[] { GameTagIds.Medic }, flavorTags: new[] { GameTagIds.Utility },
                customAbilities: new[]
                {
                    Ability("field_hospital_infantry_hp", PieceAbilityTrigger.FightStart, SynergyStat.MaxHp, SynergyModType.Flat, 10,
                        neighborFilter: new NeighborFilter { PrimaryTagId = GameTagIds.Infantry })
                },
                rarity: Rarity.Uncommon),
            SavePiece("officer_quarters", "Officer Quarters", PieceCategory.Building, DemoSandboxShapes.Square2x2,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.IronmarchUnion, 25, 45, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, requisitionCost: 1, synergyTags: new[] { GameTagIds.Command },
                rarity: Rarity.Uncommon),
            SavePiece("command_outpost", "Command Outpost", PieceCategory.Building, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Building, GameTagIds.Support, FactionIds.IronmarchUnion, 15, 40, 0, 0, AttackType.None, ArmorType.Light,
                // Common by design: the IronMarch defensive lane needs a Common floor so
                // low-Dread tier rolls don't starve faction-source defensive slots.
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, synergyTags: new[] { GameTagIds.Command },
                rarity: Rarity.Common),
            SavePiece("surgical_center", "Surgical Center", PieceCategory.Building, DemoSandboxShapes.Single,
                GameTagIds.Building, GameTagIds.Support, FactionIds.IronmarchUnion, 20, 35, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, synergyTags: new[] { GameTagIds.Medic },
                customAbilities: new[]
                {
                    Ability("surgical_center_infantry_hp_percent", PieceAbilityTrigger.FightStart, SynergyStat.MaxHp, SynergyModType.Percent, 5,
                        neighborFilter: new NeighborFilter { PrimaryTagId = GameTagIds.Infantry })
                },
                rarity: Rarity.Uncommon),
            SavePiece("recruitment_office", "Recruitment Office", PieceCategory.Building, DemoSandboxShapes.Single,
                GameTagIds.Building, GameTagIds.Utility, "neutral", 15, 35, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, musterPerShop: 1, flavorTags: new[] { GameTagIds.Logistics },
                rarity: Rarity.Common),
            SavePiece("field_medic", "Field Medic", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Support, "neutral", 10, 30, 3, 1, AttackType.Ballistic, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 2,
                synergyTags: new[] { GameTagIds.Medic }, flavorTags: new[] { GameTagIds.SmallArms },
                customAbilities: new[]
                {
                    Ability("field_medic_adjacent_infantry_hp", PieceAbilityTrigger.AdjacentAura, SynergyStat.MaxHp, SynergyModType.Flat, 10,
                        neighborFilter: new NeighborFilter { PrimaryTagId = GameTagIds.Infantry })
                },
                rarity: Rarity.Uncommon),
            SavePiece("conscript_rifleman", "Conscript Rifleman", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Assault, "neutral", 12, 50, 5, 1, AttackType.Ballistic, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Medium, 2, flavorTags: new[] { GameTagIds.SmallArms },
                rarity: Rarity.Common),
            // maxMorale 50: vehicle (see iron horse note).
            SavePiece("armored_transport", "Armored Transport", PieceCategory.Unit, DemoSandboxShapes.TransportL,
                GameTagIds.Vehicle, GameTagIds.Defender, "neutral", 18, 75, 2, 3, AttackType.Ballistic, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 4, grantedAbility: GrantedAbility.ShieldAllies,
                synergyTags: new[] { GameTagIds.Convoy }, flavorTags: new[] { GameTagIds.Support },
                rarity: Rarity.Rare, maxMorale: 50),
            SavePiece("ironmarch_surgeon", "Ironmarch Surgeon", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Support, FactionIds.IronmarchUnion, 15, 40, 3, 1, AttackType.Ballistic, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 2, synergyTags: new[] { GameTagIds.Medic },
                customAbilities: new[]
                {
                    Ability("surgeon_medic_hp_percent", PieceAbilityTrigger.BoardPerTagCount, SynergyStat.MaxHp, SynergyModType.Percent, 2,
                        countTagId: GameTagIds.Medic,
                        neighborFilter: new NeighborFilter { PrimaryTagId = GameTagIds.Infantry })
                },
                rarity: Rarity.Uncommon),
            SavePiece("bulwark_squad", "Bulwark Squad", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Assault, FactionIds.IronmarchUnion, 18, 55, 3, 1, AttackType.Ballistic, ArmorType.Medium,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 3, synergyTags: new[] { GameTagIds.Phalanx }, flavorTags: new[] { GameTagIds.Veteran },
                customAbilities: new[]
                {
                    Ability("bulwark_adjacent_phalanx_damage", PieceAbilityTrigger.AdjacentAura, SynergyStat.Damage, SynergyModType.Flat, 1,
                        neighborFilter: new NeighborFilter { SynergyTagId = GameTagIds.Phalanx }, applyToSelf: true),
                    Ability("bulwark_adjacent_phalanx_hp", PieceAbilityTrigger.AdjacentAura, SynergyStat.MaxHp, SynergyModType.Flat, 5,
                        neighborFilter: new NeighborFilter { SynergyTagId = GameTagIds.Phalanx }, applyToSelf: true)
                },
                rarity: Rarity.Uncommon),
            SavePiece("enlisted_rifleman", "Enlisted Rifleman", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Assault, FactionIds.IronmarchUnion, 15, 55, 6, 1, AttackType.Ballistic, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Medium, 2, flavorTags: new[] { GameTagIds.SmallArms },
                customAbilities: new[]
                {
                    Ability("enlisted_rifleman_adjacent_command_attack_speed", PieceAbilityTrigger.AdjacentAura, SynergyStat.AttackSpeedSteps, SynergyModType.TierStep, 1,
                        neighborFilter: new NeighborFilter { SynergyTagId = GameTagIds.Command }, applyToSelf: true)
                },
                rarity: Rarity.Common),
            SavePiece("ironmarch_iron_horse", "Ironmarch Iron Horse", PieceCategory.Unit, IronHorseShape,
                GameTagIds.Vehicle, GameTagIds.Tank, FactionIds.IronmarchUnion, 24, 75, 6, 4, AttackType.Piercing, ArmorType.Medium,
                AttackSpeedTier.Slow, AttackRangeTier.Medium, 2, abilityTags: new[] { GameTagIds.Ironclad }, flavorTags: new[] { GameTagIds.Shells },
                customAbilities: new[]
                {
                    Ability("iron_horse_adjacent_infantry_hp", PieceAbilityTrigger.AdjacentAura, SynergyStat.MaxHp, SynergyModType.Flat, 10,
                        neighborFilter: new NeighborFilter { PrimaryTagId = GameTagIds.Infantry }, applyToSelf: true)
                },
                // Vehicles hold the line longer than flesh (M5 initial, tune in playtest);
                // their Break reads as collapse-abandon in presentation, not a sprint.
                rarity: Rarity.Rare, maxMorale: 50),
            SavePiece("ironclad_mortars", "Ironclad Mortars", PieceCategory.Unit, DemoSandboxShapes.VerticalPair,
                GameTagIds.Infantry, GameTagIds.Artillery, FactionIds.IronmarchUnion, 20, 25, 8, 3, AttackType.Piercing, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Long, 1, grantedAbility: GrantedAbility.MortarShot,
                abilityTags: new[] { GameTagIds.Ironclad }, flavorTags: new[] { GameTagIds.Shells, GameTagIds.Siege },
                rarity: Rarity.Rare),
            SavePiece("ironclad_marksman", "IronClad Marksman", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Sniper, FactionIds.IronmarchUnion, 20, 35, 6, 2, AttackType.Piercing, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Long, 3, abilityTags: new[] { GameTagIds.Ironclad, GameTagIds.Stealth },
                rarity: Rarity.Uncommon),
            SavePiece("ironclad_field_marshal", "IronClad Field Marshal", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.IronmarchUnion, 30, 50, 3, 2, AttackType.Ballistic, ArmorType.Medium,
                AttackSpeedTier.Medium, AttackRangeTier.Short, 3, requisitionCost: 1, abilityTags: new[] { GameTagIds.Ironclad }, synergyTags: new[] { GameTagIds.Command }, flavorTags: new[] { GameTagIds.Inspiring },
                customAbilities: new[]
                {
                    Ability("field_marshal_adjacent_infantry_hp", PieceAbilityTrigger.AdjacentAura, SynergyStat.MaxHp, SynergyModType.Flat, 5,
                        neighborFilter: new NeighborFilter { PrimaryTagId = GameTagIds.Infantry }),
                    Ability("field_marshal_adjacent_infantry_movement", PieceAbilityTrigger.AdjacentAura, SynergyStat.MovementSpeed, SynergyModType.Flat, 1,
                        neighborFilter: new NeighborFilter { PrimaryTagId = GameTagIds.Infantry })
                },
                rarity: Rarity.Rare),
            SavePiece("machine_gun_nest", "Machine Gun Nest", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Structure, GameTagIds.Utility, "neutral", 20, 100, 2, 2, AttackType.Shredding, ArmorType.Heavy,
                AttackSpeedTier.Medium, AttackRangeTier.Medium, 0, synergyTags: new[] { GameTagIds.Entrenched }, flavorTags: new[] { GameTagIds.Fortification },
                // First terror content (M5, ADR-0005): sustained shredding fire breaks
                // will before bodies — the nest suppresses. Crewed emplacement, so it
                // CAN break (abandon the gun), just later than infantry.
                rarity: Rarity.Rare, maxMorale: 40, terrorDamage: 4)
        };
    }
}
