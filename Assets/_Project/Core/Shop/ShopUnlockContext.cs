using DeadManZone.Core.Board;
using DeadManZone.Core.Content;

namespace DeadManZone.Core.Shop
{
    public sealed class ShopUnlockContext
    {
        public BoardState Board { get; init; }
        public string FactionId { get; init; }
        public ContentRegistry Registry { get; init; }
        public ShopModifiers Modifiers { get; init; }
    }
}
