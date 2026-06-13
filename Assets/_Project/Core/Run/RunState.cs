using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Shop;

namespace DeadManZone.Core.Run
{
    public sealed class CombatPauseContext
    {
        public int CheckpointIndex { get; init; }
        public PauseTriggerContext Trigger { get; init; }
        public int Authority { get; init; }
        public TacticType ActiveTactic { get; init; }
        public bool HqAlive { get; init; }
        public bool HasCommandPiece { get; init; }
        public IReadOnlyList<AvailableCommand> AvailableAbilities { get; init; }
        public TacticType? PendingSelectedTactic { get; init; }
        public IReadOnlyList<GrantedAbility> PendingSelectedAbilities { get; init; }
    }

    public sealed class CombatSaveState
    {
        public int CombatSeed { get; set; }
        public BoardSnapshot EnemyBoard { get; set; }
        public int CheckpointsFired { get; set; }
        public int GlobalTick { get; set; }
        public int LastSegmentIndex { get; set; }
        public bool AwaitingCommand { get; set; }
        public int Requisition { get; set; }
        public int Authority { get; set; }
        public TacticType PlayerTactic { get; set; } = TacticType.DisciplinedFire;
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
        public int SaveSchemaVersion { get; set; } = 6;
        public int FightIndex { get; set; } = 1;
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
        public BoardSnapshot PlayerBoard { get; set; }
        public ReservesSnapshot Reserves { get; set; }
        public ShopState Shop { get; set; }
        public string FrozenOfferId { get; set; }
        public ShopOfferRecord LockedOffer { get; set; }
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
                SaveSchemaVersion = 6,
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

