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

        /// <summary>2026-07-17 round-2 playtest fix (Armored Ark cargo-as-mini-board): this
        /// piece's footprint anchor inside its carrier's fixed BoardState.CargoGridWidth x
        /// BoardState.CargoGridHeight cargo hold. Only meaningful when CarrierInstanceId is
        /// set — null otherwise. Purely a capacity/rendering bookkeeping coordinate (separate
        /// coordinate space from this piece's own Anchor on the main board, which stays
        /// wherever it was placed); recomputed by BoardState.TryLoadCargo, never persisted
        /// directly (BoardSnapshotMapper.ToBoard re-derives it on load).</summary>
        public GridCoord? CargoAnchor { get; init; }

        /// <summary>2026-07-16 faction-roster-v1 §1.4: acquisition-based and PERMANENT — set
        /// only when this instance was bought through the Cartel mercenary shop slot
        /// (CartelMercenarySlotProvider / ShopOffer.IsMercenary). Suppresses OffFactionRules
        /// .IsSalvage and sells for 0 (SalvageCalculator.Compute). Carried across
        /// moves/reserves transfers by re-passing it into TryPlace at every re-placement.</summary>
        public bool IsMercenary { get; init; }
    }
}
