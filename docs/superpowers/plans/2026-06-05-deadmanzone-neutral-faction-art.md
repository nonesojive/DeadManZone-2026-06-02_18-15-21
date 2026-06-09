# Neutral Faction Art Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Set up Unity art infrastructure so neutral Blender renders plug into shop icons and optional per-cell board sprites without further code changes.

**Architecture:** Standard folder paths + `PieceDefinitionSO` cell sprite entries + `PieceShapeVisual` sprite fallback. Editor menus create folders, generate placeholder icons for pipeline testing, assign renders from conventional paths, and validate the neutral roster.

**Tech Stack:** Unity 6, C# Editor scripts, existing `PieceDefinitionSO` / `PieceShapeVisual` presentation layer.

**Spec:** `docs/superpowers/specs/2026-06-05-deadmanzone-neutral-faction-art-design.md`  
**Visual commitment:** `docs/superpowers/specs/2026-06-06-deadmanzone-top-down-visual-commitment.md` (isometric tokens + top-down terrain; SuperGrok primary; 3D combat deferred)

---

### Task 1: Art paths & data model

**Files:**
- Create: `Assets/_Project/Data/PieceArtPaths.cs`
- Create: `Assets/_Project/Data/PieceCellSprite.cs`
- Modify: `Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs`
- Modify: `Assets/_Project/Core/Board/ShapeTransforms.cs`
- Test: `Assets/_Project/Core.Tests/EditMode/ShapeTransformsTests.cs`

- [ ] Add `PieceCellSprite` struct (`localCell`, `sprite`)
- [ ] Add `cellSprites[]` and `TryGetCellSprite` on `PieceDefinitionSO`
- [ ] Add `InverseRotateOffset` on `ShapeTransforms` with unit test
- [ ] Add path constants for neutral icon/cell folders

### Task 2: Board sprite rendering

**Files:**
- Create: `Assets/_Project/Presentation/Board/PieceArtResolver.cs`
- Modify: `Assets/_Project/Presentation/Board/PieceShapeVisual.cs`
- Modify: `Assets/_Project/Presentation/Board/BoardView.cs`
- Modify: `Assets/_Project/Presentation/Reserves/ReservesView.cs`

- [ ] Resolve local cell offset from anchor + rotation
- [ ] Render `Image.sprite` per cell when art present; tint fallback otherwise
- [ ] Hide footprint label when every cell has a sprite

### Task 3: Editor pipeline tools

**Files:**
- Create: `Assets/_Project/Data/Editor/NeutralArtPipelineEditor.cs`

- [ ] Menu: Create Neutral Art Folders
- [ ] Menu: Generate Placeholder Neutral Icons
- [ ] Menu: Assign Neutral Icons From Renders
- [ ] Menu: Assign Neutral Cell Sprites From Renders
- [ ] Menu: Validate Neutral Art Assets

### Task 4: Verify

- [ ] Run Edit Mode tests
- [ ] Commit infrastructure on `art-exploration` branch
