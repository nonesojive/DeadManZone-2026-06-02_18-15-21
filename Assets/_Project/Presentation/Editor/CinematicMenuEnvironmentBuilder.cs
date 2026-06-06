using DeadManZone.Presentation.Visual;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Presentation.Editor
{
    internal static class CinematicMenuEnvironmentBuilder
    {
        internal static GameObject Build()
        {
            var profile = VisualProfilePresetFactory.EnsureDefaultProfile();
            profile.mainMenuAtmosphere?.ApplyToRenderSettings();

            var root = new GameObject("MenuEnvironment");
            var lighting = profile.mainMenuLighting;
            var hasLightingEntries = lighting?.lights != null && lighting.lights.Count > 0;

            if (hasLightingEntries)
            {
                foreach (var entry in lighting.lights)
                    CreateLightFromEntry(root.transform, entry);
            }
            else
            {
                CreateKeyLight(root.transform);
                CreateFillLight(root.transform);
                CreateRimLight(root.transform);
            }

            if (lighting != null && !hasLightingEntries)
            {
                lighting.CaptureFromEnvironment(root.transform);
                EditorUtility.SetDirty(lighting);
            }

            return root;
        }

        private static void CreateLightFromEntry(Transform parent, MenuLightEntry entry)
        {
            var lightName = string.IsNullOrEmpty(entry.lightName) ? "Light" : entry.lightName;
            var lightGo = new GameObject(lightName);
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.localPosition = entry.localPosition;
            lightGo.transform.localRotation = Quaternion.Euler(entry.eulerRotation);

            var light = lightGo.AddComponent<Light>();
            light.type = entry.lightType;
            light.color = entry.color;
            light.intensity = entry.intensity;
            light.range = entry.range;
        }

        private static void CreateKeyLight(Transform parent)
        {
            var lightGo = new GameObject("KeyLight");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.82f, 0.58f);
            light.intensity = 0.55f;
        }

        private static void CreateFillLight(Transform parent)
        {
            var lightGo = new GameObject("FillLight");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.localPosition = new Vector3(-2.5f, 1.8f, 1f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.95f, 0.65f, 0.35f);
            light.intensity = 1.4f;
            light.range = 12f;
        }

        private static void CreateRimLight(Transform parent)
        {
            var lightGo = new GameObject("RimLight");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.localPosition = new Vector3(2.5f, 2f, -1f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.55f, 0.45f, 0.32f);
            light.intensity = 0.9f;
            light.range = 10f;
        }
    }
}
