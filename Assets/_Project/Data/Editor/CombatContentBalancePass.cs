using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    /// <summary>One-shot content tuning for four-band range + accuracy defaults.</summary>
    public static class CombatContentBalancePass
    {
        [MenuItem("DeadManZone/Combat Content Balance Pass")]
        public static void RunOnAllPieces()
        {
            var guids = AssetDatabase.FindAssets("t:PieceDefinitionSO");
            int rangeUpdates = 0;
            int accuracyUpdates = 0;

            foreach (var guid in guids)
            {
                var piece = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (piece == null)
                    continue;

                bool changed = false;
                var targetRange = ResolveRange(piece);
                if (piece.attackRange != targetRange)
                {
                    piece.attackRange = targetRange;
                    rangeUpdates++;
                    changed = true;
                }

                int? targetAccuracy = ResolveAccuracyOverride(piece);
                if (targetAccuracy.HasValue && piece.accuracyOverride != targetAccuracy.Value)
                {
                    piece.accuracyOverride = targetAccuracy.Value;
                    accuracyUpdates++;
                    changed = true;
                }

                if (changed)
                    EditorUtility.SetDirty(piece);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[CombatContentBalancePass] Updated {rangeUpdates} range values and {accuracyUpdates} accuracy overrides.");
        }

        private static AttackRangeTier ResolveRange(PieceDefinitionSO piece)
        {
            if (piece.attackType == AttackType.Melee)
                return AttackRangeTier.Melee;

            if (piece.combatRole == GameTagIds.Sniper
                || piece.combatRole == GameTagIds.Artillery)
                return AttackRangeTier.Long;

            if (piece.attackType == AttackType.Shredding
                || piece.id != null && (piece.id.Contains("mg_") || piece.id.Contains("mortar")))
                return AttackRangeTier.Medium;

            if (piece.category == PieceCategory.Building
                || piece.combatRole == GameTagIds.Headquarters)
                return AttackRangeTier.Melee;

            if (piece.attackType == AttackType.Explosive
                && (piece.combatRole == GameTagIds.Artillery
                    || (piece.id != null && piece.id.Contains("artillery"))
                    || (piece.id != null && piece.id.Contains("field_gun"))
                    || (piece.id != null && piece.id.Contains("cannon"))))
                return AttackRangeTier.Long;

            // Default infantry / rifle line
            return AttackRangeTier.Short;
        }

        private static int? ResolveAccuracyOverride(PieceDefinitionSO piece)
        {
            if (piece.combatRole == GameTagIds.Sniper)
                return 90;

            if (piece.attackType == AttackType.Shredding)
                return 70;

            return null;
        }
    }
}
