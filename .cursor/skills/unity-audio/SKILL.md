---
name: unity-audio
description: >-
  Implements Unity audio with Audio Mixer, event-driven playback, SFX, music,
  ambience, spatial audio, and dynamic mixing integrated with gameplay. Use when
  adding sound effects, music systems, mixer snapshots, audio pooling, combat/UI
  feedback sounds, or refactoring direct AudioSource calls. Invoke when
  unity-game-director needs audio architecture or gameplay events need sound.
paths:
  - "Assets/_Project/Presentation/**"
  - "Assets/_Project/Data/**"
disable-model-invocation: false
---

# Unity Audio

Specialized skill for building polished, performant, and maintainable audio in Unity. Emphasizes event-driven design so audio reacts cleanly to gameplay without tight coupling.

## Using in Cursor

- Mention audio, SFX, music, mixer, ambience, spatial audio, or invoke **@unity-audio** in chat.
- Wire playback to gameplay via **@unity-gameplay-systems** events — never scatter `AudioSource.Play()` in combat logic.
- UI click/transition sounds coordinate with **@unity-ui-ux**.
- Verify voice counts and mixer CPU with **@unity-qa-build** Profiler checks.

## Core Principles

- **Centralized Audio Manager / Event-Driven**: Never play sounds directly from gameplay code. Route everything through a central system or ScriptableObject event channels.
- **Audio Mixer First**: Use Unity's Audio Mixer for grouping (Master, SFX, Music, UI, Ambience, Voice), volume control, snapshots, and dynamic ducking.
- **Object Pooling for SFX**: Pool AudioSources for frequently played sounds (impacts, footsteps, UI clicks) to avoid Instantiate/Destroy costs.
- **Spatial Audio Where It Matters**: Use 3D audio for world events, but 2D for UI, music, and critical feedback.
- **Data-Driven Sounds**: Define sound events, variations, and parameters in ScriptableObjects so designers can tweak without code changes.
- **Performance**: Limit concurrent voices, use audio compression wisely, and profile mixer CPU usage.

## Key Systems & Patterns

### 1. Audio Mixer Setup

- Recommended groups: Master → SFX, Music, UI, Ambience, Voice.
- Snapshots for different game states (Exploration, Combat Intense, Menu, Low Health, etc.).
- Exposed parameters for runtime control (volume, pitch, low-pass filter for underwater/muffled effects, reverb zones).
- Ducking: Lower music/ambience when important SFX or voice plays.

### 2. Event-Driven Playback

- Create `AudioEvent` ScriptableObjects that contain:
  - One or more AudioClips (with random selection + pitch/volume variation)
  - Mixer group reference
  - 2D/3D settings
  - Optional cooldown or priority
- Gameplay systems raise events → Audio system plays the matching event.
- This decouples audio completely from specific MonoBehaviours.

### 3. SFX (Sound Effects)

- Footsteps, impacts, attacks, ability casts, UI clicks, notifications, environmental interactions.
- Variations and randomization to avoid repetition.
- Pooled AudioSources with proper cleanup.
- One-shot vs looping sounds with clear start/stop handling.

### 4. Music & Stingers

- Layered or adaptive music systems (intensity layers driven by gameplay state).
- Music stingers/transitions for key moments (combat start, victory, death, new area).
- Crossfading and smooth parameter changes via Mixer snapshots or exposed parameters.

### 5. Ambience & Environment

- Looping ambient beds (wind, trench atmosphere, dungeon hum).
- Positional ambience (distant combat, water, machinery) using 3D AudioSources or AudioSource pools.
- Reverb zones or mixer snapshots that change based on location or state.

### 6. Spatial & 3D Audio

- Proper use of `AudioSource.spatialBlend`, `dopplerLevel`, `spread`, and rolloff curves.
- AudioListener placement (usually on main camera or player).
- Occlusion and obstruction simulation (optional advanced feature via custom scripts or plugins).

### 7. Voice / Dialogue (if applicable)

- TTS or recorded lines triggered via events.
- Lip sync hooks or simple facial animation triggers.
- Subtitle system integration.

## Workflow When Activated

1. **Audit current audio** for direct AudioSource calls, missing pooling, hard-coded clips, or lack of mixer usage.
2. **Design the audio event system** (ScriptableObject-based events + central player).
3. **Set up or refine the Audio Mixer** with groups, snapshots, and exposed parameters.
4. **Generate**:
   - `AudioEvent` ScriptableObject and editor tooling.
   - Centralized `AudioManager` or event listener.
   - Example gameplay event → audio wiring.
5. **Provide Editor steps**: Creating mixers, groups, snapshots, exposing parameters, setting up reverb zones, and pooling setup.
6. **Integrate with gameplay**: Work with `unity-gameplay-systems` to hook key actions (attacks, damage taken, resource changes, state transitions) to audio events.

## DeadManZone conventions

### Current state (early-stage audio)

Audio is not yet centralized. Existing patterns to respect or migrate:

| Location | Current pattern |
|----------|-----------------|
| Main menu | `MainMenuCameraDirector` — serialized `AudioSource` for transitions |
| Menu builder | `CinematicMenuUiBuilder` — direct clip load from SlimUI assets |
| Combat arena | `CombatArenaUiController` — swaps `AudioListener` between Run and Arena cameras |
| Third-party mixer | `Assets/SlimUI/Modern Menu 1/Audio/Mixer.mixer` (menu only) |

**Do not add more scattered `AudioSource.Play()` calls.** New work should introduce the event-driven system below.

### Recommended layout for new audio code

| Asset / code | Path |
|--------------|------|
| Audio events (SO) | `Assets/_Project/Data/Audio/` with `[CreateAssetMenu(menuName = "DeadManZone/Audio/...")]` |
| Audio manager + pooling | `Assets/_Project/Presentation/Audio/` |
| Mixer asset | `Assets/_Project/Settings/Audio/DeadManZone_Mixer.mixer` |
| Editor bootstrap | `Assets/_Project/Presentation/Editor/Audio/` |

### Integration hook points

Wire audio events from presentation layer, driven by existing combat/VFX flow:

- **Combat impacts** — alongside `CombatArenaVfx` muzzle/tracer/impact (pool SFX like VFX)
- **Tactic pause** — combat intensity snapshot when pause opens/closes
- **Shop UI** — drag-drop, reroll, sell via **@unity-ui-ux** (2D, UI mixer group)
- **Run flow** — fight start/end, win/lose stingers from `RunOrchestrator` phase changes
- **Main menu** — migrate `MainMenuCameraDirector.transitionSound` to `AudioEvent` when manager exists

### AudioListener rule

Only one active `AudioListener`. `CombatArenaUiController` already disables Run listener during arena — extend this pattern; never add a second listener without disabling the first.

### Mixer snapshots (suggested)

| Snapshot | When |
|----------|------|
| Menu | Main menu, settings |
| Build | Run scene shop/build phase |
| Combat | Active tick combat / Combat Arena |
| Pause | Tactic pause panel open |
| Victory / Defeat | Run end screens |

### Validation checklist (DeadManZone)

- [ ] No duplicate active `AudioListener` when switching Run ↔ Combat Arena
- [ ] Combat SFX pooled (match `CombatArenaVfx` spawn frequency)
- [ ] UI sounds on UI mixer group; combat on SFX group
- [ ] Settings volume sliders expose mixer parameters (when settings UI exists)
- [ ] Profiler: Audio thread CPU stable during 10-fight combat peaks

## Quality Gates

- All important gameplay actions have satisfying audio feedback.
- No audio cutoffs, stacking, or performance issues from too many simultaneous sources.
- Volumes and mixing feel balanced and intentional.
- Music and ambience support the desired mood without overpowering gameplay audio.
- Easy for non-coders to add or tweak sounds via ScriptableObjects.

## Cursor + Unity Notes

- Generate the ScriptableObject definitions and the central audio playback system together.
- Provide clear steps to create the Audio Mixer asset and configure groups/snapshots in the Editor.
- Suggest how to wire events from gameplay systems (e.g., "Raise the OnPlayerDamaged AudioEvent from the Health component").
- Include pooling implementation and cleanup patterns.

This skill is used by the director when audio feedback, music, or ambience needs to be added, improved, or properly architected. It produces clean, designer-friendly, and performant audio systems.
