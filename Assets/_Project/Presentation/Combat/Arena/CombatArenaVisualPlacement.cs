using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Uniform scale and ground alignment for arena prefabs (no footprint stretching).</summary>
    internal static class CombatArenaVisualPlacement
    {
        public static void PlaceOnGround(Transform instance, Vector3 worldCenter, float targetHeight, float modelScale)
        {
            if (instance == null)
                return;

            instance.localRotation = Quaternion.identity;
            instance.localScale = Vector3.one;

            float uniformScale = modelScale > 0f ? modelScale : 1f;
            if (TryMeasureLocalHeight(instance, out float currentHeight) && targetHeight > 0f)
                uniformScale *= targetHeight / currentHeight;

            instance.localScale = Vector3.one * uniformScale;

            // Seat the pivot at the anchor, then drop the measured WORLD-space renderer
            // bounds so the feet rest exactly at the anchor's ground height. (The old code
            // assigned a local-space bounds min to a world-space y, which floated the feet
            // by boundsMin.y * (scale - 1) on any model whose pivot isn't at its feet.)
            instance.position = worldCenter;
            float footY = MeasureWorldBounds(instance).min.y;
            instance.position += new Vector3(0f, worldCenter.y - footY, 0f);
        }

        private static Bounds MeasureWorldBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(root.position, Vector3.zero);

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        private static bool TryMeasureLocalHeight(Transform root, out float height)
        {
            height = MeasureLocalBounds(root).size.y;
            return height > 0.001f;
        }

        private static Bounds MeasureLocalBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(root.localPosition, Vector3.zero);

            var bounds = TransformBoundsToLocal(root, renderers[0].bounds);
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(TransformBoundsToLocal(root, renderers[i].bounds));

            return bounds;
        }

        public static bool TryMeasureMeshFootprint(Transform root, out float width, out float depth)
        {
            width = 0f;
            depth = 0f;
            if (root == null)
                return false;

            var bounds = MeasureLocalBounds(root);
            width = bounds.size.x;
            depth = bounds.size.z;
            return width > 0.001f && depth > 0.001f;
        }

        private static Bounds TransformBoundsToLocal(Transform root, Bounds worldBounds)
        {
            var center = root.InverseTransformPoint(worldBounds.center);
            var extents = worldBounds.extents;
            var localExtents = new Vector3(
                Mathf.Abs(root.InverseTransformVector(new Vector3(extents.x, 0f, 0f)).x),
                Mathf.Abs(root.InverseTransformVector(new Vector3(0f, extents.y, 0f)).y),
                Mathf.Abs(root.InverseTransformVector(new Vector3(0f, 0f, extents.z)).z));

            return new Bounds(center, localExtents * 2f);
        }
    }
}
