using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
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
        public void ComputeUpkeep_IgnoresNonCombatantPieces()
        {
            var board = TestBoards.WithCommandBunker();
            int upkeep = ManpowerCalculator.ComputeUpkeep(board, Registry);
            Assert.AreEqual(10, upkeep);
        }

        [Test]
        public void ComputeFieldingRequirement_IncludesHqManpowerCost()
        {
            var board = TestBoards.WithBuildingAndRifle();
            Assert.AreEqual(10, ManpowerCalculator.ComputeFieldingRequirement(board, Registry));
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
        public void ComputeCasualties_BrokenUnit_CostsNothing()
        {
            // ADR-0005 mercy mechanic: a routed unit fled intact — no death cost, no
            // damage-taken attrition, even with heavy damage on the books.
            var rifle = TestPieces.RifleSquadTenMan();
            var combatants = new[]
            {
                new CombatantState
                {
                    InstanceId = "rifle_1",
                    Definition = rifle,
                    CurrentHp = 1,
                    DamageTakenThisFight = 99,
                    IsBroken = true
                }
            };
            Assert.AreEqual(0, ManpowerCalculator.ComputeCasualties(combatants));
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

        // ---------------------------------------------------------------
        // 2026-07-15 faction-roster-v1 §2.1 Field Hospital: post-fight, damaged-survivor
        // Manpower loss is reduced when field_hospital is fielded on the HQ board.
        // ---------------------------------------------------------------

        [Test]
        public void ComputeCasualties_WithFieldHospital_ReducesSurvivorLoss()
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

            var hqBoard = new BoardState(TestBoards.IronMarchHqLayout);
            hqBoard.TryPlace(TestPieces.FieldHospital(), new GridCoord(0, 0), "field_hospital_1");

            // Baseline (no hospital) is 2 (ComputeCasualties_Survivor_UsesDamageOverHpPerBody);
            // -50% PROVISIONAL floors to 1.
            Assert.AreEqual(1, ManpowerCalculator.ComputeCasualties(combatants, hqBoard));
        }

        [Test]
        public void ComputeCasualties_WithoutFieldHospital_UnaffectedByOptionalBoard()
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

            var hqBoard = new BoardState(TestBoards.IronMarchHqLayout);
            hqBoard.TryPlace(TestPieces.CommandOutpost(), new GridCoord(0, 0), "command_outpost_1");

            Assert.AreEqual(2, ManpowerCalculator.ComputeCasualties(combatants, hqBoard));
        }

        [Test]
        public void ComputeCasualties_FieldHospital_DoesNotReduceDeathCost()
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

            var hqBoard = new BoardState(TestBoards.IronMarchHqLayout);
            hqBoard.TryPlace(TestPieces.FieldHospital(), new GridCoord(0, 0), "field_hospital_1");

            Assert.AreEqual(10, ManpowerCalculator.ComputeCasualties(combatants, hqBoard), "insurance covers the wounded, not the dead");
        }
    }
}
