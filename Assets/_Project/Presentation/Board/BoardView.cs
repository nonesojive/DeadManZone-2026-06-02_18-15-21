using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Game.Dev;
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
        [SerializeField] private BoardTerrainArtSO terrainArt;
        [SerializeField] private BoardBattlefieldBackdrop battlefieldBackdrop;
        [SerializeField] private BoardGridOverlay gridOverlay;
        [SerializeField] private PieceHoverCardController pieceHoverCardController;

        [SerializeField] private BoardKind boardBinding = BoardKind.Combat;

        private readonly Dictionary<GridCoord, BoardTileView> _tiles = new();
        private readonly Dictionary<string, PieceShapeVisual> _shapeVisualsByInstance = new();
        private RectTransform _piecesOverlay;
        private BoardSynergyOverlay _synergyOverlay;
        private PieceAbilityEngine.FightStartSynergySnapshot _lastSynergySnapshot;
        private BoardLayout _layout;
        private BoardState _boardState;
private PieceDefinition _selectedPiece;
        private string _selectedPlacedInstanceId;

        public int TileCount => _tiles.Count;

        public Vector2 CellSize => gridLayout != null ? gridLayout.cellSize : Vector2.one * 48f;

        public Vector2 CellSpacing => gridLayout != null ? gridLayout.spacing : Vector2.one * 3f;

        public GridLayoutGroup GridLayout => gridLayout;

        public BoardKind BoardBinding => boardBinding;

        public void SetBoardBinding(BoardKind kind)
        {
            boardBinding = kind;
            if (_layout != null && _layout.Kind != kind)
                RefreshFromRunManager();
        }

        public BoardState GetBoardState() => _boardState;

        public void SyncLayoutFromBoard()
        {
            if (gridLayout == null)
                return;

            SyncPiecesOverlay();
            Canvas.ForceUpdateCanvases();
            if (_layout != null && _layout.UsesZones)
                GetComponent<BoardZoneStripLayout>()?.ApplyLayout();
            else
            {
                var zoneStrip = GetComponent<BoardZoneStripLayout>();
                if (zoneStrip != null)
                    zoneStrip.enabled = false;
            }

            SyncBattlefieldPresentation();

            if (_boardState != null)
                RefreshOccupancyVisuals();
        }

        private void Awake()
        {
            if (boardBinding == BoardKind.Combat)
                BoardZoneStripBootstrap.Ensure(this);
            RemoveLegacySynergySidePanel(transform);
            EnsureBattlefieldPresentation();
            ResolvePieceHoverCardController();
        }

        public event Action<GridCoord, PlacementResult> TilePlacementAttempted;

        public void BuildBoard(BoardLayout layout)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _boardState = new BoardState(layout);
            ClearTiles();

            if (tileRoot == null || tilePrefab == null)
                throw new InvalidOperationException("BoardView requires tileRoot and tilePrefab.");

            if (gridLayout != null)
            {
                gridLayout.constraintCount = layout.Width;
                var fitter = gridLayout.GetComponent<GridLayoutCellFitter>();
                if (fitter == null)
                    fitter = gridLayout.gameObject.AddComponent<GridLayoutCellFitter>();
                fitter.Configure(layout.Width, layout.Height);
            }

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
                    InitializeTile(tileView, coord, isSpecial);
                    tileView.Clicked += OnTileClicked;
                    if (tileObject.GetComponent<BoardTileDropTarget>() == null)
                        tileObject.AddComponent<BoardTileDropTarget>();
                    if (tileObject.GetComponent<BoardTileHover>() == null)
                        tileObject.AddComponent<BoardTileHover>();
                    _tiles[coord] = tileView;
                }
            }

            EnsurePiecesOverlay();
            Canvas.ForceUpdateCanvases();
            GetComponent<BoardZoneStripLayout>()?.ApplyLayout();
            SyncBattlefieldPresentation();
        }

        public bool TryPlaceFromReserves(string instanceId, GridCoord anchor, PieceRotation rotation)
        {
            if (RunManager.Instance == null)
                return false;

            bool placed = RunManager.Instance.TryPlaceFromReserves(instanceId, anchor, rotation);
            if (placed)
                RefreshFromRunManager();
            return placed;
        }

        public bool TryAcquireOfferToBoard(string offerId, GridCoord anchor, PieceRotation rotation)
        {
            if (RunManager.Instance == null)
                return false;

            bool placed = RunManager.Instance.TryAcquireOfferToBoard(offerId, anchor, rotation);
            if (placed)
                RefreshFromRunManager();
            return placed;
        }

        public bool TryMovePlacedPiece(string instanceId, GridCoord anchor, PieceRotation rotation)
        {
            if (RunManager.Instance == null)
                return false;

            bool moved = RunManager.Instance.TryMovePlacedPiece(instanceId, anchor, rotation);
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

            var specialTiles = snapshot.SpecialTiles.Select(s => new GridCoord(s.X, s.Y)).ToArray();
            var layout = snapshot.RearCols > 0 || snapshot.SupportCols > 0
                ? BoardLayout.CreateHorizontalZones(
                    snapshot.Width,
                    snapshot.Height,
                    snapshot.RearCols,
                    snapshot.SupportCols,
                    specialTiles)
                : BoardLayout.CreateStandard(
                    snapshot.Width,
                    snapshot.Height,
                    snapshot.RearRows,
                    snapshot.SupportRows,
                    specialTiles);

            BuildBoard(layout);

            foreach (var record in snapshot.Pieces)
            {
                var definition = registry.GetById(record.PieceId);
                var rotation = RotationFromDegrees(record.RotationDegrees);
                _boardState.TryPlace(
                    definition,
                    new GridCoord(record.AnchorX, record.AnchorY),
                    record.InstanceId,
                    rotation);
            }
        }

        public void RefreshFromRunManager()
        {
            if (RunManager.Instance == null || !RunManager.Instance.HasActiveRun)
                return;

            var database = ContentDatabase.Load();
            if (database == null)
                return;

            var state = RunManager.Instance.State;
            if (state.Phase == RunPhase.Combat && state.Combat?.EnemyBoard != null)
            {
                RefreshCombatFromRunManager(database);
                return;
            }

            var snapshot = boardBinding == BoardKind.Hq ? state.HqBoard : state.CombatBoard;
            if (snapshot != null)
                LoadSnapshot(snapshot, ContentRegistryProvider.Build(database));
        }

        public void RefreshCombatFromRunManager(ContentDatabase database = null)
        {
            database ??= ContentDatabase.Load();
            if (database == null || RunManager.Instance == null)
                return;

            var state = RunManager.Instance.State;
            if (state?.Combat?.EnemyBoard == null || state.CombatBoard == null)
                return;

            var registry = ContentRegistryProvider.Build(database);
            var playerBoard = BoardSnapshotMapper.ToBoard(state.CombatBoard, registry);
            var enemyBoard = BoardSnapshotMapper.ToBoard(state.Combat.EnemyBoard, registry);
            var battlefield = BattlefieldState.FromBoards(playerBoard, enemyBoard);

            var layout = BoardLayout.CreateFromZoneMap(
                battlefield.Layout.TotalWidth,
                battlefield.Layout.Height,
                BattlefieldZoneMap.Create(battlefield.Layout, playerBoard.Layout));

            BuildBoard(layout);
            _boardState = new BoardState(layout);

            foreach (var cell in battlefield.Cells)
            {
                var piece = playerBoard.Pieces.FirstOrDefault(p => p.InstanceId == cell.InstanceId)
                    ?? enemyBoard.Pieces.FirstOrDefault(p => p.InstanceId == cell.InstanceId);
                if (piece != null)
                    _boardState.TryPlace(piece.Definition, cell.Position, piece.InstanceId);
            }

            RefreshOccupancyVisuals();
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
                PersistBoundBoard();
                RefreshOccupancyVisuals();
                _selectedPiece = null;
            }

            return result;
        }

        private void PersistBoundBoard()
        {
            var orchestrator = RunManager.Instance?.Orchestrator;
            if (orchestrator == null || _boardState == null)
                return;

            if (boardBinding == BoardKind.Hq)
                orchestrator.SaveHqBoard(_boardState);
            else
                orchestrator.SaveCombatBoard(_boardState);
        }

        public BoardTileView GetTile(GridCoord coord) => _tiles.TryGetValue(coord, out var tile) ? tile : null;

        public PieceAbilityEngine.SynergyResult? GetSynergyForInstance(string instanceId)
        {
            if (_lastSynergySnapshot != null && _lastSynergySnapshot.TryGet(instanceId, out var result))
                return result;
            return null;
        }

        public PieceAbilityEngine.FightStartSynergySnapshot GetSynergySnapshot() => _lastSynergySnapshot;

        public void RefreshZoneColors()
{
            if (_layout == null)
                return;

            foreach (var pair in _tiles)
            {
                var coord = pair.Key;
                var tile = pair.Value;
                if (tile == null)
                    continue;

                var zone = _layout.GetZone(coord);
                var color = GetZoneColor(zone);
                tile.SetBaseColor(color);
                tile.SetOverlay(color, _layout.IsSpecialTile(coord), false);
            }
        }

        private void MarkFootprint(
            string instanceId,
            PieceDefinition definition,
            GridCoord anchor,
            PieceRotation rotation)
        {
            foreach (var cell in definition.Shape.GetCells(anchor, rotation))
            {
                if (_tiles.TryGetValue(cell, out var tile))
                    tile.SetOccupied(instanceId, true);
            }
        }

        private PieceShapeVisual CreateShapeVisual(
            string instanceId,
            PieceDefinition definition,
            GridCoord anchor,
            PieceRotation rotation)
        {
            var shapeCells = definition.Shape.GetCells(anchor, rotation).ToList();
            var source = PieceVisualLookup.GetSource(definition.Id);
            var shapeVisual = PieceShapeVisual.Create(
                _piecesOverlay,
                gridLayout,
                shapeCells,
                definition,
                source,
                anchor,
                rotation,
                ResolveCellCenterInOverlay);
            if (shapeVisual == null)
                return null;

            _shapeVisualsByInstance[instanceId] = shapeVisual;
            return shapeVisual;
        }

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
                InitializeTile(tile, coord, _layout.IsSpecialTile(coord));
                tile.SetOccupied(null, false);
                var dragSource = tile.GetComponent<BoardPieceDragSource>();
                if (dragSource != null)
                    Destroy(dragSource);
            }

            if (_boardState == null)
                return;

            if (_piecesOverlay == null || gridLayout == null)
                EnsurePiecesOverlay();

            SyncPiecesOverlay();
            Canvas.ForceUpdateCanvases();
            if (gridLayout != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridLayout.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();
            
            _lastSynergySnapshot = PieceAbilityEngine.EvaluateFightStart(_boardState);
            var hoverController = ResolvePieceHoverCardController();

            foreach (var piece in _boardState.Pieces)
            {
                foreach (var cell in piece.Definition.Shape.GetCells(piece.Anchor, piece.Rotation))
                {
                    if (_tiles.TryGetValue(cell, out var tile))
                        tile.SetOccupied(piece.InstanceId, true);
                }

                var shapeVisual = CreateShapeVisual(
                    piece.InstanceId,
                    piece.Definition,
                    piece.Anchor,
                    piece.Rotation);
                if (shapeVisual == null)
                    continue;

                var footprintHit = shapeVisual.GetComponent<BoardPieceFootprintHit>();
                if (footprintHit == null)
                    footprintHit = shapeVisual.gameObject.AddComponent<BoardPieceFootprintHit>();
                footprintHit.Configure(
                    piece.InstanceId,
                    piece.Definition,
                    piece.Anchor,
                    piece.Rotation,
                    hoverController,
                    this);
            }

            if (_synergyOverlay != null)
                _synergyOverlay.RefreshLinks(_boardState, _shapeVisualsByInstance);

            hoverController?.Hide();
}

        public void HidePieceHoverCard() => pieceHoverCardController?.Hide();

        private void EnsurePiecesOverlay()
        {
            if (tileRoot == null || gridLayout == null)
                return;

            RemoveStrayOverlayChildrenFromGrid();
            RemoveLegacySynergySidePanel(transform);

            if (_piecesOverlay == null)
            {
                var gridRect = gridLayout.GetComponent<RectTransform>();
                var parent = gridRect.parent;

                var overlayGo = new GameObject("PiecesOverlay", typeof(RectTransform));
                overlayGo.transform.SetParent(parent, false);
                overlayGo.transform.SetSiblingIndex(gridRect.GetSiblingIndex() + 1);
                _piecesOverlay = overlayGo.GetComponent<RectTransform>();
                _synergyOverlay = overlayGo.AddComponent<BoardSynergyOverlay>();
            }

            SyncPiecesOverlay();
        }

        private static void RemoveLegacySynergySidePanel(Transform searchRoot)
        {
            if (searchRoot == null)
                return;

            foreach (Transform child in searchRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != "SynergySidePanel")
                    continue;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                else
#endif
                    UnityEngine.Object.Destroy(child.gameObject);
            }
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

        private void RemoveStrayOverlayChildrenFromGrid()
        {
            if (tileRoot == null)
                return;

            for (int i = tileRoot.childCount - 1; i >= 0; i--)
            {
                var child = tileRoot.GetChild(i);
                if (child.name == "PiecesOverlay")
                    Destroy(child.gameObject);
            }
        }

        private void ClearPieceChips()
        {
            pieceHoverCardController?.Hide();

            foreach (var visual in _shapeVisualsByInstance.Values)
            {
                if (visual != null)
                    Destroy(visual.gameObject);
            }

            _shapeVisualsByInstance.Clear();
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

        private PieceHoverCardController ResolvePieceHoverCardController()
        {
            if (pieceHoverCardController != null)
                return pieceHoverCardController;

            pieceHoverCardController = GetComponent<PieceHoverCardController>();
            if (pieceHoverCardController == null)
                pieceHoverCardController = GetComponentInParent<PieceHoverCardController>();
            if (pieceHoverCardController == null)
                pieceHoverCardController = FindFirstObjectByType<PieceHoverCardController>();
            if (pieceHoverCardController == null)
                pieceHoverCardController = gameObject.AddComponent<PieceHoverCardController>();
            return pieceHoverCardController;
        }

        private Color GetZoneColor(ZoneType zone) => Theme.GetZoneColor(zone);

        private void InitializeTile(BoardTileView tile, GridCoord coord, bool isSpecial)
        {
            var zone = _layout.GetZone(coord);
            bool useBackdrop = UsesBattlefieldBackdrop();
            tile.Initialize(
                coord,
                GetZoneColor(zone),
                isSpecial,
                useBackdrop ? null : PickTerrainSprite(zone, coord),
                useBackdrop);
        }

        private BoardTerrainArtSO ResolveTerrainArt() =>
            terrainArt != null ? terrainArt : BoardTerrainArtProvider.Current;

        private bool UsesBattlefieldBackdrop()
        {
            var art = ResolveTerrainArt();
            return art != null && art.HasBattlefieldBackdrop;
        }

        private Sprite PickTerrainSprite(ZoneType zone, GridCoord coord)
        {
            var art = ResolveTerrainArt();
            return art != null && art.HasTerrainTiles ? art.PickTile(zone, coord) : null;
        }

        private void EnsureBattlefieldPresentation()
        {
            if (gridLayout == null)
                return;

            var boardRect = transform as RectTransform;
            var gridRect = gridLayout.GetComponent<RectTransform>();
            if (boardRect == null || gridRect == null)
                return;

            if (battlefieldBackdrop == null || gridOverlay == null)
            {
                var ensured = BoardBattlefieldBootstrap.Ensure(this, boardRect, gridRect, gridLayout);
                battlefieldBackdrop ??= ensured.backdrop;
                gridOverlay ??= ensured.overlay;
            }
        }

        private void SyncBattlefieldPresentation()
        {
            if (gridLayout == null || _layout == null)
                return;

            EnsureBattlefieldPresentation();

            var gridRect = gridLayout.GetComponent<RectTransform>();
            var art = ResolveTerrainArt();
            var theme = Theme;
            bool useBackdrop = art != null && art.HasBattlefieldBackdrop;

            if (battlefieldBackdrop != null)
            {
                battlefieldBackdrop.Configure(gridRect, useBackdrop ? art.battlefieldBackdrop : null);
                battlefieldBackdrop.gameObject.SetActive(useBackdrop);
            }

            if (gridOverlay != null)
            {
                _layout.GetHorizontalZoneColumns(out int rearCols, out int supportCols);
                gridOverlay.gameObject.SetActive(useBackdrop);
                if (useBackdrop)
                {
                    gridOverlay.Configure(
                        gridRect,
                        gridLayout,
                        _layout.Width,
                        _layout.Height,
                        rearCols,
                        supportCols,
                        theme.boardGridLineColor,
                        theme.boardZoneDividerColor);
                }
            }
        }

        private static PieceRotation RotationFromDegrees(int degrees) =>
            degrees switch
            {
                90 => PieceRotation.R90,
                180 => PieceRotation.R180,
                270 => PieceRotation.R270,
                _ => PieceRotation.R0
            };

        private void ClearTiles()
        {
            ClearPieceChips();
            if (_piecesOverlay != null)
            {
                Destroy(_piecesOverlay.gameObject);
                _piecesOverlay = null;
            }

            if (tileRoot == null)
                return;

            foreach (Transform child in tileRoot)
                Destroy(child.gameObject);
            _tiles.Clear();
        }
    }
}
