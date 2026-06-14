---
name: unity-asset-pipeline
description: >-
  Manages Unity asset pipeline: import settings, ScriptableObject databases,
  Addressables, procedural asset generation, texture/mesh optimization, and
  version-control-friendly workflows. Use when organizing content, building data
  pipelines, setting up async loading, fixing import settings, generating
  ScriptableObject content, or improving build size and load performance.
  Invoke when unity-game-director needs scalable content systems.
paths:
  - "Assets/_Project/Data/**"
  - "tools/**"
disable-model-invocation: false
---

# Unity Asset Pipeline

Specialized skill for building efficient, maintainable, and scalable asset workflows in Unity. Focuses on getting assets into the game cleanly and loading them performantly.

## Using in Cursor

- Mention ScriptableObjects, Addressables, content databases, import settings, or invoke **@unity-asset-pipeline** in chat.
- For imported FBX/material/LOD setup, use **@unity-3d-pipeline** (mesh import focus).
- Data models consumed at runtime coordinate with **@unity-gameplay-systems**.
- Texture/shader optimization with **@unity-graphics-pipeline**.
- Validate load performance with **@unity-qa-build** build/size checks.

## Core Principles

- **ScriptableObject Databases**: The backbone of data-driven design. Almost all game content (items, enemies, upgrades, levels, effects) should be defined in ScriptableObjects.
- **Addressables over Resources**: Use Addressables for almost everything that isn't tiny. Better memory management, async loading, remote content, and build size control.
- **Import Settings Matter**: Wrong import settings are one of the biggest sources of performance problems and visual issues. Automate or document them.
- **Procedural Generation > Hand-Authored** when it creates replayability (especially for roguelikes).
- **Version Control Friendly**: Binary assets in LFS, clear folder structure, minimal meta file churn, and good naming conventions.
- **Designer-Friendly Tooling**: Custom inspectors, creation wizards, and validation tools so non-programmers can work efficiently.

## Key Areas & Patterns

### 1. Project Folder Structure (Recommended)

```
Assets/
├── Art/
│   ├── Models/
│   ├── Textures/
│   ├── Materials/
│   ├── VFX/
│   └── Animations/
├── Audio/
├── Prefabs/
├── Scenes/
├── ScriptableObjects/          ← Core databases live here
│   ├── Items/
│   ├── Enemies/
│   ├── Upgrades/
│   ├── Levels/
│   └── Effects/
├── Scripts/
│   ├── Gameplay/
│   ├── Systems/
│   ├── Data/                   ← ScriptableObject definitions + runtime models
│   └── Editor/
├── Editor/                     ← Custom editors, wizards, validation
├── Resources/                  ← Minimal usage (only for true bootstrap assets)
└── Addressables/               ← Groups organized by type or scene
```

### 2. ScriptableObject Databases

- Create base classes like `ItemDefinition`, `EnemyArchetype`, `UpgradeData`, `LevelData`.
- Use `[CreateAssetMenu]` + custom inspectors for easy creation and editing.
- Runtime lookup systems (e.g., `ItemDatabase.GetById(id)` or dictionaries built at startup).
- Validation tools that run on import or via menu to catch missing references or invalid data.

### 3. Addressables Setup & Best Practices

- Create Addressable groups logically (e.g., Core, Gameplay, UI, Levels, Shared).
- Use labels for filtering and remote vs local content.
- Async loading patterns with proper error handling and cancellation.
- Build Addressables content as part of the normal build process.
- Analyze build size and dependencies regularly.

### 4. Import Settings Automation

- Editor scripts or AssetPostprocessors to enforce consistent settings:
  - Models: Read/Write disabled when possible, optimize mesh, generate lightmap UVs appropriately.
  - Textures: Max size, compression format per platform, mipmaps, streaming.
  - Audio: Compression format, load type (Streaming vs Decompress on Load).
- Provide clear documentation or menu items that re-apply "correct" settings to selected assets.

### 5. Procedural Asset Generation

- Level/room generators that output prefabs or ScriptableObject configurations.
- Item/enemy variant generators.
- Texture or material procedural generation via shaders or C#.
- Seeded generation so results are reproducible and debuggable.

### 6. Optimization & Memory

- Texture streaming + Addressables.
- Mesh LODs and simplification.
- Material variant reduction and GPU instancing.
- Asset bundle / Addressables dependency analysis.
- Memory Profiler integration to find leaks from assets.

### 7. Version Control & Collaboration

- Proper `.gitignore` + Git LFS for models, textures, audio, animations.
- Clear naming conventions and folder discipline.
- Lock files or work-in-progress markers for large assets.
- Meta file stability (avoid unnecessary re-imports).

## Workflow When Activated

1. **Audit current asset situation**: Folder structure, import settings, Addressables usage, ScriptableObject coverage, and obvious performance issues.
2. **Propose improved structure** and migration path (if needed).
3. **Generate**:
   - ScriptableObject definitions + editor tooling.
   - Addressables group setup and loading patterns.
   - AssetPostprocessor or validation scripts.
   - Procedural generation systems (especially for roguelikes).
4. **Provide Editor steps**: Creating Addressable groups, setting up labels, building content, using the Addressables Profiler, re-importing with correct settings, etc.
5. **Tie to other systems**: Work with `unity-gameplay-systems` for data models and `unity-graphics-pipeline` for material/texture optimization.

## DeadManZone conventions

### Current stack (Resources bootstrap, not Addressables yet)

| Concern | Location / pattern |
|---------|-------------------|
| Content hub | `ContentDatabase` SO — `Assets/_Project/Data/ContentDatabase.cs` |
| Runtime load | `ContentDatabase.Load()` from `Resources/DeadManZone/ContentDatabase` |
| Generated pieces | `Assets/_Project/Data/Resources/DeadManZone/Pieces/` |
| Factions / enemies | `Resources/DeadManZone/Factions/`, `Enemies/` |
| SO definitions | `Assets/_Project/Data/ScriptableObjects/` |
| Editor generators | `Assets/_Project/Data/Editor/` |
| Offline tooling | `tools/` (batch scripts when Unity unavailable) |

**Menu prefix:** `[CreateAssetMenu(menuName = "DeadManZone/...")]`

Addressables is the recommended future migration path; do not add new content to `Resources/` unless it is bootstrap-critical (like `ContentDatabase` today).

### Primary editor menus

| Task | Menu |
|------|------|
| Generate full demo content | `DeadManZone > Generate Demo Content (5 Factions)` |
| Legacy vertical slice | `DeadManZone > Generate Vertical Slice Content` |
| Sandbox art catalog | `DeadManZone > Art > Create Default Sandbox Art Catalog` |
| Full sandbox art pipeline | `DeadManZone > Art > Run Full Sandbox Art Pipeline` |
| Validate art coverage | `DeadManZone > Art > Validate Sandbox Art Coverage` |
| Synty art pass | `DeadManZone > Synty > Apply Full Synty Art Pass` |
| Combat arena VFX/anim sets | `DeadManZone > Combat Arena > Create Or Refresh VFX Set` |
| Tag migration | `DeadManZone > Migrate Piece Tags` |
| Tag authoring | `DeadManZone > Tag Creator` |

First-time setup order: **Generate Demo Content** → **Create Default UI Theme** → **Setup Main Menu & Run Scenes**.

### Content types

- `PieceDefinitionSO` — board/combat units (referenced by `ContentDatabase`)
- `FactionSO` — playable + enemy factions
- `EnemyTemplateSO` — 10-fight gauntlet encounters
- `CombatArenaVfxSetSO`, `CombatArenaAnimationSetSO`, `CombatArenaConfigSO` — arena presentation
- `SandboxArtCatalog` — icon/prefab mapping for sandbox roster

Runtime lookup goes through `ContentRegistryProvider.Build(database)` — do not hardcode piece lists in gameplay code.

### Validation tests

| Test | Covers |
|------|--------|
| `ContentDatabaseTests` | Database integrity |
| `SandboxArtCoverageTests` | Art references on all demo pieces |
| `VerticalSliceRegressionTests` | Fixed-seed combat determinism |

Run EditMode tests via **@tdd-iteration** after content pipeline changes.

### Adding new content (checklist)

- [ ] New piece/enemy defined as SO under `Resources/DeadManZone/`
- [ ] Registered in `ContentDatabase` via generator or manual assign + `DemoContentDatabaseWriter` pattern
- [ ] Tags via `Tag Creator` / catalogs — not raw strings in Core
- [ ] Art pass applied (`Validate Sandbox Art Coverage` passes)
- [ ] EditMode test or regression seed updated if combat-relevant

## Quality Gates

- All major game content is defined in ScriptableObjects (not hard-coded).
- Addressables are used for content that benefits from async/streaming.
- Import settings are consistent and appropriate for target platforms.
- Procedural systems produce valid, varied, seeded output.
- Build size is reasonable and dependencies are understood.
- Assets load without hitching and can be unloaded when no longer needed.

## Cursor + Unity Notes

- Generate both the data classes and the editor tooling together.
- Provide exact steps to set up Addressables groups and labels in the Editor.
- Suggest validation menu items or custom inspectors that make the pipeline designer-friendly.
- After changes, include steps to test loading/unloading and check the Addressables report.

This skill is used by the director when asset organization, data pipelines, or loading performance need improvement. It creates scalable, maintainable content systems.
