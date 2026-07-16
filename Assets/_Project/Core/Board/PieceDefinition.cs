using System.Collections.Generic;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Board
{
    public sealed class PieceDefinition
    {
        public string Id { get; init; }
        public string DisplayName { get; init; }
        public PieceCategory Category { get; init; }
        public PieceShape Shape { get; init; }
        public string Primary { get; init; }
        public string CombatRole { get; init; }
        public string SystemTag { get; init; }
        public IReadOnlyList<string> SynergyTags { get; init; } = System.Array.Empty<string>();
        public IReadOnlyList<string> AbilityTags { get; init; } = System.Array.Empty<string>();
        public IReadOnlyList<string> FlavorTags { get; init; } = System.Array.Empty<string>();
        public IReadOnlyList<string> Tags { get; init; } = System.Array.Empty<string>();
        public IReadOnlyList<PieceAbilityDefinition> Abilities { get; init; }
            = System.Array.Empty<PieceAbilityDefinition>();
        public int MaxHp { get; init; }
        /// <summary>Morale pool (ADR-0005). 0 = morale-immune: never breaks, takes no morale damage (structures).</summary>
        public int MaxMorale { get; init; }
        public int BaseDamage { get; init; }
        /// <summary>Morale damage dealt to the target on any damaging attack (ADR-0005 terror channel).</summary>
        public int TerrorDamage { get; init; }
        /// <summary>Percent (0-100) this piece's own incoming morale damage is reduced by, before
        /// any aura contribution (MoraleRules.ApplyResistance). 2026-07-15 faction-roster-v1 §2.2:
        /// Iron Guard's "takes reduced morale damage".</summary>
        public int MoraleDamageResistancePercent { get; init; }
        public int CooldownTicks { get; init; }
        public int RequisitionCost { get; init; }
        public int ManpowerCost { get; init; }
        public int MusterPerShop { get; init; }
        public ShopModifierFlags ShopModifiers { get; init; }
        public int SalvageChanceBonus { get; init; }
        public CommandActionFlags CommandActions { get; init; }
        public AttackSpeedTier AttackSpeed { get; init; } = AttackSpeedTier.Medium;
        public AttackRangeTier AttackRange { get; init; } = AttackRangeTier.Medium;
        public int MovementSpeed { get; init; } = 2;
        public ArmorType ArmorType { get; init; } = ArmorType.Light;
        public AttackType AttackType { get; init; } = AttackType.Ballistic;
        public GrantedAbility GrantedAbility { get; init; } = GrantedAbility.None;
        public int? AccuracyOverride { get; init; }
        public string FactionId { get; init; } = "neutral";

        /// <summary>Design role, not raw power (M3). Defaults Common so pieces from
        /// assets generated before the rarity pass stay valid.</summary>
        public Rarity Rarity { get; init; } = Rarity.Common;

        // ---- 2026-07-15 faction-roster-v1 §1.8/§4 new-tech ledger fields ----
        // Content (piece data) lands in a later wave; these are the seams that wave authors
        // against. All magnitudes referenced by the paired Rules classes are PROVISIONAL.

        /// <summary>Suppression tentpole (Crimson, §1.8): this piece's attacks apply
        /// Suppression on hit (SuppressionRules.Apply) — the game's ONLY enemy-facing debuff
        /// family (border rule). Attack-speed tier step-down + movement charge slow for N ticks.</summary>
        public bool AppliesSuppressionOnHit { get; init; }

        /// <summary>Transport tentpole (Oathborn, §1.8/§2.5): pieces load as cargo during
        /// Build (PlacedPiece.CarrierInstanceId) and ride embarked until this transport reaches
        /// its opening-window target cell (unload) or is destroyed (spill). See TransportRules.</summary>
        public bool IsTransport { get; init; }

        /// <summary>Max cargo pieces this transport can carry. Only meaningful when IsTransport.</summary>
        public int TransportCapacity { get; init; }

        /// <summary>Low-state trigger bonus (Ashen, §2.9): flat damage bonus while this piece is
        /// below the universal 50% HP-or-morale threshold (LowStateRules.IsLowState).</summary>
        public int LowStateDamageBonus { get; init; }

        /// <summary>Low-state trigger bonus (Ashen, §2.9): attack-speed tier steps while below
        /// the 50% threshold (LowStateRules.IsLowState).</summary>
        public int LowStateAttackSpeedSteps { get; init; }

        /// <summary>In-combat healing (Oathborn medics, §4 🟡): HP restored to each eligible
        /// ally per pulse (HealPulseRules), capped at the ally's MaxHp.</summary>
        public int HealPulseAmount { get; init; }

        /// <summary>Chebyshev radius HealPulseAmount reaches (HealPulseRules.GetHealTargets).</summary>
        public int HealPulseRadius { get; init; }

        /// <summary>Tick cadence between pulses (HealPulseRules.IsPulseTick). 0 = no pulse (default).</summary>
        public int HealPulseIntervalTicks { get; init; }

        /// <summary>Gas→morale fusion (Blightborn's Duchess of Sighs, rare-only, §2.7): while a
        /// piece with this flag is active, gas-type attacks from its side also deal equal
        /// morale damage (TickCombatRun.ResolveAttacks).</summary>
        public bool GasDealsMoraleDamage { get; init; }

        /// <summary>Ambient-gas hijack (Blightborn's Yellow Autumn, rare-only, §2.7): while a
        /// piece with this flag is active on a side, the ambient anti-stall gas
        /// (GasDamageSystem) starts earlier for the whole fight and that side's units are
        /// immune to it.</summary>
        public bool HijacksAmbientGas { get; init; }

        /// <summary>Third pause window (Paradox's The Second Hand, §1.7/§4 🟡): a fielded piece
        /// with this flag appends one extra threshold to the fight's pause windows
        /// (TickCombatRun's per-instance pause-threshold list, seeded from CombatPacingConfig).</summary>
        public bool AddsPauseWindow { get; init; }

        /// <summary>Repeat activations tentpole (Paradox's Doctor Recursion, §1.8): while a
        /// piece with this flag is active, this army's pause-window abilities each fire twice
        /// (CommandProcessor.TryApplyBatch) — deterministic, zero randomness (border rule).</summary>
        public bool RepeatsPauseAbilities { get; init; }
    }

    [System.Flags]
    public enum ShopModifierFlags
    {
        None = 0,
        ExtraGeneralSlot = 1 << 0,
        GoldDiscount10 = 1 << 1,
        EnemyTagPreview = 1 << 2,
        GuaranteeEngineerOffer = 1 << 3,
        SalvageChanceBoost5 = 1 << 4
    }

    [System.Flags]
    public enum CommandActionFlags
    {
        None = 0,
        ChangeStance = 1 << 0,
        SpendRequisitionBuff = 1 << 1,
        CallStrike = 1 << 2
    }
}
