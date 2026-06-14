using System.Collections;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Data;
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
        public IEnumerator LoadAsync_SetsArenaActive()
        {
            _root = new GameObject("CombatArenaSceneLoaderRoot");
            var loader = _root.AddComponent<CombatArenaSceneLoader>();

            yield return loader.LoadAsync();

            Assert.IsTrue(loader.IsLoaded, "Arena loader should report loaded after LoadAsync.");
            Assert.IsTrue(CombatArenaSession.IsActive, "Arena mode should be active after LoadAsync.");
            Assert.IsTrue(
                SceneManager.GetSceneByName(GameScenes.CombatArena).isLoaded,
                "CombatArena scene should be loaded additively.");

            yield return loader.UnloadAsync();
            Assert.IsFalse(CombatArenaSession.IsActive, "Arena mode should be inactive after UnloadAsync.");
        }

        [UnityTest]
        public IEnumerator InitializeArena_ShowsHqFieldGunAndSupplyDepot()
        {
            var database = ContentDatabase.Load();
            if (database == null || database.Pieces.Count == 0)
            {
                Assert.Ignore("Generated ContentDatabase not found. Run DeadManZone/Generate Vertical Slice Content first.");
                yield break;
            }

            _root = new GameObject("CombatArenaSceneLoaderRoot");
            var loader = _root.AddComponent<CombatArenaSceneLoader>();
            yield return loader.LoadAsync();

            var presenterGo = new GameObject("CombatArenaPresenterRoot");
            var presenter = presenterGo.AddComponent<CombatArenaPresenter>();
            yield return null;

            var playerBoard = BuildBuildingShowcaseBoard(database);
            var enemyBoard = BuildMinimalEnemyBoard(database);
            var battlefield = BattlefieldState.FromBoards(playerBoard, enemyBoard);

            presenter.InitializeArena(battlefield);

            Assert.IsTrue(
                presenter.HasBuildingVisualForTests("hq_player"),
                "HQ should spawn a static building visual in the arena.");
            Assert.IsTrue(
                presenter.HasBuildingVisualForTests("supply_1"),
                "Supply depot should spawn a static building visual in the arena.");
            Assert.IsTrue(
                presenter.GetActiveActors().Any(actor => actor.InstanceId == "field_gun_1"),
                "Field gun nest should spawn as a combat arena unit actor.");

            yield return loader.UnloadAsync();
        }

        private static BoardState BuildBuildingShowcaseBoard(ContentDatabase database)
        {
            var faction = database.GetFaction("iron_vanguard");
            Assert.NotNull(faction, "iron_vanguard faction required for arena building test.");

            var board = new BoardState(faction.CreateBoardLayout());
            Place(board, database, "ironmarch_hq", new GridCoord(0, 4), "hq_player");
            Place(board, database, "field_gun_nest", new GridCoord(3, 2), "field_gun_1");
            Place(board, database, "supply_depot", new GridCoord(2, 6), "supply_1");
            return board;
        }

        private static BoardState BuildMinimalEnemyBoard(ContentDatabase database)
        {
            var faction = database.GetFaction("iron_vanguard");
            var board = new BoardState(faction.CreateBoardLayout());
            Place(board, database, "ironmarch_hq", new GridCoord(0, 4), "enemy_hq");
            return board;
        }

        private static void Place(
            BoardState board,
            ContentDatabase database,
            string pieceId,
            GridCoord anchor,
            string instanceId)
        {
            var piece = database.Pieces.First(p => p.id == pieceId).ToCore();
            var result = board.TryPlace(piece, anchor, instanceId);
            Assert.IsTrue(result.Success, $"Failed to place {pieceId} at {anchor}: {result.Reason}");
        }
    }
}
