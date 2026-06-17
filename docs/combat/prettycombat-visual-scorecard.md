# Pretty Combat Pass — Visual Scorecard

Branch: `prettycombat`  
Date: 2026-06-15  
Scope: Iron Vanguard premium combat vertical slice (animated units, trench ring, Apocalypse HUD).

## Encounter

- **Seed:** `424242`
- **Player:** `ironmarch_hq`, 2× `ironmarch_rifle`, `ironmarch_heavy_tank`, `field_gun_nest`
- **Enemy:** `ironmarch_hq`, 2× `ironmarch_rifle`
- **Layout builder:** `CombatSliceLayouts.BuildIronVanguardSkirmish`

## Bootstrap (run once per fresh clone)

```
DeadManZone → Combat Arena → Launch Iron Vanguard Slice (Bootstrap All)
```

Or individually: Import Apocalypse HUD, Create/Refresh VFX + Animation sets, Rebuild Combat Infantry Animator, Apply Iron Vanguard Slice Environment.

## Visual Quality Rubric (target ≥ 4/5 each)

| # | Criterion | 4/5 bar | Score | Evidence |
|---|-----------|---------|-------|----------|
| 1 | Unit idle/walk/attack/death | All four visible in one fight | _pending_ | `Assets/_Project/Art/QA/CombatPrettyPass/` |
| 2 | Vehicle combat read | Tank fires with cannon VFX + audio | _pending_ | Mid-fight screenshot |
| 3 | Building presence | HQ + field gun are Synty meshes | _pending_ | Scene + play screenshot |
| 4 | Battlefield ground | Synty dirt, no missing materials | _pending_ | Play screenshot |
| 5 | Trench ring | No void at camera edges | _pending_ | Wide play screenshot |
| 6 | HUD clarity | Synty bars top-center; drop on damage | _pending_ | Before/after damage screenshot |
| 7 | Hit feedback | VFX + SFX + damage pop same tick window | _pending_ | Frame-step or slow replay |
| 8 | Performance | ≥55 FPS @ 1080p; no bar-path GC spike | _pending_ | Profiler screenshot |
| 9 | Automated tests | Spectacle + replay + slice tests green | _pending_ | Test Runner log |
| 10 | Reproducibility | Seed 424242 → same layout | _pending_ | `IronVanguardSliceLayoutTests` |

Capture menu: `DeadManZone → Combat Arena → Pretty Combat Pass — Capture Screenshot`

## Pretty Combat Pass (HUD / VFX / audio baseline)

| Area | Before (prototype) | After (prettycombat) | Status |
|------|-------------------|----------------------|--------|
| Army health bars | Flat colored `Image` fills | `HUD_Apocalypse_HealthBar_02` Synty prefab | Implemented |
| Checkpoint notches | Thin grey lines | Overlay notches at 75% / 30% | Implemented |
| Side labels | None | ALLIED / HOSTILE TMP labels | Implemented |
| Damage numbers | Plain red TMP | Bold outlined pop-up with scale pulse | Implemented |
| Arena lighting | Single weak key light | Key + fill + rim directional lights | Implemented |
| Combat audio | Silent hits | Rifle, cannon, impact, explosion, death | Implemented |
| Fog / ambient | Basic trench fog | Denser fog + trilight ambient | Implemented |

## Evidence Checklist

1. Run bootstrap menu (above)
2. Open `Run` scene → enter combat with Iron Vanguard slice board
3. Play through first shot + first death (verify HUD bars stay top-center — no UI freeze)
4. Capture 3 screenshots via editor menu
5. Run Test Runner: EditMode + PlayMode (include `IronVanguardSliceLayoutTests`, `IronVanguardSlicePlayModeTests`)
6. Fill rubric scores above

## Key Files

- `CombatSliceLayouts.cs` / `CombatSliceConstants.cs` — slice board builder
- `CombatSliceLauncher.cs` / `CombatSliceEnvironmentBootstrap.cs` — editor bootstrap
- `CombatHealthBarUiFactory.cs` / `ArmyHealthBarView.cs` — Synty HUD
- `CombatArenaAudioPresenter.cs` — `PlayClipAtPoint` only (no UI transform moves)
- `HumanoidCombatVisualDriver.cs` — idle/walk/shoot/death
- `CombatArenaConfig.asset` — trench ring preset (`spawnPerimeterProps: true`)

## Remaining Manual Steps

- Screenshot evidence requires Play mode capture (attach PNGs to PR)
- Profiler spot-check during slice combat
- Standalone build smoke test for Resources refs
