> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# Combat Sim Completion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire movement-speed charge budget, attack-speed cooldowns, and 7-column neutral battlefield so combat pacing reflects piece data before the next balance pass.

**Architecture:** Add small config/rule modules (`CombatBattlefieldConfig`, `CombatMovementSpeed`, `CombatAttackSpeed`, `CombatMovementRules`) and integrate into `TickCombatRun`. Widen neutral via `BattlefieldLayout` default. Validate with EditMode tests; tutorial balance tests remain non-gating.

**Tech Stack:** Unity 6, C# Core (`DeadManZone.Core`), NUnit EditMode tests

**Spec:** `docs/superpowers/specs/2026-06-04-deadmanzone-combat-sim-completion-design.md`

---

### Task 1: Battlefield config (7 neutral columns)

**Files:**
- Create: `Assets/_Project/Core/Combat/CombatBattlefieldConfig.cs`
- Modify: `Assets/_Project/Core/Board/BattlefieldLayout.cs`
- Test: `Assets/_Project/Core.Tests/EditMode/CombatBattlefieldConfigTests.cs`

- [x] Add `NeutralColumnCount = 7`
- [x] Default `FromPlayerBoard` to config
- [x] Assert total width 25

### Task 2: Movement charge + attack speed modules

**Files:**
- Create: `CombatMovementSpeed.cs`, `CombatAttackSpeed.cs`, `CombatMovementRules.cs`
- Modify: `CombatMovement.cs`, `CombatantState.cs`

- [x] Charge rates Low/Medium/High = 3/5/6 per tick
- [x] Step costs 100 normal / 200 neutral
- [x] Attack speed multipliers per demo spec

### Task 3: TickCombatRun integration

**Files:**
- Modify: `TickCombatRun.cs`, `PhasedCombatRun.cs`

- [x] Accrue charge, gate on range, max 1 step/tick
- [x] Apply attack speed on cooldown reset

### Task 4: EditMode tests

**Files:**
- Create: `CombatMovementSpeedTests.cs`, `CombatAttackSpeedTests.cs`, `CombatMovementRangeGateTests.cs`
- Modify: `BattlefieldStateTests.cs`, `TestPieces.cs`

- [x] Opening move count, tier ordering, range gate, layout width

### Task 5: Verification

- [ ] Run EditMode tests in Unity Test Runner
- [ ] Manual smoke: fight 1 opening advance feels slow
