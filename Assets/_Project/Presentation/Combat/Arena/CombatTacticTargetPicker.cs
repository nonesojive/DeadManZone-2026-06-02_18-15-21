using System;
using DeadManZone.Core.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Battlefield target selection for targeted pause abilities (e.g. GrenadeLob):
    /// while picking, the mouse ray is intersected with the ground plane (the arena has
    /// no colliders by design), converted to a grid cell via <see cref="CombatGridMapper"/>,
    /// validated by the caller-supplied predicate (which mirrors what the Core executor
    /// will honor), and confirmed with LMB. RMB / Escape cancels. Markers are flat
    /// ground discs (DMZ/CombatRingFill quads) in the max-contrast targeting register
    /// (game bible §6 mechanical-overlay exception): amber = valid / pending target,
    /// dark red = invalid hover. <see cref="TryPickWorldPoint"/> is the single accept
    /// path — mouse clicks and programmatic verification both go through it.
    /// </summary>
    public sealed class CombatTacticTargetPicker : MonoBehaviour
    {
        private static readonly Color ValidFill = new(0.93f, 0.70f, 0.26f, 1f);
        private static readonly Color ValidRim = new(1f, 0.85f, 0.45f, 1f);
        private static readonly Color InvalidFill = new(0.50f, 0.16f, 0.12f, 1f);
        private static readonly Color InvalidRim = new(0.66f, 0.24f, 0.18f, 1f);
        private static readonly Color PendingFill = new(0.80f, 0.58f, 0.20f, 1f);
        private static readonly Color DarkCore = new(0.10f, 0.09f, 0.08f, 1f);

        private CombatGridMapper _mapper;
        private Camera _camera;
        private Func<GridCoord, bool> _isValidTarget;
        private Action<GridCoord> _onPicked;
        private Action _onCancelled;

        private Transform _hoverMarker;
        private MeshRenderer _hoverRenderer;
        private Transform _pendingRoot;
        private Material _markerMaterial;

        public bool IsPicking { get; private set; }

        public void BeginPick(
            CombatGridMapper mapper,
            Camera arenaCamera,
            Func<GridCoord, bool> isValidTarget,
            Action<GridCoord> onPicked,
            Action onCancelled)
        {
            _mapper = mapper;
            _camera = arenaCamera;
            _isValidTarget = isValidTarget;
            _onPicked = onPicked;
            _onCancelled = onCancelled;
            IsPicking = true;
        }

        public void CancelPick()
        {
            if (!IsPicking)
                return;

            IsPicking = false;
            HideHoverMarker();
            _onCancelled?.Invoke();
        }

        /// <summary>
        /// The one accept path: world point → grid cell → caller validation → confirm.
        /// Ground clicks call this with the mouse ray's ground-plane hit; programmatic
        /// verification calls it with a world point directly (same code, same verdicts).
        /// Returns false (pick mode stays active) for off-grid or invalid cells.
        /// </summary>
        public bool TryPickWorldPoint(Vector3 worldPoint)
        {
            if (!IsPicking || _mapper == null)
                return false;

            if (!_mapper.TryWorldToCoord(worldPoint, out var cell))
                return false;

            if (_isValidTarget != null && !_isValidTarget(cell))
                return false;

            IsPicking = false;
            HideHoverMarker();
            _onPicked?.Invoke(cell);
            return true;
        }

        /// <summary>Rebuild the persistent pending-target markers (one per queued targeted command).</summary>
        public void SetPendingMarkers(System.Collections.Generic.IEnumerable<GridCoord> cells)
        {
            if (_pendingRoot == null)
            {
                _pendingRoot = new GameObject("TacticTargetPendingMarkers").transform;
                _pendingRoot.SetParent(transform, false);
            }

            for (int i = _pendingRoot.childCount - 1; i >= 0; i--)
                Destroy(_pendingRoot.GetChild(i).gameObject);

            if (cells == null || _mapper == null)
                return;

            foreach (var cell in cells)
            {
                var marker = CreateMarkerQuad("PendingTargetMarker", _pendingRoot);
                marker.transform.position = _mapper.ToWorld(cell) + new Vector3(0f, 0.04f, 0f);
                TintMarker(marker.GetComponent<MeshRenderer>(), PendingFill, ValidRim);
            }
        }

        public void ClearAll()
        {
            IsPicking = false;
            HideHoverMarker();
            if (_pendingRoot != null)
                for (int i = _pendingRoot.childCount - 1; i >= 0; i--)
                    Destroy(_pendingRoot.GetChild(i).gameObject);
        }

        private void Update()
        {
            if (!IsPicking || _camera == null || _mapper == null)
                return;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelPick();
                return;
            }

            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool haveGround = TryMouseToGround(out var worldPoint);
            bool haveCell = haveGround && _mapper.TryWorldToCoord(worldPoint, out var cell);
            GridCoord hoverCell = default;
            if (haveCell)
                _mapper.TryWorldToCoord(worldPoint, out hoverCell);

            if (haveCell && !overUi)
            {
                bool valid = _isValidTarget == null || _isValidTarget(hoverCell);
                ShowHoverMarker(hoverCell, valid);

                if (Input.GetMouseButtonDown(0))
                    TryPickWorldPoint(worldPoint);
            }
            else
            {
                HideHoverMarker();
            }
        }

        private bool TryMouseToGround(out Vector3 worldPoint)
        {
            // No physics: the arena ships no colliders — intersect the camera ray with y=0.
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            var ground = new Plane(Vector3.up, 0f);
            if (ground.Raycast(ray, out float enter))
            {
                worldPoint = ray.GetPoint(enter);
                return true;
            }

            worldPoint = default;
            return false;
        }

        private void ShowHoverMarker(GridCoord cell, bool valid)
        {
            if (_hoverMarker == null)
            {
                var go = CreateMarkerQuad("TargetHoverMarker", transform);
                _hoverMarker = go.transform;
                _hoverRenderer = go.GetComponent<MeshRenderer>();
            }

            _hoverMarker.gameObject.SetActive(true);
            _hoverMarker.position = _mapper.ToWorld(cell) + new Vector3(0f, 0.05f, 0f);
            // Soft unscaled pulse so the reticle reads alive while the fight is time-frozen.
            float pulse = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f);
            float size = _mapper.CellWidth * 0.95f * pulse;
            _hoverMarker.localScale = new Vector3(size, size, size);
            TintMarker(_hoverRenderer, valid ? ValidFill : InvalidFill, valid ? ValidRim : InvalidRim);
        }

        private void HideHoverMarker()
        {
            if (_hoverMarker != null)
                _hoverMarker.gameObject.SetActive(false);
        }

        private GameObject CreateMarkerQuad(string name, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // flat on the ground
            float size = _mapper != null ? _mapper.CellWidth * 0.9f : 1.6f;
            go.transform.localScale = new Vector3(size, size, size);

            if (_markerMaterial == null)
            {
                var shader = Shader.Find("DMZ/CombatRingFill");
                _markerMaterial = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
            }

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _markerMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        private static void TintMarker(MeshRenderer renderer, Color fill, Color rim)
        {
            if (renderer == null)
                return;

            var block = new MaterialPropertyBlock();
            block.SetColor("_FillColor", fill);
            block.SetColor("_RimColor", rim);
            block.SetColor("_EmptyColor", DarkCore);
            block.SetFloat("_Fill", 1f);
            renderer.SetPropertyBlock(block);
        }

        private void OnDestroy()
        {
            if (_markerMaterial != null)
                Destroy(_markerMaterial);
        }
    }
}
