using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Y-sort for 2.5D arena: lower world Z (screen-down) draws in front.</summary>
    public static class CombatArena2DSortOrder
    {
        public const int SortScale = 100;
        public const int GroundRenderQueue = 2450;
        // ponytail: 9-row board z span ~±7.2 → ±720 sort; keep back row above ground tiles
        private const int SpriteQueueBase = 2500 + 800;

        public static int FromWorldZ(float worldZ, int layerOffset = 0) =>
            -(Mathf.RoundToInt(worldZ * SortScale)) + layerOffset;

        public static int RenderQueueFromWorldZ(float worldZ, int layerOffset = 0) =>
            SpriteQueueBase + FromWorldZ(worldZ, layerOffset);
    }
}
