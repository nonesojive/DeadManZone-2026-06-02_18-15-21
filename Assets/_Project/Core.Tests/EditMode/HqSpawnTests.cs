using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using DeadManZone.Game;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class HqSpawnTests
    {
        [Test]
        public void StartNewRun_PlacesHqAtFactionAnchor()
        {
            var database = ContentDatabase.Load();
            if (database == null)
            {
                Assert.Ignore("ContentDatabase not found.");
                return;
            }

            SaveManager.DeleteSave();
            var orchestrator = new RunOrchestrator(database);
            orchestrator.StartNewRun("iron_vanguard", runSeed: 1);
            var board = orchestrator.GetPlayerBoard();
            var hq = System.Linq.Enumerable.FirstOrDefault(board.Pieces, p => p.Definition.Tags.Contains(GameTags.Hq));
            Assert.IsNotNull(hq);
            Assert.AreEqual(new GridCoord(0, 4), hq.Anchor);
        }

        [Test]
        public void TryMovePlacedPiece_RejectsHq()
        {
            var database = ContentDatabase.Load();
            if (database == null)
            {
                Assert.Ignore("ContentDatabase not found.");
                return;
            }

            SaveManager.DeleteSave();
            var orchestrator = new RunOrchestrator(database);
            orchestrator.StartNewRun("iron_vanguard", runSeed: 1);
            Assert.IsFalse(orchestrator.TryMovePlacedPiece("hq_player", new GridCoord(2, 2)));
        }

        [Test]
        public void TrySellPlacedPiece_RejectsHq()
        {
            var database = ContentDatabase.Load();
            if (database == null)
            {
                Assert.Ignore("ContentDatabase not found.");
                return;
            }

            SaveManager.DeleteSave();
            var orchestrator = new RunOrchestrator(database);
            orchestrator.StartNewRun("iron_vanguard", runSeed: 1);
            Assert.IsFalse(orchestrator.TrySellPlacedPiece("hq_player"));
        }
    }
}
