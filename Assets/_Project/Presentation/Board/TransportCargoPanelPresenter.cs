using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Presentation.DragDrop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    /// <summary>2026-07-17 Oathborn transport tentpole (§2.5 Armored Ark): the build-phase
    /// cargo panel a placed transport exposes — up to 4 slots (2x2), matching
    /// PieceDefinition.TransportCapacity. Authored in-editor as a prefab (fixed "Slot0".."Slot3"
    /// children, each with a "SlotIcon" Image) — this presenter only fills in state, never
    /// creates/destroys slot GameObjects. One TransportCargoSlotDropTarget on the panel root
    /// covers every empty slot; BoardState.TryLoadCargo's own capacity check is what actually
    /// rejects a drop once every slot is full (see RunOrchestrator.Transport.cs).</summary>
    public sealed class TransportCargoPanelPresenter : MonoBehaviour
    {
        private const int MaxSlots = 4;

        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Image[] slotBackgrounds = new Image[MaxSlots];
        [SerializeField] private Image[] slotIcons = new Image[MaxSlots];

        private string _transportInstanceId;
        private BoardView _boardView;

        public string TransportInstanceId => _transportInstanceId;

        public void Configure(string transportInstanceId, BoardView boardView)
        {
            _transportInstanceId = transportInstanceId;
            _boardView = boardView;

            var dropTarget = GetComponent<TransportCargoSlotDropTarget>();
            if (dropTarget == null)
                dropTarget = gameObject.AddComponent<TransportCargoSlotDropTarget>();
            dropTarget.Configure(transportInstanceId, boardView);
        }

        /// <summary>Repaint every slot from the live board: filled slots show the embarked
        /// piece's icon (draggable back out — a normal BoardPieceDragSource, since dragging a
        /// loaded piece onto an ordinary board tile un-embarks it as a side effect of
        /// BoardState.TryRelocate), empty slots show nothing but stay part of the panel's
        /// single drop-target region.</summary>
        public void Refresh(BoardState board, PieceDefinition transportDefinition)
        {
            if (titleLabel != null)
                titleLabel.text = $"{transportDefinition?.DisplayName ?? "TRANSPORT"} — CARGO";

            var cargo = board?.Pieces
                .Where(p => p.CarrierInstanceId == _transportInstanceId)
                .OrderBy(p => p.InstanceId)
                .ToList() ?? new List<PlacedPiece>();

            int capacity = Mathf.Clamp(transportDefinition?.TransportCapacity ?? 0, 0, MaxSlots);

            for (int i = 0; i < MaxSlots; i++)
            {
                var background = slotBackgrounds.Length > i ? slotBackgrounds[i] : null;
                var icon = slotIcons.Length > i ? slotIcons[i] : null;
                if (background == null || icon == null)
                    continue;

                bool slotExists = i < capacity;
                background.gameObject.SetActive(slotExists);
                if (!slotExists)
                {
                    icon.enabled = false;
                    continue;
                }

                var existingDrag = icon.GetComponent<BoardPieceDragSource>();
                if (existingDrag != null)
                    Destroy(existingDrag);

                if (i >= cargo.Count)
                {
                    icon.enabled = false;
                    continue;
                }

                var piece = cargo[i];
                var source = PieceVisualLookup.GetSource(piece.Definition.Id);
                icon.enabled = source?.icon != null;
                icon.sprite = source?.icon;

                var dragSource = icon.gameObject.AddComponent<BoardPieceDragSource>();
                dragSource.Configure(
                    piece.InstanceId,
                    piece.Definition.Id,
                    piece.Anchor,
                    piece.Definition,
                    piece.Rotation);
            }
        }
    }
}
