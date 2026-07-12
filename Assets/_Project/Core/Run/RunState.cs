using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Shop;

namespace DeadManZone.Core.Run
{
    public sealed class CombatPauseContext
    {
        public int CheckpointIndex { get; init; }
        public PauseTriggerContext Trigger { get; init; }
        public int Authority { get; init; }
        public TacticType ActiveTactic { get; init; }
        public bool HasCommandPiece { get; init; }
        public IReadOnlyList<AvailableCommand> AvailableAbilities { get; init; }
        public TacticType? PendingSelectedTactic { get; init; }
        public IReadOnlyList<GrantedAbility> PendingSelectedAbilities { get; init; }
        public TacticType[] StartingTactics { get; init; }

        /// <summary>Cells a targeted ability may aim at right now (live enemy positions,
        /// mirroring <see cref="Combat.CombatAbilityExecutor.IsValidTargetCell"/>). Null on
        /// contexts built before combat state exists.</summary>
        public IReadOnlyList<GridCoord> EnemyTargetCells { get; init; }
    }

    public sealed class CombatSaveState
    {
        public int CombatSeed { get; set; }
        public BoardSnapshot EnemyBoard { get; set; }

        /// <summary>Boss fight marker; null for normal fights.</summary>
        public string BossId { get; set; }

        /// <summary>The fight's rule-modifier id — a boss Twist OR a hard option's Battle
        /// Condition (M2); null for unmodified fights. Restore resolves it through
        /// RuleModifierCatalog and re-applies it, so a restored fight replays identically.
        /// Keeps its historical name: renaming the property would break the JSON key in
        /// existing saves.</summary>
        public string ActiveTwistId { get; set; }

        /// <summary>Fight Option tier this combat was begun at (M2). Null on boss fights
        /// and legacy saves — treated as Normal (no enemy-engine suppression).</summary>
        public FightOptionTier? ActiveTier { get; set; }

        /// <summary>Arena Theme this combat renders on (M4): the chosen option's roll, or
        /// the boss pool's signature ground. Additive on v9 — null on older saves; resolve
        /// via ArenaThemes.Normalize so a restored fight reloads the same arena scene.</summary>
        public string ArenaThemeId { get; set; }
        public int CheckpointsFired { get; set; }
        public int GlobalTick { get; set; }
        public int LastSegmentIndex { get; set; }
        public bool AwaitingCommand { get; set; }
        public int Requisition { get; set; }
        public int Authority { get; set; }
        public TacticType PlayerTactic { get; set; } = TacticType.DisciplinedFire;

        /// <summary>Tactic active at fight start. Restore re-applies this one (mid-fight
        /// changes replay via <see cref="SubmittedCommands"/>); null on older saves.</summary>
        public TacticType? StartingTactic { get; set; }
        public TacticType? PendingSelectedTactic { get; set; }
        public List<GrantedAbility> PendingSelectedAbilities { get; set; } = new();
        public List<PhaseCommand> SubmittedCommands { get; set; } = new();
        public List<CombatEventRecord> EventLog { get; set; } = new();
    }

    public sealed class CombatEventRecord
    {
        public int Segment { get; set; }
        public int Tick { get; set; }
        public string ActorId { get; set; }
        public string ActionType { get; set; }
        public string TargetId { get; set; }
        public int Value { get; set; }
    }

    public sealed class RunState
    {
        // v9 (2026-07-12, M0): dropped obsolete PlayerBoard + singular LockedOffer
        // (v8 JSON migrates: LockedOffer folds into LockedOffers, stale keys ignored).
        public int SaveSchemaVersion { get; set; } = 9;

        /// <summary>Plain fight counter (banner, logs, combat-seed index). Since M1 it no
        /// longer drives difficulty — that's <see cref="Dread"/> via DreadRules.FightEquivalent.</summary>
        public int FightIndex { get; set; } = 1;

        // M1 Dread clock (ADR-0004). Schema stays v9: these are additive fields that
        // deserialize to 0 on older v9 saves — acceptable pre-release (such a save just
        // restarts its escalation clock).
        public int Dread { get; set; }
        public int BossesDefeated { get; set; }

        // M2 Fight Options (ADR-0004). Schema stays v9 — additive fields; older v9
        // saves deserialize with an empty list / -1 and fall back to the legacy
        // template path for the round already in progress.
        public List<FightOptionRecord> FightOptions { get; set; } = new();

        /// <summary>Index into <see cref="FightOptions"/> chosen for the next combat; -1 = none.</summary>
        public int ChosenFightOption { get; set; } = -1;

        // M3 rarity + pity. Schema stays v9 — additive fields; older v9 saves
        // deserialize 0/false (pity clock restarts, no hard-victory boost pending).

        /// <summary>Consecutive generated offer batches (round rolls AND rerolls)
        /// without a rare-or-above APPEARING in the shown shop. Reset on appearance,
        /// not purchase (see Shop.RarityWeights for the step/guarantee rules).</summary>
        public int RarePityBatches { get; set; }

        /// <summary>True after a hard-front VICTORY: this build round's salvage
        /// offers upweight rarer spoils. Rewritten every fight completion.</summary>
        public bool SalvageHardBoost { get; set; }
        public int Supplies { get; set; }
        public int Manpower { get; set; }
        public int Authority { get; set; }
        public int Morale { get; set; }
        public int LastMusterGained { get; set; }
        public bool EmergencyDraftUsed { get; set; }
        public int RerollCountThisRound { get; set; }
        public int RunSeed { get; set; }
        public string FactionId { get; set; }
        public RunPhase Phase { get; set; } = RunPhase.Build;
        public BoardSnapshot CombatBoard { get; set; }
        public BoardSnapshot HqBoard { get; set; }
        public ReservesSnapshot Reserves { get; set; }
        public ShopState Shop { get; set; }
        public string FrozenOfferId { get; set; }
        public List<ShopOfferRecord> LockedOffers { get; set; } = new();
        public CombatSaveState Combat { get; set; }
        public BattleReport LastBattleReport { get; set; }
        public string LastEnemyFactionId { get; set; }
        public int SalvageChancePercent { get; set; }

        /// <summary>DEV-ONLY: formatted log from the most recently completed fight.</summary>
        public string LastCombatLogText { get; set; }

        public static RunState CreateNew(
            string factionId,
            int runSeed,
            int startingSupplies,
            int startingManpower,
            int startingAuthority,
            int startingMorale)
        {
            return new RunState
            {
                FactionId = factionId,
                RunSeed = runSeed,
                Supplies = startingSupplies,
                Manpower = startingManpower,
                Authority = startingAuthority,
                Morale = startingMorale,
                Phase = RunPhase.Build,
                FightIndex = 1,
                SaveSchemaVersion = 9,
                Reserves = new ReservesSnapshot
                {
                    Width = ReservesState.Width,
                    Height = ReservesState.Height,
                    Pieces = new List<PlacedPieceRecord>()
                }
            };
        }
    }
}

