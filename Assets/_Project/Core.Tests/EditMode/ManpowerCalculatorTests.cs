using DeadManZone.Core.Combat;
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
        public void CanStartBattle_FalseWhenFieldingExceedsManpower()
        {
            var board = TestBoards.WithHqAndRifle();
            Assert.IsFalse(ManpowerCalculator.CanStartBattle(board, manpower: 17, Registry));
        }

        [Test]
        public void CanStartBattle_TrueWhenManpowerMeetsFielding()
        {
            var board = TestBoards.WithHqAndRifle();
            Assert.IsTrue(ManpowerCalculator.CanStartBattle(board, manpower: 18, Registry));
        }

        [Test]
        public void ComputeUpkeep_IgnoresNonCombatantPieces()
        {
            var board = TestBoards.WithCommandBunker();
            int upkeep = ManpowerCalculator.ComputeUpkeep(board, Registry);
            Assert.AreEqual(18, upkeep);
        }

        [Test]
        public void ComputeFieldingRequirement_IncludesHqManpowerCost()
        {
            var board = TestBoards.WithHqAndRifle();
            Assert.AreEqual(18, ManpowerCalculator.ComputeFieldingRequirement(board, Registry));
        }

        [Test]
        public void ComputeCasualties_Survivor_UsesDamageOverHpPerBody()
        {
            var rifle = TestPieces.RifleSquadTenMan();
            var combatants = new[]
            {
                new CombatantState
                {
                    InstanceId = "rifle_1",
                    Definition = rifle,
                    CurrentHp = 78,
                    DamageTakenThisFight = 22
                }
            };
            Assert.AreEqual(2, ManpowerCalculator.ComputeCasualties(combatants));
        }

        [Test]
        public void ComputeCasualties_Destroyed_AlwaysCostsFullSquad()
        {
            var rifle = TestPieces.RifleSquadTenMan();
            var combatants = new[]
            {
                new CombatantState
                {
                    InstanceId = "rifle_1",
                    Definition = rifle,
                    CurrentHp = 0,
                    DamageTakenThisFight = 15
                }
            };
            Assert.AreEqual(10, ManpowerCalculator.ComputeCasualties(combatants));
        }

        [Test]
        public void ComputeCasualties_CapsSurvivorLossAtManpowerCost()
        {
            var rifle = TestPieces.RifleSquadTenMan();
            var combatants = new[]
            {
                new CombatantState
                {
                    InstanceId = "rifle_1",
                    Definition = rifle,
                    CurrentHp = 1,
                    DamageTakenThisFight = 99
                }
            };
            Assert.AreEqual(9, ManpowerCalculator.ComputeCasualties(combatants));
        }
    }
}
