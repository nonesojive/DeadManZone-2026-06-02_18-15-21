#if UNITY_URP_PRESENT
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Builds the combat arena's post-processing grade at runtime: a grimdark
    /// filmic look (ACES tonemap, subtle desat + contrast), bloom so the additive
    /// muzzle/tracer/explosion VFX glow, a framing vignette, and faint film grain.
    /// The scene's authored volume profile was empty and renderPostProcessing did
    /// nothing — this makes the enabled post stack actually do something.</summary>
    internal static class CombatArenaPostFx
    {
        private const string VolumeName = "CombatArenaPostFx";

        public static void Ensure(Transform arenaRoot)
        {
            if (arenaRoot == null || !CombatArenaMaterialUtility.IsUrpActive())
                return;

            var existing = arenaRoot.Find(VolumeName);
            var go = existing != null
                ? existing.gameObject
                : new GameObject(VolumeName);
            if (existing == null)
                go.transform.SetParent(arenaRoot, false);

            var volume = go.GetComponent<Volume>() ?? go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 1f;

            var profile = volume.profile;
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.hideFlags = HideFlags.HideAndDontSave;
                volume.profile = profile;
            }

            ConfigureTonemapping(profile);
            ConfigureBloom(profile);
            ConfigureVignette(profile);
            ConfigureColorGrade(profile);
            ConfigureSplitToning(profile);
            ConfigureFilmGrain(profile);
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
            => profile.TryGet<T>(out var component) ? component : profile.Add<T>(true);

        private static void ConfigureTonemapping(VolumeProfile profile)
        {
            var tm = GetOrAdd<Tonemapping>(profile);
            tm.mode.overrideState = true;
            tm.mode.value = TonemappingMode.ACES; // filmic rolloff so bright VFX don't blow out flat
        }

        private static void ConfigureBloom(VolumeProfile profile)
        {
            var bloom = GetOrAdd<Bloom>(profile);
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.85f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.55f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.62f;
            bloom.tint.overrideState = true;
            bloom.tint.value = new Color(1f, 0.9f, 0.74f); // warm muzzle/fire glow
        }

        private static void ConfigureVignette(VolumeProfile profile)
        {
            var vig = GetOrAdd<Vignette>(profile);
            vig.intensity.overrideState = true;
            vig.intensity.value = 0.3f;
            vig.smoothness.overrideState = true;
            vig.smoothness.value = 0.5f;
            vig.color.overrideState = true;
            vig.color.value = new Color(0.04f, 0.03f, 0.02f);
        }

        private static void ConfigureColorGrade(VolumeProfile profile)
        {
            var ca = GetOrAdd<ColorAdjustments>(profile);
            ca.postExposure.overrideState = true;
            ca.postExposure.value = 0.04f;
            ca.contrast.overrideState = true;
            ca.contrast.value = 11f;
            ca.saturation.overrideState = true;
            ca.saturation.value = -10f; // muted, war-torn palette
            ca.colorFilter.overrideState = true;
            ca.colorFilter.value = new Color(1f, 0.96f, 0.88f); // faint sepia warmth
        }

        private static void ConfigureSplitToning(VolumeProfile profile)
        {
            var st = GetOrAdd<SplitToning>(profile);
            st.shadows.overrideState = true;
            st.shadows.value = new Color(0.28f, 0.34f, 0.42f); // cool steel shadows
            st.highlights.overrideState = true;
            st.highlights.value = new Color(0.55f, 0.46f, 0.30f); // warm dust highlights
            st.balance.overrideState = true;
            st.balance.value = -6f;
        }

        private static void ConfigureFilmGrain(VolumeProfile profile)
        {
            var fg = GetOrAdd<FilmGrain>(profile);
            fg.type.overrideState = true;
            fg.type.value = FilmGrainLookup.Medium1;
            fg.intensity.overrideState = true;
            fg.intensity.value = 0.15f; // faint war-photo texture
            fg.response.overrideState = true;
            fg.response.value = 0.8f;
        }
    }
}
#endif
