using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Makes the run canvas transparent during combat so the additive arena camera shows through.
    /// Uses Screen Space Overlay (not Screen Space Camera) to avoid viewport clipping artifacts.
    /// </summary>
    public static class CombatArenaUiController
    {
        private static Image _canvasBackdrop;
        private static Color _savedBackdropColor;
        private static bool _savedBackdropRaycast;
        private static Image _buildPanelBackdrop;
        private static Color _savedBuildPanelBackdropColor;
        private static GameObject _buildPanelRoot;
        private static bool _buildPanelWasActive;
        private static readonly List<(Camera camera, bool enabled)> _disabledRunCameras = new();
        private static AudioListener _runAudioListener;
        private static AudioListener _arenaAudioListener;
        private static CanvasGroup _buildPanelCanvasGroup;
        private static float _savedBuildPanelAlpha;
        private static bool _savedBuildPanelBlocksRaycasts;
        private static GameObject _decorBackground;
        private static bool _decorBackgroundWasActive;
        private static readonly List<(Graphic graphic, Color color, bool raycast)> _hiddenGraphics = new();

        public static void EnterArenaMode(Transform buildPanelRoot, Camera arenaCamera)
        {
            CacheRunCameras();
            CacheCanvasBackdrop(buildPanelRoot);
            SetCanvasBackdropTransparent();
            HideBuildPanelChrome(buildPanelRoot);
            DisableRunCameras();
            HideCombatChromeOverlays();
            MoveAudioListenerToArenaCamera(arenaCamera);
        }

        public static void ExitArenaMode(Transform buildPanelRoot)
        {
            RestoreHiddenGraphics();
            RestoreAudioListener();
            RestoreBuildPanelChrome(buildPanelRoot);
            RestoreCanvasBackdrop();
            RestoreRunCameras();

            _canvasBackdrop = null;
            _buildPanelBackdrop = null;
            _buildPanelRoot = null;
            _buildPanelCanvasGroup = null;
            _decorBackground = null;
            _disabledRunCameras.Clear();
            _runAudioListener = null;
            _arenaAudioListener = null;
        }

        private static void CacheRunCameras()
        {
            if (_disabledRunCameras.Count > 0)
                return;

            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (camera == null || camera.gameObject.scene.name != "Run")
                    continue;

                _disabledRunCameras.Add((camera, camera.enabled));

                if (_runAudioListener == null)
                    _runAudioListener = camera.GetComponent<AudioListener>();
            }
        }

        private static void CacheCanvasBackdrop(Transform buildPanelRoot)
        {
            if (_canvasBackdrop != null || buildPanelRoot == null)
                return;

            var canvas = buildPanelRoot.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _canvasBackdrop = canvas.GetComponent<Image>();

                var decor = canvas.transform.Find("DecorBackground");
                if (decor != null)
                    _decorBackground = decor.gameObject;
            }

            _buildPanelBackdrop = buildPanelRoot.GetComponent<Image>();
        }

        private static void SetCanvasBackdropTransparent()
        {
            if (_canvasBackdrop != null)
            {
                _savedBackdropColor = _canvasBackdrop.color;
                _savedBackdropRaycast = _canvasBackdrop.raycastTarget;
                var transparent = _savedBackdropColor;
                transparent.a = 0f;
                _canvasBackdrop.color = transparent;
                _canvasBackdrop.raycastTarget = false;
            }

            if (_buildPanelBackdrop != null)
            {
                _savedBuildPanelBackdropColor = _buildPanelBackdrop.color;
                var panelTransparent = _savedBuildPanelBackdropColor;
                panelTransparent.a = 0f;
                _buildPanelBackdrop.color = panelTransparent;
                _buildPanelBackdrop.raycastTarget = false;
            }

            if (_decorBackground != null)
            {
                _decorBackgroundWasActive = _decorBackground.activeSelf;
                _decorBackground.SetActive(false);
            }
        }

        private static void RestoreCanvasBackdrop()
        {
            if (_canvasBackdrop != null)
            {
                _canvasBackdrop.color = _savedBackdropColor;
                _canvasBackdrop.raycastTarget = _savedBackdropRaycast;
            }

            if (_buildPanelBackdrop != null)
            {
                _buildPanelBackdrop.color = _savedBuildPanelBackdropColor;
                _buildPanelBackdrop.raycastTarget = true;
            }

            if (_decorBackground != null)
                _decorBackground.SetActive(_decorBackgroundWasActive);
        }

        private static void DisableRunCameras()
        {
            for (int i = 0; i < _disabledRunCameras.Count; i++)
            {
                var entry = _disabledRunCameras[i];
                if (entry.camera != null)
                    entry.camera.enabled = false;
            }
        }

        private static void RestoreRunCameras()
        {
            for (int i = 0; i < _disabledRunCameras.Count; i++)
            {
                var entry = _disabledRunCameras[i];
                if (entry.camera != null)
                    entry.camera.enabled = entry.enabled;
            }
        }

        private static void MoveAudioListenerToArenaCamera(Camera arenaCamera)
        {
            if (_runAudioListener != null)
                _runAudioListener.enabled = false;

            if (arenaCamera == null)
                return;

            _arenaAudioListener = arenaCamera.GetComponent<AudioListener>();
            if (_arenaAudioListener == null)
                _arenaAudioListener = arenaCamera.gameObject.AddComponent<AudioListener>();
            else
                _arenaAudioListener.enabled = true;
        }

        private static void RestoreAudioListener()
        {
            if (_arenaAudioListener != null)
            {
                if (_runAudioListener != null && _runAudioListener.gameObject != _arenaAudioListener.gameObject)
                    Object.Destroy(_arenaAudioListener);
                else
                    _arenaAudioListener.enabled = false;
            }

            if (_runAudioListener != null)
                _runAudioListener.enabled = true;
        }

        private static void HideBuildPanelChrome(Transform buildPanelRoot)
        {
            if (buildPanelRoot == null)
                return;

            _buildPanelCanvasGroup = buildPanelRoot.GetComponent<CanvasGroup>();
            if (_buildPanelCanvasGroup != null)
            {
                _savedBuildPanelAlpha = _buildPanelCanvasGroup.alpha;
                _savedBuildPanelBlocksRaycasts = _buildPanelCanvasGroup.blocksRaycasts;
                _buildPanelCanvasGroup.alpha = 0f;
                _buildPanelCanvasGroup.blocksRaycasts = false;
                _buildPanelCanvasGroup.interactable = false;
            }

            HideOpaqueFullScreenGraphics(buildPanelRoot);

            _buildPanelRoot = buildPanelRoot.gameObject;
            _buildPanelWasActive = _buildPanelRoot.activeSelf;
            _buildPanelRoot.SetActive(false);
        }

        private static void HideOpaqueFullScreenGraphics(Transform buildPanelRoot)
        {
            _hiddenGraphics.Clear();
            var buildRect = buildPanelRoot as RectTransform;
            if (buildRect == null)
                return;

            foreach (var graphic in buildPanelRoot.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic == null || graphic.color.a <= 0.01f)
                    continue;

                if (graphic is not Image image || image.sprite == null)
                    continue;

                if (!IsFullScreenGraphic(image.rectTransform, buildRect))
                    continue;

                _hiddenGraphics.Add((graphic, graphic.color, graphic.raycastTarget));
                var hidden = graphic.color;
                hidden.a = 0f;
                graphic.color = hidden;
                graphic.raycastTarget = false;
            }
        }

        private static bool IsFullScreenGraphic(RectTransform rect, RectTransform panelRect)
        {
            if (rect == panelRect)
                return true;

            var anchors = rect.anchorMax - rect.anchorMin;
            return anchors.x > 0.95f && anchors.y > 0.95f;
        }

        private static void RestoreHiddenGraphics()
        {
            for (int i = 0; i < _hiddenGraphics.Count; i++)
            {
                var entry = _hiddenGraphics[i];
                if (entry.graphic == null)
                    continue;

                entry.graphic.color = entry.color;
                entry.graphic.raycastTarget = entry.raycast;
            }

            _hiddenGraphics.Clear();
        }

        private static void HideCombatChromeOverlays()
        {
            foreach (var overlay in Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None))
            {
                if (overlay == null || overlay.gameObject.scene.name != "Run")
                    continue;

                string name = overlay.gameObject.name;
                if (name is not ("CombatLoadingOverlay" or "CombatDecor" or "DecorBackground" or "PhaseBanner"))
                    continue;

                overlay.gameObject.SetActive(false);
            }
        }

        private static void RestoreBuildPanelChrome(Transform buildPanelRoot)
        {
            if (_buildPanelCanvasGroup == null && buildPanelRoot != null)
                _buildPanelCanvasGroup = buildPanelRoot.GetComponent<CanvasGroup>();

            if (_buildPanelCanvasGroup != null)
            {
                _buildPanelCanvasGroup.alpha = _savedBuildPanelAlpha;
                _buildPanelCanvasGroup.blocksRaycasts = _savedBuildPanelBlocksRaycasts;
                _buildPanelCanvasGroup.interactable = _savedBuildPanelAlpha > 0.9f;
            }

            if (_buildPanelRoot != null)
                _buildPanelRoot.SetActive(_buildPanelWasActive);
            else if (buildPanelRoot != null)
                buildPanelRoot.gameObject.SetActive(true);
        }
    }
}
