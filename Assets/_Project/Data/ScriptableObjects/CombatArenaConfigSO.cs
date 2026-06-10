using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Combat Arena Config")]
    public sealed class CombatArenaConfigSO : ScriptableObject
    {
        [Header("Grid → world (meters)")]
        public float cellWidth = 1.8f;
        public float cellDepth = 1.8f;

        [Header("Camera")]
        public float cameraElevationDegrees = 35f;
        public float cameraAzimuthDegrees = 225f;
        public float cameraDistance = 28f;
        public float fieldOfView = 45f;

        [Header("Motion")]
        public float moveLerpSeconds = 0.4f;
        public float attackLungeSeconds = 0.15f;
        public float attackLungeDistance = 0.35f;

        [Header("Transition")]
        public float unitSpawnFadeSeconds = 0.35f;
    }
}
