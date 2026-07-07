using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>URP-safe unlit materials from combat sprites (SpriteRenderer is unreliable on 3D ground).</summary>
    internal static class CombatArena2DSpriteMaterial
    {
        // Shader.Find is a linear scan of every loaded shader — called per VFX it dominates
        // CPU. Cache the resolved shaders once; they never change during a session.
        private static Shader _unlitShader;
        private static Shader _spriteShader;
        private static Shader _additiveShader;

        private static Shader UnlitShader => _unlitShader != null
            ? _unlitShader
            : _unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Texture")
                ?? CombatArenaMaterialUtility.ResolveGroundShader();

        private static Shader SpriteShader => _spriteShader != null
            ? _spriteShader
            : _spriteShader = Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Transparent")
                ?? Shader.Find("Unlit/Texture");

        private static Shader AdditiveShader => _additiveShader != null
            ? _additiveShader
            : _additiveShader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Transparent");

        public static Material CreateUnlit(Sprite sprite, Color tint)
        {
            if (sprite == null || sprite.texture == null)
                return null;

            var shader = UnlitShader;
            if (shader == null)
                return null;

            var material = new Material(shader)
            {
                name = $"Combat2D_{sprite.name}",
                hideFlags = HideFlags.HideAndDontSave
            };

            var texture = sprite.texture;
            var rect = sprite.textureRect;
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_MainTex", texture);
            material.SetTextureScale("_BaseMap", new Vector2(rect.width / texture.width, rect.height / texture.height));
            material.SetTextureOffset("_BaseMap", new Vector2(rect.x / texture.width, rect.y / texture.height));
            material.SetTextureScale("_MainTex", material.GetTextureScale("_BaseMap"));
            material.SetTextureOffset("_MainTex", material.GetTextureOffset("_BaseMap"));

            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", (float)CullMode.Off);

            CombatArenaMaterialUtility.ApplyColor(material, tint);
            material.renderQueue = 2450;
            return material;
        }

        /// <summary>Alpha-aware sprite material for vertical billboards (mesh supplies sprite UVs).</summary>
        public static Material CreateSprite(
            Sprite sprite,
            Color tint,
            int renderQueue,
            bool softAlpha = false,
            bool ignoreDepth = true)
        {
            if (sprite == null || sprite.texture == null)
                return null;

            var shader = SpriteShader;
            if (shader == null)
                return null;

            var material = new Material(shader)
            {
                name = $"Combat2DSprite_{sprite.name}",
                hideFlags = HideFlags.HideAndDontSave
            };

            material.mainTexture = sprite.texture;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", sprite.texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", sprite.texture);

            CombatArenaMaterialUtility.ApplyColor(material, tint);

            if (shader.name.Contains("Universal Render Pipeline"))
            {
                if (softAlpha)
                    EnableAlphaBlend(material, renderQueue);
                else
                    EnableAlphaClip(material, renderQueue);
            }
            else
                material.renderQueue = renderQueue;

            // Billboards must ignore ground depth or oblique quads clip through the grid plane.
            if (ignoreDepth)
                DisableDepthTest(material);
            return material;
        }

        public static Material CreateUnlit(Sprite sprite, Color tint, int renderQueue)
        {
            var material = CreateUnlit(sprite, tint);
            if (material != null)
                material.renderQueue = renderQueue;
            return material;
        }

        /// <summary>Additive material for fire/impact VFX strips: black backgrounds add
        /// nothing, so strips authored without alpha still composite cleanly.</summary>
        public static Material CreateSpriteAdditive(Sprite sprite, int renderQueue)
        {
            if (sprite == null || sprite.texture == null)
                return null;

            var shader = AdditiveShader;
            if (shader == null)
                return null;

            var material = new Material(shader)
            {
                name = $"Combat2DVfxAdditive_{sprite.name}",
                hideFlags = HideFlags.HideAndDontSave
            };

            material.mainTexture = sprite.texture;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", sprite.texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", sprite.texture);

            CombatArenaMaterialUtility.ApplyColor(material, Color.white);
            EnableAlphaBlend(material, renderQueue);
            if (material.HasProperty("_SrcBlend"))
                material.SetInt("_SrcBlend", (int)BlendMode.One);
            if (material.HasProperty("_DstBlend"))
                material.SetInt("_DstBlend", (int)BlendMode.One);

            DisableDepthTest(material);
            return material;
        }

        internal static void EnableAlphaClip(Material material, int renderQueue, float cutoff = 0.08f)
        {
            if (material == null)
                return;

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_AlphaClip"))
                material.SetFloat("_AlphaClip", 1f);
            if (material.HasProperty("_Cutoff"))
                material.SetFloat("_Cutoff", cutoff);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", (float)CullMode.Off);

            material.EnableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = renderQueue;
        }

        internal static void EnableAlphaBlend(Material material, int renderQueue)
        {
            if (material == null)
                return;

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend"))
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite"))
                material.SetInt("_ZWrite", 0);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", (float)CullMode.Off);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = renderQueue;
        }

        internal static void DisableDepthTest(Material material)
        {
            if (material == null)
                return;

            if (material.HasProperty("_ZTest"))
                material.SetInt("_ZTest", (int)CompareFunction.Always);
        }

        /// <summary>Repeat sprite UVs across a world-sized quad (backdrop dirt fill).</summary>
        public static Material CreateUnlitTiled(
            Sprite sprite,
            Color tint,
            int renderQueue,
            Vector2 worldSize)
        {
            var material = CreateUnlit(sprite, tint, renderQueue);
            if (material == null)
                return null;

            Vector2 repeat = ComputeTileRepeat(sprite, worldSize);
            ApplyTextureRepeat(material, repeat.x, repeat.y);
            return material;
        }

        internal static Vector2 ComputeTileRepeat(Sprite sprite, Vector2 worldSize)
        {
            if (sprite == null)
                return Vector2.one;

            float tileWorld = sprite.rect.width / sprite.pixelsPerUnit;
            if (tileWorld <= 0.0001f)
                return Vector2.one;

            return new Vector2(
                Mathf.Max(1f, worldSize.x / tileWorld),
                Mathf.Max(1f, worldSize.y / tileWorld));
        }

        private static void ApplyTextureRepeat(Material material, float repeatX, float repeatY)
        {
            var baseScale = material.GetTextureScale("_BaseMap");
            var baseOffset = material.GetTextureOffset("_BaseMap");
            var repeat = new Vector2(baseScale.x * repeatX, baseScale.y * repeatY);
            material.SetTextureScale("_BaseMap", repeat);
            material.SetTextureOffset("_BaseMap", baseOffset);
            material.SetTextureScale("_MainTex", repeat);
            material.SetTextureOffset("_MainTex", baseOffset);
        }

        /// <summary>Subtle zone wash so art stays readable (full multiply turns tiles to mud).</summary>
        public static Color ResolveZoneTint(Color zoneCellColor, float strength = 0.35f) =>
            Color.Lerp(Color.white, zoneCellColor, Mathf.Clamp01(strength));
    }
}
