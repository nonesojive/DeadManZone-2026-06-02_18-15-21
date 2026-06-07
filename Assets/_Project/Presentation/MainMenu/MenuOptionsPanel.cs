using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.MainMenu
{
    /// <summary>Minimal main-menu options: music, SFX, and fullscreen.</summary>
    public sealed class MenuOptionsPanel : MonoBehaviour
    {
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Button fullscreenButton;
        [SerializeField] private TMP_Text fullscreenLabel;

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

            RefreshFullscreenLabel();
        }

        private void OnEnable() => RefreshFullscreenLabel();

        private void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            RefreshFullscreenLabel();
        }

        private void RefreshFullscreenLabel()
        {
            if (fullscreenLabel != null)
                fullscreenLabel.text = Screen.fullScreen ? "Fullscreen: On" : "Fullscreen: Off";
        }
    }
}
