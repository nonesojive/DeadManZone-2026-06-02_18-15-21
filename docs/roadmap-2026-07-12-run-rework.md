# Run-rework roadmap (2026-07-12 grilling session)

Product of the 2026-07-12 design grilling. Domain language: CONTEXT.md (Dread, Boss Fight, Fight Option, Front Report, Battle Condition, Twist, Break/Rout, Manpower, Rarity, Arena Theme). Structural decisions: ADR-0004 (Dread run clock), ADR-0005 (per-unit morale / rout economy). Sequencing decision: scoped deletion pass first, then features in dependency order, each refactoring what it touches — no big-bang audit.

## M0 — Deletion & seams — **DONE 2026-07-12**

Shipped same-day: 51 files deleted (2D renderer stack, TacticPausePanel, Synty bar factory/health bars, 2D scene+bootstrap+tests, 2D art SOs+assets), mode gates collapsed (ToonInk3D is the only renderer; `ResolveCombatArenaScene()` is zero-arg), RunState v9 (obsolete `PlayerBoard` + singular `LockedOffer` dropped; v8 saves migrate + stamp), `SeedStreams` named sub-streams landed (golden-value locked; combat + shop seeds derive through it — the old arithmetic collided across systems), presentation-only `UnityEngine.Random` uses audited clean. Suites 338 EditMode / 14 PlayMode green; full run loop smoke-verified. Notables: `CombatArenaBuildingSpawner` renders buildings as placeholder cubes (2D building sprites deleted — needs a 3D building treatment, tracked for M4/art), `Data/` 2D animation-strip classes + `PieceDefinitionSO` 2D fields deferred (touch every piece asset; fold into M3's card/rarity asset pass).

### Original scope (for reference)

- Execute the 2D switchover deletion list (audit doc §6): 17 CombatArena2D files, CombatUnitVisual2D, Synty bar factory, TacticPausePanel, CombatArena2D scene + bootstrap, the replay tests' 2D-config pin.
- RunState cleanup: delete obsolete `PlayerBoard`, reconcile `LockedOffer` vs `LockedOffers`, save schema v9 + migration test.
- **Named RNG sub-streams** in Core: `hash(RunSeed, systemName, roundIndex)` per consumer (shop, options, themes, boss permutation, pity, combat). Order-independent determinism — every later milestone consumes this. Grep-and-fix any `UnityEngine.Random` feeding gameplay decisions.
- Exit: suites green, a full run plays with zero 2D-path code.

## M1 — Dread run skeleton — **DONE 2026-07-12**

Shipped same-day: Dread + BossesDefeated on RunState (schema stays v9, additive), `DreadRules` (per-win 2 interim, thresholds 6/12/18, `FightEquivalent` difficulty mapping), `ICombatRuleModifier` seam in TickCombatRun (applied after fight-start engines; restore path re-applies via `CombatSaveState.ActiveTwistId`), `TwistCatalog` (endless_muster +30% HP / iron_discipline +1 armor step / deathless_cold +60% front-rank HP), `BossRoster` (3 code-authored bosses × 3 stage loadouts from proven template placements; seeded Fisher-Yates order derived from RunSeed, never persisted), boss branch in BeginCombat + win/loss resolution in CompleteCombat (3rd boss → Victory via the old machinery; the fixed 10-fight victory check is deleted), template clamp (the old lookup silently WRAPPED difficulty to fight 1 past fight 10), morale/shop difficulty migrated to FightEquivalent, run HUD meta strip (Dread clock bottom-left, red boss warning, seed bottom-right; BOSS chip on boss rounds), GUID leak fixed in the orders window (`AvailableCommand.SourceDisplayName`). Suites 355 EditMode / 14 PlayMode; live-verified: normal win → Dread 4→6 → boss pending revealed (Crimson Marshal) → boss fight begun with twist + salvage keyed to crimson_legion.

**Flags for later milestones:**
- **Boss loadouts run HOT**: a probed max-strength combined-arms player board only *drew* with the stage-3 army (mono-unit walls lose outright). Tune stage strengths against real player boards in the M2 balance pass.
- **Draw semantics**: mutual annihilation reports `PlayerWon=true` — pre-existing Core semantics; decide whether a drawn boss counts as defeated (currently: yes).
- Crimson Legion / Ash Wraith bosses wear IronMarch pieces and have **empty salvage pools** until their piece content lands.
- Enter-seed UI deferred to the M6 menu restyle (seed display shipped); CombatFightBanner still says "FIGHT N" on boss rounds — boss banner is M2/M6 polish.

### Original scope (for reference)

- Dread on RunState; thresholds (tune from 6/12/18); threshold ⇒ mandatory Boss Fight; third boss ⇒ Victory. Losses/bosses grant no Dread.
- FightIndex → Dread migration everywhere difficulty-curves (enemy strength, muster/salvage scaling; `MoraleCalculator`'s fightIndex scale until M5 deletes it).
- Rule-modifier seam in the sim (one hook; content decks come later) + boss framework: boss = identity (commander, twist, pool) × 3 stage loadouts; seeded hidden permutation.
- Boss content v1: three pool commanders (Neutral Militia, Crimson Legion, Ash Wraiths); rifleman-fallback visuals acceptable until modeled.
- Run HUD Dread meter; seed display + enter-seed on run start (surface for the M0 streams).
- Exit: variable-length run, winnable by three bosses, boss order differs per seed.

## M2 — Fight Options + Front Report — **DONE 2026-07-12**

Shipped same-day: `FightOptionRecord`/`FightOptionTier` on RunState (schema stays v9, additive), `FightOptionGenerator` (three independent seeded army rolls per round via SeedStreams("options", FightIndex); ±1 template band around FightEquivalent; slot 0/1/2 = easy/normal/hard; hard rolls its Battle Condition), `ConditionCatalog` (entrenched_foe / veteran_cadre / storm_barrage / iron_resolve) + `RuleModifierCatalog` unifying twist+condition resolution through the M1 seam, easy tier = enemy fight-start engines suppressed (`TickCombatRun` flag, persisted as `ActiveTier`, restore-deterministic), easy Authority debit (2) into the combat snapshot, hard victory package (+15 supplies / +6 manpower ≈ half a muster), Dread per tier 1/2/3, options persist across save/load and regenerate only after a fight (re-fought losses see the same fronts), boss rounds carry an empty option set. Front Report panel in the Build screen's bottom band: three cards (tier, pool, coarse strength band vs. current army, stakes, hard's condition in amber), click-to-choose with leather highlight, COMBAT gated until a front is chosen, boss rounds show a single red boss report. Suites 386 EditMode / 14 PlayMode; live-verified: choose hard → condition (iron_resolve) into the sim → win → +3 Dread + spoils paid → fresh options rolled.

**Deferred within M2 (content passes, tracked):**
- **Enemy templates still form no synergies** — normal tier ≈ easy in practice until the enemy factory is re-authored with adjacency-aware layouts (the engines run; content never triggers them). Own content pass.
- **Recon intel ladder**: option records already carry full data and the UI deliberately shows only the coarse band — the recon-piece content that unlocks deeper intel (counts → composition → next boss identity) is a piece-content pass.
- Pools all read IRONMARCH UNION until non-IronMarch template content lands (generator is pool-ready).

### Original scope (for reference)

- Three seeded options per round (independent army rolls; pools may repeat), persisted on RunState; regenerate only after a fight resolves.
- Economy: easy debits the round's Authority pool (~2–3) pre-combat; hard grants +1 Dread over normal + materiel package (~half a muster) on victory. Dread values: easy 1 / normal 2 / hard 3.
- Enemy loadout flags: easy = fight-start engines suppressed for enemy side; normal = engines on; hard = engines on + one Battle Condition (visible in the report by default). First condition deck (3–5 cards) through the M1 rule-modifier seam.
- Content: enemy templates re-authored to actually form synergies (normal tier's real delta — the engines already run).
- Front Report panel (grimdark kit) with default intel (pool, strength band, stakes); recon intel-ladder seam (+1 recon piece to prove it). Boss report replaces options at threshold.
- Exit: every round is a legible three-way choice with Dread/economy consequences.

## M3 — Rarity + pity + card visual — **DONE 2026-07-12**

Shipped same-day: `Rarity` enum (append-only) on PieceDefinition/SO with all pieces assigned by design role through BOTH content factories (the ability-grant lesson — SavePiece writes the param) and stamped onto the 17 shipped assets directly (full regen avoided: it wipes post-gen asset touches); Dread-keyed weight TABLE (80/18/2 at FE1 → 55/30/15 at FE9+) with per-offer tier roll + down-only lane fallback; appear-reset pity (`RarePityBatches` on RunState, +4%/rare-less batch fed from the Common share, hard guarantee at 9 batches / 40% cap, both Generate call sites, rare-or-above satisfies, locked slots count as appearing); salvage quality hook (`SalvageHardBoost` after a hard-front victory upweights salvage picks Rare×3/Uncommon×2 for the following build round — salvage-source picks stay exempt from the tier gate otherwise); shop card rarity chrome (brass Rare / muted-olive Uncommon name tints + strip above the name band; commons stay quiet). Suites 406 EditMode / 14 PlayMode. Live bonus verification: same run seed reproduced the identical shop AND identical fight options — the seeded-run guarantee end-to-end.

**Notes:** command_outpost assigned Common (lane floor — the defensive lane needs a Common or early rolls always hit the fallback); `SalvageShopPool.cs` found dead (zero callers), delete in the next cleanup; the broader shop/menu grimdark sweep stays M6 (only cards shipped here, per the cards-with-rarity rule); hover/board piece cards get their rarity treatment in M6.

### Original scope (for reference)

- Rarity enum (Common/Uncommon/Rare; append-only, room for a 4th), assignments for all pieces (design role, not raw power), shop weight table keyed by Dread, salvage-quality hook (hard fights = targeted shot at a pool's rares).
- Pity: counter per generated offer batch (initial roll and each reroll both count), +step to rare odds per rare-less batch, reset on rare APPEARING, hard guarantee at the cap, rare-or-above satisfies. State on RunState (seeded).
- Card template redesign with rarity frames (saturation budget; frames lean brass/bone, never near side-channel blue/red) + shop skin sweep — cards and rarity ship together.

## M3.5 — Faction starting loadouts — **DONE 2026-07-12** (owner addition)

Each faction opens every run with a few pre-placed pieces (free; upkeep applies) — early-game combat feel + a nudge toward faction identity. `FactionSO.startingPieces` (pieceId + preferred anchor; board resolved by piece category; orchestrator scans forward from illegal anchors, never blocks a run), applied in StartNewRun BEFORE muster so economy pieces can feed the first muster. IronMarch authored: supply_depot + command_outpost (HQ), field_medic + conscript_rifleman adjacent (combat). Plumbed through the content factory AND stamped on the shipped asset. Side effect: the Front Report strength band works from round one (a real army to measure against). Blank-slate test premises updated (`RemoveStartingLoadout` helper); suites 408 EditMode green. Dust Scourge / Cartel loadouts: author with their content passes.

## M4 — Arena Themes wave 1 (independent after M0) — **DONE 2026-07-12**

- Theme framework: pool → home-theme-set keying; theme rolls seeded from the chosen option's pool; bosses on their pool's signature ground. Scene-per-theme via the existing `ResolveCombatArenaScene` branch point; `CombatEnvironmentBuilder` parameterized per theme.
- Ship 3–4 of the canonical six (Trenchline exists; pick from Forest / Ravaged Town / Trench-dressing / Siege Ground / Fog Field). All dressings — camera, flat strip, value structure never change.

**DONE notes (2026-07-12).** Shipped four: Trenchline + Fog Field + Ravaged Town + Wartorn Forest. Core: `ArenaThemes` (ids, pool→home set, pool→signature, seeded `Roll` on its own "arenaTheme" sub-stream keyed (round, slot) so pre-M4 seeds' army/condition rolls are untouched, `Normalize` for legacy nulls). Theme rolls at Front Report generation, stamped additively on `FightOptionRecord.ThemeId`; `BeginCombat` stamps `CombatSaveState.ArenaThemeId` (boss = signature ground); schema stays v9. `GameScenes.ResolveCombatArenaScene(themeId)` maps theme→scene (`CombatArena3D` stays Trenchline; `CombatArena3D_<Theme>` for the rest, all in Build Settings); the loader falls back to Trenchline if a theme scene is missing from a build, and unloads by scanning `AllCombatArenaScenes` (re-resolving from run state could pick the wrong lingering scene on the defeat path). Editor: `ArenaThemeProfile`/`CombatArenaThemeProfiles` (lighting/fog/palette/craters/prop toggles — the profile deliberately has NO camera/strip/value knobs), builder dispatches per-theme extras on ThemeId; the menu item builds all four scenes; `BuildThemeScene(themeId)` is public for one-theme iteration. Suites 423 EditMode / 14 PlayMode green; live smoke: seed 20260712's normal front rolled ravaged_town, loaded `CombatArena3D_RavagedTown`, fight played through a checkpoint pause.

**Judgment calls & flags:**
- The 11 shipped enemy templates ALL carry pool id `ironmarch_union` (the canonical pools exist only on bosses), so `ironmarch_union` got a home set (Trenchline/Ravaged Town/Wartorn Forest) to keep wave 1 visible in normal fights. When the balance pass re-authors templates onto neutral/crimson_legion/ash_wraiths, drop that entry.
- Keying: neutral → Trenchline (signature) + Ravaged Town; crimson_legion → Ravaged Town (signature) + Trenchline/Wartorn Forest; ash_wraiths → Fog Field (signature) + Wartorn Forest.
- Home-set ORDER is roll-visible (seeded index) — reordering a set changes every seed's themes; append only.
- Visual-iteration gotcha: the Game View screenshot tool can serve a STALE buffer after editor-script scene rebuilds — trust `screenshot-camera`.
- Theme palette lesson ×2: anything mid-frame under the 1.7x warm key must sit at/below the ground's dry tone (town bags needed a theme-local dimmed material; shared sandbag/crate colors only survive at frame edges).

## M5 — Morale & rout (own milestone, after M1/M2 stabilize; ADR-0005)

- Per-unit Morale bar, terror damage channel, Break ⇒ rout (flee field, not a kill); side defeated when no unbroken living units.
- Economy: enemy routs grant no salvage roll; player routs cost no Manpower and return to their slot next round. Manpower becomes run health; run-level Morale and `MoraleCalculator` deleted; casualties deduct Manpower directly.
- Event-log actions + presentation (flee move reusing march/dissolve machinery; vehicles collapse-abandon) + first terror content (Cartel identity hook).

## M6 — UI restyle sweep (anytime after M0; cards/shop already done in M3)

- Promote `CombatGrimdarkSkin` to the game-wide kit; sweep main menu, settings/options, run HUD, battle report. Restyle, not redesign — layouts and flows keep.

## Standing rules

- Every milestone that touches RunState bumps the save schema with a migration + test (v8→v9 in M0; further bumps per milestone).
- Determinism is a standing invariant: new randomness only through named sub-streams; seeded-run equality is a test target, not a hope.
- Content debt from ADR-0004: anything still keyed to FightIndex after M1 is a bug.
