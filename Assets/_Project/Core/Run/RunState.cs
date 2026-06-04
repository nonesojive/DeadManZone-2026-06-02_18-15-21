using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Shop;

namespace DeadManZone.Core.Run
{
    public sealed class CombatSaveState
    {
        public int CombatSeed { get; set; }
        public BoardSnapshot EnemyBoard { get; set; }
        public CombatPhase CompletedPhase { get; set; }
        public bool AwaitingCommand { get; set; }
        public int Requisition { get; set; }
        public int Authority { get; set; }
        public int ActiveSegment { get; set; }
        public int SegmentTick { get; set; }
        public List<PhaseCommand> SubmittedCommands { get; set; } = new();
        public List<CombatEventRecord> EventLog { get; set; } = new();
    }

    public sealed class CombatEventRecord
    {
        public CombatPhase Phase { get; set; }
        public int Tick { get; set; }
        public string ActorId { get; set; }
        public string ActionType { get; set; }
        public string TargetId { get; set; }
        public int Value { get; set; }
    }

    public sealed class RunState
    {
        public int SaveSchemaVersion { get; set; } = 3;
        public int FightIndex { get; set; } = 1;
        public int Supplies { get; set; }
        public int Manpower { get; set; }
        public int Authority { get; set; }
        public int Morale { get; set; }
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
                SaveSchemaVersion = 3,
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

