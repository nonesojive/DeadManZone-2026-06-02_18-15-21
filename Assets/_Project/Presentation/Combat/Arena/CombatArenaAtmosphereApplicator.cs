using DeadManZone.Data;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_URP_PRESENT
using UnityEngine.Rendering.Universal;
#endif

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Applies grim trench atmosphere: fog, ambient, lights, and URP post volume.</summary>
    internal static class CombatArenaAtmosphereApplicator
    {
        private const string KeyLightName = "ArenaKeyLight";
        private const string FillLightName = "ArenaFillLight";
        private const string RimLightName = "ArenaRimLight";
        private const string VolumeName = "ArenaPostVolume";

        public static void Apply(
            CombatArenaAtmosphereProfileSO profile,
            CombatArenaConfigSO config,
            Transform arenaRoot,
            Camera arenaCamera)
        {
            if (profile == null)
            {
                CombatArenaEnvironment.Apply(config, arenaRoot, arenaCamera);
                return;
            }

            ApplyAmbient(profile);
            ApplyFog(profile);
            ApplyLights(profile, arenaRoot);
            ApplyPostVolume(profile, arenaRoot);
            ApplyCameraBackground(arenaCamera, profile);
        }

        public static void ClearAtmosphere(Transform arenaRoot)
        {
            if (arenaRoot == null)
                return;

            DestroyLightObject(arenaRoot.Find(KeyLightName)?.gameObject);
            DestroyLightObject(arenaRoot.Find(FillLightName)?.gameObject);
            DestroyLightObject(arenaRoot.Find(RimLightName)?.gameObject);
            DestroyLightObject(arenaRoot.Find(VolumeName)?.gameObject);
            DestroyLightObject(arenaRoot.Find("ArenaLight")?.gameObject);
        }

        private static void ApplyAmbient(CombatArenaAtmosphereProfileSO profile)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = profile.ambientSkyColor;
            RenderSettings.ambientEquatorColor = profile.ambientEquatorColor;
            RenderSettings.ambientGroundColor = profile.ambientGroundColor;
            RenderSettings.ambientIntensity = profile.ambientIntensity;
        }

        private static void ApplyFog(CombatArenaAtmosphereProfileSO profile)
        {
            RenderSettings.fog = profile.enableFog;
            if (!profile.enableFog)
                return;

            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = profile.fogColor;
            RenderSettings.fogDensity = profile.fogDensity;
        }

        private static void ApplyLights(CombatArenaAtmosphereProfileSO profile, Transform arenaRoot)
        {
            if (arenaRoot == null)
                return;

            var stale = arenaRoot.Find("ArenaLight");
            if (stale != null)
                DestroyLightObject(stale.gameObject);

            var key = EnsureDirectionalLight(arenaRoot, KeyLightName);
            ConfigureLight(key, profile.keyLightEuler, profile.keyLightIntensity,
                profile.keyLightColor, profile.keyLightShadows, profile.keyLightShadowStrength);

            var fill = EnsureDirectionalLight(arenaRoot, FillLightName);
            ConfigureLight(fill, profile.fillLightEuler, profile.fillLightIntensity,
                profile.fillLightColor, LightShadows.None, 0f);

            var rim = EnsureDirectionalLight(arenaRoot, RimLightName);
            ConfigureLight(rim, profile.rimLightEuler, profile.rimLightIntensity,
                profile.rimLightColor, LightShadows.None, 0f);
        }

        private static void ApplyPostVolume(CombatArenaAtmosphereProfileSO profile, Transform arenaRoot)
        {
            if (arenaRoot == null)
                return;

            var existing = arenaRoot.Find(VolumeName);
            if (existing != null)
                DestroyLightObject(existing.gameObject);

            if (!profile.enablePostProcessing)
                return;

            var volumeProfile = profile.postVolumeProfile
                ?? LoadVolumeProfile(profile.postVolumeProfilePath);
            if (volumeProfile == null)
                return;

            var volumeGo = new GameObject(VolumeName);
            volumeGo.transform.SetParent(arenaRoot, false);
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = profile.postVolumePriority;
            volume.sharedProfile = volumeProfile;
        }

        private static VolumeProfile LoadVolumeProfile(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>(assetPath);
#else
            return null;
#endif
        }

        private static void ApplyCameraBackground(Camera arenaCamera, CombatArenaAtmosphereProfileSO profile)
        {
            if (arenaCamera == null || RenderSettings.skybox != null)
                return;

            arenaCamera.clearFlags = CameraClearFlags.SolidColor;
            arenaCamera.backgroundColor = profile.enableFog
                ? profile.fogColor
                : new Color(0.06f, 0.055f, 0.05f);
        }

        private static Light EnsureDirectionalLight(Transform arenaRoot, string lightName)
        {
            var existing = arenaRoot.Find(lightName);
            if (existing != null)
                DestroyLightObject(existing.gameObject);

            var lightGo = new GameObject(lightName);
            lightGo.transform.SetParent(arenaRoot, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            return light;
        }

        private static void DestroyLightObject(GameObject target)
        {
            if (target == null)
                return;

            // Lights are runtime-spawned only; remove immediately so Find() does not
            // return a stale shell on the same frame (Destroy is deferred in play mode).
            Object.DestroyImmediate(target);
        }

        private static void ConfigureLight(
            Light light,
            Vector3 eulerAngles,
            float intensity,
            Color color,
            LightShadows shadows,
            float shadowStrength)
        {
            if (light == null)
                return;

            light.transform.rotation = Quaternion.Euler(eulerAngles);
            light.intensity = intensity;
            light.color = color;
            light.shadows = shadows;
            light.shadowStrength = shadowStrength;
        }
    }
}
