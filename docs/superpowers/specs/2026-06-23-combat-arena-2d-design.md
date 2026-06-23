# DeadManZone — Combat Arena 2D (Top Troops Hybrid) Design

**Date:** 2026-06-23  
**Branch:** `2dcombatreworkv2`  
**Engine:** Unity 6 / URP  
**Status:** Approved — implemented on `2dcombatreworkv2`  
**Builds on:** `2026-06-10-deadmanzone-combat-arena-presentation-design.md`, `2026-06-16-combat-rework-v2-design.md`, existing Top Troops v3 prototype (`TopTroopsBattlefieldBuilder`, `CombatArenaConfigSO`)  
**Supersedes (presentation mode only):** Nothing removed — adds a parallel 2D backend alongside legacy 3D

---

## Summary

Convert combat **active-fight presentation** to a Top Troops–style **2.5D hybrid**: orthographic fixed camera, square grid battlefield, sprite-based units with Y-sort depth, arced projectiles, and cartoon-readable VFX — while keeping combat sim, event replay, shop/build phases, and formation setup **unchanged**.

The legacy 3D `CombatArena` scene remains intact for A/B comparison. A new `CombatArena2D` additive scene (or config-selected backend) hosts the 2D presentation path.

---

## Goals

- Match Top Troops combat **look and feel**: square grid, god's-eye angled view, colorful readable units, natural projectile arcs, full battlefield visible.
- **Presentation-only** change — `TickCombatRun`, `CombatDirector`, tactic pauses, damage, abilities, AI unchanged.
- Clean **abstraction layer** so 3D and 2D backends swap via config without touching `CombatArenaPresenter` event handling.
- Performant with **20–40+ units** and concurrent projectiles on mid-tier mobile-class hardware.

## Non-Goals

- Shop phase, reserves, drag-drop, build grid, or pre-battle formation UI.
- New combat rules, sim pacing, or save schema changes.
- Replacing 3D arena assets or deleting Synty prefab pipeline.
- Full artist-quality sprite pass (placeholders ship first; pipeline supports final art drop-in).

---

## Locked decisions

| Area | Choice |
|------|--------|
| Architecture | **Hybrid 2.5D** — keep `CombatGridMapper` world coords; swap rendering/camera/VFX backends |
| Parallel implementation | New **`CombatArena2D.unity`** scene; legacy `CombatArena.unity` untouched |
| Mode selection | `CombatArenaVisualMode` enum on `CombatArenaConfigSO` (`Legacy3D` \| `TopTroops2D`) |
| Camera | **Orthographic**, fixed pose, auto-framed to board width/height (port of `CombatArenaCameraFramer`) |
| Battlefield | Square cell grid via **SpriteRenderer quads** (not diamond iso); zone tint + checkerboard |
| Unit sprites (v1) | **Mix A + B** — see Sprite resolution below |
| Unit movement | Reuse `CombatUnitActor` lerp + `CombatArenaFreeChaseMovement` (world-space unchanged) |
| Depth sorting | **Y-sort** on world Z: lower Z (screen-down) draws in front |
| Projectiles | **Parabolic arc** (LineRenderer or pooled sprite tween); not straight stretched particles |
| Buildings | 2D silhouettes / icons on same sorting pipeline as units |
| Tests | TDD — EditMode for sort order, arc math, ortho framing; PlayMode smoke for scene load |

---

## Sprite resolution (A + B mix)

Priority order when building a unit visual:

1. **`piece.combatArenaSprite`** (optional new field) — dedicated combat sprite when artist provides it.
2. **`piece.icon`** (A) — use when icon exists and reads at combat scale (min ~64×64 effective; not tiny UI chip).
3. **Role silhouette sprite** (B) — procedurally assigned from a small shared atlas keyed by `combatRole` + `CombatSide` + `categoryTint`:
   - Assault → shielded infantry blob
   - Sniper / Support → ranged blob + weapon hint
   - Artillery → cannon silhouette
   - Vehicle / heavy → wider chassis blob
   - Default → generic squad rectangle
4. **Faction tint** applied as `SpriteRenderer.color` multiplier on silhouettes.

Silhouette atlas lives at `Assets/_Project/Art/Combat2D/Placeholders/` (generated 64×64 PNGs or runtime `Texture2D` stubs until art pass).

Squad pieces (manpower > 1): duplicate icon/silhouette at small offsets (reuse formation offsets from `TopTroopsSquadVisualFactory`).

---

## Section 1 — Architecture

### Core principle (unchanged)

```
TickCombatRun → CombatEventLog → CombatDirector.EventReplayed
                                      ↓
                            CombatArenaPresenter (orchestrator — unchanged contract)
                                      ↓
                         ICombatArenaPresentationBackend (NEW factory)
                              /                    \
                    Legacy3DBackend          TopTroops2DBackend
```

### What stays untouched

| Layer | Components |
|-------|------------|
| Sim | `TickCombatRun`, `CombatResolver`, `PieceAbilityEngine`, all Core combat |
| Run flow | `RunOrchestrator`, shop, build board, `CombatFlowPresenter` transition timing |
| Replay | `CombatDirector`, `CombatReplayState`, event types |
| Movement logic | `CombatUnitActor` Update, chase controller, anchor sync |
| Freeze | `CombatArenaFreezeController` on tactic pause |

### New abstraction interfaces

```csharp
enum CombatArenaVisualMode { Legacy3D, TopTroops2D }

interface ICombatArenaPresentationBackend {
    void Initialize(Transform arenaRoot, CombatArenaConfigSO config);
    void FrameBattlefield(BattlefieldLayout layout);
    Camera ArenaCamera { get; }
    Transform UnitsRoot { get; }
    Transform BuildingsRoot { get; }
    ICombatUnitVisualFactory UnitVisualFactory { get; }
    ICombatArenaVfxPresenter Vfx { get; }
}

interface ICombatUnitVisualFactory {
    ICombatUnitVisual Create(Transform actorRoot, CombatUnitVisualContext ctx);
}

interface ICombatUnitVisual {
    void SetWalking(bool walking);
    void FaceDirection(Vector3 worldDir);
    void PlayAttack(CombatAttackPresentationProfile profile,
        Vector3 targetWorld, Action<Vector3> onMuzzle, Action onImpact);
    void PlayDeath(Action onComplete);
    void UpdateSortDepth(Vector3 worldPosition);
    void Dispose();
}

interface ICombatArenaBattlefieldView {
    void Build(Transform root, BattlefieldLayout layout, CombatGridMapper mapper, CombatArenaConfigSO config);
    void Clear();
}

interface ICombatArenaVfxPresenter {
    void PlayRifleShot(Vector3 from, Vector3 to);
    void PlayCannonShot(Vector3 from, Vector3 to);
    void PlayImpact(Vector3 at, int damage);
    void PlayExplosion(Vector3 at, int damage);
    void PlayDeath(Vector3 at);
    void PlayMiss(Vector3 at);
}
```

### Refactor targets (minimal surface)

| File | Change |
|------|--------|
| `CombatUnitActor` | Extract visual branches into `ICombatUnitVisual`; keep movement/attack timing |
| `CombatArenaBootstrap` | Delegate to `ICombatArenaPresentationBackend` based on config mode |
| `CombatArenaPresenter` | Inject VFX via `ICombatArenaVfxPresenter` instead of concrete `CombatArenaVfx` |
| `CombatArenaSceneLoader` | Load `CombatArena` or `CombatArena2D` based on config |
| `GameScenes` | Add `CombatArena2D` constant |
| `PieceDefinitionSO` | Add optional `combatArenaSprite` field |
| `CombatArenaConfigSO` | Add `visualMode`, 2D-specific tuning (ortho size scale, arc height, sort layer names) |

Existing 3D types (`CombatArenaUnitVisual`, `TopTroopsSquadVisualFactory`, `CombatBillboard`) move behind `Legacy3DUnitVisualFactory` — no deletion.

---

## Section 2 — 2D scene & camera

### Scene: `CombatArena2D.unity`

Duplicate hierarchy from `CombatArena.unity`:

- `CombatArenaBootstrap` (same component; picks 2D backend when mode = TopTroops2D)
- `CombatArenaPresenter`
- `CombatArenaVfx` slot → `CombatArena2DVfx` component implementing `ICombatArenaVfxPresenter`
- Orthographic `Camera` tagged for arena UI routing
- Sorting layers (project settings): `ArenaGround`, `ArenaShadow`, `ArenaUnits`, `ArenaVfx`, `ArenaUI`

### Camera

- **Projection:** Orthographic
- **Rotation:** Fixed ~35–40° pitch on X (slight oblique; matches Top Troops "tilted board" without perspective foreshortening)
- **Position:** Solved by `CombatArenaOrthographicFramer` — board width fills ~95% viewport width; vertical center at `boardVerticalViewportCenter` (reuse config field)
- **No player control** during fight
- Background: solid sky color from `topTroopsSkyColor` or gradient sprite quad behind grid

---

## Section 3 — Battlefield grid

### `CombatArena2DBattlefieldView`

- One `SpriteRenderer` child per layout cell (square, not rotated 45°).
- Colors from existing `TopTroopsBattlefieldPalette` + checker shade (`TopTroopsBattlefieldBuilder.ResolveCellColor`).
- Cell size = `config.cellWidth` × `config.cellDepth` world units.
- **Inset gap** between cells (`gridCellInset`) exposes darker backdrop color as grid lines.
- Neutral column divider: 1px-taller tint strip or alternate cell color band.
- No 3D cube meshes in 2D mode.

### Environment

- Skip Synty ground, perimeter props, cliff cubes, fog (optional very light color gradient only).
- Keeps draw calls low: one shared material, vertex colors or palette swap.

---

## Section 4 — Units & depth

### `CombatUnitVisual2D`

Structure per actor:

```
CombatUnitActor (Transform — world movement, unchanged)
  └── PresentationRoot
        ├── Shadow (SpriteRenderer, ArenaShadow layer, fixed scale)
        └── SquadRoot
              ├── Soldier_0 (SpriteRenderer, ArenaUnits)
              └── ...
```

- **Shadow:** soft ellipse sprite, alpha ~0.35, offset slightly south in world space.
- **Sort order:** `baseOrder = -(int)(worldPosition.z * 100)` so south-on-screen draws in front. Per-soldier +1 offset for squad depth.
- **Idle bob:** reuse existing sine bob on `PresentationRoot.localPosition.y`.
- **Walk:** toggle subtle bob amplitude; no root translation beyond actor transform.
- **Face direction:** flip `SpriteRenderer.flipX` or rotate squad root on Y for 2.5D (no billboarding to camera).
- **Attack:** short forward lunge on Z + muzzle/impact callbacks at profile delays (same as procedural path today).
- **Death:** scale-down + alpha fade over 0.4s; return actor to pool.

### Buildings (`CombatArena2DBuildingVisual`)

- Static sprite at mapped grid footprint center.
- Uses `piece.icon` or building silhouette from placeholder atlas.
- Sorted with units using footprint min-Z rule.

---

## Section 5 — VFX & projectiles

### `CombatArena2DVfx`

| Effect | Implementation |
|--------|----------------|
| Rifle shot | Pooled tracer: sprite or thin quad tweened along **parabola** from muzzle to target; peak height scales with distance × `projectileArcHeight` |
| Cannon | Same arc, larger sprite, longer duration |
| Impact | 3–4 frame flash sprite at target + existing floating damage TMP |
| Explosion | Radial scale-up sprite + screen-space shake optional (config off by default) |
| Death | Small puff sprite at unit feet |
| Miss | Short arc ending short of target + "MISS" text |

Arc formula (testable):

```
t ∈ [0,1]: pos = lerp(from, to, t) + up * (4 * arcHeight * t * (1-t))
```

Pool sizes: 24 tracers, 12 impacts, 8 explosions (configurable).

Legacy `CombatArenaVfx` particle path remains for 3D mode.

---

## Section 6 — Integration & scene routing

```
CombatArenaConfigSO.visualMode
        ↓
CombatArenaSceneLoader.LoadAsync()
  → TopTroops2D: LoadSceneAsync("CombatArena2D")
  → Legacy3D:    LoadSceneAsync("CombatArena")
        ↓
CombatArenaBootstrap.Awake()
  → backend = PresentationBackendFactory.Create(visualMode)
        ↓
CombatArenaPresenter.InitializeArena() — unchanged event/spawn flow
```

Editor menu: **DeadManZone → Combat Arena → Create CombatArena2D Scene** (bootstrap script duplicates scene setup with ortho camera defaults).

---

## Section 7 — Testing (TDD)

### EditMode (`Assets/_Project/Presentation.Tests/EditMode/`)

| Test class | Asserts |
|------------|---------|
| `CombatArena2DSortOrderTests` | South Z → higher sort order |
| `CombatArena2DProjectileArcTests` | Parabola midpoint height, endpoints |
| `CombatArenaOrthographicFramerTests` | Ortho size from layout + cell dimensions |
| `CombatUnitSpriteResolverTests` | Priority: combatArenaSprite → icon → role silhouette |
| `CombatArenaPresentationBackendFactoryTests` | Mode enum returns correct backend type |

### PlayMode (`Assets/_Project/Tests.PlayMode/`)

| Test | Asserts |
|------|---------|
| `CombatArena2DLoadPlayModeTests` | Additive load, bootstrap instance, ortho camera |
| `CombatArena2DReplaySmokeTests` | Mock board + replay move/damage events → actors exist, sorted |

Run filtered EditMode during iteration; full suite before merge.

---

## Section 8 — Performance

| Concern | Mitigation |
|---------|------------|
| Draw calls | Shared material/atlas for grid + silhouettes; static batching on grid |
| Sorting | Integer sort order from Z — no per-frame `Sort()` on lists |
| Allocations | Pool actors, tracers, damage text; no `new Material()` per unit |
| Overdraw | Limit particles; sprite impacts instead of particle systems in 2D mode |
| Target | 60 FPS, ≤150 draw calls, ≤256 MB arena scene with 40 units + 20 tracers |

---

## Section 9 — Artist deliverables (later)

| Asset | Spec | Placeholder (now) |
|-------|------|-------------------|
| Unit combat sprites | 128×128 PNG, one per piece or role family, transparent | `icon` + role silhouettes |
| Grid tiles | 64×64 light/dark dirt variants | Procedural color quads |
| Shadow blob | 32×32 soft circle | Generated texture |
| Projectile | 8×16 bullet, 16×16 shell | White quad tinted |
| Impact flash | 4-frame 64×64 sheet | Single white sprite fade |
| Building sprites | 128×128 footprint-centered | `icon` scaled |
| Sky/backdrop | Optional gradient 512×256 | Solid color from config |

Optional SO field: `PieceDefinitionSO.combatArenaSprite` — assign when art ready; resolver picks it first.

---

## Section 10 — Setup instructions (post-implementation)

1. Open **CombatArenaConfig** asset (`Resources/DeadManZone/CombatArenaConfig`).
2. Set **Visual Mode** → `TopTroops2D`.
3. Ensure **CombatArena2D** is in **Build Settings** (Editor script adds it).
4. Enter Play mode → start a fight from Run scene → additive 2D arena loads.
5. To compare 3D: set Visual Mode → `Legacy3D` (loads original scene).

No changes required on Run scene prefabs if loader reads config automatically.

---

## Risks & mitigations

| Risk | Mitigation |
|------|------------|
| Icons too small/low-detail at combat scale | Role silhouette fallback (B); document min icon size |
| Refactor breaks 3D path | Parallel backend; Legacy3D factory wraps existing code unchanged |
| Sort order pops when units cross | Stable sort key + small squad offsets |
| Ortho framing differs from perspective tuning | Separate framer tests; manual pose override fields on config |
| Scope creep into sim | Presenter-only edits; Core.Tests must stay green without changes |

---

## Success criteria

1. Fight loads **CombatArena2D** when config mode = TopTroops2D; 3D scene still loads for Legacy3D.
2. Full battlefield visible in fixed ortho camera; square grid readable.
3. Units show icon or role silhouette; Y-sort correct when overlapping.
4. Rifle/cannon shots follow visible arc; damage numbers appear on impact.
5. Tactic pause freezes all 2D motion (sprites, tweens, tracers).
6. EditMode + PlayMode tests above pass; Core combat tests unchanged.
7. 40-unit stress scene holds ≥55 FPS in Editor Play mode on dev machine (Profiler spot-check).

---

## Implementation phases (for writing-plans)

| Phase | Deliverable |
|-------|-------------|
| **1 — Abstraction** | Interfaces, factory, refactor `CombatUnitActor` visual extraction, config enum |
| **2 — Battlefield + camera** | `CombatArena2DBattlefieldView`, ortho framer, `CombatArena2D` scene shell |
| **3 — Unit visuals** | `CombatUnitVisual2D`, sprite resolver A+B, shadows, sorting |
| **4 — VFX** | Arc tracers, impacts, wire into presenter |
| **5 — Buildings + polish** | 2D building sprites, chase movement validation, PlayMode smoke |
| **6 — QA gate** | Full test run, profiler pass, setup doc verification |

---

**Next step:** User reviews this spec → approve → `writing-plans` → TDD implementation on `2dcombatreworkv2`.
