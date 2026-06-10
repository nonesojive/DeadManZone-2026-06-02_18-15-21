using System.Collections;
using DeadManZone.Game;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadManZone.PlayMode.Tests
{
    public sealed class CombatArenaPlayModeTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            PlayModeTestHelpers.CleanupPersistentManagers();
            CombatPresentationMode.ArenaActive = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);

            var arenaScene = SceneManager.GetSceneByName(GameScenes.CombatArena);
            if (arenaScene.isLoaded)
                SceneManager.UnloadSceneAsync(arenaScene);

            CombatPresentationMode.ArenaActive = false;
            PlayModeTestHelpers.CleanupPersistentManagers();
        }

        [UnityTest]
        public IEnumerator LoadAsync_SetsArenaActive()
        {
            _root = new GameObject("CombatArenaSceneLoaderRoot");
            var loader = _root.AddComponent<CombatArenaSceneLoader>();

            yield return loader.LoadAsync();

            Assert.IsTrue(loader.IsLoaded, "Arena loader should report loaded after LoadAsync.");
            Assert.IsTrue(CombatPresentationMode.ArenaActive, "Arena mode should be active after LoadAsync.");
            Assert.IsTrue(
                SceneManager.GetSceneByName(GameScenes.CombatArena).isLoaded,
                "CombatArena scene should be loaded additively.");

            yield return loader.UnloadAsync();
            Assert.IsFalse(CombatPresentationMode.ArenaActive, "Arena mode should be inactive after UnloadAsync.");
        }
    }
}
