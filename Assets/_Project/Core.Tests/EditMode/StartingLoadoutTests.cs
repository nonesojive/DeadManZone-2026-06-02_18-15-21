using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Run;
using DeadManZone.Data;
using DeadManZone.Game;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>Faction starting loadout: pieces pre-placed at run start (free, upkeep
    /// applies), buildings on the HQ board, units on the combat board, and the first
    /// muster already counting the starting economy.</summary>
    public sealed class StartingLoadoutTests
    {
        private ContentDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _database = ContentDatabase.Load();
            if (_database == null || _database.Pieces.Count == 0)
                Assert.Ignore(DeadManZoneTestContent.MissingDatabaseHint);

            SaveManager.DeleteSave();
        }

        [TearDown]
        public void TearDown() => SaveManager.DeleteSave();

        [Test]
        public void StartNewRun_Ironmarch_PrePlacesTheStartingLoadout()
        {
            var faction = _database.GetFaction(FactionIds.IronmarchUnion);
            if (faction.startingPieces == null || faction.startingPieces.Length == 0)
                Assert.Ignore("ironmarch_union.asset has no starting loadout yet — regenerate content.");

            var orchestrator = new RunOrchestrator(_database);
            orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 42);

            var combat = orchestrator.GetCombatBoard();
            var hq = orchestrator.GetHqBoard();

            Assert.IsTrue(combat.Pieces.Any(p => p.InstanceId == "start_field_medic"),
                "field medic starts on the combat board");
            Assert.IsTrue(combat.Pieces.Any(p => p.InstanceId == "start_conscript_rifleman"),
                "conscript rifleman starts on the combat board");
            Assert.IsTrue(hq.Pieces.Any(p => p.InstanceId == "start_supply_depot"),
                "supply depot starts on the HQ board");
            Assert.IsTrue(hq.Pieces.Any(p => p.InstanceId == "start_command_outpost"),
                "command outpost starts on the HQ board");

            Assert.AreEqual(faction.startingSupplies, orchestrator.State.Supplies,
                "the starting loadout is free — no supplies were charged");
        }

        [Test]
        public void StartNewRun_LoadoutPlacesBeforeMuster_SoEconomyPiecesCanFeedIt()
        {
            // The ordering guarantee (loadout → muster) matters when a faction ever starts
            // with a MusterPerShop piece; the current depot is a supplies building, so the
            // assertion is only that muster ran and produced at least the base.
            var orchestrator = new RunOrchestrator(_database);
            orchestrator.StartNewRun(FactionIds.IronmarchUnion, runSeed: 42);

            var faction = _database.GetFaction(FactionIds.IronmarchUnion);
            Assert.GreaterOrEqual(orchestrator.State.LastMusterGained, faction.baseMusterPerShop);
        }
    }
}
