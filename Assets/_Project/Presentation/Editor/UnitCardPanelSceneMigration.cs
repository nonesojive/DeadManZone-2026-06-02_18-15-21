using DeadManZone.Presentation.Run;
using DeadManZone.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    public static class UnitCardPanelSceneMigration
    {
        private const string RunScenePath = "Assets/_Project/Scenes/Run.unity";

        [MenuItem("DeadManZone/UI/Migrate Unit Card Panels In Open Scenes")]
        public static void MigrateOpenScenes()
        {
            int migrated = 0;
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                migrated += MigrateSceneRoots(scene.GetRootGameObjects(), saveScene: false);
                if (migrated > 0)
                    EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log(migrated > 0
                ? $"Migrated {migrated} unit card panel(s) in open scene(s). Save scenes to keep changes."
                : "No unit card panels needed migration.");
        }

        [MenuItem("DeadManZone/UI/Migrate Run Scene Unit Card Panel")]
        public static void MigrateRunScene()
        {
            if (!System.IO.File.Exists(RunScenePath))
            {
                Debug.LogError($"Run scene not found at {RunScenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(RunScenePath, OpenSceneMode.Single);
            int migrated = MigrateSceneRoots(scene.GetRootGameObjects(), saveScene: true);
            if (migrated > 0)
                Debug.Log($"Migrated {migrated} unit card panel(s) in Run scene.");
            else
                Debug.Log("Run scene unit card panel already migrated.");
        }

        private static int MigrateSceneRoots(GameObject[] roots, bool saveScene)
        {
            int migrated = 0;
            foreach (var root in roots)
            {
                foreach (var panel in root.GetComponentsInChildren<UnitCardPanelView>(true))
                {
                    if (MigratePanel(panel))
                        migrated++;
                }
            }

            LegacyUnitCardCleanup.RemoveFloatingHoverLayers();
            return migrated;
        }

        private static bool MigratePanel(UnitCardPanelView panel)
        {
            if (panel == null)
                return false;

            var host = panel.transform;
            var panelRoot = host;
            var panelRootField = typeof(UnitCardPanelView).GetField(
                "panelRoot",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (panelRootField?.GetValue(panel) is RectTransform serializedRoot && serializedRoot != null)
                host = serializedRoot;

            LegacyUnitCardCleanup.RemoveLegacyChildren(host);

            var cardView = host.GetComponentInChildren<PieceCardView>(true);
            if (cardView == null)
            {
                if (!EditorUtility.DisplayDialog(
                        "Add Unit Detail Card Instance?",
                        "This panel has no PieceCardView child. Add an instance from UnitDetailCard.prefab?\n\nThe prefab asset itself will NOT be modified. Cancel if you want to place/link your own card manually.",
                        "Add Instance",
                        "Cancel"))
                    return false;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPaths.UnitDetailCard);
                if (prefab == null)
                {
                    Debug.LogWarning(
                        $"Unit card panel '{panel.name}' has no PieceCardView and prefab is missing at {CardPrefabPaths.UnitDetailCard}.",
                        panel);
                    return false;
                }

                var cardGo = PrefabUtility.InstantiatePrefab(prefab, host) as GameObject;
                if (cardGo == null)
                {
                    Debug.LogWarning($"Failed to instantiate unit detail card prefab under '{panel.name}'.", panel);
                    return false;
                }

                cardGo.name = prefab.name;
                cardView = cardGo.GetComponent<PieceCardView>();
            }

            var serialized = new SerializedObject(panel);
            var panelRootProperty = serialized.FindProperty("panelRoot");
            if (panelRootProperty.objectReferenceValue == null)
                panelRootProperty.objectReferenceValue = host as RectTransform ?? panelRoot as RectTransform;

            serialized.FindProperty("cardView").objectReferenceValue = cardView;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }
    }
}
