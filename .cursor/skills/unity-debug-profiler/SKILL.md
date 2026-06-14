---
name: unity-debug-profiler
description: >-
  Diagnoses Unity bugs and performance issues using Profiler, Frame Debugger,
  Memory Profiler, custom debug tooling, structured logging, and hot-path
  optimization. Use when tracking down bugs, GC spikes, frame drops, memory
  leaks, combat desync, or building in-editor visualizers and dev consoles.
  Invoke during active investigation; use unity-qa-build for release verification
  gates.
paths:
  - "Assets/_Project/**"
  - "TestResults*.xml"
disable-model-invocation: false
---

# Unity Debug & Profiler

Specialized skill for diagnosing issues, measuring performance, and building the tooling that makes development sustainable. Complements `unity-qa-build` heavily.

## Using in Cursor

- Mention debugging, profiling, performance spikes, memory leaks, or invoke **@unity-debug-profiler** in chat.
- For release sign-off and build gates, hand off to **@unity-qa-build** after the fix is verified.
- For code-level stability review, use subagent **gamedev-qa-tester**.
- Roguelike repro issues: coordinate with **@unity-gameplay-systems** (seed + event log).

## Core Principles

- **Profile Before Optimizing**: Never guess. Always capture real data with the Unity Profiler, Frame Debugger, and Memory Profiler.
- **Make Debugging Easy**: Invest in in-game consoles, visualizers, and Editor tools early — they pay for themselves many times over in solo development.
- **Hot Path Awareness**: Identify and protect the functions that run every frame or during key gameplay moments.
- **Deterministic Logging**: Especially important for roguelikes — logs should be reproducible with the same seed.
- **Editor vs Runtime Separation**: Heavy debugging tools should be Editor-only or easily stripped for release builds.

## Key Areas & Tools

### 1. Unity Profiler Mastery

- **Timeline view**: The most important — understand CPU, GPU, Rendering, Memory, and Audio tracks in context.
- Capture Play mode sessions and analyze spikes during procedural generation, combat, UI updates, etc.
- Deep profiling for detailed call stacks (use sparingly as it has overhead).
- Custom Profiler markers (`Profiler.BeginSample` / `EndSample` or `using` scopes) around key systems.
- Compare snapshots before/after changes.

### 2. Frame Debugger

- Step through draw calls to find overdraw, expensive shaders, or unnecessary state changes.
- Identify batches that should be combining but aren't (material or texture issues).
- Great for diagnosing rendering problems quickly.

### 3. Memory Profiler & Analysis

- Find leaks from unreleased Addressables, lingering GameObjects, or event subscriptions.
- Texture and mesh memory breakdown.
- Compare snapshots to see what grew during a run or scene transition.

### 4. In-Editor & In-Game Debug Tools (High ROI)

- **Runtime Console**: Toggleable in-game console for logs, warnings, and custom commands (great for testing procedural generation or combat).
- **Gizmos & Scene View Tools**: Visual debugging for grids, paths, FOV, generation boundaries, collision, etc.
- **Custom Editor Windows**: Dedicated windows for spawning test entities, triggering events, tweaking runtime values, or previewing procedural output.
- **Entity Inspectors**: Click on an enemy or item in Play mode and see its current state (health, AI state, buffs).
- **Generation Visualizers**: Step-through or heatmap views for procedural systems.

### 5. Logging Strategy

- Structured logging with categories (Gameplay, Generation, Save, UI, Audio).
- Conditional compilation (`#if UNITY_EDITOR` or development builds) so verbose logs don't ship.
- Log important random decisions with their seed context for roguelike debugging.
- Avoid `Debug.Log` in hot paths — use `ProfilerMarker` + occasional sampling instead.

### 6. Common Performance Hotspots & Fixes

- Excessive `GetComponent` or `FindObjectOfType` calls → cache references.
- `Update()` doing heavy work → move to coroutines, jobs, or event-driven.
- String concatenation or formatting every frame.
- Unpooled instantiation of VFX, projectiles, or UI elements.
- Expensive LINQ or allocations in hot paths.
- Shader variant explosion or material duplication.

### 7. Build & Release Debugging

- Development builds vs Release builds (IL2CPP, stripping, Profiler connection).
- Crash reporting and symbolicated stack traces.
- Remote logging or telemetry hooks (optional, via Unity Gaming Services or custom).

## Workflow When Activated

1. **Reproduce the issue** or performance problem with clear steps.
2. **Capture data**: Profiler timeline + markers, Frame Debugger, Memory snapshots, or custom visualizers.
3. **Analyze & diagnose**: Identify root cause (allocation, draw call spike, logic error, coupling, etc.).
4. **Propose fix + tooling**: Not just the code change, but also the debug visualization or test that will prevent regression.
5. **Implement** the optimization or bugfix along with improved tooling where valuable.
6. **Verify**: Re-profile and show before/after data. Confirm the issue is resolved without introducing new problems.

## DeadManZone conventions

### Deterministic repro (combat / shop)

| Tool | Use |
|------|-----|
| `RunState.RunSeed` | Reproduce shop offers and combat rolls |
| Shop seed | `RunSeed + FightIndex * 100 + RerollCountThisRound` |
| `CombatEventLog` / `LastCombatLogText` | Compare combat outcomes (`CombatLogFormatter`) |
| `VerticalSliceRegressionTests` | Fixed-seed combat must match golden event logs |
| `VerticalSliceTestFixtures.RegressionRunSeed` | Standard regression seed in EditMode tests |

When combat desyncs: log seed + fight index, dump event logs from two runs, diff in test.

### Known hot paths (profile these first)

- `TickCombatRun` / `CombatResolver` — tick combat simulation
- `ShopGenerator` — shop roll during build phase
- `CombatDirector` / `CombatArenaVfx` — presentation during fights
- `BoardView` / shop drag-drop UI — canvas rebuild risk
- `ContentDatabase.Load()` — startup; cache, don't repeat per frame

Target: **60 FPS** (≤ 16 ms frame time) during shop + active combat.

### Dev tooling already in repo

| Tool | Path |
|------|-----|
| Session prototype overlay | `Assets/_Project/Game/Dev/SessionContentOverlay.cs` |
| Content registry helper | `Assets/_Project/Game/Dev/ContentRegistryProvider.cs` |
| Batch test runner | `Assets/_Project/Core.Tests/Editor/BatchTestRunner.cs` |
| Tag Creator window | `DeadManZone > Tag Creator` |

Place new Editor-only debug tools under `Assets/_Project/Presentation/Editor/` or `Game/Dev/`. Strip or `#if UNITY_EDITOR` guard verbose runtime debug UI.

### Profiler capture checklist

1. `Window > Analysis > Profiler` — record shop open, full combat segment, tactic pause
2. `Window > Analysis > Frame Debugger` — Combat Arena scene during peak unit count
3. Memory Profiler — compare Main Menu vs mid-run vs post-10-fights
4. Run `BatchTestRunner.RunEditModeTests` after logic fixes; Play Mode tests if UI involved

### Investigation report template

```markdown
## Issue
[Symptom + repro steps]

## Evidence
- Seed / fight index: ...
- Profiler: [CPU ms / GC alloc / top 3 frames]
- Frame Debugger: [draw calls if rendering]

## Root cause
...

## Fix
...

## Verification
- Before/after profiler numbers
- Test: [test class/method added or run]
```

## Quality Gates

- Performance issues have concrete Profiler/Frame Debugger evidence and a clear fix.
- New debug tools make future diagnosis faster.
- Hot paths are protected with markers and have budgets.
- Logging is useful but not performance-destructive.
- Fixes are verified with data, not just "it feels better now".

## Cursor + Unity Notes

- Generate both the diagnostic code and the custom Editor tooling or Gizmo visualizers.
- Provide exact menu paths to open Profiler, Frame Debugger, Memory Profiler, etc.
- Suggest how to add `ProfilerMarker` scopes around the systems being worked on.
- After changes, include steps to capture and compare profiler data in Play mode.

This skill is used by the director (or directly) whenever there are bugs, performance problems, or a need for better debugging visibility. It turns painful investigation into fast, data-driven iteration.
