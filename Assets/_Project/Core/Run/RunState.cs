using System.Collections.Generic;
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
        public int FightIndex { get; set; } = 1;
        public int Gold { get; set; }
        public int Requisition { get; set; }
        public int RerollCountThisRound { get; set; }
        public int RunSeed { get; set; }
        public string FactionId { get; set; }
        public RunPhase Phase { get; set; } = RunPhase.Build;
        public BoardSnapshot PlayerBoard { get; set; }
        public List<string> BenchPieceIds { get; set; } = new();
        public ShopState Shop { get; set; }
        public string FrozenOfferId { get; set; }
        public ShopOfferRecord LockedOffer { get; set; }
        public CombatSaveState Combat { get; set; }

        public static RunState CreateNew(string factionId, int runSeed, int startingGold, int startingRequisition)
        {
            return new RunState
            {
                FactionId = factionId,
                RunSeed = runSeed,
                Gold = startingGold,
                Requisition = startingRequisition,
                Phase = RunPhase.Build,
                FightIndex = 1
            };
        }
    }
}
