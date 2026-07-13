using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Shop;
using UnityEngine;

namespace DeadManZone.Presentation.DragDrop
{
    /// <summary>
    /// Cell size/spacing for a shop-offer drag ghost. The ghost has to read as the piece you
    /// are about to place, so its cells are sized off the live board rather than the shop
    /// card. Shared by the legacy ShopOfferDragSource and ShopV2OfferSlotInput so the two
    /// shops cannot drift to different ghost sizes.
    /// </summary>
    public static class ShopDragMetrics
    {
        public static void Resolve(out float cellSize, out float spacing)
        {
            var board = Object.FindFirstObjectByType<BoardView>();
            if (board != null)
            {
                var resolved = ShopLayoutMetrics.Resolve(
                    board.CellSize.x,
                    new Vector2(board.CellSpacing.x, board.CellSpacing.y));
                cellSize = resolved.cellSize;
                spacing = resolved.spacing;
                return;
            }

            var fallback = ShopLayoutMetrics.Resolve(48f, new Vector2(3f, 3f));
            cellSize = fallback.cellSize;
            spacing = fallback.spacing;
        }
    }
}
