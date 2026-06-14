#if UNITY_EDITOR
using DeadManZone.Data;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Editor play-mode tool for positioning the combat arena camera, then saving to CombatArenaConfig.
    /// </summary>
    public sealed class CombatArenaCameraTuner : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.C;
        [SerializeField] private KeyCode saveKey = KeyCode.F5;
        [SerializeField] private float orbitSensitivity = 0.25f;
        [SerializeField] private float panSpeed = 8f;
        [SerializeField] private float zoomSpeed = 2.5f;
        [SerializeField] private float fovStep = 1f;

        private bool _active;
        private CombatArenaCameraPose _pose;
        private string _statusMessage = string.Empty;
        private float _statusUntil;

        private void Update()
        {
            if (!CombatArenaSession.IsActive)
            {
                _active = false;
                return;
            }

            if (Input.GetKeyDown(toggleKey))
            {
                _active = !_active;
                if (_active)
                    SyncFromCamera();
                else
                    Cursor.lockState = CursorLockMode.None;

                SetStatus(_active ? "Camera tuning ON" : "Camera tuning OFF");
            }

            if (!_active)
                return;

            HandleSaveAndExitInput();
            HandleOrbitInput();
            HandlePanInput();
            HandleZoomInput();
            HandleFovInput();
            ApplyPose();
        }

        private void OnGUI()
        {
            if (!_active || !CombatArenaSession.IsActive)
                return;

            const int width = 430;
            const int height = 250;
            var rect = new Rect(12f, 12f, width, height);
            GUI.Box(rect, GUIContent.none);

            GUILayout.BeginArea(rect);
            GUILayout.Label("<b>Combat Camera Tuner</b>");
            GUILayout.Space(4f);
            GUILayout.Label($"[{toggleKey}] toggle   [{saveKey}] save to config   [Esc] exit");
            GUILayout.Label("RMB drag = orbit   Scroll = zoom   WASD = pan look-at   Q/E = look-at Y");
            GUILayout.Label("[ / ] = FOV");
            GUILayout.Space(6f);
            GUILayout.Label($"Elevation: {_pose.ElevationDegrees:F2}");
            GUILayout.Label($"Azimuth: {_pose.AzimuthDegrees:F2}");
            GUILayout.Label($"Distance: {_pose.Distance:F2}");
            GUILayout.Label($"Look-at: ({_pose.LookAt.x:F2}, {_pose.LookAt.y:F2}, {_pose.LookAt.z:F2})");
            GUILayout.Label($"FOV: {_pose.FieldOfView:F1}");

            if (!string.IsNullOrEmpty(_statusMessage) && Time.unscaledTime < _statusUntil)
                GUILayout.Label(_statusMessage);

            GUILayout.EndArea();
        }

        private void SyncFromCamera()
        {
            var bootstrap = CombatArenaBootstrap.Instance;
            var camera = bootstrap?.ArenaCamera;
            if (camera == null)
                return;

            var config = bootstrap.Config;
            Vector3 lookAt = config != null && config.useManualCameraPose
                ? config.lookAtWorld
                : TryGetGroundLookAt(camera, out var groundHit)
                    ? groundHit
                    : Vector3.zero;

            _pose = CombatArenaCameraPose.FromCamera(camera, lookAt);
        }

        private static bool TryGetGroundLookAt(Camera camera, out Vector3 lookAt)
        {
            var groundPlane = new Plane(Vector3.up, Vector3.zero);
            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (groundPlane.Raycast(ray, out float distance))
            {
                lookAt = ray.GetPoint(distance);
                return true;
            }

            lookAt = Vector3.zero;
            return false;
        }

        private void HandleOrbitInput()
        {
            if (!Input.GetMouseButton(1))
                return;

            _pose.AzimuthDegrees += Input.GetAxis("Mouse X") * orbitSensitivity * 100f * Time.unscaledDeltaTime;
            _pose.ElevationDegrees -= Input.GetAxis("Mouse Y") * orbitSensitivity * 100f * Time.unscaledDeltaTime;
            _pose.ElevationDegrees = Mathf.Clamp(_pose.ElevationDegrees, 5f, 85f);
        }

        private void HandleSaveAndExitInput()
        {
            if (Input.GetKeyDown(saveKey))
                SavePose();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _active = false;
                Cursor.lockState = CursorLockMode.None;
                SetStatus("Camera tuning OFF");
            }
        }

        private void HandlePanInput()
        {
            float speed = panSpeed * Time.unscaledDeltaTime * (Input.GetKey(KeyCode.LeftShift) ? 3f : 1f);
            var camera = CombatArenaBootstrap.Instance?.ArenaCamera;
            if (camera == null)
                return;

            var right = camera.transform.right;
            right.y = 0f;
            right.Normalize();
            var forward = camera.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            if (Input.GetKey(KeyCode.W))
                _pose.LookAt += forward * speed;
            if (Input.GetKey(KeyCode.S))
                _pose.LookAt -= forward * speed;
            if (Input.GetKey(KeyCode.D))
                _pose.LookAt += right * speed;
            if (Input.GetKey(KeyCode.A))
                _pose.LookAt -= right * speed;
            if (Input.GetKey(KeyCode.E))
                _pose.LookAt.y += speed;
            if (Input.GetKey(KeyCode.Q))
                _pose.LookAt.y -= speed;
        }

        private void HandleZoomInput()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.001f)
                return;

            _pose.Distance = Mathf.Clamp(
                _pose.Distance - scroll * zoomSpeed,
                2f,
                250f);
        }

        private void HandleFovInput()
        {
            if (Input.GetKey(KeyCode.LeftBracket))
                _pose.FieldOfView = Mathf.Clamp(_pose.FieldOfView - fovStep * Time.unscaledDeltaTime * 60f, 20f, 90f);
            if (Input.GetKey(KeyCode.RightBracket))
                _pose.FieldOfView = Mathf.Clamp(_pose.FieldOfView + fovStep * Time.unscaledDeltaTime * 60f, 20f, 90f);
        }

        private void ApplyPose()
        {
            var camera = CombatArenaBootstrap.Instance?.ArenaCamera;
            if (camera == null)
                return;

            _pose.ApplyOrbitToPosition();
            _pose.ApplyTo(camera);
        }

        private void SavePose()
        {
            var camera = CombatArenaBootstrap.Instance?.ArenaCamera;
            if (camera == null)
                return;

            _pose.WorldPosition = camera.transform.position;
            _pose.FieldOfView = camera.fieldOfView;
            _pose.SyncOrbitFromPosition();

            const string configAssetPath = "Assets/_Project/Data/Resources/DeadManZone/CombatArenaConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<CombatArenaConfigSO>(configAssetPath);
            if (config == null)
            {
                Debug.LogError($"Combat arena config not found at {configAssetPath}");
                return;
            }

            Undo.RecordObject(config, "Save Combat Arena Camera");
            config.useManualCameraPose = true;
            config.autoFrameWidth = false;
            config.autoFrameVerticalPosition = false;
            config.manualCameraWorldPosition = _pose.WorldPosition;
            config.lookAtWorld = _pose.LookAt;
            config.fieldOfView = _pose.FieldOfView;
            config.cameraElevationDegrees = _pose.ElevationDegrees;
            config.cameraAzimuthDegrees = _pose.AzimuthDegrees;
            config.cameraDistance = _pose.Distance;
            config.lookAtDepthOffset = _pose.LookAt.z;

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Saved combat arena camera to CombatArenaConfig.asset\n" +
                $"  position=({_pose.WorldPosition.x:F2}, {_pose.WorldPosition.y:F2}, {_pose.WorldPosition.z:F2})\n" +
                $"  lookAt=({_pose.LookAt.x:F2}, {_pose.LookAt.y:F2}, {_pose.LookAt.z:F2})  fov={_pose.FieldOfView:F1}");

            SetStatus("Saved to CombatArenaConfig.asset");
        }

        private void SetStatus(string message)
        {
            _statusMessage = message;
            _statusUntil = Time.unscaledTime + 3f;
        }
    }
}
#endif
