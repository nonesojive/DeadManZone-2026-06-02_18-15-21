#if UNITY_EDITOR
using DeadManZone.Core.Run;
using DeadManZone.Game;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// Everything an Arena Theme is allowed to vary (M4): lighting/fog RenderSettings,
    /// ground palette, crater layout, and which prop sets build. Everything a theme is
    /// NOT allowed to vary is simply absent — camera, flat combat strip, key light and
    /// value structure have no knobs here (CONTEXT.md "Arena Theme": hue and props only).
    /// Editor-time only: profiles feed the scene bootstraps and CombatEnvironmentBuilder;
    /// nothing at runtime reads them.
    /// </summary>
    public sealed class ArenaThemeProfile
    {
        public string ThemeId;
        public string SceneName;

        // ---- RenderSettings (per-scene; the arena scene owns them while active) ----
        public Color AmbientSky, AmbientEquator, AmbientGround;
        public Color FogColor;
        public float FogDensity;
        /// <summary>Slightly above FogColor — a bg at raw fog color reads as a dark seam
        /// where the fog line meets the terrain crest (Trenchline lesson).</summary>
        public Color CameraBackground;

        // ---- Ground palette (baked into the world-mapped albedo) ----
        public Color Mud, Dry, Scorch;

        // ---- Terrain: shell craters (x, z, radius, depth), all outside the flat strip ----
        public Vector4[] Craters;

        // ---- Prop sets (shared machinery, toggled per theme) ----
        public bool Trenchworks, WireLines, TankTraps, TelegraphPoles, RuinBackdrop;

        /// <summary>Theme asset folder. Trenchline keeps the pre-M4 flat layout so the
        /// existing scene's asset references stay valid; new themes get subfolders.</summary>
        public string EnvFolder => ThemeId == ArenaThemes.Trenchline
            ? "Assets/_Project/Combat3D/Environment"
            : $"Assets/_Project/Combat3D/Environment/{ThemeId}";

        /// <summary>Theme material path. Same Trenchline-keeps-legacy-paths rule.</summary>
        public string MaterialPath(string name) => ThemeId == ArenaThemes.Trenchline
            ? $"Assets/_Project/Combat3D/Combat3D_{name}.mat"
            : $"{EnvFolder}/Combat3D_{name}.mat";
    }

    public static class CombatArenaThemeProfiles
    {
        // Trenchline crater field — the pre-M4 authored layout, verbatim.
        private static readonly Vector4[] TrenchlineCraters =
        {
            new(-9f, 15f, 3.2f, 0.9f), new(4f, 19f, 4.0f, 1.1f), new(14f, 14f, 2.6f, 0.7f),
            new(-20f, 22f, 3.4f, 1.0f), new(24f, 24f, 4.4f, 1.2f), new(-2f, 28f, 3.0f, 0.8f),
            new(10f, 31f, 3.6f, 1.0f), new(-13f, 34f, 4.2f, 1.1f),
            new(-26f, 6f, 3.0f, 0.9f), new(27f, 2f, 3.4f, 1.0f),
            new(-24f, -6f, 2.8f, 0.8f), new(25f, -9f, 3.0f, 0.9f),
            new(-8f, -14f, 3.2f, 0.9f), new(7f, -16f, 3.8f, 1.0f),
        };

        /// <summary>Today's arena, exactly — palette/lighting/craters lifted verbatim from
        /// the pre-M4 constants so a rebuild regenerates the shipped look.</summary>
        public static readonly ArenaThemeProfile Trenchline = new()
        {
            ThemeId = ArenaThemes.Trenchline,
            SceneName = GameScenes.CombatArena3D,
            AmbientSky = new Color(0.38f, 0.44f, 0.56f),
            AmbientEquator = new Color(0.42f, 0.40f, 0.38f),
            AmbientGround = new Color(0.24f, 0.21f, 0.19f),
            FogColor = new Color(0.14f, 0.16f, 0.20f),
            FogDensity = 0.022f,
            CameraBackground = new Color(0.17f, 0.19f, 0.24f),
            Mud = new Color(0.105f, 0.088f, 0.068f),
            Dry = new Color(0.255f, 0.22f, 0.155f),
            Scorch = new Color(0.075f, 0.066f, 0.058f),
            Craters = TrenchlineCraters,
            Trenchworks = true, WireLines = true, TankTraps = true,
            TelegraphPoles = true, RuinBackdrop = true,
        };

        /// <summary>Ash Wraiths signature ground: heavy cold fog, sparse dead remnants,
        /// the horizon dissolved. Density ~2.5x Trenchline — the far wire line should be
        /// a silhouette, units must stay fully readable (~mid strip is the fog budget).</summary>
        public static readonly ArenaThemeProfile FogField = new()
        {
            ThemeId = ArenaThemes.FogField,
            SceneName = GameScenes.CombatArenaFogField,
            AmbientSky = new Color(0.38f, 0.42f, 0.42f),
            AmbientEquator = new Color(0.37f, 0.39f, 0.38f),
            AmbientGround = new Color(0.21f, 0.22f, 0.21f),
            // Cold grey-green murk (Ash Wraiths breath), darker than v1 — the pale grey
            // fog over lifted ambient read as an overcast SNOWFIELD, not dread.
            FogColor = new Color(0.225f, 0.250f, 0.245f),
            FogDensity = 0.052f,
            CameraBackground = new Color(0.25f, 0.275f, 0.27f),
            Mud = new Color(0.100f, 0.096f, 0.082f),
            Dry = new Color(0.185f, 0.180f, 0.150f),
            Scorch = new Color(0.070f, 0.070f, 0.064f),
            Craters = new Vector4[]
            {
                new(-11f, 17f, 3.4f, 0.8f), new(8f, 22f, 3.8f, 0.9f),
                new(-22f, 8f, 3.0f, 0.8f), new(24f, -4f, 3.2f, 0.9f),
                new(2f, -14f, 3.0f, 0.8f),
            },
            Trenchworks = false, WireLines = true, TankTraps = false,
            TelegraphPoles = false, RuinBackdrop = false,
        };

        /// <summary>Crimson Legion signature ground: a shelled town — battered facades and
        /// rubble flank the strip, warm dust haze, chimney silhouettes. The strip itself
        /// stays an open street.</summary>
        public static readonly ArenaThemeProfile RavagedTown = new()
        {
            ThemeId = ArenaThemes.RavagedTown,
            SceneName = GameScenes.CombatArenaRavagedTown,
            AmbientSky = new Color(0.44f, 0.42f, 0.40f),
            AmbientEquator = new Color(0.42f, 0.39f, 0.36f),
            AmbientGround = new Color(0.24f, 0.22f, 0.20f),
            FogColor = new Color(0.19f, 0.17f, 0.15f),
            FogDensity = 0.024f,
            CameraBackground = new Color(0.22f, 0.20f, 0.18f),
            Mud = new Color(0.115f, 0.108f, 0.100f),
            Dry = new Color(0.235f, 0.222f, 0.198f),
            Scorch = new Color(0.070f, 0.068f, 0.065f),
            Craters = TrenchlineCraters,
            Trenchworks = false, WireLines = false, TankTraps = true,
            TelegraphPoles = true, RuinBackdrop = true,
        };

        /// <summary>Splintered woodland: shattered trunks and deadfall in a green-grey
        /// murk, a dark tree line for a horizon. No earthworks — the war passed through
        /// here, nobody dug in.</summary>
        public static readonly ArenaThemeProfile WartornForest = new()
        {
            ThemeId = ArenaThemes.WartornForest,
            SceneName = GameScenes.CombatArenaWartornForest,
            AmbientSky = new Color(0.34f, 0.40f, 0.36f),
            AmbientEquator = new Color(0.38f, 0.39f, 0.34f),
            AmbientGround = new Color(0.20f, 0.20f, 0.17f),
            FogColor = new Color(0.13f, 0.15f, 0.13f),
            FogDensity = 0.026f,
            CameraBackground = new Color(0.16f, 0.18f, 0.16f),
            Mud = new Color(0.095f, 0.088f, 0.060f),
            Dry = new Color(0.200f, 0.190f, 0.120f),
            Scorch = new Color(0.060f, 0.060f, 0.050f),
            Craters = new Vector4[]
            {
                new(-14f, 16f, 3.0f, 0.8f), new(6f, 20f, 3.6f, 1.0f), new(19f, 13f, 2.8f, 0.7f),
                new(-25f, 5f, 3.0f, 0.9f), new(26f, -7f, 2.8f, 0.8f), new(-6f, -15f, 3.2f, 0.9f),
            },
            Trenchworks = false, WireLines = true, TankTraps = false,
            TelegraphPoles = false, RuinBackdrop = false,
        };

        /// <summary>Wave-1 build order for the scene bootstrap.</summary>
        public static readonly ArenaThemeProfile[] All =
        {
            Trenchline, FogField, RavagedTown, WartornForest
        };
    }
}
#endif
