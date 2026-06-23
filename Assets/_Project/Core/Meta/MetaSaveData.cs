using System;
using System.Collections.Generic;
using DeadManZone.Core;

namespace DeadManZone.Core.Meta
{
    [Serializable]
    public sealed class MetaSaveData
    {
        public int SaveVersion = 1;
        public HashSet<string> UnlockedFactions = new() { FactionIds.IronVanguard };
        public HashSet<string> UnlockedAchievements = new();
        public List<LeaderboardEntryRecord> LeaderboardEntries = new();
        public int TotalRunsCompleted;
        public int TotalFightsWon;
    }

    [Serializable]
    public sealed class LeaderboardEntryRecord
    {
        public string FactionId;
        public int Score;
        public int FightsCleared;
        public long TimestampUtc;
        public string PlayerName = "Commander";
    }
}
