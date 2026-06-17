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
    public sealed class CombatArenaGridPlayModeTests
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
        public IEnumerator FrameBattlefield_SpawnsCheckerboardGrid_WhenEnabled()
        {
            _root = new GameObject("CombatArenaSceneLoaderRoot");
            var loader = _root.AddComponent<CombatArenaSceneLoader>();
            yield return loader.LoadAsync();

            var bootstrap = CombatArenaBootstrap.Instance;
            Assert.NotNull(bootstrap, "Combat arena bootstrap should exist after load.");

            var config = bootstrap.Config;
            if (config == null || !config.showCheckerboardGrid)
            {
                Assert.Ignore("CombatArenaConfig lacks checkerboard grid enabled.");
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

            var grid = bootstrap.transform.Find("CombatArenaGrid");
            Assert.NotNull(grid, "Checkerboard grid root should spawn when framing the battlefield.");

            var meshFilter = grid.GetComponent<MeshFilter>();
            var meshRenderer = grid.GetComponent<MeshRenderer>();
            Assert.NotNull(meshFilter, "Grid should have a mesh filter.");
            Assert.NotNull(meshRenderer, "Grid should have a mesh renderer.");
            Assert.NotNull(meshFilter.sharedMesh, "Grid mesh should be built.");
            Assert.Greater(meshFilter.sharedMesh.vertexCount, 0, "Grid mesh should contain vertices.");
            Assert.GreaterOrEqual(meshRenderer.sharedMaterials.Length, 2, "Grid should use light/dark materials.");

            yield return loader.UnloadAsync();
        }
    }
}
