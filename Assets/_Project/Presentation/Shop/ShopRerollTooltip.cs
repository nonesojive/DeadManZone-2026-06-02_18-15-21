using DeadManZone.Core.Run;
using DeadManZone.Game;
using DeadManZone.Presentation.Run;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeadManZone.Presentation.Shop
{
    /// <summary>
    /// Shows unified shop reroll cost in the build messages panel while hovering the reroll button.
    /// </summary>
    public sealed class ShopRerollTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static int _hoverCount;

        [SerializeField] private BuildMessagesView messagesView;

        private bool _hovering;

        public static bool AnyHovered => _hoverCount > 0;

        public void Configure(BuildMessagesView messages) => messagesView = messages;

        private void OnEnable()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.RunStateChanged += OnRunStateChanged;
        }

        private void OnDisable()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.RunStateChanged -= OnRunStateChanged;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ResolveMessagesView();
            if (messagesView == null)
                return;

            if (!_hovering)
                _hoverCount++;

            _hovering = true;
            RefreshCostText();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (! _hovering)
                return;

            _hovering = false;
            _hoverCount = Mathf.Max(0, _hoverCount - 1);
            messagesView?.ClearRerollHover();
        }

        private void OnRunStateChanged(RunState state)
        {
            if (_hovering)
                RefreshCostText();
        }

        private void RefreshCostText()
        {
            if (messagesView == null || !_hovering)
                return;

            int supplyCost = RunOrchestrator.BaseRerollCost;
            if (RunManager.Instance is { HasActiveRun: true })
                supplyCost += RunManager.Instance.State.RerollCountThisRound;

            int lockCost = 0;
            if (RunManager.Instance is { HasActiveRun: true, Orchestrator: not null })
                lockCost = RunManager.Instance.Orchestrator.ComputeRerollLockAuthorityCost();

            string text = lockCost > 0
                ? $"Reroll: {supplyCost} supplies + {lockCost} authority (locks)"
                : $"Reroll: {supplyCost} supplies";

            messagesView.SetRerollHoverMessage(text);
        }

        private void ResolveMessagesView()
        {
            if (messagesView != null)
                return;

            messagesView = FindFirstObjectByType<BuildMessagesView>();
        }
    }
}
