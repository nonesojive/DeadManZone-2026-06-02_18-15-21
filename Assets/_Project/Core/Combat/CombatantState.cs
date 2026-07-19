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

        /// <summary>Fight max HP: the durability-scaled definition HP
        /// (CombatPacingConfig.ScaleUnitMaxHp), set once at spawn by
        /// TickCombatRun.SpawnCombatants — every max-HP magnitude read in the sim (heal clamp,
        /// low-state threshold, army-bar fractions, critical-mass base, casualty conversion)
        /// goes through this, never raw Definition.MaxHp. Falls back to raw Definition.MaxHp
        /// when unset so directly-constructed combatants (pure-rules tests, the synthetic HQ
        /// ability source) keep pre-scale behavior. Fight-start buffs (critical mass, synergy
        /// flats, twists/conditions) raise CurrentHp above this cap and never raise the cap
        /// itself — same semantics as before the scale existed.</summary>
        public int MaxHp
        {
            get => _maxHp > 0 ? _maxHp : Definition?.MaxHp ?? 0;
            init => _maxHp = value;
        }
        private readonly int _maxHp;

        /// <summary>Fight base attack damage: Definition.BaseDamage scaled by the piece's
        /// StatScale (PROVISIONAL 2026-07-19 owner spec — fight-option strength ratios), set
        /// once at spawn by TickCombatRun.SpawnCombatants; attack resolution routes through
        /// this, never raw Definition.BaseDamage. Falls back to raw Definition.BaseDamage
        /// when unset so directly-constructed combatants (pure-rules tests, the synthetic HQ
        /// ability source) keep pre-scale behavior — same pattern as <see cref="MaxHp"/>.
        /// Flat/percent damage bonuses (synergy, tactics, low-state) stack on top of this
        /// exactly as they stacked on BaseDamage before.</summary>
        public int Damage
        {
            get => _damage > 0 ? _damage : Definition?.BaseDamage ?? 0;
            init => _damage = value;
        }
        private readonly int _damage;

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

        /// <summary>2026-07-15 faction-roster-v1 §1.8 Suppression tentpole: ticks remaining on
        /// an on-hit Suppression application (SuppressionRules). Refreshed, not stacked
        /// (PROVISIONAL stacking rule) — a new hit resets this to the full duration.</summary>
        public int SuppressionTicksRemaining { get; set; }

        public bool IsSuppressed => SuppressionTicksRemaining > 0;

        /// <summary>2026-07-15 faction-roster-v1 §1.9 Crimson faction CM rule: extra ticks folded
        /// into this piece's own on-hit Suppression applications (SuppressionRules.Apply). Set at
        /// fight start by CriticalMassEngine.ApplyToCombatants; harmless (never read) on pieces
        /// without AppliesSuppressionOnHit.</summary>
        public int SuppressionDurationBonusTicks { get; set; }

        /// <summary>2026-07-15 faction-roster-v1 §1.9 Ashen faction CM rule: percent uplift to this
        /// piece's own Definition.LowStateDamageBonus while in low-state (LowStateRules).</summary>
        public int LowStateDamageBonusPercentFromCM { get; set; }

        /// <summary>2026-07-15 faction-roster-v1 §2.5 transport tentpole: true for a fielded
        /// transport piece (Definition.IsTransport). Transports are never embarked themselves.</summary>
        public bool IsTransport { get; set; }

        /// <summary>True while this piece rides inside a transport (TransportRules): off the
        /// field, untargetable, doesn't move/attack, "never dies inside". Cleared on unload or
        /// on the carrier's destruction (spill).</summary>
        public bool IsEmbarked { get; set; }

        /// <summary>Instance id of the transport carrying this piece, or null. Set at spawn from
        /// PlacedPiece.CarrierInstanceId.</summary>
        public string CarrierInstanceId { get; set; }

        /// <summary>For a transport only: cargo instance ids currently embarked. Cleared on
        /// unload/spill.</summary>
        public IReadOnlyList<string> EmbarkedCargoIds { get; set; } = System.Array.Empty<string>();

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

        /// <summary>The liveness gate for FIGHTING — routed is alive but out of the fight.
        /// Embarked cargo (transport tentpole, §2.5) is also excluded: it can't move, attack,
        /// be targeted, or be counted for the win check while riding, by construction of every
        /// loop that already gates on IsActive.</summary>
        public bool IsActive => IsAlive && !IsBroken && !IsEmbarked;

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
