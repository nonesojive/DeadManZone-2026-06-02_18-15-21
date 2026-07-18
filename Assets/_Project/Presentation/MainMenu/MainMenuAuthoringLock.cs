using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadManZone.Presentation.MainMenu
{
    /// <summary>
    /// Marks MainMenu.unity as hand-authored scene state (painted background, title layout,
    /// button column — 2026-07 visual overhaul). While present with
    /// <see cref="preserveSceneAuthoring"/> enabled, scene regeneration menus
    /// ("Setup Main Menu &amp; Run Scenes" / "Refresh Main Menu Scene") skip rebuilding the scene.
    /// Same pattern as <c>RunUiAuthoringLock</c> for the ShopV2 build surface.
    /// </summary>
    public sealed class MainMenuAuthoringLock : MonoBehaviour
    {
        [SerializeField] private bool preserveSceneAuthoring = true;

        public bool PreserveSceneAuthoring
        {
            get => preserveSceneAuthoring;
            set => preserveSceneAuthoring = value;
        }

        public static bool ShouldPreserve(Scene scene)
        {
            if (!scene.IsValid())
                return false;

            foreach (var root in scene.GetRootGameObjects())
            {
                var authoringLock = root.GetComponentInChildren<MainMenuAuthoringLock>(true);
                if (authoringLock != null)
                    return authoringLock.preserveSceneAuthoring;
            }

            return false;
        }
    }
}
