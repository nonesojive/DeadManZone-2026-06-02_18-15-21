using DeadManZone.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Grimdark trench lighting and fog for the combat arena.</summary>
    internal static class CombatArenaEnvironment
    {
        private const string KeyLightName = "ArenaKeyLight";
        private const string FillLightName = "ArenaFillLight";
        private const string RimLightName = "ArenaRimLight";

        public static void Apply(CombatArenaConfigSO config, Transform arenaRoot, Camera arenaCamera)
        {
            ApplyAmbient();
            ApplyFog(config);
            ApplyLights(arenaRoot);
            ApplyCameraBackground(arenaCamera);
        }

        private static void ApplyAmbient()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.19f, 0.22f);
            RenderSettings.ambientEquatorColor = new Color(0.14f, 0.13f, 0.12f);
            RenderSettings.ambientGroundColor = new Color(0.07f, 0.06f, 0.05f);
            RenderSettings.ambientIntensity = 0.88f;
        }

        private static void ApplyFog(CombatArenaConfigSO config)
        {
            bool useFog = config == null || config.enableArenaFog;
            RenderSettings.fog = useFog;
            if (!useFog)
                return;

            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.06f, 0.055f, 0.05f);
            RenderSettings.fogDensity = config != null && config.fogDensity > 0f ? config.fogDensity : 0.024f;
        }

        private static void ApplyLights(Transform arenaRoot)
        {
            if (arenaRoot == null)
                return;

            var stale = arenaRoot.Find("ArenaLight");
            if (stale != null)
                Object.Destroy(stale.gameObject);

            EnsureDirectionalLight(
                arenaRoot,
                KeyLightName,
                new Vector3(48f, -28f, 0f),
                0.34f,
                new Color(0.94f, 0.88f, 0.78f),
                LightShadows.Soft);

            EnsureDirectionalLight(
                arenaRoot,
                FillLightName,
                new Vector3(22f, 42f, 0f),
                0.12f,
                new Color(0.62f, 0.68f, 0.78f),
                LightShadows.None);

            EnsureDirectionalLight(
                arenaRoot,
                RimLightName,
                new Vector3(12f, 168f, 0f),
                0.08f,
                new Color(0.82f, 0.72f, 0.58f),
                LightShadows.None);
        }

        private static void EnsureDirectionalLight(
            Transform arenaRoot,
            string lightName,
            Vector3 eulerAngles,
            float intensity,
            Color color,
            LightShadows shadows)
        {
            var existing = arenaRoot.Find(lightName);
            Light light;
            if (existing != null)
            {
                light = existing.GetComponent<Light>();
                if (light == null)
                    light = existing.gameObject.AddComponent<Light>();
            }
            else
            {
                var lightGo = new GameObject(lightName);
                lightGo.transform.SetParent(arenaRoot, false);
                light = lightGo.AddComponent<Light>();
            }

            light.transform.rotation = Quaternion.Euler(eulerAngles);
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            light.shadows = shadows;
        }

        private static void ApplyCameraBackground(Camera arenaCamera)
        {
            if (arenaCamera == null || RenderSettings.skybox != null)
                return;

            arenaCamera.clearFlags = CameraClearFlags.SolidColor;
            arenaCamera.backgroundColor = new Color(0.06f, 0.055f, 0.05f);
        }
    }
}
