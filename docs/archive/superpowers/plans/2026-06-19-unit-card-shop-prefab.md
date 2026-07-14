> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# Unit Card Prefab, Shop Card Asset & Footprint Input Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix empty center unit card and single-cell-only multi-piece hover/drag by introducing authorable prefabs (`UnitDetailCard`, `ShopOfferCard`) with `PieceCardView` binding and full-footprint pointer input.

**Architecture:** Extract bind logic from `PieceHoverCard` into `PieceCardView`; `UnitCardPanelView` owns one prefab instance (hidden when idle). Replace tile-anchor `BoardPieceDragSource` with `BoardPieceFootprintHit` on shape overlay. Save shop offer hierarchy as Project prefab; `RunSceneSetup` loads assets via path constants. Add hover depth lock on `PieceHoverCardController` to prevent flicker.

**Tech Stack:** Unity 6 (6000.3.8f1), C# Presentation layer, NUnit EditMode + PlayMode

**Spec:** `docs/superpowers/specs/2026-06-19-unit-card-shop-prefab-design.md`

**Branch:** `unit-card-shop-prefab` (from `master` or current mainline)

---

## File map

| File | Action |
|------|--------|
| `Assets/_Project/Presentation/UI/PieceCardView.cs` | **Create** — bind/show/hide |
| `Assets/_Project/Presentation/UI/PieceCardBindHelper.cs` | **Create** — shared overflow tooltip + chip logic (optional extract) |
| `Assets/_Project/Presentation/UI/Prefabs/UnitDetailCard.prefab` | **Create** (Editor menu) |
| `Assets/_Project/Presentation/UI/Prefabs/ShopOfferCard.prefab` | **Create** (Editor menu) |
| `Assets/_Project/Presentation/UI/CardPrefabPaths.cs` | **Create** — const asset paths |
| `Assets/_Project/Presentation/Editor/CardPrefabAuthoring.cs` | **Create** — menu to bake prefabs |
| `Assets/_Project/Presentation/Run/UnitCardPanelView.cs` | Modify — `PieceCardView` |
| `Assets/_Project/Presentation/Board/PieceHoverCardController.cs` | Modify — hover lock API |
| `Assets/_Project/Presentation/Board/PieceHoverCard.cs` | Modify — delegate bind to `PieceCardView` or thin wrapper |
| `Assets/_Project/Presentation/Board/BoardPieceFootprintHit.cs` | **Create** |
| `Assets/_Project/Presentation/Board/BoardView.cs` | Footprint hit; remove tile drag |
| `Assets/_Project/Presentation/Board/PieceShapeVisual.cs` | Add hit target helper |
| `Assets/_Project/Presentation/Editor/RunSceneSetup.cs` | Load prefab assets |
| `Assets/_Project/Presentation.Tests/EditMode/PieceCardViewTests.cs` | **Create** |
| `Assets/_Project/Presentation.Tests/EditMode/PieceHoverLockTests.cs` | **Create** |
| `Assets/_Project/Presentation.Tests/EditMode/BoardFootprintLookupTests.cs` | **Create** |

**Unity test commands:**

```powershell
# EditMode filtered
& "C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform editmode `
  -testFilter "DeadManZone.Presentation.Tests.EditMode.PieceCardViewTests" `
  -testResults "TestResults-EditMode.xml" -logFile "UnityTest.log" -quit

# PlayMode shop regression
& "C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform playmode `
  -testFilter "DeadManZone.Tests.PlayMode.ShopViewPlayModeTests|DeadManZone.Tests.PlayMode.ShopOfferDragPlayModeTests" `
  -testResults "TestResults-PlayMode.xml" -logFile "UnityTest.log" -quit
```

---

### Task 0: Branch setup

- [ ] **Step 1:** Create branch

```powershell
cd "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone"
git checkout master
git pull
git checkout -b unit-card-shop-prefab
```

---

### Task 1: Hover lock (pure C# helper)

**Files:**
- Create: `Assets/_Project/Presentation/Board/PieceHoverLock.cs`
- Create: `Assets/_Project/Presentation.Tests/EditMode/PieceHoverLockTests.cs`
- Modify: `Assets/_Project/Presentation/Board/PieceHoverCardController.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// PieceHoverLockTests.cs
[Test]
public void Enter_IncrementsDepthForSameInstance()
{
    var lockState = new PieceHoverLock();
    lockState.Enter("a");
    lockState.Enter("a");
    Assert.IsTrue(lockState.ShouldShow("a"));
    lockState.Exit("a");
    Assert.IsTrue(lockState.ShouldShow("a"));
    lockState.Exit("a");
    Assert.IsFalse(lockState.ShouldShow("a"));
}

[Test]
public void Exit_WrongInstance_DoesNotClearActive()
{
    var lockState = new PieceHoverLock();
    lockState.Enter("a");
    lockState.Exit("b");
    Assert.IsTrue(lockState.ShouldShow("a"));
}
```

- [ ] **Step 2: Run tests — expect FAIL**

Filter: `DeadManZone.Presentation.Tests.EditMode.PieceHoverLockTests`

- [ ] **Step 3: Implement `PieceHoverLock`**

```csharp
namespace DeadManZone.Presentation.Board
{
    public sealed class PieceHoverLock
    {
        private string _activeInstanceId;
        private int _depth;

        public void Enter(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return;
            if (_activeInstanceId == instanceId) { _depth++; return; }
            _activeInstanceId = instanceId;
            _depth = 1;
        }

        public void Exit(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId) || _activeInstanceId != instanceId) return;
            _depth = Math.Max(0, _depth - 1);
            if (_depth == 0) _activeInstanceId = null;
        }

        public bool ShouldShow(string instanceId) =>
            !string.IsNullOrEmpty(instanceId) && _activeInstanceId == instanceId && _depth > 0;

        public void Clear() { _activeInstanceId = null; _depth = 0; }
    }
}
```

- [ ] **Step 4: Wire into `PieceHoverCardController`**

Add field `PieceHoverLock _hoverLock = new();`

```csharp
public void NotifyPieceHoverEnter(string instanceId, PieceDefinition def, PieceCardBuildContext ctx)
{
    _hoverLock.Enter(instanceId);
    Show(def, Vector2.zero, ctx); // fixed panel ignores screen pos
}

public void NotifyPieceHoverExit(string instanceId)
{
    _hoverLock.Exit(instanceId);
    if (!_hoverLock.ShouldShow(instanceId))
        Hide();
}
```

Keep existing `Show`/`Hide` public for backward compat; footprint hit calls `Notify*` methods.

- [ ] **Step 5: Run tests — PASS**

- [ ] **Step 6: Commit**

```powershell
git add Assets/_Project/Presentation/Board/PieceHoverLock.cs Assets/_Project/Presentation/Board/PieceHoverCardController.cs Assets/_Project/Presentation.Tests/EditMode/PieceHoverLockTests.cs
git commit -m "feat(ui): add piece hover lock to prevent card flicker"
```

---

### Task 2: `PieceCardView` binder

**Files:**
- Create: `Assets/_Project/Presentation/UI/PieceCardView.cs`
- Create: `Assets/_Project/Presentation.Tests/EditMode/PieceCardViewTests.cs`
- Modify: `Assets/_Project/Presentation/Board/PieceHoverCard.cs` (optional delegate)

- [ ] **Step 1: Write failing bind test**

Create minimal test hierarchy in test (no prefab required for first test):

```csharp
[Test]
public void Bind_SetsDisplayNameAndHp()
{
    var go = new GameObject("Card", typeof(RectTransform), typeof(PieceCardView));
    var view = go.GetComponent<PieceCardView>();
    // use reflection or test-only Init(nameText, hpText) — prefer [SerializeField] + test fixture method:
    view.InitializeForTests(nameText: CreateTmp(go.transform, "Name"), hpText: CreateTmp(go.transform, "Hp"));

    var model = PieceCardViewModelBuilder.Build(TestPieces.RifleSquad());
    view.Bind(model, overflowTooltip: string.Empty);

    Assert.AreEqual(model.DisplayName, view.NameTextForTests);
    StringAssert.Contains(model.Hp.ToString(), view.HpTextForTests);
}
```

Add `InitializeForTests` / test accessors on `PieceCardView` (internal or `#if UNITY_INCLUDE_TESTS`).

- [ ] **Step 2: Run — FAIL**

- [ ] **Step 3: Implement `PieceCardView`**

Port bind sections from `PieceHoverCard.Bind`, `ApplyTheme`, chip logic, synergy/salvage visibility. Serialized fields:

```csharp
[SerializeField] private TMP_Text nameText;
[SerializeField] private TMP_Text hpText;
[SerializeField] private TMP_Text damageText;
// ... mirror PieceHoverCard fields
[SerializeField] private RectTransform tagChipContainer;
[SerializeField] private TMP_Text tagChipTemplate;
[SerializeField] private Image background;
[SerializeField] private UiThemeSO theme;
```

```csharp
public void Bind(PieceCardViewModel model, string overflowTooltip) { /* port from PieceHoverCard */ }
public void Show() { gameObject.SetActive(true); }
public void Hide() { gameObject.SetActive(false); }
```

- [ ] **Step 4: Run `PieceCardViewTests` — PASS**

- [ ] **Step 5: Commit**

```powershell
git commit -m "feat(ui): add PieceCardView binder for unit detail cards"
```

---

### Task 3: Editor prefab authoring + asset paths

**Files:**
- Create: `Assets/_Project/Presentation/UI/CardPrefabPaths.cs`
- Create: `Assets/_Project/Presentation/Editor/CardPrefabAuthoring.cs`

- [ ] **Step 1: Add path constants**

```csharp
namespace DeadManZone.Presentation.UI
{
    public static class CardPrefabPaths
    {
        public const string UnitDetailCard = "Assets/_Project/Presentation/UI/Prefabs/UnitDetailCard.prefab";
        public const string ShopOfferCard = "Assets/_Project/Presentation/UI/Prefabs/ShopOfferCard.prefab";
    }
}
```

- [ ] **Step 2: Editor menu `DeadManZone/UI/Bake Card Prefabs`**

`CardPrefabAuthoring.cs`:
- Call existing `RunSceneSetup` private builders OR duplicate minimal hierarchy creation
- For **ShopOfferCard**: invoke logic equivalent to `CreateOfferCardPrefab(theme)` → `PrefabUtility.SaveAsPrefabAsset`
- For **UnitDetailCard**: build hierarchy with `PieceCardView` + TMP slots wired via SerializedObject
- Ensure folder `Assets/_Project/Presentation/UI/Prefabs/` exists
- Log paths on success

- [ ] **Step 3: Run menu in Unity Editor** (manual gate) — verify both `.prefab` files exist in Project

- [ ] **Step 4: Commit prefabs + scripts**

```powershell
git add Assets/_Project/Presentation/UI/Prefabs/ Assets/_Project/Presentation/UI/CardPrefabPaths.cs Assets/_Project/Presentation/Editor/CardPrefabAuthoring.cs
git commit -m "feat(ui): add UnitDetailCard and ShopOfferCard prefab assets"
```

---

### Task 4: Wire `UnitCardPanelView` to prefab

**Files:**
- Modify: `Assets/_Project/Presentation/Run/UnitCardPanelView.cs`
- Modify: `Assets/_Project/Presentation/Editor/RunSceneSetup.cs` (`CreateCenterColumnSection`)

- [ ] **Step 1: Update `UnitCardPanelView`**

Replace `[SerializeField] PieceHoverCard unitCard` with `[SerializeField] PieceCardView cardView`.

```csharp
public void Show(PieceDefinition definition, PieceCardBuildContext context = null)
{
    if (definition == null || cardView == null)
    {
        Debug.LogError("UnitCardPanelView: cardView prefab reference missing.");
        return;
    }
    var model = PieceCardViewModelBuilder.Build(definition, context);
    string overflow = PieceCardOverflowTooltip.Build(definition, model); // extract from controller helper
    cardView.Bind(model, overflow);
    cardView.Show();
    if (panelRoot != null) panelRoot.gameObject.SetActive(true);
}
```

Extract overflow helper to shared static (move from `PieceHoverCardController.BuildOverflowTooltip`).

- [ ] **Step 2: Update `CreateCenterColumnSection`**

Load prefab:

```csharp
var prefab = AssetDatabase.LoadAssetAt<GameObject>(CardPrefabPaths.UnitDetailCard);
var cardGo = prefab != null
    ? (GameObject)PrefabUtility.InstantiatePrefab(prefab, panelGo.transform)
    : /* fallback error log */;
Stretch(cardGo.GetComponent<RectTransform>());
panelSerialized.FindProperty("cardView").objectReferenceValue = cardGo.GetComponent<PieceCardView>();
```

- [ ] **Step 3: Play mode smoke** — hover unit, center card shows name/stats

- [ ] **Step 4: Commit**

```powershell
git commit -m "feat(ui): wire UnitCardPanelView to UnitDetailCard prefab"
```

---

### Task 5: Shop prefab asset wiring

**Files:**
- Modify: `Assets/_Project/Presentation/Editor/RunSceneSetup.cs` (`CreateShopSection`)
- Modify: `Assets/_Project/Presentation/Shop/ShopView.cs` (optional default load)

- [ ] **Step 1: Change `CreateShopSection`**

Replace procedural `CreateOfferCardPrefab(theme)` with:

```csharp
var offerPrefab = AssetDatabase.LoadAssetAt<GameObject>(CardPrefabPaths.ShopOfferCard);
if (offerPrefab == null)
{
    Debug.LogWarning("ShopOfferCard prefab missing — run DeadManZone/UI/Bake Card Prefabs");
    offerPrefab = CreateOfferCardPrefab(theme); // fallback
}
```

- [ ] **Step 2: Run PlayMode shop tests**

Filter: `ShopViewPlayModeTests|ShopOfferDragPlayModeTests`

- [ ] **Step 3: Commit**

```powershell
git commit -m "feat(ui): load ShopOfferCard from project prefab asset"
```

---

### Task 6: Footprint lookup helper + tests

**Files:**
- Create: `Assets/_Project/Presentation/Board/BoardFootprintLookup.cs`
- Create: `Assets/_Project/Presentation.Tests/EditMode/BoardFootprintLookupTests.cs`

- [ ] **Step 1: Failing test**

```csharp
[Test]
public void TryGetPieceAtCell_ReturnsMultiCellPieceForNonAnchorCell()
{
    var board = TestBoards.EmptyPlayerBoard();
    var def = TestPieces.With(TestPieces.CreateUnit("tank", primary: GameTagIds.Vehicle), shape: new[] { new GridCoord(0,0), new GridCoord(1,0), new GridCoord(0,1), new GridCoord(1,1) });
    board.TryPlace(def, new GridCoord(2, 3), "tank_1");
    Assert.IsTrue(BoardFootprintLookup.TryGetPieceAt(board, new GridCoord(3, 4), out var piece));
    Assert.AreEqual("tank_1", piece.InstanceId);
}
```

- [ ] **Step 2: Implement lookup** — scan `board.Pieces`, check `Shape.GetCells(anchor, rotation).Contains(cell)`

- [ ] **Step 3: Run tests — PASS**

- [ ] **Step 4: Commit**

---

### Task 7: `BoardPieceFootprintHit` + `BoardView` integration

**Files:**
- Create: `Assets/_Project/Presentation/Board/BoardPieceFootprintHit.cs`
- Modify: `Assets/_Project/Presentation/Board/BoardView.cs`
- Modify: `Assets/_Project/Presentation/Board/PieceShapeVisual.cs`

- [ ] **Step 1: Implement footprint hit**

Copy pointer/drag logic from `BoardPieceDragSource`; call `hoverController.NotifyPieceHoverEnter/Exit`.

On `PieceShapeVisual.Create` return, add to root:

```csharp
var hitImage = root.AddComponent<Image>();
hitImage.color = new Color(1f, 1f, 1f, 0f);
hitImage.raycastTarget = true;
var hit = root.AddComponent<BoardPieceFootprintHit>();
```

- [ ] **Step 2: `BoardView.RefreshOccupancyVisuals`**

Remove anchor-tile `BoardPieceDragSource` block (lines ~420-433).

After `CreateShapeVisual`, configure hit:

```csharp
if (_shapeVisualsByInstance.TryGetValue(piece.InstanceId, out var visual))
{
    var hit = visual.GetComponent<BoardPieceFootprintHit>();
    hit?.Configure(piece.InstanceId, piece.Definition, piece.Anchor, piece.Rotation, hoverController, this);
}
```

- [ ] **Step 3: Manual test** — 2×2 piece: hover non-anchor cell shows card; drag works

- [ ] **Step 4: Commit**

```powershell
git commit -m "feat(board): footprint hit target for multi-cell hover and drag"
```

---

### Task 8: Regression + cleanup

- [ ] **Step 1:** Run full Presentation EditMode + shop PlayMode tests
- [ ] **Step 2:** Deprecate procedural `PieceHoverCard.EnsureRuntimeUi` path for center panel (keep for floating card if still used)
- [ ] **Step 3:** Update `MechanicsSandboxChecklistTests` if it references `PieceHoverCard` directly
- [ ] **Step 4:** Commit any test fixes

```powershell
git commit -m "test(ui): regression fixes for card prefab and footprint input"
```

---

### Task 9: Manual verification gate

- [ ] Hover 1×1 unit → center card populated, hidden on exit
- [ ] Hover 2×2+ piece on non-anchor cell → same
- [ ] Drag from any footprint cell → drag ghost appears
- [ ] Edit `UnitDetailCard.prefab` font size → visible in Play mode
- [ ] Edit `ShopOfferCard.prefab` → shop offers reflect change
- [ ] No empty frame when idle

---

## Spec self-review

| Spec requirement | Task |
|------------------|------|
| UnitDetailCard prefab + PieceCardView | 2, 3, 4 |
| ShopOfferCard prefab asset | 3, 5 |
| Footprint hover/drag | 6, 7 |
| Hidden when idle | 4 |
| Hover flicker fix | 1 |
| EditMode tests | 1, 2, 6 |
| PlayMode shop tests | 5, 8 |

No placeholders. Prefab binary files created via Editor menu (Task 3) — documented manual step.

---

## Execution handoff

Plan complete and saved to `docs/superpowers/plans/2026-06-19-unit-card-shop-prefab.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** — fresh subagent per task, review between tasks  
2. **Inline Execution** — implement in this session with checkpoints  

Which approach?
