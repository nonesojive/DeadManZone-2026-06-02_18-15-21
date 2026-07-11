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
        public int DamagePercentBonus { get; set; }
        public int AccuracyPercentBonus { get; set; }
        public int AttackSpeedSteps { get; set; }
        public int MovementSpeedBonus { get; set; }
        public int AttackRangeSteps { get; set; }
        /// <summary>Fight-start armor (synergies, critical mass, ProtectSupport). Permanent for the whole fight.</summary>
        public int ArmorBuffSteps { get; set; }

        /// <summary>Pause-granted armor (ShieldAllies). Expires when the next pause boundary fires.</summary>
        public int PauseArmorBuffSteps { get; set; }

        /// <summary>Effective armor steps for damage resolution: permanent + pause-scoped.</summary>
        public int TotalArmorSteps => ArmorBuffSteps + PauseArmorBuffSteps;
        public int DamageDealtThisFight { get; set; }
        public int DamageTakenThisFight { get; set; }
        public GridCoord AnchorPosition { get; set; }
        public int SpawnAnchorY { get; init; }
        public IReadOnlyList<GridCoord> ShapeOffsets { get; init; }
        public IReadOnlyList<GridCoord> OccupiedCells { get; private set; }

        public bool IsAlive => CurrentHp > 0;

        public int EffectiveMovementSpeed =>
            System.Math.Max(0, Definition.MovementSpeed + MovementSpeedBonus);

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
