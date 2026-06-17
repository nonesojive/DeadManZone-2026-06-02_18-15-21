using DeadManZone.Presentation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>One army health bar with threshold notches placed in-scene.</summary>
    public sealed class ArmyHealthBarView : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Slider fillSlider;
        [SerializeField] private RectTransform fillRect;

        public float DisplayedFraction
        {
            get
            {
                if (fillRect != null)
                    return fillRect.anchorMax.x;

                if (fillSlider != null)
                    return fillSlider.value;

                return fillImage != null ? fillImage.fillAmount : 0f;
            }
        }

        public void BindFillImage(Image image)
        {
            fillImage = image;
            fillRect = null;
            EnsureFillReady();
        }

        public void BindSlider(Slider slider)
        {
            fillSlider = slider;
            if (fillSlider == null)
                return;

            fillSlider.interactable = false;
            fillSlider.transition = Selectable.Transition.None;
            fillSlider.enabled = false;

            var handle = fillSlider.handleRect;
            if (handle != null)
                handle.gameObject.SetActive(false);

            fillRect = fillSlider.fillRect;
            fillImage = fillRect != null ? fillRect.GetComponent<Image>() : fillImage;
            EnsureFillReady();
        }

        public void InitializeForTests(Image testFillImage)
        {
            fillImage = testFillImage;
            fillSlider = null;
            fillRect = null;
            EnsureFillReady();
        }

        public void ConfigureSyntyBar(Color fillTint)
        {
            ResolveFillControls();
            EnsureFillReady();

            if (fillImage != null)
                fillImage.color = fillTint;

            SetFractionImmediate(1f);
        }

        private void Awake()
        {
            ResolveFillControls();
            EnsureFillReady();
        }

        public void SetFractionImmediate(float fraction)
        {
            float clamped = Mathf.Clamp01(fraction);

            if (fillRect != null)
            {
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(clamped, 1f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                return;
            }

            if (fillSlider != null)
            {
                fillSlider.SetValueWithoutNotify(clamped);
                return;
            }

            if (fillImage != null)
                fillImage.fillAmount = clamped;
        }

        /// <summary>Updates the visible fill immediately so bars track replay ticks.</summary>
        public void SetFraction(float fraction) => SetFractionImmediate(fraction);

        private void ResolveFillControls()
        {
            if (fillSlider == null)
                fillSlider = GetComponentInChildren<Slider>(true);

            if (fillSlider != null)
            {
                BindSlider(fillSlider);
                return;
            }

            ResolveFillImage();
        }

        private void ResolveFillImage()
        {
            if (fillImage != null)
                return;

            var fillTransform = transform.Find("FillRegion")
                ?? transform.Find("Fill")
                ?? transform.Find("SliderBox/Slider/Fill Area/Fill");

            if (fillTransform != null)
            {
                fillRect = fillTransform as RectTransform;
                fillImage = fillTransform.GetComponent<Image>();
            }
        }

        private void EnsureFillReady()
        {
            if (fillImage == null)
                return;

            if (fillRect != null || fillSlider != null)
                return;

            if (fillImage.type != Image.Type.Filled)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            }

            if (fillImage.sprite == null)
            {
                var barBackground = GetComponent<Image>();
                if (barBackground != null && barBackground.sprite != null)
                    fillImage.sprite = barBackground.sprite;
                else
                    fillImage.sprite = UiWhiteSprite.Get();
            }
        }
    }
}
