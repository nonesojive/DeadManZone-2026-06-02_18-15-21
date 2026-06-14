# Combat Spectacle Pass Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make sandbox combat arena fights readable and gunfire-focused — larger units, stand-and-shoot animations, PolygonParticleFX muzzle/tracer/impact/death VFX, and humanoid death clips — without changing core combat sim.

**Architecture:** Extend the existing `Presentation/Combat/Arena/` layer with `CombatArenaVfxSetSO` (build-safe particle refs), `CombatAttackPresentationProfile` (maps `AttackType` → anim + VFX timing), and `HumanoidCombatVisualDriver` (locomotion + one-shot shoot/death clips). `CombatArenaPresenter` passes piece attack profiles into actors; VFX fires on a timed sequence (muzzle → tracer → impact). Phase 2 extends `SyntyArenaPrefabGenerator` for faction pieces.

**Tech Stack:** Unity 6, C#, Animator humanoid retargeting, PolygonParticleFX, Kevin Iglesias Human Soldier Animations, Synty AnimationSwordCombat (death fallback), NUnit EditMode + PlayMode tests.

**Spec reference:** `docs/superpowers/specs/2026-06-14-combat-spectacle-pass-design.md`

---

## File map

| Path | Responsibility |
|------|----------------|
| `Assets/_Project/Data/ScriptableObjects/CombatArenaVfxSetSO.cs` | Serialized particle prefab bundle |
| `Assets/_Project/Data/Resources/DeadManZone/CombatArenaVfxSet.asset` | Default VFX prefab refs |
| `Assets/_Project/Data/ScriptableObjects/CombatArenaAnimationSetSO.cs` | Shoot/death AnimationClip refs |
| `Assets/_Project/Data/Resources/DeadManZone/CombatArenaAnimationSet.asset` | Default anim clip refs |
| `Assets/_Project/Presentation/Combat/Arena/CombatAttackPresentationKind.cs` | Enum: Rifle, Grenade, Cannon, Melee |
| `Assets/_Project/Presentation/Combat/Arena/CombatAttackPresentationProfile.cs` | Profile struct + timing constants |
| `Assets/_Project/Presentation/Combat/Arena/CombatAttackProfileResolver.cs` | Maps `PieceDefinitionSO` → profile |
| `Assets/_Project/Presentation/Combat/Arena/CombatArenaMuzzleAnchor.cs` | Optional muzzle transform marker on unit prefabs |
| `Assets/_Project/Presentation/Combat/Arena/HumanoidCombatVisualDriver.cs` | Locomotion + shoot/death one-shots |
| `Assets/_Project/Presentation/Combat/Arena/VehicleCombatVisualDriver.cs` | Static mesh recoil shake |
| `Assets/_Project/Presentation/Combat/Arena/CombatArenaVfx.cs` | Muzzle, tracer, impact, death APIs |
| `Assets/_Project/Presentation/Combat/Arena/CombatArenaUnitVisual.cs` | Stand-and-shoot coroutine |
| `Assets/_Project/Presentation/Combat/Arena/CombatUnitActor.cs` | Profile-aware attack/death |
| `Assets/_Project/Presentation/Combat/Arena/CombatArenaPresenter.cs` | Pass profile + piece to actors |
| `Assets/_Project/Presentation/Combat/Arena/CombatArenaPrefabResolver.cs` | Category height defaults |
| `Assets/_Project/Presentation/Combat/Arena/ICombatUnitVisualDriver.cs` | Extended interface |
| `Assets/_Project/Presentation.Tests/EditMode/CombatAttackProfileResolverTests.cs` | Profile mapping tests |
| `Assets/_Project/Presentation.Tests/EditMode/CombatArenaVfxSetTests.cs` | VFX asset non-null tests |
| `Assets/_Project/Tests.PlayMode/CombatArenaSpectaclePlayModeTests.cs` | VFX spawn smoke tests |

---

## Phase 1 — VFX infrastructure

### Task 1: CombatArenaVfxSetSO

**Files:**
- Create: `Assets/_Project/Data/ScriptableObjects/CombatArenaVfxSetSO.cs`
- Create: `Assets/_Project/Data/Resources/DeadManZone/CombatArenaVfxSet.asset` (Unity Create menu)
- Create: `Assets/_Project/Presentation.Tests/EditMode/CombatArenaVfxSetTests.cs`

- [ ] **Step 1: Write failing test**

Create `Assets/_Project/Presentation.Tests/EditMode/CombatArenaVfxSetTests.cs`:

```csharp
using DeadManZone.Data;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatArenaVfxSetTests
    {
        [Test]
        public void DefaultVfxSet_AllRequiredPrefabsAssigned()
        {
            var vfxSet = Resources.Load<CombatArenaVfxSetSO>("DeadManZone/CombatArenaVfxSet");
            Assert.NotNull(vfxSet, "CombatArenaVfxSet.asset missing from Resources/DeadManZone/");

            Assert.NotNull(vfxSet.rifleMuzzle, "rifleMuzzle");
            Assert.NotNull(vfxSet.rifleImpact, "rifleImpact");
            Assert.NotNull(vfxSet.bulletTracer, "bulletTracer");
            Assert.NotNull(vfxSet.deathBurst, "deathBurst");
            Assert.NotNull(vfxSet.explosionSmall, "explosionSmall");
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Unity → Window → General → Test Runner → EditMode → run `CombatArenaVfxSetTests`.
Expected: FAIL — type `CombatArenaVfxSetSO` not found.

- [ ] **Step 3: Create ScriptableObject**

Create `Assets/_Project/Data/ScriptableObjects/CombatArenaVfxSetSO.cs`:

```csharp
using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Combat Arena VFX Set")]
    public sealed class CombatArenaVfxSetSO : ScriptableObject
    {
        [Header("Rifle / ballistic")]
        public ParticleSystem rifleMuzzle;
        public ParticleSystem rifleMuzzleSmoke;
        public ParticleSystem bulletTracer;
        public ParticleSystem rifleImpact;

        [Header("Heavy / explosive")]
        public ParticleSystem cannonShot;
        public ParticleSystem explosionSmall;
        public ParticleSystem explosionLarge;

        [Header("Death")]
        public ParticleSystem deathBurst;
        public ParticleSystem deathSmoke;
    }
}
```

- [ ] **Step 4: Create asset and assign prefabs in Unity**

Unity menu → Create → DeadManZone → Combat Arena VFX Set → save as  
`Assets/_Project/Data/Resources/DeadManZone/CombatArenaVfxSet.asset`

Assign prefabs (drag from Project window):

| Field | Prefab path |
|-------|-------------|
| `rifleMuzzle` | `Assets/Synty/PolygonParticleFX/Prefabs/FX_Gunshot_01.prefab` |
| `rifleMuzzleSmoke` | `Assets/Synty/PolygonParticleFX/Prefabs/FX_Gunshot_BarrelSmoke_01.prefab` |
| `bulletTracer` | `Assets/Synty/PolygonParticleFX/Prefabs/FX_Gunshot_Heavy_Single_Tracers_01.prefab` |
| `rifleImpact` | `Assets/Synty/PolygonWar/Prefabs/FX/FX_Bullet_Impact_01.prefab` |
| `cannonShot` | `Assets/Synty/PolygonWar/Prefabs/FX/FX_Cannon_Shot_01.prefab` |
| `explosionSmall` | `Assets/Synty/PolygonWar/Prefabs/FX/FX_Explosion_Small_01.prefab` |
| `explosionLarge` | `Assets/Synty/PolygonWar/Prefabs/FX/FX_Explosion_Large_01.prefab` |
| `deathBurst` | `Assets/Synty/PolygonParticleFX/Prefabs/FX_Dust_Small_01.prefab` |
| `deathSmoke` | `Assets/Synty/PolygonWar/Prefabs/FX/FX_Smoke_Small_Dark_01.prefab` |

- [ ] **Step 5: Run test — expect PASS**

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Data/ScriptableObjects/CombatArenaVfxSetSO.cs Assets/_Project/Data/Resources/DeadManZone/CombatArenaVfxSet.asset Assets/_Project/Presentation.Tests/EditMode/CombatArenaVfxSetTests.cs
git commit -m "feat: add combat arena VFX set ScriptableObject"
```

---

### Task 2: Refactor CombatArenaVfx to use VfxSetSO

**Files:**
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatArenaVfx.cs`

- [ ] **Step 1: Replace runtime path loading with VfxSetSO reference**

Modify `CombatArenaVfx.cs`:

```csharp
[SerializeField] private CombatArenaVfxSetSO vfxSet;

private void Awake()
{
    if (freezeController == null)
        freezeController = GetComponent<CombatArenaFreezeController>();

    if (vfxSet == null)
        vfxSet = Resources.Load<CombatArenaVfxSetSO>("DeadManZone/CombatArenaVfxSet");
}

public void PlayRifleHit(Vector3 muzzleWorld, Vector3 targetWorld, int damageAmount)
{
    SpawnBurst(vfxSet?.rifleMuzzle, muzzleWorld);
    SpawnBurst(vfxSet?.rifleMuzzleSmoke, muzzleWorld);
    SpawnTracer(muzzleWorld, targetWorld);
    SpawnBurst(vfxSet?.rifleImpact, targetWorld);
    SpawnFloatingText(targetWorld + Vector3.up * 1.1f, damageAmount > 0 ? $"-{damageAmount}" : damageAmount.ToString());
}

public void PlayExplosiveHit(Vector3 attackerWorld, Vector3 targetWorld, int damageAmount)
{
    SpawnBurst(vfxSet?.explosionSmall, targetWorld);
    SpawnFloatingText(targetWorld + Vector3.up * 1.1f, damageAmount > 0 ? $"-{damageAmount}" : damageAmount.ToString());
}

public void PlayCannonHit(Vector3 muzzleWorld, Vector3 targetWorld, int damageAmount)
{
    SpawnBurst(vfxSet?.cannonShot, muzzleWorld);
    SpawnTracer(muzzleWorld, targetWorld);
    SpawnBurst(vfxSet?.explosionSmall, targetWorld);
    SpawnFloatingText(targetWorld + Vector3.up * 1.1f, damageAmount > 0 ? $"-{damageAmount}" : damageAmount.ToString());
}

public void PlayDeath(Vector3 worldPosition)
{
    SpawnBurst(vfxSet?.deathBurst, worldPosition);
    SpawnBurst(vfxSet?.deathSmoke, worldPosition);
}

private void SpawnTracer(Vector3 from, Vector3 to)
{
    if (vfxSet?.bulletTracer == null)
        return;

    Vector3 direction = to - from;
    if (direction.sqrMagnitude < 0.001f)
        return;

    var rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    var particle = Instantiate(vfxSet.bulletTracer, from, rotation, transform);
    particle.transform.localScale = Vector3.one * direction.magnitude;
    particle.Play();
    freezeController?.TrackParticle(particle);
    float lifetime = particle.main.duration + particle.main.startLifetime.constantMax + 0.1f;
    Destroy(particle.gameObject, Mathf.Max(lifetime, 0.5f));
}
```

Keep existing `SpawnBurst` and `SpawnFloatingText`; remove `DefaultImpactPrefabPath` / `EnsureDefaultPrefabs` / `SyntyRuntimeAssetLoader` calls for impact/death.

- [ ] **Step 2: Wire VfxSet on CombatArena scene object**

In `CombatArena.unity`, select the GameObject with `CombatArenaVfx` and assign `CombatArenaVfxSet.asset` to the `vfxSet` field (or rely on Resources fallback).

- [ ] **Step 3: Update CombatArenaPresenter damage call**

In `PlayDamageEvent`, replace `vfx?.PlayDamage(targetWorld, ...)` with profile-aware call (Task 8 completes wiring; for now call `PlayRifleHit` as default).

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Presentation/Combat/Arena/CombatArenaVfx.cs
git commit -m "feat: combat VFX uses build-safe VfxSetSO with tracer support"
```

---

## Phase 1 — Attack profiles

### Task 3: CombatAttackPresentationProfile + resolver

**Files:**
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatAttackPresentationKind.cs`
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatAttackPresentationProfile.cs`
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatAttackProfileResolver.cs`
- Create: `Assets/_Project/Presentation.Tests/EditMode/CombatAttackProfileResolverTests.cs`

- [ ] **Step 1: Write failing tests**

Create `Assets/_Project/Presentation.Tests/EditMode/CombatAttackProfileResolverTests.cs`:

```csharp
using DeadManZone.Core.Board;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatAttackProfileResolverTests
    {
        [Test]
        public void BallisticInfantry_ReturnsRifleStandShoot()
        {
            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            piece.category = PieceCategory.Infantry;
            piece.attackType = AttackType.Ballistic;

            var profile = CombatAttackProfileResolver.Resolve(piece);

            Assert.AreEqual(CombatAttackPresentationKind.InfantryRifle, profile.Kind);
            Assert.IsFalse(profile.UseForwardStep, "Gun units must not step toward target");
        }

        [Test]
        public void ExplosiveInfantry_ReturnsGrenadeThrow()
        {
            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            piece.category = PieceCategory.Infantry;
            piece.attackType = AttackType.Explosive;

            var profile = CombatAttackProfileResolver.Resolve(piece);

            Assert.AreEqual(CombatAttackPresentationKind.InfantryGrenade, profile.Kind);
        }

        [Test]
        public void MeleeInfantry_UsesForwardStep()
        {
            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            piece.category = PieceCategory.Infantry;
            piece.attackType = AttackType.Melee;

            var profile = CombatAttackProfileResolver.Resolve(piece);

            Assert.AreEqual(CombatAttackPresentationKind.InfantryMelee, profile.Kind);
            Assert.IsTrue(profile.UseForwardStep);
        }

        [Test]
        public void VehicleBallistic_ReturnsCannon()
        {
            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            piece.category = PieceCategory.Vehicle;
            piece.attackType = AttackType.Ballistic;

            var profile = CombatAttackProfileResolver.Resolve(piece);

            Assert.AreEqual(CombatAttackPresentationKind.VehicleCannon, profile.Kind);
        }
    }
}
```

- [ ] **Step 2: Run tests — expect FAIL**

- [ ] **Step 3: Implement types**

Create `CombatAttackPresentationKind.cs`:

```csharp
namespace DeadManZone.Presentation.Combat.Arena
{
    public enum CombatAttackPresentationKind
    {
        InfantryRifle,
        InfantryGrenade,
        InfantryMelee,
        VehicleCannon,
        BuildingArtillery
    }
}
```

Create `CombatAttackPresentationProfile.cs`:

```csharp
namespace DeadManZone.Presentation.Combat.Arena
{
    public readonly struct CombatAttackPresentationProfile
    {
        public CombatAttackPresentationKind Kind { get; }
        public bool UseForwardStep { get; }
        public float MuzzleDelaySeconds { get; }
        public float ImpactDelaySeconds { get; }
        public float TotalDurationSeconds { get; }

        public CombatAttackPresentationProfile(
            CombatAttackPresentationKind kind,
            bool useForwardStep,
            float muzzleDelaySeconds,
            float impactDelaySeconds,
            float totalDurationSeconds)
        {
            Kind = kind;
            UseForwardStep = useForwardStep;
            MuzzleDelaySeconds = muzzleDelaySeconds;
            ImpactDelaySeconds = impactDelaySeconds;
            TotalDurationSeconds = totalDurationSeconds;
        }

        public static CombatAttackPresentationProfile InfantryRifle => new(
            CombatAttackPresentationKind.InfantryRifle,
            useForwardStep: false,
            muzzleDelaySeconds: 0.08f,
            impactDelaySeconds: 0.20f,
            totalDurationSeconds: 0.55f);

        public static CombatAttackPresentationProfile InfantryGrenade => new(
            CombatAttackPresentationKind.InfantryGrenade,
            useForwardStep: false,
            muzzleDelaySeconds: 0.35f,
            impactDelaySeconds: 0.50f,
            totalDurationSeconds: 0.80f);

        public static CombatAttackPresentationProfile InfantryMelee => new(
            CombatAttackPresentationKind.InfantryMelee,
            useForwardStep: true,
            muzzleDelaySeconds: 0.10f,
            impactDelaySeconds: 0.18f,
            totalDurationSeconds: 0.45f);

        public static CombatAttackPresentationProfile VehicleCannon => new(
            CombatAttackPresentationKind.VehicleCannon,
            useForwardStep: false,
            muzzleDelaySeconds: 0.05f,
            impactDelaySeconds: 0.25f,
            totalDurationSeconds: 0.50f);

        public static CombatAttackPresentationProfile BuildingArtillery => new(
            CombatAttackPresentationKind.BuildingArtillery,
            useForwardStep: false,
            muzzleDelaySeconds: 0.05f,
            impactDelaySeconds: 0.30f,
            totalDurationSeconds: 0.55f);
    }
}
```

Create `CombatAttackProfileResolver.cs`:

```csharp
using DeadManZone.Core.Board;
using DeadManZone.Data;

namespace DeadManZone.Presentation.Combat.Arena
{
    internal static class CombatAttackProfileResolver
    {
        public static CombatAttackPresentationProfile Resolve(PieceDefinitionSO piece)
        {
            if (piece == null)
                return CombatAttackPresentationProfile.InfantryRifle;

            if (piece.grantedAbility == GrantedAbility.GrenadeLob
                || piece.attackType == AttackType.Explosive)
                return CombatAttackPresentationProfile.InfantryGrenade;

            if (piece.attackType == AttackType.Melee)
                return CombatAttackPresentationProfile.InfantryMelee;

            if (piece.category == PieceCategory.Building)
                return CombatAttackPresentationProfile.BuildingArtillery;

            if (piece.category == PieceCategory.Vehicle)
                return CombatAttackPresentationProfile.VehicleCannon;

            return CombatAttackPresentationProfile.InfantryRifle;
        }
    }
}
```

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Presentation/Combat/Arena/CombatAttackPresentationKind.cs Assets/_Project/Presentation/Combat/Arena/CombatAttackPresentationProfile.cs Assets/_Project/Presentation/Combat/Arena/CombatAttackProfileResolver.cs Assets/_Project/Presentation.Tests/EditMode/CombatAttackProfileResolverTests.cs
git commit -m "feat: add combat attack presentation profiles"
```

---

## Phase 1 — Animation set + visual drivers

### Task 4: CombatArenaAnimationSetSO

**Files:**
- Create: `Assets/_Project/Data/ScriptableObjects/CombatArenaAnimationSetSO.cs`
- Create: `Assets/_Project/Data/Resources/DeadManZone/CombatArenaAnimationSet.asset`

- [ ] **Step 1: Create ScriptableObject**

```csharp
using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Combat Arena Animation Set")]
    public sealed class CombatArenaAnimationSetSO : ScriptableObject
    {
        [Header("Kevin Iglesias — shoot")]
        public AnimationClip rifleShoot;
        public AnimationClip grenadeThrow;

        [Header("Kevin Iglesias — death")]
        public AnimationClip death01;
        public AnimationClip death02;
        public AnimationClip death03;

        [Header("Synty Sidekick fallback death")]
        public AnimationClip sidekickDeathForward;
    }
}
```

- [ ] **Step 2: Create asset and assign clips**

Save as `Assets/_Project/Data/Resources/DeadManZone/CombatArenaAnimationSet.asset`.

Assign from FBX clip sub-assets:

| Field | Source FBX |
|-------|------------|
| `rifleShoot` | `Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Rifle/HumanM@Rifle_Aim01_Shoot01.fbx` → clip `HumanM@Rifle_Aim01_Shoot01` |
| `grenadeThrow` | `.../Grenade/HumanM@ThrowGrenade01_L.fbx` |
| `death01` | `.../Combat/HumanM@Death01.fbx` |
| `death02` | `.../Combat/HumanM@Death02.fbx` |
| `death03` | `.../Combat/HumanM@Death03.fbx` |
| `sidekickDeathForward` | `Assets/Synty/AnimationSwordCombat/Animations/Sidekick/Death/A_MOD_SWD_Death_F_Neut.fbx` |

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Data/ScriptableObjects/CombatArenaAnimationSetSO.cs Assets/_Project/Data/Resources/DeadManZone/CombatArenaAnimationSet.asset
git commit -m "feat: add combat arena animation clip set"
```

---

### Task 5: Extend ICombatUnitVisualDriver + HumanoidCombatVisualDriver

**Files:**
- Modify: `Assets/_Project/Presentation/Combat/Arena/ICombatUnitVisualDriver.cs`
- Create: `Assets/_Project/Presentation/Combat/Arena/HumanoidCombatVisualDriver.cs`
- Create: `Assets/_Project/Presentation/Combat/Arena/VehicleCombatVisualDriver.cs`
- Modify: `Assets/_Project/Presentation/Combat/Arena/StaticMeshVisualDriver.cs` (keep as thin wrapper or remove in favor of VehicleCombatVisualDriver)

- [ ] **Step 1: Extend interface**

Replace `ICombatUnitVisualDriver.cs`:

```csharp
using System;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public interface ICombatUnitVisualDriver
    {
        void Bind(Animator animator, CombatArenaAnimationSetSO animationSet);
        void SetWalking(bool walking);
        void PlayAttack(CombatAttackPresentationProfile profile, Action onImpactFrame);
        void PlayDeath(Action onComplete);
        void Clear();
        Vector3 GetMuzzleWorldPosition();
    }
}
```

- [ ] **Step 2: Implement HumanoidCombatVisualDriver**

Create `HumanoidCombatVisualDriver.cs` (~120 lines):

```csharp
using System;
using System.Collections;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class HumanoidCombatVisualDriver : ICombatUnitVisualDriver
    {
        private static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int IsWalking = Animator.StringToHash("IsWalking");
        private static readonly int CurrentGait = Animator.StringToHash("CurrentGait");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");

        private const float WalkSpeed = 1.5f;
        private const int WalkGait = 1;

        private Animator _animator;
        private Transform _modelRoot;
        private CombatArenaMuzzleAnchor _muzzleAnchor;
        private CombatArenaAnimationSetSO _animationSet;
        private MonoBehaviour _coroutineHost;
        private Coroutine _attackRoutine;

        public void Configure(MonoBehaviour coroutineHost, Transform modelRoot)
        {
            _coroutineHost = coroutineHost;
            _modelRoot = modelRoot;
            _muzzleAnchor = modelRoot != null ? modelRoot.GetComponentInChildren<CombatArenaMuzzleAnchor>() : null;
        }

        public void Bind(Animator animator, CombatArenaAnimationSetSO animationSet)
        {
            _animator = animator;
            _animationSet = animationSet;
            if (_animator == null)
                return;

            _animator.applyRootMotion = false;
            _animator.SetBool(IsGrounded, true);
            _animator.SetFloat(MoveSpeed, 0f);
            _animator.SetBool(IsWalking, false);
            _animator.SetInteger(CurrentGait, 0);
        }

        public void SetWalking(bool walking)
        {
            if (_animator == null)
                return;

            _animator.SetBool(IsWalking, walking);
            _animator.SetFloat(MoveSpeed, walking ? WalkSpeed : 0f);
            _animator.SetInteger(CurrentGait, walking ? WalkGait : 0);
        }

        public void PlayAttack(CombatAttackPresentationProfile profile, Action onImpactFrame)
        {
            if (_attackRoutine != null && _coroutineHost != null)
                _coroutineHost.StopCoroutine(_attackRoutine);

            AnimationClip clip = profile.Kind switch
            {
                CombatAttackPresentationKind.InfantryGrenade => _animationSet?.grenadeThrow,
                _ => _animationSet?.rifleShoot
            };

            if (clip != null && _animator != null)
                _animator.CrossFadeInFixedTime(clip.name, 0.08f, layer: 0);

            if (_coroutineHost != null)
                _attackRoutine = _coroutineHost.StartCoroutine(AttackTimingRoutine(profile, onImpactFrame));
        }

        public void PlayDeath(Action onComplete)
        {
            AnimationClip clip = PickDeathClip();
            if (clip != null && _animator != null)
            {
                _animator.CrossFadeInFixedTime(clip.name, 0.06f, layer: 0);
                if (_coroutineHost != null)
                    _coroutineHost.StartCoroutine(WaitThenComplete(clip.length, onComplete));
                return;
            }

            onComplete?.Invoke();
        }

        public Vector3 GetMuzzleWorldPosition()
        {
            if (_muzzleAnchor != null)
                return _muzzleAnchor.transform.position;

            if (_modelRoot == null)
                return Vector3.zero;

            return _modelRoot.position + _modelRoot.forward * 0.4f + Vector3.up * 1.2f;
        }

        public void Clear()
        {
            _animator = null;
            _animationSet = null;
            _modelRoot = null;
            _muzzleAnchor = null;
            _coroutineHost = null;
            _attackRoutine = null;
        }

        private AnimationClip PickDeathClip()
        {
            if (_animationSet == null)
                return null;

            int pick = UnityEngine.Random.Range(0, 3);
            return pick switch
            {
                0 => _animationSet.death01 ?? _animationSet.sidekickDeathForward,
                1 => _animationSet.death02 ?? _animationSet.sidekickDeathForward,
                _ => _animationSet.death03 ?? _animationSet.sidekickDeathForward
            };
        }

        private IEnumerator AttackTimingRoutine(CombatAttackPresentationProfile profile, Action onImpactFrame)
        {
            if (profile.MuzzleDelaySeconds > 0f)
                yield return new WaitForSeconds(profile.MuzzleDelaySeconds);

            onImpactFrame?.Invoke();

            float remaining = profile.TotalDurationSeconds - profile.MuzzleDelaySeconds;
            if (remaining > 0f)
                yield return new WaitForSeconds(remaining);

            SetWalking(false);
            _attackRoutine = null;
        }

        private static IEnumerator WaitThenComplete(float seconds, Action onComplete)
        {
            yield return new WaitForSeconds(Mathf.Max(seconds, 0.1f));
            onComplete?.Invoke();
        }
    }
}
```

- [ ] **Step 3: Create CombatArenaMuzzleAnchor marker**

Create `CombatArenaMuzzleAnchor.cs`:

```csharp
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Empty marker on arena unit prefab; place at rifle muzzle.</summary>
    public sealed class CombatArenaMuzzleAnchor : MonoBehaviour { }
}
```

Add child `MuzzleAnchor` to each `ArenaUnit_*.prefab` at approximate rifle tip (editor step).

- [ ] **Step 4: Implement VehicleCombatVisualDriver**

Create `VehicleCombatVisualDriver.cs` with short recoil oscillation coroutine on `_modelRoot.localPosition` and `GetMuzzleWorldPosition()` using forward offset.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Presentation/Combat/Arena/ICombatUnitVisualDriver.cs Assets/_Project/Presentation/Combat/Arena/HumanoidCombatVisualDriver.cs Assets/_Project/Presentation/Combat/Arena/VehicleCombatVisualDriver.cs Assets/_Project/Presentation/Combat/Arena/CombatArenaMuzzleAnchor.cs
git commit -m "feat: humanoid and vehicle combat visual drivers"
```

---

### Task 6: Update CombatArenaUnitVisual + CombatUnitActor

**Files:**
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatArenaUnitVisual.cs`
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatUnitActor.cs`

- [ ] **Step 1: Pass animation set + profile into unit visual**

In `CombatArenaUnitVisual.Build`, load animation set:

```csharp
var animationSet = Resources.Load<CombatArenaAnimationSetSO>("DeadManZone/CombatArenaAnimationSet");
var animator = instance.GetComponentInChildren<Animator>();
if (animator != null && animator.runtimeAnimatorController != null)
{
    _driver = new HumanoidCombatVisualDriver();
    ((HumanoidCombatVisualDriver)_driver).Configure(this, _modelRoot);
    _driver.Bind(animator, animationSet);
}
else
{
    _driver = new VehicleCombatVisualDriver();
    ((VehicleCombatVisualDriver)_driver).Configure(this, _modelRoot);
    _driver.Bind(null, animationSet);
}
```

Replace `PlayAttackToward`:

```csharp
public void PlayAttackToward(Vector3 targetWorld, CombatAttackPresentationProfile profile, Action<Vector3> onMuzzle, Action onImpact)
{
    if (_modelRoot == null)
        return;

    FaceWorldDirection(flatTarget - _modelRoot.position);

    _driver?.PlayAttack(profile, () =>
    {
        onMuzzle?.Invoke(_driver.GetMuzzleWorldPosition());
        onImpact?.Invoke();
    });
}
```

Remove old `AttackRoutine` that only waited without VFX callbacks.

- [ ] **Step 2: Update CombatUnitActor**

Add field `CombatAttackPresentationProfile _attackProfile`.

Add to `Initialize(...)` signature: `CombatAttackPresentationProfile attackProfile`.

In `PlayAttackToward`:
- If `_attackProfile.UseForwardStep` (Melee only): run existing `LungeRoutine`
- Else if model visual: call `_unitVisual.PlayAttackToward` with VFX callbacks passed from presenter via delegate, OR store `CombatArenaVfx` reference on actor

In `PlayDeath`: call `_unitVisual.PlayDeath(onComplete)` instead of scale-to-zero when model visual; keep scale fallback for billboards.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Presentation/Combat/Arena/CombatArenaUnitVisual.cs Assets/_Project/Presentation/Combat/Arena/CombatUnitActor.cs
git commit -m "feat: stand-and-shoot attack path on arena unit actors"
```

---

### Task 7: Wire CombatArenaPresenter + timed VFX

**Files:**
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatArenaPresenter.cs`

- [ ] **Step 1: Pass attack profile at Initialize**

When calling `actor.Initialize`, add:

```csharp
CombatAttackProfileResolver.Resolve(source)
```

- [ ] **Step 2: Profile-aware PlayDamageEvent**

Replace `PlayDamageEvent`:

```csharp
private void PlayDamageEvent(CombatEvent combatEvent)
{
    if (!TryGetDamageTargetPosition(combatEvent, out var targetWorld))
        return;

    PieceDefinitionSO piece = null;
    if (_actors.TryGetValue(combatEvent.ActorId, out var attacker))
    {
        // resolve piece from actor instance → piece id lookup via battlefield if needed
        piece = ResolvePieceForActor(combatEvent.ActorId);
        var profile = CombatAttackProfileResolver.Resolve(piece);

        attacker.PlayAttackToward(targetWorld, profile, muzzleWorld =>
        {
            switch (profile.Kind)
            {
                case CombatAttackPresentationKind.InfantryGrenade:
                    // explosion only at impact timing — handled in onImpact
                    break;
                case CombatAttackPresentationKind.VehicleCannon:
                case CombatAttackPresentationKind.BuildingArtillery:
                    vfx?.PlayCannonHit(muzzleWorld, targetWorld, combatEvent.Value);
                    break;
                default:
                    vfx?.PlayRifleHit(muzzleWorld, targetWorld, combatEvent.Value);
                    break;
            }
        });
    }
    else
    {
        vfx?.PlayRifleHit(targetWorld, targetWorld, combatEvent.Value);
    }
}
```

Refactor `PlayRifleHit` to accept damage only once (split muzzle/impact timing): add `PlayRifleMuzzle`, `PlayRifleImpact`, or pass `spawnDamageText` flag on impact call.

Preferred split in `CombatArenaVfx`:

```csharp
public void PlayRifleMuzzleAndTracer(Vector3 muzzleWorld, Vector3 targetWorld) { ... }
public void PlayImpact(Vector3 targetWorld, int amount) { ... }
public void PlayExplosion(Vector3 targetWorld, int amount) { ... }
```

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Presentation/Combat/Arena/CombatArenaPresenter.cs Assets/_Project/Presentation/Combat/Arena/CombatArenaVfx.cs Assets/_Project/Presentation/Combat/Arena/CombatUnitActor.cs
git commit -m "feat: profile-aware combat VFX timing in arena presenter"
```

---

## Phase 1 — Visibility tuning

### Task 8: Scale + camera defaults

**Files:**
- Modify: `Assets/_Project/Data/Resources/DeadManZone/CombatArenaConfig.asset`
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatArenaPrefabResolver.cs`

- [ ] **Step 1: Update CombatArenaConfig.asset values**

| Field | New value |
|-------|-----------|
| `unitModelScaleMultiplier` | 1.6 |
| `defaultUnitModelHeight` | 2.1 |
| `fieldOfView` | 42 |
| `boardVerticalViewportCenter` | 0.48 |

- [ ] **Step 2: Category height in resolver**

Add to `CombatArenaPrefabResolver.ResolveUnitHeight`:

```csharp
if (piece != null && piece.combatArenaModelHeight > 0f)
    return piece.combatArenaModelHeight;

if (piece != null && piece.category == PieceCategory.Vehicle)
    return config != null && config.defaultVehicleModelHeight > 0f
        ? config.defaultVehicleModelHeight
        : 1.8f;

return config != null && config.defaultUnitModelHeight > 0f
    ? config.defaultUnitModelHeight
    : 2.1f;
```

Add `defaultVehicleModelHeight = 1.8f` to `CombatArenaConfigSO.cs`.

- [ ] **Step 3: Manual verify**

Play sandbox fight → press **C** for camera tuner if units still too small.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Data/ScriptableObjects/CombatArenaConfigSO.cs Assets/_Project/Data/Resources/DeadManZone/CombatArenaConfig.asset Assets/_Project/Presentation/Combat/Arena/CombatArenaPrefabResolver.cs
git commit -m "feat: increase combat arena unit scale and camera readability"
```

---

## Phase 1 — PlayMode smoke tests

### Task 9: CombatArenaSpectaclePlayModeTests

**Files:**
- Create: `Assets/_Project/Tests.PlayMode/CombatArenaSpectaclePlayModeTests.cs`

- [ ] **Step 1: Write test**

```csharp
[UnityTest]
public IEnumerator VfxSet_LoadsFromResources()
{
    var vfxSet = Resources.Load<CombatArenaVfxSetSO>("DeadManZone/CombatArenaVfxSet");
    Assert.NotNull(vfxSet);
    Assert.NotNull(vfxSet.rifleMuzzle);
    yield return null;
}
```

- [ ] **Step 2: Run PlayMode tests — expect PASS**

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Tests.PlayMode/CombatArenaSpectaclePlayModeTests.cs
git commit -m "test: combat spectacle VFX set loads in play mode"
```

---

## Phase 2 — Faction prefab assignment

### Task 10: Extend Synty catalog for faction pieces

**Files:**
- Modify: `Assets/_Project/Data/Editor/Synty/SyntyArtCatalogFactory.cs`
- Modify: `Assets/_Project/Data/Editor/Synty/SyntyArtBatchRunner.cs`
- Modify: `Assets/_Project/Data/Resources/DeadManZone/SandboxArtCatalog.asset` (regenerated)

- [ ] **Step 1: Add faction piece IDs to catalog factory**

For each missing piece (~15), map to existing wrapper prefab path:

| Piece ID | Wrapper prefab |
|----------|----------------|
| `wraith_stalker` | `ArenaUnit_Sniper.prefab` |
| `crimson_tank` | `ArenaVehicle_Tank.prefab` |
| `echo_hq` | `ArenaBuilding_Hq.prefab` |
| *(etc.)* | Reuse role/vehicle/building wrappers per faction theme |

- [ ] **Step 2: Run menu DeadManZone → Synty → Apply Full Synty Art Pass**

- [ ] **Step 3: Commit generated assets**

```bash
git add Assets/_Project/Data/Editor/Synty/ Assets/_Project/Data/Resources/DeadManZone/Pieces/
git commit -m "feat: assign Synty arena prefabs to faction pieces"
```

---

### Task 11: CombatArenaArtCoverageTests

**Files:**
- Create: `Assets/_Project/Core.Tests/EditMode/CombatArenaArtCoverageTests.cs`

- [ ] **Step 1: Write test covering all catalog entries with RequiresCombatArenaPrefab**

Extend pattern from `SandboxArtCoverageTests` but iterate `SandboxArtCatalogSO` entries instead of `SandboxArtRoster.AllPieceIds` only.

- [ ] **Step 2: Run EditMode — expect PASS after Task 10**

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Core.Tests/EditMode/CombatArenaArtCoverageTests.cs
git commit -m "test: all catalog pieces have combat arena prefabs"
```

---

## Spec coverage self-review

| Spec requirement | Task |
|------------------|------|
| Larger units / camera | Task 8 |
| Stand-and-shoot (no gun lunge) | Tasks 3, 6, 7 |
| PolygonParticleFX VFX | Tasks 1, 2, 7 |
| Human Soldier shoot anims | Tasks 4, 5, 6 |
| Death anims + VFX | Tasks 4, 5, 6 |
| Melee forward step only | Task 3 profile |
| Phase 2 faction prefabs | Tasks 10, 11 |
| Build-safe VFX refs | Task 1 |
| Tests | Tasks 1, 3, 9, 11 |

---

## Manual QA checklist

- [ ] Sandbox fight: units clearly larger than before
- [ ] Rifle unit fires in place — no charge toward enemy
- [ ] Muzzle flash + tracer + impact visible on each hit
- [ ] Grenade/explosive unit throws without running forward
- [ ] Tank/vehicle fires with cannon VFX, no mesh translation
- [ ] Death: animation + dust/explosion, not shrink-to-zero
- [ ] Tactic pause still freezes particles and animators
- [ ] Player build works after fight ends
