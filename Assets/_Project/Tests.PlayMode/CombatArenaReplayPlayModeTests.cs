using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
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
    public sealed class CombatArenaReplayPlayModeTests
    {
        private GameObject _root;

        private sealed class ArenaHarness
        {
            public GameObject Root;
            public CombatArenaSceneLoader Loader;
            public CombatArenaPresenter Presenter;
            public CombatDirector Director;
        }

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

            var arenaScene = SceneManager.GetSceneByName(GameScenes.CombatArena2D);
            if (arenaScene.isLoaded)
                SceneManager.UnloadSceneAsync(arenaScene);

            CombatArenaSession.ResetForTests();
            PlayModeTestHelpers.CleanupPersistentManagers();
        }

        [UnityTest]
        public IEnumerator RestoreState_SnapsActorToReplayedMoveAnchor()
        {
            var database = RequireDatabase();
            if (database == null)
                yield break;

            var harness = new ArenaHarness();
            yield return LoadArena(harness, withDirector: false);
            _root = harness.Root;

            var battlefield = CombatArenaTestBoards.BuildFieldGunVsHq(database);
            var destination = new GridCoord(5, 2);
            var events = new List<CombatEvent>
            {
                new()
                {
                    Segment = 0,
                    Tick = 0,
                    ActorId = "field_gun_1",
                    ActionType = "move",
                    TargetId = $"{destination.X},{destination.Y}"
                }
            };

            harness.Presenter.RestoreState(battlefield, events);

            var actor = FindActor(harness.Presenter, "field_gun_1");
            Assert.NotNull(actor, "Field gun actor should exist after RestoreState.");
            Assert.AreEqual(destination, actor.Anchor, "RestoreState should snap the actor to the replayed move anchor.");
        }

        [UnityTest]
        public IEnumerator PlayLog_Move_UpdatesActorAnchor()
        {
            var database = RequireDatabase();
            if (database == null)
                yield break;

            var harness = new ArenaHarness();
            yield return LoadArena(harness, withDirector: true);
            _root = harness.Root;

            harness.Presenter.InitializeArena(CombatArenaTestBoards.BuildFieldGunVsHq(database));

            var destination = new GridCoord(5, 2);
            var log = new CombatEventLog();
            log.Append(0, 0, "field_gun_1", "move", $"{destination.X},{destination.Y}", 0);

            harness.Director.PlayLog(log, segment: 0);
            yield return new WaitUntil(() => !harness.Director.IsPlaying);

            var actor = FindActor(harness.Presenter, "field_gun_1");
            Assert.NotNull(actor, "Actor should exist after move replay.");
            Assert.AreEqual(destination, actor.Anchor, "Move replay should update the actor anchor.");
        }

        [UnityTest]
        public IEnumerator InitializeArena_ConscriptRifleman_RendersSingleFrameScale()
        {
            var database = RequireDatabase();
            if (database == null)
                yield break;

            var harness = new ArenaHarness();
            yield return LoadArena(harness, withDirector: false);
            _root = harness.Root;

            harness.Presenter.InitializeArena(CombatArenaTestBoards.BuildFieldGunVsRifle(database));
            yield return null;

            var actor = FindActor(harness.Presenter, "enemy_rifle_1");
            Assert.NotNull(actor, "Enemy conscript actor should exist after arena initialization.");

            var quad = actor
                .GetComponentsInChildren<Transform>(includeInactive: false)
                .FirstOrDefault(child => child.name == "Quad");
            Assert.NotNull(quad, "Enemy conscript should render through a sprite quad.");

            // A full 4096 sheet rendered as one sprite was roughly 2.7 units tall.
            Assert.Less(Mathf.Abs(quad.localScale.y), 2.1f, "Conscript quad should be a sliced frame, not the full sprite sheet.");
            Assert.Less(Mathf.Abs(quad.localScale.x), 2.1f, "Conscript quad width should be a sliced frame, not the full sprite sheet.");
        }

        [UnityTest]
        public IEnumerator PlayLog_Destroyed_RemovesActor()
        {
            var database = RequireDatabase();
            if (database == null)
                yield break;

            var harness = new ArenaHarness();
            yield return LoadArena(harness, withDirector: true);
            _root = harness.Root;

            harness.Presenter.InitializeArena(CombatArenaTestBoards.BuildFieldGunVsHq(database));

            var log = new CombatEventLog();
            log.Append(0, 0, "field_gun_1", "damage", "enemy_rifle_1", 8);
            log.Append(0, 1, "field_gun_1", "destroyed", string.Empty, 0);

            harness.Director.PlayLog(log, segment: 0);
            yield return new WaitUntil(() => !harness.Director.IsPlaying);
            yield return new WaitUntil(() => FindActor(harness.Presenter, "field_gun_1") == null);

            Assert.IsNull(
                FindActor(harness.Presenter, "field_gun_1"),
                "Destroyed replay should remove the targeted unit actor.");
        }

        [UnityTest]
        public IEnumerator PlayLog_MoveDamageDestroyed_ReplaysFullSequence()
        {
            var database = RequireDatabase();
            if (database == null)
                yield break;

            var harness = new ArenaHarness();
            yield return LoadArena(harness, withDirector: true);
            _root = harness.Root;

            harness.Presenter.InitializeArena(CombatArenaTestBoards.BuildFieldGunVsHq(database));

            var destination = new GridCoord(5, 2);
            var log = new CombatEventLog();
            log.Append(0, 0, "field_gun_1", "move", $"{destination.X},{destination.Y}", 0);
            log.Append(0, 1, "field_gun_1", "damage", "enemy_rifle_1", 8);
            log.Append(0, 2, "field_gun_1", "destroyed", string.Empty, 0);

            harness.Director.PlayLog(log, segment: 0);
            yield return new WaitUntil(() => !harness.Director.IsPlaying);
            yield return new WaitUntil(() => FindActor(harness.Presenter, "field_gun_1") == null);

            Assert.IsNull(
                FindActor(harness.Presenter, "field_gun_1"),
                "Full move/damage/destroy replay should remove the targeted unit actor.");
        }

        [UnityTest]
        public IEnumerator UnloadAsync_ClearsActorsAndBuildingVisuals()
        {
            var database = RequireDatabase();
            if (database == null)
                yield break;

            _root = new GameObject("CombatArenaLoaderRoot");
            var loader = _root.AddComponent<CombatArenaSceneLoader>();
            var presenter = _root.AddComponent<CombatArenaPresenter>();

            yield return loader.LoadAsync();
            presenter.InitializeArena(CombatArenaTestBoards.BuildFieldGunVsHq(database));

            Assert.IsNotNull(FindActor(presenter, "field_gun_1"));
            Assert.Greater(presenter.GetActiveActors().Count(), 0);

            yield return loader.UnloadAsync();

            Assert.IsFalse(presenter.GetActiveActors().Any());
        }

        [UnityTest]
        public IEnumerator PlayLog_WithoutArenaSession_DoesNotMoveActor()
        {
            var database = RequireDatabase();
            if (database == null)
                yield break;

            _root = new GameObject("CombatArenaOfflineRoot");
            var director = _root.AddComponent<CombatDirector>();
            var presenter = _root.AddComponent<CombatArenaPresenter>();
            presenter.Configure(director, null);
            director.SetSecondsPerTickForTests(0f);

            yield return SceneManager.LoadSceneAsync(GameScenes.CombatArena2D, LoadSceneMode.Additive);
            yield return null;

            var start = new GridCoord(3, 2);
            var destination = new GridCoord(5, 2);
            presenter.InitializeArena(CombatArenaTestBoards.BuildFieldGunVsHq(database));

            var log = new CombatEventLog();
            log.Append(0, 0, "field_gun_1", "move", $"{destination.X},{destination.Y}", 0);
            director.PlayLog(log, segment: 0);
            yield return new WaitUntil(() => !director.IsPlaying);

            var actor = FindActor(presenter, "field_gun_1");
            Assert.NotNull(actor);
            Assert.AreEqual(
                start,
                actor.Anchor,
                "Presenter should ignore director events when CombatArenaSession is inactive.");
        }

        private static IEnumerator LoadArena(ArenaHarness harness, bool withDirector)
        {
            harness.Root = new GameObject("CombatArenaLoaderRoot");
            harness.Loader = harness.Root.AddComponent<CombatArenaSceneLoader>();

            if (withDirector)
            {
                harness.Director = harness.Root.AddComponent<CombatDirector>();
                harness.Director.SetSecondsPerTickForTests(0f);
            }

            harness.Presenter = harness.Root.AddComponent<CombatArenaPresenter>();

            if (withDirector)
                harness.Presenter.Configure(harness.Director, null);

            yield return harness.Loader.LoadAsync();
            yield return null;
        }

        private static CombatUnitActor FindActor(CombatArenaPresenter presenter, string instanceId) =>
            presenter.GetActiveActors().FirstOrDefault(actor => actor.InstanceId == instanceId);

        private static ContentDatabase RequireDatabase()
        {
            var database = ContentDatabase.Load();
            if (database == null || database.Pieces.Count == 0)
            {
                Assert.Ignore(DeadManZoneTestContent.MissingDatabaseHint);
                return null;
            }

            return database;
        }
    }
}
