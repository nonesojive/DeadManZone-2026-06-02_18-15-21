using System.Collections.Generic;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Tests
{
    public static class TestPieces
    {
        public static PieceDefinition RifleSquadTenMan() => new()
        {
            Id = "conscript_rifleman",
            DisplayName = "Conscript Rifleman",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 100,
            ManpowerCost = 10
        };

        /// <summary>Two-cell rear structure for combat footprint/layout tests.</summary>
        public static PieceDefinition MultiCellRearBlocker() => new()
        {
            Id = "rear_blocker",
            DisplayName = "Rear Blocker",
            Category = PieceCategory.Unit,
            Primary = GameTagIds.Structure,
            Shape = MultiCellHorizontalPairShape(),
            MaxHp = 80,
            ManpowerCost = 8
        };

        public static PieceShape MultiCellHorizontalPairShape() =>
            new(new[] { new GridCoord(0, 0), new GridCoord(1, 0) });

        public static PieceDefinition RifleSquad() => new()
        {
            Id = "conscript_rifleman",
            DisplayName = "Conscript Rifleman",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            Tags = new[] { GameTagIds.Infantry },
            MaxHp = 100,
            BaseDamage = 20,
            CooldownTicks = 3,
            GoldCost = 10,
            ManpowerCost = 10,
            FactionId = FactionIds.IronmarchUnion
        };

        public static PieceDefinition With(
            PieceDefinition source,
            int? baseDamage = null,
            int? maxHp = null,
            AttackType? attackType = null,
            ArmorType? armorType = null,
            AttackRangeTier? attackRange = null,
            int? movementSpeed = null,
            AttackSpeedTier? attackSpeed = null,
            GrantedAbility? grantedAbility = null,
            int? accuracyOverride = null,
            int? cooldownTicks = null,
            int? maxMorale = null,
            int? terrorDamage = null,
            IReadOnlyList<PieceAbilityDefinition> abilities = null) =>
            new()
            {
                Id = source.Id,
                DisplayName = source.DisplayName,
                Category = source.Category,
                Shape = source.Shape,
                Primary = source.Primary,
                CombatRole = source.CombatRole,
                SystemTag = source.SystemTag,
                SynergyTags = source.SynergyTags,
                AbilityTags = source.AbilityTags,
                FlavorTags = source.FlavorTags,
                Tags = source.Tags,
                Abilities = abilities ?? source.Abilities,
                MaxHp = maxHp ?? source.MaxHp,
                MaxMorale = maxMorale ?? source.MaxMorale,
                BaseDamage = baseDamage ?? source.BaseDamage,
                TerrorDamage = terrorDamage ?? source.TerrorDamage,
                CooldownTicks = cooldownTicks ?? source.CooldownTicks,
                GoldCost = source.GoldCost,
                RequisitionCost = source.RequisitionCost,
                ManpowerCost = source.ManpowerCost,
                ShopModifiers = source.ShopModifiers,
                CommandActions = source.CommandActions,
                AttackRange = attackRange ?? source.AttackRange,
                MovementSpeed = movementSpeed ?? source.MovementSpeed,
                AttackSpeed = attackSpeed ?? source.AttackSpeed,
                ArmorType = armorType ?? source.ArmorType,
                AttackType = attackType ?? source.AttackType,
                GrantedAbility = grantedAbility ?? source.GrantedAbility,
                AccuracyOverride = accuracyOverride ?? source.AccuracyOverride,
                FactionId = source.FactionId
            };

        public static PieceDefinition CommandBunker() => new()
        {
            Id = "command_outpost",
            DisplayName = "Command Outpost",
            Category = PieceCategory.Building,
            Shape = MultiCellHorizontalPairShape(),
            MaxHp = 20,
            GoldCost = 8,
            CommandActions = CommandActionFlags.ChangeStance,
            ShopModifiers = ShopModifierFlags.ExtraGeneralSlot
        };

        public static PieceDefinition SupplyDepot() => new()
        {
            Id = "supply_depot",
            DisplayName = "Supply Depot",
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 50,
            GoldCost = 6,
            ManpowerCost = 0,
            MusterPerShop = 3,
            ShopModifiers = ShopModifierFlags.GoldDiscount10,
            CommandActions = CommandActionFlags.SpendRequisitionBuff
        };

        public static PieceDefinition CommandOutpost() => new()
        {
            Id = "command_outpost",
            DisplayName = "Command Outpost",
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 40,
            GoldCost = 8
        };

        public static PieceDefinition OfficerQuarters() => new()
        {
            Id = "officer_quarters",
            DisplayName = "Officer Quarters",
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 35,
            GoldCost = 7
        };

        public static PieceDefinition FieldWorkshop() => new()
        {
            Id = "recruitment_office",
            DisplayName = "Recruitment Office",
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 120,
            GoldCost = 7,
            MusterPerShop = 2,
            ShopModifiers = ShopModifierFlags.GuaranteeEngineerOffer
        };

        public static PieceDefinition GasDrone() => new()
        {
            Id = "gas_drone",
            DisplayName = "Gas Drone",
            Category = PieceCategory.Hybrid,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            AttackType = AttackType.Gas,
            MaxHp = 8,
            BaseDamage = 4,
            GoldCost = 5,
            RequisitionCost = 3,
            ManpowerCost = 1,
            CommandActions = CommandActionFlags.CallStrike
        };

        public static PieceDefinition CreateUnit(
            string id,
            string[] tags = null,
            string primary = null,
            string combatRole = null,
            string systemTag = null,
            string[] synergyTags = null,
            string[] abilityTags = null) => new()
            {
                Id = id,
                DisplayName = id,
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                Primary = primary,
                CombatRole = combatRole,
                SystemTag = systemTag,
                SynergyTags = synergyTags ?? System.Array.Empty<string>(),
                AbilityTags = abilityTags ?? System.Array.Empty<string>(),
                Tags = PieceTagQueries.BuildLegacyTags(
                    PieceCategory.Unit,
                    baseDamage: 2,
                    primary,
                    combatRole,
                    systemTag,
                    synergyTags ?? System.Array.Empty<string>(),
                    abilityTags ?? System.Array.Empty<string>(),
                    System.Array.Empty<string>(),
                    tags ?? System.Array.Empty<string>()),
                MaxHp = 10,
                BaseDamage = 2,
                CooldownTicks = 3,
                ManpowerCost = 1
            };

        public static PieceDefinition WithTags(bool command = false, bool building = false) => new()
        {
            Id = building ? "tagged_building" : "tagged_piece",
            DisplayName = building ? "Tagged Building" : "Tagged Piece",
            Category = building ? PieceCategory.Building : PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            SynergyTags = command ? new[] { GameTagIds.Command } : System.Array.Empty<string>(),
            MaxHp = 10,
            ManpowerCost = building ? 0 : 1
        };

        public static PieceDefinition WeakConscript() => new()
        {
            Id = "weak_conscript",
            DisplayName = "Weak Conscript",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            Tags = new[] { GameTagIds.Infantry },
            MaxHp = 3,
            BaseDamage = 1,
            CooldownTicks = 4,
            ManpowerCost = 1
        };

        public static PieceDefinition FieldMedic() => With(
            CreateUnit(
                "field_medic",
                primary: GameTagIds.Infantry,
                combatRole: GameTagIds.Support,
                synergyTags: new[] { GameTagIds.Medic }),
            maxHp: 30,
            baseDamage: 3,
            movementSpeed: 2,
            abilities: new[]
            {
                new PieceAbilityDefinition
                {
                    Id = "field_medic_adjacent_infantry_hp",
                    Trigger = PieceAbilityTrigger.AdjacentAura,
                    NeighborFilter = new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
                    Stat = SynergyStat.MaxHp,
                    ModType = SynergyModType.Flat,
                    Magnitude = 10
                }
            });

        public static PieceDefinition BulwarkSquad() => With(
            CreateUnit(
                "bulwark_squad",
                primary: GameTagIds.Infantry,
                combatRole: GameTagIds.Assault,
                synergyTags: new[] { GameTagIds.Phalanx, GameTagIds.Veteran }),
            maxHp: 55,
            baseDamage: 3,
            movementSpeed: 3,
            abilities: new[]
            {
                new PieceAbilityDefinition
                {
                    Id = "bulwark_adjacent_phalanx_damage",
                    Trigger = PieceAbilityTrigger.AdjacentAura,
                    NeighborFilter = new NeighborFilter { SynergyTagId = GameTagIds.Phalanx },
                    Stat = SynergyStat.Damage,
                    ModType = SynergyModType.Flat,
                    Magnitude = 1,
                    ApplyToSelf = true
                },
                new PieceAbilityDefinition
                {
                    Id = "bulwark_adjacent_phalanx_hp",
                    Trigger = PieceAbilityTrigger.AdjacentAura,
                    NeighborFilter = new NeighborFilter { SynergyTagId = GameTagIds.Phalanx },
                    Stat = SynergyStat.MaxHp,
                    ModType = SynergyModType.Flat,
                    Magnitude = 5,
                    ApplyToSelf = true
                }
            });

        public static PieceDefinition EnlistedRifleman() => With(
            CreateUnit(
                "enlisted_rifleman",
                primary: GameTagIds.Infantry,
                combatRole: GameTagIds.Assault,
                synergyTags: new[] { GameTagIds.SmallArms }),
            maxHp: 55,
            baseDamage: 6,
            movementSpeed: 2,
            abilities: new[]
            {
                new PieceAbilityDefinition
                {
                    Id = "enlisted_rifleman_adjacent_command_attack_speed",
                    Trigger = PieceAbilityTrigger.AdjacentAura,
                    NeighborFilter = new NeighborFilter { SynergyTagId = GameTagIds.Command },
                    Stat = SynergyStat.AttackSpeedSteps,
                    ModType = SynergyModType.TierStep,
                    Magnitude = 1,
                    ApplyToSelf = true
                }
            });

        public static PieceDefinition IronmarchIronHorse() => With(
            CreateUnit(
                "ironmarch_iron_horse",
                primary: GameTagIds.Vehicle,
                combatRole: GameTagIds.Tank,
                synergyTags: new[] { GameTagIds.Ironclad }),
            maxHp: 75,
            baseDamage: 6,
            movementSpeed: 2,
            abilities: new[]
            {
                new PieceAbilityDefinition
                {
                    Id = "iron_horse_adjacent_infantry_hp",
                    Trigger = PieceAbilityTrigger.AdjacentAura,
                    NeighborFilter = new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
                    Stat = SynergyStat.MaxHp,
                    ModType = SynergyModType.Flat,
                    Magnitude = 10,
                    ApplyToSelf = true
                }
            });

        public static PieceDefinition IroncladFieldMarshal() => With(
            CreateUnit(
                "ironclad_field_marshal",
                primary: GameTagIds.Infantry,
                combatRole: GameTagIds.Utility,
                synergyTags: new[] { GameTagIds.Command, GameTagIds.Ironclad, GameTagIds.Inspiring }),
            maxHp: 50,
            baseDamage: 3,
            movementSpeed: 3,
            attackSpeed: AttackSpeedTier.Medium,
            abilities: new[]
            {
                new PieceAbilityDefinition
                {
                    Id = "field_marshal_adjacent_infantry_hp",
                    Trigger = PieceAbilityTrigger.AdjacentAura,
                    NeighborFilter = new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
                    Stat = SynergyStat.MaxHp,
                    ModType = SynergyModType.Flat,
                    Magnitude = 5
                },
                new PieceAbilityDefinition
                {
                    Id = "field_marshal_adjacent_infantry_movement",
                    Trigger = PieceAbilityTrigger.AdjacentAura,
                    NeighborFilter = new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
                    Stat = SynergyStat.MovementSpeed,
                    ModType = SynergyModType.Flat,
                    Magnitude = 1
                }
            });
    }
}
