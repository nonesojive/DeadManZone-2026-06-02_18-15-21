using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public enum CombatSide
    {
        Player,
        Enemy
    }

    public sealed class CombatantState
    {
        public string InstanceId { get; init; }
        public CombatSide Side { get; init; }
        public PieceDefinition Definition { get; init; }
        public int CurrentHp { get; set; }
        public int CooldownRemaining { get; set; }
        public int MoveCharge { get; set; }
        public int MoveChargePercentBonus { get; set; }
        public int DamageBonus { get; set; }
        public int ArmorBuffSteps { get; set; }
        public int DamageDealtThisFight { get; set; }
        public int DamageTakenThisFight { get; set; }
        public GridCoord AnchorPosition { get; set; }
        public IReadOnlyList<GridCoord> ShapeOffsets { get; init; }
        public IReadOnlyList<GridCoord> OccupiedCells { get; private set; }

        [Obsolete("Use AnchorPosition")]
        public GridCoord Position
        {
            get => AnchorPosition;
            set => AnchorPosition = value;
        }

        public bool IsAlive => CurrentHp > 0;

        public bool HasTag(string tag) => PieceTagQueries.HasTag(Definition, tag);

        public bool CanAttack => IsAlive && Definition.BaseDamage > 0;

        public void RecomputeOccupiedCells()
        {
            if (ShapeOffsets == null || ShapeOffsets.Count == 0)
            {
                OccupiedCells = Array.Empty<GridCoord>();
                return;
            }

            OccupiedCells = CombatFootprint.ComputeOccupiedCells(AnchorPosition, ShapeOffsets);
        }
    }
}
