using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Data;

namespace DeadManZone.Presentation.Combat.Arena
{
    public static class CombatSliceLayouts
    {
        public static BattlefieldState BuildIronmarchUnionSkirmish(ContentDatabase database)
        {
            var faction = database.GetFaction(FactionIds.IronmarchUnion);
            if (faction == null)
                return null;

            var player = new BoardState(faction.CreateCombatBoardLayout());
            Place(player, database, CombatSliceConstants.PlayerRifle, new GridCoord(5, 3), "rifle_1");
            Place(player, database, CombatSliceConstants.PlayerRifle, new GridCoord(5, 5), "rifle_2");
            Place(player, database, CombatSliceConstants.PlayerTank, new GridCoord(3, 3), "tank_1");
            Place(player, database, CombatSliceConstants.PlayerFieldGun, new GridCoord(2, 2), "field_gun_1");

            var enemy = new BoardState(faction.CreateCombatBoardLayout());
            Place(enemy, database, CombatSliceConstants.EnemyRifle, new GridCoord(5, 3), "enemy_rifle_1");
            Place(enemy, database, CombatSliceConstants.EnemyRifle, new GridCoord(5, 5), "enemy_rifle_2");

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
            if (!result.Success)
            {
                throw new System.InvalidOperationException(
                    $"Slice placement failed: {pieceId} at {anchor} — {result.Reason}");
            }
        }
    }
}
