#if UNITY_EDITOR
using DeadManZone.Core.Board;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Reserves;
using DeadManZone.Presentation.Run;
using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// One-shot editor setup: HQ (left), combat (right), reserves (bottom) inside BoardArea.
    /// Layout is anchor-driven RectTransforms so artists can tweak in the Inspector.
    /// </summary>
    public static class BoardAreaTripleLayoutSetup
    {
        private const string MenuPath = "DeadManZone/Run/Setup BoardArea Triple Layout";

        private const int CombatColumns = 6;
        private const int CombatRows = 6;
        private const int HqColumns = 3;
        private const int HqRows = 6;

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var boardAreaGo = GameObject.Find("Canvas/RunScene/ShopScene/MainRow/BoardArea");
            if (boardAreaGo == null)
            {
                Debug.LogError("BoardArea not found at Canvas/RunScene/ShopScene/MainRow/BoardArea.");
                return;
            }

            var boardArea = boardAreaGo.transform;
            var combatTemplate = ExtractCombatBoardTemplate(boardArea);
            if (combatTemplate == null)
            {
                Debug.LogError("No combat BoardView found under BoardArea — cannot set up triple layout.");
                return;
            }

            ClearBoardAreaChildren(boardArea);

            var hqSection = CreateSection(boardArea, "HqBoardSection",
                new Vector2(0.02f, 0.30f), new Vector2(0.46f, 0.98f)).transform;
            var combatSection = CreateSection(boardArea, "CombatBoardSection",
                new Vector2(0.54f, 0.30f), new Vector2(0.98f, 0.98f)).transform;
            var reservesSection = CreateSection(boardArea, "ReservesSection",
                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.26f)).transform;

            var combatBoard = PlaceBoard(combatSection, "CombatBoard", combatTemplate, BoardKind.Combat, CombatColumns, CombatRows);
            var hqBoard = PlaceBoard(hqSection, "HqBoard", combatBoard, BoardKind.Hq, HqColumns, HqRows);
            Object.DestroyImmediate(combatTemplate);
            var reservesView = EnsureReservesRegion(reservesSection);

            WireSceneReferences(boardAreaGo, combatBoard, hqBoard, reservesView);

            var shopScene = boardArea.parent?.parent;
            if (shopScene != null)
                RunUiAuthoringLock.EnsureOn(shopScene);

            EditorSceneManager.MarkSceneDirty(boardAreaGo.scene);
            Debug.Log("BoardArea triple layout ready. Tweak HqBoardSection, CombatBoardSection, and ReservesSection anchors in the Inspector.");
        }

        private static GameObject ExtractCombatBoardTemplate(Transform boardArea)
        {
            BoardView combatView = null;
            foreach (var view in boardArea.GetComponentsInChildren<BoardView>(true))
            {
                if (view.BoardBinding == BoardKind.Combat)
                {
                    combatView = view;
                    break;
                }
            }

            if (combatView == null)
                combatView = boardArea.GetComponent<BoardView>();

            if (combatView == null)
                return null;

            var template = combatView.gameObject;
            template.transform.SetParent(null, false);
            template.hideFlags = HideFlags.HideInHierarchy;
            StripNestedLayoutSections(template.transform);
            return template;
        }

        private static void StripNestedLayoutSections(Transform boardRoot)
        {
            for (int i = boardRoot.childCount - 1; i >= 0; i--)
            {
                var child = boardRoot.GetChild(i);
                if (child.name is "HqBoardSection" or "CombatBoardSection" or "ReservesSection")
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void ClearBoardAreaChildren(Transform boardArea)
        {
            Object.DestroyImmediate(boardArea.GetComponent<BoardView>());

            for (int i = boardArea.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(boardArea.GetChild(i).gameObject);
        }

        private static GameObject PlaceBoard(
            Transform section,
            string boardName,
            GameObject template,
            BoardKind binding,
            int columns,
            int rows)
        {
            var boardGo = Object.Instantiate(template, section);
            boardGo.name = boardName;
            boardGo.hideFlags = HideFlags.None;
            StretchAnchors(boardGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 6f);
            ConfigureBoard(boardGo, binding, columns, rows);
            return boardGo;
        }

        private static GameObject CreateSection(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            StretchAnchors(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
            return go;
        }

        private static void ConfigureBoard(GameObject boardGo, BoardKind binding, int columns, int rows)
        {
            var view = boardGo.GetComponent<BoardView>();
            if (view != null)
                view.SetBoardBinding(binding);

            var grid = boardGo.GetComponentInChildren<GridLayoutGroup>(true);
            if (grid != null)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = columns;

                for (int i = grid.transform.childCount - 1; i >= 0; i--)
                {
                    var child = grid.transform.GetChild(i);
                    if (child.GetComponent<BoardTileView>() != null)
                        Object.DestroyImmediate(child.gameObject);
                }
            }

            var fitter = grid != null ? grid.GetComponent<GridLayoutCellFitter>() : null;
            if (fitter == null && grid != null)
                fitter = grid.gameObject.AddComponent<GridLayoutCellFitter>();
            if (fitter != null)
                fitter.Configure(columns, rows);

            var gridRect = grid != null ? grid.GetComponent<RectTransform>() : null;
            if (gridRect != null)
                StretchAnchors(gridRect, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));
        }

        private static ReservesView EnsureReservesRegion(Transform reservesSection)
        {
            var existing = reservesSection.Find("ReservesRegion");
            if (existing != null && existing.TryGetComponent<ReservesView>(out var existingView))
            {
                StretchAnchors(existing as RectTransform, Vector2.zero, Vector2.one, 4f);
                ConfigureReservesGrid(existing);
                return existingView;
            }

            var theme = UiThemeProvider.Current;
            var reservesRegion = CreateSection(reservesSection, "ReservesRegion", Vector2.zero, Vector2.one).transform;
            StretchAnchors(reservesRegion as RectTransform, Vector2.zero, Vector2.one, 4f);

            var gridRoot = new GameObject("ReservesGrid", typeof(RectTransform));
            gridRoot.transform.SetParent(reservesRegion, false);
            var gridRect = gridRoot.GetComponent<RectTransform>();
            StretchAnchors(gridRect, new Vector2(0.08f, 0.08f), new Vector2(0.98f, 0.92f));

            var grid = gridRoot.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = ReservesState.Width;
            grid.cellSize = new Vector2(44f, 44f);
            grid.spacing = new Vector2(3f, 3f);
            grid.padding = new RectOffset(4, 4, 4, 4);

            var fitter = gridRoot.AddComponent<GridLayoutCellFitter>();
            fitter.Configure(ReservesState.Width, ReservesState.Height);

            ReservesLabelStripFactory.Ensure(reservesRegion, theme);

            var tilePrefab = CreateReservesTilePrefab(theme);
            tilePrefab.transform.SetParent(reservesRegion, false);
            tilePrefab.SetActive(false);

            var reservesView = reservesRegion.gameObject.AddComponent<ReservesView>();
            var serialized = new SerializedObject(reservesView);
            serialized.FindProperty("tileRoot").objectReferenceValue = gridRoot.transform;
            serialized.FindProperty("gridLayout").objectReferenceValue = grid;
            serialized.FindProperty("tilePrefab").objectReferenceValue = tilePrefab;
            serialized.FindProperty("theme").objectReferenceValue = theme;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return reservesView;
        }

        private static void ConfigureReservesGrid(Transform reservesRegion)
        {
            var grid = reservesRegion.Find("ReservesGrid")?.GetComponent<GridLayoutGroup>();
            if (grid == null)
                return;

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = ReservesState.Width;

            var fitter = grid.GetComponent<GridLayoutCellFitter>();
            if (fitter != null)
                fitter.Configure(ReservesState.Width, ReservesState.Height);
        }

        private static GameObject CreateReservesTilePrefab(UiThemeSO theme)
        {
            var tile = new GameObject("ReservesTilePrefab", typeof(RectTransform));
            var image = tile.AddComponent<Image>();
            UiThemeApplicator.ApplyStorageSlotEmpty(image, theme);
            tile.AddComponent<ReservesTileView>();
            var serialized = new SerializedObject(tile.GetComponent<ReservesTileView>());
            serialized.FindProperty("baseImage").objectReferenceValue = image;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return tile;
        }

        private static void WireSceneReferences(
            GameObject boardAreaGo,
            GameObject combatBoard,
            GameObject hqBoard,
            ReservesView reservesView)
        {
            var combatView = combatBoard != null ? combatBoard.GetComponent<BoardView>() : null;
            var hqView = hqBoard != null ? hqBoard.GetComponent<BoardView>() : null;

            var row = boardAreaGo.transform.parent;
            var rowFitter = row != null ? row.GetComponent<BuildRowLayoutFitter>() : null;
            if (rowFitter != null && combatView != null)
            {
                var rowSo = new SerializedObject(rowFitter);
                rowSo.FindProperty("boardView").objectReferenceValue = combatView;
                rowSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var shop = boardAreaGo.transform.parent?.parent;
            var reservesFitter = shop != null ? shop.GetComponent<ReservesLayoutFitter>() : null;
            if (reservesFitter != null && reservesView != null)
            {
                var rfSo = new SerializedObject(reservesFitter);
                rfSo.FindProperty("reservesRegion").objectReferenceValue = reservesView.transform as RectTransform;
                rfSo.FindProperty("boardView").objectReferenceValue = combatView;
                rfSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var controller = Object.FindFirstObjectByType<RunSceneController>(FindObjectsInactive.Include);
            if (controller != null)
            {
                var cso = new SerializedObject(controller);
                cso.FindProperty("boardArea").objectReferenceValue = boardAreaGo.GetComponent<RectTransform>();
                cso.FindProperty("boardView").objectReferenceValue = combatView;
                cso.FindProperty("hqBoardView").objectReferenceValue = hqView;
                cso.FindProperty("reservesView").objectReferenceValue = reservesView;
                cso.ApplyModifiedPropertiesWithoutUndo();
            }

            var bootstrap = shop != null ? shop.GetComponent<RunBuildUiBootstrap>() : null;
            if (bootstrap != null && combatView != null)
            {
                var bso = new SerializedObject(bootstrap);
                bso.FindProperty("boardView").objectReferenceValue = combatView;
                bso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void StretchAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, float inset = 0f)
        {
            if (rect == null)
                return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
#endif
