using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadManZone.Presentation.Visual
{
    [Serializable]
    public struct MenuLightEntry
    {
        public string lightName;
        public LightType lightType;
        public Color color;
        public float intensity;
        public float range;
        public Vector3 localPosition;
        public Vector3 eulerRotation;
    }

    [CreateAssetMenu(menuName = "DeadManZone/Visual/Menu Lighting")]
    public sealed class MenuLightingSO : ScriptableObject
    {
        public List<MenuLightEntry> lights = new();

        public void ApplyToEnvironment(Transform menuEnvironmentRoot)
        {
            if (menuEnvironmentRoot == null)
                return;

            foreach (var entry in lights)
            {
                if (string.IsNullOrEmpty(entry.lightName))
                    continue;

                var lightTransform = menuEnvironmentRoot.Find(entry.lightName);
                if (lightTransform == null)
                    continue;

                var light = lightTransform.GetComponent<Light>();
                if (light == null)
                    continue;

                light.type = entry.lightType;
                light.color = entry.color;
                light.intensity = entry.intensity;
                light.range = entry.range;
                lightTransform.localPosition = entry.localPosition;
                lightTransform.localRotation = Quaternion.Euler(entry.eulerRotation);
            }
        }

        public void CaptureFromEnvironment(Transform menuEnvironmentRoot)
        {
            lights.Clear();
            if (menuEnvironmentRoot == null)
                return;

            for (var i = 0; i < menuEnvironmentRoot.childCount; i++)
            {
                var child = menuEnvironmentRoot.GetChild(i);
                var light = child.GetComponent<Light>();
                if (light == null)
                    continue;

                lights.Add(new MenuLightEntry
                {
                    lightName = child.name,
                    lightType = light.type,
                    color = light.color,
                    intensity = light.intensity,
                    range = light.range,
                    localPosition = child.localPosition,
                    eulerRotation = child.localEulerAngles
                });
            }
        }
    }
}
