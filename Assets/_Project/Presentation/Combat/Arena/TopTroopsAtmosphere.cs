using DeadManZone.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Atmosphere for the Top Troops battlefield only — no legacy trench-ring backdrop.</summary>
    public static class TopTroopsAtmosphere
    {
        private const string BackdropRootName = "CombatArenaBackdrop";
        private const string KeyLightName = "TopTroopsKeyLight";
        private const string FillLightName = "TopTroopsFillLight";
        private const string RimLightName = "TopTroopsRimLight";

        public static void Apply(CombatArenaConfigSO config, Transform arenaRoot, Camera arenaCamera)
        {
            if (config == null)
                return;

            RenderSettings.skybox = null;
            ClearLegacyBackdrop(arenaRoot);
            CombatArenaAtmosphereController.Ensure(arenaRoot)?.ClearOwnedSceneObjects();

            if (config.useTopTroopsBrightSky)
            {
                RenderSettings.fog = false;
                if (arenaCamera != null)
                {
                    arenaCamera.clearFlags = CameraClearFlags.SolidColor;
                    arenaCamera.backgroundColor = config.topTroopsSkyColor;
                }

                return;
            }

            ApplyGrim(config, arenaRoot, arenaCamera);
        }

        private static void ApplyGrim(CombatArenaConfigSO config, Transform arenaRoot, Camera arenaCamera)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.34f, 0.32f, 0.30f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.26f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.18f, 0.16f);
            RenderSettings.ambientIntensity = 1.05f;

            bool useFog = config.enableArenaFog;
            RenderSettings.fog = useFog;
            if (useFog)
            {
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = config.topTroopsSkyColor;
                float density = config.fogDensity > 0f ? config.fogDensity : 0.018f;
                RenderSettings.fogDensity = Mathf.Min(density, 0.022f);
            }

            if (arenaCamera != null)
            {
                arenaCamera.clearFlags = CameraClearFlags.SolidColor;
                arenaCamera.backgroundColor = config.topTroopsSkyColor;
            }

            ApplyLights(arenaRoot);
        }

        private static void ApplyLights(Transform arenaRoot)
        {
            if (arenaRoot == null)
                return;

            EnsureDirectionalLight(
                arenaRoot,
                KeyLightName,
                new Vector3(48f, -28f, 0f),
                0.62f,
                new Color(1f, 0.92f, 0.78f),
                LightShadows.Soft);

            EnsureDirectionalLight(
                arenaRoot,
                FillLightName,
                new Vector3(22f, 42f, 0f),
                0.24f,
                new Color(0.72f, 0.76f, 0.82f),
                LightShadows.None);

            EnsureDirectionalLight(
                arenaRoot,
                RimLightName,
                new Vector3(12f, 168f, 0f),
                0.16f,
                new Color(0.92f, 0.84f, 0.68f),
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

        private static void ClearLegacyBackdrop(Transform arenaRoot)
        {
            if (arenaRoot == null)
                return;

            var backdrop = arenaRoot.Find(BackdropRootName);
            if (backdrop == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(backdrop.gameObject);
            else
                Object.DestroyImmediate(backdrop.gameObject);
        }
    }
}
