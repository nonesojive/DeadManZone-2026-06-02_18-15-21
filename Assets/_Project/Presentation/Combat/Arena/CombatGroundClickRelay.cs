using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>2026-07-17 round-2 playtest fix: routes a battlefield ground click through the
    /// real EventSystem pointer-click pipeline instead of only the legacy Input.mousePosition
    /// poll CombatTacticTargetPicker.Update already does. Sits on a full-screen, invisible,
    /// raycastable Image — active only while CombatTacticOrdersWindow's SELECT TRANSPORT TARGET
    /// gate is picking — and turns a genuine OnPointerClick (real mouse click, or an
    /// EventSystem/ExecuteEvents-dispatched one for automated verification) into the same
    /// world-point accept call a normal ground click makes. Never the only path: the picker's
    /// own Update() polling keeps working unchanged for ordinary ability targeting.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class CombatGroundClickRelay : MonoBehaviour, IPointerClickHandler
    {
        private Func<Camera> _resolveCamera;
        private Func<Vector3, bool> _onWorldPointClicked;

        public void Configure(Func<Camera> resolveCamera, Func<Vector3, bool> onWorldPointClicked)
        {
            _resolveCamera = resolveCamera;
            _onWorldPointClicked = onWorldPointClicked;
        }

        public void SetActive(bool active) => gameObject.SetActive(active);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            var camera = _resolveCamera?.Invoke();
            if (camera == null || _onWorldPointClicked == null)
                return;

            if (TryScreenPointToGround(camera, eventData.position, out var worldPoint))
                _onWorldPointClicked(worldPoint);
        }

        /// <summary>Same ground-plane (y=0) intersection CombatTacticTargetPicker.TryMouseToGround
        /// uses for the legacy poll path — kept in lockstep so both input routes agree on
        /// exactly the same world point for a given screen position.</summary>
        private static bool TryScreenPointToGround(Camera camera, Vector2 screenPoint, out Vector3 worldPoint)
        {
            var ray = camera.ScreenPointToRay(screenPoint);
            var ground = new Plane(Vector3.up, 0f);
            if (ground.Raycast(ray, out float enter))
            {
                worldPoint = ray.GetPoint(enter);
                return true;
            }

            worldPoint = default;
            return false;
        }
    }
}
