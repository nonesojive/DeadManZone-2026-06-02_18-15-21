---
name: unity-graphics-pipeline
description: >-
  Configures Unity rendering with URP/HDRP, materials, Shader Graph, VFX Graph,
  lighting, post-processing, and visual polish with performance tuning. Use when
  visuals look flat or basic, setting up URP, authoring shaders, adding VFX,
  tuning post-processing volumes, optimizing draw calls, or moving from
  functional to premium presentation. Invoke when unity-game-director needs
  visual polish or profiling shows rendering bottlenecks.
paths:
  - "Assets/_Project/Presentation/**"
  - "Assets/_Project/Data/Visual/**"
  - "Assets/_Project/Settings/Rendering/**"
  - "**/*.shadergraph"
  - "**/*.shadersubgraph"
disable-model-invocation: false
---

# Unity Graphics Pipeline

Specialized skill for building high-quality, performant visuals in Unity using URP (recommended for most games) or HDRP. Works with `unity-game-director` for overall polish and `unity-gameplay-systems` for gameplay-synced effects.

## Using in Cursor

- Mention URP, shaders, lighting, VFX, post-processing, visual polish, or invoke **@unity-graphics-pipeline** in chat.
- For imported model materials and LOD setup, use **@unity-3d-pipeline** (not gameplay rendering code).
- Before claiming perf is fixed, profile with **@unity-qa-build** and cite Frame Debugger / Profiler evidence.
- For runtime gameplay scripts that trigger effects, use **@game-developer**.

## Core Principles

- **URP First** (for most projects): Better performance, easier mobile/WebGL support, excellent Shader Graph + VFX Graph integration. Only use HDRP for high-end realistic or stylized cinematic needs.
- **SRP Batcher + GPU Instancing**: Design materials and shaders to be batcher-compatible. Minimize material variants and state changes.
- **Data-Driven Materials**: Use MaterialPropertyBlocks or instanced properties heavily. Avoid per-instance material duplication.
- **Visual Feedback is Gameplay**: Hit reactions, status effects, ability tells, and environmental feedback must be clear, juicy, and performant.
- **Optimization is Iterative**: Profile first (Unity Profiler + Frame Debugger), then optimize. Never guess.
- **Stylized > Photorealistic** for most indie/roguelike games: Strong art direction and consistent style beats chasing realism.

## Key Areas & Implementation Patterns

### 1. Project & URP Setup

- Graphics Settings: Assign URP asset, set up multiple URP assets for different quality levels (Low/Medium/High) with Quality Settings.
- Renderer Features: Custom render features for outlines, edge detection, distortion, or gameplay-specific effects (e.g., fog of war, vision cones).
- Forward+ or Deferred rendering choice based on light count and target platform.
- **Editor steps the AI will provide**: Exact Project Settings paths and recommended asset creation menu items.

### 2. Lighting & Environment

- Mixed lighting strategy (baked key lights + real-time fill + dynamic for moving objects).
- Light Layers / Culling Masks for performance.
- Reflection Probes, Light Probes, and Probe Volumes (URP) for dynamic objects.
- Volumetric fog, ambient occlusion, and screen-space effects via Renderer Features or Volume overrides.
- Day/night or dynamic environment lighting with Volume blending.

### 3. Materials & Shaders (Shader Graph)

- Lit shaders as base; custom Shader Graph for unique gameplay elements (energy shields, holographic UI, stylized water, trench effects, etc.).
- Use Sub Graphs heavily for reusability.
- Custom lighting models or stylized ramps when needed.
- Texture streaming + Addressables for large texture sets.
- **Common patterns**: Triplanar mapping, parallax occlusion (carefully), vertex animation, dissolve effects, fresnel, etc.

### 4. VFX Graph & Particles

- VFX Graph preferred for complex, GPU-simulated effects (explosions, muzzle flashes, environmental debris, status auras).
- Integrate tightly with gameplay events (damage numbers, hit impacts, ability activations, death effects).
- Object pooling for VFX that are spawned frequently.
- Budget: Keep draw calls and overdraw low. Use LODs or quality tiers for VFX.

### 5. Post-Processing (Volume Overrides)

- Core stack: Bloom, Color Grading, Vignette, Film Grain, Depth of Field (use sparingly), Motion Blur (gameplay-dependent), Screen Space Ambient Occlusion / Global Illumination.
- Multiple volumes with priorities and blending for different game states (combat intensity, menus, exploration).
- Custom post effects via custom Renderer Features when Volume stack is insufficient.

### 6. Performance & Optimization

- **Always profile** with Unity Profiler (Timeline view), Frame Debugger, and Rendering Profiler.
- Key metrics to target: Draw calls, triangles, texture memory, batch count, shader variants.
- Techniques: Static/Dynamic batching, GPU instancing, LOD Groups + LOD cross-fade, Occlusion Culling, Texture mip streaming, Addressables for scene streaming.
- Mobile-specific: Lower resolution scaling, simplified shaders, reduced real-time lights/shadows, compressed textures (ASTC/ETC2).
- Shader variant stripping and build-time optimization.

### 7. Animation & Juice Integration

- Mecanim + Animator for characters; Playables or custom animation jobs for complex runtime animation.
- Cinemachine for camera work (follow, shake, zoom on impact, dynamic framing).
- Tight coupling between gameplay events and visual/audio feedback (no delay, clear cause-effect).

## Workflow When Activated

1. **Visual Audit**: Analyze current screenshots or Play mode footage for flat lighting, weak materials, missing feedback, overdraw, or performance issues.
2. **Propose Visual Direction**: Suggest art style pillars + technical approach (e.g., "stylized low-poly with strong rim lighting and VFX Graph hits" or "gritty trench realism with heavy post and dynamic shadows").
3. **Implement in Order**:
   - URP + lighting foundation
   - Core materials and Shader Graph library
   - VFX Graph for key gameplay moments
   - Post-processing volumes tuned per game state
   - Optimization pass with profiling evidence
4. **Provide Editor Instructions**: Exact steps to create URP assets, assign in Graphics Settings, set up Volumes in scenes, create new Shader Graph assets, etc.
5. **Gameplay Sync**: Ensure visual effects are driven by events from gameplay systems (not polling). Use ScriptableObject event channels or UnityEvents.

## Quality Gates (Before Returning to Director)

- Lighting reads as intentional and supports gameplay readability (important elements pop, hazards are clear).
- Materials have depth, response to lighting, and consistent art direction.
- Key gameplay actions have satisfying visual feedback (impacts, status, abilities).
- Performance is stable at target FPS with headroom (Profiler data provided).
- No obvious overdraw, shader compilation hitches, or excessive draw calls in typical gameplay scenarios.
- Visuals scale reasonably across quality settings.

## DeadManZone conventions

### Project context

| Item | Value |
|------|-------|
| Pipeline | URP — `Assets/_Project/Settings/Rendering/DeadManZone_URP.asset` |
| Art direction | Stylized (Synty-style); avoid batch-converting Synty materials to URP/Lit |
| Target FPS | 60 (frame time ≤ 16 ms) |
| Visual data | `VisualProfileSO` + atmosphere/lighting presets under `Assets/_Project/Data/Visual/` |
| Combat VFX | `CombatArenaVfxSetSO` + `CombatArenaVfx.cs` (pooled muzzle/tracer/impact) |

### Editor menu paths (use these first)

| Task | Menu |
|------|------|
| Assign URP pipeline | `DeadManZone > Rendering > Setup URP For Project` |
| Validate pipeline active | `DeadManZone > Rendering > Validate URP Setup` |
| Default visual profile | `DeadManZone > Visual Studio > Create Default Profile` |
| Starter atmosphere presets | `DeadManZone > Visual Studio > Create Starter Presets` |
| Combat arena VFX set | `DeadManZone > Combat Arena > Create Or Refresh VFX Set` |
| Synty art pass | `DeadManZone > Synty > Apply Full Synty Art Pass` |

Setup order after fresh clone: **Setup URP** → Synty packages → **Synty Art Pass** → **Validate URP Setup**.

### Key asset paths

- URP asset: `Assets/_Project/Settings/Rendering/DeadManZone_URP.asset`
- Default profile: `Assets/_Project/Data/Visual/DeadManZoneDefaultVisualProfile.asset`
- Runtime profile: `Assets/_Project/Data/Resources/DeadManZone/VisualProfile.asset`
- Main menu atmosphere: `Assets/_Project/Data/Visual/Atmosphere/MainMenuAtmosphere.asset`
- Run atmosphere: `Assets/_Project/Data/Visual/Atmosphere/RunAtmosphere.asset`
- Editor tooling: `Assets/_Project/Presentation/Editor/Rendering/`

### Validation workflow

1. Play Main Menu and Run scenes — check readability of UI, board, and combat units.
2. `Window > Analysis > Frame Debugger` during active combat (Combat Arena scene).
3. `Window > Analysis > Profiler` (Rendering + CPU) during shop UI + combat peaks.
4. Confirm VFX triggers from gameplay events (`CombatArenaVfx`), not per-frame polling.

## Cursor-Specific Notes

- Generate Shader Graph assets via menu paths the user can follow (or describe creation).
- Provide C# scripts for runtime material property changes, VFX event triggers, or custom Renderer Feature setup.
- Suggest Scene setup steps (Volume placement, lighting rig prefabs, post-process volume profiles).
- After code changes, always suggest a quick Play mode + Frame Debugger validation workflow.

This skill is used by the director when visuals need to move from "functional" to "premium" or when performance profiling shows rendering bottlenecks. It produces clean, maintainable rendering code and Editor-friendly setups.
