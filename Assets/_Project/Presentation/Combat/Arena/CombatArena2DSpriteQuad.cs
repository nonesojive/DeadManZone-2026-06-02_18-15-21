using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>URP unlit quads for vertical/flat combat sprites (SpriteRenderer clips in oblique 3D).</summary>
    internal static class CombatArena2DSpriteQuad
    {
        public static GameObject CreateBillboard(
            Sprite sprite,
            Color tint,
            Vector3 feetWorldPosition,
            float uniformScale,
            int renderQueue,
            Camera camera,
            bool softAlpha = false)
        {
            if (sprite == null)
                return null;

            var root = new GameObject("SpriteQuad");
            root.transform.position = feetWorldPosition;
            AttachQuad(root.transform, sprite, tint, uniformScale, renderQueue, camera, Vector3.zero, softAlpha, groundBottom: false);
            return root;
        }

        public static GameObject AttachBillboard(
            Transform parent,
            Sprite sprite,
            Color tint,
            float uniformScale,
            int renderQueue,
            Camera camera,
            Vector3 localFeet = default,
            bool softAlpha = false,
            bool groundBottom = false)
        {
            if (sprite == null || parent == null)
                return null;

            var root = new GameObject("SpriteQuad");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localFeet;
            AttachQuad(root.transform, sprite, tint, uniformScale, renderQueue, camera, Vector3.zero, softAlpha, groundBottom);
            return root;
        }

        private static void AttachQuad(
            Transform root,
            Sprite sprite,
            Color tint,
            float uniformScale,
            int renderQueue,
            Camera camera,
            Vector3 localFeet,
            bool softAlpha,
            bool groundBottom = false)
        {
            float width = sprite.rect.width / sprite.pixelsPerUnit * uniformScale;
            float height = sprite.rect.height / sprite.pixelsPerUnit * uniformScale;

            root.localPosition = localFeet;
            if (camera != null)
                root.gameObject.AddComponent<CombatArena2DSpriteBillboard>().Bind(camera);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Quad";
            quad.transform.SetParent(root, false);
            quad.transform.localPosition = groundBottom
                ? GroundBottomOffset(sprite, uniformScale)
                : PivotCenterOffset(sprite, uniformScale);
            quad.transform.localScale = new Vector3(width, height, 1f);
            ApplyMaterial(quad, sprite, tint, renderQueue, softAlpha, ignoreDepth: true);
            DestroyCollider(quad);
        }

        public static GameObject CreateFlatShadow(
            Transform parent,
            Sprite sprite,
            Vector3 localPosition,
            Vector3 localScale,
            int renderQueue)
        {
            if (sprite == null)
                return null;

            float width = sprite.rect.width / sprite.pixelsPerUnit;
            float height = sprite.rect.height / sprite.pixelsPerUnit;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Shadow";
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = localPosition;
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(width * localScale.x, height * localScale.y, 1f);
            ApplyMaterial(quad, sprite, Color.white, renderQueue, softAlpha: true, ignoreDepth: false);
            DestroyCollider(quad);
            return quad;
        }

        public static void SetRenderQueue(GameObject root, int renderQueue)
        {
            if (root == null)
                return;

            var renderer = root.GetComponentInChildren<Renderer>();
            if (renderer?.sharedMaterial != null)
                renderer.sharedMaterial.renderQueue = renderQueue;
        }

        internal static Vector3 GroundBottomOffset(Sprite sprite, float scale)
        {
            float height = sprite.rect.height / sprite.pixelsPerUnit * scale;
            return Vector3.up * (height * 0.5f);
        }

        internal static Vector3 PivotCenterOffset(Sprite sprite, float scale)
        {
            float height = sprite.rect.height / sprite.pixelsPerUnit * scale;
            float pivotFromBottom = sprite.pivot.y / sprite.pixelsPerUnit * scale;
            return Vector3.up * (height * 0.5f - pivotFromBottom);
        }

        private static void ApplyMaterial(
            GameObject quad,
            Sprite sprite,
            Color tint,
            int renderQueue,
            bool softAlpha = false,
            bool ignoreDepth = true)
        {
            var meshFilter = quad.GetComponent<MeshFilter>();
            CombatArena2DSpriteMesh.Apply(meshFilter, sprite);

            var renderer = quad.GetComponent<Renderer>();
            var material = CombatArena2DSpriteMaterial.CreateSprite(sprite, tint, renderQueue, softAlpha, ignoreDepth);
            if (material != null)
                renderer.sharedMaterial = material;
        }

        private static void DestroyCollider(GameObject obj)
        {
            var collider = obj.GetComponent<Collider>();
            if (collider == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(collider);
            else
                Object.DestroyImmediate(collider);
        }
    }
}
