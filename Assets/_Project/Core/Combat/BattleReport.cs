using System.Collections.Generic;

namespace DeadManZone.Core.Combat
{
    public sealed class BattleReportEntry
    {
        public string InstanceId { get; init; }
        public string DisplayName { get; init; }
        public int Damage { get; init; }
    }

    public sealed class BattleReport
    {
        public bool PlayerWon { get; init; }
        public bool IsDraw { get; init; }
        public int ManpowerCasualties { get; init; }
        public int SuppliesEarned { get; init; }
        /// <summary>Player units alive-and-broken at fight end — they fled, survive the
        /// fight, and stand on their board slot again next round (ADR-0005).</summary>
        public int PlayerRouted { get; init; }
        /// <summary>Enemy units that routed (escaped with their gear — no salvage roll).</summary>
        public int EnemyRouted { get; init; }
        /// <summary>Enemy units actually killed (the salvage-bearing share).</summary>
        public int EnemyKilled { get; init; }
        public IReadOnlyList<BattleReportEntry> TopDamageDealt { get; init; } =
            System.Array.Empty<BattleReportEntry>();
        public IReadOnlyList<BattleReportEntry> TopDamageTaken { get; init; } =
            System.Array.Empty<BattleReportEntry>();
    }
}
