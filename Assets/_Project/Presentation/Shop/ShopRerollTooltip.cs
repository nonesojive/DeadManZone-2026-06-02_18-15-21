using DeadManZone.Core.Run;
using DeadManZone.Game;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeadManZone.Presentation.Shop
{
    /// <summary>
    /// Shows reroll supply cost in the shared top-bar tooltip while hovering a lane reroll button.
    /// </summary>
    public sealed class ShopRerollTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static int _hoverCount;

        [SerializeField] private TMP_Text tooltipText;

        private string _savedTooltip;
        private bool _hovering;

        public static bool AnyHovered => _hoverCount > 0;

        public void Configure(TMP_Text tooltip) => tooltipText = tooltip ?? ResolveSharedTooltip();

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
            if (tooltipText == null)
                tooltipText = ResolveSharedTooltip();

            if (tooltipText == null)
                return;

            if (!_hovering)
            {
                _hoverCount++;
                _savedTooltip = tooltipText.text;
            }

            _hovering = true;
            RefreshCostText();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltipText == null || !_hovering)
                return;

            _hovering = false;
            _hoverCount = Mathf.Max(0, _hoverCount - 1);
            tooltipText.text = _savedTooltip ?? string.Empty;
        }

        private void OnRunStateChanged(RunState state)
        {
            if (_hovering)
                RefreshCostText();
        }

        private void RefreshCostText()
        {
            if (tooltipText == null || !_hovering)
                return;

            int cost = RunOrchestrator.BaseRerollCost;
            if (RunManager.Instance is { HasActiveRun: true })
                cost += RunManager.Instance.State.RerollCountThisRound;

            tooltipText.text = $"Reroll lane: {cost} supplies";
        }

        private static TMP_Text ResolveSharedTooltip()
        {
            foreach (var marker in FindObjectsByType<ShopSharedTooltip>(FindObjectsSortMode.None))
            {
                var label = marker.GetComponent<TMP_Text>();
                if (label != null)
                    return label;
            }

            return null;
        }
    }
}
