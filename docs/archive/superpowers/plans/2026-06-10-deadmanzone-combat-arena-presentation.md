> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# Combat Arena Presentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace flat UI-grid combat replay with a Top Troops–style 3D arena: additive scene, camera-facing unit billboards, smooth movement, world VFX, and full visual freeze on tactic pauses — while leaving Core combat sim unchanged.

**Architecture:** Add a `Presentation/Combat/Arena/` layer driven by the existing `CombatDirector.EventReplayed` bus. `CombatArenaPresenter` spawns `CombatUnitActor` billboards from the saved battlefield snapshot; `CombatBoardPresenter` is skipped when arena mode is active. `RunSceneController` hides build UI during combat; `CombatFlowPresenter` loads the additive `CombatArena` scene during the existing loading overlay.

**Tech Stack:** Unity 6, C#, uGUI overlay + 3D sub-scene, Unity Test Framework (Edit Mode + Play Mode), existing asmdefs under `Assets/_Project/`.

**Spec reference:** `docs/superpowers/specs/2026-06-10-deadmanzone-combat-arena-presentation-design.md`

**Branch:** `combat-rework`

---

## File map

| Path | Responsibility |
|------|----------------|
| `Assets/_Project/Game/GameScenes.cs` | Add `CombatArena` scene name |
| `Assets/_Project/Data/ScriptableObjects/CombatArenaConfigSO.cs` | Cell size, camera angles, lerp timings |
| `Assets/_Project/Presentation/Combat/Arena/CombatGridMapper.cs` | `GridCoord` ↔ world position |
| `Assets/_Project/Presentation/Combat/Arena/CombatBillboard.cs` | Camera-facing quad |
| `Assets/_Project/Presentation/Combat/Arena/CombatUnitActor.cs` | Per-combatant billboard + motion |
| `Assets/_Project/Presentation/Combat/Arena/CombatUnitActorPool.cs` | Reuse actor instances |
| `Assets/_Project/Presentation/Combat/Arena/CombatArenaBootstrap.cs` | Ground, camera, lighting in arena scene |
| `Assets/_Project/Presentation/Combat/Arena/CombatArenaSceneLoader.cs` | Additive load/unload |
| `Assets/_Project/Presentation/Combat/Arena/CombatArenaPresenter.cs` | Event replay → actors |
| `Assets/_Project/Presentation/Combat/Arena/CombatArenaFreezeController.cs` | Pause/resume motion on tactic pause |
| `Assets/_Project/Presentation/Combat/Arena/CombatArenaVfx.cs` | Damage numbers, impact/death bursts |
| `Assets/_Project/Presentation/Combat/Arena/CombatPresentationMode.cs` | Arena-active flag for routing |
| `Assets/_Project/Presentation/Combat/CombatFlowPresenter.cs` | Load arena during loading coroutine |
| `Assets/_Project/Presentation/Combat/CombatBoardPresenter.cs` | Skip board replay when arena active |
| `Assets/_Project/Presentation/Run/RunSceneController.cs` | Hide board area during arena combat |
| `Assets/_Project/Scenes/CombatArena.unity` | Additive 3D arena scene |
| `Assets/_Project/Presentation.Tests/EditMode/CombatGridMapperTests.cs` | Mapper unit tests |
| `Assets/_Project/Presentation.Tests/DeadManZone.Presentation.Tests.asmdef` | Edit Mode test assembly |
| `Assets/_Project/Tests.PlayMode/CombatArenaPlayModeTests.cs` | Integration smoke tests |

---

## Phase 1 — Config, mapper & test assembly

### Task 1: Combat arena config SO

**Files:**
- Create: `Assets/_Project/Data/ScriptableObjects/CombatArenaConfigSO.cs`
- Create: `Assets/_Project/Data/Resources/DeadManZone/CombatArenaConfig.asset` (via Unity Create menu after script exists)
- Modify: `Assets/_Project/Game/GameScenes.cs`

- [ ] **Step 1: Add scene constant**

Modify `Assets/_Project/Game/GameScenes.cs`:

```csharp
public const string CombatArena = "CombatArena";
```

- [ ] **Step 2: Create config ScriptableObject**

Create `Assets/_Project/Data/ScriptableObjects/CombatArenaConfigSO.cs`:

```csharp
using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Combat Arena Config")]
    public sealed class CombatArenaConfigSO : ScriptableObject
    {
        [Header("Grid → world (meters)")]
        public float cellWidth = 1.8f;
        public float cellDepth = 1.8f;

        [Header("Camera")]
        public float cameraElevationDegrees = 35f;
        public float cameraAzimuthDegrees = 225f;
        public float cameraDistance = 28f;
        public float fieldOfView = 45f;

        [Header("Motion")]
        public float moveLerpSeconds = 0.4f;
        public float attackLungeSeconds = 0.15f;
        public float attackLungeDistance = 0.35f;

        [Header("Transition")]
        public float unitSpawnFadeSeconds = 0.35f;
    }
}
```

- [ ] **Step 3: Create asset in Unity**

Unity menu → Create → DeadManZone → Combat Arena Config → save as  
`Assets/_Project/Data/Resources/DeadManZone/CombatArenaConfig.asset`

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Data/ScriptableObjects/CombatArenaConfigSO.cs Assets/_Project/Data/Resources/DeadManZone/CombatArenaConfig.asset Assets/_Project/Game/GameScenes.cs
git commit -m "feat: add combat arena config and scene constant"
```

---

### Task 2: Presentation Edit Mode test assembly

**Files:**
- Create: `Assets/_Project/Presentation.Tests/DeadManZone.Presentation.Tests.asmdef`
- Create: `Assets/_Project/Presentation.Tests/EditMode/CombatGridMapperTests.cs`

- [ ] **Step 1: Create asmdef**

Create `Assets/_Project/Presentation.Tests/DeadManZone.Presentation.Tests.asmdef`:

```json
{
  "name": "DeadManZone.Presentation.Tests",
  "rootNamespace": "DeadManZone.Presentation.Tests",
  "references": [
    "DeadManZone.Core",
    "DeadManZone.Presentation",
    "DeadManZone.Data",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": [
    "nunit.framework.dll"
  ],
  "autoReferenced": false,
  "defineConstraints": [
    "UNITY_INCLUDE_TESTS"
  ]
}
```

- [ ] **Step 2: Commit asmdef**

```bash
git add Assets/_Project/Presentation.Tests/DeadManZone.Presentation.Tests.asmdef
git commit -m "test: add Presentation Edit Mode test assembly"
```

---

### Task 3: CombatGridMapper

**Files:**
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatGridMapper.cs`
- Create: `Assets/_Project/Presentation.Tests/EditMode/CombatGridMapperTests.cs`

- [ ] **Step 1: Write the failing test**

Create `Assets/_Project/Presentation.Tests/EditMode/CombatGridMapperTests.cs`:

```csharp
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatGridMapperTests
    {
        [Test]
        public void ToWorld_CentersBattlefieldAtOrigin()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            var mapper = new CombatGridMapper(layout, cellWidth: 2f, cellDepth: 2f);

            var center = new GridCoord(layout.TotalWidth / 2, layout.Height / 2);
            Vector3 world = mapper.ToWorld(center);

            Assert.AreEqual(0f, world.x, 0.001f);
            Assert.AreEqual(0f, world.z, 0.001f);
        }

        [Test]
        public void ToWorld_PlayerFrontRow_HasLowerZThanEnemyFrontRow()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            var mapper = new CombatGridMapper(layout, 2f, 2f);

            var playerFront = mapper.ToWorld(new GridCoord(0, layout.Height - 1));
            var enemyFront = mapper.ToWorld(new GridCoord(layout.EnemyOriginX, layout.Height - 1));

            Assert.Less(playerFront.z, enemyFront.z);
        }

        [Test]
        public void TryWorldToCoord_RoundTrips()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            var mapper = new CombatGridMapper(layout, 1.8f, 1.8f);
            var original = new GridCoord(3, 2);

            Vector3 world = mapper.ToWorld(original);
            Assert.IsTrue(mapper.TryWorldToCoord(world, out var roundTripped));
            Assert.AreEqual(original, roundTripped);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run Edit Mode tests filtered to `CombatGridMapperTests`.  
Expected: FAIL — `CombatGridMapper` not found.

- [ ] **Step 3: Implement mapper**

Create `Assets/_Project/Presentation/Combat/Arena/CombatGridMapper.cs`:

```csharp
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatGridMapper
    {
        private readonly BattlefieldLayout _layout;
        private readonly float _cellWidth;
        private readonly float _cellDepth;

        public CombatGridMapper(BattlefieldLayout layout, float cellWidth, float cellDepth)
        {
            _layout = layout;
            _cellWidth = cellWidth;
            _cellDepth = cellDepth;
        }

        public Vector3 ToWorld(GridCoord coord)
        {
            float x = (coord.X + 0.5f - _layout.TotalWidth * 0.5f) * _cellWidth;
            float z = (_layout.Height * 0.5f - coord.Y - 0.5f) * _cellDepth;
            return new Vector3(x, 0f, z);
        }

        public bool TryWorldToCoord(Vector3 world, out GridCoord coord)
        {
            float xIndex = world.x / _cellWidth + _layout.TotalWidth * 0.5f - 0.5f;
            float yIndex = _layout.Height * 0.5f - world.z / _cellDepth - 0.5f;

            int x = Mathf.RoundToInt(xIndex);
            int y = Mathf.RoundToInt(yIndex);
            if (x < 0 || y < 0 || x >= _layout.TotalWidth || y >= _layout.Height)
            {
                coord = default;
                return false;
            }

            coord = new GridCoord(x, y);
            return true;
        }
    }
}
```

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/_Project/Presentation/Combat/Arena/CombatGridMapper.cs Assets/_Project/Presentation.Tests/EditMode/CombatGridMapperTests.cs
git commit -m "feat: add combat grid to world mapper"
```

---

## Phase 2 — Unit actors & billboard rendering

### Task 4: CombatBillboard & CombatUnitActor

**Files:**
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatBillboard.cs`
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatUnitActor.cs`

- [ ] **Step 1: Create CombatBillboard**

Create `Assets/_Project/Presentation/Combat/Arena/CombatBillboard.cs`:

```csharp
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatBillboard : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        private Transform _cameraTransform;

        public void Configure(Transform cameraTransform, Sprite sprite, float height = 1.6f)
        {
            _cameraTransform = cameraTransform;
            if (visualRoot == null)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "BillboardQuad";
                Destroy(quad.GetComponent<Collider>());
                visualRoot = quad.transform;
                visualRoot.SetParent(transform, false);
            }

            visualRoot.localPosition = new Vector3(0f, height * 0.5f, 0f);
            visualRoot.localScale = new Vector3(height * 0.75f, height, 1f);

            var renderer = visualRoot.GetComponent<MeshRenderer>();
            if (renderer != null && sprite != null)
            {
                var mat = new Material(Shader.Find("Unlit/Texture"));
                mat.mainTexture = sprite.texture;
                renderer.material = mat;
            }
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null || visualRoot == null)
                return;

            visualRoot.rotation = _cameraTransform.rotation;
        }
    }
}
```

- [ ] **Step 2: Create CombatUnitActor**

Create `Assets/_Project/Presentation/Combat/Arena/CombatUnitActor.cs`:

```csharp
using System.Collections;
using DeadManZone.Core.Common;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatUnitActor : MonoBehaviour
    {
        private CombatBillboard _billboard;
        private CombatGridMapper _mapper;
        private GridCoord _anchor;
        private Coroutine _moveRoutine;
        private Coroutine _lungeRoutine;
        private float _moveLerpSeconds = 0.4f;
        private float _lungeSeconds = 0.15f;
        private float _lungeDistance = 0.35f;
        private bool _frozen;

        public string InstanceId { get; private set; }
        public GridCoord Anchor => _anchor;
        public bool IsAlive { get; private set; } = true;

        public void Initialize(
            string instanceId,
            Sprite icon,
            Transform cameraTransform,
            CombatGridMapper mapper,
            GridCoord anchor,
            float moveLerpSeconds,
            float lungeSeconds,
            float lungeDistance)
        {
            InstanceId = instanceId;
            _mapper = mapper;
            _anchor = anchor;
            _moveLerpSeconds = moveLerpSeconds;
            _lungeSeconds = lungeSeconds;
            _lungeDistance = lungeDistance;
            IsAlive = true;
            gameObject.SetActive(true);

            _billboard = GetComponent<CombatBillboard>();
            if (_billboard == null)
                _billboard = gameObject.AddComponent<CombatBillboard>();
            _billboard.Configure(cameraTransform, icon);

            SnapToAnchor(anchor);
        }

        public void SetFrozen(bool frozen) => _frozen = frozen;

        public void SnapToAnchor(GridCoord anchor)
        {
            _anchor = anchor;
            transform.position = _mapper.ToWorld(anchor);
        }

        public void MoveTo(GridCoord anchor)
        {
            _anchor = anchor;
            if (_frozen)
            {
                SnapToAnchor(anchor);
                return;
            }

            if (_moveRoutine != null)
                StopCoroutine(_moveRoutine);
            _moveRoutine = StartCoroutine(MoveRoutine(_mapper.ToWorld(anchor)));
        }

        public void PlayAttackToward(Vector3 targetWorld)
        {
            if (_frozen || !IsAlive)
                return;

            if (_lungeRoutine != null)
                StopCoroutine(_lungeRoutine);
            _lungeRoutine = StartCoroutine(LungeRoutine(targetWorld));
        }

        public void PlayDeath(System.Action onComplete)
        {
            IsAlive = false;
            if (_moveRoutine != null)
                StopCoroutine(_moveRoutine);
            if (_lungeRoutine != null)
                StopCoroutine(_lungeRoutine);
            StartCoroutine(DeathRoutine(onComplete));
        }

        private IEnumerator MoveRoutine(Vector3 target)
        {
            Vector3 start = transform.position;
            float elapsed = 0f;
            while (elapsed < _moveLerpSeconds)
            {
                if (_frozen)
                    yield return null;
                else
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / _moveLerpSeconds);
                    transform.position = Vector3.Lerp(start, target, t);
                    yield return null;
                }
            }

            transform.position = target;
            _moveRoutine = null;
        }

        private IEnumerator LungeRoutine(Vector3 targetWorld)
        {
            Vector3 start = transform.position;
            Vector3 flatTarget = new Vector3(targetWorld.x, start.y, targetWorld.z);
            Vector3 dir = (flatTarget - start).normalized;
            Vector3 lungePoint = start + dir * _lungeDistance;
            float half = _lungeSeconds * 0.5f;

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                if (!_frozen)
                    transform.position = Vector3.Lerp(start, lungePoint, t / half);
                yield return null;
            }

            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                if (!_frozen)
                    transform.position = Vector3.Lerp(lungePoint, start, t / half);
                yield return null;
            }

            transform.position = start;
            _lungeRoutine = null;
        }

        private IEnumerator DeathRoutine(System.Action onComplete)
        {
            float duration = 0.35f;
            Vector3 startScale = transform.localScale;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float p = t / duration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
                yield return null;
            }

            onComplete?.Invoke();
            gameObject.SetActive(false);
        }

        public void ResetForPool()
        {
            InstanceId = null;
            IsAlive = true;
            transform.localScale = Vector3.one;
            if (_moveRoutine != null)
                StopCoroutine(_moveRoutine);
            if (_lungeRoutine != null)
                StopCoroutine(_lungeRoutine);
            _moveRoutine = null;
            _lungeRoutine = null;
            _frozen = false;
        }
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Presentation/Combat/Arena/CombatBillboard.cs Assets/_Project/Presentation/Combat/Arena/CombatUnitActor.cs
git commit -m "feat: add combat unit billboard actors with lerp motion"
```

---

### Task 5: CombatUnitActorPool

**Files:**
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatUnitActorPool.cs`

- [ ] **Step 1: Implement pool**

Create `Assets/_Project/Presentation/Combat/Arena/CombatUnitActorPool.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatUnitActorPool
    {
        private readonly Transform _root;
        private readonly Stack<CombatUnitActor> _available = new();

        public CombatUnitActorPool(Transform root) => _root = root;

        public CombatUnitActor Rent()
        {
            if (_available.Count > 0)
            {
                var actor = _available.Pop();
                actor.gameObject.SetActive(true);
                return actor;
            }

            var go = new GameObject("CombatUnitActor");
            go.transform.SetParent(_root, false);
            return go.AddComponent<CombatUnitActor>();
        }

        public void Release(CombatUnitActor actor)
        {
            if (actor == null)
                return;

            actor.ResetForPool();
            actor.transform.SetParent(_root, false);
            _available.Push(actor);
        }

        public void ReleaseAll(IEnumerable<CombatUnitActor> actors)
        {
            foreach (var actor in actors)
                Release(actor);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Presentation/Combat/Arena/CombatUnitActorPool.cs
git commit -m "feat: add combat unit actor pool"
```

---

## Phase 3 — Arena scene & bootstrap

### Task 6: CombatArena scene + bootstrap

**Files:**
- Create: `Assets/_Project/Scenes/CombatArena.unity`
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatArenaBootstrap.cs`
- Modify: `ProjectSettings/EditorBuildSettings.asset` (add CombatArena scene)

- [ ] **Step 1: Create bootstrap script**

Create `Assets/_Project/Presentation/Combat/Arena/CombatArenaBootstrap.cs`:

```csharp
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatArenaBootstrap : MonoBehaviour
    {
        [SerializeField] private Camera arenaCamera;
        [SerializeField] private Transform unitsRoot;
        [SerializeField] private Transform groundRoot;
        [SerializeField] private CombatArenaConfigSO config;

        public Camera ArenaCamera => arenaCamera;
        public Transform UnitsRoot => unitsRoot;
        public CombatArenaConfigSO Config => config;

        public static CombatArenaBootstrap Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            if (config == null)
                config = Resources.Load<CombatArenaConfigSO>("DeadManZone/CombatArenaConfig");

            EnsureGround();
            ConfigureCamera();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void EnsureGround()
        {
            if (groundRoot == null)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "ArenaGround";
                ground.transform.SetParent(transform, false);
                ground.transform.localScale = new Vector3(3f, 1f, 2f);
                groundRoot = ground.transform;
            }
        }

        private void ConfigureCamera()
        {
            if (arenaCamera == null)
            {
                var camGo = new GameObject("ArenaCamera");
                camGo.transform.SetParent(transform, false);
                arenaCamera = camGo.AddComponent<Camera>();
                arenaCamera.tag = "MainCamera";
            }

            if (config == null)
                return;

            arenaCamera.fieldOfView = config.fieldOfView;
            float elev = config.cameraElevationDegrees * Mathf.Deg2Rad;
            float azim = config.cameraAzimuthDegrees * Mathf.Deg2Rad;
            var offset = new Vector3(
                Mathf.Cos(elev) * Mathf.Cos(azim),
                Mathf.Sin(elev),
                Mathf.Cos(elev) * Mathf.Sin(azim)) * config.cameraDistance;

            arenaCamera.transform.position = offset;
            arenaCamera.transform.LookAt(Vector3.zero);

            if (unitsRoot == null)
            {
                var root = new GameObject("UnitsRoot");
                root.transform.SetParent(transform, false);
                unitsRoot = root.transform;
            }
        }
    }
}
```

- [ ] **Step 2: Create Unity scene**

1. File → New Scene → Basic (Built-in) or URP equivalent → save as `Assets/_Project/Scenes/CombatArena.unity`
2. Create empty GameObject `CombatArenaRoot`, add `CombatArenaBootstrap`
3. Assign `CombatArenaConfig` asset to config field
4. Save scene

- [ ] **Step 3: Add scene to Build Settings**

File → Build Settings → Add Open Scenes (must include `MainMenu`, `Run`, and `CombatArena`).

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Scenes/CombatArena.unity Assets/_Project/Scenes/CombatArena.unity.meta Assets/_Project/Presentation/Combat/Arena/CombatArenaBootstrap.cs ProjectSettings/EditorBuildSettings.asset
git commit -m "feat: add CombatArena scene and bootstrap"
```

---

### Task 7: CombatArenaSceneLoader

**Files:**
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatArenaSceneLoader.cs`
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatPresentationMode.cs`

- [ ] **Step 1: Presentation mode flag**

Create `Assets/_Project/Presentation/Combat/Arena/CombatPresentationMode.cs`:

```csharp
namespace DeadManZone.Presentation.Combat.Arena
{
    public static class CombatPresentationMode
    {
        public static bool ArenaActive { get; set; }
    }
}
```

- [ ] **Step 2: Scene loader**

Create `Assets/_Project/Presentation/Combat/Arena/CombatArenaSceneLoader.cs`:

```csharp
using System.Collections;
using DeadManZone.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatArenaSceneLoader : MonoBehaviour
    {
        public bool IsLoaded { get; private set; }

        public IEnumerator LoadAsync()
        {
            if (IsLoaded)
                yield break;

            var op = SceneManager.LoadSceneAsync(GameScenes.CombatArena, LoadSceneMode.Additive);
            while (op != null && !op.isDone)
                yield return null;

            IsLoaded = true;
            CombatPresentationMode.ArenaActive = true;
        }

        public IEnumerator UnloadAsync()
        {
            if (!IsLoaded)
                yield break;

            CombatPresentationMode.ArenaActive = false;
            var op = SceneManager.UnloadSceneAsync(GameScenes.CombatArena);
            while (op != null && !op.isDone)
                yield return null;

            IsLoaded = false;
        }
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Presentation/Combat/Arena/CombatPresentationMode.cs Assets/_Project/Presentation/Combat/Arena/CombatArenaSceneLoader.cs
git commit -m "feat: add additive combat arena scene loader"
```

---

## Phase 4 — Arena presenter, freeze & VFX

### Task 8: CombatArenaPresenter

**Files:**
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatArenaPresenter.cs`

- [ ] **Step 1: Implement presenter**

Create `Assets/_Project/Presentation/Combat/Arena/CombatArenaPresenter.cs` — mirrors `CombatBoardPresenter` restore logic but drives `CombatUnitActor` instances. Key behaviors:

- On enable: subscribe to `CombatDirector.EventReplayed` and `CombatAdvanced`
- `SpawnFromBattlefield(BattlefieldState, ContentRegistry)`: spawn actors only for cells whose definition includes `GameTagIds.Combatant`
- `RestoreFromBattlefieldAndEvents(...)`: same algorithm as `CombatReplayVisuals.RestoreFromBattlefieldAndEvents`
- `OnEventReplayed`: `move` → `actor.MoveTo`, `damage` → lunge toward target actor + VFX hook, `destroyed` → `PlayDeath` + pool release

Skeleton:

```csharp
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using DeadManZone.Game;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatArenaPresenter : MonoBehaviour
    {
        [SerializeField] private CombatDirector combatDirector;
        [SerializeField] private CombatArenaVfx vfx;

        private readonly Dictionary<string, CombatUnitActor> _actors = new();
        private readonly Dictionary<string, GridCoord> _anchors = new();
        private CombatUnitActorPool _pool;
        private CombatGridMapper _mapper;
        private ContentRegistry _registry;

        private void Awake()
        {
            if (combatDirector == null)
                combatDirector = GetComponent<CombatDirector>();
            _registry = ContentRegistryProvider.Build(ContentDatabase.Load());
        }

        private void OnEnable()
        {
            if (combatDirector != null)
                combatDirector.EventReplayed += OnEventReplayed;
            if (RunManager.Instance != null)
                RunManager.Instance.CombatAdvanced += OnCombatAdvanced;
        }

        private void OnDisable()
        {
            if (combatDirector != null)
                combatDirector.EventReplayed -= OnEventReplayed;
            if (RunManager.Instance != null)
                RunManager.Instance.CombatAdvanced -= OnCombatAdvanced;
        }

        public void InitializeArena(BattlefieldState battlefield)
        {
            var bootstrap = CombatArenaBootstrap.Instance;
            if (bootstrap == null)
                return;

            var config = bootstrap.Config;
            _mapper = new CombatGridMapper(battlefield.Layout, config.cellWidth, config.cellDepth);
            _pool ??= new CombatUnitActorPool(bootstrap.UnitsRoot);

            ClearActors();
            foreach (var cell in battlefield.Cells)
            {
                if (!PieceTagQueries.HasTag(cell.Definition, GameTagIds.Combatant))
                    continue;

                var source = _registry.GetPiece(cell.Definition.Id);
                var actor = _pool.Rent();
                actor.Initialize(
                    cell.InstanceId,
                    source?.icon,
                    bootstrap.ArenaCamera.transform,
                    _mapper,
                    cell.Position,
                    config.moveLerpSeconds,
                    config.attackLungeSeconds,
                    config.attackLungeDistance);
                _actors[cell.InstanceId] = actor;
                _anchors[cell.InstanceId] = cell.Position;
            }
        }

        public void RestoreState(BattlefieldState battlefield, IEnumerable<CombatEvent> events, CombatPhase? excludePhase)
        {
            InitializeArena(battlefield);
            if (events == null)
                return;

            foreach (var e in events.OrderBy(x => x.Phase).ThenBy(x => x.Tick))
            {
                if (excludePhase.HasValue && e.Phase == excludePhase.Value)
                    continue;
                ApplyEventStateOnly(e);
            }

            foreach (var pair in _anchors)
            {
                if (_actors.TryGetValue(pair.Key, out var actor))
                    actor.SnapToAnchor(pair.Value);
            }
        }

        private void OnCombatAdvanced(CombatAdvanceResult result)
        {
            RestoreBeforeSegment(result.CompletedPhase);
        }

        private void RestoreBeforeSegment(CombatPhase completedPhase)
        {
            if (RunManager.Instance == null || _registry == null)
                return;

            var state = RunManager.Instance.State;
            if (state.Phase != RunPhase.Combat || state.Combat?.EnemyBoard == null)
                return;

            var playerBoard = RunManager.Instance.Orchestrator.GetPlayerBoard();
            var enemyBoard = BoardSnapshotMapper.ToBoard(state.Combat.EnemyBoard, _registry);
            var battlefield = BattlefieldState.FromBoards(playerBoard, enemyBoard);
            var exclude = state.Combat.AwaitingCommand ? state.Combat.CompletedPhase : (CombatPhase?)null;
            var events = state.Combat.EventLog.Select(Convert).ToList();
            RestoreState(battlefield, events, exclude);
        }

        private void OnEventReplayed(CombatEvent e)
        {
            if (!CombatPresentationMode.ArenaActive || e == null)
                return;

            ApplyEventStateOnly(e);
            ApplyEventVisual(e);
        }

        private void ApplyEventStateOnly(CombatEvent e)
        {
            switch (e.ActionType)
            {
                case "move":
                    if (TryParseCoord(e.TargetId, out var dest))
                        _anchors[e.ActorId] = dest;
                    break;
                case "destroyed":
                    _anchors.Remove(e.ActorId);
                    break;
            }
        }

        private void ApplyEventVisual(CombatEvent e)
        {
            if (!_actors.TryGetValue(e.ActorId, out var actor) && e.ActionType != "damage")
                return;

            switch (e.ActionType)
            {
                case "move":
                    if (_anchors.TryGetValue(e.ActorId, out var dest))
                        actor.MoveTo(dest);
                    break;
                case "damage":
                    if (_actors.TryGetValue(e.ActorId, out var attacker))
                    {
                        Vector3 targetPos = _actors.TryGetValue(e.TargetId, out var target)
                            ? target.transform.position
                            : _mapper.ToWorld(ParseCoordOrDefault(e.TargetId));
                        attacker.PlayAttackToward(targetPos);
                    }
                    vfx?.PlayDamage(_mapper.ToWorld(ParseCoordOrDefault(e.TargetId)), e.Value);
                    break;
                case "destroyed":
                    if (_actors.TryGetValue(e.ActorId, out var dead))
                    {
                        _actors.Remove(e.ActorId);
                        dead.PlayDeath(() => _pool.Release(dead));
                    }
                    vfx?.PlayDeath(_mapper.ToWorld(ParseCoordOrDefault(e.ActorId)));
                    break;
            }
        }

        private static CombatEvent Convert(CombatEventRecord r) => new()
        {
            Phase = r.Phase,
            Tick = r.Tick,
            ActorId = r.ActorId,
            ActionType = r.ActionType,
            TargetId = r.TargetId,
            Value = r.Value
        };

        private static bool TryParseCoord(string s, out GridCoord coord)
        {
            coord = default;
            if (string.IsNullOrEmpty(s) || !s.Contains(','))
                return false;
            var parts = s.Split(',');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
                return false;
            coord = new GridCoord(x, y);
            return true;
        }

        private GridCoord ParseCoordOrDefault(string s) =>
            TryParseCoord(s, out var c) ? c : default;

        private void ClearActors()
        {
            _pool?.ReleaseAll(_actors.Values);
            _actors.Clear();
            _anchors.Clear();
        }
    }
}
```

**Note:** Verify `move` event `TargetId` format against `TickCombatRun` log calls — adjust parser if target encodes coords differently (grep `"move"` append sites and match format).

- [ ] **Step 2: Grep move event format and fix parser if needed**

Run: `rg "Append.*move" Assets/_Project/Core/Combat`

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Presentation/Combat/Arena/CombatArenaPresenter.cs
git commit -m "feat: add combat arena presenter driven by event log"
```

---

### Task 9: CombatArenaFreezeController

**Files:**
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatArenaFreezeController.cs`
- Modify: `Assets/_Project/Presentation/Combat/CombatFlowPresenter.cs`

- [ ] **Step 1: Freeze controller**

Create `Assets/_Project/Presentation/Combat/Arena/CombatArenaFreezeController.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatArenaFreezeController : MonoBehaviour
    {
        [SerializeField] private CombatDirector combatDirector;
        [SerializeField] private CombatArenaPresenter arenaPresenter;

        private readonly List<ParticleSystem> _trackedParticles = new();
        private bool _frozen;

        private void OnEnable()
        {
            if (combatDirector != null)
                combatDirector.PausedForCommands += OnPausedForCommands;
        }

        private void OnDisable()
        {
            if (combatDirector != null)
                combatDirector.PausedForCommands -= OnPausedForCommands;
        }

        public void Resume()
        {
            _frozen = false;
            SetActorFreeze(false);
            SetParticlesPaused(false);
        }

        private void OnPausedForCommands(CombatPhase _)
        {
            _frozen = true;
            SetActorFreeze(true);
            SetParticlesPaused(true);
        }

        private void SetActorFreeze(bool frozen)
        {
            if (arenaPresenter == null)
                return;

            foreach (var actor in arenaPresenter.GetActiveActors())
                actor.SetFrozen(frozen);
        }

        private void SetParticlesPaused(bool paused)
        {
            _trackedParticles.RemoveAll(p => p == null);
            foreach (var ps in _trackedParticles)
            {
                if (paused) ps.Pause(true);
                else ps.Play(true);
            }
        }

        public void TrackParticle(ParticleSystem ps)
        {
            if (ps != null && !_trackedParticles.Contains(ps))
                _trackedParticles.Add(ps);
        }
    }
}
```

Add to `CombatArenaPresenter`:

```csharp
public IEnumerable<CombatUnitActor> GetActiveActors() => _actors.Values;
```

- [ ] **Step 2: Wire resume in CombatFlowPresenter**

In `CombatFlowPresenter`, add optional `[SerializeField] CombatArenaFreezeController freezeController;`  
Call `freezeController?.Resume()` at start of `LoadingThenPresent` and when tactic panel submits continue (hook via existing continue path — when `combatDirector.ContinueCombat()` is invoked from UI, call `Resume()` first).

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Presentation/Combat/Arena/CombatArenaFreezeController.cs Assets/_Project/Presentation/Combat/CombatFlowPresenter.cs
git commit -m "feat: freeze arena motion on tactic pause"
```

---

### Task 10: CombatArenaVfx

**Files:**
- Create: `Assets/_Project/Presentation/Combat/Arena/CombatArenaVfx.cs`

- [ ] **Step 1: Implement world-space VFX**

Create `Assets/_Project/Presentation/Combat/Arena/CombatArenaVfx.cs`:

```csharp
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatArenaVfx : MonoBehaviour
    {
        [SerializeField] private CombatArenaFreezeController freezeController;
        [SerializeField] private ParticleSystem impactPrefab;
        [SerializeField] private ParticleSystem deathPrefab;

        public void PlayDamage(Vector3 worldPosition, int amount)
        {
            SpawnBurst(impactPrefab, worldPosition);
            SpawnFloatingText(worldPosition, $"-{amount}");
        }

        public void PlayDeath(Vector3 worldPosition) =>
            SpawnBurst(deathPrefab, worldPosition);

        private void SpawnBurst(ParticleSystem prefab, Vector3 position)
        {
            if (prefab == null)
                return;

            var ps = Instantiate(prefab, position, Quaternion.identity, transform);
            ps.Play();
            freezeController?.TrackParticle(ps);
            Destroy(ps.gameObject, 2f);
        }

        private void SpawnFloatingText(Vector3 worldPosition, string text)
        {
            var cam = CombatArenaBootstrap.Instance?.ArenaCamera;
            if (cam == null)
                return;

            var go = new GameObject("DamageNumber");
            go.transform.position = worldPosition + Vector3.up * 1.2f;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 4;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.35f, 0.35f);

            Destroy(go, 0.9f);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Presentation/Combat/Arena/CombatArenaVfx.cs
git commit -m "feat: add world-space combat arena VFX"
```

---

## Phase 5 — Integration & routing

### Task 11: Wire flow — hide build UI, load arena, skip board replay

**Files:**
- Modify: `Assets/_Project/Presentation/Combat/CombatFlowPresenter.cs`
- Modify: `Assets/_Project/Presentation/Combat/CombatBoardPresenter.cs`
- Modify: `Assets/_Project/Presentation/Run/RunSceneController.cs`

- [ ] **Step 1: CombatFlowPresenter loads arena during overlay**

Add serialized fields:

```csharp
[SerializeField] private CombatArenaSceneLoader arenaLoader;
[SerializeField] private CombatArenaPresenter arenaPresenter;
[SerializeField] private CombatArenaFreezeController freezeController;
```

Replace `LoadingThenPresent`:

```csharp
private IEnumerator LoadingThenPresent()
{
    freezeController?.Resume();
    if (arenaLoader != null)
        yield return arenaLoader.LoadAsync();

    if (arenaPresenter != null && RunManager.Instance != null)
    {
        var registry = ContentRegistryProvider.Build(ContentDatabase.Load());
        var playerBoard = RunManager.Instance.Orchestrator.GetPlayerBoard();
        var enemyBoard = BoardSnapshotMapper.ToBoard(RunManager.Instance.State.Combat.EnemyBoard, registry);
        var battlefield = BattlefieldState.FromBoards(playerBoard, enemyBoard);
        arenaPresenter.InitializeArena(battlefield);
        arenaPresenter.RestoreState(
            battlefield,
            System.Array.Empty<CombatEvent>(),
            excludePhase: null);
    }

    if (loadingDurationSeconds > 0f)
        yield return new WaitForSeconds(loadingDurationSeconds);
    else
        yield return null;

    HideLoadingOverlay();
    combatDirector?.PresentCombatAfterLoading();
    _loadingRoutine = null;
}
```

On `OnCombatPresentationCompleted`, after battle report: `StartCoroutine(UnloadArena())`:

```csharp
private IEnumerator UnloadArena()
{
    if (arenaLoader != null)
        yield return arenaLoader.UnloadAsync();
}
```

- [ ] **Step 2: Skip board replay when arena active**

In `CombatBoardPresenter.OnEventReplayed` and `OnCombatAdvanced`, return early if `CombatPresentationMode.ArenaActive`.

- [ ] **Step 3: Hide board area during arena combat**

In `RunSceneController.SetCombatPresentationLayout`, when `combatActive`:

```csharp
if (boardArea != null)
    boardArea.gameObject.SetActive(false);
```

When restoring build (`combatActive == false`):

```csharp
if (boardArea != null)
    boardArea.gameObject.SetActive(true);
```

Remove or guard the full-screen board stretch block — arena is 3D, not expanded UI board.

- [ ] **Step 4: Wire components on Run scene**

In `Run.unity` combat panel hierarchy, add GameObjects with:
- `CombatArenaSceneLoader`
- `CombatArenaPresenter`
- `CombatArenaFreezeController`
- `CombatArenaVfx`

Assign references on `CombatFlowPresenter`.

- [ ] **Step 5: Manual smoke test**

1. Play Run scene → start run → place units → Begin Fight  
2. Expect: loading overlay → 3D arena with billboards → units move on replay  
3. After segment 1: motion freezes, tactic panel appears  
4. Continue: motion resumes  
5. After fight: battle report, build UI returns

- [ ] **Step 6: Commit**

```bash
git add Assets/_Project/Presentation/Combat/CombatFlowPresenter.cs Assets/_Project/Presentation/Combat/CombatBoardPresenter.cs Assets/_Project/Presentation/Run/RunSceneController.cs Assets/_Project/Scenes/Run.unity
git commit -m "feat: wire combat arena presentation into run flow"
```

---

### Task 12: Play Mode tests

**Files:**
- Create: `Assets/_Project/Tests.PlayMode/CombatArenaPlayModeTests.cs`

- [ ] **Step 1: Write Play Mode tests**

```csharp
using System.Collections;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadManZone.PlayMode.Tests
{
    public sealed class CombatArenaPlayModeTests
    {
        [UnityTest]
        public IEnumerator LoadAsync_SetsArenaActive()
        {
            var go = new GameObject("Loader");
            var loader = go.AddComponent<CombatArenaSceneLoader>();
            CombatPresentationMode.ArenaActive = false;

            yield return loader.LoadAsync();

            Assert.IsTrue(CombatPresentationMode.ArenaActive);
            Assert.IsTrue(loader.IsLoaded);
            Assert.IsNotNull(CombatArenaBootstrap.Instance);

            yield return loader.UnloadAsync();
            Object.Destroy(go);
        }
    }
}
```

- [ ] **Step 2: Run Play Mode tests**

Expected: PASS (requires `CombatArena` in build settings).

- [ ] **Step 3: Run full Edit Mode regression**

Run all Edit Mode tests — Core combat tests must remain green (sim untouched).

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Tests.PlayMode/CombatArenaPlayModeTests.cs
git commit -m "test: add combat arena Play Mode smoke tests"
```

---

## Spec coverage checklist

| Spec requirement | Task |
|------------------|------|
| Additive 3D arena scene | Task 6, 7 |
| Direct mirror from build board | Task 8, 11 |
| Billboards from existing icons | Task 4, 8 |
| Smooth move lerp | Task 4 |
| Attack lunge + VFX | Task 4, 10 |
| Tactic pause full visual freeze | Task 9 |
| Build UI hidden during combat | Task 11 |
| Board replay bypassed | Task 11 |
| Save/resume reconstruct from log | Task 8 (`RestoreBeforeSegment`) |
| Buildings not rendered v1 | Task 8 (Combatant filter) |
| Core sim unchanged | No Core tasks |
| 3D models deferred | Out of scope |

---

## Out of scope (follow-up)

- `PieceDefinitionSO.combatModel` prefab swap
- Building decals / 3D props
- Ground trench art pass (placeholder plane OK for v1)
- Camera drift / pause zoom
- Fog-of-war intro

---

## Execution handoff

Plan complete and saved to `docs/superpowers/plans/2026-06-10-deadmanzone-combat-arena-presentation.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** — dispatch a fresh subagent per task, review between tasks, fast iteration  
2. **Inline Execution** — implement tasks in this session with checkpoints

Which approach do you want?
