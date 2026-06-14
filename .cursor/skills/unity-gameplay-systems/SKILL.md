---
name: unity-gameplay-systems
description: >-
  Designs and implements Unity gameplay architecture: player controllers, state
  machines, procedural generation, combat/action systems, progression, and
  save/load. Optimized for roguelikes, tactical, and grid-based games. Use when
  building core loops, refactoring mechanics, adding deterministic generators,
  combat resolvers, meta progression, or deep systems work beyond surface-level
  scripting. Invoke when unity-game-director needs mechanics implementation or
  roguelike architecture.
paths:
  - "Assets/_Project/Core/**"
  - "Assets/_Project/Game/**"
  - "Assets/_Project/Data/**"
disable-model-invocation: false
---

# Unity Gameplay Systems

Specialized skill for building robust, extensible, and fun gameplay systems in Unity (C#). Complements `unity-game-director` and `senior-game-dev-lead`.

## Using in Cursor

- Mention core loop, combat systems, procedural generation, save/load, or invoke **@unity-gameplay-systems** in chat.
- For hands-on Unity C# across engines, also use **@game-developer**.
- For TDD on mechanics, combine with **@tdd-iteration**.
- Before claiming systems are done, verify with **@unity-qa-build**.
- For phased multi-agent gameplay work, use subagent **gamedev-gameplay-dev**.

## Core Principles (Always Apply)

- **Data-driven first**: Use ScriptableObjects extensively for balancing, entity definitions, upgrades, level data, and runtime configuration. Avoid hardcoding numbers in code.
- **Composition & Events**: Prefer composition over deep inheritance. Use event-driven communication (UnityEvents, custom event buses, or ScriptableObject-based event channels) to decouple systems.
- **Determinism for Roguelikes**: Seeded RNG (System.Random or Unity.Mathematics with seed). All procedural generation must be reproducible from seed + player choices.
- **Object Pooling**: Mandatory for any frequently spawned objects (projectiles, particles, enemies, effects). Never Instantiate/Destroy in hot paths.
- **Separation of Concerns**: Simulation (logic, state) vs Presentation (visuals, audio, FX). Keep gameplay code testable and independent of MonoBehaviour where possible.
- **Performance Awareness**: Minimize allocations in Update/FixedUpdate. Use object pools, struct-based data where appropriate, and consider Jobs/Burst for heavy generation or AI.
- **State Management**: Clear state machines or stack-based state for game flow (MainMenu → Playing → Paused → GameOver). Entity-level FSMs or behavior trees for AI.

## Key Systems to Implement / Refactor

### 1. Player Controller & Input

- Use Unity's new Input System (recommended) or legacy with clear abstraction layer.
- For grid/turn-based roguelikes: Custom grid movement with validation, path preview, action points or energy system.
- Support multiple input schemes (keyboard, gamepad, touch) via Input System action maps.
- Camera follow with Cinemachine (virtual cameras, blending, shake on impact).

### 2. Core Game Loop & Action Resolution

- Turn-based or hybrid real-time with pause: Action queue or energy tick system.
- Centralized Action/Combat Resolver that processes player intent → validation → application of effects → reactions.
- Damage, healing, status effects as data (ScriptableObject effects with Apply/Remove methods or event hooks).
- Faction/alignment system for asymmetric gameplay (player factions, enemy types, neutral).

### 3. Procedural Generation (Critical for Roguelikes)

- Seeded generators for maps/levels (grid, rooms, trenches, paths).
- Entity placement with constraints (no overlap, connectivity, difficulty scaling).
- Item/loot tables driven by ScriptableObject databases + weighted random with seed.
- Room templates or wave-based spawning with increasing complexity per run.
- Provide visual debugging (Gizmos, custom Editor windows for generation preview).

### 4. Entity & Component Architecture

- Base Entity or use composition with "Gameplay Components" (Health, Attack, Movement, Inventory, AI).
- ScriptableObject "Archetypes" or "Definitions" that spawn configured entities.
- Avoid "God classes" — split responsibilities (e.g., separate Combatant, Mover, Inventory).

### 5. Progression, Meta, & Economy

- Run-based progression: Upgrades between runs stored in persistent data.
- Currency, resources, ammo with clear sinks and meaningful choices.
- Unlockables, achievements, or meta-persistent unlocks via ScriptableObject save data or JSON.
- Balance tools: In-editor sliders or dedicated balancing scenes/windows.

### 6. Save / Load & Serialization

- Robust serialization strategy that survives scene reloads, app close, and Unity version changes.
- Options: JSON.NET (or built-in JsonUtility with care), Odin Serializer, or custom binary.
- Save scope: Player state, discovered upgrades, current run state, settings.
- Versioning and migration paths for save data.
- Cloud save hooks (Unity Gaming Services or custom) if planned.

### 7. AI & Enemy Behavior (for tactical games)

- Simple FSM or behavior tree patterns (or integrate Behavior Designer / custom).
- Perception (FOV, hearing, grid line-of-sight).
- Pathfinding (A* Pathfinding Project recommended, or custom grid A* with jobified version for scale).
- Coordinated enemy actions in turn-based mode.

## Workflow When Activated

1. **Audit existing code** for violations of the above principles (god classes, magic numbers, direct references, missing pooling, non-deterministic RNG).
2. **Propose architecture** with clear diagrams (textual or suggest Mermaid/PlantUML) before coding.
3. **Implement in layers**:
   - Data layer (ScriptableObjects)
   - Core simulation layer (pure C# where possible)
   - MonoBehaviour adapters / presenters
   - Editor tooling for designers (custom inspectors, generation preview windows)
4. **Create isolated test scenes** for each major system before integrating into main scene.
5. **Add instrumentation** early: Logging for key events, in-game debug console, performance markers.
6. **Playtest focus**: Core verbs (move, attack, use item, manage resources) must feel responsive and meaningful within first 5-10 minutes.

## Roguelike / Tactical Specific Guidance

- Design around a strong **core verb loop** (e.g., "Position → Act → React → Resource decision").
- Make **information** a resource (FOV, scouting, intel).
- **Asymmetric factions** or unit types create interesting decisions (your examples: Trench Warfare factions).
- **Timed or limited actions** (90s battles, action points) create tension and meaningful choices.
- Ghost / async elements: Design systems that can support replays, ghosts, or future multiplayer without major rewrites.

## DeadManZone conventions

### Layer layout (follow strictly)

| Layer | Path | Responsibility |
|-------|------|----------------|
| Core | `Assets/_Project/Core/` | Pure simulation — board, shop, combat, meta (Unity-free where possible) |
| Game | `Assets/_Project/Game/` | `RunOrchestrator`, `RunManager`, `SaveManager` — run flow glue |
| Presentation | `Assets/_Project/Presentation/` | UI, drag-drop, `CombatDirector`, VFX triggers |
| Data | `Assets/_Project/Data/` | ScriptableObjects, content pipeline, Editor generators |

New gameplay logic belongs in **Core** first; add MonoBehaviour adapters in **Game** or **Presentation** only when Unity lifecycle is required.

### Core loop & state

- **Run flow**: `RunOrchestrator` (partial classes) owns `RunState`, shop, and combat handoff.
- **Demo scope**: 10-fight gauntlet (`RunOrchestrator.MaxFights = 10`).
- **Resources**: Supplies, Manpower, Authority, Morale — never reintroduce legacy Gold/Requisition in new code.
- **Phases**: Build → Fight → (win/lose meta) — persist via `RunSaveSerializer` + `SaveManager`.
- **Combat**: Tick-based with tactic pause — `TickCombatRun`, `CombatResolver`, `CommandProcessor`, `TacticPauseValidator`.

### Determinism

- `RunState.RunSeed` drives shop and combat; shop seed formula: `RunSeed + FightIndex * 100 + RerollCountThisRound`.
- Use seeded RNG helpers from Core; add EditMode regression tests for fixed-seed combat (see `VerticalSliceRegressionTests`).
- Combat replay: `CombatReplayState` + event log on `RunState` for deterministic playback.

### Data & content

- ScriptableObject menu prefix: `[CreateAssetMenu(menuName = "DeadManZone/...")]`.
- Piece/faction/enemy definitions via `ContentDatabase` / `ContentRegistry`.
- Tag/synergy system lives under `Assets/_Project/Core/Tags/` — extend via catalogs, not hardcoded strings.

### Save / load

- Serializer: `RunSaveSerializer` (JSON.NET, schema versioning + migration).
- File I/O: `SaveManager` → `Application.persistentDataPath/run_save.json`.
- Meta/achievements: local under `%LOCALAPPDATA%/DeadManZone/`.
- Any schema change requires bumping `SaveSchemaVersion` + migration test in `RunSaveSerializerTests`.

### Tests (required for Core changes)

| Scope | Path |
|-------|------|
| Core logic | `Assets/_Project/Core.Tests/EditMode/` |
| Presentation mappers | `Assets/_Project/Presentation.Tests/EditMode/` |
| Integration | `Assets/_Project/Tests.PlayMode/` |

Use **@tdd-iteration** and `BatchTestRunner` before handing back to director.

### Editor content menus

| Task | Menu |
|------|------|
| Generate demo content | `DeadManZone > Generate Demo Content (5 Factions)` |
| Setup scenes | `DeadManZone > Setup Main Menu & Run Scenes` |
| Combat arena VFX | `DeadManZone > Combat Arena > Create Or Refresh VFX Set` |

## Cursor + Unity Usage Notes

- Generate code in logical folders: `Assets/Scripts/Gameplay/`, `Assets/ScriptableObjects/Definitions/`, `Assets/Scripts/Systems/`.
- Provide Editor scripts (`Assets/Editor/`) for custom windows, inspectors, or generation previews.
- Always include `[CreateAssetMenu]` for new ScriptableObject types.
- Suggest concrete menu paths and Inspector workflows the user should follow in the Unity Editor.
- After generating systems code, immediately suggest a minimal test scene setup and Play mode validation steps.

## Verification Checklist (Before Handing Back to Director)

- Core player actions feel good in Play mode (responsive, no jank, clear feedback).
- Procedural generation produces valid, varied, connected levels from different seeds.
- No major allocations or GC spikes during core gameplay loop.
- Save/load round-trips correctly for at least one full run + meta progression.
- Systems are decoupled enough that changing one (e.g., combat formula) doesn't break unrelated systems.
- Clear extension points for new content (new enemy types, new upgrades, new room features) via data only.

This skill is invoked by the director for deep mechanics work or directly by the user when focusing on gameplay systems, core loop, or roguelike architecture. It produces production-ready, maintainable code aligned with senior game dev standards.
