using DeadManZone.Core.Combat;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Static building sprite for TopTroops2D mode.</summary>
    public static class CombatArena2DBuildingVisual
    {
        // Match grid cell surface (CellYOffset 0.06) so texture bottom sits on the tile.
        private const float FeetLift = 0.08f;

        public static GameObject Spawn(
            string instanceId,
            Vector3 center,
            PieceDefinitionSO source,
            CombatSide side)
        {
            var root = new GameObject($"Building2D_{instanceId}");
            root.transform.position = center;

            Sprite sprite = source != null
                ? CombatUnitSpriteResolver.Resolve(source, side)
                : CombatArena2DPlaceholderSprites.DefaultSilhouette;
            Color tint = source != null
                ? CombatUnitSpriteResolver.ResolveTint(source, side)
                : Color.white;
            int renderQueue = CombatArena2DSortOrder.RenderQueueFromWorldZ(center.z);

            float scale = source != null && source.combatArenaModelScale > 0f ? source.combatArenaModelScale : 1f;
            scale *= 1.4f;

            var camera = CombatArenaBootstrap.Instance?.ArenaCamera;
            CombatArena2DSpriteQuad.AttachBillboard(
                root.transform,
                sprite,
                tint,
                scale,
                renderQueue,
                camera,
                localFeet: Vector3.up * FeetLift,
                softAlpha: true,
                groundBottom: true);

            AttachBuildingShadow(root.transform, renderQueue);
            return root;
        }

        private static void AttachBuildingShadow(Transform buildingRoot, int renderQueue)
        {
            CombatArena2DSpriteQuad.CreateFlatShadow(
                buildingRoot,
                CombatArena2DEnvironmentArt.BuildingShadow,
                new Vector3(0f, FeetLift * 0.5f, -0.1f),
                Vector3.one * 1.15f,
                renderQueue - 1);
        }
    }
}
