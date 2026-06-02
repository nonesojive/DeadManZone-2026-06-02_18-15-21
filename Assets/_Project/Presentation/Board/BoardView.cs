using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Presentation.DragDrop;
using DeadManZone.Presentation.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    public sealed class BoardView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Transform tileRoot;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private BoardTileView tileViewTemplate;

        [SerializeField] private UiThemeSO theme;

        private readonly Dictionary<GridCoord, BoardTileView> _tiles = new();
        private readonly Dictionary<string, PieceChipView> _chipsByInstance = new();
        private BoardLayout _layout;
        private BoardState _boardState;
        private PieceDefinition _selectedPiece;
        private string _selectedPlacedInstanceId;

        public int TileCount => _tiles.Count;

        public event Action<GridCoord, PlacementResult> TilePlacementAttempted;

        public void BuildBoard(BoardLayout layout)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _boardState = new BoardState(layout);
            ClearTiles();

            if (tileRoot == null || tilePrefab == null)
                throw new InvalidOperationException("BoardView requires tileRoot and tilePrefab.");

            if (gridLayout != null)
                gridLayout.constraintCount = layout.Width;

            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    var coord = new GridCoord(x, y);
                    var tileObject = Instantiate(tilePrefab, tileRoot);
                    tileObject.SetActive(true);

                    var tileView = tileObject.GetComponent<BoardTileView>();
                    if (tileView == null && tileViewTemplate != null)
                        tileView = tileObject.AddComponent<BoardTileView>();
                    if (tileView == null)
                        throw new InvalidOperationException("Tile prefab needs BoardTileView.");

                    var zone = layout.GetZone(coord);
                    bool isSpecial = layout.IsSpecialTile(coord);
                    tileView.Initialize(coord, GetZoneColor(zone), isSpecial);
                    tileView.Clicked += OnTileClicked;
                    if (tileObject.GetComponent<BoardTileDropTarget>() == null)
                        tileObject.AddComponent<BoardTileDropTarget>();
                    if (tileObject.GetComponent<BoardTileHover>() == null)
                        tileObject.AddComponent<BoardTileHover>();
                    _tiles[coord] = tileView;
                }
            }
        }

        public bool TryPlaceFromBench(int benchIndex, GridCoord anchor)
        {
            if (RunManager.Instance == null)
                return false;

            bool placed = RunManager.Instance.TryPlaceFromBench(benchIndex, anchor);
            if (placed)
                RefreshFromRunManager();
            return placed;
        }

        public bool TryMovePlacedPiece(string instanceId, GridCoord anchor)
        {
            if (RunManager.Instance == null)
                return false;

            bool moved = RunManager.Instance.TryMovePlacedPiece(instanceId, anchor);
            if (moved)
                RefreshFromRunManager();
            return moved;
        }

        public void LoadSnapshot(BoardSnapshot snapshot, ContentRegistry registry)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            var layout = BoardLayout.CreateStandard(
                snapshot.Width,
                snapshot.Height,
                snapshot.RearRows,
                snapshot.SupportRows,
                snapshot.SpecialTiles.Select(s => new GridCoord(s.X, s.Y)).ToArray());

            BuildBoard(layout);

            foreach (var record in snapshot.Pieces)
            {
                var definition = registry.GetById(record.PieceId);
                _boardState.TryPlace(definition, new GridCoord(record.AnchorX, record.AnchorY), record.InstanceId);
            }

            RefreshOccupancyVisuals();
        }

        public void RefreshFromRunManager()
        {
            if (RunManager.Instance == null || !RunManager.Instance.HasActiveRun)
                return;

            var database = ContentDatabase.Load();
            if (database == null)
                return;

            var snapshot = RunManager.Instance.State.PlayerBoard;
            if (snapshot != null)
                LoadSnapshot(snapshot, database.BuildRegistry());
        }

        public void SelectPieceForPlacement(PieceDefinition definition) => _selectedPiece = definition;

        public void SelectPlacedPiece(string instanceId) => _selectedPlacedInstanceId = instanceId;

        public PlacementResult TryPlaceSelectedPiece(GridCoord anchor)
        {
            if (_boardState == null)
                return new PlacementResult { Success = false, Reason = "Board not initialized" };

            if (_selectedPiece == null)
                return new PlacementResult { Success = false, Reason = "No selected piece" };

            var result = _boardState.TryPlace(_selectedPiece, anchor);
            ApplyPlacementHighlight(anchor, !result.Success);
            TilePlacementAttempted?.Invoke(anchor, result);

            if (result.Success)
            {
                RunManager.Instance.Orchestrator.SavePlayerBoard(_boardState);
                RefreshOccupancyVisuals();
                _selectedPiece = null;
            }

            return result;
        }

        public BoardTileView GetTile(GridCoord coord) => _tiles.TryGetValue(coord, out var tile) ? tile : null;

        public bool TrySellSelectedPiece()
        {
            if (string.IsNullOrWhiteSpace(_selectedPlacedInstanceId) || RunManager.Instance == null)
                return false;

            bool sold = RunManager.Instance.TrySellPlacedPiece(_selectedPlacedInstanceId);
            if (!sold)
                return false;

            var state = RunManager.Instance.State;
            RefreshFromRunManager();
            _selectedPlacedInstanceId = null;
            return true;
        }

        private void OnTileClicked(GridCoord coord)
        {
            if (_selectedPiece != null)
            {
                TryPlaceSelectedPiece(coord);
                return;
            }

            var tile = GetTile(coord);
            if (tile != null && !string.IsNullOrEmpty(tile.OccupyingInstanceId))
                SelectPlacedPiece(tile.OccupyingInstanceId);
        }

        public void SetPlacementPreview(GridCoord anchor, bool invalid)
        {
            ClearPlacementPreviews();
            if (_tiles.TryGetValue(anchor, out var tile))
                tile.SetInvalidPreview(invalid);
        }

        public void ClearPlacementPreviews()
        {
            foreach (var tile in _tiles.Values)
                tile.SetInvalidPreview(false);
        }

        private void RefreshOccupancyVisuals()
        {
            if (_layout == null)
                return;

            ClearPieceChips();

            foreach (var pair in _tiles)
            {
                var coord = pair.Key;
                var tile = pair.Value;
                var zone = _layout.GetZone(coord);
                tile.Initialize(coord, GetZoneColor(zone), _layout.IsSpecialTile(coord));
                tile.SetOccupied(null, false);
            }

            if (_boardState == null)
                return;

            foreach (var piece in _boardState.Pieces)
            {
                foreach (var cell in piece.Definition.Shape.GetCells(piece.Anchor))
                {
                    if (_tiles.TryGetValue(cell, out var tile))
                        tile.SetOccupied(piece.InstanceId, true);
                }

                if (_tiles.TryGetValue(piece.Anchor, out var anchorTile))
                {
                    var drag = anchorTile.GetComponent<BoardPieceDragSource>();
                    if (drag == null)
                        drag = anchorTile.gameObject.AddComponent<BoardPieceDragSource>();
                    drag.Configure(piece.InstanceId, piece.Definition.Id, piece.Anchor, piece.Definition);

                    var source = PieceVisualLookup.GetSource(piece.Definition.Id);
                    var chip = PieceChipView.Create(anchorTile.transform, piece.Definition, source);
                    _chipsByInstance[piece.InstanceId] = chip;
                }
            }
        }

        private void ClearPieceChips()
        {
            foreach (var chip in _chipsByInstance.Values)
            {
                if (chip != null)
                    Destroy(chip.gameObject);
            }

            _chipsByInstance.Clear();
        }

        public void InitializeForTests(
            Transform testTileRoot,
            GridLayoutGroup testGrid,
            GameObject testTilePrefab,
            BoardTileView _)
        {
            tileRoot = testTileRoot;
            gridLayout = testGrid;
            tilePrefab = testTilePrefab;
        }

        private void ApplyPlacementHighlight(GridCoord anchor, bool invalid)
        {
            if (_layout == null || !_tiles.TryGetValue(anchor, out var tile))
                return;

            var zoneColor = GetZoneColor(_layout.GetZone(anchor));
            tile.SetOverlay(zoneColor, _layout.IsSpecialTile(anchor), invalid);
        }

        private UiThemeSO Theme => theme != null ? theme : UiThemeProvider.Current;

        private Color GetZoneColor(ZoneType zone) => Theme.GetZoneColor(zone);

        private void ClearTiles()
        {
            if (tileRoot == null)
                return;

            foreach (Transform child in tileRoot)
                Destroy(child.gameObject);
            _tiles.Clear();
        }
    }
}
