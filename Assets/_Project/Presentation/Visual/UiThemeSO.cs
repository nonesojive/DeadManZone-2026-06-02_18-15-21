using DeadManZone.Core.Board;
using DeadManZone.Core.Shop;
using UnityEngine;

namespace DeadManZone.Presentation.Visual
{
    [CreateAssetMenu(menuName = "DeadManZone/UI Theme")]
    public sealed class UiThemeSO : ScriptableObject
    {
        [Header("Surfaces")]
        public Color backgroundColor = new(0.08f, 0.09f, 0.11f, 1f);
        public Color panelColor = new(0.12f, 0.13f, 0.16f, 0.96f);
        public Color cardColor = new(0.16f, 0.17f, 0.21f, 0.98f);
        public Color resourcePanelBackgroundColor = new(0.24f, 0.16f, 0.09f, 1f);

        [Header("Accents")]
        public Color accentColor = new(0.72f, 0.58f, 0.28f, 1f);
        public Color accentMutedColor = new(0.45f, 0.36f, 0.18f, 1f);
        public Color dangerColor = new(0.75f, 0.22f, 0.18f, 1f);
        public Color sellZoneColor = new(0.32f, 0.12f, 0.12f, 0.85f);

        [Header("Text")]
        public Color textPrimary = new(0.92f, 0.9f, 0.84f, 1f);
        public Color textSecondary = new(0.65f, 0.62f, 0.56f, 1f);
        public Color textOnAccent = new(0.12f, 0.1f, 0.08f, 1f);

        [Header("Buttons")]
        public Color buttonNormal = new(0.2f, 0.21f, 0.26f, 0.98f);
        public Color buttonHighlighted = new(0.32f, 0.34f, 0.42f, 1f);
        public Color buttonPressed = new(0.14f, 0.15f, 0.18f, 1f);

        [Header("Board zones")]
        public Color rearZoneColor = new(0.20f, 0.34f, 0.52f, 1f);
        public Color supportZoneColor = new(0.22f, 0.42f, 0.30f, 1f);
        public Color frontZoneColor = new(0.52f, 0.24f, 0.24f, 1f);
        public Color neutralZoneColor = new(0.38f, 0.30f, 0.24f, 1f);
        public Color specialTileColor = new(0.55f, 0.48f, 0.22f, 0.45f);
        public Color tileHoverColor = new(0.35f, 0.38f, 0.45f, 0.5f);
        public Color invalidPlacementColor = new(0.8f, 0.15f, 0.12f, 0.55f);

        [Header("Shop lanes")]
        public Color generalLaneTint = new(0.2f, 0.22f, 0.28f, 0.35f);
        public Color engineersLaneTint = new(0.18f, 0.24f, 0.2f, 0.35f);
        public Color requisitionLaneTint = new(0.26f, 0.18f, 0.24f, 0.35f);

        [Header("Shop background")]
        public Sprite shopBackgroundSprite;
        public Color shopBackgroundScrimColor = new(0.04f, 0.05f, 0.07f, 0.52f);
        [Range(0.1f, 1f)]
        public float shopLaneTintScaleWithBackground = 0.45f;

        [Header("Board terrain")]
        [Range(0f, 1f)]
        public float terrainZoneTintStrength = 0.2f;
        [Range(0f, 0.5f)]
        public float boardCellZoneOverlayAlpha = 0.1f;
        public Color boardGridLineColor = new(1f, 1f, 1f, 0.14f);
        public Color boardZoneDividerColor = new(1f, 1f, 1f, 0.28f);

        [Header("Piece token backgrounds")]
        public Color neutralTokenBackgroundColor = new(0.32f, 0.33f, 0.36f, 0.42f);

        [Header("Piece categories")]
        public Color unitTint = new(0.35f, 0.42f, 0.55f, 1f);
        public Color buildingTint = new(0.48f, 0.4f, 0.28f, 1f);
        public Color hybridTint = new(0.38f, 0.32f, 0.48f, 1f);

        [Header("Combat overlay")]
        public Color combatOverlayColor = new(0.02f, 0.03f, 0.05f, 0.55f);
        public Color combatBannerColor = new(0.1f, 0.1f, 0.12f, 0.88f);

        [Header("Sprites (optional 9-slice)")]
        public Sprite panelSprite;
        public Sprite cardSprite;
        public Sprite modalFrameSprite;
        public Sprite sidebarPanelSprite;
        public Sprite inventoryPanelSprite;
        public Sprite securityTerminalFrameSprite;
        public Sprite bannerSprite;
        public Sprite buttonNormalSprite;
        public Sprite buttonHighlightedSprite;
        public Sprite buttonPressedSprite;
        public Sprite buttonDisabledSprite;
        public Sprite accentButtonSprite;
        public Sprite secondaryButtonSprite;
        public Sprite dangerButtonSprite;
        public Sprite warningButtonSprite;
        public Sprite sellZoneSprite;
        public Sprite sellZoneIconSprite;
        public Sprite slotEmptySprite;
        public Sprite slotSelectedSprite;
        public Sprite storageSlotEmptySprite;
        public Sprite storageSlotSelectedSprite;

        [Header("Sprites (background plates)")]
        public Sprite menuBackgroundSprite;
        public Sprite runBackgroundSprite;
        public Sprite combatBackgroundSprite;

        public bool UsesButtonSprites =>
            buttonNormalSprite != null
            || accentButtonSprite != null
            || dangerButtonSprite != null;

        public bool UsesSlotSprites => slotEmptySprite != null || storageSlotEmptySprite != null;

        public Color GetZoneColor(ZoneType zone) =>
            zone switch
            {
                ZoneType.Rear => rearZoneColor,
                ZoneType.Support => supportZoneColor,
                ZoneType.Front => frontZoneColor,
                ZoneType.Neutral => neutralZoneColor,
                _ => supportZoneColor
            };

        public Color GetCategoryTint(PieceCategory category) =>
            category switch
            {
                PieceCategory.Unit => unitTint,
                PieceCategory.Building => buildingTint,
                PieceCategory.Hybrid => hybridTint,
                _ => unitTint
            };

        public Color GetLaneTint(ShopLane lane) =>
            lane switch
            {
                ShopLane.Offensive => generalLaneTint,
                ShopLane.Defensive => engineersLaneTint,
                ShopLane.Specialty => requisitionLaneTint,
                _ => generalLaneTint
            };

        public Color GetTileDisplayColor(Color zoneColor) =>
            slotEmptySprite != null ? Color.Lerp(Color.white, zoneColor, 0.35f) : zoneColor;

        public Color GetTerrainTileTint(Color zoneColor) =>
            Color.Lerp(Color.white, zoneColor, terrainZoneTintStrength);

        public Color GetBoardCellOverlayColor(Color zoneColor) =>
            new Color(zoneColor.r, zoneColor.g, zoneColor.b, boardCellZoneOverlayAlpha);

        public Color GetReserveSlotColor() =>
            storageSlotEmptySprite != null ? new Color(0.92f, 0.91f, 0.88f, 1f) : cardColor;

        public void ApplyIronVanguardDefaults()
        {
            // Values match serialized defaults; explicit for editor menu creation.
        }

        public void ApplyBunkerSurvivalDefaults()
        {
            backgroundColor = new Color(0.06f, 0.07f, 0.08f, 1f);
            panelColor = new Color(0.14f, 0.15f, 0.13f, 0.96f);
            cardColor = new Color(0.18f, 0.19f, 0.17f, 0.98f);
            accentColor = new Color(0.85f, 0.65f, 0.2f, 1f);
            accentMutedColor = new Color(0.55f, 0.42f, 0.12f, 1f);
            dangerColor = new Color(0.78f, 0.22f, 0.15f, 1f);
            sellZoneColor = new Color(0.42f, 0.14f, 0.1f, 0.85f);
            textPrimary = new Color(0.88f, 0.92f, 0.84f, 1f);
            textSecondary = new Color(0.55f, 0.6f, 0.52f, 1f);
            textOnAccent = new Color(0.1f, 0.08f, 0.06f, 1f);
            buttonNormal = new Color(0.22f, 0.24f, 0.21f, 0.98f);
            buttonHighlighted = new Color(0.32f, 0.34f, 0.3f, 1f);
            buttonPressed = new Color(0.14f, 0.15f, 0.13f, 1f);
            rearZoneColor = new Color(0.18f, 0.28f, 0.38f, 1f);
            supportZoneColor = new Color(0.2f, 0.32f, 0.24f, 1f);
            frontZoneColor = new Color(0.48f, 0.22f, 0.18f, 1f);
            neutralZoneColor = new Color(0.32f, 0.28f, 0.22f, 1f);
            specialTileColor = new Color(0.62f, 0.52f, 0.18f, 0.45f);
            combatOverlayColor = new Color(0.02f, 0.03f, 0.04f, 0.6f);
            combatBannerColor = new Color(0.08f, 0.09f, 0.08f, 0.9f);
        }
    }
}
