using DeadManZone.Core.Board;

namespace DeadManZone.Core.Combat
{
    public enum CombatSide
    {
        Player,
        Enemy
    }

    public sealed class CombatantState
    {
        public string InstanceId { get; init; }
        public CombatSide Side { get; init; }
        public PieceDefinition Definition { get; init; }
        public int CurrentHp { get; set; }
        public int CooldownRemaining { get; set; }
        public int DamageBonus { get; set; }
        public bool IsAlive => CurrentHp > 0;

        public bool CanAttack => IsAlive && Definition.BaseDamage > 0;
    }
}
