using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Shop;

namespace DeadManZone.Core.Content
{
    public sealed class ContentRegistry
    {
        private readonly Dictionary<string, PieceDefinition> _piecesById = new();
        private readonly Dictionary<ShopLane, List<PieceDefinition>> _pools = new();

        public void Register(PieceDefinition piece, ShopLane lane)
        {
            _piecesById[piece.Id] = piece;
            if (!_pools.TryGetValue(lane, out var pool))
            {
                pool = new List<PieceDefinition>();
                _pools[lane] = pool;
            }

            pool.Add(piece);
        }

        public PieceDefinition GetById(string pieceId) => _piecesById[pieceId];

        public IReadOnlyList<PieceDefinition> GetPool(ShopLane lane) =>
            _pools.TryGetValue(lane, out var pool) ? pool : System.Array.Empty<PieceDefinition>();

        public IReadOnlyList<PieceDefinition> GetBuildings() =>
            _piecesById.Values
                .Where(p => p.Category is PieceCategory.Building or PieceCategory.Hybrid)
                .ToList();
    }
}
