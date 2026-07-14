> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# Army Strength System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Top Troops–style combat army strength (base + effective with synergy delta) for player matchup preview, enemy template tooling, and balance reports.

**Architecture:** Pure-C# `PieceCombatRating` + `ArmyStrengthCalculator` + `MatchupAssessment` in Core; `MatchupStrengthView` on build HUD; editor report menu. TDD in `Assets/_Project/Core.Tests/EditMode/`.

**Tech Stack:** Unity 6, C# Core (`DeadManZone.Core`), NUnit EditMode, existing `SynergyEngine` / `ManpowerCalculator` fielding rules

**Spec:** `docs/superpowers/specs/2026-06-18-army-strength-system-design.md`

---

## File map

| File | Action |
|------|--------|
| `Assets/_Project/Core/Combat/CombatStrengthConfig.cs` | **Create** — scale, thresholds, multipliers |
| `Assets/_Project/Core/Combat/PieceCombatRating.cs` | **Create** — per-piece rating |
| `Assets/_Project/Core/Combat/ArmyStrengthSnapshot.cs` | **Create** — board totals struct |
| `Assets/_Project/Core/Combat/ArmyStrengthCalculator.cs` | **Create** — board evaluation |
| `Assets/_Project/Core/Combat/MatchupAssessment.cs` | **Create** — ratio + label |
| `Assets/_Project/Core/Run/ManpowerCalculator.cs` | Expose `CountsTowardFielding` public |
| `Assets/_Project/Core.Tests/EditMode/PieceCombatRatingTests.cs` | **Create** |
| `Assets/_Project/Core.Tests/EditMode/ArmyStrengthCalculatorTests.cs` | **Create** |
| `Assets/_Project/Core.Tests/EditMode/MatchupAssessmentTests.cs` | **Create** |
| `Assets/_Project/Core.Tests/EditMode/EnemyTemplateStrengthCurveTests.cs` | **Create** |
| `Assets/_Project/Presentation/Run/MatchupStrengthView.cs` | **Create** — HUD widget |
| `Assets/_Project/Presentation/Run/RunHudView.cs` | Wire matchup refresh |
| `Assets/_Project/Presentation/Run/RunHudPanelBuilder.cs` | Build matchup UI nodes |
| `Assets/_Project/Presentation/Run/RunBuildUiBootstrap.cs` | Ensure wiring |
| `Assets/_Project/Game/RunOrchestrator.cs` | `GetUpcomingEnemyBoard()` helper if needed |
| `Assets/_Project/Data/Editor/CombatStrengthReport.cs` | **Create** — editor menu report |

---

### Task 1: `CombatStrengthConfig` + `PieceCombatRating`

**Files:**
- Create: `Assets/_Project/Core/Combat/CombatStrengthConfig.cs`
- Create: `Assets/_Project/Core/Combat/PieceCombatRating.cs`
- Modify: `Assets/_Project/Core/Run/ManpowerCalculator.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/PieceCombatRatingTests.cs`

- [ ] **Step 1: Write failing tests** — MG team > conscript; HQ > 0; building = 0; synergy damage increases rating
- [ ] **Step 2: Run tests — expect FAIL**
- [ ] **Step 3: Implement config + rating + public `CountsTowardFielding`**
- [ ] **Step 4: Run `PieceCombatRatingTests` — PASS**

---

### Task 2: `ArmyStrengthCalculator` + snapshot

**Files:**
- Create: `Assets/_Project/Core/Combat/ArmyStrengthSnapshot.cs`
- Create: `Assets/_Project/Core/Combat/ArmyStrengthCalculator.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/ArmyStrengthCalculatorTests.cs`

- [ ] **Step 1: Write failing tests** — sum two pieces; synergy bonus > 0 when adjacency buff applies; empty board = 0
- [ ] **Step 2: Run — FAIL**
- [ ] **Step 3: Implement calculator using `SynergyEngine.EvaluateFightStart`**
- [ ] **Step 4: Run — PASS**

---

### Task 3: `MatchupAssessment`

**Files:**
- Create: `Assets/_Project/Core/Combat/MatchupAssessment.cs`
- Create: `Assets/_Project/Core.Tests/EditMode/MatchupAssessmentTests.cs`

- [ ] **Step 1: Write failing tests** — ratio 1.2 → Favorable; 1.0 → Even; 0.7 → Dangerous; zero enemy → Even (safe fallback)
- [ ] **Step 2: Run — FAIL**
- [ ] **Step 3: Implement**
- [ ] **Step 4: Run — PASS**

---

### Task 4: Enemy template curve tests + editor report

**Files:**
- Create: `Assets/_Project/Core.Tests/EditMode/EnemyTemplateStrengthCurveTests.cs`
- Create: `Assets/_Project/Data/Editor/CombatStrengthReport.cs`

- [ ] **Step 1: Write curve test** — all fights 1–10 enemy base within 0.5×–2.0× reference player base (soft band, adjust after calibration)
- [ ] **Step 2: Run — may FAIL until scale tuned**
- [ ] **Step 3: Tune `CombatStrengthConfig.Scale` if needed**
- [ ] **Step 4: Add editor menu `DeadManZone/Combat Strength Report`**
- [ ] **Step 5: Run curve test — PASS**

---

### Task 5: Build screen UI

**Files:**
- Create: `Assets/_Project/Presentation/Run/MatchupStrengthView.cs`
- Modify: `Assets/_Project/Presentation/Run/RunHudView.cs`
- Modify: `Assets/_Project/Presentation/Run/RunHudPanelBuilder.cs`

- [ ] **Step 1: Build `MatchupStrengthView`** — formats `1,240 (+120) vs 980 · Favorable`
- [ ] **Step 2: Wire into HUD** — refresh on `RunStateChanged` + expose method for board refresh from `RunSceneController`
- [ ] **Step 3: Manual Play mode** — fight 1 build screen shows plausible numbers

---

### Task 6: Full verification

- [ ] Run full EditMode suite
- [ ] `ReadLints` on edited files
- [ ] Manual smoke: change board layout, synergy delta updates

---

**Unity test command (filtered):**

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.0.XXf1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform editmode `
  -testFilter "DeadManZone.Core.Tests.EditMode.ArmyStrengthCalculatorTests|DeadManZone.Core.Tests.EditMode.PieceCombatRatingTests|DeadManZone.Core.Tests.EditMode.MatchupAssessmentTests" `
  -testResults "TestResults-EditMode.xml" -quit
```

Replace `6000.0.XXf1` with installed editor version.
