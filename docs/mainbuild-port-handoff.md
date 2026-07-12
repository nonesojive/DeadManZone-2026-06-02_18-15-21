# Main-build port handoff — 3D combat arena → Run flow (2026-07-11)

> **STATUS (2026-07-12): PORTED.** See "## Main-build port (2026-07-12)" at the bottom of
> docs/audit-combat-sim-2026-07-10.md for what landed, live verification, and remaining
> open items (fight-1 framing, grade/ink comparison, 2D deletion list sign-off).
> Combat is switched to 3D via the shared CombatArenaConfig.visualMode; switch back any
> time with `DeadManZone → Combat3D → Switch Combat To 2D Arena`.

**Goal:** replace the 2D sprite combat arena in the real game flow (Run scene / RunOrchestrator) with the proven 3D toon-ink arena + interactive tactics window from `Combat3D_Demo.unity`.

**Read first:** CLAUDE.md, then docs/audit-combat-sim-2026-07-10.md BOTTOM-UP (every section from "Combat3D wiring" down is current state), especially "## Interactive tactics window" (porting notes) and "## Full roster".

## What's proven and portable (all in `Assets/_Project`, regenerable via menu "DeadManZone → Combat3D → Build Combat3D Demo Scene")
- 3D actor path: `CombatUnitVisual3D` behind the `ICombatUnitVisual` seam (2D path still intact); archetype visuals per piece id via `CombatUnitVisual3DInstaller`; ring-fill health (orb drain); rifles + two-hand carry + aim/recoil; punch-in camera; `Combat3DVfxPresenter`; placeholder audio; army HUD; environment builder.
- Tactics window: `CombatTacticOrdersDraft` (emits TacticPausePanel's exact submit shape — `SubmitCombatCommands` just works), `CombatTacticTargetPicker`, `CombatTacticOrdersWindow`. Replace `BuildPauseContext` with the real `GetCombatPauseContext`; surface enemy anchors through the pause context (demo uses `EnemyCombatantsForTests`, strictly conservative vs Core's any-occupied-cell rule); drop roster overrides.
- Content: field_medic → ShieldAllies, ironclad_mortars → MortarShot (real grants, generator-preserved). GrenadeLob→MortarShot rename incl. v8 save migration (`MigrateGrenadeLobRename`).

## Port outline (suggested order)
1. Branch point: `combatvisualv5` tip. Run scene loads combat via `CombatArenaSceneLoader` (additive 2D arena scene) — decide: swap the loaded arena scene for a 3D arena scene built by the same bootstrap pieces, or embed like the demo does (`MarkEmbeddedArenaLoaded`).
2. RunOrchestrator.AdvanceCombat already interleaves Continue/commands — the demo driver's loop is the reference; the real flow keeps ownership, the 3D presenter/director just replace the 2D ones.
3. TacticPausePanel → CombatTacticOrdersWindow swap (or wire the window as the panel's 3D skin; Draft already speaks the same command shape).
4. Environment/bootstrap: the demo scene builder must become a runtime-or-prefab arena the Run flow can instantiate (bootstrap is editor-only today — likely convert generated scene to a prefab or keep a dedicated combat scene in build settings).
5. Keep 2D path deletable-but-present until the port is verified; then the switchover deletion list in the audit doc applies (17 CombatArena2D files + CombatUnitVisual2D + bar factory).

## Open items (owner decisions now RESOLVED — see audit doc "Ability swap-back + non-humanoid treatment spec")
- ShieldAllies: RESOLVED — armored_transport grants it (restored, original design); field_medic has no granted ability.
- Non-humanoid trio: treatment SPECIFIED (MG nest static + shoot/destroyed anims; iron horse animated treads as walk; transport spinning wheels; built-in weapon muzzle points, no rifle prop; skip rig pipeline). Still need ref images + a vehicle mode in generate_unit.py + a vehicle variant of CombatUnitVisual3D.
- Real SFX to replace placeholder WAVs (same filenames under Assets/_Project/Combat3D/Audio/).
- New units: `python tools/meshy/generate_unit.py <unit>` (skill: docs/skills/meshy-unit-pipeline/SKILL.md).

## Session gotchas (hard-won)
- OneDrive: repo files via file tools only, NEVER bash cat/sed/python (stale mirror; two file corruptions this way).
- script-execute >15s times out AND the MCP server RETRIES it — only idempotent operations in long calls.
- Modal dialogs freeze the bridge — fresh-open a scene before menu rebuilds.
- `editor-application-set-state` with only isPaused STOPS play — always pass both flags.
- Tests: EditMode 366/366 as of this commit. PlayMode `CombatArenaReplayPlayModeTests` covers the 2D path.
