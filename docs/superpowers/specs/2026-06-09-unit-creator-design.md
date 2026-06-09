# DeadManZone Unit Creator — Design Spec

**Date:** 2026-06-09  
**Engine:** Unity (C# / ScriptableObjects)  
**Status:** Approved (pending user spec review)  
**Scope:** Editor window and in-game dev panel for creating, editing, and duplicating units (`PieceDefinitionSO`) with stats, tags, art, and configurable ContentDatabase registration.

---

## Problem

Units are authored today through code factories (`DemoContentGenerator`, `DemoPieceFactory`) or by hand-editing scattered ScriptableObject assets. Shop lane is manually assigned per piece and duplicated in factory calls. Shop pool membership is a hardcoded `DemoShopPieceIds` hash set in `ContentDatabase.cs`. There is no unified form to plug in stats, assign art, pick tags, and click **Create** to add a unit to the game.

## Goal

Deliver a **Unit Creator** tool with:

1. **Unity Editor window** — full authoring workflow for production content
2. **In-game dev panel** — session-only prototyping with optional **Save to Project**
3. **Create / Edit / Duplicate** flows against existing `PieceDefinitionSO` assets
4. **Auto-derived shop lane** from combat role tags
5. **Configurable registration** — checkboxes for ContentDatabase and shop pool inclusion

## Non-Goals (v1)

- Specialty lane rule implementation (stub + validator info only; rules defined later)
- Runtime 5×5 shape grid editor (presets only in Play mode)
- Faction creation or enemy template authoring
- Blender / art pipeline automation beyond sprite assignment helpers
- Player-facing modding UI (dev tools only)

---

## Architecture

### Overview

```
UnitCreationDraft (shared form state)
        │
        ├── UnitCreationValidator
        ├── ShopLaneResolver (combat role → lane)
        └── TagPickerCatalog (from TagRegistry)

Editor shell                          Runtime shell
UnitCreatorWindow                     UnitCreatorRuntimePanel
        │                                     │
        └── UnitPersistenceService            ├── SessionContentOverlay
            (Editor only)                     └── SaveToProjectBridge (Editor API)
                    │                                     │
                    ▼                                     ▼
            PieceDefinitionSO.asset              MergedContentRegistry
            ContentDatabase.asset                (DB + session prototypes)
```

### Shared core (`Assets/_Project/Core/` or `Assets/_Project/Data/`)

**`UnitCreationDraft`** — Plain C# class mirroring `PieceDefinitionSO` fields:

- Identity: `id`, `displayName`, `factionId`, `category`
- Shape: `Vector2Int[] shapeCells`
- Tags: `primary`, `combatRole`, `systemTag`, `synergyTags[]`, `abilityTags[]`
- Stats: HP, damage, cooldown, gold/requisition/manpower, muster, combat tiers, armor/attack type, granted ability, shop/command flags
- Visuals: `icon`, `categoryTint`, `cellSprites[]`
- Registration intent: `addToContentDatabase`, `includeInShopPool`
- Computed (not user-edited): `shopLane` from `ShopLaneResolver`

**`UnitCreationValidator`** — Returns errors/warnings:

- `id` required, snake_case, unique (unless editing same asset)
- Tags validated against `TagRegistry`
- Primary tag required; unknown tags warn
- Shape must have ≥1 cell, anchor at origin
- Shop lane info when role maps to Specialty (rules pending)

**`ShopLaneResolver`** — Single source of truth for lane assignment:

| Combat role | Shop lane |
|-------------|-----------|
| `support`, `utility`, `headquarters` | **Defensive** |
| `defender` *(future tag)* | **Defensive** |
| `assault`, `sniper`, `tank` | **Offensive** |
| `artillery` | **Specialty** *(interim until specialty rules catalog)* |

- Empty or unknown combat role → **Offensive** with validator warning
- When `defender` is added to `GameTagIds` / `TagRegistry`, add it to the Defensive mapping (no other changes required)
- **`SpecialtyLaneRuleCatalog`** — empty stub with `TryResolveSpecialty(...)` hook for future rules (requisition cost, synergy tags, hybrid category, etc.)

**`TagPickerCatalog`** — Exposes `TagRegistry` entries grouped by `TagCategory` for UI dropdowns/checklists.

### Data model changes

**`PieceDefinitionSO`** — Add:

```csharp
public bool includeInShopPool = true;
```

**`ContentDatabase.BuildRegistry()`** — Replace `DemoShopPieceIds.Contains(piece.id)` with `piece.includeInShopPool`.

**Migration:**

- One-time editor menu: `DeadManZone/Migrate Shop Pool Flags From Demo List` — sets `includeInShopPool = true` for ids currently in `DemoShopPieceIds`, `false` otherwise
- Optional: `DeadManZone/Migrate Shop Lanes From Combat Roles` — rewrites `shopLane` on all pieces via `ShopLaneResolver`
- Remove or deprecate static `DemoShopPieceIds` after migration (keep as migration source only until complete)

`shopLane` remains serialized on the asset (written automatically on save) so `ContentRegistry.Register(piece, lane, ...)` continues unchanged.

### Editor persistence (`Assets/_Project/Data/Editor/`)

**`UnitPersistenceService`**

1. Create or update `Assets/_Project/Data/Resources/DeadManZone/Pieces/{id}.asset`
2. Apply all draft fields; rebuild legacy `tags` via `PieceTagQueries.BuildLegacyTags`
3. Set `shopLane` from `ShopLaneResolver`
4. Set `includeInShopPool` from draft checkbox
5. If `addToContentDatabase`: append or update reference in `ContentDatabase.pieces` array (no duplicates by id)
6. `EditorUtility.SetDirty`, `AssetDatabase.SaveAssets`
7. Log success with clickable asset ping

**Delete (edit mode):** Remove from ContentDatabase array, delete asset file (confirmation dialog).

### Session overlay (Play mode)

**`SessionContentOverlay`** — Singleton holding in-memory `PieceDefinition` prototypes keyed by id.

**`MergedContentRegistryProvider`** — Builds base registry from `ContentDatabase`, then overlays session prototypes (session wins on id collision). Used by shop/board/reserves when overlay is non-empty in dev builds.

**`SaveToProjectBridge`** — `#if UNITY_EDITOR` wrapper calling `UnitPersistenceService` from Play mode after confirmation.

---

## Editor Window UX

**Menu:** `DeadManZone → Unit Creator`  
**Window:** `UnitCreatorWindow` — scrollable IMGUI form, min size ~420×600 (pattern matches Visual Studio window).

### Header

- Mode: **New** | **Edit** (dropdown of pieces from ContentDatabase)
- **Duplicate** — clones selected unit, appends `_copy` to id/display name, switches to New mode
- **Reset** — clears form
- Asset path preview: `Assets/_Project/Data/Resources/DeadManZone/Pieces/{id}.asset`

### Form sections (collapsible)

**Identity** — id, displayName, factionId (dropdown), category (Unit/Building/Hybrid)

**Shape**

- Preset buttons: 1×1, 1×2, 2×1, 2×2, L, T
- 5×5 grid editor with anchor at center-bottom; toggle cells; live cell count
- Read-only shape cell list

**Tags**

- Primary — single dropdown (Primary category)
- Combat Role — single dropdown (optional)
- System Tag — single dropdown (optional)
- Synergy Tags — multi-select checklist
- Ability Tags — multi-select with suggestions
- Tooltips from `TagDefinition.Tooltip`

**Stats** — HP, damage, cooldown, costs, muster, combat tier enums, armor/attack type, granted ability, shop/command flag enums

**Visuals**

- Icon sprite field + Browse (`PieceArtPaths.NeutralIcons`)
- categoryTint (auto-suggested from faction on change)
- Per-cell sprite rows keyed by shape offset
- **Auto-assign from renders** using `PieceArtPaths.CellAssetPath` convention

**Registration**

- ☑ Add to ContentDatabase (default: on for new units)
- ☑ Include in shop pool (default: on for Unit category, off for HQ/buildings)
- **Computed shop lane** — read-only, live-updated from combat role via `ShopLaneResolver`

### Footer

- **Validate** — summary of errors/warnings
- **Create Unit** / **Save Changes** — primary action
- **Delete** — edit mode only, with confirmation

---

## In-Game Runtime Panel

**Access:** Dev-only — toggle **F9** in Play mode or `[DEV]` button on Run scene debug HUD.

**UI:** Compact right-docked panel — subset of editor form:

- Identity, combat role + tags, core stats, shape presets (no grid in v1)
- Optional icon sprite
- Registration checkboxes (for Save to Project)
- Read-only computed shop lane

### Prototype mode (default)

- **Spawn Prototype** — validates, registers in `SessionContentOverlay`
- Visible in shop/reserves/combat for current session only
- Prototype list with **Remove** per entry
- Cleared on Play mode exit

### Save to Project

- Secondary button, confirmation dialog
- Calls `UnitPersistenceService` via `SaveToProjectBridge`
- Disabled on validation failure or id collision (unless editing)
- After save: prompt to keep session copy or remove (now using disk asset)

### Edit / Duplicate (runtime)

- Dropdown: session prototypes + saved ContentDatabase pieces
- Duplicate → new session-only copy until Save to Project

---

## Validation, Error Handling & Testing

### Validation rules

| Rule | Severity |
|------|----------|
| Empty or invalid `id` | Error |
| Duplicate `id` on create | Error |
| Missing primary tag | Error |
| Unknown tag id | Warning |
| Empty shape | Error |
| Specialty lane (artillery) with pending rules | Info |
| Unknown combat role for lane resolve | Warning (fallback Offensive) |
| Add to DB checked but piece already registered | Info (update in place) |

### Error handling

- **Create/Save** blocked when any Error exists; Warnings/Info shown but allow save
- Persistence failures (IO, null ContentDatabase) log error, no partial DB write (asset write is transactional: validate → write asset → update DB → save)
- Session overlay: duplicate prototype id replaces previous entry with warning
- Save to Project in build without editor: button hidden / no-op

### Tests (Edit Mode)

**`ShopLaneResolverTests`**

- Each combat role maps to expected lane
- `tank` → Offensive
- Unknown role → Offensive + fallback behavior
- Placeholder test for future `defender` → Defensive (ignored until tag exists)

**`UnitCreationValidatorTests`**

- Valid draft passes
- Duplicate id fails on create
- Missing primary fails

**`UnitPersistenceServiceTests`** *(Editor tests)*

- Creates asset at expected path
- Appends to ContentDatabase when flagged
- Sets `includeInShopPool` and derived `shopLane`

**`ContentDatabaseTests`** *(update existing)*

- `BuildRegistry` respects `includeInShopPool` field instead of static hash set

### Manual test plan

1. Open Unit Creator → create 1×1 assault infantry → verify Offensive lane, appears in shop when flagged
2. Create support medic → Defensive lane
3. Duplicate rifle_squad → edit stats → Save Changes
4. Play mode → Spawn Prototype → verify shop offer → exit Play → prototype gone
5. Play mode → Spawn → Save to Project → verify asset on disk and ContentDatabase entry
6. Run migrate menu → confirm existing demo roster shop flags preserved

---

## File Layout (implementation)

```
Assets/_Project/Core/Shop/ShopLaneResolver.cs
Assets/_Project/Core/Shop/SpecialtyLaneRuleCatalog.cs (stub)
Assets/_Project/Data/UnitCreation/UnitCreationDraft.cs
Assets/_Project/Data/UnitCreation/UnitCreationValidator.cs
Assets/_Project/Data/UnitCreation/TagPickerCatalog.cs
Assets/_Project/Data/Editor/UnitCreatorWindow.cs
Assets/_Project/Data/Editor/UnitPersistenceService.cs
Assets/_Project/Data/Editor/UnitCreatorShapeGridDrawer.cs
Assets/_Project/Data/Editor/ShopPoolMigration.cs
Assets/_Project/Game/Dev/SessionContentOverlay.cs
Assets/_Project/Game/Dev/MergedContentRegistryProvider.cs
Assets/_Project/Presentation/Dev/UnitCreatorRuntimePanel.cs
Assets/_Project/Core.Tests/EditMode/ShopLaneResolverTests.cs
Assets/_Project/Core.Tests/EditMode/UnitCreationValidatorTests.cs
```

---

## Open Items (post-v1)

- Add `defender` to `GameTagIds` / `TagRegistry` → Defensive lane in `ShopLaneResolver`
- Implement `SpecialtyLaneRuleCatalog` rules (requisition, synergy, hybrid)
- Runtime shape grid editor if play-mode authoring needs multi-cell buildings
- Batch import from spreadsheet (optional, separate tool)

---

## Approval Summary

| Decision | Choice |
|----------|--------|
| Tool location | Editor window + in-game dev panel |
| Registration | Configurable checkboxes (ContentDatabase + shop pool) |
| Shape | Presets + grid editor (editor); presets only (runtime) |
| In-game persistence | Session prototype default; optional Save to Project |
| CRUD | Create + edit + duplicate |
| Shop lane | Auto from combat role; `tank` → Offensive; `defender` → Defensive when added |
| Specialty lane | Stub + info validator; full rules later |
