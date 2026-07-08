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
            AttachQuad(root.transform, sprite, tint, uniformScale, renderQueue, camera, Vector3.zero, softAlpha, groundBottom: false);
            // After AttachQuad: it resets root.localPosition, which for a parentless
            // root IS the world position — every impact/explosion rendered at origin.
            root.transform.position = feetWorldPosition;
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
            bool groundBottom = false,
            bool outline = false)
        {
            if (sprite == null || parent == null)
                return null;

            var root = new GameObject("SpriteQuad");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localFeet;
            AttachQuad(root.transform, sprite, tint, uniformScale, renderQueue, camera, Vector3.zero, softAlpha, groundBottom, outline);
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
            bool groundBottom = false,
            bool outline = false)
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
            ApplyMaterial(quad, sprite, tint, renderQueue, softAlpha, ignoreDepth: true, outline: outline);
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

        /// <summary>Swap the displayed animation frame on a built quad (UVs, scale, feet pivot).</summary>
        public static void SetFrame(GameObject root, Sprite frame, float uniformScale = 1f)
        {
            if (root == null || frame == null)
                return;

            var quad = root.transform.Find("Quad");
            if (quad == null)
                return;

            CombatArena2DSpriteMesh.UpdateUvs(quad.GetComponent<MeshFilter>(), frame);

            float width = frame.rect.width / frame.pixelsPerUnit * uniformScale;
            float height = frame.rect.height / frame.pixelsPerUnit * uniformScale;
            quad.localScale = new Vector3(
                Mathf.Abs(quad.localScale.x) < 0.001f ? width : Mathf.Sign(quad.localScale.x) * width,
                height,
                1f);
            quad.localPosition = PivotCenterOffset(frame, uniformScale);

            var renderer = quad.GetComponent<Renderer>();
            var material = renderer != null ? renderer.sharedMaterial : null;
            if (material == null)
                return;

            // Frames within one state share the sheet texture but occupy different UV
            // sub-rects — refresh the outline clamp rect every swap, before the texture
            // early-out below (which fires for the common same-sheet case).
            CombatArena2DSpriteMaterial.ApplyFrameRect(material, frame);

            if (material.mainTexture == frame.texture)
                return;

            material.mainTexture = frame.texture;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", frame.texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", frame.texture);
        }

        /// <summary>Mirror the figure horizontally (sprites are authored facing right).</summary>
        public static void SetFlipX(GameObject root, bool flip)
        {
            if (root == null)
                return;

            var quad = root.transform.Find("Quad");
            if (quad == null)
                return;

            var scale = quad.localScale;
            float magnitude = Mathf.Abs(scale.x);
            scale.x = flip ? -magnitude : magnitude;
            quad.localScale = scale;
        }

        /// <summary>Swap to a soft-alpha (blended) material so the quad can fade smoothly —
        /// the default cutout material pops instead of fading when alpha drops.</summary>
        public static void SetFadeMaterial(GameObject root, Sprite sprite, int renderQueue)
        {
            if (root == null || sprite == null)
                return;

            var quad = root.transform.Find("Quad");
            var renderer = quad != null ? quad.GetComponent<Renderer>() : null;
            if (renderer == null)
                return;

            var material = CombatArena2DSpriteMaterial.CreateSprite(
                sprite, Color.white, renderQueue, softAlpha: true);
            if (material != null)
                renderer.sharedMaterial = material;
        }

        /// <summary>Tint the quad's material (each quad owns its material instance).</summary>
        public static void SetTint(GameObject root, Color tint)
        {
            if (root == null)
                return;

            var quad = root.transform.Find("Quad");
            var renderer = quad != null ? quad.GetComponent<Renderer>() : null;
            if (renderer?.sharedMaterial != null)
                CombatArenaMaterialUtility.ApplyColor(renderer.sharedMaterial, tint);
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
            float visibleBottom = CombatArena2DSpriteMetrics.VisibleBottomUnits(sprite) * scale;
            return Vector3.up * (height * 0.5f - visibleBottom);
        }

        private static void ApplyMaterial(
            GameObject quad,
            Sprite sprite,
            Color tint,
            int renderQueue,
            bool softAlpha = false,
            bool ignoreDepth = true,
            bool outline = false)
        {
            var meshFilter = quad.GetComponent<MeshFilter>();
            CombatArena2DSpriteMesh.Apply(meshFilter, sprite);

            var renderer = quad.GetComponent<Renderer>();
            var material = outline
                ? CombatArena2DSpriteMaterial.CreateSpriteOutlined(sprite, tint, renderQueue, ignoreDepth)
                : CombatArena2DSpriteMaterial.CreateSprite(sprite, tint, renderQueue, softAlpha, ignoreDepth);
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
