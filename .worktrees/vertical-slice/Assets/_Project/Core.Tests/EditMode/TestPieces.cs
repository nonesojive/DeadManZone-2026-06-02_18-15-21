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
            Tags = new[] { "Infantry" },
            MaxHp = 10,
            BaseDamage = 2,
            CooldownTicks = 3
        };

        public static PieceDefinition CommandBunker() => new()
        {
            Id = "command_bunker",
            DisplayName = "Command Bunker",
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0), new GridCoord(1, 0) }),
            Tags = new[] { "Command" },
            MaxHp = 20,
            CommandActions = CommandActionFlags.ChangeStance,
            ShopModifiers = ShopModifierFlags.ExtraGeneralSlot
        };
    }
}
