using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Faction")]
    public class FactionSO : ScriptableObject
    {
        public string factionId = FactionIds.IronmarchUnion;
        public string displayName = "IronMarch Union";

        [Header("Combat board")]
        public int combatBoardSize = 6;
        public Vector2Int[] combatSpecialTileCoords = System.Array.Empty<Vector2Int>();

        [Header("HQ board")]
        public int hqBoardWidth = 3;
        public int hqBoardHeight = 6;
        public Vector2Int[] hqBlockedCells = System.Array.Empty<Vector2Int>();
        public Vector2Int[] hqSpecialTileCoords = System.Array.Empty<Vector2Int>();

        [Header("Legacy (deprecated)")]
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

        [Header("Income")]
        public int baseSuppliesPerRound;

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

        public BoardLayout CreateCombatBoardLayout()
        {
            var specialTiles = ToGridCoords(combatSpecialTileCoords);
            return BoardLayout.CreateCombatBoard(combatBoardSize, specialTiles);
        }

        public BoardLayout CreateHqBoardLayout()
        {
            return BoardLayout.CreateHqBoard(
                hqBoardWidth,
                hqBoardHeight,
                ToGridCoords(hqBlockedCells),
                ToGridCoords(hqSpecialTileCoords));
        }

        public BoardSnapshot CreateEmptyCombatBoardSnapshot()
        {
            var layout = CreateCombatBoardLayout();
            return BoardSnapshotMapper.FromBoard(new BoardState(layout));
        }

        public BoardSnapshot CreateEmptyHqBoardSnapshot()
        {
            var layout = CreateHqBoardLayout();
            return BoardSnapshotMapper.FromBoard(new BoardState(layout));
        }

        [System.Obsolete("Use CreateCombatBoardLayout for schema v8.")]
        public BoardLayout CreateBoardLayout()
        {
            var specialTiles = ToGridCoords(specialTileCoords);
            return BoardLayout.CreateHorizontalZones(
                boardWidth,
                boardHeight,
                rearCols,
                supportCols,
                specialTiles);
        }

        [System.Obsolete("Use CreateEmptyCombatBoardSnapshot and CreateEmptyHqBoardSnapshot.")]
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

        private static GridCoord[] ToGridCoords(Vector2Int[] coords)
        {
            if (coords == null || coords.Length == 0)
                return System.Array.Empty<GridCoord>();

            var tiles = new GridCoord[coords.Length];
            for (int i = 0; i < coords.Length; i++)
                tiles[i] = new GridCoord(coords[i].x, coords[i].y);
            return tiles;
        }
    }
}
