using System;
using System.Collections.Generic;

namespace DeadManZone.Core.Meta
{
    [Serializable]
    public sealed class MetaSaveData
    {
        public int SaveVersion = 1;
        public HashSet<string> UnlockedFactions = new() { "iron_vanguard" };
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
