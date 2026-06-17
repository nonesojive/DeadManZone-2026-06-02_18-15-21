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
        public string displayName = "IronMarch Union";
        public int boardWidth = 9;
        public int boardHeight = 10;
        public int rearCols = 4;
        public int supportCols = 3;
        public Vector2Int[] specialTileCoords =
        {
            new Vector2Int(1, 4),
            new Vector2Int(4, 4),
            new Vector2Int(7, 4)
        };
        public int startingSupplies = 100;
        public int startingManpower = 100;

        [Header("Manpower")]
        public int baseMusterPerShop = 12;
        public int startingAuthority = 2;
        public int startingMorale = 100;

        [Header("Salvage")]
        [Range(0, 50)]
        public int baseSalvageChancePercent = 10;

        [Header("Shop")]
        public FactionShopOverrideSO shopOverride;

        [Header("Visuals")]
        [Tooltip("Semi-transparent fill behind unit tokens on board, shop, and drag ghost.")]
        public Color tokenBackgroundColor = new Color(0f, 0f, 0f, 0f);

        [Header("HQ")]
        public string hqPieceId = "ironmarch_hq";
        public Vector2Int hqSpawnAnchor = new(0, 4);
        public int hqSpawnRotation = 0;

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
