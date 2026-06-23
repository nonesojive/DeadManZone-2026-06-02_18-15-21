using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Camera-locked sky gradient behind the board (ortho-safe).</summary>
    public sealed class CombatArena2DSkyBillboard : MonoBehaviour
    {
        private const float Distance = 80f;
        private const float WidthScale = 2.2f;
        private const float HeightFraction = 0.55f;
        private const float UpOffsetFraction = 0.22f;

        private Camera _camera;

        public void Bind(Camera arenaCamera) => _camera = arenaCamera;

        private void LateUpdate()
        {
            if (_camera == null)
                return;

            transform.rotation = _camera.transform.rotation;

            float halfHeight = _camera.orthographic
                ? _camera.orthographicSize
                : Distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfWidth = halfHeight * _camera.aspect;
            float skyHalfHeight = halfHeight * HeightFraction;
            transform.localScale = new Vector3(halfWidth * WidthScale, skyHalfHeight * 2f, 1f);

            transform.position = _camera.transform.position
                + _camera.transform.forward * Distance
                + _camera.transform.up * (halfHeight * UpOffsetFraction);
        }
    }
}
