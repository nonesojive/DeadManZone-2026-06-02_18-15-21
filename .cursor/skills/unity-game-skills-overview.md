# Unity Game Skills Overview

**A complete Game Director + specialist skills system for Cursor + Unity**, adapted from the excellent [threejs-game-skills](https://github.com/majidmanzarpour/threejs-game-skills) pattern by Majid Manzarpour.

This system gives you (and Cursor's AI) a structured, high-quality way to build Unity games — especially roguelikes and tactical games — with strong emphasis on:

- Playable vertical slices first
- Data-driven design (heavy ScriptableObject usage)
- Event-driven architecture
- Editor tooling for solo developers
- Evidence-based verification before claiming "done"
- Roguelike-friendly patterns (seeding, grid systems, meaningful decisions, procedural generation)

---

## Skill Architecture

There is **one main orchestrator** and **seven focused specialist skills**:

```
unity-game-director          ← Main entry point (always start here for big work)
├── unity-gameplay-systems   ← Core mechanics & roguelike systems
├── unity-graphics-pipeline  ← Rendering, URP, Shader Graph, VFX
├── unity-ui-ux              ← UI Toolkit / HUDs / Menus / Accessibility
├── unity-audio              ← Audio Mixer + event-driven sound
├── unity-asset-pipeline     ← ScriptableObjects, Addressables, imports
├── unity-debug-profiler     ← Profiling, custom debug tools, optimization
└── unity-qa-build           ← Testing, builds, verification gates
```

The **director** routes work intelligently and pulls in the right specialists when needed. You can also invoke any specialist skill directly with `@skill-name` in Cursor chat.

**DeadManZone also has:**

| Skill | Role |
|-------|------|
| `@tdd-iteration` | Write tests first, iterate until green (project skill) |
| `@game-developer` | General Unity C# (personal skill) |
| `@unity-3d-pipeline` | Imported model materials/LODs (personal skill) |

---

## Complete Skill List

| Skill | Short Description | Best Used When... |
|-------|-------------------|-------------------|
| **unity-game-director** | Central orchestrator. Plans architecture, enforces workflow (playable first → iterate → verify), routes to specialists, and ensures quality gates. | Starting a new game, major feature, vertical slice, or polish pass. Almost always the right starting point. |
| **unity-gameplay-systems** | Deep gameplay architecture: player controllers, grid/turn-based systems, procedural generation, combat, progression, save/load, roguelike design patterns. | Implementing core mechanics, combat, procedural systems, or refactoring gameplay code. |
| **unity-graphics-pipeline** | URP/HDRP setup, lighting, Shader Graph, VFX Graph, materials, post-processing, rendering optimization, and visual polish. | Visuals look flat, performance issues in rendering, or moving from prototype to premium look. |
| **unity-ui-ux** | UI Toolkit vs Canvas, HUDs, menus, inventories, responsive layouts, input handling, accessibility, theming. | Building or overhauling any interface, HUD, inventory, or menu system. |
| **unity-audio** | Audio Mixer setup, event-driven SFX/music/ambience, spatial audio, dynamic mixing, and clean integration with gameplay. | Adding or improving audio feedback, music systems, or ambience. |
| **unity-asset-pipeline** | ScriptableObject databases, Addressables, import settings automation, procedural asset generation, folder structure, and asset optimization. | Organizing content, setting up data-driven systems, or improving loading/performance of assets. |
| **unity-debug-profiler** | Unity Profiler & Frame Debugger mastery, custom in-editor debug tools, hot-path optimization, logging strategy, and visual debuggers (Gizmos, inspectors, etc.). | Tracking down bugs, performance problems, or building tools to make development faster. |
| **unity-qa-build** | Testing (Test Framework), profiling, build pipeline, Player/Quality Settings, release checklists, and evidence-based completion gates. | Before claiming any major work is "done". Final verification pass. |

---

## How to Use

### 1. In Cursor (Recommended Daily Workflow)

1. **unity-game-director** is active via Cursor user rules (and/or project `.cursorrules`).
2. Keep the Unity Editor open alongside Cursor.
3. Start prompts with:
   - `@unity-game-director` or `"Use unity-game-director to ..."`
   - Or invoke a specialist: `@unity-gameplay-systems`, `@unity-qa-build`, `@tdd-iteration`, etc.

Project skill copies include **DeadManZone conventions** (paths, seeds, editor menus, test commands).

### 2. File Locations (this repo)

```
.cursor/skills/
├── unity-game-skills-overview.md   ← this file
├── tdd-iteration/SKILL.md
├── unity-gameplay-systems/SKILL.md
├── unity-graphics-pipeline/SKILL.md
├── unity-ui-ux/SKILL.md
├── unity-audio/SKILL.md
├── unity-asset-pipeline/SKILL.md
├── unity-debug-profiler/SKILL.md
└── unity-qa-build/SKILL.md
```

Personal copies also live in `~/.cursor/skills/` for use outside this repo.

---

## Core Philosophy (Enforced by All Skills)

- **Playable vertical slice first** — Get the core loop fun and working before adding content or polish.
- **Data-driven by default** — ScriptableObjects for almost everything configurable.
- **Event-driven & decoupled** — Gameplay systems talk through events, not direct references.
- **Editor tooling is mandatory** — Custom inspectors, windows, and generators for solo dev speed.
- **Evidence before "done"** — Play mode testing + Profiler data + successful builds required.
- **Roguelike-aware** — Seeded generation, grid systems, meaningful decisions, replayability focus.
- **Performance by design** — Profile early, protect hot paths, use pooling and Addressables.

---

## Example Invocation Patterns (DeadManZone)

**New mechanic with tests:**

> @tdd-iteration — add salvage shop lane filtering with EditMode tests first.

**Deep mechanics work:**

> @unity-gameplay-systems — extend TickCombatRun tactic pause for a new command type.

**Visual upgrade:**

> @unity-graphics-pipeline — tune URP and combat arena VFX after Synty art pass.

**Pre-release check:**

> @unity-qa-build — verify 10-fight demo loop, EditMode tests, and PC build.

---

## Recommended Project Setup

1. First-time: **DeadManZone → Generate Demo Content (5 Factions)** → **Setup Main Menu & Run Scenes**.
2. Commit `.cursor/skills/` so teammates share the same AI guidance.
3. Use `Assets/_Project/` layer layout from `@unity-gameplay-systems`.
4. Run `BatchTestRunner` before claiming mechanics done.

---

## Credits & Inspiration

This system is directly adapted from **Majid Manzarpour**'s excellent [threejs-game-skills](https://github.com/majidmanzarpour/threejs-game-skills) repository, which pioneered the "Game Director + specialist skills" pattern for AI coding agents.

Extended and specialized for **Unity + Cursor** with strong roguelike/tactical game support, based on ongoing DeadManZone development.
