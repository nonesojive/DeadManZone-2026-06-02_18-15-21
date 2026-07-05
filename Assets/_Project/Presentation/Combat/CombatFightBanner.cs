using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat
{
    /// <summary>"FIGHT N" ceremony banner shown as the arena is revealed: dark band
    /// sweeps in, title punches, everything fades before the doctrine gate needs focus.
    /// Self-contained overlay canvas; never blocks raycasts; destroys itself.</summary>
    public static class CombatFightBanner
    {
        private const float PunchSeconds = 0.18f;
        private const float HoldSeconds = 0.85f;
        private const float FadeSeconds = 0.35f;

        /// <summary>Full banner lifetime; the flow presenter holds the tactics gate
        /// until the ceremony finishes (field → banner → orders).</summary>
        public const float TotalSeconds = PunchSeconds + HoldSeconds + FadeSeconds;

        public static void Show(MonoBehaviour host, int fightIndex)
        {
            if (host == null)
                return;

            var go = new GameObject("CombatFightBanner", typeof(Canvas), typeof(CanvasGroup));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 240; // over the arena, under modal panels (report/pause)

            var group = go.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var band = new GameObject("Band", typeof(RectTransform), typeof(Image));
            band.transform.SetParent(go.transform, false);
            var bandRect = band.GetComponent<RectTransform>();
            bandRect.anchorMin = new Vector2(0f, 0.60f);
            bandRect.anchorMax = new Vector2(1f, 0.72f);
            bandRect.offsetMin = Vector2.zero;
            bandRect.offsetMax = Vector2.zero;
            var bandImage = band.GetComponent<Image>();
            bandImage.color = new Color(0.06f, 0.05f, 0.04f, 0.82f);
            bandImage.raycastTarget = false;

            var textGo = new GameObject("Title", typeof(RectTransform));
            textGo.transform.SetParent(band.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = $"FIGHT  {Mathf.Max(1, fightIndex)}";
            text.fontSize = 64f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.characterSpacing = 14f;
            text.color = new Color(0.92f, 0.87f, 0.74f, 1f);
            text.outlineColor = new Color32(20, 14, 8, 230);
            text.outlineWidth = 0.22f;
            text.raycastTarget = false;

            host.StartCoroutine(Animate(go, group, band.transform));
        }

        private static IEnumerator Animate(GameObject root, CanvasGroup group, Transform band)
        {
            // Punch in: band stretches from a horizontal sliver, title scales down onto it.
            for (float t = 0f; t < PunchSeconds; t += Time.deltaTime)
            {
                float p = Mathf.Clamp01(t / PunchSeconds);
                float ease = 1f - (1f - p) * (1f - p);
                band.localScale = new Vector3(1f, ease, 1f);
                group.alpha = ease;
                yield return null;
            }

            band.localScale = Vector3.one;
            group.alpha = 1f;
            yield return new WaitForSeconds(HoldSeconds);

            for (float t = 0f; t < FadeSeconds; t += Time.deltaTime)
            {
                group.alpha = 1f - Mathf.Clamp01(t / FadeSeconds);
                yield return null;
            }

            Object.Destroy(root);
        }
    }
}
