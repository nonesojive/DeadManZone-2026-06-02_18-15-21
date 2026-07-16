using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public static class CombatStealthRules
    {
        // Piece-id special case pending a data-driven stealth flag; keep the id in one place.
        // 2026-07-15 faction-roster-v1: renamed from "ironclad_marksman" — the stealth-until-
        // mid-fight-pause identity moved to the Rare marksman_doctrine_officer.
        public const string MarksmanPieceId = "marksman_doctrine_officer";

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
