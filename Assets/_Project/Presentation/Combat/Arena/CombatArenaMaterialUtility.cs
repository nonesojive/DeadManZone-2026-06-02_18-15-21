using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Pipeline helpers for combat arena. Synty prefabs use native shader graphs on URP —
    /// do not batch-remap their materials at runtime.
    /// </summary>
    internal static class CombatArenaMaterialUtility
    {
        private static readonly Color FallbackGroundColor = new(0.28f, 0.24f, 0.18f);

        // Ordered shader candidates. URP first, then built-in, then unlit options so the
        // ground always resolves a usable shader even if some are stripped from a build.
        private static readonly string[] GroundShaderCandidates =
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Standard",
            "Sprites/Default",
            "Unlit/Color",
        };

        private static Shader _groundShader;
        private static bool? _urpActive;

        public static bool IsUrpActive()
        {
            if (!_urpActive.HasValue)
                _urpActive = GraphicsSettings.currentRenderPipeline != null;

            return _urpActive.Value;
        }

        public static void ResetPipelineCache()
        {
            _urpActive = null;
            _groundShader = null;
        }

        public static Material CreateFallbackGroundMaterial()
        {
            var shader = ResolveGroundShader();
            if (shader == null)
                return null;

            var material = new Material(shader);
            ApplyColor(material, FallbackGroundColor);
            DampenSmoothness(material);
            return material;
        }

        /// <summary>
        /// Guarantees a ground renderer never keeps the bright default primitive material.
        /// Falls back to recoloring the renderer's existing material when no shader can be
        /// resolved (e.g. shader stripped from a player build).
        /// </summary>
        public static void ApplyFallbackGroundMaterial(Renderer renderer)
        {
            if (renderer == null)
                return;

            var material = CreateFallbackGroundMaterial();
            if (material != null)
            {
                renderer.sharedMaterial = material;
                return;
            }

            // Last resort: recolor the renderer's existing (default primitive) material so the
            // ground is dark dirt-toned instead of the obstructive light-grey default.
            var existing = renderer.material;
            if (existing != null)
            {
                ApplyColor(existing, FallbackGroundColor);
                DampenSmoothness(existing);
            }
        }

        public static void ApplyColor(Material material, Color color)
        {
            if (material == null)
                return;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            material.color = color;
        }

        private static void DampenSmoothness(Material material)
        {
            if (material == null)
                return;

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0f);

            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0f);
        }

        public static Shader ResolveGroundShader()
        {
            if (_groundShader != null)
                return _groundShader;

            foreach (var candidate in GroundShaderCandidates)
            {
                var shader = Shader.Find(candidate);
                if (shader != null)
                {
                    _groundShader = shader;
                    break;
                }
            }

            return _groundShader;
        }
    }
}
