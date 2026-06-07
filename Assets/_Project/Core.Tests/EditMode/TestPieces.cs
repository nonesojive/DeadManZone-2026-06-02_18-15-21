using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Tests
{
    public static class TestPieces
    {
        public static PieceDefinition RifleSquadTenMan() => new()
        {
            Id = "rifle_squad",
            DisplayName = "Rifle Squad",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 100,
            ManpowerCost = 10,
            Tags = new[] { GameTagIds.Combatant }
        };

        public static PieceDefinition FieldingHq() => new()
        {
            Id = "hq",
            DisplayName = "HQ",
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0), new GridCoord(1, 0) }),
            Tags = new[] { GameTagIds.Hq },
            MaxHp = 80,
            ManpowerCost = 8
        };

        public static PieceDefinition RifleSquad() => new()
        {
            Id = "rifle_squad",
            DisplayName = "Rifle Squad",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            Tags = new[] { GameTagIds.Infantry, GameTagIds.Combatant },
            MaxHp = 100,
            BaseDamage = 20,
            CooldownTicks = 3,
            GoldCost = 10,
            ManpowerCost = 10,
            FactionId = "iron_vanguard"
        };

        public static PieceDefinition With(
            PieceDefinition source,
            int? baseDamage = null,
            AttackType? attackType = null,
            ArmorType? armorType = null,
            AttackRangeTier? attackRange = null,
            MovementSpeedTier? movementSpeed = null,
            AttackSpeedTier? attackSpeed = null,
            GrantedAbility? grantedAbility = null) =>
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
                Tags = source.Tags,
                MaxHp = source.MaxHp,
                BaseDamage = baseDamage ?? source.BaseDamage,
                CooldownTicks = source.CooldownTicks,
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
                FactionId = source.FactionId
            };

        public static PieceDefinition CommandBunker() => new()
        {
            Id = "command_bunker",
            DisplayName = "Command Bunker",
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0), new GridCoord(1, 0) }),
            Tags = new[] { GameTagIds.Command },
            MaxHp = 20,
            GoldCost = 8,
            CommandActions = CommandActionFlags.ChangeStance,
            ShopModifiers = ShopModifierFlags.ExtraGeneralSlot
        };

        public static PieceDefinition HqPiece() => new()
        {
            Id = "hq",
            DisplayName = "HQ",
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0), new GridCoord(1, 0) }),
            Tags = new[] { GameTagIds.Hq },
            MaxHp = 80,
            ManpowerCost = 8
        };

        public static PieceDefinition SupplyDepot() => new()
        {
            Id = "supply_depot",
            DisplayName = "Supply Depot",
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            SynergyTags = new[] { GameTagIds.Supply },
            Tags = new[] { GameTagIds.Supply },
            MaxHp = 50,
            GoldCost = 6,
            ManpowerCost = 0,
            MusterPerShop = 3,
            ShopModifiers = ShopModifierFlags.GoldDiscount10,
            CommandActions = CommandActionFlags.SpendRequisitionBuff
        };

        public static PieceDefinition FieldWorkshop() => new()
        {
            Id = "field_workshop",
            DisplayName = "Field Workshop",
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            Tags = new[] { GameTagIds.Mechanical },
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
            Tags = new[] { GameTagIds.Gas, GameTagIds.Combatant },
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
                    tags ?? System.Array.Empty<string>()),
                MaxHp = 10,
                BaseDamage = 2,
                CooldownTicks = 3,
                ManpowerCost = 1
            };

        public static PieceDefinition WeakConscript() => new()
        {
            Id = "weak_conscript",
            DisplayName = "Weak Conscript",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            Tags = new[] { GameTagIds.Infantry, GameTagIds.Combatant },
            MaxHp = 3,
            BaseDamage = 1,
            CooldownTicks = 4,
            ManpowerCost = 1
        };
    }
}
