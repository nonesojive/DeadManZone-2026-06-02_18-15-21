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

        public void ApplyNow()
        {
            if (profile == null)
                profile = VisualProfileProvider.Current;
            if (profile == null)
                return;

            switch (sceneKind)
            {
                case VisualProfileSceneKind.MainMenu:
                    profile.mainMenuAtmosphere?.ApplyToRenderSettings();
                    profile.mainMenuLighting?.ApplyToEnvironment(menuEnvironmentRoot);
                    break;
                case VisualProfileSceneKind.Run:
                    profile.runAtmosphere?.ApplyToRenderSettings();
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
