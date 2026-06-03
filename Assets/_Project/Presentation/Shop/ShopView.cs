using System.Linq;
using System.Text;
using DeadManZone.Core.Shop;
using DeadManZone.Game;
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

        [Header("Summary")]
        [SerializeField] private TMP_Text currenciesText;

        private void Awake()
        {
            if (rerollGeneralButton != null)
                rerollGeneralButton.onClick.AddListener(() => OnRerollClicked(ShopLane.General));
            if (rerollEngineersButton != null)
                rerollEngineersButton.onClick.AddListener(() => OnRerollClicked(ShopLane.Engineers));
            if (rerollRequisitionButton != null)
                rerollRequisitionButton.onClick.AddListener(() => OnRerollClicked(ShopLane.Requisition));
        }

        private void OnEnable()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.RunStateChanged += OnRunStateChanged;
            RefreshFromRunManager();
        }

        private void OnDisable()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.RunStateChanged -= OnRunStateChanged;
        }

        public void Render(ShopState state, string nextEnemyTag)
        {
            if (state == null)
                return;

            RebuildLane(generalLaneRoot, state, ShopLane.General);
            RebuildLane(engineersLaneRoot, state, ShopLane.Engineers);
            RebuildLane(requisitionLaneRoot, state, ShopLane.Requisition);
            UpdateModifierTooltip(state.Modifiers, nextEnemyTag);
            UpdateCurrencyText();
        }

        public void InitializeForTests(
            Transform generalRoot,
            Transform engineersRoot,
            Transform requisitionRoot,
            GameObject offerPrefab,
            TMP_Text tooltipText)
        {
            generalLaneRoot = generalRoot;
            engineersLaneRoot = engineersRoot;
            requisitionLaneRoot = requisitionRoot;
            this.offerCardPrefab = offerPrefab;
            modifiersTooltipText = tooltipText;
        }

        private void OnRunStateChanged(Core.Run.RunState _) => RefreshFromRunManager();

        public void RefreshFromRunManager()
        {
            if (RunManager.Instance == null || !RunManager.Instance.HasActiveRun)
                return;

            var state = RunManager.Instance.State;
            Render(state.Shop, RunManager.Instance.Orchestrator.GetNextEnemyPreviewTag());
        }

        private void RebuildLane(Transform laneRoot, ShopState state, ShopLane lane)
        {
            if (laneRoot == null || offerCardPrefab == null)
                return;

            ClearChildren(laneRoot);
            var offers = state.Offers.Where(o => o.Lane == lane);
            foreach (var offer in offers)
            {
                var cardObject = Instantiate(offerCardPrefab, laneRoot);
                cardObject.SetActive(true);
                ConfigureOfferCardLayout(cardObject);
                var card = cardObject.GetComponent<ShopOfferView>() ?? cardObject.AddComponent<ShopOfferView>();
                bool isLocked = RunManager.Instance != null &&
                    RunManager.Instance.Orchestrator.IsOfferLocked(offer);
                card.Bind(offer, isLocked);
                card.LockToggled += OnLockToggled;
            }
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

        private void UpdateCurrencyText()
        {
            if (currenciesText == null || RunManager.Instance?.State == null)
                return;

            var state = RunManager.Instance.State;
            int rerollCost = RunOrchestrator.BaseRerollCost + state.RerollCountThisRound;
            currenciesText.text =
                $"Supplies: {state.Supplies}   Manpower: {state.Manpower}   " +
                $"Authority: {state.Authority}   Morale: {state.Morale}   Reroll: {rerollCost}S";
        }

        private void UpdateModifierTooltip(ShopModifiers modifiers, string nextEnemyTag)
        {
            if (modifiersTooltipText == null || modifiers == null)
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
                ? "Drag pieces to bench or board. Drop on Sell to refund."
                : sb.ToString().TrimEnd();
        }

        private static void ConfigureOfferCardLayout(GameObject cardObject)
        {
            var rect = cardObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(200f, 110f);
            }

            var layout = cardObject.GetComponent<LayoutElement>();
            if (layout == null)
                layout = cardObject.AddComponent<LayoutElement>();
            layout.minWidth = 200f;
            layout.minHeight = 110f;
            layout.preferredWidth = 200f;
            layout.preferredHeight = 110f;
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }
    }
}
