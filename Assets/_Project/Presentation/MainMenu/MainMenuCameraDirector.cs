using System.Collections;
using DeadManZone.Game;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeadManZone.Presentation.MainMenu
{
    /// <summary>Drives SlimUI menu camera motion and scene loading. Lives on MenuCamera.</summary>
    public sealed class MainMenuCameraDirector : MonoBehaviour
    {
        private static readonly int AnimateHash = Animator.StringToHash("Animate");

        [SerializeField] private Animator menuCameraAnimator;
        [SerializeField] private GameObject menuCanvasRoot;
        [SerializeField] private GameObject loadingOverlay;
        [SerializeField] private Slider loadingBar;
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private AudioSource transitionSound;

        private void Awake()
        {
            if (menuCameraAnimator == null)
                menuCameraAnimator = GetComponent<Animator>();
        }

        public void FocusMain()
        {
            SetCameraFocus(mainView: true);
        }

        public void FocusSubPanel()
        {
            SetCameraFocus(mainView: false);
        }

        public void LoadRunScene()
        {
            StartCoroutine(LoadRunSceneRoutine());
        }

        private IEnumerator LoadRunSceneRoutine()
        {
            if (loadingOverlay != null)
                loadingOverlay.SetActive(true);

            if (menuCanvasRoot != null)
                menuCanvasRoot.SetActive(false);

            yield return null;

            AsyncOperation operation = SceneManager.LoadSceneAsync(GameScenes.Run);
            if (operation == null)
            {
                SceneManager.LoadScene(GameScenes.Run);
                yield break;
            }

            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                if (loadingBar != null)
                    loadingBar.value = Mathf.Clamp01(operation.progress / 0.9f);
                if (loadingText != null)
                    loadingText.text = "Deploying…";
                yield return null;
            }

            if (loadingBar != null)
                loadingBar.value = 1f;
            operation.allowSceneActivation = true;
        }

        private void SetCameraFocus(bool mainView)
        {
            if (menuCameraAnimator != null)
                menuCameraAnimator.SetFloat(AnimateHash, mainView ? 0f : 1f);

            if (!mainView && transitionSound != null)
                transitionSound.Play();
        }
    }
}
