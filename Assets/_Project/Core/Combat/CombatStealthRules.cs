using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public static class CombatStealthRules
    {
        // Piece-id special case pending a data-driven stealth flag; keep the id in one place.
        public const string MarksmanPieceId = "ironclad_marksman";

        public static bool IsTargetableByEnemies(CombatantState target, int tacticsCheckpointIndex)
        {
            if (target == null || !target.IsAlive)
                return false;

            if (!PieceTagQueries.HasAbilityTag(target.Definition, GameTagIds.Stealth))
                return true;

            // Marksman rule: hidden until the mid-fight pause fires (CheckpointsFired
            // maxes at PauseThresholds.Length == 1, so >= 1 is the reachable expiry).
            if (target.Definition.Id == MarksmanPieceId)
                return tacticsCheckpointIndex >= 1;

            return true; // default stealth behavior for other units
        }
    }
}
