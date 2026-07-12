using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>Pure M2 Fight Option rules: seeded option generation, the Battle
    /// Condition deck, tier Dread grants, easy-tier enemy-engine suppression, and
    /// save-schema round trips — no ContentDatabase required.</summary>
    public sealed class FightOptionTests
    {
        // ---- FightOptionGenerator ----

        private static FightOptionArmySource Source(int fightNumber, string factionId = "ironmarch_union") =>
            new()
            {
                FightNumber = fightNumber,
                EnemyFactionId = factionId,
                BuildBoard = () => BoardWithRifles(fightNumber)
            };

        private static BoardState BoardWithRifles(int count)
        {
            var board = new BoardState(TestBoards.CombatLayout);
            for (int i = 0; i < count; i++)
                Assert.IsTrue(
                    board.TryPlace(TestPieces.RifleSquad(), new GridCoord(i, 0), $"enemy_{i}").Success,
                    $"rifle {i} must place");
            return board;
        }

        private static List<FightOptionArmySource> FourAuthoredArmies() =>
            new() { Source(1), Source(2), Source(3), Source(4) };

        private static string Fingerprint(IEnumerable<FightOptionRecord> options) =>
            string.Join(";", options.Select(o =>
                $"{o.Tier}|{o.EnemyFactionId}|{o.TemplateFightNumber}|{o.ConditionId}|{o.StrengthPreview}"));

        [Test]
        public void Generate_SameSeedAndRound_IsDeterministic()
        {
            var first = FightOptionGenerator.Generate(4242, 3, dread: 2, FourAuthoredArmies());
            var second = FightOptionGenerator.Generate(4242, 3, dread: 2, FourAuthoredArmies());

            Assert.AreEqual(Fingerprint(first), Fingerprint(second));
        }

        [Test]
        public void Generate_DiffersAcrossRounds()
        {
            var fingerprints = Enumerable.Range(1, 5)
                .Select(round => Fingerprint(
                    FightOptionGenerator.Generate(4242, round, dread: 2, FourAuthoredArmies())))
                .Distinct()
                .ToList();

            Assert.Greater(fingerprints.Count, 1,
                "the 'options' stream is indexed by round — rounds must not share rolls");
        }

        [Test]
        public void Generate_SlotsMapToTiers_AndOnlyHardCarriesACondition()
        {
            var options = FightOptionGenerator.Generate(777, 1, dread: 0, FourAuthoredArmies());

            Assert.AreEqual(3, options.Count);
            Assert.AreEqual(FightOptionTier.Easy, options[0].Tier);
            Assert.AreEqual(FightOptionTier.Normal, options[1].Tier);
            Assert.AreEqual(FightOptionTier.Hard, options[2].Tier);
            Assert.IsNull(options[0].ConditionId);
            Assert.IsNull(options[1].ConditionId);
            CollectionAssert.Contains(ConditionCatalog.Ids.ToList(), options[2].ConditionId);
        }

        [Test]
        public void Generate_ClampsFightNumbersToAuthoredRange()
        {
            // Dread 40 → fight-equivalent 21, far past the authored 4: the target
            // clamps to 4 and every roll stays within ±1 of it.
            var options = FightOptionGenerator.Generate(999, 8, dread: 40, FourAuthoredArmies());

            foreach (var option in options)
                Assert.That(option.TemplateFightNumber, Is.InRange(3, 4));
        }

        [Test]
        public void Generate_StrengthPreviewMatchesTheHudCalculator()
        {
            var options = FightOptionGenerator.Generate(123, 2, dread: 2, FourAuthoredArmies());

            foreach (var option in options)
            {
                int expected = ArmyStrengthCalculator
                    .Evaluate(BoardWithRifles(option.TemplateFightNumber))
                    .EffectiveTotal;
                Assert.AreEqual(expected, option.StrengthPreview,
                    $"fight {option.TemplateFightNumber} preview must reuse ArmyStrengthCalculator");
            }
        }

        // ---- ConditionCatalog / RuleModifierCatalog ----

        [Test]
        public void ConditionCatalog_ResolvesEveryAuthoredId()
        {
            foreach (var id in ConditionCatalog.Ids)
            {
                Assert.IsTrue(ConditionCatalog.TryResolve(id, out var modifier), id);
                Assert.AreEqual(id, modifier.Id);
            }

            Assert.IsFalse(ConditionCatalog.TryResolve("no_such_condition", out _));
        }

        [Test]
        public void RuleModifierCatalog_ResolvesConditionsAndFallsThroughToTwists()
        {
            Assert.AreEqual(
                ConditionCatalog.StormBarrage,
                RuleModifierCatalog.Resolve(ConditionCatalog.StormBarrage).Id);
            Assert.AreEqual(
                TwistCatalog.EndlessMuster,
                RuleModifierCatalog.Resolve(TwistCatalog.EndlessMuster).Id);
            Assert.Throws<InvalidOperationException>(
                () => RuleModifierCatalog.Resolve("no_such_modifier"));
        }

        [Test]
        public void EntrenchedFoe_ArmorsOnlyTheEnemyFrontRank()
        {
            var run = StartTwoEnemyFight(Resolve(ConditionCatalog.EntrenchedFoe));
            var enemies = run.EnemyCombatantsForTests;
            int frontX = enemies.Min(e => e.AnchorPosition.X);

            Assert.IsTrue(enemies.Where(e => e.AnchorPosition.X == frontX)
                .All(e => e.ArmorBuffSteps == 1));
            Assert.IsTrue(enemies.Where(e => e.AnchorPosition.X != frontX)
                .All(e => e.ArmorBuffSteps == 0));
            Assert.IsTrue(run.PlayerCombatantsForTests.All(p => p.ArmorBuffSteps == 0));
        }

        [Test]
        public void VeteranCadre_BoostsOnlyTheRanksBehindTheFront()
        {
            var run = StartTwoEnemyFight(Resolve(ConditionCatalog.VeteranCadre));
            var enemies = run.EnemyCombatantsForTests;
            int frontX = enemies.Min(e => e.AnchorPosition.X);

            var rear = enemies.Where(e => e.AnchorPosition.X != frontX).ToList();
            Assert.IsNotEmpty(rear, "fixture must field a rear rank");
            Assert.IsTrue(rear.All(e => e.CurrentHp == e.Definition.MaxHp * 125 / 100));
            Assert.IsTrue(enemies.Where(e => e.AnchorPosition.X == frontX)
                .All(e => e.CurrentHp == e.Definition.MaxHp));
        }

        [Test]
        public void StormBarrage_WeathersOnlyThePlayerSide()
        {
            var run = StartTwoEnemyFight(Resolve(ConditionCatalog.StormBarrage));

            Assert.IsTrue(run.PlayerCombatantsForTests
                .All(p => p.CurrentHp == Math.Max(1, p.Definition.MaxHp * 85 / 100)));
            Assert.IsTrue(run.EnemyCombatantsForTests
                .All(e => e.CurrentHp == e.Definition.MaxHp));
        }

        [Test]
        public void IronResolve_GrantsEnemiesOneDamageBonus()
        {
            var control = StartTwoEnemyFight(null);
            var conditioned = StartTwoEnemyFight(Resolve(ConditionCatalog.IronResolve));

            Assert.IsTrue(control.EnemyCombatantsForTests.All(e => e.DamageBonus == 0));
            Assert.IsTrue(conditioned.EnemyCombatantsForTests.All(e => e.DamageBonus == 1));
            Assert.IsTrue(conditioned.PlayerCombatantsForTests.All(p => p.DamageBonus == 0));
        }

        // ---- DreadRules tier economy ----

        [Test]
        public void DreadFor_GrantsOneTwoThreeByTier()
        {
            Assert.AreEqual(1, DreadRules.DreadFor(FightOptionTier.Easy));
            Assert.AreEqual(2, DreadRules.DreadFor(FightOptionTier.Normal));
            Assert.AreEqual(3, DreadRules.DreadFor(FightOptionTier.Hard));
            Assert.AreEqual(DreadRules.DreadPerWin, DreadRules.DreadFor(FightOptionTier.Normal),
                "Normal keeps the M1 flat rate");
        }

        // ---- Easy-tier enemy fight-start engine suppression ----

        [Test]
        public void SuppressEnemyEngines_LeavesEnemyAtBaseline()
        {
            var control = StartSynergyFight(suppressEnemyEngines: false);
            var suppressed = StartSynergyFight(suppressEnemyEngines: true);

            Assert.IsTrue(control.EnemyCombatantsForTests.All(e =>
                    e.CurrentHp > e.Definition.MaxHp && e.DamageBonus > 0),
                "control fixture must actually form enemy synergies");
            Assert.IsTrue(suppressed.EnemyCombatantsForTests.All(e =>
                e.CurrentHp == e.Definition.MaxHp
                && e.DamageBonus == 0
                && e.ArmorBuffSteps == 0));
        }

        [Test]
        public void SuppressEnemyEngines_LeavesPlayerBuffsUntouched()
        {
            var control = StartSynergyFight(suppressEnemyEngines: false);
            var suppressed = StartSynergyFight(suppressEnemyEngines: true);

            foreach (var player in suppressed.PlayerCombatantsForTests)
            {
                var baseline = control.PlayerCombatantsForTests
                    .First(p => p.InstanceId == player.InstanceId);
                Assert.AreEqual(baseline.CurrentHp, player.CurrentHp, player.InstanceId);
                Assert.AreEqual(baseline.DamageBonus, player.DamageBonus, player.InstanceId);
            }

            Assert.IsTrue(suppressed.PlayerCombatantsForTests.Any(p =>
                    p.CurrentHp > p.Definition.MaxHp),
                "player fixture must form its own synergies to prove they survive");
        }

        // ---- Save schema (v9 additive) ----

        [Test]
        public void SaveRoundTrip_PreservesOptionsChoiceAndTier()
        {
            var state = RunState.CreateNew(FactionIds.IronmarchUnion, 42, 100, 100, 2, 100);
            state.FightOptions = new List<FightOptionRecord>
            {
                new() { Tier = FightOptionTier.Easy, EnemyFactionId = "ironmarch_union", TemplateFightNumber = 2, StrengthPreview = 120 },
                new() { Tier = FightOptionTier.Normal, EnemyFactionId = "ironmarch_union", TemplateFightNumber = 3, StrengthPreview = 150 },
                new() { Tier = FightOptionTier.Hard, EnemyFactionId = "ironmarch_union", TemplateFightNumber = 3, ConditionId = ConditionCatalog.VeteranCadre, StrengthPreview = 150 }
            };
            state.ChosenFightOption = 2;
            state.Combat = new CombatSaveState
            {
                CombatSeed = 7,
                ActiveTwistId = ConditionCatalog.VeteranCadre,
                ActiveTier = FightOptionTier.Hard
            };

            var restored = RunSaveSerializer.FromJson(RunSaveSerializer.ToJson(state));

            Assert.AreEqual(
                Fingerprint(state.FightOptions),
                Fingerprint(restored.FightOptions));
            Assert.AreEqual(2, restored.ChosenFightOption);
            Assert.AreEqual(FightOptionTier.Hard, restored.Combat.ActiveTier);
            Assert.AreEqual(ConditionCatalog.VeteranCadre, restored.Combat.ActiveTwistId);
        }

        [Test]
        public void LegacyV9Save_WithoutOptionKeys_DefaultsCleanly()
        {
            const string legacyJson =
                "{\n" +
                "  \"SaveSchemaVersion\": 9,\n" +
                "  \"FightIndex\": 3,\n" +
                "  \"RunSeed\": 42,\n" +
                "  \"FactionId\": \"ironmarch_union\",\n" +
                "  \"Phase\": \"Build\",\n" +
                "  \"Combat\": { \"CombatSeed\": 7 }\n" +
                "}";

            var restored = RunSaveSerializer.FromJson(legacyJson);

            Assert.IsNotNull(restored.FightOptions);
            Assert.IsEmpty(restored.FightOptions);
            Assert.AreEqual(-1, restored.ChosenFightOption);
            Assert.IsNull(restored.Combat.ActiveTier, "legacy combats restore as Normal");
        }

        // ---- fixtures ----

        private static ICombatRuleModifier Resolve(string conditionId)
        {
            Assert.IsTrue(ConditionCatalog.TryResolve(conditionId, out var modifier), conditionId);
            return modifier;
        }

        /// <summary>Player rifle vs two enemy rifles in distinct columns (front + rear
        /// rank) — mirrors the DreadRulesTests twist fixture.</summary>
        private static TickCombatRun StartTwoEnemyFight(ICombatRuleModifier modifier)
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(), "player_rifle");

            var enemy = new BoardState(TestBoards.Layout);
            Assert.IsTrue(
                enemy.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(3), "enemy_front").Success);
            Assert.IsTrue(
                enemy.TryPlace(TestPieces.RifleSquad(), new GridCoord(5, 3), "enemy_rear").Success);

            return TickCombatRun.Start(
                player,
                enemy,
                seed: 42,
                modifiers: modifier == null ? null : new[] { modifier });
        }

        /// <summary>Both sides field two ADJACENT BulwarkSquads — the Phalanx aura
        /// (+damage, +HP, self-applying) forms on each side at fight start.</summary>
        private static TickCombatRun StartSynergyFight(bool suppressEnemyEngines)
        {
            var player = new BoardState(TestBoards.Layout);
            Assert.IsTrue(player.TryPlace(
                TestPieces.BulwarkSquad(), TestBoards.FrontLineAnchor(3), "player_bulwark_a").Success);
            Assert.IsTrue(player.TryPlace(
                TestPieces.BulwarkSquad(), TestBoards.FrontLineAnchor(2), "player_bulwark_b").Success);

            var enemy = new BoardState(TestBoards.Layout);
            Assert.IsTrue(enemy.TryPlace(
                TestPieces.BulwarkSquad(), TestBoards.FrontLineAnchor(3), "enemy_bulwark_a").Success);
            Assert.IsTrue(enemy.TryPlace(
                TestPieces.BulwarkSquad(), TestBoards.FrontLineAnchor(2), "enemy_bulwark_b").Success);

            return TickCombatRun.Start(
                player,
                enemy,
                seed: 42,
                suppressEnemyFightStartEngines: suppressEnemyEngines);
        }
    }
}
