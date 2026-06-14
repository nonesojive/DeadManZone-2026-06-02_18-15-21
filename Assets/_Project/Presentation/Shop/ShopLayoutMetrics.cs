using DeadManZone.Core.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Shop
{
    /// <summary>
    /// Derives shop slot dimensions from the live board grid so pieces render at 1:1 scale.
    /// </summary>
    public static class ShopLayoutMetrics
    {
        public const int ViewportCells = 3;
        public const int MaxOffersPerLane = 5;
        public const int MaxGridSlots = 12;
        public const float GridSpacing = 8f;
        public const float NameStripHeight = 22f;
        public const float CardPadding = 8f;
        public const float LaneSpacing = 24f;
        public const float LaneHorizontalPadding = 8f;
        public const float LaneVerticalPadding = 4f;
        public const float OffersWidthFraction = 0.82f;
        public const float OffersHeightFraction = 0.76f;

        public static float IconAreaSize(float cellSize, float spacing) =>
            ViewportCells * cellSize + (ViewportCells - 1) * spacing;

        /// <summary>
        /// Resolves the usable offers region from layout; falls back to parent row size when not yet laid out.
        /// </summary>
        public static (float width, float height) ResolveOffersMetrics(RectTransform offersRoot)
        {
            if (offersRoot == null)
                return (400f, 100f);

            var row = offersRoot.parent as RectTransform;
            if (row != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(row);

            Canvas.ForceUpdateCanvases();

            float width = offersRoot.rect.width;
            float height = offersRoot.rect.height;

            if (row != null)
            {
                float rowW = row.rect.width;
                float rowH = row.rect.height;
                if (width < 48f)
                    width = rowW * OffersWidthFraction;
                if (height < 48f)
                    height = rowH * OffersHeightFraction;
            }

            return (Mathf.Max(width, 64f), Mathf.Max(height, 56f));
        }

        public static (int columns, int rows) GetGridShape(int offerCount) =>
            ShopSlotLayoutResolver.GetGridShape(Mathf.Clamp(offerCount, 1, MaxGridSlots));

        public static Vector2 OfferCardSize(
            float cellSize,
            float spacing,
            float gridInnerWidth,
            float gridInnerHeight,
            int offerCount = 6)
        {
            var (cell, gap) = Resolve(cellSize, new Vector2(spacing, spacing));
            float icon = IconAreaSize(cell, gap);
            var (columns, rows) = GetGridShape(offerCount);

            float usableWidth = gridInnerWidth > 1f
                ? gridInnerWidth - LaneHorizontalPadding
                : icon + CardPadding;

            float usableHeight = gridInnerHeight > 1f
                ? gridInnerHeight - LaneVerticalPadding
                : icon + CardPadding + NameStripHeight;

            float maxSlotWidth = usableWidth > 1f
                ? (usableWidth - GridSpacing * (columns - 1)) / columns
                : icon + CardPadding;

            float maxSlotHeight = usableHeight > 1f
                ? (usableHeight - GridSpacing * (rows - 1)) / rows
                : icon + CardPadding + NameStripHeight;

            float maxSquareFromWidth = maxSlotWidth - CardPadding;
            float maxSquareFromHeight = maxSlotHeight - NameStripHeight - CardPadding;
            float square = Mathf.Min(icon, maxSquareFromWidth, maxSquareFromHeight);
            square = Mathf.Max(square, 24f);

            float height = square + NameStripHeight + CardPadding;
            return new Vector2(square + CardPadding, height);
        }

        public static Vector2 OfferCardSize(
            float cellSize,
            float spacing,
            float laneInnerWidth,
            float laneInnerHeight)
        {
            return OfferCardSize(cellSize, spacing, laneInnerWidth, laneInnerHeight, 6);
        }

        public static (float cellSize, float spacing) Resolve(float cellSize, Vector2 spacing)
        {
            float cell = cellSize > 1f ? cellSize : 48f;
            float gap = spacing.x > 0f ? spacing.x : 3f;
            return (cell, gap);
        }
    }
}
