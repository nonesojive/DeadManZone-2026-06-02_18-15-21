using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Board
{
    /// <summary>Combat participation after HQ/combat board split (replaces combatant/non-combatant system tags).</summary>
    public static class PieceCombatRules
    {
        public static bool ParticipatesInCombat(PieceDefinition piece)
        {
            if (piece == null)
                return false;

            return piece.Category switch
            {
                PieceCategory.Unit => true,
                PieceCategory.Hybrid => true,
                PieceCategory.Building => piece.BaseDamage > 0,
                _ => false
            };
        }

        public static bool IsDeprioritizedTarget(PieceDefinition definition)
        {
            if (definition == null)
                return true;

            if (definition.Category == PieceCategory.Building && definition.BaseDamage <= 0)
                return true;

            return CombatRoleProfile.ResolveBias(definition.CombatRole) == CombatRoleTargetingBias.NoAttack;
        }
    }
}
