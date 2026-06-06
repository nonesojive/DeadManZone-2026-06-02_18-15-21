using UnityEngine;

namespace DeadManZone.Presentation.Visual
{
    [CreateAssetMenu(menuName = "DeadManZone/Visual/Profile")]
    public sealed class VisualProfileSO : ScriptableObject
    {
        public string displayName = "Default";
        public UiThemeSO uiTheme;
        public SceneAtmosphereSO mainMenuAtmosphere;
        public MenuLightingSO mainMenuLighting;
        public SceneAtmosphereSO runAtmosphere;
        public Object postProcessProfile; // optional PostProcessProfile ref

        public UiThemeSO UiTheme => uiTheme;
    }
}
