# DeadManZone Codebase Audit Report

**Date**: 2026-06-22 (session-based, re-executed for verification)  
**Scope**: Static source analysis of Assets/_Project (Core, Data, Game, Presentation + tests + scenes references). ~590 .cs files total. No runtime/editor execution.  
**Objective**: Post-iteration architecture review; identify technical debt, inconsistencies, duplication, god classes, magic values; prioritize high-ROI non-breaking refactors.  
**Non-goals** (as per plan): No code changes, refactors, new tests or edits of any kind. No third-party/Synty assets, art, generated images or non-C# content. No gameplay balance/tuning. No full runtime verification (static + test code analysis only). No exhaustive per-file inventory or low-ROI style nits.

## Architecture Summary (AC1)

The audit examined the post-iteration architecture using direct list_dir, grep (glob *.cs, path-limited to Assets/_Project), and read_file on representatives from:
- Core (Board/, Combat/, Run/, Shop/, Tags/, Common/, Content/, Meta/)
- Game (RunOrchestrator*, RunManager, SaveManager, GamePlayBootstrap, RunSaveBootstrap)
- Data (ContentDatabase.cs, FactionSO.cs + other ScriptableObjects/, Editor generators, Resources/)
- Presentation (BoardView.cs, Run/*, Combat/*, ShopView.cs, DragDrop, UI, all *Bootstrap, controllers)
- Tests (Core.Tests, Presentation.Tests, PlayMode)

**Layered structure (signs of evolution observed)**:
- **Core** (DeadManZone.Core.asmdef, pure logic + Newtonsoft only): Board (State/Layout/Snapshot/Zone rules), Combat (TickCombatRun + resolvers + CriticalMass + Tactic*), Run (State/Phase/Snapshots/Serializer + calculators), Shop (generators/pools/calculators), Tags (registries/rules/synergies + partial Generated catalog). Snapshots for serialization, extracted calculators.
- **Data** (depends on Core): ContentDatabase (hard-coded PlayableFactionIds + DemoShopPieceIds, string GetFaction, BuildRegistry, Load fallbacks), FactionSO + Enemy/Piece/Shop/CriticalMass/CombatArena SOs (many defaults + layout data), Editor generators (VerticalSlice* + Demo* + menu items), Resources.
- **Game** (Core+Data): RunManager (MB singleton), RunOrchestrator (sealed partial across 3 files), SaveManager, FightRewardTable, GameScenes, GamePlayBootstrap (static RuntimeInitializeOnLoad), RunSaveBootstrap (MB + delegate, comment "until RunManager owns save triggers").
- **Presentation** (all layers + Unity): Heavy MBs (BoardView, ShopView, RunSceneController, Combat*Presenter/Director/Actor, many layout fitters), *Bootstrap proliferation (8+ in Run/ + Combat/), event wiring to RunManager.Instance, Visual/SO themes.

**Visible signs of multiple iterations**:
- Save schema migrations (RunSaveSerializer CurrentSchemaVersion=7, Legacy=2; multiple MigrateLegacySave/Migrate*JObject; checks <2, <3 for reserves, <5, <6 upgrade).
- Legacy layout support in snapshots/BoardLayout.CreateHorizontalZones used in tests + generators.
- RunOrchestrator partial class splits (main + .Shop.cs + .Meta.cs).
- Proliferation of *Bootstrap classes (static vs MB mix).
- RunSaveBootstrap transitional code + ownership comment.
- Legacy cleanup classes (LegacyUnitCardCleanup) + generators retained.

Coupling: Core remains pure/testable (ctor DI, many EditMode tests). Game layer adds singletons + bootstraps. Presentation heavily uses RunManager.Instance for state/events.

## Debt Catalog

### God Classes / Large Files (AC2)
Concrete examples documented via captured file sizes (audit-file-sizes.txt) + direct reads:
- Presentation/Editor/RunSceneSetup.cs: 931 LOC (editor).
- Presentation/Board/BoardView.cs: 579 LOC (see reinspect-BoardView.txt): MB with serialized tileRoot/grid, piecesOverlay, synergy, hover card, terrain/backdrop, multiple private resolvers (ResolvePieceHoverCardController with 4 fallbacks), legacy remove, test-only InitializeForTests, zone visuals. Excerpts confirm mixed UI/state/visual responsibilities.
- Game/RunOrchestrator.cs + partials: 496 (main) + 279 (.Shop) + ~73 (.Meta) = ~850 LOC combined (see reinspect-RunOrchestrator.txt): orchestrates run flow, shop, meta, combat restore, faction, persist, seed math, refund paths, schema upgrades.
- Core/Combat/TickCombatRun.cs: 453 LOC (Core "sim god"): combatants, occupancy, tactics, log, command processor, tick state, many privates. Confirmed via list_dir + read.
- Presentation/Combat/Arena/CombatArenaPresenter.cs: 437 LOC.
- Presentation/Run/RunSceneController.cs: 370 LOC (many [SerializeField] views + panels, phase logic, layout capture).
Other large: RoleEngagement 368, CombatArenaBootstrap 362, ShopGenerator 288, RunManager 284.

These concentrate logic, making changes and testing difficult.

### Duplicated Logic (AC2)
- Faction string handling in 100+ locations (broad grep counts + targeted): PlayableFactionIds array (ContentDatabase.cs:16-20), default "iron_vanguard" (FactionSO.cs:12, RunManager.cs:96), GetFaction string compare, usage in 30+ files (DemoPieceFactory 23 matches, tests 17+, generators, Meta, Pres, Shop filters). See audit-grep-evidence.txt.
- Refund/credit application repeated: State.Supplies += refund.Supplies; Authority += ; Manpower += ; (RunOrchestrator.cs:363 ApplySalvageRefund and Shop.cs:177-179 sell path; also reward/critical mass sites).
- Seed derivation math *100/*1000: shopSeed = RunSeed + FightIndex*100 + rerolls (Shop.cs 285/321); combatSeed *1000 (RunOrchestrator.cs:165); score FightIndex*100 (Meta.cs:59).
- Event subscription boilerplate: RunManager.Instance.RunStateChanged += OnXXX (OnEnable/OnDisable) repeated across Pres views (ShopView.cs:55/70 examples + similar in others). Tests also direct wire.

Captured in audit-grep-evidence.txt + greps.

### Magic Numbers/Strings (AC2)
At least 8 (actually 12+) distinct:
1. "iron_vanguard", "dust_scourge", "cartel_of_echoes" (ContentDatabase, FactionSO, RunManager default, 20+ files).
2. hqPieceId = "ironmarch_hq" (FactionSO.cs:44).
3. SaveSchemaVersion checks <3 (orchestrator:47), <6 (53), <5 (559); Current=7, Legacy=2 (serializer).
4. MaxFights = 10, BaseRerollCost = 1 (orchestrator:19-20).
5. FightIndex *100 (shop/score) and *1000 (combat seed).
6. Board 9x10/rear=4/support=3 + startingSupplies=100 etc + baseSalvage=10 (FactionSO + snapshot calls).
7. DemoShopPieceIds ~20+ string literals (ContentDatabase).
8. Lane rename magic: "General"=>"Offensive", "Engineers"=>"Defensive", "Requisition"=>"Specialty" (serializer:104).
9. ResourcesPath = "DeadManZone/ContentDatabase" + LoadAll fallback.
10. LegacyDefault* = 100, index % length in GetEnemyTemplate.
11. board/specialTileCoords/hqSpawnAnchor/rotation/tokenColor defaults.
12. Various <0 / sizeDelta <1f / % guards + index magic in calcs/views/editors.

See audit-grep-evidence.txt for counts/snippets from greps.

### Inconsistent Patterns (AC3)
- Singleton RunManager.Instance (and RunSaveBootstrap.Instance) direct access in nearly all Presentation classes vs. pure Core DI.
- Bootstrap styles: static GamePlayBootstrap (RuntimeInitialize) vs MB RunSaveBootstrap vs many Pres *Bootstrap (Ensure/Apply).
- Save trigger ownership comments inconsistent/transitional (RunSaveBootstrap explicitly notes "until RunManager owns").
- Partial classes only in few spots (RunOrchestrator trio + CustomTagCatalog.Generated).
- String factionId/piece ids everywhere (no strong type).
- Magic duplicated across SO defaults + DB arrays + runtime + generators + tests.
- Legacy generators retained alongside full demo content generators.

## Prioritized Highest-ROI Cleanup Opportunities (AC4)

Short actionable list (specific files/classes, desired outcome, no behavior change):

1. Centralize faction identifiers and constants.
   - Introduce Core/FactionIds.cs (or enum + map for JSON/SO compat). Replace all string literals and arrays.
   - Files: ContentDatabase.cs (Playable+Demo), FactionSO.cs, RunManager.cs (default), RunOrchestrator.cs (Get/Start), Data/Editor/* generators, Core/Meta/*, Pres/* resolvers, tests.
   - Outcome: single source eliminates 100+ string sites and typo risk.

2. Reduce RunManager.Instance direct access via dependency patterns or events.
   - Replace direct .Instance in views/controllers with event channels (SO events) or explicit injection from scene bootstrap/Game.
   - Files: ShopView.cs, RunSceneController.cs, MainMenuController.cs, BattleReportPresenter, many other Pres + tests.
   - Outcome: testable Pres layer, less global coupling.

3. Split responsibilities in largest classes.
   - Extract from RunOrchestrator (Shop state, seed helper, meta facade); from BoardView (tile factory, overlay, visual resolver).
   - Files: Game/RunOrchestrator*.cs, Presentation/Board/BoardView.cs, Core/Combat/TickCombatRun.cs (already large in Core).
   - Outcome: smaller focused classes while keeping public API stable.

4. Consolidate/refactor save migration logic.
   - Single migrator class or table-driven steps instead of scattered if <N + private Migrate* in serializer + checks in orchestrator.
   - Files: Core/Run/RunSaveSerializer.cs, Game/RunOrchestrator.cs (load + upgrades).
   - Outcome: easier to maintain as schema evolves past 7.

5. Standardize bootstrap and scene init.
   - Choose consistent pattern (e.g. MB Ensure + explicit init from controller). Reduce number of tiny *Bootstrap.
   - Files: Game/GamePlayBootstrap.cs + RunSaveBootstrap.cs, Presentation/Run/*Bootstrap.cs + Combat/*Bootstrap + editor setups.
   - Outcome: less historical debt and confusion.

6. Extract repeated resource math and event wiring.
   - Helper ApplyResourceDelta or ResourceDelta struct; seed helper (RunSeedHelper.CombatSeed etc); common subscribe/unsubscribe or channel.
   - Files: refund sites in RunOrchestrator*.cs, seed calcs, Pres view OnEnable/OnDisable pairs.
   - Outcome: DRY, fewer copy-paste bugs.

Rationale for all: high duplication surface + coupling pain + maintenance risk from iterations; changes are mechanical/extractive and preserve observable behavior.

## Report Notes and Verification (AC5)
This is the self-contained Markdown report at {SCRATCH}/deadmanzone-codebase-audit.md.
It contains:
- Architecture overview
- Debt catalog with concrete examples + file/line refs
- Prioritized ROI list (6 items) with locations + outcomes
- Next-steps outline (in full version captured)

Non-goals and risks noted above and in source plan.

## Next Steps Outline
- Address 1-2 (faction + singleton) first for broadest impact with low risk.
- Add/maintain tests around extracted helpers and migrator (using existing test assemblies).
- Re-run same verif greps + reads after any future work for regression.

**Supporting captured artifacts in this scratch dir**:
- audit-grep-evidence.txt (step 2)
- audit-file-sizes.txt (god counts)
- reinspect-RunOrchestrator.txt, reinspect-BoardView.txt, reinspect-ContentDatabase.txt, reinspect-RunSaveSerializer.txt (step 4 separate excerpts)
- audit-no-edits-check.txt (step 5)
- verif-report-checks.txt (explicit (a-f) asserts)
- Plus session list_dir + grep tool outputs.

All backed by direct tool calls on the real source (no code changes made).

**End of report** (clean, self-contained, readable version written 2026-06-22).