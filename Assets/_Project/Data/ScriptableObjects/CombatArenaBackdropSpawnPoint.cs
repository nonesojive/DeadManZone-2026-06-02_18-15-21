using UnityEngine;

namespace DeadManZone.Data
{
    public readonly struct CombatArenaBackdropSpawnPoint
    {
        public CombatArenaBackdropSpawnPoint(
            CombatArenaBackdropRing ring,
            Vector3 localPosition,
            float yawDegrees,
            float uniformScale,
            int catalogIndex)
        {
            Ring = ring;
            LocalPosition = localPosition;
            YawDegrees = yawDegrees;
            UniformScale = uniformScale;
            CatalogIndex = catalogIndex;
        }

        public CombatArenaBackdropRing Ring { get; }
        public Vector3 LocalPosition { get; }
        public float YawDegrees { get; }
        public float UniformScale { get; }
        public int CatalogIndex { get; }
    }
}
