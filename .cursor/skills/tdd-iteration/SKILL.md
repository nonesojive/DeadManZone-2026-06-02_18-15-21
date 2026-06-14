---
name: tdd-iteration
description: Run test-driven development loops. Write tests first, implement, then iterate fixes until all relevant tests pass and lint is clean. Use for new mechanics, combat systems, or refactors.
paths:
  - "**/*.cs"
  - "Assets/_Project/Core.Tests/**"
  - "Assets/_Project/Presentation.Tests/**"
  - "Assets/_Project/Tests.PlayMode/**"
disable-model-invocation: false
---

# TDD Iteration Workflow

When asked to implement or fix a feature/mechanic:

1. **Clarify requirements** — Ask for expected behavior, edge cases, and success criteria if unclear.
2. **Write failing tests first** — Create or update test files for the new/changed behavior. Do NOT implement the feature yet.
3. **Run tests** — Confirm they fail as expected.
4. **Implement minimal code** — Write the smallest amount of code to make tests pass.
5. **Iterate** — Run tests again. Fix failures. Repeat until all tests in scope are green.
6. **Polish & validate** — Run full relevant test suite + any linting. Ensure no regressions in related systems (combat, resources, factions).
7. **Document** — Update any related GDD notes or inline comments.

**Success condition**: All targeted tests pass + no new lint errors.

**Best used with**: Fable 5 or other strong reasoning models. Combine with Plan Mode for complex mechanics.

**Example invocation**:
"Use /tdd-iteration to add a new trench fortification system with destructible cover and resource cost."

## DeadManZone conventions

### Where tests live

| Scope | Path | When |
|-------|------|------|
| Core logic (deterministic, Unity-free) | `Assets/_Project/Core.Tests/EditMode/` | Combat, shop, board, meta calculators |
| Presentation | `Assets/_Project/Presentation.Tests/EditMode/` | UI mappers, camera framing |
| Integration / UI flows | `Assets/_Project/Tests.PlayMode/` | Scene wiring, drag-drop, combat director |

Follow existing patterns: NUnit `[Test]`, shared fixtures in `VerticalSliceTestFixtures`, `TestBoards`, `TestPieces`.

### Run tests (required at steps 3, 5, 6)

**Edit Mode — full suite:**

```powershell
& "<UnityEditorPath>\Unity.exe" -batchmode -nographics `
  -projectPath "<repo-root>" `
  -runTests -testPlatform editmode `
  -testResults "<repo-root>/TestResults-EditMode.xml" -quit
```

**Edit Mode — filtered (preferred while iterating):**

```powershell
& "<UnityEditorPath>\Unity.exe" -batchmode -nographics `
  -projectPath "<repo-root>" `
  -runTests -testPlatform editmode `
  -testFilter "DeadManZone.Core.Tests.EditMode.YourTestClass" `
  -testResults "<repo-root>/TestResults-EditMode.xml" -quit
```

**Play Mode** (only when the change needs scene/UI integration):

```powershell
& "<UnityEditorPath>\Unity.exe" -batchmode -nographics `
  -projectPath "<repo-root>" `
  -runTests -testPlatform playmode `
  -testResults "<repo-root>/TestResults-PlayMode.xml" -quit
```

Replace `<UnityEditorPath>` with the installed Unity editor folder (e.g. `C:\Program Files\Unity\Hub\Editor\6000.x.xf1\Editor`).

### Lint

After tests pass, run `ReadLints` on every file you edited. Fix new diagnostics before marking work complete.

### Iron law

If implementation was written before a failing test existed, delete the implementation and restart from step 2. Do not keep "reference" code and backfill tests.
