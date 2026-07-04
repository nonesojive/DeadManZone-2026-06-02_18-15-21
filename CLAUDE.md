# DeadManZone

Unity 6 demo: 10-fight gauntlet, 3 playable factions (IronMarch Union, Dust Scourge, Cartel of Echoes), tick-based combat with tactics, synergies, salvage, achievements, and local leaderboards. See [README.md](README.md) and [docs/demo-guide.md](docs/demo-guide.md) for player-facing details.

## Architecture

Layered under `Assets/_Project/`:

| Path | Purpose |
|------|---------|
| `Core/` | Board, shop, combat, meta rules — deterministic, **no UnityEngine dependency**. Keep it that way; this is what makes `Core.Tests` fast EditMode tests. |
| `Game/` | RunOrchestrator, RunManager, Steam stub — wires Core into a Unity run loop. |
| `Presentation/` | UI, drag-drop, combat director, VFX. Reads from Core, never contains game rules. |
| `Data/` | ScriptableObjects and the content-generation pipeline (faction/piece/enemy data). |

When adding gameplay logic, ask which layer it belongs to before writing it — rules go in `Core`, wiring in `Game`, visuals in `Presentation`. Don't leak `UnityEngine` types into `Core`.

## Editor setup commands

First-time / after a content pipeline change, run from the Unity menu (not CLI):

1. `DeadManZone → Generate Demo Content (5 Factions)` — pieces, factions, enemies, ContentDatabase
2. `DeadManZone → Create Default UI Theme` (once)
3. `DeadManZone → Setup Main Menu & Run Scenes`

IronMarch-specific content pass: `DeadManZone → Content → Generate IronMarch Union Content Pass`.

## Tests

Three assemblies: `Core.Tests` (EditMode), `Presentation.Tests` (EditMode), `Tests.PlayMode`.

- In-editor: Window → General → Test Runner → Edit Mode / Play Mode → Run All.
- CLI: `Unity.exe -batchmode -nographics -projectPath "<path>" -runTests -testPlatform editmode -testResults "<path>/TestResults-EditMode.xml" -quit`
- The Unity MCP `tests-run` tool can trigger these directly without shelling out.

New `Core` logic needs `Core.Tests` coverage (deterministic, no Unity APIs required — keep them fast).

## Tooling available in this session

- **Unity MCP** (`com.ivanmurzak.unity.mcp` + extensions) is connected — prefer its tools (`gameobject-*`, `scene-*`, `assets-*`, `animator-*`, `tests-run`, etc.) over hand-editing scenes/prefabs/assets as raw YAML.
- After editing `.cs` files outside the Editor, call `assets-refresh` to force recompilation before relying on Play Mode or test results.
- Don't hand-edit `.meta` files — Unity owns GUIDs there.
- A vendored skill set lives in `.agents/skills/` (sourced via `skills-lock.json`, restored with `npx skills`) and is gitignored — don't expect it to be present on a fresh clone until re-synced.

## Coding conventions

Lazy-senior-dev / YAGNI bar (mirrors `.cursor/rules/ponytail.mdc`):

- No abstractions beyond what's asked; no speculative extensibility.
- Prefer deletion over addition; fewest files, boring over clever.
- Non-trivial logic leaves one runnable check behind (a test or assert-based self-check) — trivial one-liners don't need one.
- Don't skimp on input validation at trust boundaries, error handling that prevents data/save loss, or anything explicitly requested.

## Known gotchas

- Dust Scourge and Cartel of Echoes are unlocked after a campaign win, and hidden until their content passes land — don't assume they're playable in a fresh checkout.
- Steam achievements/leaderboards are stubbed (`SteamIntegration.cs`) pending Steamworks SDK wiring.
- Leaderboard/save data lives under `%LOCALAPPDATA%/DeadManZone/`, not in the repo.
