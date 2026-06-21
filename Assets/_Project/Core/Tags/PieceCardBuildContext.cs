using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;

namespace DeadManZone.Core.Tags
{
    public sealed class PieceCardBuildContext
    {
        public PieceAbilityEngine.SynergyResult? Synergy { get; init; }
        public PieceAbilityEngine.FightStartSynergySnapshot SynergySnapshot { get; init; }
        public BoardState Board { get; init; }
        public string InstanceId { get; init; }
        public bool IsSalvaged { get; init; }
        public string LastEnemyFactionId { get; init; }
        public string LastEnemyFactionDisplayName { get; init; }
    }
}
