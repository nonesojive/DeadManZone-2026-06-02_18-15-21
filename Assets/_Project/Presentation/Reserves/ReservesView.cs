using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Game.Dev;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.DragDrop;
using DeadManZone.Presentation.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Reserves
{
    public sealed class ReservesView : MonoBehaviour
    {
        [SerializeField] private Transform tileRoot;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private UiThemeSO theme;

        private readonly Dictionary<GridCoord, ReservesTileView> _tiles = new();
        private readonly Dictionary<string, PieceShapeVisual> _shapeVisualsByInstance = new();
        private RectTransform _piecesOverlay;
        private ContentDatabase _database;

        private void Awake() => _database = ContentDatabase.Load();

        private void OnEnable()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.RunStateChanged += OnRunStateChanged;
            EnsureGridBuilt();
            Refresh();
        }

        private void OnDisable()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.RunStateChanged -= OnRunStateChanged;
        }

        private void OnRunStateChanged(Core.Run.RunState _) => Refresh();

        public void SyncLayoutFromBoard()
        {
            SyncGridFromBoardView();
            SyncPiecesOverlay();
        }

        public void Refresh()
        {
            if (RunManager.Instance == null || !RunManager.Instance.HasActiveRun)
                return;

            EnsureGridBuilt();
            SyncGridFromBoardView();
            SyncPiecesOverlay();
            var registry = _database != null ? ContentRegistryProvider.Build(_database) : ContentRegistryProvider.Build(ContentDatabase.Load());
            if (registry == null)
                return;

            var reserves = RunManager.Instance.Orchestrator.GetReserves();
            ClearOccupancy();
            ClearChips();

            if (_piecesOverlay == null || gridLayout == null)
                EnsurePiecesOverlay();

            Canvas.ForceUpdateCanvases();

            foreach (var piece in reserves.Pieces)
            {
                foreach (var cell in piece.Definition.Shape.GetCells(piece.Anchor, piece.Rotation))
                {
                    if (_tiles.TryGetValue(cell, out var tile))
                        tile.SetOccupied(piece.InstanceId, true);
                }

                if (!_tiles.TryGetValue(piece.Anchor, out var anchorTile))
                    continue;

                var drag = anchorTile.GetComponent<ReservesPieceDragSource>();
                if (drag == null)
                    drag = anchorTile.gameObject.AddComponent<ReservesPieceDragSource>();
                drag.Configure(
                    piece.InstanceId,
                    piece.Definition.Id,
                    piece.Anchor,
                    piece.Definition,
                    piece.Rotation);

                var shapeCells = piece.Definition.Shape
                    .GetCells(piece.Anchor, piece.Rotation)
                    .ToList();

                var shapeVisual = PieceShapeVisual.Create(
                    _piecesOverlay,
                    gridLayout,
                    shapeCells,
                    piece.Definition,
                    PieceVisualLookup.GetSource(piece.Definition.Id),
                    piece.Anchor,
                    piece.Rotation,
                    ResolveCellCenterInOverlay);
                if (shapeVisual != null)
                    _shapeVisualsByInstance[piece.InstanceId] = shapeVisual;
            }
        }

        private void EnsurePiecesOverlay()
        {
            if (tileRoot == null || gridLayout == null)
                return;

            for (int i = tileRoot.childCount - 1; i >= 0; i--)
            {
                var child = tileRoot.GetChild(i);
                if (child.name == "PiecesOverlay")
                    Destroy(child.gameObject);
            }

            if (_piecesOverlay == null)
            {
                var gridRect = gridLayout.GetComponent<RectTransform>();
                var parent = gridRect.parent;

                var overlayGo = new GameObject("PiecesOverlay", typeof(RectTransform));
                overlayGo.transform.SetParent(parent, false);
                overlayGo.transform.SetSiblingIndex(gridRect.GetSiblingIndex() + 1);
                _piecesOverlay = overlayGo.GetComponent<RectTransform>();
            }

            SyncPiecesOverlay();
        }

        private void SyncPiecesOverlay()
        {
            if (_piecesOverlay == null || gridLayout == null)
                return;

            var gridRect = gridLayout.GetComponent<RectTransform>();
            _piecesOverlay.anchorMin = gridRect.anchorMin;
            _piecesOverlay.anchorMax = gridRect.anchorMax;
            _piecesOverlay.pivot = gridRect.pivot;
            _piecesOverlay.anchoredPosition = gridRect.anchoredPosition;
            _piecesOverlay.sizeDelta = gridRect.sizeDelta;
            _piecesOverlay.offsetMin = gridRect.offsetMin;
            _piecesOverlay.offsetMax = gridRect.offsetMax;
        }

        private void SyncGridFromBoardView()
        {
            if (gridLayout == null)
                return;

            var boardView = FindFirstObjectByType<BoardView>();
            var boardGrid = boardView?.GridLayout;
            if (boardGrid == null)
                return;

            gridLayout.cellSize = boardGrid.cellSize;
            gridLayout.spacing = boardGrid.spacing;
            gridLayout.padding = new RectOffset(0, 0, 0, 0);

            var cellFitter = gridLayout.GetComponent<GridLayoutCellFitter>();
            if (cellFitter != null)
                cellFitter.enabled = false;
        }

        private Vector2? ResolveCellCenterInOverlay(GridCoord cell)
        {
            if (_piecesOverlay == null || !_tiles.TryGetValue(cell, out var tileView))
                return null;

            var tileRect = tileView.GetComponent<RectTransform>();
            if (tileRect == null)
                return null;

            var world = tileRect.TransformPoint(tileRect.rect.center);
            return _piecesOverlay.InverseTransformPoint(world);
        }

        public bool TryRelocatePiece(string instanceId, GridCoord anchor, PieceRotation rotation)
        {
            if (RunManager.Instance == null)
                return false;

            var reserves = RunManager.Instance.Orchestrator.GetReserves();
            if (!reserves.TryRemove(instanceId, out var removed))
                return false;

            var place = reserves.TryPlace(removed.Definition, anchor, removed.InstanceId, rotation);
            if (!place.Success)
            {
                reserves.TryPlace(removed.Definition, removed.Anchor, removed.InstanceId, removed.Rotation);
                return false;
            }

            RunManager.Instance.Orchestrator.SaveReserves(reserves);
            Refresh();
            return true;
        }

        private void EnsureGridBuilt()
        {
            if (_tiles.Count > 0 || tileRoot == null || tilePrefab == null)
                return;

            if (gridLayout != null)
            {
                gridLayout.constraintCount = ReservesState.Width;
                var cellFitter = gridLayout.GetComponent<GridLayoutCellFitter>();
                if (cellFitter != null)
                    cellFitter.enabled = false;
            }

            for (int y = 0; y < ReservesState.Height; y++)
            {
                for (int x = 0; x < ReservesState.Width; x++)
                {
                    var coord = new GridCoord(x, y);
                    var tileObject = Instantiate(tilePrefab, tileRoot);
                    tileObject.SetActive(true);

                    var tileView = tileObject.GetComponent<ReservesTileView>();
                    if (tileView == null)
                        tileView = tileObject.AddComponent<ReservesTileView>();

                    tileView.Initialize(coord, Theme.GetReserveSlotColor());
                    if (tileObject.GetComponent<ReservesTileDropTarget>() == null)
                        tileObject.AddComponent<ReservesTileDropTarget>();
                    _tiles[coord] = tileView;
                }
            }

            EnsurePiecesOverlay();
            Canvas.ForceUpdateCanvases();
        }

        private void ClearOccupancy()
        {
            foreach (var tile in _tiles.Values)
                tile.SetOccupied(null, false);
        }

        private void ClearChips()
        {
            foreach (var visual in _shapeVisualsByInstance.Values)
            {
                if (visual != null)
                    Destroy(visual.gameObject);
            }

            _shapeVisualsByInstance.Clear();
        }

        private UiThemeSO Theme => theme != null ? theme : UiThemeProvider.Current;
    }
}
