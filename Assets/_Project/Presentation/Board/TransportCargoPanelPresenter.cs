using System.Collections;
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
    /// cargo panel a placed transport exposes — a real 2x2 mini board (BoardState
    /// .CargoGridWidth/Height), matching PieceDefinition.TransportCapacity's 4-cell total.
    /// Authored in-editor as a prefab (fixed "Slot0".."Slot3" children in a 2-column
    /// GridLayoutGroup, each with a "SlotIcon" Image) — this presenter only fills in state,
    /// never creates/destroys slot GameObjects. One TransportCargoSlotDropTarget on the panel
    /// root covers every slot; BoardState.TryLoadCargo's own footprint-fit check is what
    /// actually rejects a drop that doesn't fit (see RunOrchestrator.Transport.cs) — a reject
    /// flashes this panel (FlashRejected) with the reason instead of silently snapping back.</summary>
    public sealed class TransportCargoPanelPresenter : MonoBehaviour
    {
        private const int MaxSlots = 4;

        private static readonly Color EmptySlotColor = new(0.12f, 0.11f, 0.095f, 0.55f);
        private static readonly Color FilledSlotColor = new(0.30f, 0.245f, 0.16f, 0.98f);
        private static readonly Color RejectedTitleColor = new(0.86f, 0.27f, 0.21f, 1f);

        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Image[] slotBackgrounds = new Image[MaxSlots];
        [SerializeField] private Image[] slotIcons = new Image[MaxSlots];

        private string _transportInstanceId;
        private BoardView _boardView;
        private string _baseTitle;
        private bool _isFlashingRejection;
        private Coroutine _rejectionRoutine;

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

        /// <summary>Repaint every slot from the live board: the hold is a fixed 2x2 grid
        /// (always all 4 slots shown — capacity is now footprint-fit, not a per-transport
        /// slot count), each loaded piece's real footprint (CargoAnchor + its own Shape)
        /// marks every cell it occupies with its icon and a "filled" background tint, empty
        /// cells stay dim/empty. Dragging a marked cell back OUT (see BoardPieceDragSource on
        /// its first occupied cell) is an ordinary drag onto a normal board tile, which
        /// un-embarks it as a side effect of BoardState.TryRelocate.</summary>
        public void Refresh(BoardState board, PieceDefinition transportDefinition)
        {
            _baseTitle = $"{transportDefinition?.DisplayName ?? "TRANSPORT"} — CARGO";
            if (titleLabel != null && !_isFlashingRejection)
                titleLabel.text = _baseTitle;

            var cargo = board?.Pieces
                .Where(p => p.CarrierInstanceId == _transportInstanceId)
                .OrderBy(p => p.InstanceId)
                .ToList() ?? new List<PlacedPiece>();

            for (int i = 0; i < MaxSlots; i++)
                ResetSlot(i);

            foreach (var piece in cargo)
                PaintFootprint(piece);
        }

        private void ResetSlot(int index)
        {
            var background = slotBackgrounds.Length > index ? slotBackgrounds[index] : null;
            var icon = slotIcons.Length > index ? slotIcons[index] : null;
            if (background == null || icon == null)
                return;

            background.color = EmptySlotColor;

            var existingDrag = icon.GetComponent<BoardPieceDragSource>();
            if (existingDrag != null)
                Destroy(existingDrag);

            icon.enabled = false;
            icon.sprite = null;
        }

        private void PaintFootprint(PlacedPiece piece)
        {
            if (piece.CargoAnchor is not { } anchor)
                return; // tagged without a hold position shouldn't happen post-fit-check

            var source = PieceVisualLookup.GetSource(piece.Definition.Id);
            bool wiredDragSource = false;

            foreach (var cell in piece.Definition.Shape.GetCells(anchor, PieceRotation.R0))
            {
                int slotIndex = cell.Y * BoardState.CargoGridWidth + cell.X;
                if (slotIndex < 0 || slotIndex >= MaxSlots)
                    continue;

                var background = slotBackgrounds.Length > slotIndex ? slotBackgrounds[slotIndex] : null;
                var icon = slotIcons.Length > slotIndex ? slotIcons[slotIndex] : null;
                if (background == null || icon == null)
                    continue;

                background.color = FilledSlotColor;
                icon.enabled = source?.icon != null;
                icon.sprite = source?.icon;

                // Only the first marked cell gets the drag-out source — a multi-cell piece
                // marks every cell it spans, but dragging any one of them should un-embark
                // the SAME piece once, not fight over N independent drag sources.
                if (!wiredDragSource)
                {
                    var dragSource = icon.gameObject.AddComponent<BoardPieceDragSource>();
                    dragSource.Configure(
                        piece.InstanceId,
                        piece.Definition.Id,
                        piece.Anchor,
                        piece.Definition,
                        piece.Rotation);
                    wiredDragSource = true;
                }
            }
        }

        /// <summary>2026-07-17 round-2 playtest fix: rejected-drop affordance — a brief shake
        /// + the panel title swaps to the actual rejection reason for a beat before reverting,
        /// instead of the drop just silently snapping back to where it came from.</summary>
        public void FlashRejected(string reason)
        {
            if (!isActiveAndEnabled)
                return;

            if (_rejectionRoutine != null)
                StopCoroutine(_rejectionRoutine);
            _rejectionRoutine = StartCoroutine(RejectionFlashRoutine(reason));
        }

        private IEnumerator RejectionFlashRoutine(string reason)
        {
            _isFlashingRejection = true;
            var rect = transform as RectTransform;
            var home = rect != null ? rect.anchoredPosition : Vector2.zero;

            if (titleLabel != null)
            {
                titleLabel.text = string.IsNullOrEmpty(reason) ? "REJECTED" : reason.ToUpperInvariant();
                titleLabel.color = RejectedTitleColor;
            }

            const float shakeDuration = 0.3f;
            for (float t = 0f; t < shakeDuration; t += Time.unscaledDeltaTime)
            {
                if (rect != null)
                {
                    float falloff = 1f - t / shakeDuration;
                    rect.anchoredPosition = home + new Vector2(Mathf.Sin(t * 70f) * 6f * falloff, 0f);
                }

                yield return null;
            }

            if (rect != null)
                rect.anchoredPosition = home;

            yield return new WaitForSecondsRealtime(1.0f);

            if (titleLabel != null)
            {
                titleLabel.text = _baseTitle;
                titleLabel.color = CombatGrimdarkSkinBone();
            }

            _isFlashingRejection = false;
            _rejectionRoutine = null;
        }

        // Local, dependency-free "bone" white so this file doesn't need to pull in the 3D
        // combat arena's skin class just for one default text color.
        private static Color CombatGrimdarkSkinBone() => new(0.92f, 0.90f, 0.86f, 1f);
    }
}
