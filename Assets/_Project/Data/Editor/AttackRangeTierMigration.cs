using UnityEditor;

namespace DeadManZone.Data.Editor
{
    public static class AttackRangeTierMigration
    {
        [MenuItem("DeadManZone/Migrate Attack Range Tiers (Legacy)")]
        public static void MigrateAllPieceDefinitions()
        {
            CombatContentBalancePass.RunOnAllPieces();
        }

        [MenuItem("DeadManZone/Migrate Attack Range Tiers")]
        public static void MigrateLegacyMenu() =>
            MigrateAllPieceDefinitions();
    }
}
