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

        [Header("Zone tile pools (legacy per-cell sprites)")]
        public Sprite[] rearTiles = System.Array.Empty<Sprite>();
        public Sprite[] supportTiles = System.Array.Empty<Sprite>();
        public Sprite[] frontTiles = System.Array.Empty<Sprite>();
        public Sprite[] neutralTiles = System.Array.Empty<Sprite>();

        public Sprite PickTile(ZoneType zone, GridCoord coord)
        {
            var pool = GetPool(zone);
            if (pool == null || pool.Length == 0)
                return null;

            int index = Mathf.Abs(coord.X * 73 + coord.Y * 97) % pool.Length;
            return pool[index];
        }

        public bool HasBattlefieldBackdrop => battlefieldBackdrop != null;

        public bool HasTerrainTiles =>
            !HasBattlefieldBackdrop
            && (rearTiles.Length > 0
                || supportTiles.Length > 0
                || frontTiles.Length > 0
                || neutralTiles.Length > 0);

        private Sprite[] GetPool(ZoneType zone) =>
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
