using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Board
{
    public static class BoardTerrainArtProvider
    {
        public const string ResourcePath = "DeadManZone/BoardTerrainArt";

        private static BoardTerrainArtSO _cached;

        public static BoardTerrainArtSO Current
        {
            get
            {
                if (_cached != null)
                    return _cached;

                _cached = Resources.Load<BoardTerrainArtSO>(ResourcePath);
                return _cached;
            }
        }

        public static void InvalidateCache() => _cached = null;
    }
}
