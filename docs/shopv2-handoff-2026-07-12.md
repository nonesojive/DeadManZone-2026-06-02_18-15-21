# ShopV2 handoff — session end 2026-07-12 (night)

**State:** branch `shopvisualrefreshv1`. This session's commits, in order: `4ac48a71`
(port wave 1: icons, lock toggle test, presenters, live-bound lab scene), `9aa03720`
(port wave 2: hovercard binding, Run-scene staging, flip checklist). Suites green:
**450 EditMode / 14 PlayMode**. Working tree clean apart from pre-existing noise
(`Assets/_Recovery*`, `Untitled.blend*`, `*.prebranch.bak`) and untracked `Screenshots/`
(session review artifacts, deliberately uncommitted).

**Read first, in order:** CLAUDE.md → `docs/shopv2-flip-checklist.md` (the entire
remaining job + validation gate) → this file.

## Where the shop rework is now

Everything up to the flip is DONE and owner-approved at each gate:

- **Icon set (41 glyphs).** Bone-on-transparent silhouettes: resources/economy, all 7
  damage types, all 7 combat roles, every critical-mass rule tag (primaries, run,
  synergies, abilities, IronMarch anvil crest), plus hp/morale/lock utility glyphs.
  Sprites at `Assets/_Project/Art/UI/Icons/{64,128}` (Sprite/no-mips/uncompressed/128max).
  Vector masters + generator: `ArtSource/IconMasters/make_icons.py` — the single source
  of truth; regenerate, never hand-draw. Kit 9-slice plates in `Art/UI/Kit/`.
- **`dmz-icon-forge` skill** installed on the owner's Cowork (also produced as a .skill
  package): palette law, reserved-metaphor table, full pipeline. Use it for any new icon.
- **Lab scene `ShopBuildV2.unity`** — fully authored greenfield shop per approved
  wireframe v3 (in session outputs; geometry: HQ region sized 4×8 max with 3×6 grid,
  6×6 combat, 6×2 reserves aligned beneath, 8-slot shop band w/ 3 dormant slots +
  Smelter, right column trays, Front Report modal authored inactive). No builder code —
  scene is authored objects; presenters only fill data.
- **Presenters** (`Presentation/ShopV2/`, all bind authored children BY NAME):
  `ShopV2PresenterBase` (RunStateChanged subscribe/retry), CommandBar, BuffsRail
  (BuffStripEvaluator), WarFooting (ArmyStrength vs chosen StrengthPreview), ShopBand
  (+`ShopV2OfferSlotInput`: left-click acquire-to-reserves scan, right-click lock
  toggle, hover → hovercard), FightOrders (modal/tray/BeginCombat gating),
  `ShopV2IconLibrary` (id→sprite, auto-populate), `ShopV2HovercardView`/`Presenter`
  (binds approved card prefabs; structure-primary pieces route to the building card).
- **Core changes:** `ReservesState` 8×2 → **6×2** with wider-legacy save migration
  (keep anchor → repack → drop only on overflow; `ReservesSnapshotMigrationTests`).
  Offer-lock mechanic PRE-EXISTED in `RunOrchestrator.Shop` (locks pin slots through
  reroll via fixedSlots, extra locks cost Authority) — verified, toggle-off test added.
- **Run.unity staging:** `ShopV2Canvas` sits INACTIVE as a root object (overlay,
  sorting 10 — below Front Report 250). Live game verified unaffected.

## The next job: the flip

`docs/shopv2-flip-checklist.md` is the whole plan — 8 steps + validation gate.
Owner-present by design: it re-parents REAL board views (`Canvas/RunScene/ShopScene/
MainRow/BoardArea` sections) into the ShopV2 frames, retires old chrome (toggle off,
never delete before sign-off), re-docks the real CriticalMassDrawer, and needs drag/drop
playtesting between step groups. Deferred TODOs listed at the checklist bottom
(hovercard flavor text, chrome cross-cases, dormant-slot unlock wiring, buffs tooltips).

## Working agreements (unchanged from last handoff; kept all session)

Subagents do FILE work with tight specs (no Unity MCP/bash/asset edits); parent session
owns the editor: `assets-refresh` to compile, `tests-run` for suites (scenes must be
saved), `script-execute` for editor ops + play-mode smokes, `screenshot-camera` to
verify. Stamp-don't-regen for content. Save schema v10 additive. Determinism via
SeedStreams. OneDrive repo = file tools only for edits.

## New gotchas earned this session

- **Play-mode edit trap:** the owner may enter Play Mode while you work — `script-execute`
  edits during play evaporate on exit. Check `editor-application-get-state` before any
  scene mutation; exit play (set-state), re-apply, save. Keep mutation scripts idempotent.
- **`screenshot-game-view` serves a stale buffer in edit mode** (known) — the lab canvas
  is ScreenSpaceCamera in `ShopBuildV2.unity` precisely so `screenshot-camera` renders
  fresh. The staged copy in Run.unity was switched to Overlay (order 10).
- **New PNGs import as the wrong texture type** — a sprite that renders as a solid
  tinted square means the importer pass didn't run. Always run the Sprite/no-mips pass
  after adding icons (see the icon-forge skill).
- **ImageMagick fallback SVG renderer** (session art pipeline): no `<symbol>/<use>`, no
  nested `transform="rotate()"` (elements fly off-canvas — compute rotations numerically),
  `&` must be `&amp;`, use `PNG32:` to force alpha.
- **Presenter name-binding:** authored child names are the API (`OfferSlot_i`,
  `Val_ARMY STRENGTH` — note spaces; hovercard damage row children carry the mock's
  attack type in their names → match by `Val_DAMAGE`/`Lbl_DAMAGE`/`Icon_DAMAGE` prefix).
  Renaming scene objects breaks bindings silently except for the one consolidated
  LogWarning each presenter emits.
- **`RunState.FightIndex` is already the human-facing fight number** (initializes to 1)
  — display it raw, no +1.
- **Board structures (MG nest) are `PieceCategory.Unit`** with `primary: structure`;
  only HQ pieces are `Category.Building`. Anything routing unit-vs-building must check
  the primary tag too.
- **Sandbox git can't read this repo's index** (unchanged) — but Desktop Commander's
  PowerShell works for commits (`git -C <repo> add <paths> && commit`); its
  `interact_with_process` often can't reuse a finished shell — start a fresh process
  per command chain.
- **Smoke-driving a run without the menu:** in play mode, `RunManager.Instance` (or
  AddComponent on a fresh GO) → `InitializeOrchestrator()` → `StartNewRun(FactionIds.
  IronmarchUnion)` gives presenters a real run in the lab scene; Run.unity boots to the
  in-scene main menu ("Until The Trenches Fall") — the old ShopScene activates through
  the normal flow, not on load.

## Queued after the flip (unchanged from previous handoff)

Enemy pool re-authoring onto neutral/crimson_legion/ash_wraiths, Dust Scourge/Cartel
content passes, recon intel ladder UI, 3D building visuals, boss commander models,
enter-seed UI, owner-driven tuning (MoraleRules death-shock feel, DreadRules/
RarityWeights constants).
