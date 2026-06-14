using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Composes a static Military Warehouse corridor behind the cinematic menu camera.
    /// Skipped automatically when PolygonMapsMilitaryWarehouse is not imported.
    /// </summary>
    internal static class MainMenuBackdropBuilder
    {
        internal const string KitRoot = "Assets/Synty/PolygonMapsMilitaryWarehouse";
        internal const string RootName = "MainMenuBackdrop";

        private static readonly string[] CorridorPrefabPaths =
        {
            $"{KitRoot}/Prefabs/SM_Bld_Floor_Destroyed_01.prefab",
            $"{KitRoot}/Prefabs/SM_Bld_Wall_Garage_Large_01.prefab",
            $"{KitRoot}/Prefabs/SM_Bld_Mil_Wall_Window_Double_01.prefab",
            $"{KitRoot}/Prefabs/SM_Bld_Mil_Door_Large_01.prefab",
            $"{KitRoot}/Prefabs/SM_Prop_Shipping_Container_Small_01.prefab",
            $"{KitRoot}/Prefabs/SM_Prop_Barrel_Bomb_01.prefab"
        };

        private static readonly (Vector3 position, Vector3 euler)[] CorridorLayout =
        {
            (new Vector3(0f, 0f, 0f), Vector3.zero),
            (new Vector3(0f, 0f, 3.2f), new Vector3(0f, 180f, 0f)),
            (new Vector3(-3.5f, 0f, 1.2f), new Vector3(0f, 90f, 0f)),
            (new Vector3(0f, 0f, 2.8f), Vector3.zero),
            (new Vector3(-1.8f, 0f, 0.8f), new Vector3(0f, 25f, 0f)),
            (new Vector3(1.5f, 0f, 1.5f), new Vector3(0f, -15f, 0f))
        };

        internal static bool IsKitAvailable() => AssetDatabase.IsValidFolder(KitRoot);

        internal static GameObject TryBuild()
        {
            if (!IsKitAvailable())
            {
                Debug.LogWarning(
                    $"PolygonMapsMilitaryWarehouse not found at {KitRoot}. Skipping MainMenu backdrop.");
                return null;
            }

            var root = new GameObject(RootName);
            root.transform.position = new Vector3(2.8f, 0f, 0.2f);
            root.transform.rotation = Quaternion.Euler(0f, -132f, 0f);

            var placed = 0;
            for (var i = 0; i < CorridorPrefabPaths.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CorridorPrefabPaths[i]);
                if (prefab == null)
                    continue;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                if (instance == null)
                    continue;

                var (localPos, localEuler) = i < CorridorLayout.Length
                    ? CorridorLayout[i]
                    : (Vector3.zero, Vector3.zero);
                instance.transform.localPosition = localPos;
                instance.transform.localRotation = Quaternion.Euler(localEuler);
                instance.transform.localScale = Vector3.one;
                placed++;
            }

            if (placed == 0)
            {
                Object.DestroyImmediate(root);
                Debug.LogWarning("No Military Warehouse prefabs available for MainMenu backdrop.");
                return null;
            }

            DisableColliders(root);
            return root;
        }

        internal static void DisableColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
        }
    }
}
