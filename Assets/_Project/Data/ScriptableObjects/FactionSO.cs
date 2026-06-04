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
        public int boardWidth = 9;
        public int boardHeight = 6;
        public int rearCols = 3;
        public int supportCols = 3;
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

            return BoardLayout.CreateHorizontalZones(
                boardWidth,
                boardHeight,
                rearCols,
                supportCols,
                specialTiles);
        }

        public BoardSnapshot CreateEmptyBoardSnapshot()
        {
            return new BoardSnapshot
            {
                Width = boardWidth,
                Height = boardHeight,
                RearCols = rearCols,
                SupportCols = supportCols,
                SpecialTiles = specialTileCoords
                    .Select(c => new GridCoordRecord { X = c.x, Y = c.y })
                    .ToList()
            };
        }
    }
}
