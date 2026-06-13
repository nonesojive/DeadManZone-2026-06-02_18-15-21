using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Lets the 3D arena show through the run canvas and restores UI when combat ends.
    /// </summary>
    public static class CombatArenaUiController
    {
        private static Image _canvasBackdrop;
        private static Color _savedBackdropColor;
        private static Camera _runMainCamera;
        private static bool _runCameraWasEnabled;

        public static void EnterArenaMode(Transform buildPanelRoot)
        {
            CacheRunCamera();
            SetCanvasBackdropTransparent(buildPanelRoot);
            if (_runMainCamera != null)
            {
                _runCameraWasEnabled = _runMainCamera.enabled;
                _runMainCamera.enabled = false;
            }
        }

        public static void ExitArenaMode(Transform buildPanelRoot)
        {
            RestoreCanvasBackdrop(buildPanelRoot);
            if (_runMainCamera != null)
                _runMainCamera.enabled = _runCameraWasEnabled;

            _canvasBackdrop = null;
        }

        private static void CacheRunCamera()
        {
            if (_runMainCamera != null)
                return;

            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var camera in cameras)
            {
                if (camera.gameObject.scene.name != "Run")
                    continue;

                if (camera.CompareTag("MainCamera"))
                {
                    _runMainCamera = camera;
                    return;
                }
            }
        }

        private static void SetCanvasBackdropTransparent(Transform buildPanelRoot)
        {
            var backdrop = ResolveCanvasBackdrop(buildPanelRoot);
            if (backdrop == null)
                return;

            _savedBackdropColor = backdrop.color;
            var transparent = _savedBackdropColor;
            transparent.a = 0f;
            backdrop.color = transparent;
        }

        private static void RestoreCanvasBackdrop(Transform buildPanelRoot)
        {
            var backdrop = ResolveCanvasBackdrop(buildPanelRoot);
            if (backdrop == null)
                return;

            backdrop.color = _savedBackdropColor;
        }

        private static Image ResolveCanvasBackdrop(Transform buildPanelRoot)
        {
            if (_canvasBackdrop != null)
                return _canvasBackdrop;

            if (buildPanelRoot == null)
                return null;

            var canvas = buildPanelRoot.GetComponentInParent<Canvas>();
            if (canvas == null)
                return null;

            _canvasBackdrop = canvas.GetComponent<Image>();
            return _canvasBackdrop;
        }
    }
}
