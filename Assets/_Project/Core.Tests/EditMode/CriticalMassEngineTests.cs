using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CriticalMassEngineTests
    {
        [TearDown]
        public void TearDown() => CriticalMassRuleSource.ClearTestOverride();

        [Test]
        public void DefaultRules_ContainsAllThirtyEntries()
        {
            Assert.AreEqual(30, CriticalMassDefaultRules.Build().Length);
        }

        [Test]
        public void ResolveTier_HighestOnly_NotCumulative()
        {
            var tiers = new[]
            {
                new CriticalMassTier { Threshold = 5, Magnitude = 10 },
                new CriticalMassTier { Threshold = 7, Magnitude = 15 },
                new CriticalMassTier { Threshold = 10, Magnitude = 20 }
            };

            Assert.IsTrue(CriticalMassEngine.TryResolveTier(7, tiers, out int index, out var tier));
            Assert.AreEqual(1, index);
            Assert.AreEqual(15, tier.Magnitude);
        }

        [Test]
        public void FiveInfantry_GrantsTier1HpToInfantryOnly()
        {
            CriticalMassRuleSource.SetRulesForTests(new[]
            {
                new CriticalMassRuleDefinition
                {
                    Id = "infantry",
                    CountTagId = GameTagIds.Infantry,
                    CountCategory = CriticalMassCountCategory.Primary,
                    Tiers = new[]
                    {
                        new CriticalMassTier { Threshold = 5, Magnitude = 10 },
                        new CriticalMassTier { Threshold = 7, Magnitude = 15 },
                        new CriticalMassTier { Threshold = 10, Magnitude = 20 }
                    },
                    Stat = CriticalMassStat.MaxHp,
                    ModType = SynergyModType.Flat,
                    Scope = CriticalMassScope.FightCombat,
                    Target = new CriticalMassTargetFilter
                    {
                        PrimaryTagIds = new[] { GameTagIds.Infantry }
                    }
                }
            });

            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            var infantry = TestPieces.CreateUnit(
                "inf",
                primary: GameTagIds.Infantry,
                combatRole: GameTagIds.Assault,
                systemTag: GameTagIds.Combatant);
            var vehicle = TestPieces.CreateUnit(
                "veh",
                primary: GameTagIds.Vehicle,
                combatRole: GameTagIds.Tank,
                systemTag: GameTagIds.Combatant);

            for (int i = 0; i < 5; i++)
                Assert.IsTrue(board.TryPlace(infantry, TestBoards.SupportLineAnchor(i), $"inf{i}").Success);
            Assert.IsTrue(board.TryPlace(vehicle, TestBoards.SupportLineAnchor(5), "veh").Success);

            var snapshot = CriticalMassEngine.Evaluate(board);
            Assert.IsTrue(snapshot.HasAnyActiveRule);

            var combatants = new List<CombatantState>
            {
                new()
                {
                    InstanceId = "inf0",
                    Definition = infantry,
                    CurrentHp = infantry.MaxHp
                },
                new()
                {
                    InstanceId = "veh",
                    Definition = vehicle,
                    CurrentHp = vehicle.MaxHp
                }
            };

            CriticalMassEngine.ApplyToCombatants(snapshot, combatants);
            Assert.AreEqual(20, combatants[0].CurrentHp);
            Assert.AreEqual(10, combatants[1].CurrentHp);
        }

        [Test]
        public void CommandMass_GrantsAuthorityAtTier2()
        {
            CriticalMassRuleSource.SetRulesForTests(new[]
            {
                new CriticalMassRuleDefinition
                {
                    Id = "command",
                    CountTagId = GameTagIds.Command,
                    CountCategory = CriticalMassCountCategory.Synergy,
                    Tiers = new[]
                    {
                        new CriticalMassTier { Threshold = 2, Magnitude = 1 },
                        new CriticalMassTier { Threshold = 4, Magnitude = 3 },
                        new CriticalMassTier { Threshold = 6, Magnitude = 6 },
                        new CriticalMassTier { Threshold = 8, Magnitude = 10 }
                    },
                    Stat = CriticalMassStat.Authority,
                    ModType = SynergyModType.Flat,
                    Scope = CriticalMassScope.RunResources,
                    Target = CriticalMassTargetFilter.Any
                }
            });

            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            var commandPiece = TestPieces.CreateUnit(
                "cmd",
                synergyTags: new[] { GameTagIds.Command });

            for (int i = 0; i < 4; i++)
                Assert.IsTrue(board.TryPlace(commandPiece, TestBoards.SupportLineAnchor(i), $"cmd{i}").Success);

            var snapshot = CriticalMassEngine.Evaluate(board);
            Assert.AreEqual(3, snapshot.AuthorityBonus);
        }

        [Test]
        public void FightStartSnapshot_DoesNotChangeAfterPieceRemoved()
        {
            CriticalMassRuleSource.SetRulesForTests(new[]
            {
                new CriticalMassRuleDefinition
                {
                    Id = "infantry",
                    CountTagId = GameTagIds.Infantry,
                    CountCategory = CriticalMassCountCategory.Primary,
                    Tiers = new[] { new CriticalMassTier { Threshold = 5, Magnitude = 10 } },
                    Stat = CriticalMassStat.MaxHp,
                    ModType = SynergyModType.Flat,
                    Scope = CriticalMassScope.FightCombat,
                    Target = new CriticalMassTargetFilter { PrimaryTagIds = new[] { GameTagIds.Infantry } }
                }
            });

            var layout = BoardLayout.CreateHorizontalZones(9, 6, 3, 3, System.Array.Empty<GridCoord>());
            var board = new BoardState(layout);
            var infantry = TestPieces.CreateUnit("inf", primary: GameTagIds.Infantry, systemTag: GameTagIds.Combatant);

            for (int i = 0; i < 5; i++)
                Assert.IsTrue(board.TryPlace(infantry, TestBoards.SupportLineAnchor(i), $"p{i}").Success);

            var snapshot = CriticalMassEngine.Evaluate(board);
            Assert.IsTrue(snapshot.HasAnyActiveRule);
            Assert.IsTrue(board.TryRemove("p4", out _));

            var reevaluated = CriticalMassEngine.Evaluate(board);
            Assert.IsTrue(snapshot.HasAnyActiveRule);
            Assert.IsFalse(reevaluated.HasAnyActiveRule);
        }
    }
}
