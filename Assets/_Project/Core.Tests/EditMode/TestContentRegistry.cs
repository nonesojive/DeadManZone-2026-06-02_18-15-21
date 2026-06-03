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
            registry.Register(TestPieces.RifleSquad(), ShopLane.General);
            registry.Register(TestPieces.CommandBunker(), ShopLane.Engineers);
            registry.Register(TestPieces.SupplyDepot(), ShopLane.Engineers);
            registry.Register(TestPieces.FieldWorkshop(), ShopLane.Engineers);
            registry.Register(TestPieces.GasDrone(), ShopLane.Requisition);
            return registry;
        }
    }
}
