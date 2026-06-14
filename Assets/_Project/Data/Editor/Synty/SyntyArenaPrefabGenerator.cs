#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class SyntyArenaPrefabGenerator
    {
        [MenuItem("DeadManZone/Synty/Generate Arena Prefab Wrappers")]
        public static void GenerateAll()
        {
            EnsureFolder(SyntyArtPaths.ArenaUnits);
            EnsureFolder(SyntyArtPaths.ArenaVehicles);
            EnsureFolder(SyntyArtPaths.ArenaBuildings);

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SyntyArtPaths.LocomotionController);
            if (controller == null)
            {
                Debug.LogError($"Locomotion controller missing at {SyntyArtPaths.LocomotionController}");
                return;
            }

            CreateUnitPrefab(SyntyArtPaths.UnitRifle, SyntyArtPaths.SidekickRifle, controller);
            CreateUnitPrefab(SyntyArtPaths.UnitSupport, SyntyArtPaths.SidekickSupport, controller);
            CreateUnitPrefab(SyntyArtPaths.UnitMedic, SyntyArtPaths.SidekickMedic, controller);
            CreateUnitPrefab(SyntyArtPaths.UnitSniper, SyntyArtPaths.SidekickSniper, controller);
            CreateUnitPrefab(SyntyArtPaths.UnitOfficer, SyntyArtPaths.SidekickOfficer, controller);

            CreateMeshWrapper(SyntyArtPaths.VehicleTruck, SyntyArtPaths.GermanTruck);
            CreateMeshWrapper(SyntyArtPaths.VehicleCar, SyntyArtPaths.GermanCar);
            CreateMeshWrapper(SyntyArtPaths.VehicleHalftrack, SyntyArtPaths.GermanHalftrack);
            CreateMeshWrapper(SyntyArtPaths.VehicleTank, SyntyArtPaths.GermanTank);
            CreateMeshWrapper(SyntyArtPaths.VehicleMech, SyntyArtPaths.DieselWalkerSource);

            CreateMeshWrapper(SyntyArtPaths.BuildingHq, SyntyArtPaths.BunkerLarge);
            CreateMeshWrapper(SyntyArtPaths.BuildingFieldGun, SyntyArtPaths.BunkerGun);
            CreateMeshWrapper(SyntyArtPaths.BuildingSupplyDepot, SyntyArtPaths.Barracks);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Synty arena prefab wrappers generated under Assets/_Project/Art/Synty/Arena/");
        }

        private static void CreateUnitPrefab(string outputPath, string sourcePath, RuntimeAnimatorController controller)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null)
            {
                Debug.LogWarning($"Unit source missing: {sourcePath}");
                return;
            }

            var instance = Object.Instantiate(source);
            instance.name = System.IO.Path.GetFileNameWithoutExtension(outputPath);

            var animator = instance.GetComponentInChildren<Animator>();
            if (animator == null)
                animator = instance.AddComponent<Animator>();

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            SavePrefab(instance, outputPath);
            Object.DestroyImmediate(instance);
        }

        private static void CreateMeshWrapper(string outputPath, string sourcePath)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null)
            {
                Debug.LogWarning($"Mesh source missing: {sourcePath}");
                return;
            }

            var root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(outputPath));
            var child = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
            if (child == null)
            {
                Object.DestroyImmediate(root);
                Debug.LogWarning($"Failed to instantiate: {sourcePath}");
                return;
            }

            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            AlignPivotToGround(root);

            SavePrefab(root, outputPath);
            Object.DestroyImmediate(root);
        }

        private static void AlignPivotToGround(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var offset = root.transform.position.y - bounds.min.y;
            root.transform.position = new Vector3(0f, offset, 0f);
        }

        private static void SavePrefab(GameObject instance, string path)
        {
            EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
            PrefabUtility.SaveAsPrefabAsset(instance, path);
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                return;

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
