using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Board Terrain Art")]
    public sealed class BoardTerrainArtSO : ScriptableObject
    {
        [Header("Battlefield backdrop")]
        [Tooltip("Single full-board image; when set, per-cell terrain tiles are ignored.")]
        public Sprite battlefieldBackdrop;

        [Header("Uniform cell texture")]
        [Tooltip("When set, every board cell uses this sprite instead of per-board tile pools.")]
        public Sprite cellSprite;

        [Header("Board-kind tile pools")]
        public Sprite[] combatBoardTiles = System.Array.Empty<Sprite>();
        [Tooltip("Rightmost combat column (enemy-facing edge). Falls back to combatBoardTiles when empty.")]
        public Sprite[] combatFrontColumnTiles = System.Array.Empty<Sprite>();
        public Sprite[] hqBoardTiles = System.Array.Empty<Sprite>();

        [Header("Zone tile pools (legacy — used only when board-kind pools are empty)")]
        public Sprite[] rearTiles = System.Array.Empty<Sprite>();
        public Sprite[] supportTiles = System.Array.Empty<Sprite>();
        public Sprite[] frontTiles = System.Array.Empty<Sprite>();
        public Sprite[] neutralTiles = System.Array.Empty<Sprite>();

        [Header("Reserves slot pool")]
        public Sprite[] reserveSlotTiles = System.Array.Empty<Sprite>();

        public Sprite PickTile(BoardKind boardKind, GridCoord coord, int boardWidth = 6)
        {
            if (cellSprite != null)
                return cellSprite;

            var pool = ResolvePool(boardKind, coord, boardWidth);
            if (pool == null || pool.Length == 0)
                return null;

            int index = Mathf.Abs(coord.X * 73 + coord.Y * 97) % pool.Length;
            return pool[index];
        }

        /// <summary>Legacy zone picker — kept for older callers.</summary>
        public Sprite PickTile(ZoneType zone, GridCoord coord)
        {
            if (cellSprite != null)
                return cellSprite;

            if (HasBoardKindTiles)
                return PickTile(BoardKind.Combat, coord);

            var pool = GetLegacyPool(zone);
            if (pool == null || pool.Length == 0)
                return null;

            int index = Mathf.Abs(coord.X * 73 + coord.Y * 97) % pool.Length;
            return pool[index];
        }

        public Sprite PickReserveSlot(GridCoord coord)
        {
            if (reserveSlotTiles == null || reserveSlotTiles.Length == 0)
                return null;

            int index = Mathf.Abs(coord.X * 53 + coord.Y * 89) % reserveSlotTiles.Length;
            return reserveSlotTiles[index];
        }

        public bool HasBattlefieldBackdrop => battlefieldBackdrop != null;

        public bool HasBoardKindTiles =>
            combatBoardTiles.Length > 0
            || combatFrontColumnTiles.Length > 0
            || hqBoardTiles.Length > 0;

        public bool HasTerrainTiles =>
            !HasBattlefieldBackdrop
            && (cellSprite != null
                || HasBoardKindTiles
                || rearTiles.Length > 0
                || supportTiles.Length > 0
                || frontTiles.Length > 0
                || neutralTiles.Length > 0);

        private Sprite[] ResolvePool(BoardKind boardKind, GridCoord coord, int boardWidth)
        {
            if (boardKind == BoardKind.Hq && hqBoardTiles.Length > 0)
                return hqBoardTiles;

            if (boardKind == BoardKind.Combat)
            {
                bool isFrontColumn = boardWidth > 0 && coord.X >= boardWidth - 1;
                if (isFrontColumn && combatFrontColumnTiles.Length > 0)
                    return combatFrontColumnTiles;

                if (combatBoardTiles.Length > 0)
                    return combatBoardTiles;
            }

            if (HasLegacyZoneTiles)
                return GetLegacyPool(ZoneType.Support);

            return null;
        }

        private bool HasLegacyZoneTiles =>
            rearTiles.Length > 0
            || supportTiles.Length > 0
            || frontTiles.Length > 0
            || neutralTiles.Length > 0;

        private Sprite[] GetLegacyPool(ZoneType zone) =>
            zone switch
            {
                ZoneType.Rear => rearTiles,
                ZoneType.Support => supportTiles,
                ZoneType.Front => frontTiles,
                ZoneType.Neutral => neutralTiles.Length > 0 ? neutralTiles : supportTiles,
                _ => supportTiles
            };
    }
}
