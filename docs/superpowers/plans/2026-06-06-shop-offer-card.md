# Shop Offer Card Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign shop offer cards as rounded squares with board-scale piece preview, name strip, price badge (top-left), lock icon (top-right), max-5 lane sizing, and piece-only drag ghosts.

**Architecture:** Extend `ShopLayoutMetrics` for the new card geometry and 5-across lane budget. Rebuild the offer prefab in `RunSceneSetup`. `ShopOfferView` owns bind + preview hide/show; `ShopOfferDragSource` notifies the view on drag begin/end. `DragGhost` accepts live board cell size and a `pieceOnly` mode (no card background/label). No Core/shop logic changes.

**Tech Stack:** Unity uGUI/TMP, existing `ShopView`/`ShopPiecePreview`/`DragDropController`, PlayMode tests in `Assets/_Project/Tests.PlayMode`.

**Spec:** `docs/superpowers/specs/2026-06-06-shop-offer-card-design.md`

---

## File map

| File | Action |
|------|--------|
| `Presentation/Shop/ShopLayoutMetrics.cs` | Modify — max-slot sizing, name strip height, remove lock row |
| `Presentation/Shop/ShopOfferView.cs` | Modify — new refs, layout, preview visibility, lock icon state |
| `Presentation/Shop/ShopView.cs` | Modify — lane-width-aware card sizing |
| `Presentation/DragDrop/DragGhost.cs` | Modify — dynamic cell size, piece-only mode, cell sprites |
| `Presentation/DragDrop/DragDropController.cs` | Modify — pass cell metrics to ghost; optional drag callbacks |
| `Presentation/DragDrop/ShopOfferDragSource.cs` | Modify — notify `ShopOfferView` on drag begin/end |
| `Presentation/Editor/RunSceneSetup.cs` | Modify — rebuild `OfferCard` prefab hierarchy |
| `Tests.PlayMode/ShopViewPlayModeTests.cs` | Modify — card count assertions remain |
| `Tests.PlayMode/ShopOfferDragPlayModeTests.cs` | Create — preview hide/restore on drag |
| `Scenes/Run.unity` | Refresh via **DeadManZone → Refresh Run Scene** after prefab rebuild |

---

## Task 1: Shop layout metrics (5-across budget)

**Files:**
- Modify: `Assets/_Project/Presentation/Shop/ShopLayoutMetrics.cs`

- [ ] **Step 1: Replace `OfferCardSize` with lane-aware sizing**

```csharp
public const int MaxOffersPerLane = 5;
public const float NameStripHeight = 22f;
public const float CardPadding = 8f;
public const float LaneSpacing = 8f; // match HorizontalLayoutGroup.spacing

public static float IconAreaSize(float cellSize, float spacing) =>
    ViewportCells * cellSize + (ViewportCells - 1) * spacing;

public static Vector2 OfferCardSize(float cellSize, float spacing, float laneInnerWidth)
{
    var (cell, gap) = Resolve(cellSize, new Vector2(spacing, spacing));
    float icon = IconAreaSize(cell, gap);

    float maxSlotWidth = laneInnerWidth > 1f
        ? (laneInnerWidth - LaneSpacing * (MaxOffersPerLane - 1)) / MaxOffersPerLane
        : icon + CardPadding;

    float square = Mathf.Min(icon, maxSlotWidth - CardPadding);
    float height = square + NameStripHeight + CardPadding;
    return new Vector2(square + CardPadding, height);
}
```

Remove old `infoHeight` + separate `lockHeight` (lock is overlay inside square).

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Presentation/Shop/ShopLayoutMetrics.cs
git commit -m "feat(shop): lane-aware offer card sizing for max 5 slots"
```

---

## Task 2: ShopOfferView layout API

**Files:**
- Modify: `Assets/_Project/Presentation/Shop/ShopOfferView.cs`

- [ ] **Step 1: Add serialized refs and preview visibility**

New fields (wire in Task 4 prefab):

```csharp
[SerializeField] private RectTransform squareRoot;      // rounded square body
[SerializeField] private RectTransform previewRoot;     // parent of ShopPiecePreview blocks
[SerializeField] private RectTransform nameStripRoot;
[SerializeField] private Image priceBadgeBackground;
[SerializeField] private TMP_Text priceBadgeText;         // was priceText
[SerializeField] private Button lockIconButton;          // was lockButton
[SerializeField] private Image lockIconImage;            // optional swap sprite/color
```

Add public methods:

```csharp
public void SetPreviewVisible(bool visible)
{
    if (previewRoot != null)
        previewRoot.gameObject.SetActive(visible);
}

public void ConfigureLayout(float cellSize, float spacing, float laneInnerWidth)
{
    var cardSize = ShopLayoutMetrics.OfferCardSize(cellSize, spacing, laneInnerWidth);
    float square = cardSize.y - ShopLayoutMetrics.NameStripHeight - ShopLayoutMetrics.CardPadding;
    // set LayoutElement on card, squareRoot size = square x square
}
```

- [ ] **Step 2: Update `Bind`**

- Price → `priceBadgeText` (top-left badge; position set in prefab).
- Name → `pieceIdText` on name strip only.
- Lock: toggle `lockIconImage` color or child text `"🔒"` / `"🔓"`; keep `lockedIndicator` full-card tint overlay.
- Remove bottom lock text button label `"Lock"` / `"Unlock"`.

- [ ] **Step 3: Lock button must not start drag**

Ensure `lockIconButton` is a separate raycast target; drag source stays on card root / square (not on lock button). If lock is child of card with drag handler, add `lockIconButton` check in `ShopOfferDragSource` or put drag on `squareRoot` only.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Presentation/Shop/ShopOfferView.cs
git commit -m "feat(shop): offer view layout hooks for rounded square card"
```

---

## Task 3: ShopView lane-width sizing

**Files:**
- Modify: `Assets/_Project/Presentation/Shop/ShopView.cs`

- [ ] **Step 1: Pass lane inner width into `RebuildLane`**

```csharp
private void RebuildLane(Transform laneRoot, ShopState state, ShopLane lane, float cellSize, float spacing)
{
    if (laneRoot == null || offerCardPrefab == null)
        return;

    float laneWidth = 800f;
    var laneRect = laneRoot as RectTransform;
    if (laneRect != null)
    {
        Canvas.ForceUpdateCanvases();
        laneWidth = laneRect.rect.width;
    }

    ClearChildren(laneRoot);
    foreach (var offer in state.Offers.Where(o => o.Lane == lane).OrderBy(o => o.SlotIndex))
    {
        var cardObject = Instantiate(offerCardPrefab, laneRoot);
        cardObject.SetActive(true);
        var card = cardObject.GetComponent<ShopOfferView>() ?? cardObject.AddComponent<ShopOfferView>();
        bool isLocked = RunManager.Instance is { HasActiveRun: true } manager &&
            manager.Orchestrator.IsOfferLocked(offer);
        card.Bind(offer, isLocked, cellSize, spacing, laneWidth);
        card.LockToggled += OnLockToggled;
    }
}
```

Update `Bind` signature to accept `laneInnerWidth` and forward to `ConfigureLayout`.

- [ ] **Step 2: Commit**

```bash
git add Assets/_Project/Presentation/Shop/ShopView.cs
git commit -m "feat(shop): size offer cards for max 5 slots in lane width"
```

---

## Task 4: Rebuild offer card prefab (RunSceneSetup)

**Files:**
- Modify: `Assets/_Project/Presentation/Editor/RunSceneSetup.cs`

- [ ] **Step 1: Replace `CreateOfferCardPrefab` hierarchy**

Structure:

```
OfferCard (ShopOfferView + ShopOfferDragSource + Image rounded via theme)
├── LockedOverlay (stretch, disabled by default)
├── SquareRoot (LayoutElement — square)
│   ├── PreviewRoot (ShopPiecePreview + Blocks child)
│   ├── PriceBadge (top-left anchored Image + TMP)
│   └── LockIconButton (top-right, small Image/Button)
└── NameStrip (bottom bar Image + TMP name)
```

Use `UiThemeApplicator.ApplyCard` on card root. Name strip: dark semi-transparent bar. Price badge: small pill anchored `(0,1)-(0,1)` pivot top-left with padding.

- [ ] **Step 2: Wire SerializedObject fields** on `ShopOfferView` to new refs.

- [ ] **Step 3: Refresh Run scene**

In Unity: **DeadManZone → Refresh Run Scene**

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Presentation/Editor/RunSceneSetup.cs Assets/_Project/Scenes/Run.unity
git commit -m "feat(shop): rebuild offer card prefab as rounded square"
```

---

## Task 5: DragGhost board cell size + piece-only

**Files:**
- Modify: `Assets/_Project/Presentation/DragDrop/DragGhost.cs`

- [ ] **Step 1: Extend `Create` signature**

```csharp
public static DragGhost Create(
    Transform parent,
    string pieceId,
    PieceDefinition definition = null,
    PieceRotation rotation = PieceRotation.R0,
    float cellSize = 36f,
    float cellSpacing = 3f,
    bool pieceOnly = false)
```

- [ ] **Step 2: Use `cellSize` / `cellSpacing` instead of `CellPixels` constant**

Match block rendering to `ShopPiecePreview` (use cell sprites from `PieceVisualLookup` when available).

- [ ] **Step 3: When `pieceOnly == true`**

- Hide or destroy `background` and `label` GameObjects.
- Ghost size = footprint only (minimal padding ~4px).

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Presentation/DragDrop/DragGhost.cs
git commit -m "feat(drag): ghost uses board cell size and piece-only mode"
```

---

## Task 6: Drag lifecycle — hide card preview

**Files:**
- Modify: `Assets/_Project/Presentation/DragDrop/DragDropController.cs`
- Modify: `Assets/_Project/Presentation/DragDrop/ShopOfferDragSource.cs`

- [ ] **Step 1: Pass metrics into `BeginDrag`**

Add optional parameters or read from static `BoardView`/`ShopView` resolver:

```csharp
public void BeginDrag(DragPayload payload, Transform returnParent, PointerEventData eventData,
    float cellSize = 36f, float cellSpacing = 3f, bool pieceOnlyGhost = false)
{
    // ...
    _ghost = DragGhost.Create(canvasTransform, pieceId, payload.Definition, payload.Rotation,
        cellSize, cellSpacing, pieceOnlyGhost);
}
```

- [ ] **Step 2: `ShopOfferDragSource`**

```csharp
public void OnBeginDrag(PointerEventData eventData)
{
    if (_offer == null || DragDropController.Instance == null)
        return;
    var view = GetComponent<ShopOfferView>();
    view?.SetPreviewVisible(false);

    // resolve cellSize from ShopView/BoardView via FindFirstObjectByType<BoardView>()
    DragDropController.Instance.BeginDrag(payload, transform, eventData, cellSize, cellSpacing, pieceOnlyGhost: true);
}

public void OnEndDrag(PointerEventData eventData)
{
    DragDropController.Instance?.EndDrag(eventData);
    GetComponent<ShopOfferView>()?.SetPreviewVisible(true);
}
```

Ensure `EndDrag` always restores preview even on successful drop (card may be destroyed — null-check view).

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Presentation/DragDrop/DragDropController.cs Assets/_Project/Presentation/DragDrop/ShopOfferDragSource.cs
git commit -m "feat(shop): hide offer preview while dragging piece ghost"
```

---

## Task 7: PlayMode tests

**Files:**
- Modify: `Assets/_Project/Tests.PlayMode/ShopViewPlayModeTests.cs`
- Create: `Assets/_Project/Tests.PlayMode/ShopOfferDragPlayModeTests.cs`

- [ ] **Step 1: Update existing shop render test** if `Bind` signature changed (pass lane width `600f`).

- [ ] **Step 2: Add drag preview test**

```csharp
[UnityTest]
public IEnumerator BeginDrag_HidesPreview_EndDrag_Restores()
{
    // Setup minimal ShopOfferView + ShopOfferDragSource + mock offer on canvas
    var view = /* ... */;
    view.Bind(offer, false, 48f, 3f, 600f);
    yield return null;
    Assert.IsTrue(view.PreviewRootActive); // expose read-only for test or check child active

    view.SetPreviewVisible(false);
    Assert.IsFalse(view.PreviewRootActive);

    view.SetPreviewVisible(true);
    Assert.IsTrue(view.PreviewRootActive);
    yield return null;
}
```

Prefer testing `SetPreviewVisible` directly if simulating full drag is heavy.

- [ ] **Step 3: Run PlayMode tests**

Unity Test Runner → PlayMode → run `ShopViewPlayModeTests` and `ShopOfferDragPlayModeTests`.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Tests.PlayMode/ShopViewPlayModeTests.cs Assets/_Project/Tests.PlayMode/ShopOfferDragPlayModeTests.cs
git commit -m "test(shop): offer card layout and preview visibility"
```

---

## Task 8: Manual verification

- [ ] **Step 1: Enter Play Mode on Run scene**

Checklist:
- [ ] 3 offers per lane, evenly spaced, same card size as when 4th slot modifier active
- [ ] Rounded square, name on bottom strip, price top-left, lock top-right
- [ ] Lock toggles icon + card tint; locked offer survives reroll
- [ ] Drag from shop: preview disappears on card; ghost shows piece shape at board scale
- [ ] Cancel drag: preview returns
- [ ] Successful buy: offer removed

- [ ] **Step 2: Final commit if any polish fixes**

---

## Spec coverage checklist

| Spec requirement | Task |
|------------------|------|
| Rounded square card | Task 4 |
| Centered board-scale preview | Task 2, 4 (existing ShopPiecePreview) |
| Name strip bottom | Task 4 |
| Price top-left | Task 4 |
| Lock top-right + tint when locked | Task 2, 4 |
| Prefab per offer spawn | Task 3 (unchanged pattern) |
| Fixed size for 5, dynamic spacing | Task 1, 3 |
| Drag hides preview, piece ghost | Task 5, 6 |
| Ghost uses board cell size | Task 5, 6 |

---

## Plan self-review

- All spec sections mapped to tasks.
- No TBD placeholders.
- `Bind` signature change propagated to tests and ShopView in Tasks 3 and 7.
- Core shop logic untouched (presentation only).
