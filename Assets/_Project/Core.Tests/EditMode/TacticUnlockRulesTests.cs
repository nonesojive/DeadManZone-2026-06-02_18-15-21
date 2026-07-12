using DeadManZone.Core;
using DeadManZone.Core.Combat;
using DeadManZone.Data;
using DeadManZone.Game;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class TacticUnlockRulesTests
    {
        [Test]
        public void IsUnlocked_EmptyList_UnlocksAllTactics()
        {
            var faction = ScriptableObject.CreateInstance<FactionSO>();
            faction.startingTactics = System.Array.Empty<TacticType>();

            Assert.IsTrue(TacticUnlockRules.IsUnlocked(faction, TacticType.ProtectSupport));
            Assert.IsTrue(TacticUnlockRules.IsUnlocked(faction, TacticType.StandGround));
        }

        [Test]
        public void IsUnlocked_NullFaction_UnlocksAllTactics()
        {
            Assert.IsTrue(TacticUnlockRules.IsUnlocked(null, TacticType.ProtectSupport));
        }

        [Test]
        public void IsUnlocked_NullStartingTactics_UnlocksAllTactics()
        {
            var faction = ScriptableObject.CreateInstance<FactionSO>();
            faction.startingTactics = null;

            Assert.IsTrue(TacticUnlockRules.IsUnlocked(faction, TacticType.ProtectSupport));
        }

        [Test]
        public void IsUnlocked_IronmarchUnionAsset_MatchesStartingTactics()
        {
            var database = ContentDatabase.Load();
            if (database == null)
            {
                Assert.Ignore(DeadManZoneTestContent.MissingDatabaseHint);
                return;
            }

            var faction = database.GetFaction(FactionIds.IronmarchUnion);
            Assert.NotNull(faction);

            Assert.IsTrue(TacticUnlockRules.IsUnlocked(faction, TacticType.StandGround));
            Assert.IsTrue(TacticUnlockRules.IsUnlocked(faction, TacticType.Advance));
            Assert.IsTrue(TacticUnlockRules.IsUnlocked(faction, TacticType.DisciplinedFire));
            Assert.IsFalse(TacticUnlockRules.IsUnlocked(faction, TacticType.ProtectSupport));
        }

        [Test]
        public void TacticPauseValidator_RejectsLockedTactic()
        {
            var startingTactics = new[]
            {
                TacticType.StandGround,
                TacticType.Advance,
                TacticType.DisciplinedFire
            };

            var validator = new TacticPauseValidator();
            Assert.IsFalse(validator.ValidatePause(
                TacticType.ProtectSupport,
                TacticType.DisciplinedFire,
                hasCommandPiece: true,
                checkpointIndex: 0,
                authority: 5,
                abilities: null,
                out var reason,
                startingTactics));
            Assert.That(reason, Does.Contain("not unlocked").IgnoreCase);
        }

        [Test]
        public void BeginCombat_DefaultPlayerTactic_IsDisciplinedFireForIronmarch()
        {
            var database = ContentDatabase.Load();
            if (database == null)
            {
                Assert.Ignore(DeadManZoneTestContent.MissingDatabaseHint);
                return;
            }

            SaveManager.DeleteSave();
            var orchestrator = new RunOrchestrator(database);
            orchestrator.StartNewRun(FactionIds.IronmarchUnion);

            var board = orchestrator.GetCombatBoard();
            Assert.IsTrue(board.TryPlace(
                TestPieces.RifleSquad(),
                TestBoards.CombatBoardAnchor(5, 3),
                "rifle_1").Success);
            orchestrator.SaveCombatBoard(board);

            orchestrator.ChooseFightOption(1);
            orchestrator.BeginCombat();

            Assert.AreEqual(TacticType.DisciplinedFire, orchestrator.State.Combat.PlayerTactic);
            SaveManager.DeleteSave();
        }
    }
}
