using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public static class CombatStealthRules
    {
        public static bool IsTargetableByEnemies(CombatantState target, int tacticsCheckpointIndex)
        {
            if (target == null || !target.IsAlive)
                return false;

            if (!PieceTagQueries.HasAbilityTag(target.Definition, GameTagIds.Stealth))
                return true;

            // Marksman rule: hidden until after 2nd tactics window
            if (target.Definition.Id == "ironclad_marksman")
                return tacticsCheckpointIndex >= 2;

            return true; // default stealth behavior for other units
        }
    }
}
