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

            ApplyGrimdarkSkin();
            Close();
        }

        /// <summary>M6: grimdark kit over the scene-authored review sheet. The dim
        /// backdrop image on the root is left alone (it is an overlay scrim, not chrome).</summary>
        private void ApplyGrimdarkSkin()
        {
            if (overlayRoot != null)
            {
                var sheet = overlayRoot.transform.Find("LastBattleLogSheet");
                if (sheet != null)
                {
                    CombatGrimdarkSkin.StyleFrame(sheet.GetComponent<Image>());
                    var scrollImage = sheet.Find("LogScroll")?.GetComponent<Image>();
                    if (scrollImage != null)
                    {
                        // Darker band inside the card so the log region stays legible.
                        scrollImage.sprite = null;
                        scrollImage.color = CombatGrimdarkSkin.BandDark;
                    }
                    CombatGrimdarkSkin.StylePanelText(sheet.gameObject, titleFontSize: 20f);
                }
            }

            CombatGrimdarkSkin.StyleBody(logText);
            CombatGrimdarkSkin.StyleButton(closeButton);
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
                    ? "No battle report saved yet.\n\nFinish a fight to capture it here."
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
