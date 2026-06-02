# DeadManZone

Unity 6 vertical slice: Iron Vanguard faction, zoned board loadout, three-lane shop, three-phase deterministic combat, and mid-run save/resume.

## Prerequisites

- Unity 6 (project uses URP-style UI; Input System set to **Both** in Player Settings if prompted)
- Generated content assets (first-time setup below)

## First-time setup

1. Open the project in Unity.
2. Run **DeadManZone → Create Default UI Theme** (once; creates `UiTheme` under Resources).
3. Run **DeadManZone → Generate Vertical Slice Content** (creates pieces, enemies, and `ContentDatabase` under `Assets/_Project/Data/Resources/`).
4. Run **DeadManZone → Setup Main Menu & Run Scenes** (builds `MainMenu` and `Run` scenes with UI wiring).
5. Optional: **DeadManZone → Set Play Mode Start Scene to MainMenu** so Play always opens the main menu.

## Visual theme

UI colors and board zone tints live in [`UiThemeSO`](Assets/_Project/Presentation/Visual/UiThemeSO.cs) (`Assets/_Project/Data/Resources/DeadManZone/UiTheme.asset`). Tune the grimdark palette (steel panels, brass accents, rear/support/front zone colors) in the Inspector, then re-run scene setup if you change prefab wiring. Piece category colors are on each `PieceDefinitionSO` (`categoryTint`); optional `icon` sprites can be assigned per piece.

## Play the slice

1. Press Play (from Main Menu scene recommended).
2. **New Run** → Iron Vanguard.
3. Drag shop offers to the bench or board; use **Lock** and lane **Reroll** as needed.
4. Drag pieces on the board to reposition or move to bench; drop on **Sell** to refund.
5. **Begin Fight** → issue between-phase commands → advance combat.
6. **Save & Exit** returns to the main menu; **Continue** restores the run.

## Running tests

### Unity Test Runner (recommended)

1. **Window → General → Test Runner**
2. **Edit Mode** → **Run All** (core logic, shop, combat determinism, save round-trips)
3. **Play Mode** → **Run All** (UI smoke tests, save in play mode)

Regression coverage for the vertical slice lives in `VerticalSliceRegressionTests` (fixed-seed combat for all five enemy templates, save/load for every `RunPhase`).

### Command line (Unity batch)

Adjust the Unity editor path for your install:

```bash
Unity.exe -batchmode -nographics -projectPath "<path-to-DeadManZone>" -runTests -testPlatform editmode -testResults "<path-to>/TestResults-EditMode.xml" -quit
```

```bash
Unity.exe -batchmode -nographics -projectPath "<path-to-DeadManZone>" -runTests -testPlatform playmode -testResults "<path-to>/TestResults-PlayMode.xml" -quit
```

## Project layout

| Path | Purpose |
|------|---------|
| `Assets/_Project/Core/` | Board, shop, combat simulation (deterministic, Unity-free) |
| `Assets/_Project/Game/` | `RunOrchestrator`, `RunManager`, save/load |
| `Assets/_Project/Presentation/` | UI, drag-drop, combat director |
| `Assets/_Project/Data/` | ScriptableObjects and content pipeline |
| `docs/superpowers/` | Design spec and implementation plan |

## Design docs

- [Autobattler design spec](docs/superpowers/specs/2026-05-31-deadmanzone-autobattler-design.md)
- [Vertical slice implementation plan](docs/superpowers/plans/2026-05-31-deadmanzone-vertical-slice.md)
