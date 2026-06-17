using System.Collections;
using System.Linq;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Presentation.Combat;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadManZone.PlayMode.Tests
{
    public sealed class IronVanguardSlicePlayModeTests
    {
        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");

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
        public IEnumerator SliceBoard_SpawnsRifleWithAnimator()
        {
            var database = RequireDatabase();
            if (database == null)
                yield break;

            _root = new GameObject("SliceTestRoot");
            var loader = _root.AddComponent<CombatArenaSceneLoader>();
            yield return loader.LoadAsync();

            var presenterGo = new GameObject("Presenter");
            var presenter = presenterGo.AddComponent<CombatArenaPresenter>();
            yield return null;

            presenter.InitializeArena(CombatSliceLayouts.BuildIronVanguardSkirmish(database));

            var rifle = presenter.GetActiveActors().FirstOrDefault(a => a.InstanceId == "rifle_1");
            Assert.NotNull(rifle, "Player rifle should spawn.");
            var animator = rifle.GetComponentInChildren<Animator>();
            Assert.NotNull(animator, "Rifle should have an Animator.");
            Assert.IsNotNull(animator.runtimeAnimatorController, "Animator needs AC_CombatArena_Infantry.");

            yield return loader.UnloadAsync();
        }

        [UnityTest]
        public IEnumerator PlayLog_Move_SetsRifleWalkingDuringReplay()
        {
            var database = RequireDatabase();
            if (database == null)
                yield break;

            _root = new GameObject("SliceMoveTestRoot");
            var loader = _root.AddComponent<CombatArenaSceneLoader>();
            var director = _root.AddComponent<CombatDirector>();
            director.SetSecondsPerTickForTests(0f);
            var presenter = _root.AddComponent<CombatArenaPresenter>();
            presenter.Configure(director, null);

            yield return loader.LoadAsync();
            yield return null;

            presenter.InitializeArena(CombatSliceLayouts.BuildIronVanguardSkirmish(database));

            var rifle = presenter.GetActiveActors().FirstOrDefault(a => a.InstanceId == "rifle_1");
            Assert.NotNull(rifle, "Player rifle should spawn for move replay test.");
            var animator = rifle.GetComponentInChildren<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                Assert.Ignore("Rifle animator not configured. Run Create Combat Infantry Animator + Assign to Arena Units.");
                yield break;
            }

            var destination = new GridCoord(3, 3);
            var log = new CombatEventLog();
            log.Append(0, 0, "rifle_1", "move", $"{destination.X},{destination.Y}", 0);

            director.PlayLog(log, segment: 0);

            bool sawWalking = false;
            float deadline = Time.realtimeSinceStartup + 2f;
            while (Time.realtimeSinceStartup < deadline && director.IsPlaying)
            {
                if (animator.GetBool(IsWalkingHash) || animator.GetFloat(MoveSpeedHash) > 0.01f)
                {
                    sawWalking = true;
                    break;
                }

                yield return null;
            }

            yield return new WaitUntil(() => !director.IsPlaying);

            Assert.IsTrue(sawWalking, "Move replay should drive rifle walk animation parameters.");
            Assert.AreEqual(destination, rifle.Anchor, "Move replay should update rifle anchor.");

            yield return loader.UnloadAsync();
        }

        private static ContentDatabase RequireDatabase()
        {
            var database = ContentDatabase.Load();
            if (database == null || database.Pieces.Count == 0)
            {
                Assert.Ignore("Generated ContentDatabase not found. Run DeadManZone/Generate Vertical Slice Content first.");
                return null;
            }

            return database;
        }
    }
}
