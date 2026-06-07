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
        public int MoraleDelta { get; init; }
        public IReadOnlyList<BattleReportEntry> TopDamageDealt { get; init; } =
            System.Array.Empty<BattleReportEntry>();
        public IReadOnlyList<BattleReportEntry> TopDamageTaken { get; init; } =
            System.Array.Empty<BattleReportEntry>();
    }
}
