#if UNITY_EDITOR
using System.IO;
using DeadManZone.Data;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class CombatArenaBuildingPrefabGenerator
    {
        private const string OutputFolder =
            "Assets/_Project/Presentation/Combat/Arena/Prefabs/Buildings";

        [MenuItem("DeadManZone/Combat Arena/Generate Building Placeholder Prefabs")]
        public static void GeneratePlaceholderPrefabs()
        {
            Directory.CreateDirectory(OutputFolder);

            CreatePlaceholder("ArenaBuilding_Hq", new Color(0.48f, 0.4f, 0.28f), new Vector3(1.6f, 1.4f, 1.6f));
            CreatePlaceholder("ArenaBuilding_FieldGun", new Color(0.38f, 0.38f, 0.4f), new Vector3(1.2f, 0.9f, 1.2f));
            CreatePlaceholder("ArenaBuilding_SupplyDepot", new Color(0.34f, 0.42f, 0.28f), new Vector3(1.4f, 1f, 1.4f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated combat arena building placeholders in {OutputFolder}.");
        }

        [MenuItem("DeadManZone/Combat Arena/Assign Building Placeholders To Vertical Slice Pieces")]
        public static void AssignBuildingPlaceholdersToVerticalSlicePieces()
        {
            Assign("ironmarch_hq", "ArenaBuilding_Hq.prefab");
            Assign("field_gun_nest", "ArenaBuilding_FieldGun.prefab");
            Assign("supply_depot", "ArenaBuilding_SupplyDepot.prefab");
            AssetDatabase.SaveAssets();
            Debug.Log("Assigned combat arena building placeholders to HQ, field gun, and supply depot.");
        }

        private static void Assign(string pieceId, string prefabFileName)
        {
            string piecePath = $"Assets/_Project/Data/Resources/DeadManZone/Pieces/{pieceId}.asset";
            string prefabPath = $"{OutputFolder}/{prefabFileName}";
            var piece = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>(piecePath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (piece == null)
            {
                Debug.LogWarning($"Piece not found: {piecePath}");
                return;
            }

            if (prefab == null)
            {
                Debug.LogWarning($"Prefab not found: {prefabPath}. Run Generate Building Placeholder Prefabs first.");
                return;
            }

            piece.combatArenaPrefab = prefab;
            piece.combatArenaModelScale = 1f;
            piece.combatArenaModelHeight = 0f;
            EditorUtility.SetDirty(piece);
        }

        private static void CreatePlaceholder(string name, Color color, Vector3 localScale)
        {
            string path = $"{OutputFolder}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return;

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;

            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = color;
                renderer.sharedMaterial = material;
            }

            Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.transform.localScale = localScale;

            PrefabUtility.SaveAsPrefabAsset(cube, path);
            Object.DestroyImmediate(cube);
        }
    }
}
#endif
