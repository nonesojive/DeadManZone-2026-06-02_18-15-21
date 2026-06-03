using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class ManpowerCalculatorTests
    {
        private static ContentRegistry Registry => TestContentRegistry.Instance;

        [Test]
        public void ComputeUpkeep_SumsManpowerCostPerCombatantOnBoard()
        {
            var board = TestBoards.StandardPlayer();
            int upkeep = ManpowerCalculator.ComputeUpkeep(board, Registry);
            Assert.Greater(upkeep, 0);
        }

        [Test]
        public void CanStartBattle_FalseWhenUpkeepExceedsManpower()
        {
            var board = TestBoards.StrongPlayerVsWeakEnemy();
            Assert.IsFalse(ManpowerCalculator.CanStartBattle(board, manpower: 1, Registry));
        }

        [Test]
        public void CanStartBattle_TrueWhenManpowerMeetsUpkeep()
        {
            var board = TestBoards.StandardPlayer();
            Assert.IsTrue(ManpowerCalculator.CanStartBattle(board, manpower: 1, Registry));
        }

        [Test]
        public void ComputeUpkeep_IgnoresNonCombatantPieces()
        {
            var board = TestBoards.WithCommandBunker();
            int upkeep = ManpowerCalculator.ComputeUpkeep(board, Registry);
            Assert.AreEqual(1, upkeep);
        }

        [Test]
        public void RefundSurvivors_SumsManpowerCostForListedSurvivors()
        {
            var board = TestBoards.StrongPlayerVsWeakEnemy();
            int refund = ManpowerCalculator.RefundSurvivors(
                board,
                new[] { "player_rifle_2" },
                Registry);
            Assert.AreEqual(1, refund);
        }
    }
}
