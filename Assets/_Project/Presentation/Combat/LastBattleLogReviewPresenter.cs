using DeadManZone.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat
{
    /// <summary>DEV-ONLY: review overlay for the previous fight's combat log. Remove before public release.</summary>
    public sealed class LastBattleLogReviewPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private TMP_Text logText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            Close();
        }

        public void Open()
        {
            if (overlayRoot == null)
                return;

            overlayRoot.SetActive(true);
            if (logText != null)
            {
                string log = RunManager.Instance?.State?.LastCombatLogText;
                logText.overflowMode = TextOverflowModes.Overflow;
                logText.text = string.IsNullOrWhiteSpace(log)
                    ? "No combat log saved yet.\n\nFinish a fight to capture the log here."
                    : log;
            }

            RefreshScrollExtents();
        }

        private void RefreshScrollExtents()
        {
            if (logText == null || scrollRect == null)
                return;

            RectTransform content = scrollRect.content;
            RectTransform viewport = scrollRect.viewport;
            if (content == null || viewport == null)
                return;

            Canvas.ForceUpdateCanvases();

            const float horizontalPadding = 24f;
            const float verticalPadding = 16f;
            float viewportWidth = viewport.rect.width;
            float viewportHeight = viewport.rect.height;

            var logRect = logText.rectTransform;
            logRect.anchorMin = new Vector2(0f, 1f);
            logRect.anchorMax = new Vector2(1f, 1f);
            logRect.pivot = new Vector2(0.5f, 1f);
            logRect.anchoredPosition = Vector2.zero;
            logRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0f, viewportWidth - horizontalPadding));

            logText.ForceMeshUpdate();
            float textHeight = logText.preferredHeight + verticalPadding;
            float contentHeight = Mathf.Max(textHeight, viewportHeight);

            content.sizeDelta = new Vector2(0f, contentHeight);
            logRect.offsetMin = new Vector2(horizontalPadding * 0.5f, -textHeight);
            logRect.offsetMax = new Vector2(-horizontalPadding * 0.5f, 0f);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scrollRect.verticalNormalizedPosition = 1f;
        }

        public void Close()
        {
            if (overlayRoot != null)
                overlayRoot.SetActive(false);
        }
    }
}
