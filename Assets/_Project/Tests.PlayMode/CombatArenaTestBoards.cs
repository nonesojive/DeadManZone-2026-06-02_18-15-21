using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.PlayMode.Tests
{
    internal static class CombatArenaTestBoards
    {
        public static BattlefieldState BuildFieldGunVsHq(ContentDatabase database)
        {
            var faction = database.GetFaction("iron_vanguard");
            Assert.NotNull(faction, "iron_vanguard faction required for arena replay tests.");

            var player = new BoardState(faction.CreateBoardLayout());
            Place(player, database, "ironmarch_hq", new GridCoord(0, 4), "hq_player");
            Place(player, database, "field_gun_nest", new GridCoord(3, 2), "field_gun_1");

            var enemy = new BoardState(faction.CreateBoardLayout());
            Place(enemy, database, "ironmarch_hq", new GridCoord(0, 4), "enemy_hq");

            return BattlefieldState.FromBoards(player, enemy);
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
