using System.Collections;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Game;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadManZone.PlayMode.Tests
{
    public sealed class CombatArenaBackdropPlayModeTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            PlayModeTestHelpers.CleanupPersistentManagers();
            CombatArenaSession.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);

            var arenaScene = SceneManager.GetSceneByName(GameScenes.CombatArena);
            if (arenaScene.isLoaded)
                SceneManager.UnloadSceneAsync(arenaScene);

            CombatArenaSession.ResetForTests();
            PlayModeTestHelpers.CleanupPersistentManagers();
        }

        [UnityTest]
        public IEnumerator FrameBattlefield_SpawnsGrimBackdrop_WhenProfileEnabled()
        {
            _root = new GameObject("CombatArenaSceneLoaderRoot");
            var loader = _root.AddComponent<CombatArenaSceneLoader>();
            yield return loader.LoadAsync();

            var bootstrap = CombatArenaBootstrap.Instance;
            Assert.NotNull(bootstrap, "Combat arena bootstrap should exist after load.");

            var config = bootstrap.Config;
            if (config?.atmosphereProfile == null || !config.atmosphereProfile.enableBackdrop)
            {
                Assert.Ignore("CombatArenaConfig lacks an atmosphere profile with backdrop enabled.");
                yield break;
            }

            var layout = BattlefieldLayout.FromPlayerBoard(
                BoardLayout.CreateHorizontalZones(
                    width: 9,
                    height: 10,
                    rearCols: 4,
                    supportCols: 3,
                    specialTiles: new GridCoord[0]));

            bootstrap.FrameBattlefield(layout);
            yield return null;

            var backdrop = bootstrap.transform.Find("CombatArenaBackdrop");
            Assert.NotNull(backdrop, "Grim backdrop root should spawn when framing the battlefield.");

            int dressingCount = backdrop.Find("TrenchDressing")?.childCount ?? 0;
            int skylineCount = backdrop.Find("SkylineBackdrop")?.childCount ?? 0;
            Assert.Greater(dressingCount, 0, "Trench dressing ring should contain props.");
            Assert.Greater(skylineCount, 0, "Skyline ring should contain distant ruins.");

            var atmosphereFx = backdrop.Find("AtmosphereFx");
            if (config.atmosphereProfile.enableAtmosphereFx)
                Assert.NotNull(atmosphereFx, "Atmosphere FX root expected when enabled.");
            else if (atmosphereFx != null)
                Assert.AreEqual(0, atmosphereFx.childCount, "Atmosphere FX should stay empty when disabled.");

            yield return loader.UnloadAsync();
        }
    }
}
