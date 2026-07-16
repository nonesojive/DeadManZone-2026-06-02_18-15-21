using DeadManZone.Core.Common;

namespace DeadManZone.Core.Board
{
    public sealed class PlacedPiece
    {
        public string InstanceId { get; init; }
        public PieceDefinition Definition { get; init; }
        public GridCoord Anchor { get; init; }
        public PieceRotation Rotation { get; init; } = PieceRotation.R0;

        /// <summary>2026-07-15 faction-roster-v1 §2.5 transport tentpole: instance id of the
        /// transport this piece was loaded into during Build (BoardState.TryLoadCargo). Null =
        /// not cargo. Loading is a data tag only — it doesn't move or re-validate this piece's
        /// own board cell; TickCombatRun.SpawnCombatants reads it to spawn the piece embarked
        /// (off-field) instead of independently.</summary>
        public string CarrierInstanceId { get; init; }
    }
}
