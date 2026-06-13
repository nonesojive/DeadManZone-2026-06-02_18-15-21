using DeadManZone.Presentation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>One army health bar with threshold notches placed in-scene.</summary>
    public sealed class ArmyHealthBarView : MonoBehaviour
    {
        [SerializeField] private Image fillImage;

        public float DisplayedFraction => fillImage != null ? fillImage.fillAmount : 0f;

        public void BindFillImage(Image image)
        {
            fillImage = image;
            EnsureFillReady();
        }

        public void InitializeForTests(Image testFillImage)
        {
            fillImage = testFillImage;
            EnsureFillReady();
        }

        private void Awake()
        {
            ResolveFillImage();
            EnsureFillReady();
        }

        public void SetFractionImmediate(float fraction)
        {
            if (fillImage == null)
                return;

            fillImage.fillAmount = Mathf.Clamp01(fraction);
        }

        /// <summary>Updates the visible fill immediately so bars track replay ticks.</summary>
        public void SetFraction(float fraction) => SetFractionImmediate(fraction);

        private void ResolveFillImage()
        {
            if (fillImage != null)
                return;

            var fillTransform = transform.Find("FillRegion") ?? transform.Find("Fill");
            if (fillTransform != null)
                fillImage = fillTransform.GetComponent<Image>();
        }

        private void EnsureFillReady()
        {
            if (fillImage == null)
                return;

            if (fillImage.sprite == null)
            {
                var barBackground = GetComponent<Image>();
                if (barBackground != null && barBackground.sprite != null)
                    fillImage.sprite = barBackground.sprite;
                else
                    fillImage.sprite = UiWhiteSprite.Get();
            }

            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }
}
