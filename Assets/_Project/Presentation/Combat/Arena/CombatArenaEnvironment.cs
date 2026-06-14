using DeadManZone.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Grimdark trench lighting and fog for the combat arena.</summary>
    internal static class CombatArenaEnvironment
    {
        private const string KeyLightName = "ArenaKeyLight";

        public static void Apply(CombatArenaConfigSO config, Transform arenaRoot, Camera arenaCamera)
        {
            ApplyAmbient();
            ApplyFog(config);
            ApplyKeyLight(arenaRoot);
            ApplyCameraBackground(arenaCamera);
        }

        private static void ApplyAmbient()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.20f, 0.22f, 0.26f);
            RenderSettings.ambientEquatorColor = new Color(0.16f, 0.15f, 0.14f);
            RenderSettings.ambientGroundColor = new Color(0.09f, 0.08f, 0.07f);
            RenderSettings.ambientIntensity = 0.95f;
        }

        private static void ApplyFog(CombatArenaConfigSO config)
        {
            bool useFog = config == null || config.enableArenaFog;
            RenderSettings.fog = useFog;
            if (!useFog)
                return;

            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.07f, 0.065f, 0.06f);
            RenderSettings.fogDensity = config != null && config.fogDensity > 0f ? config.fogDensity : 0.022f;
        }

        private static void ApplyKeyLight(Transform arenaRoot)
        {
            if (arenaRoot == null)
                return;

            var stale = arenaRoot.Find("ArenaLight");
            if (stale != null)
                Object.Destroy(stale.gameObject);

            var existing = arenaRoot.Find(KeyLightName);
            if (existing != null)
                return;

            var lightGo = new GameObject(KeyLightName);
            lightGo.transform.SetParent(arenaRoot, false);
            lightGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.26f;
            light.color = new Color(0.92f, 0.86f, 0.78f);
            light.shadows = LightShadows.None;
        }

        private static void ApplyCameraBackground(Camera arenaCamera)
        {
            if (arenaCamera == null || RenderSettings.skybox != null)
                return;

            arenaCamera.clearFlags = CameraClearFlags.SolidColor;
            arenaCamera.backgroundColor = new Color(0.07f, 0.065f, 0.06f);
        }
    }
}
