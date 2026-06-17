using DeadManZone.Core.Board;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Presentation.Tests.EditMode
{
    /// <summary>Guards Top Troops grim art config and readability tuning.</summary>
    public sealed class TopTroopsGrimArtProfileTests
    {
        [Test]
        public void LoadedCombatArenaConfig_UsesTopTroopsBattlefieldOnly()
        {
            var config = Resources.Load<CombatArenaConfigSO>("DeadManZone/CombatArenaConfig");
            Assert.NotNull(config, "CombatArenaConfig missing from Resources/DeadManZone/");
            Assert.IsTrue(config.useTopTroopsProceduralBattlefield,
                "Combat must stay on the Top Troops procedural battlefield path.");
        }

        [Test]
        public void LoadedCombatArenaConfig_UsesSyntyUnitsNotCapsules()
        {
            var config = Resources.Load<CombatArenaConfigSO>("DeadManZone/CombatArenaConfig");
            Assert.NotNull(config);
            Assert.IsFalse(config.useProceduralUnitVisuals,
                "Units should use Synty arena prefabs, not procedural capsule squads.");
        }

        [Test]
        public void LoadedCombatArenaConfig_ZonesReadableNotPrototypeBright()
        {
            var config = Resources.Load<CombatArenaConfigSO>("DeadManZone/CombatArenaConfig");
            Assert.NotNull(config);

            Assert.IsFalse(config.useTopTroopsBrightSky,
                "Bright prototype sky should be off for grim muddy combat.");
            Assert.Less(config.topTroopsSkyColor.b, config.topTroopsSkyColor.r + 0.08f,
                "Sky should read as smog brown-grey, not bright blue.");
            Assert.Greater(config.topTroopsPlayerZoneColor.r, config.topTroopsEnemyZoneColor.r,
                "Player zone should read warmer/lighter than enemy for board readability.");
        }

        [Test]
        public void DefaultBattlefieldPalette_ZonesAreDistinctAndReadable()
        {
            var palette = TopTroopsBattlefieldPalette.Default;
            float playerToEnemy = ColorDistance(palette.PlayerZoneColor, palette.EnemyZoneColor);
            float playerToNeutral = ColorDistance(palette.PlayerZoneColor, palette.NeutralZoneColor);

            Assert.Greater(playerToEnemy, 0.08f, "Player and enemy zones need visible separation.");
            Assert.Greater(playerToNeutral, 0.05f, "Neutral band should read between player and enemy.");
            Assert.Greater(palette.PlayerZoneColor.r, palette.EnemyZoneColor.r);
        }

        [Test]
        public void ResolveCellColor_CheckerboardAlternatesShade()
        {
            var layout = new BattlefieldLayout(3, 1, 3, 2);
            var palette = TopTroopsBattlefieldPalette.Default;

            Color even = TopTroopsBattlefieldBuilder.ResolveCellColor(layout, 0, 0, palette);
            Color odd = TopTroopsBattlefieldBuilder.ResolveCellColor(layout, 1, 0, palette);

            Assert.Greater(even.r, odd.r, "Checkerboard should alternate cell brightness for grid readability.");
        }

        private static float ColorDistance(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }

        [Test]
        public void PrefabResolver_ReturnsSyntyFallbackWhenProceduralUnitsDisabled()
        {
            var config = ScriptableObject.CreateInstance<CombatArenaConfigSO>();
            config.useProceduralUnitVisuals = false;
            config.fallbackUnitPrefabPath =
                "Assets/_Project/Art/Synty/Arena/Units/ArenaUnit_Rifle.prefab";

            try
            {
                var prefab = CombatArenaPrefabResolver.ResolveUnitPrefab(null, config);
                Assert.NotNull(prefab, "Synty rifle fallback should load when procedural visuals are off.");
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ApplyGrim_EnablesFogAndMuddyCameraBackground()
        {
            var config = ScriptableObject.CreateInstance<CombatArenaConfigSO>();
            config.useTopTroopsBrightSky = false;
            config.useTopTroopsProceduralBattlefield = true;
            config.enableArenaFog = true;
            config.fogDensity = 0.03f;
            config.topTroopsSkyColor = new Color(0.14f, 0.12f, 0.10f);

            var root = new GameObject("ArenaRoot");
            var cameraGo = new GameObject("ArenaCamera");
            var camera = cameraGo.AddComponent<Camera>();

            try
            {
                TopTroopsAtmosphere.Apply(config, root.transform, camera);

                Assert.IsTrue(RenderSettings.fog);
                Assert.That(RenderSettings.fogColor, Is.EqualTo(config.topTroopsSkyColor));
                Assert.AreEqual(CameraClearFlags.SolidColor, camera.clearFlags);
                Assert.That(camera.backgroundColor, Is.EqualTo(config.topTroopsSkyColor));
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ApplyBright_DisablesFog()
        {
            var config = ScriptableObject.CreateInstance<CombatArenaConfigSO>();
            config.useTopTroopsBrightSky = true;
            config.topTroopsSkyColor = new Color(0.53f, 0.81f, 0.92f);

            var root = new GameObject("ArenaRoot");
            var cameraGo = new GameObject("ArenaCamera");
            var camera = cameraGo.AddComponent<Camera>();

            try
            {
                TopTroopsAtmosphere.Apply(config, root.transform, camera);

                Assert.IsFalse(RenderSettings.fog);
                Assert.That(camera.backgroundColor, Is.EqualTo(config.topTroopsSkyColor));
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(config);
            }
        }
    }
}
