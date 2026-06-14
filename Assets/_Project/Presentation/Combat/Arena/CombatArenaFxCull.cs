using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Disables transparent Synty FX meshes that read as grey overlays in the oblique combat camera.</summary>
    internal static class CombatArenaFxCull
    {
        public static void RemoveTransparentFxRenderers(GameObject root)
        {
            if (root == null)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !ShouldCull(renderer))
                    continue;

                renderer.enabled = false;
            }
        }

        private static bool ShouldCull(Renderer renderer)
        {
            string objectName = renderer.gameObject.name;
            if (ContainsFxToken(objectName))
                return true;

            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null)
                    continue;

                if (material.renderQueue >= 3000)
                    return true;

                if (material.GetTag("RenderType", false, string.Empty) is "Transparent" or "TransparentCutout")
                {
                    // Keep opaque cutout terrain; cull only known FX materials.
                    if (ContainsFxToken(material.name))
                        return true;
                }

                var shader = material.shader;
                if (shader == null)
                    continue;

                string shaderName = shader.name;
                if (shaderName.Contains("Transparent") && ContainsFxToken(material.name))
                    return true;
            }

            return false;
        }

        private static bool ContainsFxToken(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            return value.Contains("Lightray")
                   || value.Contains("Sun_Beam")
                   || value.Contains("Circle_Soft")
                   || value.Contains("Wind_Streak")
                   || value.Contains("Candle_Glow")
                   || value.Contains("Generic_Blank")
                   || value.StartsWith("FX_");
        }
    }
}
