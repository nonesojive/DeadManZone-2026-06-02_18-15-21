using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using DeadManZone.Game;
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
        private readonly Dictionary<string, PieceChipView> _chipsByInstance = new();
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

        public void Refresh()
        {
            if (RunManager.Instance == null || !RunManager.Instance.HasActiveRun)
                return;

            EnsureGridBuilt();
            var registry = _database != null ? _database.BuildRegistry() : ContentDatabase.Load()?.BuildRegistry();
            if (registry == null)
                return;

            var reserves = RunManager.Instance.Orchestrator.GetReserves();
            ClearOccupancy();
            ClearChips();

            foreach (var piece in reserves.Pieces)
            {
                foreach (var cell in piece.Definition.Shape.GetCells(piece.Anchor, piece.Rotation))
                {
                    if (_tiles.TryGetValue(cell, out var tile))
                        tile.SetOccupied(piece.InstanceId, true);
                }

                if (_tiles.TryGetValue(piece.Anchor, out var anchorTile))
                {
                    var drag = anchorTile.GetComponent<ReservesPieceDragSource>();
                    if (drag == null)
                        drag = anchorTile.gameObject.AddComponent<ReservesPieceDragSource>();
                    drag.Configure(
                        piece.InstanceId,
                        piece.Definition.Id,
                        piece.Anchor,
                        piece.Definition,
                        piece.Rotation);

                    var chip = PieceChipView.Create(
                        anchorTile.transform,
                        piece.Definition,
                        PieceVisualLookup.GetSource(piece.Definition.Id));
                    _chipsByInstance[piece.InstanceId] = chip;
                }
            }
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
                gridLayout.constraintCount = ReservesState.Width;

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

                    tileView.Initialize(coord, Theme.cardColor);
                    if (tileObject.GetComponent<ReservesTileDropTarget>() == null)
                        tileObject.AddComponent<ReservesTileDropTarget>();
                    _tiles[coord] = tileView;
                }
            }
        }

        private void ClearOccupancy()
        {
            foreach (var tile in _tiles.Values)
                tile.SetOccupied(null, false);
        }

        private void ClearChips()
        {
            foreach (var chip in _chipsByInstance.Values)
            {
                if (chip != null)
                    Destroy(chip.gameObject);
            }

            _chipsByInstance.Clear();
        }

        private UiThemeSO Theme => theme != null ? theme : UiThemeProvider.Current;
    }
}
