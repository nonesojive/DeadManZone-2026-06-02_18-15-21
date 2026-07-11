#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Generates the stylized low-poly rifle prop (Assets/_Project/Combat3D/Rifle_Prop.prefab):
    /// primitive boxes + a cylinder barrel under one root, ~0.73 m bolt-action silhouette,
    /// gunmetal/wood DMZ/ToonInk materials with the inverted-hull outline ON (clean primitive
    /// normals take the crisp ink line — same doctrine as the sandbag graybox). A MuzzlePoint
    /// empty at the barrel tip is the muzzle-flash anchor consumed by CombatUnitVisual3D.
    /// Deterministic: rebuilding overwrites the prefab in place (GUID stable).
    /// </summary>
    public static class RiflePropBuilder
    {
        public const string PrefabPath = "Assets/_Project/Combat3D/Rifle_Prop.prefab";
        private const string MetalMaterialPath = "Assets/_Project/Combat3D/Combat3D_RifleMetal.mat";
        private const string WoodMaterialPath = "Assets/_Project/Combat3D/Combat3D_RifleWood.mat";

        [MenuItem("DeadManZone/Combat3D/Rebuild Rifle Prop")]
        public static void RebuildFromMenu() => EnsurePrefab();

        /// <summary>Builds (or rebuilds) the rifle prefab and returns the saved asset.
        /// Local +Z is the barrel direction; origin sits at the grip.</summary>
        public static GameObject EnsurePrefab()
        {
            var metal = EnsureToonInkMaterial(MetalMaterialPath, new Color(0.16f, 0.17f, 0.20f));
            var wood = EnsureToonInkMaterial(WoodMaterialPath, new Color(0.30f, 0.20f, 0.12f));

            var root = new GameObject("Rifle_Prop");
            try
            {
                AddPart(root.transform, PrimitiveType.Cube, "Stock_Wood", wood,
                    new Vector3(0f, -0.012f, -0.20f), new Vector3(-5f, 0f, 0f),
                    new Vector3(0.045f, 0.085f, 0.20f));
                AddPart(root.transform, PrimitiveType.Cube, "Receiver_Metal", metal,
                    new Vector3(0f, 0.03f, -0.02f), Vector3.zero,
                    new Vector3(0.042f, 0.06f, 0.18f));
                AddPart(root.transform, PrimitiveType.Cube, "Forestock_Wood", wood,
                    new Vector3(0f, 0.012f, 0.16f), Vector3.zero,
                    new Vector3(0.04f, 0.05f, 0.22f));
                AddPart(root.transform, PrimitiveType.Cylinder, "Barrel_Metal", metal,
                    new Vector3(0f, 0.035f, 0.31f), new Vector3(90f, 0f, 0f),
                    new Vector3(0.016f, 0.115f, 0.016f));
                AddPart(root.transform, PrimitiveType.Cube, "BoltHandle_Metal", metal,
                    new Vector3(0.032f, 0.045f, -0.04f), Vector3.zero,
                    new Vector3(0.05f, 0.015f, 0.015f));

                var muzzle = new GameObject("MuzzlePoint");
                muzzle.transform.SetParent(root.transform, false);
                muzzle.transform.localPosition = new Vector3(0f, 0.035f, 0.43f);

                // Left-hand IK anchor: underside of the forestock, where the support
                // palm rests (consumed by CombatUnitVisual3D's two-bone left-arm IK).
                var forestock = new GameObject("ForestockPoint");
                forestock.transform.SetParent(root.transform, false);
                forestock.transform.localPosition = new Vector3(0f, -0.018f, 0.16f);

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[Combat3D] Rifle prop prefab saved to {PrefabPath}.");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AddPart(
            Transform root, PrimitiveType type, string name, Material material,
            Vector3 localPosition, Vector3 localEuler, Vector3 localScale)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(root, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localEuler);
            part.transform.localScale = localScale;
            if (material != null)
                part.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Material EnsureToonInkMaterial(string path, Color baseColor)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("DMZ/ToonInk");
                if (shader == null)
                {
                    Debug.LogError("[Combat3D] DMZ/ToonInk shader not found — rifle material falls back to URP Lit (no ink line).");
                    shader = Shader.Find("Universal Render Pipeline/Lit");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", baseColor);
            // Clean primitive normals → hull outline stays a crisp ink line here
            // (units keep it OFF; their silhouette comes from the fullscreen pass).
            if (material.HasProperty("_OutlineWidth"))
                material.SetFloat("_OutlineWidth", 2f);
            if (material.HasProperty("_ShadowColor"))
                material.SetColor("_ShadowColor", baseColor * 0.55f);
            if (material.HasProperty("_InkStrength"))
                material.SetFloat("_InkStrength", 0.25f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
#endif
