# DeadManZone — Demo Art & Visual Checklist

**Quick reference version** of the art tasks from the main roadmap. Print or keep open while working.

---

## Priority Order (Do in This Sequence)

### Phase 1 – Milestone 1 (Highest Impact)
- [ ] Generate **Conscript Rifleman** shop icon (256×256) — style anchor
- [ ] Generate **Grenade Thrower** shop icon (1×2)
- [ ] Generate **Field Medic** shop icon (1×1)
- [ ] Generate **Armored Transport** shop icon (L-shape)
- [ ] Generate **Mobile Cannon** shop icon (3×2)
- [ ] Assign all 5 icons to their `PieceDefinitionSO` assets
- [ ] Test readability at shop card size (~48 px) on dark panel
- [ ] Screenshot: `neutral_roster_review.png`

**Status:** ___ / 5 icons done

---

### Phase 2 – Milestone 2 (Board Readability)
- [ ] Generate per-cell modular sprites for the 5 neutral pieces
  - [ ] `infantry_cell.png` (Conscript)
  - [ ] `grenade_upper.png` + `grenade_lower.png`
  - [ ] `medic_cell.png`
  - [ ] Armored Transport modular set (`vehicle_cab`, `vehicle_hull`, `vehicle_track`, `vehicle_rear`)
  - [ ] Mobile Cannon modular set (`cannon_barrel`, `cannon_carriage`, `cannon_wheel`)
- [ ] Wire `PieceArtResolver.cs` + update `PieceShapeVisual.cs`
- [ ] Support rotation on per-cell sprites
- [ ] Test on actual board with rotation (Q/R)

**Status:** ___ / 5 pieces have working per-cell art

---

### Phase 3 – Polish (Only After Core Art)
- [ ] Basic zone color tints (rear / support / front) already in `BoardView`
- [ ] Subtle trench / ground texture or grid lines for battlefield background
- [ ] Shop panel background + card styling (via `UiThemeSO` or equivalent)
- [ ] Button and panel polish (hover, pressed, disabled states)
- [ ] Gas VFX emphasis in neutral columns
- [ ] Drag ghost sprites (optional but nice)

**Status:** ___ / Polish items done

---

## When to Work on Backgrounds, Shop UI & Battlefield Look

| Visual Area | When to Do It | Priority for Demo | Notes |
|-------------|---------------|-------------------|-------|
| **Zone tints (rear/support/front)** | Early – Milestone 1 | Medium | Already partially implemented in `BoardView`. Make sure they look good behind your new icons. |
| **Shop panel background + card style** | Milestone 1 (parallel) | High | Icons need to sit on something readable. Use `UiThemeSO` or the "Create Default UI Theme" editor menu. |
| **Battlefield ground / subtle trench texture** | Milestone 2 | Medium | Improves readability of pieces and gas. Keep subtle — don't fight the sprites. |
| **Button / panel polish (hover, disabled, etc.)** | Milestone 3 | Medium | Part of overall "feels good" polish. Not required for success criteria. |
| **Gas VFX / neutral column emphasis** | Milestone 3 | Medium | Makes the final segment feel dangerous. Visual feedback for contested ground. |
| **Full custom UI theme / fancy borders** | After demo success criteria met | Low | Nice-to-have. Can wait until post-demo. |
| **Drag ghost sprites** | Milestone 2–3 | Low | Helpful but not critical. Tinted blocks work for testing. |

**Rule:**  
Finish the **5 neutral shop icons** and **per-cell sprites** before spending significant time on fancy backgrounds or full UI theme. The piece art gives 80% of the visual improvement for the demo.

---

## Quick Daily Checklist (While Working)

**Today I will:**
- [ ] Do 1–2 code steps from the current milestone
- [ ] Generate or refine **at least one** art asset from the priority list above
- [ ] Test the new art in the actual game (shop or board)
- [ ] Run EditMode tests at the end of the day
- [ ] Note any visual issues (readability, contrast, silhouette)

---

## Files & Folders Reference

Shop icons go here:
`Assets/_Project/Art/Neutral/Renders/Icons/`

Per-cell / modular sprites go here:
`Assets/_Project/Art/Neutral/Renders/Cells/`

Assign in:
`Assets/_Project/Data/Resources/DeadManZone/Pieces/*.asset`

UI theme / panel styles usually live in:
`Assets/_Project/Presentation/Visual/UiThemeSO.cs` (or equivalent setup menu)

---

**Print this page.** Keep it next to your monitor while generating art and working through the roadmap.

You now have everything you need:
- Main Roadmap (detailed steps + timing)
- Art Prompt Pack (copy-paste prompts)
- This Checklist (daily / priority overview)

Stay focused on the 5 neutral pieces first. Everything else is secondary for hitting demo success criteria.

Good luck — you're making excellent progress.