namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Shared vertical anchors for board grid, zone strips, and shop lanes within MainRow columns.
    /// </summary>
    public static class BuildLayoutMetrics
    {
        public const float BoardGridBottomY = 0.05f;
        public const float BoardGridTopY = 0.89f;
        public const float BoardGridHorizontalMin = 0.02f;
        public const float BoardGridHorizontalMax = 0.98f;
        public const float ZoneStripMinY = 0f;
        public const float ZoneStripMaxY = 0.045f;
        public const int ShopLaneCount = 3;
        public const float ShopRightInset = 0.98f;
        public const float ShopBottomInset = 0.02f;
        public const float SellAnchorX = 0.76f;
        public const float BeginFightAnchorX = 0.92f;
        public const float BottomBarCenterY = 0.58f;
        public const float BottomBarVerticalOffsetPixels = 10f;
        public const float TopBarAnchorMinY = 0.92f;
        public const float TopBarHudTopInsetPixels = 8f;
        public const float TopBarHudBottomInsetPixels = 2f;
        public const float HudPanelHeightMultiplier = 2f;

        public static float HudPanelAnchorMinY =>
            TopBarAnchorMinY - (1f - TopBarAnchorMinY) * (HudPanelHeightMultiplier - 1f);

        public static float ShopStackBottomY => ZoneStripMaxY + ShopBottomInset;

        public static float ShopStackTopY => BoardGridTopY;

        public static (float minY, float maxY) GetShopLaneAnchors(int laneIndexFromBottom)
        {
            float span = ShopStackTopY - ShopStackBottomY;
            float laneHeight = span / ShopLaneCount;
            float minY = ShopStackBottomY + laneIndexFromBottom * laneHeight;
            return (minY, minY + laneHeight);
        }
    }
}
