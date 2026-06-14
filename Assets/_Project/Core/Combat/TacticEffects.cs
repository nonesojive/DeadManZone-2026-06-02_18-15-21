using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    /// <summary>Applies tactic modifiers to movement, damage, and targeting behavior.</summary>
    public static class TacticEffects
    {
        public const float AdvanceMoveBonus = 1.10f;
        public const float StandGroundMovePenalty = 0.90f;
        public const int DisciplinedFireDamageBonus = 1;
        public const int ProtectSupportRearArmorSteps = 2;

        public static int GetMovementChargeMultiplier(TacticType tactic) => tactic switch
        {
            TacticType.Advance => (int)(AdvanceMoveBonus * 100),
            TacticType.StandGround => (int)(StandGroundMovePenalty * 100),
            _ => 100
        };

        public static int GetDamageBuff(TacticType tactic) => tactic switch
        {
            TacticType.DisciplinedFire => DisciplinedFireDamageBonus,
            _ => 0
        };

        public static void ApplyProtectSupportBuffs(
            TacticType tactic,
            IList<CombatantState> playerCombatants,
            BoardLayout boardLayout)
        {
            if (tactic != TacticType.ProtectSupport || boardLayout == null)
                return;

            foreach (var combatant in playerCombatants)
            {
                if (!combatant.IsAlive)
                    continue;

                int localX = combatant.AnchorPosition.X;
                if (localX < 0 || localX >= boardLayout.Width)
                    continue;

                if (boardLayout.GetZone(new GridCoord(localX, combatant.AnchorPosition.Y)) == ZoneType.Rear)
                    combatant.ArmorBuffSteps += ProtectSupportRearArmorSteps;
            }
        }
    }
}
