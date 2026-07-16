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
        public int CurrentMorale { get; set; }
        /// <summary>Set when morale hit 0: the unit routed — fled the field, alive but out of the fight (ADR-0005).</summary>
        public bool IsBroken { get; set; }
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

        /// <summary>Percent (0-100, clamped) incoming morale damage is reduced by: own
        /// Definition value + any aura contribution (Breakthrough Tank). Set at spawn,
        /// added to by PieceAbilityEngine.ApplyToCombatants.</summary>
        public int MoraleDamageResistancePercent { get; set; }

        /// <summary>Effective armor steps for damage resolution: permanent + pause-scoped.</summary>
        public int TotalArmorSteps => ArmorBuffSteps + PauseArmorBuffSteps;
        public int DamageDealtThisFight { get; set; }
        public int DamageTakenThisFight { get; set; }
        public GridCoord AnchorPosition { get; set; }
        public int SpawnAnchorY { get; init; }
        public IReadOnlyList<GridCoord> ShapeOffsets { get; init; }
        public IReadOnlyList<GridCoord> OccupiedCells { get; private set; }

        public bool IsAlive => CurrentHp > 0;

        /// <summary>Morale-immune units (MaxMorale 0, e.g. structures) never break and take no morale damage.</summary>
        public bool CanBreak => Definition.MaxMorale > 0;

        /// <summary>The liveness gate for FIGHTING — routed is alive but out of the fight.</summary>
        public bool IsActive => IsAlive && !IsBroken;

        public int EffectiveMovementSpeed =>
            System.Math.Max(0, Definition.MovementSpeed + MovementSpeedBonus);

        public bool HasTag(string tag) => PieceTagQueries.HasTag(Definition, tag);

        public bool CanAttack => IsActive && Definition.BaseDamage > 0;

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
