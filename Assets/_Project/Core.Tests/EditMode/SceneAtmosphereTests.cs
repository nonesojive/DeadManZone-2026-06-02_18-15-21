using DeadManZone.Presentation.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class SceneAtmosphereTests
    {
        [Test]
        public void ApplyToRenderSettings_SetsFogAndAmbient()
        {
            var atmosphere = ScriptableObject.CreateInstance<SceneAtmosphereSO>();
            atmosphere.fogEnabled = true;
            atmosphere.fogColor = new Color(0.2f, 0.1f, 0.05f, 1f);
            atmosphere.fogDensity = 0.04f;
            atmosphere.fogMode = FogMode.Exponential;
            atmosphere.ambientSkyColor = Color.red;
            atmosphere.ambientEquatorColor = Color.green;
            atmosphere.ambientGroundColor = Color.blue;
            atmosphere.ambientMode = AmbientMode.Trilight;

            atmosphere.ApplyToRenderSettings();

            Assert.IsTrue(RenderSettings.fog);
            Assert.AreEqual(atmosphere.fogColor, RenderSettings.fogColor);
            Assert.AreEqual(atmosphere.fogDensity, RenderSettings.fogDensity, 0.0001f);
            Assert.AreEqual(atmosphere.ambientSkyColor, RenderSettings.ambientSkyColor);

            Object.DestroyImmediate(atmosphere);
        }
    }
}
