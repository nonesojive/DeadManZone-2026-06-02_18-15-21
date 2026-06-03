using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Tests
{
    public static class TestPieces
    {
        public static PieceDefinition RifleSquad() => new()
        {
            Id = "rifle_squad",
            DisplayName = "Rifle Squad",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            Tags = new[] { "Infantry", GameTags.Combatant },
            MaxHp = 10,
            BaseDamage = 2,
            CooldownTicks = 3,
            GoldCost = 10
        };

        public static PieceDefinition CommandBunker() => new()
        {
            Id = "command_bunker",
            DisplayName = "Command Bunker",
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0), new GridCoord(1, 0) }),
            Tags = new[] { "Command" },
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
            Tags = new[] { GameTags.Hq },
            MaxHp = 25
        };

        public static PieceDefinition SupplyDepot() => new()
        {
            Id = "supply_depot",
            DisplayName = "Supply Depot",
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            Tags = new[] { "Supply" },
            MaxHp = 15,
            GoldCost = 6,
            ShopModifiers = ShopModifierFlags.GoldDiscount10,
            CommandActions = CommandActionFlags.SpendRequisitionBuff
        };

        public static PieceDefinition FieldWorkshop() => new()
        {
            Id = "field_workshop",
            DisplayName = "Field Workshop",
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            Tags = new[] { "Mechanical" },
            MaxHp = 12,
            GoldCost = 7,
            ShopModifiers = ShopModifierFlags.GuaranteeEngineerOffer
        };

        public static PieceDefinition GasDrone() => new()
        {
            Id = "gas_drone",
            DisplayName = "Gas Drone",
            Category = PieceCategory.Hybrid,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            Tags = new[] { "Gas", GameTags.Combatant },
            MaxHp = 8,
            BaseDamage = 4,
            GoldCost = 5,
            RequisitionCost = 3,
            CommandActions = CommandActionFlags.CallStrike
        };

        public static PieceDefinition WeakConscript() => new()
        {
            Id = "weak_conscript",
            DisplayName = "Weak Conscript",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            Tags = new[] { "Infantry", GameTags.Combatant },
            MaxHp = 3,
            BaseDamage = 1,
            CooldownTicks = 4
        };
    }
}
