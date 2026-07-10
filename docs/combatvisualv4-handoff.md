# DeadManZone — combatvisualv4 handoff (for the sprite-shader chat)

**Purpose:** Context dump so a fresh chat can start the **sprite shader work** without re-deriving
everything. The combat-presentation punch list for this branch is done; shaders are the next task.

- **Repo:** `C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone`
- **Branch:** `combatvisualv4` (branched from combatvisualv3). Not yet merged/pushed to master.
- **Engine:** Unity 6 (6000.3.8f1), URP. Windows 11, PowerShell primary shell.
- **Tooling:** Unity MCP (`com.ivanmurzak.unity.mcp`) is connected — prefer its tools
  (`gameobject-*`, `assets-*`, `assets-shader-*`, `assets-material-create`, `tests-run`,
  `screenshot-game-view`, `script-execute`, `editor-application-*`) over hand-editing YAML.
- **Read first:** `CLAUDE.md` (layered architecture: Core has NO UnityEngine dependency; Game wires;
  Presentation = visuals/rules-free; Data = ScriptableObjects). Sprite shader work lives in
  **Presentation** + shader/material assets under `Assets/_Project/`.

---

## THE NEXT TASK: sprite outline shader (P4)

The user wants to begin **sprite shader work** to push combat toward Top Troops-level quality.
The scoped item is a **per-unit outline shader** (rim/silhouette outline so units pop from the
battlefield), with room to grow into other sprite effects (hit flash is already done in C#; a
shader could unify it, plus team-color tint, damage desaturation, etc.).

### Critical constraint discovered this session — READ BEFORE WRITING THE SHADER
Combat units are **NOT Unity SpriteRenderers.** In this URP arena setup SpriteRenderers do not draw.
Units are rendered as **camera-facing mesh-quad billboards** built at runtime by
`CombatUnitVisual2D` (`Assets/_Project/Presentation/Combat/Arena/CombatUnitVisual2D.cs`), textured
from **per-state sprite sheets** (idle / walk / attack / die). Material/shader plumbing:
- `Assets/_Project/Presentation/Combat/Arena/CombatArena2DSpriteMaterial.cs` (caches shaders/materials)
- `Assets/_Project/Presentation/Combat/Arena/CombatArena2DVfxSpriteAnim.cs` (VFX strip anim)
- `Assets/_Project/Presentation/Combat/Arena/CombatArena2DSpriteMetrics.cs` (alpha-bounds/feet scan)

**The gotcha for an outline shader:** each unit texture is a **sprite SHEET** (multiple frames in
one texture). The quad shows one frame as a **UV sub-rect** at a time. A naive outline (sample
neighbors in UV space) will **bleed across adjacent frames** in the sheet. The shader must **clamp
outline sampling to the current frame's UV rect** (pass the active frame rect as a material
property / `MaterialPropertyBlock`, and treat out-of-rect samples as empty/transparent). Confirm how
the current frame's UV rect is set on the quad material (look in `CombatUnitVisual2D` /
`CombatArena2DVfxSpriteAnim`) and thread the same rect into the outline shader.

- Unit textures were shrunk this branch: **3584² → 1024²** per-state sheets, DXT5 compressed
  (fixed per-fight load hitches + memory). Frames are sub-rects within these.
- Textures live under `Assets/_Project/Art/Combat2D/Units/Animations/` (91 `.meta` files were
  reimported at maxTextureSize 1024). Some are `isReadable` only if a bounds scan needed it.

### Suggested starting moves for the shader chat
1. Find the current unit quad shader/material: `assets-shader-list-all`, and read
   `CombatArena2DSpriteMaterial.cs` to see which shader is assigned and how frame UVs are pushed.
2. Decide: extend the existing shader with an outline pass vs. a new URP shader. Keep it URP-compatible.
3. Implement outline with **frame-rect UV clamping** (the key correctness issue).
4. Verify in Play Mode via Unity MCP: start a run, buy units, `BeginCombat`, `screenshot-game-view`.
   The `script-execute` play-mode setup recipe is in the "How to drive a combat in Play Mode" section.
5. Keep `Core.Tests` green (`tests-run` EditMode) — shader work shouldn't touch Core, but run anyway.

---

## What was accomplished on this branch (combatvisualv4)

The branch brought combat presentation to near-Top-Troops quality. Highlights (all committed):

- **Board & shop layout** rebuilt after the user removed the center area and enlarged board/shop:
  3 board placements (Combat / HQ-building / Reserves) arranged for readability with largest tiles
  that fit; shop cards likewise. (`Run.unity` section rects.)
- **Hover cards** self-provision a floating panel opposite the pointer (center column was deleted);
  z-order fix (overriding Canvas sortingOrder 500) + on-screen corner clamp.
  (`PieceHoverCardController.cs`, `UnitCardPanelView.cs`)
- **Perf/jitter:** `Time.maximumDeltaTime` clamp, VFX material/GameObject pooling, cached shaders;
  save-freeze fixed (per-segment O(n²) save → save only on combat Completed) in `RunOrchestrator.cs`.
- **First-fight/new-unit hitch:** unit textures 3584²→1024² + DXT5 (see above).
- **Post-processing:** grimdark URP volume (ACES tonemap, bloom, vignette, split-tone, grain) via
  `CombatArenaPostFx.cs` + `CombatArenaBootstrap.cs`; **settings toggle** to disable it
  (`GraphicsSettings.cs`, `MenuOptionsPanel.cs`).
- **Battlefield color** shifted olive/khaki to match unit-art tone (`CombatArenaConfig.asset`).
- **Defeat-path arena unload** (arena no longer lingers behind shop) — `CombatArenaSceneLoader.cs`,
  `CombatArenaSession.cs`, `RunSceneController.cs`.
- Earlier: HP bars above head, blob shadows, bullet-look shoot VFX at shoulder height, board tiles.

### The last thing fixed: advancing-unit "lunge" (movement)  — DONE, verify feel when testing
Slow units (bulwark, field marshal) marched then **lunged forward ~1–2 squares**. This was NOT a
smoothing bug — it was a **playback rate mismatch**:
- A unit takes `100/(movementSpeed+1)` = **20–50 sim ticks to cross one cell** (units are speed 1–4).
- The director **skipped every event-less "charge" tick**, so a lone marcher's grid anchor jumped a
  full cell per *event* tick (~0.1s) — tens of × faster than its calibrated walk speed. The visual
  fell 3–4 cells behind and caught up in a lunge. Ranged units that barely march didn't show it.

**Fix (commit `b5b43f99`), 3 files:**
- `Assets/_Project/Presentation/Combat/CombatDirector.cs` — pace the empty charge ticks too, at
  `EmptyTickPaceScale = 0.45` of a full tick (const), so the anchor advances at ~walk speed. Event
  ticks keep full pacing (attack/volley timing unchanged). `MaxPacedEmptyTicks = 64` still compresses
  pathological dead air.
- `Assets/_Project/Data/Resources/DeadManZone/CombatArenaConfig.asset` — rescaled walk to match new
  real-time-per-cell: `moveSpeedPresentationScale 1.1 → 2.2`, `topTroopsChaseSpeedMultiplier 1.2 → 1.0`.
  **These two are coupled to EmptyTickPaceScale** — if marches feel too slow/fast, change the pace
  scale AND the presentation scale together (lower pace scale ⇒ higher presentation scale).
- `Assets/_Project/Presentation/Combat/Arena/CombatUnitActor.cs` — free-chase SmoothDamp follow of
  the smoothed anchor (biased toward goal, no hard-stop); `ChaseMaxSpeedScale 1.6 → 1.25`.

**Measured (bulwark, per-frame visual-vs-anchor lead):** was `-0.78..-3.30` cells → now
`-0.99..+0.60` (tight tracking, no accumulation). 345/345 EditMode tests pass.

**OPEN QUESTION for the user to confirm when they next play:** marches now play at true brisk walk
pace (no compression), so **approaches take a bit longer** than before. If it feels too slow, adjust
the coupled `EmptyTickPaceScale` / `moveSpeedPresentationScale` pair.

---

## Combat / movement architecture cheat-sheet

- **Sim is deterministic, tick-based** (Core): `CombatPacingConfig.TicksPerSecond = 10`.
  `CombatMovementSpeed`: `chargePerTick = movementSpeed + 1`, `NormalStepChargeCost = 100` ⇒
  ticks/cell = `100/(speed+1)`.
- **Playback:** `CombatDirector.PlaybackSegmentRoutine` walks ticks firstTick→lastTick, firing
  `EventReplayed` per event; `CombatArenaPresenter.OnEventReplayed` → `_replayState.ApplyEvent` +
  `ApplyEventVisual` (a `"move"` event → `actor.MoveTo(anchorCell)`).
- **Free-chase (Top Troops feel):** `CombatArenaChaseController.Update` reads `_replayState.Anchors`
  each frame, computes a goal a couple cells ahead (`CombatPresentationEngagement.ComputeChaseAnchor`,
  clamped by `topTroopsChaseMaxLeadCells = 2`), calls `actor.SetChaseTargetWorld`. The actor
  (`CombatUnitActor.Update`) SmoothDamps toward the (smoothed) anchor biased toward that goal.
- **Walk speed** comes from `CombatArenaMoveSpeedResolver`: `worldSpeed = cellWidth/secondsPerCell ×
  presentationScale × chaseBoost`, where `secondsPerCell = ticksPerCell / TicksPerSecond`. This is
  why presentationScale must track EmptyTickPaceScale (the real per-cell playback time).

## How to drive a combat in Play Mode (Unity MCP `script-execute`)
`Script.Main()` must be `public static class Script { public static string Main() {...} }`.
Recipe used this session (IronMarch, buy a bulwark, place all reserves, begin):
```csharp
var buttons = GameObject.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
buttons.First(b=>b.gameObject.name=="NewRunButton").onClick.Invoke();
GameObject.FindObjectsByType<Button>(...).FirstOrDefault(x=>x.name=="IronmarchUnionButton" && x.activeInHierarchy)?.onClick.Invoke();
var rm = RunManager.Instance; var st = rm.State;
// reroll until a bulwark is offered, buy it + all affordable, place every reserve piece, then:
rm.BeginCombat();
// after load, click the "ContinueButton" to start playback.
```
Per-frame measurement pattern: inject a nested `MonoBehaviour` recorder via `script-execute`, read
back via reflection (`GetComponents<MonoBehaviour>().First(c=>c.GetType().Name=="Rec")` — note
`GetComponent<T>()` fails across script compilations). `Time.timeScale` can slow playback for capture.

## Tests
- `Core.Tests` + `Presentation.Tests` (EditMode), `Tests.PlayMode`. Currently **345/345 EditMode pass.**
- Run via Unity MCP `tests-run` (testMode EditMode) or Test Runner window.
- After editing `.cs` outside the editor, call `assets-refresh` to force recompile before testing.

## Housekeeping / gotchas
- Don't hand-edit `.meta` files (Unity owns GUIDs).
- `console-get-logs` output can exceed token limits — it dumps to a file; scan with python/jq, or
  just trust `tests-run` for compile validation.
- Unity Editor crashed once mid-session (unrelated to our code, which compiled clean) — just restart.
- Pre-existing/unrelated console errors exist; our changes added none.
- Working tree also has unrelated untracked items (NuGet plugin, `_Recovery/*.unity`, a `.blend`) —
  leave them; not part of this work.
