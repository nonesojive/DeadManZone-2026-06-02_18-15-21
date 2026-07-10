# Combat unit scale bug — session handoff (combatvisualv5)

**Purpose:** Show what went wrong in prior Claude reasoning, what the real bug was, and what fixed it.  
**Branch:** `combatvisualv5`  
**Date:** 2026-07-10  
**Repo:** DeadManZone (Unity 6, 2D combat billboards)

---

## TL;DR

Units were **actually changing mesh scale** between animation states. It was **not** “just the pose.”

Claude argued pose/silhouette for over an hour. Screenshots of the **same unit, ~1s apart, near-identical upright poses** still showed a clear size jump. That was correct player observation.

---

## What Claude got wrong

| Claim | Reality |
|-------|---------|
| “The size difference is just the pose / silhouette / arms closer to body.” | False. `SetFrame` resizes the billboard quad from `sprite.rect` every state swap. |
| Same unit looking bigger in a similar standing pose is an optical illusion. | False. Health bars stay fixed size (sized at spawn); the sprite grows under them. Shadows also grow. |
| Fixing “feel” or art framing is enough. | No — presentation code was resizing the mesh. |

**Evidence the user had (and Claude dismissed):**

- Screenshots back-to-back in one fight: bottom-right unit idle/standing vs same unit moments later — same piece, similar upright pose, clearly larger.
- Recording: size pops on state changes (idle ↔ walk/shoot/die), not continuous pose morph within one strip.

---

## Root cause (actual)

### Pipeline

1. Each anim state is its **own sheet** (idle / walk / shoot / die / …).
2. `CombatUnit2DStripPlayer.DetectSharedContentCrop` tight-crops opaque pixels **per strip**.
3. `CombatArena2DSpriteQuad.SetFrame` sizes the quad from that crop:

```csharp
float width  = frame.rect.width  / frame.pixelsPerUnit * uniformScale;
float height = frame.rect.height / frame.pixelsPerUnit * uniformScale;
quad.localScale = new Vector3(±width, height, 1f);
```

4. `uniformScale` was computed **once at Build** from the **idle** sprite (`CombatUnit2DVisualScale.ResolveUniformScale`), then reused for every later frame.

### Why that pops size

Different strips have different content bounds. Measured on `shock_trooper` (512² cells, 64 PPU):

| State | Content size (approx) | vs idle width |
|-------|----------------------|---------------|
| idle  | 172×369 | — |
| walk  | 238×421 | **+38%** |
| shoot | 254×358 | **+48%** |
| die   | 351×430 | **+104%** |

Same `uniformScale` + larger `frame.rect` ⇒ larger world quad. Pose can add a little; the bulk jump is **mesh scale**.

### Key files

- `Assets/_Project/Presentation/Combat/Arena/CombatUnit2DStripPlayer.cs` — per-strip content crop
- `Assets/_Project/Presentation/Combat/Arena/CombatArena2DSpriteQuad.cs` — `SetFrame` sizes from rect
- `Assets/_Project/Presentation/Combat/Arena/CombatUnitVisual2D.cs` — build-time scale + frame swaps
- `Assets/_Project/Presentation/Combat/Arena/CombatUnit2DVisualScale.cs` — target height / scale helpers

---

## Fix attempt #1 (wrong) — made deaths gigantic

**Idea:** Re-call `ResolveUniformScale(piece, frame)` every frame so **visible (alpha) height** stays ~1.85.

**Why it exploded on death:**

- Die strips share **one large crop** (union of all die poses).
- Late frames: body crumpled → **small opaque height** inside that large rect.
- `ResolveUniformScale` uses `VisibleHeightUnits` (alpha) → scale multiplier **skyrockets**.
- `SetFrame` still sizes the mesh from the **full rect** → entire empty crop drawn at inflated scale → **gigantic corpse**.

So: alpha-based normalize + rect-based mesh sizing = catastrophic on sparse frames.

User recording after attempt #1 confirmed deaths became way worse.

---

## Fix attempt #2 (correct) — lock quad rect height

**Invariant:** `frame.rect.height / ppu * scale` stays equal to the idle build-time quad height.

Helpers on `CombatUnit2DVisualScale`:

- `RectWorldHeight(sprite, scale)`
- `ResolveScaleForRectHeight(sprite, targetRectWorldHeight)`

In `CombatUnitVisual2D`:

- At Build: `_targetQuadHeight = RectWorldHeight(idleSprite, leaderScale)`
- In `TickAnimation`: scale each frame with `ResolveScaleForRectHeight(frame, _targetQuadHeight)`

Effects:

- Idle ↔ walk/shoot/die no longer pop overall size.
- Sparse die frames no longer explode (scale follows **rect**, constant per strip crop).
- Pose still changes silhouette; body doesn’t magically grow.

### Regression tests

`Assets/_Project/Presentation.Tests/EditMode/CombatArena2DHelpersTests.cs`:

- `VisualScale_ReusingIdleScaleOnLargerCrop_Pops_ReResolveKeepsHeight` — documents crop pop + rect lock
- `VisualScale_AlphaBasedScaleOnSparseDieFrame_ExplodesRect_RectLockDoesNot` — documents die blow-up + why rect lock is required

---

## Rules for future Claude sessions

1. **Believe the screenshot** when the same unit, similar pose, different size is shown — check `SetFrame` / transform scale before arguing “pose.”
2. **Never** normalize animated billboards by alpha-visible height if the mesh is sized from `sprite.rect` (especially shared strip crops).
3. Match the scale math to what `SetFrame` actually multiplies: **rect world size**, not alpha bounds.
4. Build-time idle scale alone is unsafe across per-strip content crops.

---

## Verify in Play Mode

1. Start a fight with animated infantry.
2. Watch one unit idle → shoot → idle: overall size should hold.
3. Kill a unit: death should not balloon.
4. Optional: compare unit vs fixed health-bar width across states (bar is sized at spawn).
