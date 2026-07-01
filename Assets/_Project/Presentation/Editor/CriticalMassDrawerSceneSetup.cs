using DeadManZone.Presentation.Run;
using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadManZone.Presentation.Editor
{
    public static class CriticalMassDrawerSceneSetup
    {
        private const string MenuPath = "DeadManZone/Setup/Bake Critical Mass Drawer";

        [MenuItem(MenuPath)]
        public static void BakeInActiveScene()
        {
            var shopScene = FindShopSceneRoot();
            if (shopScene == null)
            {
                EditorUtility.DisplayDialog(
                    "Critical Mass Drawer",
                    "Could not find ShopScene in the open scene. Open Run.unity first.",
                    "OK");
                return;
            }

            var existing = shopScene.Find(CriticalMassDrawerBootstrap.DrawerName);
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog(
                        "Critical Mass Drawer",
                        "CriticalMassDrawer already exists under ShopScene. Replace it?",
                        "Replace",
                        "Cancel"))
                    return;

                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            UiThemeSceneStyling.LoadTheme();
            var drawer = CriticalMassDrawerBootstrap.CreateDrawer(shopScene);
            var theme = UiThemeSceneStyling.LoadTheme();
            var tabButton = drawer.transform.Find("Tab")?.GetComponent<UnityEngine.UI.Button>();
            if (tabButton != null)
                UiThemeSceneStyling.StyleButton(tabButton, theme, accent: true);
            Undo.RegisterCreatedObjectUndo(drawer.gameObject, "Bake Critical Mass Drawer");
            drawer.transform.SetAsLastSibling();

            var hud = shopScene.GetComponent<BuildScreenHudController>();
            if (hud != null)
            {
                var serialized = new SerializedObject(hud);
                serialized.FindProperty("criticalMassDrawer").objectReferenceValue = drawer;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = drawer.gameObject;
            Debug.Log($"Baked {CriticalMassDrawerBootstrap.DrawerName} under ShopScene. Style it in the Hierarchy, then save the scene.");
        }

        [MenuItem(MenuPath, true)]
        private static bool BakeInActiveSceneValidate() => !Application.isPlaying;

        private static Transform FindShopSceneRoot()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var found = FindByName(root.transform, "ShopScene");
                if (found != null)
                    return found;
            }

            return null;
        }

        private static Transform FindByName(Transform root, string name)
        {
            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var match = FindByName(root.GetChild(i), name);
                if (match != null)
                    return match;
            }

            return null;
        }
    }
}
