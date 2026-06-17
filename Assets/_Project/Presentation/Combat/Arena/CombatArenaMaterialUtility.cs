using System.Collections.Generic;
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
        private static Shader _urpLitShader;
        private static bool? _urpActive;
        private static readonly Dictionary<Material, Material> _runtimeMaterialTwins = new();
        public static bool IsUrpActive()
        {
            if (!_urpActive.HasValue)
                _urpActive = GraphicsSettings.currentRenderPipeline != null;

            return _urpActive.Value;
        }

        public static bool IsMaterialRenderable(Material material)
        {
            if (material == null)
                return false;

            var shader = material.shader;
            return shader != null
                   && shader.isSupported
                   && shader.name != "Hidden/InternalErrorShader";
        }

        public static void ResetPipelineCache()
        {
            _urpActive = null;
            _groundShader = null;
            _urpLitShader = null;
            _runtimeMaterialTwins.Clear();
        }

        /// <summary>Remaps Built-in Standard materials on spawned props to URP Lit (prevents magenta).</summary>
        public static void FixBuiltInMaterials(GameObject root)
        {
            if (root == null)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                var sources = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < sources.Length; i++)
                {
                    var src = sources[i];
                    if (src == null || !NeedsUrpConversion(src))
                        continue;

                    sources[i] = GetRuntimeUrpTwin(src);
                    changed = true;
                }

                if (changed)
                    renderer.sharedMaterials = sources;
            }
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

        public static void ApplySolidGroundMaterial(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = ResolveGroundShader();

            if (shader == null)
            {
                ApplyFallbackGroundMaterial(renderer);
                return;
            }

            var material = new Material(shader)
            {
                name = "CombatArenaGridBackdrop",
                hideFlags = HideFlags.HideAndDontSave
            };
            ApplyColor(material, color);
            renderer.sharedMaterial = material;
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

        private static bool NeedsUrpConversion(Material mat)
        {
            if (mat.shader == null)
                return true;

            string shaderName = mat.shader.name;
            return shaderName == "Hidden/InternalErrorShader"
                   || shaderName == "Standard"
                   || shaderName == "Standard (Specular setup)"
                   || shaderName.StartsWith("Legacy Shaders/");
        }

        private static Material GetRuntimeUrpTwin(Material src)
        {
            if (_runtimeMaterialTwins.TryGetValue(src, out var cached) && cached != null)
                return cached;

            var shader = ResolveUrpLitShader();
            if (shader == null)
                return src;

            var urp = new Material(shader);
            CopyMaterialSurface(src, urp);
            _runtimeMaterialTwins[src] = urp;
            return urp;
        }

        private static Shader ResolveUrpLitShader()
        {
            if (_urpLitShader != null)
                return _urpLitShader;

            _urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            return _urpLitShader;
        }

        private static void CopyMaterialSurface(Material src, Material urp)
        {
            Texture baseTex = TryGetTexture(src, "_BaseMap") ?? TryGetTexture(src, "_MainTex");
            if (baseTex != null)
                urp.SetTexture("_BaseMap", baseTex);

            Color baseCol = src.HasProperty("_BaseColor")
                ? src.GetColor("_BaseColor")
                : (src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white);
            urp.SetColor("_BaseColor", baseCol);
            urp.SetFloat("_Smoothness", 0.12f);
            urp.SetFloat("_Metallic", 0f);

            Texture normal = TryGetTexture(src, "_BumpMap");
            if (normal != null)
            {
                urp.SetTexture("_BumpMap", normal);
                urp.EnableKeyword("_NORMALMAP");
            }

            if (IsAlphaCut(src) || IsTransparent(src))
            {
                float cutoff = src.HasProperty("_Cutoff") ? src.GetFloat("_Cutoff") : 0.5f;
                urp.SetFloat("_Surface", 0f);
                urp.SetFloat("_AlphaClip", 1f);
                urp.EnableKeyword("_ALPHATEST_ON");
                urp.SetFloat("_Cutoff", Mathf.Max(0.4f, cutoff));
                urp.SetFloat("_Cull", (float)CullMode.Off);
                urp.renderQueue = 2450;
            }
        }

        private static bool IsTransparent(Material mat)
        {
            if (mat.IsKeywordEnabled("_ALPHABLEND_ON") || mat.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"))
                return true;

            return mat.GetTag("RenderType", false, string.Empty) == "Transparent";
        }

        private static bool IsAlphaCut(Material mat)
        {
            if (mat.IsKeywordEnabled("_ALPHATEST_ON") || mat.renderQueue == 2450)
                return true;

            return mat.GetTag("RenderType", false, string.Empty) == "TransparentCutout";
        }

        private static Texture TryGetTexture(Material mat, string prop) =>
            mat.HasProperty(prop) ? mat.GetTexture(prop) : null;
    }
}