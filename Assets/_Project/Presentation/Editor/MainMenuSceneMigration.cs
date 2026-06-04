using UnityEditor;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Regenerates MainMenu.unity once after controller/scene setup changes are imported.
    /// </summary>
    [InitializeOnLoad]
    internal static class MainMenuSceneMigration
    {
        private const string MigrationKey = "DeadManZone_MainMenu_v2_20260602";

        static MainMenuSceneMigration()
        {
            EditorApplication.delayCall += TryMigrate;
        }

        private static void TryMigrate()
        {
            if (EditorPrefs.GetBool(MigrationKey, false))
                return;

            MenuSceneSetup.RefreshMainMenuScene();
            EditorPrefs.SetBool(MigrationKey, true);
        }
    }
}
