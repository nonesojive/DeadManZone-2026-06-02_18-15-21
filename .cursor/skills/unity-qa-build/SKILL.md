---
name: unity-qa-build
description: >-
  Runs Unity testing, profiling, build pipeline checks, release verification,
  and performance validation with evidence-based quality gates. Use when
  verifying playability, profiling FPS/memory, running Unity Test Framework
  tests, validating builds, checking Player Settings, preparing release
  candidates, or before claiming a Unity feature or vertical slice is done.
  Invoke after gameplay or engine changes, or when unity-game-director needs
  a final verification gate.
paths:
  - "**/*.cs"
  - "Assets/_Project/**"
  - "ProjectSettings/**"
  - "tools/**"
disable-model-invocation: false
---

# Unity QA & Build

Specialized verification and release skill. The `unity-game-director` invokes this (or its patterns) as the final gate before declaring any major feature, vertical slice, or game "done".

## Using in Cursor

- Mention Unity QA, profiling, builds, release readiness, or invoke **@unity-qa-build** in chat.
- For code-level bug/perf review after changes, also use subagent **gamedev-qa-tester**.
- For TDD loops on mechanics, combine with **@tdd-iteration** before QA gates.
- **Never claim completion without evidence** — profiler metrics, test results, or build output.

## Core Mandate

**Never claim completion without evidence.** This skill enforces measurable quality gates using Unity's built-in tools + lightweight automation. It produces build reports, profiler snapshots, test results, and risk assessments.

## Key Responsibilities

### 1. Play Mode & Functional Verification

- Core loop must be fully playable for target session length with no blocking bugs.
- Edge cases: Save/load, scene transitions, input device switching, low-end hardware simulation, out-of-memory scenarios.
- Regression testing on previously validated features when adding new systems.
- Provide clear Play mode test scripts or manual checklists the user can follow.

### 2. Performance Profiling & Budgets

- **Mandatory tools**: Unity Profiler (CPU/GPU/Memory/Rendering), Frame Debugger, Memory Profiler (if available), Stats window in Game view.
- Establish budgets early: Target FPS (60/120), max draw calls, memory usage, shader variants, texture memory.
- Capture and analyze snapshots during representative gameplay (combat, procedural generation, UI heavy moments).
- Identify hot paths and propose concrete fixes (pooling, jobification, culling, batching, shader optimization).
- Mobile-specific profiling (if targeting Android/iOS): Use Unity's mobile profiling or device logs.

### 3. Automated Testing (Unity Test Framework)

- EditMode tests for pure logic, data validation, and procedural generators (determinism, edge cases).
- PlayMode tests for integration, input simulation (where possible), and core loop flows.
- Recommend test structure: `Tests/` folder with separate assemblies if using Assembly Definition Files.
- Provide example test code for common cases (e.g., "Given seed X, generator produces valid connected map with Y entities").

### 4. Build Pipeline & Player Settings

- **Build Settings**: Correct scenes included, target platform(s), architecture, scripting backend (IL2CPP recommended for release), API compatibility.
- **Player Settings**: Company/Product name, version, bundle identifiers, icons, splash, orientation, rendering API, graphics APIs stripping, script stripping level.
- **Quality Settings**: Multiple quality levels with appropriate URP/HDRP assets, shadow settings, anti-aliasing, texture quality, etc.
- **Addressables**: Build Addressables content, analyze build size, set up remote catalogs if using CDN.
- Generate or update build scripts (Editor scripts or use Unity's CLI/batchmode for CI).

### 5. Platform-Specific Considerations

- **PC/Standalone**: Resolution, fullscreen, input, crash reporting hooks.
- **Mobile (Android/iOS)**: Keystore, provisioning, target API levels, permissions, IL2CPP + stripping, texture compression, resolution scaling, touch input + safe areas.
- **WebGL**: Specific optimizations (texture compression, code stripping, memory, threading limitations), player settings, and hosting considerations.
- Provide exact Player Settings paths and recommended values.

### 6. Release Readiness Checklist

Before the director can say "done":

- [ ] All critical paths playable end-to-end in Play mode (multiple runs)
- [ ] Performance within budgets on target hardware (Profiler evidence)
- [ ] Build succeeds cleanly for primary platform(s)
- [ ] No obvious visual glitches, clipping, z-fighting, or UI issues in builds
- [ ] Save/load works reliably across sessions and scene changes
- [ ] Input works on intended devices (keyboard/gamepad/touch)
- [ ] Basic accessibility (text contrast, remapping where feasible)
- [ ] Build size analyzed and acceptable (Addressables report if used)
- [ ] Risks documented with mitigation or next-milestone plan

### 7. Risk Reporting & Evidence

Always output:

- Visual / performance scorecard
- Build report summary (size, errors/warnings)
- Key profiler metrics + top hot spots
- Remaining risks (technical debt, content volume, balance uncertainty, platform-specific issues)
- Recommended next actions / milestone scope

## Workflow When Activated

1. **Define Quality Bar** for the current milestone (vertical slice vs polish vs release candidate).
2. **Run Verification Pass**:
   - Play mode testing with specific scenarios
   - Profiler capture + analysis
   - Build attempt + report review
   - Automated tests execution (if set up)
3. **Report Findings** using the template below.
4. **Iterate** with the director until all gates for the current scope are passed.

## Report Template

```markdown
# QA & Build Report — [Milestone / Feature]

## Quality bar
[Vertical slice | Polish pass | Release candidate] — [target platform(s)]

## What was tested
- [Scenario 1]
- [Scenario 2]

## Evidence
| Check | Result | Notes |
|-------|--------|-------|
| Play mode core loop | Pass / Fail / Conditional | |
| Profiler (FPS / frame time) | | |
| Memory peak | | |
| Build (platform) | Pass / Fail | size, warnings |
| Automated tests | X passed / Y failed | |

## Verdict
**Pass** | **Conditional pass** | **Fail** — [one-line reason]

## Top hot spots
1. [System/method] — [metric] — [recommended fix]

## Remaining risks
- [Risk] — [mitigation or defer to next milestone]

## Recommended next actions
1. ...
```

## DeadManZone conventions

### Project context

| Item | Value |
|------|-------|
| Engine | Unity 6, URP-style UI |
| Primary platform | PC Standalone (demo) |
| Target FPS | 60 (frame time ≤ 16 ms) |
| Build scenes | `MainMenu.unity`, `Run.unity`, `CombatArena.unity` |
| Core demo loop | Main Menu → New Run → shop/build → 10-fight gauntlet → win/lose |

### Where tests live

| Scope | Path |
|-------|------|
| Core logic (EditMode) | `Assets/_Project/Core.Tests/EditMode/` |
| Presentation (EditMode) | `Assets/_Project/Presentation.Tests/EditMode/` |
| Integration / UI (PlayMode) | `Assets/_Project/Tests.PlayMode/` |

See **@tdd-iteration** for full batchmode commands and TDD workflow.

### Run tests (required before QA sign-off)

**Preferred — `BatchTestRunner` (handles recompile on startup):**

```powershell
& "<UnityEditorPath>\Unity.exe" -batchmode -nographics `
  -projectPath "<repo-root>" `
  -executeMethod DeadManZone.Core.Tests.Editor.BatchTestRunner.RunEditModeTests `
  -testResults "<repo-root>/TestResults-EditMode.xml" -quit
```

```powershell
& "<UnityEditorPath>\Unity.exe" -batchmode -nographics `
  -projectPath "<repo-root>" `
  -executeMethod DeadManZone.Core.Tests.Editor.BatchTestRunner.RunPlayModeTests `
  -testResults "<repo-root>/TestResults-PlayMode.xml" -quit
```

Optional filter: add `-testFilter "DeadManZone.Core.Tests.EditMode.YourTestClass"`.

**Editor UI:** `Window > General > Test Runner` → Edit Mode / Play Mode → Run All.

### Play mode manual checklist (demo)

- [ ] Main Menu loads; **New Run** starts with faction select
- [ ] Shop drag-drop, rotate (**R** / **Q**), sell for salvage
- [ ] **Begin Fight** → tactics/abilities between combat segments
- [ ] **10 fights** clear → win flow; lose flow on defeat
- [ ] **MENU** mid-run saves and exits; reload restores run state
- [ ] Achievements and local leaderboard persist (`%LOCALAPPDATA%/DeadManZone/`)
- [ ] Combat Arena scene (if changed): spectacle, replay, health bars

Profiler during: shop UI open, active combat with many units, scene transitions.

### Build verification

1. `File > Build Settings` — confirm all three scenes enabled, **PC Standalone** target.
2. `File > Build And Run` — note build size, console warnings/errors.
3. Smoke-test the built player: menu → full run → save/load.

### Lint

After tests pass, run `ReadLints` on every file edited in the change set.

## Cursor + Unity Practical Notes

- Provide exact menu paths and window locations (e.g., `Window > Analysis > Profiler`, `File > Build Settings`).
- Generate Editor scripts that automate common QA tasks (e.g., "Build All Platforms" menu item, profiler snapshot exporter, test runner launcher).
- After suggesting code or scene changes, immediately include the verification steps the user should perform in the Editor.
- For CI/CD: Outline basic GitHub Actions / Jenkins / Unity Cloud Build integration points.

## Integration with Director

This skill is the final gatekeeper. The `unity-game-director` should invoke its patterns (or explicitly this skill) before claiming any major deliverable is complete. It prevents "it works on my machine" syndrome and enforces evidence-based shipping.

Use this skill directly when you want a focused QA/build review of an existing feature or project state.
