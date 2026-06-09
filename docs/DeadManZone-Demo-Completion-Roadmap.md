# DeadManZone — Solo Dev Demo Completion Roadmap

**Version:** 1.0  
**Date:** 2026-06-06  
**Audience:** Solo developer + Cursor AI pair-programming workflow  
**Goal:** Move from current June 2026 build state to a fully fleshed-out, playable demo that meets all success criteria in the main GDD.

---

## Current State Assessment (as of 2026-06-06 GDD)

**Strongly shipped / production-ready core:**
- 10-fight linear gauntlet
- Four-resource economy (Supplies, Manpower, Authority, Morale) + manpower gate + emergency draft logic
- Deterministic tick combat sim + replay with two pause windows
- Tactics system + 3 demo abilities (Grenade Lob, Shield Allies, Cannon Blast)
- Permanent faction HQ (auto-spawn + immovable)
- Battle report (top-3 damage dealt/taken)
- 7-column neutral band (25-wide battlefield)
- Movement charge budget + attack speed wiring
- Spatial Reserves (2×9) + Q/R rotation while dragging
- Pause menu + robust auto-save / resume
- Shop with fight-index weighting + lock-slot preservation
- 3 playable factions + content generation pipeline
- Basic synergies + critical mass
- Salvage system + Dust Scourge bonus
- Local meta (achievements, leaderboard, faction unlocks)
- Tutorial economy (125 starting Supplies + boosted rewards fights 1–3) + softened enemy templates 1–3

**Known gaps / in-flight items (prioritized):**
1. Main Fight segment ticks (code currently 200, spec target 300) — trivial config fix.
2. Neutral faction shop icons (5 pieces total; 2 in active progress).
3. Per-cell modular board sprites (Phase 3 of neutral art pipeline — both art + code wiring needed).
4. Emergency Draft button UI wiring + clear player feedback.
5. Build screen zone headers (REAR / SUPPORT / FRONT) verification + polish.
6. Full regression + 10-fight manual campaign playtest (especially pause #2 reach rate ≥90% on tutorial fights).
7. Art style consistency if relying heavily on pure 2D AI image generation.

**Out of demo scope (do not touch):**
- Async PvP matchmaking UI
- 11 additional playable factions
- Branching campaign map / event nodes
- Full 25-keyword advanced mechanics
- Fog-of-war combat intro
- 3D in-engine models / rigging / animation
- Steamworks SDK wiring (stub is ready)

---

## Demo Success Criteria (non-negotiable)

A demo is considered complete only when **all** of the following are true:

- A playtester can complete a full 10-fight run in ~30–40 minutes.
- The player clearly understands the four-resource tension and the manpower gate.
- The player uses tactic selection meaningfully at **both** pause windows and triggers at least one demo ability when the source piece is alive.
- Save mid-combat (any pause or during gas) → reload produces an **identical** combat outcome.
- ≥90% of seeded headless sims on the reference board reach pause #2 in fights 1–3 (enemy templates only — no hidden player nerfs).
- All 5 neutral shop icons are assigned and readable at shop card scale (~48 px) on dark panels.
- Every EditMode test in `DeadManZone.Core.Tests` passes.

---

## Recommended Art Strategy (Solo + AI)

The neutral art spec uses **SuperGrok isometric unit tokens** on **top-down terrain tiles** (locked in `2026-06-06-deadmanzone-top-down-visual-commitment.md`). Blender is optional for vehicles. Style anchor: `Assets/Grok Images/Isometric/grok-image-0211da6d-2b71-444a-ad30-4781dae097e0.jpg`.

**Practical solo path:**
- Use AI image tools (Flux, Midjourney, etc.) for rapid concepting and color keys.
- For final icons: either lock an extremely consistent prompt/style or move the 5 neutral pieces into the provided Blender template scene (camera, lighting, and palette are already specified).
- Per-cell modular tiles are **required** for clean rotation support later — pure 2D sprite stitching becomes painful.

**Palette lock (do not deviate):**
Worn olive drab, mud-brown leather, dull gunmetal, off-white bandages, faded markings. Grim, practical, field-expedient — never clean or heroic.

---

## Milestone Roadmap (Realistic Solo Pace: 3–5 weeks)

### Milestone 1: Stabilize + Neutral Shop Icons (Target: 3–5 days)

**Goal:** Clean, playable build through fight 3 with both pauses firing and all 5 neutral icons visible/ readable in the shop.

**Step 1.1 — Fix Main Fight pacing (30 min)**
- File: `Assets/_Project/Core/Combat/CombatPacingConfig.cs`
- Change `MainFightTicks` from 200 → **300**.
- Remove any “TEMP (dev)” comments.
- Cursor prompt starter:
  > “Update CombatPacingConfig to exactly match the demo spec values: OpeningTicks = 50, MainFightTicks = 300, BriefPushTicks = 50, TicksPerSecond = 10. Keep the MaxGasTicks safety cap at 10_000.”
- Run `CombatPacingConfigTests` + `CombatSegmentPlaybackTests`.
- Manual verification: Play fight 1 — grind segment should now feel ~30 s wall time.

**Step 1.2 — Full EditMode regression (45–60 min)**
```bash
# Run in Unity or via CLI
Unity.exe -batchmode -nographics -projectPath "." -runTests -testPlatform editmode -testResults TestResults-EditMode.xml -quit
```
Fix any failures immediately (most are minor supply number or schema updates from tutorial economy).

**Step 1.3 — Neutral art icons pipeline (parallel track)**
- Follow `2026-06-05-deadmanzone-neutral-faction-art-design.md` exactly.
- Use the editor menu in `NeutralArtPipelineEditor.cs` to create folders.
- Generate icons in this order (style anchor first):
  1. `conscript_rifleman_icon.png`
  2. `grenade_thrower_icon.png` (1×2 vertical)
  3. `field_medic_icon.png`
  4. `armored_transport_icon.png` (L-shape)
  5. `mobile_cannon_icon.png` (3×2)
- Use prompts from `docs/DeadManZone-Art-Prompt-Pack.md` (v1.1 — isometric tokens).
- Crop from Grok sheets or generate fresh roster row; remove backgrounds before Unity import.
- QA at 48px on dark shop panel and on `FronttileA1` mud terrain.
- Assign icons to the five neutral `PieceDefinitionSO` assets.
- Verification screenshot: `neutral_roster_review.png` (all 5 at full size + 50% scale on dark panel).

**Step 1.4 — Emergency Draft button + feedback (1–2 hrs)**
- Locate manpower gate messaging in `RunHudView` or `RunSceneController`.
- Add a clearly labeled button: “Emergency Draft (once per run)”.
- On click: call `RunOrchestrator.TryEmergencyDraft()`, refresh HUD, show toast “+X Manpower (one-time emergency draft)”.
- Cursor prompt starter:
  > “Add an Emergency Draft button to the Run HUD. Button is only interactable once per run. On press call the orchestrator’s TryEmergencyDraft method and display a clear toast message with the manpower gained.”
- Test: Force a shortfall in fight 1 shop → button appears and functions correctly.

**Step 1.5 — Quick manual smoke test (1 hr)**
- New run → buy 2 normal units from 125 Supplies → fight 1 → both pauses fire → win.
- Repeat through fight 3.
- Note any jank in pacing, manpower messaging, or ability card UX.

**Milestone 1 exit criteria:**
- All EditMode tests green.
- You can comfortably finish fight 3 with both tactic pauses triggering.
- All 5 neutral icons are in the shop and readable.

---

### Milestone 2: Board Visual Polish + Reserves (Target: 4–6 days)

**Goal:** Spatial Reserves feel excellent, rotation is intuitive, and per-cell sprites are working (big visual upgrade).

**Step 2.1 — Zone headers + color strip (1–2 hrs)**
- File: `RunSceneSetup.cs` (board section creation).
- Add three zone header labels (**REAR / SUPPORT / FRONT**) above the 9×10 grid, sized proportionally to column spans 4 : 3 : 2.
- Add a thin horizontal `ZoneStrip` image directly below the grid using the existing rear/support/front tints from `BoardView`.
- Cursor prompt starter:
  > “Add zone header labels REAR, SUPPORT, FRONT above the board grid aligned to the 4/3/2 column spans. Add a thin colored ZoneStrip image below the grid using the same tints already defined for rear/support/front zones.”
- Verify in the Run scene (headers not clipped, strip visible).

**Step 2.2 — Per-cell board sprite foundation (biggest visual task)**
- Follow Phase 3 of the neutral art spec.
- Implement `PieceArtResolver.cs`.
- Extend `PieceDefinitionSO` with optional `cellSprites[]` array + `TryGetCellSprite(localCell, rotation)` helper.
- Update `PieceShapeVisual.cs` (and `ReservesView` / `BoardView` as needed) so that when art exists for a cell it renders the sprite instead of a tinted block. Hide footprint label text when every cell has art.
- Cursor prompt example:
  > “In PieceShapeVisual, resolve the local cell offset from anchor + current rotation. If PieceDefinitionSO has a matching cell sprite, render that Sprite instead of the tinted Image. Hide the footprint label when all cells have sprites.”
- Generate the first modular cell tiles (start with single-cell Conscript, then vertical Grenade Thrower pair). Use the assembly maps in the art spec.

**Step 2.3 — Reserves drag-drop + rotation parity (2–3 hrs)**
- Ensure `ReservesView` / `ReservesTileView` fully mirror `BoardView` drag behavior (including invalid drop snap-back).
- Confirm Q/R rotation works while dragging from shop → reserves and reserves → main board.
- Cursor prompt:
  > “Make sure DragPayload.Rotation is respected on all Reserves drop targets. The drag ghost must preview the rotated footprint correctly on both board and reserves grids.”
- Test extensively with L-shaped and 1×2 pieces.

**Step 2.4 — Shop lock slot visual confirmation (30 min)**
- Lock an offer in the middle slot of any lane.
- Reroll that lane twice.
- Confirm the locked offer remains in the exact same vertical slot (not moved to top).
- This behavior was implemented in the build-screen plan — just verify it survived recent changes.

**Milestone 2 exit criteria:**
- You can place and rotate pieces comfortably in Reserves.
- At least 1–2 pieces show per-cell art on the main board.
- Zone headers and strip are clearly visible and correctly aligned.

---

### Milestone 3: Polish, Feedback & Onboarding (Target: 3–5 days)

**Goal:** A new player can understand the systems and have fun without frustration.

**Step 3.1 — Manpower gate + Emergency Draft messaging**
- When the gate blocks “Start Battle”, show a clear, actionable message:
  > “Need X more Manpower. Sell pieces, move units to Reserves, or use Emergency Draft (once per run).”
- Emergency Draft toast/feedback must feel powerful but clearly limited.

**Step 3.2 — Ability & Tactic card UX**
- Ability cards must clearly display: source piece name, Authority cost (different for Pause 1 vs 2), and “requires living [Piece] on board”.
- Selecting an ability whose source has died should disable the card with an inline reason.
- Tactic radio buttons: gray out “Disciplined Fire” when player HQ is dead.

**Step 3.3 — Gas readability in neutral columns**
- Gas damage / VFX / numbers must be noticeably stronger inside the 7 neutral columns during segment 3.
- Add subtle ground tint or particle emphasis if needed (keep simple).

**Step 3.4 — Battle report polish**
- Top-3 dealt and taken lists should feel satisfying (piece icons + names + clean numbers).
- “Continue” button must feel like the natural, obvious next action back to the shop.

**Step 3.5 — Save / resume stress test (critical)**
- Save at pause 1, exit to desktop, reload, submit identical tactic + ability → outcome must be identical.
- Repeat after pause 2 and during the gas segment.
- Cursor prompt if issues arise:
  > “Extend the mid-combat save blob to include current SelectedTactic and any pending abilities. On resume, re-apply them exactly so the deterministic sim produces the same event log.”

**Milestone 3 exit criteria:**
- A brand-new player (or you role-playing one) can finish fight 3 without getting stuck on economy, placement, or “what do I do during the pause?”

---

### Milestone 4: Balance Validation + Full Campaign (Target: 3–4 days)

**Goal:** Hit the tutorial pause #2 metric and confirm the entire 10-fight gauntlet is tense but fair.

**Step 4.1 — Run TutorialBalanceTests**
- In Unity Test Runner filter to `TutorialBalanceTests`.
- If any fight drops below 90% reach rate, **only** adjust enemy template placement or unit count (content-only change). Never add fight-index damage/HP modifiers.

**Step 4.2 — Full 10-fight manual campaign playtest (most important verification)**
- Play with Iron Vanguard.
- Try at least two distinct builds (one economy/building heavy, one aggressive with early big units).
- Document:
  - Where economy-vs-combat tension feels strongest.
  - Whether both pause windows are meaningful.
  - Gas tension in neutral columns.
  - Any “I have no idea what this button does” moments.
- Adjust only enemy templates or minor Authority costs if needed.

**Step 4.3 — Faction unlock flow**
- Win a full run → confirm Dust Scourge and Cartel of Echoes unlock on the Main Menu and are selectable for a new run.

**Milestone 4 exit criteria:**
- You (or a friend) can finish a complete 10-fight run in under 40 minutes.
- The experience feels tense, readable, and fair.

---

### Milestone 5: Demo Packaging & External Validation (Target: 2–3 days)

**Goal:** Something you can confidently hand to another human for useful feedback.

**Step 5.1 — Achievements & local leaderboard smoke test**
- Trigger the key achievements (`clear_gauntlet`, `win_no_hq_damage`, `critical_mass_five`, `salvage_hundred`, `perfect_morale_victory`).
- Confirm they pop, persist across runs, and appear correctly in the Main Menu panels.

**Step 5.2 — Build settings & clean-machine test**
- Set sensible defaults (resolution, quality, fullscreen).
- Test the packaged build on a completely clean machine / different PC (persistent data path must work, no missing references).

**Step 5.3 — Update demo documentation**
- Update / create `docs/demo-guide.md` with:
  - How to generate demo content (Editor menus).
  - Known issues / limitations.
  - Recommended first-play build path.
  - Controls (drag, Q/R rotate, pause menu, etc.).

**Step 5.4 — External playtest (even 1–2 people is valuable)**
- Give them the build + a short one-page “what to look for” sheet focused on:
  - Economy tension & manpower gate clarity
  - Pause window usage
  - Neutral icon readability
  - Save/resume reliability
  - Overall fun / frustration points
- Collect notes ruthlessly and prioritize fixes.

**Step 5.5 — Final commit + tag**
```bash
git add -A
git commit -m "chore: demo candidate v0.9 ready for external playtest"
git tag v0.9-demo-candidate
```

---

## Recommended Weekly Solo Rhythm

- **Monday**: Pick 1–2 tiny steps from the current milestone.
- **Tuesday–Thursday**: Execute + art generation in parallel.
- **Friday**: Full EditMode regression + one complete (or abbreviated) 10-fight campaign playtest.
- **Sunday evening**: Quick review of the GDD success criteria checklist — mark what is actually verified in practice.

**Risk Log (keep in a personal note)**
- Art style drift across the 5 neutrals (and future factions).
- Pacing feel regression after any movement/attack speed or stat changes.
- New-player onboarding friction on the four resources + spatial placement (biggest tutorial risk).
- Scope creep (“just one more small feature…”).

---

## Final Senior Lead Notes

You already have the hardest engineering work done: a deterministic core sim, spatial Reserves with rotation, a meaningful two-pause command system, and genuine economy-vs-war tension.  

The remaining work is **visible polish, art integration, and validation** — exactly what turns a prototype into something that feels like a real game.

**Prioritize Milestone 1 this week.** Once you have all 5 neutral icons in the shop and both pauses reliably firing in fights 1–3, the project will suddenly feel dramatically more alive and motivating.

Break tasks into the tiny steps above. Use Cursor for every code change. Generate art in parallel. When you hit any single stuck point, paste the exact file + error here and I will give you the next precise move.

You’ve got this. Ship the demo.

---

*Document generated from senior game dev lead analysis of the June 2026 GDD and all attached subsystem specs/plans.*