using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class BuffStripEvaluatorTests
    {
        [TearDown]
        public void TearDown() => CriticalMassRuleSource.ClearTestOverride();

        [Test]
        public void Evaluate_BuildBoardSet_CountsCombatAndHqTogether()
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
                        new CriticalMassTier { Threshold = 4, Magnitude = 3 }
                    },
                    Stat = CriticalMassStat.Authority,
                    ModType = SynergyModType.Flat,
                    Scope = CriticalMassScope.RunResources,
                    Target = CriticalMassTargetFilter.Any
                }
            });

            var combat = new BoardState(TestBoards.CombatLayout);
            var hq = new BoardState(TestBoards.IronMarchHqLayout);
            var commandBuilding = new PieceDefinition
            {
                Id = "command_post",
                DisplayName = "Command Post",
                Category = PieceCategory.Building,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                SynergyTags = new[] { GameTagIds.Command },
                MaxHp = 20
            };

            Assert.IsTrue(hq.TryPlace(commandBuilding, new GridCoord(0, 0), "hq0").Success);
            Assert.IsTrue(hq.TryPlace(commandBuilding, new GridCoord(1, 0), "hq1").Success);

            var boards = new BuildBoardSet { Combat = combat, Hq = hq };
            Assert.AreEqual(2, boards.ToAggregateBoard().Pieces.Count());

            var engineSnapshot = CriticalMassEngine.Evaluate(boards);
            var engineRule = engineSnapshot.Rules.Single(r => r.Rule.Id == "command");
            Assert.AreEqual(2, engineRule.Count);
            Assert.IsTrue(engineRule.IsActive);

            var combatOnly = BuffStripEvaluator.Evaluate(combat);
            var aggregate = BuffStripEvaluator.Evaluate(boards);

            Assert.IsFalse(combatOnly.Any(e => e.RuleId == "command"));
            var aggregateEntry = aggregate.Single(e => e.RuleId == "command");
            Assert.AreEqual(2, aggregateEntry.CurrentCount);
            Assert.IsTrue(aggregateEntry.IsActive);
            Assert.AreEqual(4, aggregateEntry.ProgressThreshold);
        }

        [Test]
        public void Evaluate_ExcludesActiveAbilityAuras()
        {
            SetInfantryRule();

            var command = TestPieces.With(
                TestPieces.CreateUnit(
                    "command",
                    synergyTags: new[] { GameTagIds.Command }),
                abilities: new[]
                {
                    new PieceAbilityDefinition
                    {
                        Id = "command_adjacent_artillery_damage_plus_two",
                        Trigger = PieceAbilityTrigger.AdjacentAura,
                        NeighborFilter = new NeighborFilter { CombatRoleTagId = GameTagIds.Artillery },
                        Stat = SynergyStat.Damage,
                        ModType = SynergyModType.Flat,
                        Magnitude = 2
                    }
                });
            var artillery = TestPieces.CreateUnit(
                "artillery",
                combatRole: GameTagIds.Artillery);

            var board = new BoardState(TestBoards.CombatLayout);
            Assert.IsTrue(board.TryPlace(command, TestBoards.CombatBoardAnchor(0, 0), "cmd").Success);
            Assert.IsTrue(board.TryPlace(artillery, TestBoards.CombatBoardAnchor(1, 0), "art").Success);

            var entries = BuffStripEvaluator.Evaluate(board);

            Assert.IsFalse(entries.Any(e => e.RuleId == "command_adjacent_artillery_damage_plus_two"));
            Assert.IsFalse(entries.Any(e => e.DetailText.StartsWith("Active ability:")));
        }

        [Test]
        public void Evaluate_ActiveTier_ShowsProgressTowardNextThreshold()
        {
            SetInfantryRule();

            var board = new BoardState(TestBoards.CombatLayout);
            var infantry = TestPieces.CreateUnit(
                "inf",
                primary: GameTagIds.Infantry);

            for (int i = 0; i < 6; i++)
                Assert.IsTrue(board.TryPlace(infantry, TestBoards.CombatBoardAnchor(i % 6, i / 6), $"p{i}").Success);

            var entry = BuffStripEvaluator.Evaluate(board).Single(e => e.RuleId == "infantry");

            Assert.IsTrue(entry.IsActive);
            Assert.AreEqual(6, entry.CurrentCount);
            Assert.AreEqual(7, entry.ProgressThreshold);
            Assert.AreEqual("6/7", BuffStripEvaluator.FormatProgressLabel(entry));
        }

        [Test]
        public void Evaluate_NearMiss_ShowsNextThreshold()
        {
            SetInfantryRule();

            var board = new BoardState(TestBoards.CombatLayout);
            var infantry = TestPieces.CreateUnit(
                "inf",
                primary: GameTagIds.Infantry);

            for (int i = 0; i < 4; i++)
                Assert.IsTrue(board.TryPlace(infantry, TestBoards.CombatBoardAnchor(i, 0), $"p{i}").Success);

            var entry = BuffStripEvaluator.Evaluate(board).Single(e => e.RuleId == "infantry");

            Assert.IsFalse(entry.IsActive);
            Assert.AreEqual(4, entry.CurrentCount);
            Assert.AreEqual(5, entry.ProgressThreshold);
        }

        [Test]
        public void CountActive_OnlyIncludesTriggeredTiers()
        {
            SetInfantryRule();
            SetSupportRule();

            var combat = new BoardState(TestBoards.CombatLayout);
            var infantry = TestPieces.CreateUnit(
                "inf",
                primary: GameTagIds.Infantry);
            var support = TestPieces.CreateUnit(
                "sup",
                combatRole: GameTagIds.Support);

            for (int i = 0; i < 5; i++)
                Assert.IsTrue(combat.TryPlace(infantry, TestBoards.CombatBoardAnchor(i, 0), $"inf{i}").Success);
            Assert.IsTrue(combat.TryPlace(support, TestBoards.CombatBoardAnchor(5, 0), "sup0").Success);

            var boards = new BuildBoardSet { Combat = combat };
            Assert.AreEqual(1, BuffStripEvaluator.CountActive(boards));
        }

        private static void SetInfantryRule()
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
        }

        private static void SetSupportRule()
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
                },
                new CriticalMassRuleDefinition
                {
                    Id = "support",
                    CountTagId = GameTagIds.Support,
                    CountCategory = CriticalMassCountCategory.CombatRole,
                    Tiers = new[] { new CriticalMassTier { Threshold = 3, Magnitude = 1 } },
                    Stat = CriticalMassStat.AttackSpeed,
                    ModType = SynergyModType.TierStep,
                    Scope = CriticalMassScope.FightCombat,
                    Target = new CriticalMassTargetFilter { PrimaryTagIds = new[] { GameTagIds.Infantry } }
                }
            });
        }
    }
}
