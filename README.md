# DeadManZone

Unity 6 demo: 10-fight gauntlet, 3 playable factions (Ironmarch Vanguard, Dust Scourge, Cartel of Echoes), neutral + 2 enemy faction variety, tick combat with tactics, synergies, salvage, achievements, and local leaderboards.

## Prerequisites

- Unity 6 (URP-style UI; Input System set to **Both** if prompted)
- Generated demo content (first-time setup below)

## First-time setup

1. Open the project in Unity.
2. Run **DeadManZone → Generate Demo Content (5 Factions)** (pieces, factions, enemies, ContentDatabase).
3. Run **DeadManZone → Create Default UI Theme** (once).
4. Run **DeadManZone → Setup Main Menu & Run Scenes**.
5. Optional: **DeadManZone → Set Play Mode Start Scene to MainMenu**.

Legacy vertical slice only: **DeadManZone → Generate Vertical Slice Content**.

## Playable factions

| Faction | Unlock |
|---------|--------|
| Ironmarch Vanguard | Default |
| Dust Scourge | After first campaign win |
| Cartel of Echoes | After first campaign win |

See [Demo guide](docs/demo-guide.md) for enemy factions, systems, and known issues.

## Play the demo

1. Press Play (Main Menu scene recommended).
2. **New Run** → choose a playable faction.
3. Build phase: drag shop offers to board/reserves; **R** / **Q** to rotate while dragging; sell for salvage refunds.
4. **Begin Fight** → issue tactics/abilities between combat segments.
5. Clear **10 fights** to win. **MENU** saves and exits mid-run.

## Meta features

- **Achievements** — 10 demo achievements (local save; Steam stub in `SteamIntegration.cs`)
- **Leaderboard** — top scores stored locally under `%LOCALAPPDATA%/DeadManZone/`
- **Emergency Draft** — once per run when manpower gate blocks Begin Fight

## Running tests

### Unity Test Runner

1. **Window → General → Test Runner**
2. **Edit Mode → Run All**
3. **Play Mode → Run All**

New coverage: `SynergyEngineTests`, `CriticalMassRulesTests`, `SalvageCalculatorTests`, `TacticEffectsTests`, `MetaProgressionServiceTests`.

### Command line

```bash
Unity.exe -batchmode -nographics -projectPath "<path-to-DeadManZone>" -runTests -testPlatform editmode -testResults "<path>/TestResults-EditMode.xml" -quit
```

## Project layout

| Path | Purpose |
|------|---------|
| `Assets/_Project/Core/` | Board, shop, combat, meta (deterministic, Unity-free) |
| `Assets/_Project/Game/` | RunOrchestrator, RunManager, Steam stub |
| `Assets/_Project/Presentation/` | UI, drag-drop, combat director, VFX |
| `Assets/_Project/Data/` | ScriptableObjects and content pipeline |
| `docs/` | Design specs and demo guide |

## Design docs

- [Autobattler design spec](docs/superpowers/specs/2026-05-31-deadmanzone-autobattler-design.md)
- [Demo guide](docs/demo-guide.md)
