using System;
using DeadManZone.Core.Board;
using DeadManZone.Core.Shop;
using DeadManZone.Data;
using DeadManZone.Game.Dev;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.DragDrop;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Shop
{
    public sealed class ShopOfferView : MonoBehaviour
    {
        [SerializeField] private Image cardBackground;
        [SerializeField] private RectTransform squareRoot;
        [SerializeField] private RectTransform previewRoot;
        [SerializeField] private ShopPiecePreview piecePreview;
        [SerializeField] private RectTransform nameStripRoot;
        [SerializeField] private TMP_Text pieceIdText;
        [SerializeField] private Image priceBadgeBackground;
        [SerializeField] private TMP_Text priceBadgeText;
        [SerializeField] private Button lockIconButton;
        [SerializeField] private Image lockIconImage;
        [SerializeField] private Image lockedIndicator;
        [SerializeField] private ShopOfferDragSource dragSource;

        private ShopOffer _offer;
        private bool _isLocked;
        private ContentDatabase _database;
        private float _cellSize;
        private float _spacing;
        private float _laneInnerWidth;
        private float _laneInnerHeight;

        public string OfferId => _offer?.OfferId;
        public ShopLane Lane => _offer?.Lane ?? ShopLane.Offensive;
        public bool IsPreviewVisible => previewRoot == null || previewRoot.gameObject.activeSelf;

        public event Action<ShopOffer, bool> LockToggled;

        public void InitializeForTests(RectTransform preview)
        {
            previewRoot = preview;
        }

        private void Awake()
        {
            _database = ContentDatabase.Load();

            if (lockIconButton != null)
                lockIconButton.onClick.AddListener(OnLockClicked);

            if (pieceIdText != null)
                pieceIdText.raycastTarget = false;
            if (priceBadgeText != null)
                priceBadgeText.raycastTarget = false;

            if (dragSource == null)
                dragSource = GetComponentInChildren<ShopOfferDragSource>();

            if (piecePreview == null && previewRoot != null)
                piecePreview = previewRoot.GetComponent<ShopPiecePreview>();

            if (piecePreview == null)
                piecePreview = GetComponentInChildren<ShopPiecePreview>(true);
        }

        public void SetPreviewVisible(bool visible)
        {
            if (previewRoot != null)
                previewRoot.gameObject.SetActive(visible);
        }

        public void ConfigureLayout(float cellSize, float spacing, float laneInnerWidth, float laneInnerHeight)
        {
            var (cell, gap) = ShopLayoutMetrics.Resolve(cellSize, new Vector2(spacing, spacing));
            var cardSize = ShopLayoutMetrics.OfferCardSize(cell, gap, laneInnerWidth, laneInnerHeight);
            float square = cardSize.y - ShopLayoutMetrics.NameStripHeight - ShopLayoutMetrics.CardPadding;

            ApplyLayoutElement(gameObject, cardSize.x, cardSize.y);

            if (squareRoot != null)
            {
                squareRoot.sizeDelta = new Vector2(square, square);
                ApplyLayoutElement(squareRoot.gameObject, square, square);
            }

            if (previewRoot != null)
            {
                previewRoot.anchorMin = Vector2.zero;
                previewRoot.anchorMax = Vector2.one;
                previewRoot.offsetMin = Vector2.zero;
                previewRoot.offsetMax = Vector2.zero;
            }

            if (nameStripRoot != null)
            {
                nameStripRoot.sizeDelta = new Vector2(square, ShopLayoutMetrics.NameStripHeight);
                ApplyLayoutElement(nameStripRoot.gameObject, square, ShopLayoutMetrics.NameStripHeight);
            }
        }

        public void Bind(ShopOffer offer, bool isLocked, float cellSize, float spacing, float laneInnerWidth, float laneInnerHeight)
        {
            _offer = offer;
            _isLocked = isLocked;
            _cellSize = cellSize;
            _spacing = spacing;
            _laneInnerWidth = laneInnerWidth;
            _laneInnerHeight = laneInnerHeight;

            ConfigureLayout(cellSize, spacing, laneInnerWidth, laneInnerHeight);

            EnsurePiecePreview();

            var source = PieceVisualLookup.GetSource(offer.PieceId);
            var registry = _database != null ? ContentRegistryProvider.Build(_database) : ContentRegistryProvider.Build(ContentDatabase.Load());
            PieceDefinition definition = null;
            if (registry != null)
                registry.TryGetById(offer.PieceId, out definition);
            definition ??= source?.ToCore();

            if (cardBackground != null)
                UiThemeApplicator.ApplyCard(cardBackground);

            var (cell, gap) = ShopLayoutMetrics.Resolve(cellSize, new Vector2(spacing, spacing));
            var cardSize = ShopLayoutMetrics.OfferCardSize(cell, gap, laneInnerWidth, laneInnerHeight);
            float viewport = cardSize.y - ShopLayoutMetrics.NameStripHeight - ShopLayoutMetrics.CardPadding;

            if (piecePreview != null && definition != null)
            {
                Canvas.ForceUpdateCanvases();
                piecePreview.Render(definition, source, cell, gap, viewport);
            }

            if (pieceIdText != null)
                pieceIdText.text = source != null && !string.IsNullOrEmpty(source.displayName)
                    ? source.displayName
                    : offer.PieceId;

            if (priceBadgeText != null)
                priceBadgeText.text = BuildPriceLabel(offer);

            if (priceBadgeBackground != null)
                UiThemeApplicator.ApplyCard(priceBadgeBackground);

            if (lockedIndicator != null)
            {
                lockedIndicator.enabled = isLocked;
                if (isLocked)
                    lockedIndicator.color = new Color(
                        UiThemeProvider.Current.accentColor.r,
                        UiThemeProvider.Current.accentColor.g,
                        UiThemeProvider.Current.accentColor.b,
                        0.35f);
            }

            UpdateLockIcon(isLocked);

            if (dragSource != null)
                dragSource.SetOffer(offer);
        }

        private void EnsurePiecePreview()
        {
            if (previewRoot == null && squareRoot != null)
            {
                var found = squareRoot.Find("PreviewRoot");
                if (found != null)
                    previewRoot = found.GetComponent<RectTransform>();
            }

            if (previewRoot == null)
                return;

#if UNITY_EDITOR
            UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(previewRoot.gameObject);
#endif

            piecePreview = previewRoot.GetComponent<ShopPiecePreview>();
            if (piecePreview == null)
                piecePreview = previewRoot.gameObject.AddComponent<ShopPiecePreview>();

            var blocks = previewRoot.Find("Blocks") as RectTransform;
            if (blocks != null)
                piecePreview.Initialize(blocks);
        }

        private void UpdateLockIcon(bool isLocked)
        {
            if (lockIconImage != null)
            {
                lockIconImage.color = isLocked
                    ? UiThemeProvider.Current.accentColor
                    : UiThemeProvider.Current.textSecondary;
            }

            var lockLabel = lockIconButton != null
                ? lockIconButton.GetComponentInChildren<TMP_Text>()
                : null;
            if (lockLabel != null)
                lockLabel.text = isLocked ? "\u2713" : "\u25CB";
        }

        private static string BuildPriceLabel(ShopOffer offer)
        {
            if (offer.RequisitionPrice > 0 && offer.GoldPrice > 0)
                return $"{offer.GoldPrice}G + {offer.RequisitionPrice}R";
            if (offer.RequisitionPrice > 0)
                return $"{offer.RequisitionPrice}R";
            return $"{offer.GoldPrice}G";
        }

        private void OnLockClicked()
        {
            if (_offer == null)
                return;

            _isLocked = !_isLocked;
            LockToggled?.Invoke(_offer, _isLocked);
            Bind(_offer, _isLocked, _cellSize, _spacing, _laneInnerWidth, _laneInnerHeight);
        }

        private static void ApplyLayoutElement(GameObject target, float width, float height)
        {
            var layout = target.GetComponent<LayoutElement>();
            if (layout == null)
                layout = target.AddComponent<LayoutElement>();
            layout.minWidth = width;
            layout.minHeight = height;
            layout.preferredWidth = width;
            layout.preferredHeight = height;
        }
    }
}
