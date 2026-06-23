using System.Collections;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadManZone.PlayMode.Tests
{
    public sealed class CombatArena2DLoadPlayModeTests
    {
        [UnityTest]
        public IEnumerator CombatArena2D_SceneLoads_WithBootstrap()
        {
            var config = Resources.Load<CombatArenaConfigSO>("DeadManZone/CombatArenaConfig");
            Assume.That(config, Is.Not.Null);

            string sceneName = GameScenes.ResolveCombatArenaScene(config);
            Assume.That(sceneName, Is.EqualTo(GameScenes.CombatArena2D).Or.EqualTo(GameScenes.CombatArena));

            if (SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(sceneName);
            }

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (op != null && !op.isDone)
                yield return null;

            var bootstrap = Object.FindFirstObjectByType<CombatArenaBootstrap>();
            Assert.IsNotNull(bootstrap, "CombatArenaBootstrap should exist in arena scene");
            Assert.IsNotNull(bootstrap.ArenaCamera, "Arena camera should be assigned");

            yield return SceneManager.UnloadSceneAsync(sceneName);
        }
    }
}
