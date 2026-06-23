using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Keeps a vertical sprite quad facing the arena camera.</summary>
    public sealed class CombatArena2DSpriteBillboard : MonoBehaviour
    {
        private Camera _camera;

        public void Bind(Camera arenaCamera) => _camera = arenaCamera;

        private void LateUpdate()
        {
            if (_camera == null)
                return;

            transform.rotation = _camera.transform.rotation;
        }
    }
}
