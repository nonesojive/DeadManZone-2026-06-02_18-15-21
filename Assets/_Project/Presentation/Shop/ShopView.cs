using System.Collections;
using System.Collections.Generic;
using System.Text;
using DeadManZone.Core.Shop;
using DeadManZone.Game;
using DeadManZone.Presentation.Board;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Shop
{
    public sealed class ShopView : MonoBehaviour
    {
        [Header("Shop Grid")]
        [SerializeField] private Transform offersGridRoot;

        [Header("Legacy Lane Roots (hidden when unified grid is active)")]
        [SerializeField] private Transform generalLaneRoot;
        [SerializeField] private Transform engineersLaneRoot;
        [SerializeField] private Transform requisitionLaneRoot;

        [Header("Controls")]
        [SerializeField] private Button rerollButton;
        [SerializeField] private Button rerollGeneralButton;
        [SerializeField] private Button rerollEngineersButton;
        [SerializeField] private Button rerollRequisitionButton;

        [Header("Cards")]
        [SerializeField] private GameObject offerCardPrefab;
        [SerializeField] private TMP_Text modifiersTooltipText;
        [SerializeField] private BoardView boardView;

        private Coroutine _deferredRefresh;
        private ShopOfferView[] _slotViews;
        private bool[] _lockHandlersAttached;
        private bool _legacyOfferChildrenPurged;

        private const string SlotNamePrefix = "OfferSlot_";

        public TMP_Text ModifiersTooltip => modifiersTooltipText;

        private void Awake()
        {
            if (rerollButton != null)
                rerollButton.onClick.AddListener(OnRerollClicked);
            else if (rerollGeneralButton != null)
                rerollGeneralButton.onClick.AddListener(OnRerollClicked);

            if (rerollEngineersButton != null)
                rerollEngineersButton.onClick.RemoveAllListeners();
            if (rerollRequisitionButton != null)
                rerollRequisitionButton.onClick.RemoveAllListeners();

            HideLegacyLaneRows();
        }

        private void OnEnable()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.RunStateChanged += OnRunStateChanged;
            boardView?.HidePieceHoverCard();
            RefreshFromRunManager();
            ScheduleDeferredRefresh();
        }

        private void OnDisable()
        {
            if (_deferredRefresh != null)
            {
                StopCoroutine(_deferredRefresh);
                _deferredRefresh = null;
            }

            if (RunManager.Instance != null)
                RunManager.Instance.RunStateChanged -= OnRunStateChanged;
        }

        private void ScheduleDeferredRefresh()
        {
            if (!isActiveAndEnabled)
                return;

            if (_deferredRefresh != null)
                StopCoroutine(_deferredRefresh);
            _deferredRefresh = StartCoroutine(DeferredRefresh());
        }

        private IEnumerator DeferredRefresh()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            RefreshFromRunManager();
            yield return null;
            Canvas.ForceUpdateCanvases();
            RefreshFromRunManager();
            _deferredRefresh = null;
        }

        public void Render(ShopState state, string nextEnemyTag)
        {
            if (state == null)
                return;

            Canvas.ForceUpdateCanvases();
            var (cellSize, spacing) = ResolveBoardMetrics();
            var gridRoot = ResolveOffersGridRoot();
            if (gridRoot == null || offerCardPrefab == null)
                return;

            int offerCount = state.Offers?.Count ?? 0;
            int layoutOfferCount = Mathf.Max(offerCount, ShopSlotLayoutResolver.VisibleOfferSlotCount);
            var (gridWidth, gridHeight) = gridRoot is RectTransform gridRect
                ? ShopLayoutMetrics.ResolveOffersMetrics(gridRect)
                : (400f, 300f);

            ConfigureOffersGrid(gridRoot, layoutOfferCount, cellSize, spacing, gridWidth, gridHeight);

            if (!_legacyOfferChildrenPurged)
            {
                PurgeLegacyOfferChildren(gridRoot);
                _legacyOfferChildrenPurged = true;
            }

            EnsureSlotViews(gridRoot, layoutOfferCount, cellSize, spacing, gridWidth, gridHeight);

            var offersBySlot = BuildOffersBySlot(state.Offers);
            for (int slot = 0; slot < ShopSlotLayoutResolver.VisibleOfferSlotCount; slot++)
            {
                var card = _slotViews[slot];
                if (offersBySlot.TryGetValue(slot, out var offer))
                {
                    bool isLocked = RunManager.Instance is { HasActiveRun: true } manager &&
                        manager.Orchestrator.IsOfferLocked(offer);
                    card.Bind(offer, isLocked, cellSize, spacing, gridWidth, gridHeight, layoutOfferCount);
                }
                else
                {
                    card.ShowEmpty(cellSize, spacing, gridWidth, gridHeight, layoutOfferCount);
                }
            }

            if (gridRoot is RectTransform offersRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(offersRect);

            UpdateModifierTooltip(state.Modifiers, nextEnemyTag);
        }

        public void InitializeForTests(
            Transform gridRoot,
            GameObject offerPrefab,
            TMP_Text tooltipText,
            BoardView board = null)
        {
            offersGridRoot = gridRoot;
            offerCardPrefab = offerPrefab;
            modifiersTooltipText = tooltipText;
            boardView = board;
        }

        private void OnRunStateChanged(Core.Run.RunState _) => RefreshFromRunManager();

        public void RefreshFromRunManager()
        {
            if (RunManager.Instance == null || !RunManager.Instance.HasActiveRun)
                return;

            var state = RunManager.Instance.State;
            Render(state.Shop, RunManager.Instance.Orchestrator.GetNextEnemyPreviewTag());
        }

        private (float cellSize, float spacing) ResolveBoardMetrics()
        {
            if (boardView == null)
                boardView = FindFirstObjectByType<BoardView>();

            if (boardView == null)
                return ShopLayoutMetrics.Resolve(48f, new Vector2(3f, 3f));

            return ShopLayoutMetrics.Resolve(boardView.CellSize.x, boardView.CellSpacing);
        }

        private Transform ResolveOffersGridRoot()
        {
            if (offersGridRoot != null)
                return offersGridRoot;

            var named = transform.Find("OffersGrid");
            if (named != null)
            {
                offersGridRoot = named;
                return offersGridRoot;
            }

            if (generalLaneRoot != null)
            {
                var offers = generalLaneRoot.Find("Offers");
                if (offers != null)
                {
                    offersGridRoot = offers;
                    return offersGridRoot;
                }

                offersGridRoot = generalLaneRoot;
                return offersGridRoot;
            }

            return null;
        }

        private static void ConfigureOffersGrid(
            Transform gridRoot,
            int offerCount,
            float cellSize,
            float spacing,
            float gridWidth,
            float gridHeight)
        {
            if (gridRoot is not RectTransform grid)
                return;

            grid.anchorMin = new Vector2(0.04f, 0.02f);
            grid.anchorMax = new Vector2(0.96f, 0.96f);
            grid.offsetMin = Vector2.zero;
            grid.offsetMax = Vector2.zero;

            var horizontal = grid.GetComponent<HorizontalLayoutGroup>();
            if (horizontal != null)
                Destroy(horizontal);

            var gridLayout = grid.GetComponent<GridLayoutGroup>();
            if (gridLayout == null)
                gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();

            int count = Mathf.Max(offerCount, 1);
            var cardSize = ShopLayoutMetrics.OfferCardSize(
                cellSize, spacing, gridWidth, gridHeight, count);

            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = ShopSlotLayoutResolver.GridColumns;
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.spacing = new Vector2(ShopLayoutMetrics.GridSpacing, ShopLayoutMetrics.GridSpacing);
            gridLayout.padding = new RectOffset(4, 4, 4, 4);
            gridLayout.cellSize = cardSize;
        }

        private void HideLegacyLaneRows()
        {
            foreach (var rowName in new[] { "OffensiveRow", "DefensiveRow", "SpecialtyRow" })
            {
                var row = transform.Find(rowName);
                if (row != null)
                    row.gameObject.SetActive(false);
            }
        }

        private void OnLockToggled(ShopOffer offer, bool shouldLock)
        {
            if (RunManager.Instance == null)
                return;

            RunManager.Instance.SetLockedOffer(offer, shouldLock);
            RefreshFromRunManager();
        }

        private void OnRerollClicked()
        {
            if (RunManager.Instance == null)
                return;

            RunManager.Instance.TryRerollShop();
            RefreshFromRunManager();
        }

        private void UpdateModifierTooltip(ShopModifiers modifiers, string nextEnemyTag)
        {
            if (modifiersTooltipText == null || modifiers == null)
                return;

            if (ShopRerollTooltip.AnyHovered)
                return;

            var sb = new StringBuilder();
            if (modifiers.GoldDiscountPercent > 0)
                sb.AppendLine($"{modifiers.GoldDiscountPercent}% gold discount");
            if (modifiers.ExtraGeneralSlots > 0)
                sb.AppendLine($"+{modifiers.ExtraGeneralSlots} extra shop slot");
            if (modifiers.GuaranteeEngineerOffer)
                sb.AppendLine("Guaranteed engineer offer");
            if (modifiers.EnemyTagPreview)
                sb.AppendLine($"Enemy preview: {nextEnemyTag ?? "Unknown"}");

            modifiersTooltipText.text = sb.Length == 0
                ? "Drag pieces to reserves or board. Drop on Sell to refund."
                : sb.ToString().TrimEnd();
        }

        private static Dictionary<int, ShopOffer> BuildOffersBySlot(IReadOnlyList<ShopOffer> offers)
        {
            var offersBySlot = new Dictionary<int, ShopOffer>();
            if (offers == null)
                return offersBySlot;

            foreach (var offer in offers)
            {
                if (offer == null || offer.SlotIndex < 0 || offer.SlotIndex >= ShopSlotLayoutResolver.VisibleOfferSlotCount)
                    continue;

                offersBySlot[offer.SlotIndex] = offer;
            }

            return offersBySlot;
        }

        private void EnsureSlotViews(
            Transform gridRoot,
            int layoutOfferCount,
            float cellSize,
            float spacing,
            float gridWidth,
            float gridHeight)
        {
            int slotCount = ShopSlotLayoutResolver.VisibleOfferSlotCount;
            if (_slotViews == null || _slotViews.Length != slotCount)
                _slotViews = new ShopOfferView[slotCount];

            if (_lockHandlersAttached == null || _lockHandlersAttached.Length != slotCount)
                _lockHandlersAttached = new bool[slotCount];

            for (int slot = 0; slot < slotCount; slot++)
            {
                string slotName = $"{SlotNamePrefix}{slot}";
                var slotTransform = gridRoot.Find(slotName);
                ShopOfferView view = slotTransform != null ? slotTransform.GetComponent<ShopOfferView>() : null;

                if (view == null)
                {
                    var cardObject = Instantiate(offerCardPrefab, gridRoot);
                    cardObject.name = slotName;
                    cardObject.SetActive(true);
                    view = cardObject.GetComponent<ShopOfferView>();
                    if (view == null)
                    {
                        Debug.LogError(
                            $"Shop offer card prefab '{offerCardPrefab.name}' is missing ShopOfferView. Add it on the prefab — runtime AddComponent is disabled to protect authored layout.");
                        Destroy(cardObject);
                        continue;
                    }
                }

                view.transform.SetSiblingIndex(slot);
                view.ConfigureLayout(cellSize, spacing, gridWidth, gridHeight, layoutOfferCount);
                _slotViews[slot] = view;

                if (!_lockHandlersAttached[slot])
                {
                    view.LockToggled += OnLockToggled;
                    _lockHandlersAttached[slot] = true;
                }
            }
        }

        private static void PurgeLegacyOfferChildren(Transform gridRoot)
        {
            for (int i = gridRoot.childCount - 1; i >= 0; i--)
            {
                var child = gridRoot.GetChild(i);
                if (!child.name.StartsWith(SlotNamePrefix))
                    Destroy(child.gameObject);
            }
        }
    }
}
