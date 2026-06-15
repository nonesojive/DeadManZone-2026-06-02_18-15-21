---
name: unity-game-director
description: Game Director orchestration skill for Cursor IDE + Unity engine projects. Routes and coordinates gameplay systems, graphics/rendering pipeline, UI/UX, audio, procedural & AI-assisted assets, debugging/profiling, testing/QA, builds, and release readiness. Invoke explicitly for complete games, vertical slices, major upgrades, or polish passes. Complements senior-game-dev-lead for planning and architecture oversight. Designed for solo or small-team Unity C# development with rapid editor iteration.
---

# Unity Game Director

Self-contained director skill for building playable, polished Unity games (primarily with Cursor IDE + Unity Editor + C#). 

After activating this skill (or referencing it in prompts), the AI acts as the central **Game Director**: it plans high-level approach, decomposes work, generates or edits the right C# scripts / ScriptableObjects / editor tools / scene setups, suggests Editor workflows the human performs, integrates assets, enforces quality gates (playable loop first, then iteration + verification), and only claims completion after evidence of functionality, performance, and polish targets.

The skill bundles:
- Core orchestration logic and workflow
- Checklists and prompt templates for sub-domains
- References to Unity best practices, common patterns, and pitfalls
- Guidance for Cursor-specific usage (Composer, multi-file edits, terminal/build commands)
- Fallbacks to procedural generation and Unity built-ins when external AI asset tools are unavailable

## Cursor + Unity Setup Recommendations

1. **Project Structure**: Open your Unity project root (or the `Assets/` + `Packages/` folders) directly in Cursor. Keep Unity Editor running alongside for Play mode testing, Scene view, Profiler, and applying manual Editor steps the AI suggests.

2. **.cursorrules or Rules Integration** (recommended for persistent director behavior):
   - Create `.cursorrules` in project root (or use Cursor Settings > Rules).
   - Paste key sections from this skill (or a summarized version) so every Composer/Agent session starts with director mindset.
   - Example minimal `.cursorrules` excerpt:
     ```
     You are the Unity Game Director. Always:
     - Start with a playable vertical slice / core loop before adding content or polish.
     - Use senior-game-dev-lead principles for scope and risk.
     - Generate modular C# with ScriptableObjects, events, composition, object pooling.
     - Suggest concrete Editor steps (menu items, Inspector setup, Project Settings).
     - Run verification: Play mode tests, Profiler snapshots, build checks before done.
     - Report skill ledger, decisions, risks, and evidence (screenshots, metrics, logs).
     ```
   - For advanced: Mirror structure into `.cursor/skills/unity-game-director/SKILL.md` if your Cursor version or plugins support skill loading (similar to Claude Code patterns).

3. **Unity Packages to Have Ready** (AI will suggest adding via Package Manager or manifest):
   - Core: Input System, Cinemachine, Addressables, URP (or HDRP), Test Framework, Visual Scripting (optional).
   - Useful: Unity AI (if available), Behavior Designer or custom state machines, Odin Inspector (paid, great for data), NaughtyAttributes or custom PropertyDrawers.
   - For roguelikes/grid: A* Pathfinding Project (or custom), Tilemap (2D) or custom grid systems.

4. **External AI Asset Tools** (optional, like Tripo/Gemini/ElevenLabs in the Three.js version):
   - 3D: Meshy.ai, Luma Dream Machine, Kaedim, TripoSR, or Blender + AI addons → export GLB/FBX → Unity import with proper Model/Animation/Material settings.
   - 2D/Textures/Icons: Midjourney, Flux, Leonardo, or Unity's AI tools / procedural.
   - Audio: ElevenLabs, ElevenLabs Unity integration, or Unity Audio + procedural.
   - The director probes for keys or local fallbacks and reports status. Core skill works 100% with Unity primitives + custom C#/Shader Graph.

5. **Git for Unity**: Use proper `.gitignore` (Unity-specific + Rider/Visual Studio), Git LFS for large assets/textures/models, conventional commits, feature branches per milestone.

## How to Invoke

In Cursor Composer, Agent mode, or chat:

```
Use unity-game-director to build a premium roguelike dungeon crawler vertical slice from scratch in Unity.
Prioritize a tight 5-10 minute playable core loop with grid movement, procedural rooms, basic combat, inventory, and win/lose states. Use URP, ScriptableObjects for data, event-driven architecture. Integrate placeholder visuals and audio hooks. Then run full verification (playtest, profiler, build) before reporting completion.
```

The AI (me or Cursor's model) should:
- Load `unity-game-director` first for broad coordination.
- Conceptually route to sub-domains: gameplay systems, graphics pipeline, UI, audio, asset strategy, debug, QA/build.
- Generate/edit code in correct locations (`Assets/Scripts/`, `Assets/ScriptableObjects/`, `Assets/Editor/`, `Assets/Resources/` or Addressables groups).
- Provide step-by-step Editor instructions the user executes (create scenes, add components via Inspector, configure Project Settings > Graphics/Quality/Player).
- Use Play mode + screenshots/logs as primary feedback loop.
- Enforce: Build a minimal playable loop **first**, then iterate on feel/polish/content.
- Output: Decision ledger, reference usage, asset sourcing choices, performance scorecard (target FPS, draw calls, memory), remaining risks, and evidence of gates passed.

Users should rarely need to invoke sub-skills directly; the director handles orchestration and knows when to go deep on one area.

## Core Workflow the Director Enforces

1. **Discovery & Planning** (tie to senior-game-dev-lead)
   - Clarify vision, target platforms, session length, core fantasy pillars, key player verbs.
   - Define vertical slice explicitly (one complete, fun, end-to-end experience with core systems interacting).
   - Identify data-driven elements (use ScriptableObjects heavily for balancing, enemies, items, levels).
   - Risk register: technical spikes, content volume, performance targets, save/load complexity.

2. **Scaffold / Architecture**
   - Recommend or generate a clean project structure if starting fresh (or audit existing).
   - Core folders: `Scripts/` (with subfolders: Core, Gameplay, UI, Systems, Data, Editor), `ScriptableObjects/`, `Prefabs/`, `Scenes/`, `Art/`, `Audio/`, `Resources/` or Addressables, `Tests/`.
   - Architecture principles: Composition over inheritance, event buses or UnityEvents + custom, ScriptableObject singletons for config/runtime data, object pooling for frequent spawns, deterministic seeded RNG for roguelikes, separation of simulation vs presentation.
   - For roguelikes specifically: Turn-based or hybrid energy system, grid/board abstraction, FOV, pathfinding, state machine for entities, robust serialization (JSON or binary via Odin or custom) that survives scene reloads/app lifecycle.

3. **Playable Loop First (MVP Vertical Slice)**
   - Prioritize: Input → Movement/Action resolution → World update (procedural or authored) → Feedback (visual/audio/UI) → Win/lose/progress.
   - Use Unity's Play mode aggressively for feel iteration. AI generates code → user adds to scene or runs test scene → feedback → refine.
   - Only after core loop feels good: expand content, juice (particles, animations, post FX, camera work with Cinemachine), meta systems.

4. **Asset & Content Pipeline**
   - Prefer procedural generation (C# scripts for levels, items, enemies) + Unity primitives / Shader Graph / VFX Graph for rapid iteration.
   - When premium assets needed: Guide import pipeline (model import settings, rig/animation import, material conversion to URP, texture compression, mipmaps, Addressables for streaming).
   - AI image/3D generation: Suggest prompts + post-processing in Unity (or external tools) → proper import.
   - Audio: Audio Mixer groups (Master, SFX, Music, UI, Ambience), event-driven playback (not direct AudioSource everywhere), spatial audio where appropriate.

5. **Graphics & Rendering Polish**
   - URP setup: Renderer Features, Volume overrides (post-processing), lighting (mixed or baked where possible), shadows optimization.
   - Materials: Lit shaders, custom Shader Graph for unique effects, GPU instancing, SRP Batcher compatibility.
   - Optimization: LODs, occlusion culling, batching, texture atlasing, draw call reduction, mobile-friendly settings if targeting mobile.
   - VFX: VFX Graph for particles, trails, hits; integrate with gameplay events.

6. **UI / UX**
   - Prefer UI Toolkit (modern, performant, data-binding friendly) or hybrid with Canvas for complex HUDs.
   - Responsive design, safe areas (mobile), keyboard/gamepad + touch support via Input System.
   - Menus, HUD, tooltips, inventory screens with clear navigation and state management (UI state machine or separate UI manager).
   - Accessibility: Colorblind modes, scalable text, remappable controls.

7. **Audio Integration**
   - Centralized AudioManager or event-driven system.
   - Mixers for dynamic volume ducking, snapshots for tense/relaxed states.
   - Footsteps, impacts, UI clicks, ambient loops, music stingers — all hooked to gameplay events.
   - Optional: FMOD or Wwise integration for advanced needs (director can scaffold the bridge).

8. **Debug, Profile, Iterate**
   - Unity Profiler (CPU, GPU, Memory, Rendering) — capture snapshots during play.
   - Frame Debugger, Scene view stats, Stats window.
   - Custom debug tools: In-game console, entity inspectors, generation visualizers (Gizmos + Editor windows).
   - Hot-reload friendly patterns (ScriptableObjects help here).

9. **QA, Build & Release Gates**
   - **Before claiming "done"**: 
     - Core loop fully playable in Editor Play mode (multiple runs, edge cases).
     - Performance targets met on target hardware (e.g., stable 60/120 FPS, memory < X MB, draw calls reasonable).
     - Build succeeds for primary platform(s) (File > Build Settings).
     - Basic playtest on target device if mobile/console.
     - No obvious bugs in critical paths (save/load, progression, input).
     - Visual scorecard: lighting/materials read as "premium" or "intentional style", UI fits, no clipping, readable text.
   - Use Unity's Build Report or custom build scripts for size analysis.
   - For WebGL: Specific optimizations and player settings.
   - Report remaining risks and recommended next milestones.

10. **Iteration & Polish Loop**
    - After vertical slice: Add juice, balance via data (ScriptableObjects make this easy), expand procedural variety, add meta progression if planned.
    - Always tie changes back to player experience goals and measurable success criteria.

## Sub-Domain Routing (Conceptual Specialists the Director Activates)

When the request implies depth in one area, the director pulls focused knowledge and generates accordingly:

- **unity-gameplay-systems** (dedicated skill available): Deep mechanics, player controllers, grid/turn-based systems, procedural generation, combat resolution, progression, save/load, and roguelike architecture.
- **unity-graphics-pipeline** (dedicated skill available): URP setup, lighting, Shader Graph, VFX Graph, materials, post-processing, and rendering optimization.
- **unity-ui-ux** (dedicated skill available): UI Toolkit / Canvas, HUDs, menus, inventories, responsive layouts, input handling, accessibility, and theming.
- **unity-audio** (dedicated skill available): Audio Mixer, event-driven SFX/music/ambience, spatial audio, dynamic mixing, and gameplay integration.
- **unity-asset-pipeline** (dedicated skill available): ScriptableObject databases, Addressables, import settings, procedural generation, and asset organization.
- **unity-debug-profiler** (dedicated skill available): Profiler/Frame Debugger usage, custom debug tools, hot-path optimization, logging, and in-editor visualizers.
- **unity-qa-build** (dedicated skill available): Testing, profiling, builds, release verification gates, and evidence-based completion.

The director decides sequencing and when to go deep vs keep high-level.

## API Keys / External Tools Policy (Optional)

Core functionality requires **zero** paid keys — everything works with Unity Editor + C# + built-in tools.

When user wants premium AI-generated assets:
- Provide environment variables or Unity Editor Prefs / ScriptableObject config for keys (e.g., `MESHY_API_KEY`, `ELEVENLABS_API_KEY`, OpenAI, etc.).
- Director runs a conceptual "credential probe" and falls back gracefully to procedural + Unity primitives + user-provided assets.
- Never hardcode keys in game code or commit them.
- Generated assets are imported into the Unity project with correct settings and referenced via Addressables or Resources where appropriate.

Example setup (user does this once):
```bash
# In shell or via Unity editor script
export MESHY_API_KEY="..."
# Or store in Unity's EditorPrefs or a local config ScriptableObject (not committed)
```

## Best Entry Points & Example Prompts

- **New game / vertical slice**: Start here. "Use unity-game-director to create a [genre] game vertical slice..."
- **Existing project upgrade**: "Use unity-game-director to upgrade the visuals / add juice / improve performance / implement save system in this Unity project..."
- **Specific deep work** (director still coordinates): Mention the sub-area, e.g., "Use unity-game-director with focus on gameplay-systems to implement turn-based combat and procedural room generation..."
- **Polish / release prep**: "... and run the full QA and build verification pass."

**Good roguelike-style example** (aligns with your ongoing projects):
```
Use unity-game-director to create a Trench Warfare roguelite vertical slice in Unity.
Core loop: 90-second timed engagements on a grid/trench map, asymmetric factions, resource/ammo management, upgrades between runs, basic ghost or async PvP hooks if feasible. Use URP, ScriptableObject data for units/upgrades, event-driven actions, seeded procedural elements. Placeholder visuals + audio hooks. Full playtest + profiler verification before done.
```

## What "Done" Looks Like (Quality Bar)

- Playable core loop that is fun to interact with for the target session length.
- Code is modular, readable, follows Unity + C# idioms, easy to extend with new content via data.
- Visuals read as intentional (even if placeholder or stylized) with good lighting, materials, and feedback.
- UI is functional, responsive, and doesn't fight the player.
- Performance is stable and meets targets on intended hardware.
- Builds cleanly; basic automated or manual tests pass.
- Risks, assumptions, and next steps are clearly documented.
- Evidence provided: Play mode behavior description or video/screenshots, Profiler stats, build output summary.

## Notes & Evolution

- This skill assumes familiarity with Unity fundamentals (scenes, GameObjects, components, Prefabs, Inspector). It layers director-level orchestration, quality enforcement, and Cursor-efficient workflows on top.
- Update this skill as your Unity version, preferred packages, or project conventions evolve (e.g., shift to DOTS/ECS for very large simulations, or new Input System patterns).
- Pairs extremely well with `senior-game-dev-lead` skill: use the latter for high-level GDD, milestone planning, risk assessment, and architecture reviews; use this director for tactical implementation, code generation, Editor orchestration, and verification in Cursor sessions.
- For pure planning or GDD work without heavy coding, lead with senior-game-dev-lead.
- The director is pragmatic and ship-oriented: perfect is the enemy of the vertical slice. Get it playable and fun fast, then iterate with data.

This skill is designed to be copied/adapted into your Cursor project (`.cursorrules` or `.cursor/skills/`) for direct use with Cursor's AI, while also serving as context for me (Grok) when assisting with your Unity projects.
