> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# DeadManZone — Combat Spectacle Pass Design

**Date:** 2026-06-14  
**Status:** Approved (2026-06-14)  
**Builds on:** `2026-06-10-deadmanzone-combat-arena-presentation-design.md`, `2026-06-13-synty-asset-revamp-design.md`  
**Scope:** Presentation only — no changes to `TickCombatRun`, event log, save schema, or tactic pause rules.

---

## Summary

Upgrade combat arena **presentation** so sandbox fights read clearly at a glance: **larger units**, **stand-and-shoot attacks** (not melee lunges), **PolygonParticleFX gunfire VFX**, and **humanoid shoot/death animations** from newly installed packs. Deliver in two phases: **Phase 1** polishes the existing sandbox roster (~25 Synty 3D prefabs); **Phase 2** assigns arena prefabs to remaining faction pieces (~15 billboard fallbacks).

**Reference feel:** Top Troops idle battles — units stay in formation, fire at range, impacts and deaths are obvious. Melee step-in is reserved for `AttackType.Melee` only (future, rare).

---

## Locked decisions

| Area | Choice |
|------|--------|
| Scope | Phase 1 sandbox roster, Phase 2 faction prefab assignment |
| Attack motion | **Stand-and-shoot** — face target, fire in place; no forward lunge for gun units |
| Melee | Separate profile using short step-in; only when `AttackType.Melee` |
| VFX source | **PolygonParticleFX** (primary) + PolygonWar FX (heavy/cannon fallback) |
| Infantry shoot anims | **Kevin Iglesias — Human Soldier Animations FREE** (humanoid rifle/grenade/bazooka clips) |
| Infantry death anims | Kevin Iglesias `HumanM@Death01–03` primary; Synty Sidekick sword-death clips fallback |
| Combat idles | **Synty Animation - Idles** when available on rig |
| Locomotion | Keep **AnimationBaseLocomotion** `AC_Polygon_Masculine` for walk |
| Core sim | Unchanged |

---

## Installed animation assets

| Pack | Path | Use in this pass |
|------|------|------------------|
| PolygonParticleFX | `Assets/Synty/PolygonParticleFX/` | Muzzle, tracer, impact, explosion, death dust |
| Animation - Sword Combat | `Assets/Synty/AnimationSwordCombat/` | **Death only** — `Animations/Sidekick/Death/A_MOD_SWD_Death_*_Neut.fbx` |
| Animation - Idles | `Assets/Synty/AnimationIdles/` *(import via SyntyPass)* | Combat idle variety when unit is stationary |
| Human Soldier Animations FREE | `Assets/Kevin Iglesias/Human Animations/` | **Shoot + death + grenade** — humanoid clips |

### Key Human Soldier clips (male; mirror female where prefab uses feminine rig)

| Clip | Path | Mapped `AttackType` / ability |
|------|------|-------------------------------|
| Rifle shoot | `Animations/Male/Combat/Rifle/HumanM@Rifle_Aim01_Shoot01.fbx` | Ballistic, Piercing, Shredding |
| Assault rifle shoot | `Animations/Male/Combat/AssaultRifle/HumanM@AssaultRifle_Aim01_Shoot01.fbx` | Fast-firing infantry (optional variant) |
| Grenade throw | `Animations/Male/Combat/Grenade/HumanM@ThrowGrenade01_L.fbx` | Explosive, `GrantedAbility.GrenadeLob` |
| Bazooka shoot | `Animations/Male/Combat/Bazooka/HumanM@Bazooka_Aim01_Shoot01.fbx` | Heavy explosive units (future) |
| Death 01–03 | `Animations/Male/Combat/HumanM@Death01–03.fbx` | All infantry deaths |

**Retargeting note:** Sidekick arena units use Sidekick skeletons. Kevin Iglesias clips are **Humanoid**. Implementation must verify avatar compatibility (Humanoid retarget) on `ArenaUnit_*` prefabs. If retarget quality is poor on a prefab, fall back to VFX-only shoot + Synty Sidekick death clip for that role.

**Do not use** Sword Combat **attack** clips — they read as melee and contradict gun-line fantasy.

---

## Section 1 — Architecture

### Principle

Sim stays pure C#; presentation gains a **combat spectacle layer** on top of existing arena actors.

```
CombatDirector.EventReplayed
        ↓
CombatArenaPresenter (existing)
        ↓
CombatUnitActor.PlayAttackToward / PlayDeath
        ↓
CombatArenaUnitVisual + ICombatUnitVisualDriver
        ↓
CombatArenaVfx (particles + damage text)
```

### New / modified presentation components

| Component | Role |
|-----------|------|
| `CombatArenaVfxSetSO` | Direct prefab refs for PolygonParticleFX + PolygonWar bursts (build-safe) |
| `CombatAttackPresentationProfile` | Maps `AttackType` + `PieceCategory` → anim clip id, VFX prefabs, timing |
| `HumanoidCombatVisualDriver` | Replaces/extends `SyntyLocomotionVisualDriver` — locomotion + one-shot shoot/death via `Animator` |
| `AC_CombatArena_Infantry.controller` | Project-owned controller under `Assets/_Project/Art/Synty/Animation/` — locomotion base + combat triggers |
| `CombatArenaPrefabResolver` | Adds attack profile + muzzle offset resolution |

### Unchanged

- `TickCombatRun`, `CombatEventLog`, segment pauses, save/resume
- Build-phase UI grid
- Vehicle/building meshes (static — no skeletal shoot anims in Phase 1)

---

## Section 2 — Visibility (scale + camera)

**Problem:** Auto-framed board at 50° / 38° FOV makes ~1.85m units occupy a tiny screen fraction.

**`CombatArenaConfigSO` defaults (Phase 1 tuning targets):**

| Setting | Current | Target |
|---------|---------|--------|
| `unitModelScaleMultiplier` | 1.25 | **1.6** |
| `defaultUnitModelHeight` | 1.85 | **2.1** |
| `fieldOfView` | 38 | **42** |
| `boardVerticalViewportCenter` | 0.44 | **0.48** |

**Per-category height** in `CombatArenaPrefabResolver`:

- Infantry / hybrids → 2.1m
- Vehicles → 1.8m
- Buildings → 0 (ground-aligned, unchanged)

Validate with existing **C-key camera tuner** before locking values.

---

## Section 3 — VFX system

**Problem:** `CombatArenaVfx` uses `SyntyRuntimeAssetLoader` path loads that fail in player builds unless assets live under `Resources/`.

**Fix:** `CombatArenaVfxSetSO` with serialized `ParticleSystem` prefab references.

### Primary prefabs (PolygonParticleFX)

| Role | Prefab |
|------|--------|
| Rifle muzzle | `Prefabs/FX_Gunshot_01.prefab` |
| Rifle muzzle smoke | `Prefabs/FX_Gunshot_BarrelSmoke_01.prefab` |
| Tracer / trail | `Prefabs/FX_Gunshot_Heavy_Single_Tracers_01.prefab` |
| Impact | `Prefabs/FX_Gunshot_Heavy_Repeating_Tracers_Impact_01.prefab` *(or PolygonWar `FX_Bullet_Impact_01`)* |
| Sniper line | `Prefabs/FX_Gunshot_Sniper_Line_01.prefab` |
| Death burst | `Prefabs/FX_Dust_Small_01.prefab` + `FX_Explosion_Small_01` (PolygonWar) |
| Grenade / explosive | PolygonWar `FX_Explosion_Small_01` at target |
| Cannon / vehicle | PolygonWar `FX_Cannon_Shot_01` + `FX_Gunshot_Heavy_Single_Tracers_01` |

### Routing

`CombatAttackPresentationProfile` selected by:

1. `PieceDefinitionSO.attackType` (primary)
2. `PieceDefinitionSO.category` (infantry vs vehicle vs building)
3. `PieceDefinitionSO.combatRole` for sniper/support variants (optional)

| AttackType | VFX sequence |
|------------|--------------|
| Ballistic, Piercing, Shredding | Muzzle → tracer → impact at target |
| Explosive, GrenadeLob | Throw timing → explosion at target cell |
| Fire | Muzzle + flame burst at target |
| Gas | Colored smoke puff at target cell |
| Melee *(future)* | Short step-in + impact spark at target (no gunshot) |

**Damage text:** Keep floating TMP; increase `damageTextScale` from 0.35 → **0.45**.

**Pooling:** Cap ~20 simultaneous particle instances; `CombatArenaFreezeController` tracks bursts for tactic pause.

---

## Section 4 — Attack presentation (stand-and-shoot)

**Explicit rule:** Gun-line units **never move toward the target** to attack. They rotate to face the target and fire from current cell.

### Infantry sequence (Ballistic default)

| Time | Action |
|------|--------|
| 0.00s | Face target; stop walk locomotion |
| 0.00s | Trigger shoot animation (`HumanM@Rifle_Aim01_Shoot01` or mapped clip) |
| 0.08s | Muzzle flash at weapon forward offset |
| 0.12s | Tracer from muzzle → target |
| 0.20s | Impact VFX + damage number at target |
| ~0.55s | Return to combat idle (Synty Idles or locomotion idle) |

### Grenade / explosive

| Time | Action |
|------|--------|
| 0.00s | Face target |
| 0.00s | `HumanM@ThrowGrenade01_L` |
| 0.35s | Arc VFX (optional simple parabolic particle) |
| 0.50s | Explosion at target |

### Vehicles / buildings (static mesh)

No body translation. Short chassis **recoil shake** (local position oscillation ~0.05m) + cannon/gunshot VFX at forward hardpoint offset.

### Melee (future, `AttackType.Melee` only)

Short forward step (~0.35m, 0.15s) + impact VFX. **Do not use** for current sandbox roster unless piece is explicitly tagged Melee.

### Legacy lunge config

`attackLungeSeconds` / `attackLungeDistance` on `CombatArenaConfigSO` remain for **billboard fallback** and **Melee profile only**. Remove from default 3D gun attack path.

---

## Section 5 — Death presentation

Replace scale-to-zero shrink with:

1. Stop locomotion and combat state
2. Play death animation — `HumanM@Death01` (random 01–03) or Sidekick `A_MOD_SWD_Death_F_Neut` fallback
3. Spawn death VFX (dust + small explosion) at unit feet
4. On anim complete (~0.5–0.8s): deactivate actor, return to pool

Billboard units keep fade/scale fallback if no animator.

---

## Section 6 — Animator architecture

### Layer strategy

| Layer | Controller / clips | Purpose |
|-------|-------------------|---------|
| Base | `AC_Polygon_Masculine` (existing) | Walk / idle locomotion |
| Combat overlay | Project `AC_CombatArena_Infantry` or `Animator.Play` one-shots | Shoot, throw, death |

**Triggers / parameters (project controller):**

- `Shoot` (trigger) — rifle fire one-shot
- `ThrowGrenade` (trigger) — explosive throw
- `Death` (trigger) — death state (no exit)
- Existing locomotion params unchanged (`IsWalking`, `MoveSpeed`, `CurrentGait`)

### `ICombatUnitVisualDriver` extension

```csharp
void PlayAttack(CombatAttackPresentationProfile profile);
void PlayDeath(Action onComplete);
float GetAttackDuration(CombatAttackPresentationProfile profile);
```

### Muzzle anchor

Resolve muzzle world position via:

1. Optional `muzzleTransform` on arena unit wrapper prefab (editor-assigned bone or empty)
2. Fallback: model forward × 0.4m + height 1.2m from actor root

---

## Section 7 — Phase 2: Faction prefab assignment

After Phase 1 is validated in sandbox fights:

1. Extend `SyntyArenaPrefabGenerator` + `SyntyArtCatalogFactory` for ~15 faction pieces (Ash Wraiths, Cartel of Echoes, Dust Scourge, etc.)
2. Reuse existing 5 role infantry prefabs + vehicle/building wrappers with scale/tint variance
3. Re-run `SandboxIconSnapshotter` for new assignments
4. Rename/expand `SandboxArtCoverageTests` → `CombatArenaArtCoverageTests` for full catalog coverage

No new mesh authoring — assignment and config only.

---

## Section 8 — Testing

| Test | Asserts |
|------|---------|
| EditMode `CombatArenaVfxSetTests` | All VfxSetSO prefab slots non-null |
| EditMode `CombatAttackProfileTests` | Every `AttackType` maps to a profile; Melee distinct from Ballistic |
| EditMode `CombatArenaArtCoverageTests` (Phase 2) | All catalog pieces have `combatArenaPrefab` |
| PlayMode `CombatArenaPlayModeTests` | Damage event spawns impact particle; destroyed event spawns death burst |
| Manual | Sandbox fight — units readable, rifle shoot visible, no forward charge on ballistic |

**Performance target:** 60 FPS desktop with ~20 units and particle cap enforced.

---

## Section 9 — Files touched (expected)

### Create

| Path | Purpose |
|------|---------|
| `Assets/_Project/Data/ScriptableObjects/CombatArenaVfxSetSO.cs` | VFX prefab bundle |
| `Assets/_Project/Data/Resources/DeadManZone/CombatArenaVfxSet.asset` | Default VFX refs |
| `Assets/_Project/Presentation/Combat/Arena/CombatAttackPresentationProfile.cs` | Attack routing struct/enum |
| `Assets/_Project/Presentation/Combat/Arena/HumanoidCombatVisualDriver.cs` | Shoot/death/locomotion driver |
| `Assets/_Project/Art/Synty/Animation/AC_CombatArena_Infantry.controller` | Project combat animator |
| `Assets/_Project/Core.Tests/EditMode/CombatArenaVfxSetTests.cs` | VFX ref tests |
| `Assets/_Project/Core.Tests/EditMode/CombatAttackProfileTests.cs` | Profile mapping tests |

### Modify

| Path | Change |
|------|--------|
| `CombatArenaVfx.cs` | Use VfxSetSO; add muzzle/tracer spawn APIs |
| `CombatArenaUnitVisual.cs` | Stand-and-shoot timing; wire HumanoidCombatVisualDriver |
| `CombatUnitActor.cs` | Death via driver; lunge only for billboard/Melee |
| `CombatArenaPresenter.cs` | Pass attack profile from piece definition |
| `CombatArenaPrefabResolver.cs` | Scale/height defaults; profile resolution |
| `CombatArenaConfigSO` + `.asset` | New scale/camera defaults |
| `ICombatUnitVisualDriver.cs` | Extended interface |
| `SyntyLocomotionVisualDriver.cs` | Locomotion-only or superseded by HumanoidCombatVisualDriver |
| `SyntyArenaPrefabGenerator.cs` (Phase 2) | Faction piece assignment |

### Untouched

- All of `DeadManZone.Core/Combat/`
- Build-phase presentation
- Save/resume JSON schema

---

## Out of scope

- Per-ability unique VFX beyond `AttackType` routing
- Per-unit HP bars (army bars only today)
- Full Kevin Iglesias demo controller port (`HumanM@SoldierAnimations.controller` is reference only — build slim project controller)
- Sidekick feminine clip variants unless prefab uses feminine rig (assign per wrapper prefab)

---

## Success criteria

| # | Criterion |
|---|-----------|
| 1 | Sandbox fight: units visibly larger; identifiable at default camera |
| 2 | Ballistic attacks show muzzle flash + tracer + impact without unit charging forward |
| 3 | Grenade/explosive pieces use throw animation + explosion at target |
| 4 | Deaths play animation + particle burst (not scale-to-zero) |
| 5 | VFX works in editor and player build (direct SO refs, not editor-only path load) |
| 6 | Phase 2: zero billboard fallbacks for catalog faction pieces |
| 7 | Existing combat PlayMode tests pass; new EditMode tests pass |
