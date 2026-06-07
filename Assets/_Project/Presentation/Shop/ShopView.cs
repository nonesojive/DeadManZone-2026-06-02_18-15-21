using System.Collections;
using System.Linq;
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
        [Header("Lane Roots")]
        [SerializeField] private Transform generalLaneRoot;
        [SerializeField] private Transform engineersLaneRoot;
        [SerializeField] private Transform requisitionLaneRoot;

        [Header("Lane Controls")]
        [SerializeField] private Button rerollGeneralButton;
        [SerializeField] private Button rerollEngineersButton;
        [SerializeField] private Button rerollRequisitionButton;

        [Header("Cards")]
        [SerializeField] private GameObject offerCardPrefab;
        [SerializeField] private TMP_Text modifiersTooltipText;
        [SerializeField] private BoardView boardView;

        private Coroutine _deferredRefresh;

        public TMP_Text ModifiersTooltip => modifiersTooltipText;

        private void Awake()
        {
            if (rerollGeneralButton != null)
                rerollGeneralButton.onClick.AddListener(() => OnRerollClicked(ShopLane.Offensive));
            if (rerollEngineersButton != null)
                rerollEngineersButton.onClick.AddListener(() => OnRerollClicked(ShopLane.Defensive));
            if (rerollRequisitionButton != null)
                rerollRequisitionButton.onClick.AddListener(() => OnRerollClicked(ShopLane.Specialty));

            ConfigureOffersLane(generalLaneRoot);
            ConfigureOffersLane(engineersLaneRoot);
            ConfigureOffersLane(requisitionLaneRoot);
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

            RebuildLane(generalLaneRoot, state, ShopLane.Offensive, cellSize, spacing);
            RebuildLane(engineersLaneRoot, state, ShopLane.Defensive, cellSize, spacing);
            RebuildLane(requisitionLaneRoot, state, ShopLane.Specialty, cellSize, spacing);
            UpdateModifierTooltip(state.Modifiers, nextEnemyTag);
        }

        public void InitializeForTests(
            Transform generalRoot,
            Transform engineersRoot,
            Transform requisitionRoot,
            GameObject offerPrefab,
            TMP_Text tooltipText,
            BoardView board = null)
        {
            generalLaneRoot = generalRoot;
            engineersLaneRoot = engineersRoot;
            requisitionLaneRoot = requisitionRoot;
            this.offerCardPrefab = offerPrefab;
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

        private void RebuildLane(
            Transform laneRoot,
            ShopState state,
            ShopLane lane,
            float cellSize,
            float spacing)
        {
            if (laneRoot == null || offerCardPrefab == null)
                return;

            ConfigureOffersLane(laneRoot);

            var (laneWidth, laneHeight) = laneRoot is RectTransform laneRect
                ? ShopLayoutMetrics.ResolveOffersMetrics(laneRect)
                : (400f, 100f);

            ClearChildren(laneRoot);
            var offers = state.Offers.Where(o => o.Lane == lane).OrderBy(o => o.SlotIndex);
            foreach (var offer in offers)
            {
                var cardObject = Instantiate(offerCardPrefab, laneRoot);
                cardObject.SetActive(true);
                var card = cardObject.GetComponent<ShopOfferView>() ?? cardObject.AddComponent<ShopOfferView>();
                bool isLocked = RunManager.Instance is { HasActiveRun: true } manager &&
                    manager.Orchestrator.IsOfferLocked(offer);
                card.Bind(offer, isLocked, cellSize, spacing, laneWidth, laneHeight);
                card.LockToggled += OnLockToggled;
            }

            if (laneRoot is RectTransform offersRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(offersRect);
        }

        private static void ConfigureOffersLane(Transform laneRoot)
        {
            if (laneRoot is not RectTransform offers)
                return;

            offers.anchorMin = new Vector2(0.04f, 0.14f);
            offers.anchorMax = new Vector2(0.90f, 0.86f);
            offers.offsetMin = Vector2.zero;
            offers.offsetMax = Vector2.zero;

            var layoutGroup = offers.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
                layoutGroup = offers.gameObject.AddComponent<HorizontalLayoutGroup>();

            layoutGroup.spacing = ShopLayoutMetrics.LaneSpacing;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.padding = new RectOffset(4, 4, 2, 2);
        }

        private void OnLockToggled(ShopOffer offer, bool shouldLock)
        {
            if (RunManager.Instance == null)
                return;

            RunManager.Instance.SetLockedOffer(offer, shouldLock);
            RefreshFromRunManager();
        }

        private void OnRerollClicked(ShopLane lane)
        {
            if (RunManager.Instance == null)
                return;

            RunManager.Instance.TryRerollLane(lane);
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
                sb.AppendLine($"+{modifiers.ExtraGeneralSlots} extra general slot");
            if (modifiers.GuaranteeEngineerOffer)
                sb.AppendLine("Guaranteed engineer offer");
            if (modifiers.EnemyTagPreview)
                sb.AppendLine($"Enemy preview: {nextEnemyTag ?? "Unknown"}");

            modifiersTooltipText.text = sb.Length == 0
                ? "Drag pieces to reserves or board. Drop on Sell to refund."
                : sb.ToString().TrimEnd();
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }
    }
}
