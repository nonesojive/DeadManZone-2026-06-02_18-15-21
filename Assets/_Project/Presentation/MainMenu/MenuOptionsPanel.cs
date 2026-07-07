using DeadManZone.Presentation.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.MainMenu
{
    /// <summary>Minimal main-menu options: music, SFX, fullscreen, and post-processing.</summary>
    public sealed class MenuOptionsPanel : MonoBehaviour
    {
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Button fullscreenButton;
        [SerializeField] private TMP_Text fullscreenLabel;
        [SerializeField] private Button postFxButton;
        [SerializeField] private TMP_Text postFxLabel;

        private void Awake()
        {
            if (musicSlider != null)
            {
                musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
                musicSlider.onValueChanged.AddListener(value =>
                {
                    PlayerPrefs.SetFloat("MusicVolume", value);
                    PlayerPrefs.Save();
                });
            }

            if (sfxSlider != null)
            {
                sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
                sfxSlider.onValueChanged.AddListener(value =>
                {
                    PlayerPrefs.SetFloat("SFXVolume", value);
                    PlayerPrefs.Save();
                });
            }

            if (fullscreenButton != null)
                fullscreenButton.onClick.AddListener(ToggleFullscreen);

            EnsurePostFxToggle();
            if (postFxButton != null)
                postFxButton.onClick.AddListener(TogglePostFx);

            RefreshLabels();
        }

        private void OnEnable() => RefreshLabels();

        private void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            RefreshLabels();
        }

        private void TogglePostFx()
        {
            GraphicsSettings.PostProcessingEnabled = !GraphicsSettings.PostProcessingEnabled;
            RefreshLabels();
        }

        /// <summary>Auto-provision the post-processing toggle by cloning the fullscreen button,
        /// so the option appears without editing the menu scene. The panel positions rows by
        /// anchor (not layout group), so slot the new button into the Back button's row and
        /// push Back down one row to keep it last.</summary>
        private void EnsurePostFxToggle()
        {
            if (postFxButton != null || fullscreenButton == null)
                return;

            var parent = fullscreenButton.transform.parent;
            var fullscreenRect = (RectTransform)fullscreenButton.transform;

            RectTransform backRect = null;
            foreach (Transform child in parent)
            {
                if (child.name.Contains("Back"))
                {
                    backRect = (RectTransform)child;
                    break;
                }
            }

            postFxButton = Instantiate(fullscreenButton, parent);
            postFxButton.name = "PostFxButton";
            postFxButton.onClick.RemoveAllListeners();
            postFxLabel = postFxButton.GetComponentInChildren<TMP_Text>();
            var postFxRect = (RectTransform)postFxButton.transform;

            if (backRect != null)
            {
                float rowSpacing = fullscreenRect.anchorMin.y - backRect.anchorMin.y;
                postFxRect.anchorMin = backRect.anchorMin;
                postFxRect.anchorMax = backRect.anchorMax;
                postFxRect.anchoredPosition = backRect.anchoredPosition;
                backRect.anchorMin -= new Vector2(0f, rowSpacing);
                backRect.anchorMax -= new Vector2(0f, rowSpacing);
            }
        }

        private void RefreshLabels()
        {
            if (fullscreenLabel != null)
                fullscreenLabel.text = Screen.fullScreen ? "Fullscreen: On" : "Fullscreen: Off";
            if (postFxLabel != null)
                postFxLabel.text = GraphicsSettings.PostProcessingEnabled
                    ? "Post-FX: On"
                    : "Post-FX: Off";
        }
    }
}
