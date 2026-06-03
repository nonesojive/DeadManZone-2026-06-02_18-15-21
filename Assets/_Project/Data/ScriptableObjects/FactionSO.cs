using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Faction")]
    public class FactionSO : ScriptableObject
    {
        public string factionId = "iron_vanguard";
        public string displayName = "Iron Vanguard";
        public int boardWidth = 8;
        public int boardHeight = 6;
        public int rearRows = 2;
        public int supportRows = 2;
        public Vector2Int[] specialTileCoords =
        {
            new Vector2Int(1, 2),
            new Vector2Int(4, 2),
            new Vector2Int(6, 2)
        };
        public int startingSupplies = 100;
        public int startingManpower = 10;
        public int startingAuthority = 2;
        public int startingMorale = 100;

        public BoardLayout CreateBoardLayout()
        {
            var specialTiles = new GridCoord[specialTileCoords.Length];
            for (int i = 0; i < specialTileCoords.Length; i++)
                specialTiles[i] = new GridCoord(specialTileCoords[i].x, specialTileCoords[i].y);

            return BoardLayout.CreateStandard(
                boardWidth,
                boardHeight,
                rearRows,
                supportRows,
                specialTiles);
        }

        public BoardSnapshot CreateEmptyBoardSnapshot()
        {
            return new BoardSnapshot
            {
                Width = boardWidth,
                Height = boardHeight,
                RearRows = rearRows,
                SupportRows = supportRows,
                SpecialTiles = specialTileCoords
                    .Select(c => new GridCoordRecord { X = c.x, Y = c.y })
                    .ToList()
            };
        }
    }
}
