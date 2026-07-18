# Handoff — Full-Faction Rollout + Ark/Combat Stabilization (2026-07-17)

**Point a fresh session here.** Branch: `shopvisualrefreshv1`. This session took the game from
1 playable faction / 19 pieces to **8 playable factions / 103 pieces** with temp art, a new
faction select, an 8-faction enemy rotation with bosses, all §4 tentpole sim tech, and several
rounds of owner-playtest fixes. `docs/GDD.md` was updated in the same commit as every rule
change and remains authoritative.

---

## ⚠️ FIRST ACTION OF THE NEXT SESSION — uncommitted work needs verification

The working tree holds **authored-but-never-compiled** fixes (Unity was closed when the last
agent finished; zero test runs, zero compile):

1. **Transport cross-early pathing** — `Core/Combat/ShapePathfinder.cs` (`straightLineBias`
   param + `NeedsCrossing`), `Core/Combat/TickCombatRun.cs` (transport branch passes the bias).
   Fixes owner-video-confirmed bug: crossing the neutral column costs 200 vs 100, so the greedy
   step picker walked the Ark down its own side of the wire (fully exposed, died at 150 dmg)
   instead of crossing. New tests written, never run:
   `TickCombatRunTransportTests.TransportTarget_ToFarSideBottomMiddle_CrossesNeutralColumnEarly_NotAfterDescending`
   + new file `ShapePathfinderCrossingTests.cs` (pins fixed AND legacy tie-break behavior).
2. **March fluidity tuning** — `CombatArenaConfig.asset` `moveSpeedPresentationScale`
   **2.2 → 0.92** (2.2 was outside the field's own 0.5–1.5 range — the dominant cause of the
   step-then-freeze cadence: units covered each sim step at double speed then idled),
   `CombatUnitActor.ChaseGoalBias` 0.3→0.15, infantry turn 720→450°/s, vehicle 240→360°/s,
   `CombatArenaConfigSO.topTroopsChaseSpeedMultiplier` class default 1.2→1.0 (live asset was
   already 1.0).

**Do:** open Unity → assets-refresh → fix any compile fallout → EditMode (baseline **628** +
the 3 new tests) + PlayMode (**14**) green twice → owner plays (Ark top-right, target bottom
far side, HOLD THE LINE: it must cross the wire within a few steps and march continuously) →
commit. Owner is the final acceptor on feel; tuning dials are documented in comments at
`CombatDirector.EmptyTickPaceScale` and `CombatUnitActor`.

Also uncommitted/untracked: `VERIFY_ARK.md` (repo root, replayable verification snippets —
useful, keep untracked or commit as you like) and the standing exclusion set (below).

---

## Commit trail (this session, newest first)

| Commit | What |
|---|---|
| 7e349e8a | Advance/HoldTheLine always available (owner rule); cadence MoveTowards fix (superseded-in-part by the uncommitted tuning) |
| b9742ba4 | Boss-pending fight entry in ShopV2 (soft-lock fix); transports ignore doctrine gates |
| cdba291c | Ark r4+5: cargo overlays the piece, aim marker fixed, mid-pause no re-gate |
| a2b045d0 | Ark r3: cargo truly leaves the board; gate-only targeting; panel off-board |
| 8e7366b5 | Ark r2: 2×2 footprint hold, save eviction, root-canvas fix |
| 59c6feb8 | Ark r1: cargo UI, deploy targeting, unload presentation (+CarrierInstanceId save fix) |
| 050c429e | Fight-option tiers sort by strength; legacy morale strip deleted |
| fecda8d9 | **Legacy builders retired**: RunSceneSetup deleted, "Refresh Run Scene" gone |
| 8a985bd7 | **Run.unity restored from 611de848** after regen stomped the ShopV2 flip |
| a18c9273 | Run-scene stabilization (legacy-HUD lock ordering; icon regen persistence) |
| c91c84fe | W5: 80 enemy templates (10/faction), boss boards, 8-faction rotation, faction-aware template lookup |
| 9d00e329 | W4: faction select — 8-crest grid + detail pane, icon-forge crests |
| 17712f24 | W3: temp art — 103 placeholder icons, model reuse by role, primitive fallbacks, ring tints |
| 50fe0a6b | W2: 103 pieces / 8 factions / identity stacks; **AssetDatabase ghost-instance fix** |
| f4f3f138 | W1b: mercenary slot, salvage pity, off-faction rules, faction passives |
| f071f13c | W1a: suppression, transport, repeat activations, healing, low-state, 3rd window, gas tech |
| a368191f | Owner WIP (strength preview / war footing) committed as-is |
| e23b82ed | Roster v1 (Neutral+IronMarch), RarityPricing (GoldCost deleted), comic-noir model pass |

Earlier same-day: a1e8f39c ring shrink + _Gutter driver, edccd64d first engineering pass.

---

## Hard rules & burned-in lessons (violating these cost hours this session)

1. **Run.unity is hand-authored scene state.** The ShopV2 surface lives ONLY in the scene.
   Never regenerate it (the builders that could are deleted; CLAUDE.md gotcha documents it).
   UI work = in-editor edits (Unity MCP gameobject tools + scene-save), never runtime codegen.
2. **AssetDatabase ghost instances:** after `CreateAsset`, field writes on the pre-import
   instance silently never serialize. Always `ImportAsset(ForceSynchronousImport)` + reload
   canonical (`DemoContentGenerator.LoadOrCreate` has the pattern + comment). SaveAssets per
   factory batch, never once at the end. Never validate in-memory returns before the flush.
3. **`ExecuteMenuItem` via script-execute MCP-times-out while STILL RUNNING.** Never refire —
   wait 40–60s, check console-get-logs/disk. Refiring queues duplicate destructive runs.
   Menu roots: content = `DeadManZone/Content/...`, scenes = `DeadManZone/Run/...`.
4. **The test suite DELETES the live save** (`%USERPROFILE%\AppData\LocalLow\DefaultCompany\
   DeadManZone\run_save.json` — shared persistentDataPath). Back up before `tests-run`,
   restore after. Isolation is backlog item #23. Existing backups: `.bak_bug_investigation`,
   `.bak_coordinator` — don't clobber.
5. **Synthetic verification lies.** Agent onClick-invokes and ExecuteEvents clicks "passed"
   three rounds of Ark fixes the owner immediately falsified. Owner-approved protocol: the
   coordinator drives the editor with **computer-use real mouse input** for UI acceptance;
   agents must close reports with "ready for real-input test", never "verified/fixed".
   The owner's own play is the final gate.
6. **FUSE/OneDrive mount:** no `sed -i`/`perl -i` (tears files with NUL bytes — hit us 3×).
   Edit/Write tools only; NUL-scan (`b'\x00'`) anything bash-generated. `.fuse_hidden*` files
   need deleting from the Windows side.
7. **Flaky EditMode trio** (BalancePass EnemyTemplates_EffectiveTotal*, CriticalMass_Raises*,
   SuppressedFightStartEngines*): fails together intermittently, passes on rerun. Rerun once
   before investigating. Root cause (suspect shared static CM state) is backlog #15.
8. **Owner conventions:** all UI hand-authored in-editor; Advance + Hold the Line always
   available to every faction (enforced in `TacticPauseValidator`, single source of truth);
   expected-vs-actual one-liners are how playtest feedback arrives.
9. **Commit hygiene:** owner's unrelated WIP may sit in the working tree — never `git add .`
   blindly. Standing exclusions: `Assets/Plugins/NuGet`, `Assets/TextMesh Pro`,
   `Assets/_Recovery*`, `Combat3D/Models/_Spike`, `Art/Neutral`, `ProjectSettings/Packages`,
   `docs/meshy-styles-jobs-2026-07.md`, `tools/meshy/walk_style*.ps1`, `refs_state.json`,
   `batch_resume.log`, `tools/shop_icon_*.png`, `VERIFY_ARK.md`.

---

## Systems state (what a fresh session can rely on)

- **All 8 factions playable** from the select screen; neutral shop-only. 103 pieces, all
  §4 tentpole tech implemented (suppression, transport, repeat activations, healing,
  low-state, death-shock inversion, 3rd pause window, gas fusion/hijack, merc slot, salvage
  pity, faction passives). All magnitudes `// PROVISIONAL` — no balance pass yet.
- **Enemy rotation:** 80 templates (10/faction own-roster ladders) + Crimson Marshal / Wraith
  Harbinger boss boards; faction-aware `ContentDatabase.GetEnemyTemplate(fight, factionId)`.
  Fight options sort Easy≤Normal≤Hard by strength. Boss-pending rounds show a BOSS card.
- **Armored Ark loop (owner-verified in play):** 2×2 cargo hold overlays the piece's own
  footprint (drag on = load; center grab handle moves/sells the Ark); RESUME gates into
  SELECT TRANSPORT TARGET (window fully hidden, live gold/red hover marker, click = order +
  resume, once per fight); cargo deploys on arrival. Pathing fix pending verification (top).
- **Temp art:** 103 role-glyph icons (regen-safe — assigner runs inside Generate Full Roster),
  humanoid model reuse by combatRole, outlined primitives for modelless vehicles/structures,
  faction ring tints. 9 real comic-noir humanoid models exist.
- **Content regen:** one menu — `DeadManZone/Content/Generate Full Roster (All 8 Factions)`.
- **Meshy pipeline:** `tools/meshy/gen_refs.py` (text-to-image refs, ~6cr/img) → refcheck →
  `run_roster_batch.ps1` (resume-safe). `MESHY_API_KEY` = user env var; inject via
  `[Environment]::GetEnvironmentVariable('MESHY_API_KEY','User')` in spawned shells.

## Backlog (owner-acknowledged)

| # | Item |
|---|---|
| 1 | **Verify + commit the uncommitted pathing/fluidity fixes** (top of this doc) |
| 2 | Meshy credits pending: marksman_doctrine_officer rig+anims + 4 statics (~110–120 cr) — `run_roster_batch.ps1` resumes |
| 3 | Flaky test trio (#15) — shared-state suspect, in owner's ArmyStrength WIP area |
| 4 | Test-save isolation (#23) — suite wipes live saves |
| 5 | Balance pass over all PROVISIONAL numbers (blocked on owner run mileage); note `PieceCombatRating` is not rarity-monotonic — distorts ladders and strength previews |
| 6 | conscript_rifles / iron_guard footprints 1-cell vs spec 2/3 (boss-anchor rework) |
| 7 | Normal units may share the transport's cost-greedy pathing artifact (flagged, unfixed — `RoleEngagement`/`ShapePathfinder` seam, bias currently transport-only) |
| 8 | Fast RESUME+battlefield-click double-input bounce (once-observed, unreproduced) |
| 9 | SKIP DEPLOY escape design review; cargo-hold visual polish (owner fine-tune) |
| 10 | Dormant legacy excision (RunHudPanelBuilder rebuild path, MatchupStrengthView, hidden Shop V1 in Run.unity) — entangled with owner's RunHudView/RunSceneController, do jointly |
| 11 | ShopV2 loose ends: "PPORT" zone-strip clip, literal "Lo" lock glyph, flat board chips |
| 12 | Echo Chairman + 2nd Saint omitted from AI ladders pending rating fix (W5 report) |
| 13 | Shop art for 5 HQ buildings (never render in combat; placeholder icons only) |
| 14 | Oathborn healing-hook passive still soft-TBD (spec §1.9) |

## Key docs
- `docs/GDD.md` — authoritative, updated through this session
- `docs/superpowers/specs/2026-07-15-faction-roster-v1-design.md` — roster/content source of truth
- `docs/superpowers/specs/2026-07-15-comic-noir-template-handoff.md` — art pipeline (add: refs now generate in-toolchain via gen_refs.py)
- `VERIFY_ARK.md` (repo root, untracked) — replayable in-editor verification snippets
