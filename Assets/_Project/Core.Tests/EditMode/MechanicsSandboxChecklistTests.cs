using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Game.Dev;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>
    /// Integration gate — one focused test per mechanics sandbox success criterion (spec §1).
    /// Arena mesh rendering (criterion 4 presentation) is covered by CombatArenaPlayModeTests.
    /// </summary>
    public sealed class MechanicsSandboxChecklistTests
    {
        private ContentDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _database = ContentDatabase.Load();
            if (_database == null || _database.Pieces.Count == 0)
                Assert.Ignore("ContentDatabase not found. Run DeadManZone → Generate Demo Content first.");

            SaveManager.DeleteSave();
        }

        [TearDown]
        public void TearDown() => SaveManager.DeleteSave();

        [Test]
        public void Criterion01_MultiCellFootprint_SpawnsAndTranslatesRigidly()
        {
            var player = VerticalSliceTestFixtures.BuildGauntletBoard(_database);
            var run = TickCombatRun.Start(player, TestBoards.WeakEnemyOnly(), seed: 24680);

            var cannon = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "cannon_1");
            Assert.AreEqual(6, cannon.OccupiedCells.Count);

            var before = cannon.OccupiedCells.ToList();
            var delta = new GridCoord(0, 1);
            cannon.AnchorPosition = new GridCoord(
                cannon.AnchorPosition.X + delta.X,
                cannon.AnchorPosition.Y + delta.Y);
            cannon.RecomputeOccupiedCells();

            var expected = before
                .Select(cell => new GridCoord(cell.X + delta.X, cell.Y + delta.Y))
                .ToList();
            CollectionAssert.AreEquivalent(expected, cannon.OccupiedCells.ToList());
        }

        [Test]
        public void Criterion02_ShapePathfinder_RoutesAroundBlockingBuilding()
        {
            var layout = new BattlefieldLayout(
                playerHalfWidth: 7,
                neutralWidth: 0,
                enemyHalfWidth: 7,
                height: 10);
            var occupancy = new CombatOccupancyGrid();
            var hqOffsets = CombatFootprint.ComputeOffsets(TestPieces.HqPiece().Shape, rotation: 0);
            var moverOffsets = CombatFootprint.ComputeOffsets(TestPieces.RifleSquad().Shape, rotation: 0);

            occupancy.Place("hq", new GridCoord(3, 5), hqOffsets);

            var step = ShapePathfinder.FindStep(
                new GridCoord(2, 5),
                new GridCoord(6, 5),
                moverOffsets,
                moverInstanceId: "mover",
                occupancy,
                layout);

            Assert.IsNotNull(step);
            Assert.AreNotEqual(new GridCoord(3, 5), step.Value);
        }

        [Test]
        public void Criterion03_RoleEngagement_AssaultClosesNearestFrontLine()
        {
            var layout = new BattlefieldLayout(7, 2, 7, 10);
            var assault = CreateCombatant("assault", GameTagIds.Assault, CombatSide.Player, new GridCoord(2, 5));
            var frontEnemy = CreateCombatant("enemy_front", GameTagIds.Assault, CombatSide.Enemy, new GridCoord(10, 5));
            var rearEnemy = CreateCombatant("enemy_rear", GameTagIds.Assault, CombatSide.Enemy, new GridCoord(12, 5));

            var goal = RoleEngagement.ComputeGoal(
                assault,
                allies: new[] { assault },
                enemies: new[] { rearEnemy, frontEnemy },
                layout);

            Assert.AreEqual(new GridCoord(9, 5), goal);
        }

        [Test]
        public void Criterion04_Buildings_BlockFullFootprintInCombatSim()
        {
            var run = TickCombatRun.Start(TestBoards.HqOnly(), TestBoards.WeakEnemyOnly(), seed: 11);
            var hq = run.PlayerCombatantsForTests.Single(c => c.HasTag(GameTagIds.Hq));
            var snapshot = run.OccupancySnapshotForTests;

            Assert.Greater(hq.OccupiedCells.Count, 1);
            foreach (var cell in hq.OccupiedCells)
            {
                Assert.IsTrue(snapshot.ContainsKey(cell));
                Assert.AreEqual(hq.InstanceId, snapshot[cell]);
            }
        }

        [Test]
        public void Criterion05_AttackArmorMatrix_DefinesEveryCombo()
        {
            foreach (AttackType attackType in Enum.GetValues(typeof(AttackType)))
            {
                if (attackType == AttackType.None)
                    continue;

                foreach (ArmorType armorType in Enum.GetValues(typeof(ArmorType)))
                {
                    float multiplier = AttackTypeProfileCatalog.GetArmorMatrixMultiplier(attackType, armorType);
                    Assert.Greater(multiplier, 0f, $"{attackType} vs {armorType}");
                }
            }
        }

        private static BoardState CreateUnitPlacementBoard() =>
            new(BoardLayout.CreateHorizontalZones(
                TestBoards.DefaultWidth,
                TestBoards.DefaultHeight,
                rearCols: 3,
                supportCols: TestBoards.DefaultSupportCols,
                specialTiles: Array.Empty<GridCoord>()));

        [Test]
        public void Criterion06_AdjacencySynergies_ApplyAtCombatStart()
        {
            var board = CreateUnitPlacementBoard();
            var medic = FindPiece("field_medic").ToCore();
            var infantry = FindPiece("conscript_rifleman").ToCore();
            Assert.IsTrue(board.TryPlace(medic, TestBoards.SupportLineAnchor(0), "medic_1").Success);
            Assert.IsTrue(board.TryPlace(infantry, TestBoards.SupportLineAnchor(1), "conscript_1").Success);

            var run = TickCombatRun.Start(board, TestBoards.WeakEnemyOnly(), seed: 3);
            var conscript = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "conscript_1");

            Assert.Greater(conscript.ArmorBuffSteps, 0);
        }

        [Test]
        public void Criterion07_SalvageShop_OffersLastEnemyFactionPieces()
        {
            var registry = ContentRegistryProvider.Build(_database);
            var generator = new ShopGenerator(registry);
            var shop = generator.Generate(
                VerticalSliceTestFixtures.BuildGauntletBoard(_database),
                "iron_vanguard",
                round: 2,
                seed: 42,
                lastEnemyFactionId: "neutral",
                salvageChancePercent: 90);

            Assert.That(shop.Offers, Is.Not.Empty);
            var salvaged = shop.Offers.Where(o => o.IsSalvaged).ToList();
            Assert.That(salvaged, Is.Not.Empty);
            Assert.IsTrue(salvaged.All(o => registry.GetById(o.PieceId).FactionId == "neutral"));
        }

        [Test]
        public void Criterion08_SpecialtyLane_ReflectsBoardComposition()
        {
            var board = new BoardState(TestBoards.Layout);
            for (int i = 0; i < 2; i++)
            {
                Assert.IsTrue(board.TryPlace(
                    TestPieces.RifleSquad(),
                    TestBoards.FrontLineAnchor(i),
                    $"rifle_{i}").Success);
            }

            var context = SpecialtyLaneRuleCatalog.Resolve(board, ContentRegistryProvider.Build(_database));

            CollectionAssert.Contains(context.PreferredCombatRoles, GameTagIds.Support);
            CollectionAssert.Contains(context.PreferredSynergyTags, GameTagIds.Spotter);
        }

        [Test]
        public void Criterion09_EmergencyDraftCriticalMassAndTactics_AreFunctional()
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

            var state = RunState.CreateNew("iron_vanguard", 1, 10, 2, 5, 100);
            Assert.IsTrue(EmergencyDraft.TryUse(state, manpowerShortfall: 3));
            Assert.IsTrue(state.EmergencyDraftUsed);

            var board = CreateUnitPlacementBoard();
            var infantry = TestPieces.CreateUnit(
                "inf",
                primary: GameTagIds.Infantry,
                combatRole: GameTagIds.Assault,
                systemTag: GameTagIds.Combatant);
            for (int i = 0; i < 5; i++)
                Assert.IsTrue(board.TryPlace(infantry, TestBoards.SupportLineAnchor(i), $"inf_{i}").Success);

            Assert.IsTrue(CriticalMassEngine.Evaluate(board).HasAnyActiveRule);
            Assert.Greater(TacticEffects.GetMovementChargeMultiplier(TacticType.Advance), 100);
            Assert.Greater(TacticEffects.GetDamageBuff(TacticType.DisciplinedFire), 0);

            CriticalMassRuleSource.ClearTestOverride();
        }

        [Test]
        public void Criterion10_TagCreator_CustomTagsAppearInRegistry()
        {
            Assume.That(CustomTagCatalog.GeneratedEntries.Count, Is.GreaterThan(0));

            var custom = CustomTagCatalog.GeneratedEntries[0];
            var tag = TagRegistry.Get(custom.Id);

            Assert.AreEqual(custom.DisplayName, tag.DisplayName);
            Assert.AreEqual(custom.Category, tag.Category);
        }

        [Test]
        public void Criterion11_UnitCardTooltips_IncludeSynergyAndSalvageContext()
        {
            var medic = FindPiece("field_medic").ToCore();
            var infantry = FindPiece("conscript_rifleman").ToCore();
            var board = CreateUnitPlacementBoard();
            Assert.IsTrue(board.TryPlace(medic, TestBoards.SupportLineAnchor(0), "medic_1").Success);
            Assert.IsTrue(board.TryPlace(infantry, TestBoards.SupportLineAnchor(1), "conscript_1").Success);

            var snapshot = SynergyEngine.EvaluateFightStart(board);
            Assert.IsTrue(snapshot.TryGet("conscript_1", out var synergy));

            var model = PieceCardViewModelBuilder.Build(
                infantry,
                new PieceCardBuildContext
                {
                    Board = board,
                    InstanceId = "conscript_1",
                    Synergy = synergy,
                    SynergySnapshot = snapshot,
                    IsSalvaged = true,
                    LastEnemyFactionDisplayName = "Neutral Militia"
                });

            Assert.Greater(model.SynergyLines.Count, 0);
            StringAssert.Contains("Neutral Militia", model.SalvageContext);
            Assert.IsFalse(string.IsNullOrWhiteSpace(model.AttackTypeTooltip));
        }

        [Test]
        public void Criterion12_SandboxRosters_MeetNeutralAndIronMarchTargets()
        {
            int neutral = _database.Pieces.Count(p => p != null && p.factionId == "neutral");
            int ironMarch = _database.Pieces.Count(p => p != null && p.factionId == "iron_vanguard");

            Assert.GreaterOrEqual(neutral, 10);
            Assert.GreaterOrEqual(ironMarch, 15);
        }

        [Test]
        public void Criterion13_SaveResumeMidCombat_GauntletFootprintBoard_MatchesFreshRunLog()
        {
            const int seed = 5151;
            var commands = VerticalSliceTestFixtures.BuildAggressiveCommands(
                VerticalSliceTestFixtures.BuildGauntletBoard(_database));

            var saved = new RunOrchestrator(_database);
            saved.StartNewRun("iron_vanguard", runSeed: seed);
            saved.SavePlayerBoard(VerticalSliceTestFixtures.BuildGauntletBoard(_database));
            saved.BeginCombat();
            saved.AdvanceCombat();
            saved.SubmitCombatCommands(commands);
            saved.SaveAndExit();

            var reloaded = new RunOrchestrator(_database);
            Assert.IsTrue(reloaded.TryLoadSavedRun());
            reloaded.SubmitCombatCommands(commands);
            var reloadedStep = reloaded.AdvanceCombat();

            var fresh = new RunOrchestrator(_database);
            fresh.StartNewRun("iron_vanguard", runSeed: seed);
            fresh.SavePlayerBoard(VerticalSliceTestFixtures.BuildGauntletBoard(_database));
            fresh.BeginCombat();
            fresh.AdvanceCombat();
            fresh.SubmitCombatCommands(commands);
            var freshStep = fresh.AdvanceCombat();

            Assert.NotNull(reloadedStep.EventLog);
            Assert.NotNull(freshStep.EventLog);
            Assert.AreEqual(freshStep.EventLog.Events.Count, reloadedStep.EventLog.Events.Count);
        }

        private PieceDefinitionSO FindPiece(string id) =>
            _database.Pieces.First(p => p != null && p.id == id);

        private static CombatantState CreateCombatant(
            string instanceId,
            string combatRole,
            CombatSide side,
            GridCoord anchor)
        {
            var definition = TestPieces.CreateUnit(instanceId, combatRole: combatRole);
            return new CombatantState
            {
                InstanceId = instanceId,
                Side = side,
                Definition = definition,
                CurrentHp = definition.MaxHp,
                AnchorPosition = anchor,
                SpawnAnchorY = anchor.Y,
                ShapeOffsets = CombatFootprint.ComputeOffsets(definition.Shape, rotation: 0)
            };
        }
    }
}
