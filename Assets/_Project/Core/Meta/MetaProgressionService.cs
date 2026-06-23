using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeadManZone.Core;
using Newtonsoft.Json;

namespace DeadManZone.Core.Meta
{
    public static class MetaProgressionService
    {
        private const string SaveFileName = "deadmanzone_meta.json";
        private static MetaSaveData _cached;

        public static MetaSaveData Load()
        {
            if (_cached != null)
                return _cached;

            string path = GetSavePath();
            if (!File.Exists(path))
            {
                _cached = new MetaSaveData();
                return _cached;
            }

            try
            {
                var json = File.ReadAllText(path);
                _cached = JsonConvert.DeserializeObject<MetaSaveData>(json) ?? new MetaSaveData();
            }
            catch
            {
                _cached = new MetaSaveData();
            }

            return _cached;
        }

        public static void Save()
        {
            if (_cached == null)
                return;

            string path = GetSavePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonConvert.SerializeObject(_cached, Formatting.Indented));
        }

        public static bool IsFactionUnlocked(string factionId)
        {
            if (string.Equals(factionId, FactionIds.IronVanguard, StringComparison.OrdinalIgnoreCase))
                return true;

            var data = Load();
            return data.UnlockedFactions.Contains(factionId);
        }

        public static void UnlockFaction(string factionId)
        {
            var data = Load();
            if (data.UnlockedFactions.Add(factionId))
                Save();
        }

        public static bool TryUnlockAchievement(string achievementId)
        {
            var data = Load();
            if (!data.UnlockedAchievements.Add(achievementId))
                return false;

            Save();
            return true;
        }

        public static void RecordVictory(string factionId, int score, int fightsCleared)
        {
            var data = Load();
            data.TotalRunsCompleted++;
            data.TotalFightsWon += fightsCleared;

            UnlockFaction("FactionIds.DustScourge");
            UnlockFaction("FactionIds.CartelOfEchoes");

            data.LeaderboardEntries.Add(new LeaderboardEntryRecord
            {
                FactionId = factionId,
                Score = score,
                FightsCleared = fightsCleared,
                TimestampUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });

            data.LeaderboardEntries = data.LeaderboardEntries
                .OrderByDescending(e => e.Score)
                .Take(100)
                .ToList();

            Save();
        }

        public static IReadOnlyList<LeaderboardEntryRecord> GetLeaderboard(string factionId = null)
        {
            var data = Load();
            var query = data.LeaderboardEntries.AsEnumerable();
            if (!string.IsNullOrEmpty(factionId))
                query = query.Where(e => e.FactionId == factionId);

            return query.OrderByDescending(e => e.Score).Take(20).ToList();
        }

        public static void ResetCache() => _cached = null;

        /// <summary>Clears cached meta and removes the on-disk save (for isolated EditMode tests).</summary>
        public static void ResetForTests()
        {
            _cached = null;
            string path = GetSavePath();
            if (File.Exists(path))
                File.Delete(path);
        }

        private static string GetSavePath()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(root, "DeadManZone", SaveFileName);
        }
    }
}
