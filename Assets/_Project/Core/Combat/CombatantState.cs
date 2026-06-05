using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

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
        public int MoveCharge { get; set; }
        public int DamageBonus { get; set; }
        public int ArmorBuffSteps { get; set; }
        public int DamageDealtThisFight { get; set; }
        public int DamageTakenThisFight { get; set; }
        public GridCoord Position { get; set; }
        public bool IsAlive => CurrentHp > 0;

        public bool HasTag(string tag) => Definition?.Tags?.Contains(tag) == true;

        public bool CanAttack => IsAlive && Definition.BaseDamage > 0;
    }
}
