using DeadManZone.Data;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatArenaAtmosphereProfileTests
    {
        [Test]
        public void ApplyGrimDefaults_MatchesDioramaMoodRanges()
        {
            var profile = ScriptableObject.CreateInstance<CombatArenaAtmosphereProfileSO>();
            try
            {
                profile.ApplyGrimDefaults();

                Assert.IsTrue(profile.enableFog);
                Assert.GreaterOrEqual(profile.fogDensity, 0.015f);
                Assert.Less(profile.ambientSkyColor.r, 0.5f,
                    "Sky ambient should stay cool and desaturated for grim trench mood.");
                Assert.Greater(profile.keyLightIntensity, 0.8f);
                Assert.IsTrue(profile.enableBackdrop);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }
    }
}
