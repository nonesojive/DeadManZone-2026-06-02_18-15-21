using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class BoardPerTagAbilityTests
    {
        private static PieceAbilityDefinition SurgeonMedicHpAbility() => new()
        {
            Id = "surgeon_medic_hp_percent",
            Trigger = PieceAbilityTrigger.BoardPerTagCount,
            CountTagId = GameTagIds.Medic,
            NeighborFilter = new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
            Stat = SynergyStat.MaxHp,
            ModType = SynergyModType.Percent,
            Magnitude = 2
        };

        private static PieceAbilityDefinition FieldHospitalFightStartAbility() => new()
        {
            Id = "field_hospital_infantry_hp",
            Trigger = PieceAbilityTrigger.FightStart,
            NeighborFilter = new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
            Stat = SynergyStat.MaxHp,
            ModType = SynergyModType.Flat,
            Magnitude = 10
        };

        private static PieceDefinition CreateMedicTaggedBuilding() => new()
        {
            Id = "field_hospital",
            DisplayName = "Field Hospital",
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            SynergyTags = new[] { GameTagIds.Medic },
            MaxHp = 0,
            ManpowerCost = 0
        };

        private static PieceDefinition CreateFieldHospital() => new()
        {
            Id = "field_hospital",
            DisplayName = "Field Hospital",
            Category = PieceCategory.Building,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            SynergyTags = new[] { GameTagIds.Medic },
            Abilities = new[] { FieldHospitalFightStartAbility() },
            MaxHp = 0,
            ManpowerCost = 0
        };

        private static PieceDefinition InfantryWithMaxHp(int maxHp) => new()
        {
            Id = "infantry",
            DisplayName = "Infantry",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            Primary = GameTagIds.Infantry,
            Tags = new[] { GameTagIds.Infantry },
            MaxHp = maxHp,
            BaseDamage = 20,
            CooldownTicks = 3,
            ManpowerCost = 10
        };

        [Test]
        public void Surgeon_GrantsPercentHpPerMedicTag_OnBothBoards()
        {
            var infantry = InfantryWithMaxHp(100);
            var fieldMedic = TestPieces.CreateUnit(
                "field_medic",
                primary: GameTagIds.Support,
                synergyTags: new[] { GameTagIds.Medic });
            var surgeon = TestPieces.With(
                TestPieces.CreateUnit("ironmarch_surgeon", primary: GameTagIds.Support),
                abilities: new[] { SurgeonMedicHpAbility() });

            var combat = new BoardState(TestBoards.CombatLayout);
            Assert.IsTrue(combat.TryPlace(surgeon, TestBoards.CombatBoardAnchor(0, 0), "surgeon_1").Success);
            Assert.IsTrue(combat.TryPlace(fieldMedic, TestBoards.CombatBoardAnchor(1, 0), "medic_1").Success);
            Assert.IsTrue(combat.TryPlace(infantry, TestBoards.CombatBoardAnchor(2, 0), "infantry_1").Success);
            Assert.AreEqual(100, infantry.MaxHp);

            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            Assert.IsTrue(hq.TryPlace(CreateMedicTaggedBuilding(), new GridCoord(0, 0), "hospital_1").Success);

            var boards = new BuildBoardSet { Combat = combat, Hq = hq };
            var snapshot = PieceAbilityEngine.EvaluateFightStart(combat, boards);

            var combatants = new List<CombatantState>
            {
                new()
                {
                    InstanceId = "infantry_1",
                    Definition = infantry,
                    CurrentHp = infantry.MaxHp
                }
            };

            PieceAbilityEngine.ApplyToCombatants(snapshot, combatants);
            Assert.AreEqual(104, combatants[0].CurrentHp);
        }

        [Test]
        public void FieldHospital_AddsTenHpToAllInfantryWhenOnHqBoard()
        {
            var infantry = TestPieces.CreateUnit("infantry", primary: GameTagIds.Infantry);
            var combat = new BoardState(TestBoards.CombatLayout);
            Assert.IsTrue(combat.TryPlace(infantry, TestBoards.CombatBoardAnchor(0, 0), "infantry_1").Success);

            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            Assert.IsTrue(hq.TryPlace(CreateFieldHospital(), new GridCoord(0, 0), "hospital_1").Success);

            var boards = new BuildBoardSet { Combat = combat, Hq = hq };
            var enemy = new BoardState(TestBoards.Layout);
            var run = TickCombatRun.Start(combat, enemy, seed: 42, playerBuildBoards: boards);

            var playerInfantry = FindCombatant(run.PlayerCombatantsForTests, "infantry_1");
            Assert.AreEqual(20, playerInfantry.CurrentHp);
        }

        private static CombatantState FindCombatant(
            IReadOnlyList<CombatantState> combatants,
            string instanceId)
        {
            for (int i = 0; i < combatants.Count; i++)
            {
                if (combatants[i].InstanceId == instanceId)
                    return combatants[i];
            }

            Assert.Fail($"Missing combatant {instanceId}");
            return null;
        }
    }
}
