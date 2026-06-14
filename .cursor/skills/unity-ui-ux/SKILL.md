---
name: unity-ui-ux
description: >-
  Implements Unity UI/UX with UI Toolkit or Canvas: HUDs, menus, overlays,
  responsive layouts, input handling, accessibility, theming, and game-specific
  UI patterns. Use when building or refactoring interfaces, HUDs, inventories,
  shop screens, tooltips, drag-and-drop UI, or menu navigation. Invoke when
  unity-game-director needs UI work or gameplay systems need reactive presentation.
paths:
  - "Assets/_Project/Presentation/**"
  - "Assets/_Project/Data/Menu/**"
  - "Assets/_Project/Data/Visual/Presets/**"
disable-model-invocation: false
---

# Unity UI/UX

Specialized skill for creating clean, responsive, maintainable, and player-friendly interfaces in Unity. Works closely with `unity-game-director` and `unity-gameplay-systems`.

## Using in Cursor

- Mention HUD, menus, UI Toolkit, Canvas, inventory, tooltips, theming, or invoke **@unity-ui-ux** in chat.
- Bind UI to gameplay via **@unity-gameplay-systems** events and data models — never poll gameplay state in `Update()`.
- For visual polish (post, fonts, VFX on UI), coordinate with **@unity-graphics-pipeline**.
- Validate layout and input with **@unity-qa-build** Play Mode checks.

## Core Principles

- **UI Toolkit preferred** for new work (modern, performant, data-binding friendly, better long-term maintainability). Use Canvas + uGUI only when necessary (complex 3D world-space UI, heavy legacy dependencies, or very simple HUDs).
- **Data-driven UI**: Bind UI to ScriptableObjects, runtime data, or observable models instead of polling or tight coupling to gameplay objects.
- **Responsive & Safe Areas**: Design for multiple aspect ratios and mobile safe areas from day one.
- **Clear Information Hierarchy**: Critical gameplay info (health, resources, objectives) must be instantly readable. Secondary info can be in panels/menus.
- **Input Agnostic**: Support keyboard, gamepad, mouse, and touch via the Input System. UI navigation must feel natural on all devices.
- **State Management**: Use a clear UI state machine or stack (e.g., Main Menu → HUD → Pause → Inventory → Settings) to avoid spaghetti.
- **Performance**: Minimize canvas rebuilds, use object pooling for dynamic lists (inventories, notifications), and prefer UI Toolkit's batching.

## Key Systems & Patterns

### 1. HUD (Heads-Up Display)

- Health, stamina/energy, ammo/resources, objectives, minimap/compass, ability cooldowns.
- Use world-space UI sparingly (damage numbers, interaction prompts) and screen-space for most HUD elements.
- Reactive updates via events rather than Update() polling.

### 2. Menus & Navigation

- Main Menu, Pause Menu, Settings, Inventory, Shop, Upgrade screens.
- UI Toolkit: Use `UIDocument`, `VisualElement`, `ListView`, `ScrollView`, and data binding.
- Canvas: Use multiple Canvases with different sort orders or a single well-managed Canvas.
- Consistent navigation (Tab / controller D-pad / mouse) and clear focus handling.

### 3. Overlays & Popups

- Damage numbers, floating text, notifications, confirmation dialogs, tooltips.
- Pool frequently used overlay elements.
- Fade/scale/animate entrances and exits for polish (DOTween or UI Toolkit animations).

### 4. Inventory & Progression UI

- Grid or list-based inventory with drag-and-drop or controller-friendly selection.
- Item tooltips with rich data (stats, descriptions, comparisons).
- Upgrade trees or radial menus for roguelike/meta progression.
- Bind directly to player data ScriptableObjects or runtime models.

### 5. Theming & Visual Consistency

- Define a UI theme (colors, fonts, spacing, button styles) in a central ScriptableObject or USS (UI Toolkit) / prefab.
- Consistent iconography and visual language across the game.
- Support colorblind modes and high-contrast options.

### 6. Accessibility

- Keyboard navigation + gamepad support (Input System + UI navigation).
- Scalable text and UI elements.
- Colorblind-friendly palettes and patterns (not just color).
- Remappable controls exposed in settings.
- Screen reader considerations where feasible (labels, descriptions).

### 7. Input Integration

- Use Input System's `InputAction` + UI action maps.
- Handle device switching gracefully (show/hide relevant prompts).
- Virtual on-screen controls only when needed (mobile) and hide them on other platforms.

## Workflow When Activated

1. **Audit current UI** for coupling, polling, performance issues, or poor information hierarchy.
2. **Propose architecture**: UI state machine, data models, binding strategy, and whether to use UI Toolkit or Canvas.
3. **Generate code + assets**:
   - UI Toolkit: `VisualTreeAsset` (UXML), `StyleSheet` (USS), C# logic with `UIDocument`.
   - Canvas: Well-organized prefabs, Canvas groups, layout groups, and event-driven controllers.
4. **Provide Editor steps**: How to create UIDocuments, set up multiple Canvases, create USS themes, wire up Input System actions, etc.
5. **Tie to gameplay**: Ensure UI reacts to events from `unity-gameplay-systems` (health changed, item picked up, turn started, etc.).
6. **Test responsiveness**: Different resolutions, aspect ratios, and safe areas in the Game view.

## DeadManZone conventions

### Current stack (Canvas-first)

This project uses **Canvas + TextMeshPro (uGUI)**, not UI Toolkit. Extend existing patterns; do not introduce UI Toolkit for a single screen unless explicitly migrating.

| Concern | Location / type |
|---------|-----------------|
| Theming | `UiThemeSO` + `UiThemeApplicator` + `UiThemeProvider` |
| Runtime theme | `Assets/_Project/Data/Resources/DeadManZone/UiTheme.asset` |
| Theme presets | `Assets/_Project/Data/Visual/Presets/` (SyntyTrench, BunkerSurvival, HighContrast, BleachedTrench) |
| Menu theme | `Assets/_Project/Data/Menu/DeadManZoneMenuTheme.asset` |
| Shop / board UI | `Assets/_Project/Presentation/Shop/`, `Board/` |
| Drag-and-drop | `Assets/_Project/Presentation/DragDrop/` |
| Piece tooltips/cards | `PieceCardViewModelBuilder`, `PieceHoverCardController` |
| Tactic pause UI | `Assets/_Project/Presentation/Combat/` |

New UI code belongs under `Assets/_Project/Presentation/`. Apply styles via `UiThemeApplicator` — do not hardcode colors on individual widgets.

### Resource HUD (four resources)

Display **Supplies, Manpower, Authority, Morale** from `RunState`. Do not reintroduce legacy Gold/Requisition labels.

### Editor menu paths

| Task | Menu |
|------|------|
| Create default theme | `DeadManZone > Create Default UI Theme` |
| Setup menu/run scenes | `DeadManZone > Setup Main Menu & Run Scenes` |
| Refresh main menu | `DeadManZone > Refresh Main Menu Scene` |
| Import Synty theme | `DeadManZone > UI Kit > Import Synty Trench Theme` |
| Apply theme to profile | `DeadManZone > UI Kit > Apply Synty Theme To Active Profile` |
| Bunker Survival kit | `DeadManZone > UI Kit > Import Bunker Survival Theme` |
| Wire sell zone | `DeadManZone > UI Kit > Wire Apocalypse Sell Zone` |
| Restyle all scenes | `DeadManZone > UI Kit > Restyle All Scenes With Bunker Kit` |

After theme changes, run the appropriate **Restyle** or **Refresh Main Menu Scene** menu item and verify in Play Mode.

### Drag-and-drop conventions

- Shop offers: drag to board/reserves; **R** / **Q** rotate while dragging.
- Sell zone: salvage refund via dedicated sell UI (see Synty Apocalypse sell zone setup).
- Controllers live in `DragDrop/` — extend existing drag handlers rather than adding parallel systems.

### Play Mode tests (UI integration)

| Test file | Covers |
|-----------|--------|
| `ShopViewPlayModeTests` | Shop display and interaction |
| `ShopOfferDragPlayModeTests` | Drag-drop shop offers |
| `BoardViewPlayModeTests` | Board layout and placement |
| `TacticPausePanelPlayModeTests` | Combat pause / tactic UI |
| `MainMenuPlayModeTests` | Main menu flow |

Run Play Mode tests via **@tdd-iteration** / `BatchTestRunner.RunPlayModeTests` after UI changes.

### Validation checklist (DeadManZone)

- [ ] Theme applied via `UiThemeApplicator` (no orphan hardcoded colors)
- [ ] Four resources readable at a glance on Run scene
- [ ] Shop drag-drop + rotate works at 16:9 and ultrawide Game view sizes
- [ ] Tactic pause panel does not block combat input when dismissed
- [ ] Main Menu → New Run navigation intact

## Quality Gates

- Critical information is instantly readable at a glance.
- Navigation feels natural on keyboard, gamepad, and touch.
- No major layout breakage across common aspect ratios.
- Dynamic lists (inventory, notifications) perform well without GC spikes.
- UI state is predictable and doesn't conflict with gameplay input.
- Theming is consistent and easy to modify globally.

## Cursor + Unity Notes

- Generate both the C# controller logic and the UI asset creation steps.
- For UI Toolkit, provide example UXML/USS snippets or describe how to create them via the UI Builder.
- Suggest concrete GameObject + Component setups in scenes (e.g., "Create a new GameObject with UIDocument component and assign the UXML").
- After changes, include quick Play mode validation steps for input and layout.

This skill is used by the director when UI/HUD/menus need implementation or major cleanup. It produces clean, reactive, and accessible interfaces that enhance rather than fight the gameplay.
