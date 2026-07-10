# DeadManZone — combatreworkv4 handoff (greenfield art/presentation rework)

**Purpose:** context dump so a fresh session can start **Phase 0 of the 3D art rework** without re-deriving anything. A full grilling session (2026-07-10) settled the art direction and tech pipeline on paper; **no code has been changed yet.**

- **Repo:** `C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone`
- **Branch:** `combatreworkv4` (currently checked out). The planning docs below are **untracked/uncommitted** — commit them first.
- **Engine:** Unity 6 (6000.3.8f1), URP. Windows 11.
- **Tooling:** Unity MCP connected (`Unity_MCP__DeadManZone___*` tools) — prefer it over hand-editing YAML. Blender MCP + Desktop Commander also available. Ask the user to have the Unity editor open.
- **Read first, in order:** `CLAUDE.md` (layered architecture: Core has NO UnityEngine dependency; Presentation = visuals only), `CONTEXT.md` (glossary — side vs. faction vs. accent language matters), `docs/adr/0001–0003`, then `docs/art/style-bible/50-combat-arena-spec.md`.

---

## What was decided (all documented, don't re-litigate)

| Decision | Where |
|---|---|
| Identity: **DD skin, TT skeleton** — Top Troops owns structure/legibility/camera/VFX coding; Darkest Dungeon owns palette/ink/mood. Legibility always wins on the battlefield. | ADR-0001 |
| Units: **cel-shaded 3D + ink outline shader** (replaces sprite-sheet billboards — deliberate reversal of the old 3D→2D pivot; the old 3D attempt failed on style, not smoothness) | ADR-0002 |
| Meshes: **kitbashed Synty-class humanoid backbone** (one rig, retargeted anims) + **AI-gen showcase pieces** (HQs/walker/bosses), cleaned to match | ADR-0003 |
| Camera: perspective ¾ (~45–55° pitch, FOV 30–40), player left / enemy right, **fixed frame + scripted punch-ins** on kills/crits/HQ hits only | arena spec §1 |
| Side channel: **outline tint + base ring** (player blue / enemy red, reserved hues). Never full-model tint. Neutral units fight for either side — allegiance never baked into art | arena spec §3, CONTEXT.md |
| Faction accents: IronMarch **brass-gold**, Dust Scourge **acid green**, Cartel **spectral violet**, Neutral **none**; Crimson Legion blood-red, Ash Wraiths ash-white | style bibles 10–40 |
| Grid: strong in placement, fades to faint diegetic markings in combat | arena spec §2 |
| Arenas: 3 palette themes (Trenchline / Siege ground / Fog field), one geometry | arena spec §5 |
| Feedback: two status pipelines (material-on-body vs. symbol-above-head), VFX owns the saturation budget, **two escalation tiers only — NO full-screen interrupts** (deliberate) | game bible §5 |

Docs live in `docs/art/style-bible/` (00 game bible, 10/20/30/40 faction bibles, 50 arena spec). The bible is the **acceptance criteria** for every phase, not a mood doc.

## THE NEXT TASK: Phase 0 — kill/keep style spike

Prove the look before any roster work: **one kitbashed unit + toon-ink uber-shader + desaturated grade, screenshotted in the ¾ perspective camera against a graybox board.**

- Pass = reads as "inked illustration". Fail = "generic toon" → stop and revisit ADR-0002 with the user.
- The make-or-break shader term is **interior ink**: material-boundary dark edges with varied stroke weight, not just a uniform exterior outline (game bible §4, arena spec §4).
- Uber-shader requirements (priority order): cel ramp (2–3 hard bands) → exterior ink outline w/ side-tint parameter → interior ink → status hooks (hit flash, desat, dissolve) via MaterialPropertyBlock → masked faction-accent emission channel.
- Build it in a throwaway scene; do NOT touch the existing TopTroops2D arena, Core, or the Run scene.
- Verify visually via Unity MCP `screenshot-game-view` / `screenshot-camera`; judge against game bible §3 (saturation audit) and §2 (black-shape test).
- User has Synty packs in the project (see `prettycombat` history / `Assets`); Blender MCP can kitbash if needed.

Roadmap after Phase 0 (arena spec §8): 1 camera+graybox+side-channel → 2 infantry backbone (4 archetypes) → 3 feedback systems → 4 arena themes → 5 showcase pieces → 6+ other factions (post-demo).

## What carries over from the old pipeline (don't rebuild)

- Core sim + event replay + `CombatDirector` pacing (incl. the v4 lunge fix: `EmptyTickPaceScale` coupled with `moveSpeedPresentationScale` — see `docs/combatvisualv4-handoff.md`).
- Free-chase smoothing (`CombatUnitActor`, chase controller) — presentation-agnostic.
- VFX/material pooling, save-on-completed, HUD layout/hover cards.
- **Being replaced (eventually, not in Phase 0):** `CombatUnitVisual2D` billboard pipeline, sprite-sheet VFX, the 46-PNG 2D art brief, grayscale+multiply faction tint.

## Housekeeping / gotchas

- Sandbox bash lacks git-lfs — use `GIT_LFS_SKIP_SMUDGE=1` or run git via Desktop Commander on the real machine.
- 345/345 EditMode tests were green at last check — run `tests-run` (EditMode) after any change; Phase 0 shouldn't touch Core at all.
- Don't hand-edit `.meta` files. After editing `.cs` outside the editor, `assets-refresh` before testing.
- Stale names in old docs: "Iron Vanguard" = IronMarch Union; `crimson_*` = Crimson Legion enemy pool (see CONTEXT.md flagged ambiguities).
- First action for the new session: **commit CONTEXT.md, docs/adr/, docs/art/style-bible/, and this handoff to `combatreworkv4`.**
