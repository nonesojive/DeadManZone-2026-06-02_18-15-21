using UnityEngine;

namespace DeadManZone.Presentation.Visual
{
    [ExecuteAlways]
    public sealed class VisualProfileApplier : MonoBehaviour
    {
        [SerializeField] private VisualProfileSO profile;
        [SerializeField] private VisualProfileSceneKind sceneKind = VisualProfileSceneKind.MainMenu;
        [SerializeField] private Transform menuEnvironmentRoot;

        public VisualProfileSO Profile
        {
            get => profile;
            set => profile = value;
        }

        public void ApplyNow(VisualProfileSO overrideProfile = null)
        {
            var active = overrideProfile ?? VisualProfileProvider.Current ?? profile;
            if (active == null)
                return;

            switch (sceneKind)
            {
                case VisualProfileSceneKind.MainMenu:
                    active.mainMenuAtmosphere?.ApplyToRenderSettings();
                    active.mainMenuLighting?.ApplyToEnvironment(menuEnvironmentRoot);
                    break;
                case VisualProfileSceneKind.Run:
                    active.runAtmosphere?.ApplyToRenderSettings();
                    break;
            }
        }

        private void OnEnable() => ApplyNow();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
                ApplyNow();
        }
#endif
    }

    public enum VisualProfileSceneKind
    {
        MainMenu,
        Run
    }
}
