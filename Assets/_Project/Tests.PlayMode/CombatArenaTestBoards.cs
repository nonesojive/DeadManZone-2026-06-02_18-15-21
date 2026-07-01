using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;

namespace DeadManZone.PlayMode.Tests
{
    internal static class CombatArenaTestBoards
    {
        public static BattlefieldState BuildIronmarchUnionSkirmish(ContentDatabase database) =>
            CombatSliceLayouts.BuildIronmarchUnionSkirmish(database);

        public static BattlefieldState BuildFieldGunVsHq(ContentDatabase database) =>
            BuildFieldGunVsRifle(database);

        public static BattlefieldState BuildFieldGunVsRifle(ContentDatabase database)
        {
            var faction = database.GetFaction(FactionIds.IronmarchUnion);
            Assert.NotNull(faction, "ironmarch_union faction required for arena replay tests.");

            var player = new BoardState(faction.CreateCombatBoardLayout());
            Place(player, database, "conscript_rifleman", new GridCoord(3, 2), "field_gun_1");
            PlaceRearBlocker(player, new GridCoord(0, 2), "player_blocker");

            var enemy = new BoardState(faction.CreateCombatBoardLayout());
            Place(enemy, database, "conscript_rifleman", new GridCoord(5, 3), "enemy_rifle_1");

            return BattlefieldState.FromBoards(player, enemy);
        }

        private static void PlaceRearBlocker(BoardState board, GridCoord anchor, string instanceId)
        {
            var result = board.TryPlace(TestPieces.MultiCellRearBlocker(), anchor, instanceId);
            Assert.IsTrue(result.Success, $"Failed to place rear blocker at {anchor}: {result.Reason}");
        }

        private static void Place(
            BoardState board,
            ContentDatabase database,
            string pieceId,
            GridCoord anchor,
            string instanceId)
        {
            var piece = database.Pieces.First(p => p.id == pieceId).ToCore();
            var result = board.TryPlace(piece, anchor, instanceId);
            Assert.IsTrue(result.Success, $"Failed to place {pieceId} at {anchor}: {result.Reason}");
        }
    }
}
