# DeadManZone

Unity 6 run-based tactics autobattler. You build an army across two spatial boards (Combat + HQ),
choose one of three fronts, and the fight auto-resolves on a deterministic tick sim with a few
tactical pauses. **Dread** is the run clock (earned only by winning); at its thresholds you fight a
**Boss**. Beat 3 bosses to win; run out of **Manpower** and the run is over.
**All 8 factions are playable** (full-faction rollout, 2026-07-17); every faction also feeds the enemy rotation. Neutral is shop-only, never playable.

## Design source of truth

**[`docs/GDD.md`](docs/GDD.md) is authoritative.** It is verified against `Core/` and cites the file
for every number. **Read it before designing or changing any rule, and update it in the same commit
as the rule change.**

Everything in **`docs/archive/`** (including the three old GDDs and all `superpowers/` plans+specs)
is **SUPERSEDED** and stamped as such — it describes systems that no longer exist (Morale as a run
resource, Gold, 8×2 reserves, 6 shop slots, a fixed 10-fight gauntlet, 3 playable factions). It kept
getting picked up as if current. **Do not design from it.**

Still authoritative: `docs/adr/` (architecture decisions) and `docs/art/style-bible/` (art direction).

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
3. `DeadManZone → Setup Main Menu & Run Scenes` — this is MainMenu-only; it never touches Run.unity (see gotcha below)

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
- **Run-scene UI is hand-authored.** The ShopV2 build surface in `Run.unity` is scene state, not
  code output — `RunUiAuthoringLock` protects it. Never regenerate `Run.unity` from a menu or script
  (the old `RunSceneSetup` builder + "Refresh Run Scene" menu did exactly that and wiped the ShopV2
  flip; both are gone). UI work on the Run scene happens in-editor by hand, not via builders.
