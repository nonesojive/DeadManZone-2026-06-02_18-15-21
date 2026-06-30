#if UNITY_EDITOR
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Reserves;
using DeadManZone.Presentation.Run;
using DeadManZone.Presentation.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>One-shot migration: wire ShopScene as the run UI root and remove BuildPanel.</summary>
    public static class ShopSceneMigrationTool
    {
        private const string MenuPath = "DeadManZone/Run/Wire ShopScene And Remove BuildPanel";

        [MenuItem(MenuPath)]
        public static void Migrate()
        {
            var build = GameObject.Find("Canvas/RunScene/BuildPanel");
            var shop = GameObject.Find("Canvas/RunScene/ShopScene");
            if (build == null)
            {
                Debug.LogError("BuildPanel not found — already migrated?");
                return;
            }

            if (shop == null)
            {
                Debug.LogError("ShopScene not found.");
                return;
            }

            var buildBoard = build.transform.Find("MainRow/BoardArea");
            var shopBoard = shop.transform.Find("MainRow/BoardArea");
            var buildShop = build.transform.Find("MainRow/ShopArea");
            var shopShop = shop.transform.Find("MainRow/ShopArea");
            if (buildBoard == null || shopBoard == null || buildShop == null || shopShop == null)
            {
                Debug.LogError("BoardArea or ShopArea missing on one of the panels.");
                return;
            }

            CopyRemapped(buildBoard.GetComponent<BoardView>(), shopBoard.gameObject, buildBoard, shopBoard);
            CopyRemapped(buildShop.GetComponent<ShopView>(), shopShop.gameObject, buildShop, shopShop);

            var shopBoardView = shopBoard.GetComponent<BoardView>();
            var shopShopView = shopShop.GetComponent<ShopView>();

            var row = shop.transform.Find("MainRow");
            var rowFitter = row != null ? row.GetComponent<BuildRowLayoutFitter>() : null;
            if (rowFitter != null)
            {
                var rowSo = new SerializedObject(rowFitter);
                rowSo.FindProperty("boardView").objectReferenceValue = shopBoardView;
                rowSo.ApplyModifiedPropertiesWithoutUndo();
            }

            if (shopShopView != null)
            {
                var reroll = shop.transform.Find("BottomBar/REROLLButton")?.GetComponent<Button>();
                var shopSo = new SerializedObject(shopShopView);
                shopSo.FindProperty("rerollButton").objectReferenceValue = reroll;
                shopSo.ApplyModifiedPropertiesWithoutUndo();
            }

            RunUiAuthoringLock.EnsureOn(shop.transform);

            var controller = Object.FindFirstObjectByType<RunSceneController>(FindObjectsInactive.Include);
            if (controller != null)
            {
                var cso = new SerializedObject(controller);
                cso.FindProperty("shopScene").objectReferenceValue = shop;
                cso.FindProperty("shopSceneCanvasGroup").objectReferenceValue = shop.GetComponent<CanvasGroup>();
                cso.FindProperty("boardArea").objectReferenceValue = shopBoard;
                cso.FindProperty("shopArea").objectReferenceValue = shopShop.gameObject;
                cso.FindProperty("bottomBar").objectReferenceValue = shop.transform.Find("BottomBar")?.gameObject;
                cso.FindProperty("mainRowLayout").objectReferenceValue = rowFitter;
                cso.FindProperty("boardView").objectReferenceValue = shopBoardView;
                cso.FindProperty("shopView").objectReferenceValue = shopShopView;
                cso.FindProperty("reservesView").objectReferenceValue =
                    shop.transform.Find("BottomBar/ReservesRegion")?.GetComponent<ReservesView>();
                cso.FindProperty("runHudView").objectReferenceValue =
                    shop.transform.Find("TopBar")?.GetComponent<RunHudView>();
                cso.FindProperty("pauseMenuView").objectReferenceValue =
                    shop.transform.Find("PauseMenu")?.GetComponent<PauseMenuView>();
                cso.FindProperty("runEndOverlay").objectReferenceValue =
                    shop.transform.Find("RunEndOverlay")?.GetComponent<RunEndOverlayView>();
                cso.FindProperty("beginFightButton").objectReferenceValue =
                    shop.transform.Find("BottomBar/COMBATButton")?.GetComponent<Button>();
                cso.FindProperty("menuButton").objectReferenceValue =
                    shop.transform.Find("TopBar/TopInfoPanel/MENUButton")?.GetComponent<Button>();
                cso.ApplyModifiedPropertiesWithoutUndo();
            }

            var lastLogBtn = shop.transform.Find("TopBar/TopInfoPanel/Last LogButton")
                ?? shop.transform.Find("TopBar/Last LogButton");
            if (lastLogBtn != null)
                lastLogBtn.gameObject.SetActive(false);

            Object.DestroyImmediate(build);
            EditorUtility.SetDirty(shop);
            if (controller != null)
                EditorUtility.SetDirty(controller.gameObject);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("ShopScene wired; BuildPanel removed.");
        }

        private static void CopyRemapped(Component src, GameObject dstGo, Transform srcRoot, Transform dstRoot)
        {
            if (src == null || dstGo == null)
                return;

            var existing = dstGo.GetComponent(src.GetType());
            if (existing == null)
            {
                ComponentUtility.CopyComponent(src);
                ComponentUtility.PasteComponentAsNew(dstGo);
                existing = dstGo.GetComponent(src.GetType());
            }

            var srcSo = new SerializedObject(src);
            var dstSo = new SerializedObject(existing);
            var prop = srcSo.GetIterator();
            var enter = true;
            while (prop.NextVisible(enter))
            {
                enter = false;
                if (prop.propertyType != SerializedPropertyType.ObjectReference || prop.objectReferenceValue == null)
                    continue;

                var mapped = Remap(prop.objectReferenceValue, srcRoot, dstRoot);
                var dstProp = dstSo.FindProperty(prop.propertyPath);
                if (dstProp != null)
                    dstProp.objectReferenceValue = mapped;
            }

            dstSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Object Remap(Object obj, Transform srcRoot, Transform dstRoot)
        {
            if (obj == null)
                return null;

            Transform srcTransform = null;
            if (obj is Component component)
                srcTransform = component.transform;
            else if (obj is GameObject go)
                srcTransform = go.transform;
            else
                return obj;

            var path = RelativePath(srcTransform, srcRoot);
            if (path == null)
                return obj;

            var dstTransform = dstRoot.Find(path);
            if (dstTransform == null)
                return null;

            if (obj is Component)
            {
                var mapped = dstTransform.GetComponent(obj.GetType());
                return mapped != null ? mapped : obj;
            }

            return dstTransform.gameObject;
        }

        private static string RelativePath(Transform target, Transform root)
        {
            if (target == null || root == null)
                return null;

            var parts = new System.Collections.Generic.List<string>();
            var current = target;
            while (current != null && current != root)
            {
                parts.Insert(0, current.name);
                current = current.parent;
            }

            return current == root ? string.Join("/", parts) : null;
        }
    }
}
#endif
