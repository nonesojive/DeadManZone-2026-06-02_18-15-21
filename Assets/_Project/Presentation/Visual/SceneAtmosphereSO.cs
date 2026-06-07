using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Presentation.Visual
{
    [CreateAssetMenu(menuName = "DeadManZone/Visual/Scene Atmosphere")]
    public sealed class SceneAtmosphereSO : ScriptableObject
    {
        public bool fogEnabled = true;
        public Color fogColor = new(0.12f, 0.08f, 0.05f, 1f);
        public FogMode fogMode = FogMode.Exponential;
        public float fogDensity = 0.035f;
        public float linearFogStart;
        public float linearFogEnd = 300f;
        public AmbientMode ambientMode = AmbientMode.Trilight;
        public Color ambientSkyColor = new(0.08f, 0.09f, 0.11f);
        public Color ambientEquatorColor = new(0.06f, 0.05f, 0.04f);
        public Color ambientGroundColor = new(0.03f, 0.025f, 0.02f);

        public void ApplyToRenderSettings()
        {
            RenderSettings.fog = fogEnabled;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogStartDistance = linearFogStart;
            RenderSettings.fogEndDistance = linearFogEnd;
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientSkyColor = ambientSkyColor;
            RenderSettings.ambientEquatorColor = ambientEquatorColor;
            RenderSettings.ambientGroundColor = ambientGroundColor;
        }

        public void CopyFromCurrentRenderSettings()
        {
            fogEnabled = RenderSettings.fog;
            fogColor = RenderSettings.fogColor;
            fogMode = RenderSettings.fogMode;
            fogDensity = RenderSettings.fogDensity;
            linearFogStart = RenderSettings.fogStartDistance;
            linearFogEnd = RenderSettings.fogEndDistance;
            ambientMode = RenderSettings.ambientMode;
            ambientSkyColor = RenderSettings.ambientSkyColor;
            ambientEquatorColor = RenderSettings.ambientEquatorColor;
            ambientGroundColor = RenderSettings.ambientGroundColor;
        }
    }
}
