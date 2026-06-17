using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Combat Arena Atmosphere Profile")]
    public sealed class CombatArenaAtmosphereProfileSO : ScriptableObject
    {
        [Header("Fog")]
        public bool enableFog = true;
        public Color fogColor = new(0.08f, 0.075f, 0.07f);
        [Range(0.005f, 0.08f)]
        public float fogDensity = 0.018f;

        [Header("Ambient")]
        public Color ambientSkyColor = new(0.40f, 0.43f, 0.48f);
        public Color ambientEquatorColor = new(0.30f, 0.30f, 0.30f);
        public Color ambientGroundColor = new(0.16f, 0.14f, 0.12f);
        [Range(0.5f, 1.5f)]
        public float ambientIntensity = 1f;

        [Header("Key light")]
        public Vector3 keyLightEuler = new(13f, -42f, 0f);
        public float keyLightIntensity = 1.05f;
        public Color keyLightColor = new(1f, 0.88f, 0.72f);
        public LightShadows keyLightShadows = LightShadows.Soft;
        public float keyLightShadowStrength = 0.78f;

        [Header("Fill light")]
        public Vector3 fillLightEuler = new(28f, 48f, 0f);
        public float fillLightIntensity = 0.22f;
        public Color fillLightColor = new(0.62f, 0.68f, 0.78f);

        [Header("Rim light")]
        public Vector3 rimLightEuler = new(8f, 168f, 0f);
        public float rimLightIntensity = 0.12f;
        public Color rimLightColor = new(0.82f, 0.72f, 0.58f);

        [Header("Post-processing")]
        public bool enablePostProcessing;
        public VolumeProfile postVolumeProfile;
        public string postVolumeProfilePath =
            "Assets/_Project/Data/Resources/DeadManZone/CombatArenaVolumeProfile.asset";
        public float postVolumePriority = 8f;

        [Header("Backdrop")]
        public bool enableBackdrop = true;
        public int backdropSeed = 424242;
        [Tooltip("Per-ring prefab catalogs. When empty, legacy built-in PolygonWar paths are used.")]
        public CombatArenaBackdropRingSO[] backdropRings;

        [Header("Atmosphere FX")]
        public bool enableAtmosphereFx;
        [Range(0, 6)]
        public int maxAtmosphereFxCount;

        /// <summary>Applies diorama-aligned grim defaults for editor bootstrap and tests.</summary>
        public void ApplyGrimDefaults()
        {
            enableFog = true;
            // Combat readability: lighter fog than the diorama showcase.
            fogColor = new Color(0.08f, 0.075f, 0.07f);
            fogDensity = 0.018f;
            ambientSkyColor = new Color(0.28f, 0.30f, 0.33f);
            ambientEquatorColor = new Color(0.20f, 0.19f, 0.18f);
            ambientGroundColor = new Color(0.10f, 0.09f, 0.08f);
            ambientIntensity = 0.95f;
            keyLightEuler = new Vector3(13f, -42f, 0f);
            keyLightIntensity = 1.05f;
            keyLightColor = new Color(1f, 0.88f, 0.72f);
            keyLightShadows = LightShadows.Soft;
            keyLightShadowStrength = 0.78f;
            fillLightEuler = new Vector3(28f, 48f, 0f);
            fillLightIntensity = 0.22f;
            fillLightColor = new Color(0.62f, 0.68f, 0.78f);
            rimLightEuler = new Vector3(8f, 168f, 0f);
            rimLightIntensity = 0.12f;
            rimLightColor = new Color(0.82f, 0.72f, 0.58f);
            enablePostProcessing = false;
            postVolumeProfilePath =
                "Assets/_Project/Data/Resources/DeadManZone/CombatArenaVolumeProfile.asset";
            postVolumePriority = 8f;
            enableBackdrop = true;
            backdropSeed = 424242;
            enableAtmosphereFx = false;
            maxAtmosphereFxCount = 0;
        }
    }
}
