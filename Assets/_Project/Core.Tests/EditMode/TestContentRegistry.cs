using DeadManZone.Core.Board;
using DeadManZone.Core.Content;
using DeadManZone.Core.Shop;

namespace DeadManZone.Core.Tests
{
    public static class TestContentRegistry
    {
        private static ContentRegistry _instance;

        public static ContentRegistry Instance => _instance ??= Create();

        public static ContentRegistry Create()
        {
            var registry = new ContentRegistry();
            registry.Register(TestPieces.RifleSquad(), ShopLane.Offensive);
            registry.Register(TestPieces.CommandBunker(), ShopLane.Defensive);
            registry.Register(TestPieces.HqPiece(), ShopLane.Defensive, includeInShopPool: false);
            registry.Register(TestPieces.SupplyDepot(), ShopLane.Defensive);
            registry.Register(TestPieces.FieldWorkshop(), ShopLane.Defensive);
            registry.Register(TestPieces.GasDrone(), ShopLane.Offensive);
            return registry;
        }
    }
}
