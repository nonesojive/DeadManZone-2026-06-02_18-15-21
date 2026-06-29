using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// Keeps scene-authored Run/shop UI layout and visuals when entering Play mode.
    /// Disable <see cref="preserveSceneAuthoring"/> to re-run migration bootstraps.
    /// </summary>
    public sealed class RunUiAuthoringLock : MonoBehaviour
    {
        [SerializeField] private bool preserveSceneAuthoring = true;

        public bool PreserveSceneAuthoring
        {
            get => preserveSceneAuthoring;
            set => preserveSceneAuthoring = value;
        }

        public static bool ShouldPreserve(Transform buildPanel)
        {
            if (buildPanel == null)
                return false;

            var authoringLock = buildPanel.GetComponent<RunUiAuthoringLock>();
            return authoringLock != null && authoringLock.preserveSceneAuthoring;
        }

        public static bool ShouldSkipVisualMigration(Transform buildPanel) =>
            ShouldSkipVisualMigration(buildPanel, Application.isPlaying);

        public static bool ShouldSkipVisualMigration(Transform buildPanel, bool isPlaying)
        {
            if (buildPanel == null)
                return false;

            // ponytail: preserveSceneAuthoring blocks migration in edit + play; isPlaying kept for callers/tests.
            return ShouldPreserve(buildPanel);
        }

        public static RunUiAuthoringLock EnsureOn(Transform buildPanel)
        {
            if (buildPanel == null)
                return null;

            var authoringLock = buildPanel.GetComponent<RunUiAuthoringLock>();
            if (authoringLock == null)
                authoringLock = buildPanel.gameObject.AddComponent<RunUiAuthoringLock>();

            authoringLock.preserveSceneAuthoring = true;
            return authoringLock;
        }

        public static Transform FindBuildPanel(Transform descendant)
        {
            var current = descendant;
            while (current != null)
            {
                if (current.GetComponent<RunUiAuthoringLock>() != null ||
                    current.GetComponent<RunBuildUiBootstrap>() != null ||
                    current.name == "ShopScene" ||
                    current.name == "BuildPanel")
                    return current;

                current = current.parent;
            }

            return null;
        }

        public static bool ShouldSkipVisualMigrationForDescendant(Transform descendant, bool isPlaying)
        {
            return ShouldSkipVisualMigration(FindBuildPanel(descendant), isPlaying);
        }
    }
}
