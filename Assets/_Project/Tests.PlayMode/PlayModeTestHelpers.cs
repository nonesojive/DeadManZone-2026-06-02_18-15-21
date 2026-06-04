using DeadManZone.Game;
using UnityEngine;

namespace DeadManZone.PlayMode.Tests
{
    internal static class PlayModeTestHelpers
    {
        public static void CleanupPersistentManagers()
        {
            SaveManager.DeleteSave();

            if (RunSaveBootstrap.Instance != null)
                Object.DestroyImmediate(RunSaveBootstrap.Instance.gameObject);

            if (RunManager.Instance != null)
                Object.DestroyImmediate(RunManager.Instance.gameObject);
        }
    }
}
