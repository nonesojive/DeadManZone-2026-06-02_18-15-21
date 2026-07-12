# Shop visual refresh handoff — session end 2026-07-12 (evening)

**State:** branch `shopvisualrefreshv1` (fresh off `combatvisualv5`, which is pushed). Working tree clean. This session's commits, in order: `c1963b4b` (M4 arena themes wave 1), `dabc7ee0` (M4 stale-arena regression fix), `1fad3685` (roadmap flag), `898298f1` (M5 morale/rout), `5da594fb` (balance pass wave 1), `76cf2d96` (M6 grimdark UI sweep), `4f6d966d` (facing/tiles/speed playtest fixes), `27db8ddf` (1x default + tray sorting). Suites green: **446 EditMode / 14 PlayMode**. Owner playtested through the day; systems feel good.

**Read first, in order:** CLAUDE.md → `docs/roadmap-2026-07-12-run-rework.md` (M0–M6 all DONE with judgment-call notes — the M3, M6 and balance-pass sections matter most here) → `CONTEXT.md` (glossary; **Rarity** and **Accent** entries are the law for card chrome) → this file.

## Where the game is now

All roadmap milestones through M6 are shipped: Dread clock, Fight Options, rarity+pity shop, faction starting loadouts, four arena themes (scene-per-theme), per-unit morale/rout with the rout-vs-kill economy (Manpower is run health, schema v10), the game-wide grimdark UI kit, and a balance pass (templates form synergies 138→804, boss stage-3s trimmed, death shock 6). Combat has spawn facing, a 1x/2x/4x speed button (always opens 1x), and the build boards use procedurally baked grimdark tiles (mud/duckboard combat, concrete HQ, riveted steel reserves — `DeadManZone → Art → Bake Grimdark Board Tiles`).

## The next job: shop visual rework

The shop column is now the most off-kit region of the game. Everything around it went grimdark (M6) and the boards under it went dark baked tiles, which makes the M3-era shop chrome read as a different game. Specific targets, all observed live this session:

1. **Offer cards** — the shipped rarity frames render bright GREEN sci-fi chrome. The M3 spec said frames "lean brass/bone, never near side-channel blue/red" but what's on screen doesn't honor it against the new kit. Re-evaluate card frame/binding art entirely against `CombatGrimdarkSkin` (Bone/leather/BandDark/VictoryGold accents; rarity tiers should read by VALUE and ornament, not hue shout). Cards are runtime-composed — find the card builder before touching anything (M3 shipped "card template redesign with rarity frames"; its builder carries a version/rebuild mechanism like RunHudPanelBuilder's PanelVersion).
2. **Shop panel background** — `ShopBackgroundBootstrap.ApplyToBuildPanel` (called from `RunBuildUiBootstrap.Apply`, structural side — note it self-gates on the authoring lock; the recolor-only pass pattern from M6 is the precedent if the lock blocks it).
3. **REROLL button** — accepted M6 straggler: keeps its authored metal-plate sprite (it lives in the shop panel, not the chrome bars `StyleBarButtons` sweeps).
4. **Sell zone / trash can** — the white trash-can card bottom-right of the board is glaring against the dark tiles now. `SellZoneVisualBootstrap` (self-gates on the lock too).
5. **Critical-mass drawer contents** — white icon swatches + unstyled text (header is `theme.textPrimary`). The drawer chrome got a sorting fix this session (`CriticalMassDrawerBootstrap.EnsureSortingIsland`, order 260) but its interior was flagged out of M6 scope.
6. **Front Report band cards** — kit-built already, but sanity-check them next to whatever the new card language becomes (they're the fight "offer cards"; visual kinship with shop cards would be a win).
7. **Drag ghost / shop piece preview** (`DragGhost`, `ShopPiecePreview`, `PieceShapeVisual`) — verify they inherit whatever the cards become; they pull `TryGetCellSprite` art and card colors.

Screenshot-heavy visual iteration — budget accordingly, and get the owner's eyes on a first card mock EARLY before sweeping all chrome.

## Working agreements (kept all session; keep them)

- **Agent split**: subagents do Core/Game/Data/Presentation FILE work with tight specs (no Unity MCP, no bash edits, no .asset/.meta/.unity edits); the parent session owns the editor — compile via `assets-refresh`, suites via `tests-run` (needs all scenes saved), live smoke via `script-execute` driving `RunManager` (reflection-invoke the orders window's private `TryResume`), screenshots to verify. Direct agents; verify everything yourself live.
- **Stamp, don't regen**: `IronmarchUnionContentFactory.Generate()` DELETES and recreates piece assets (wipes post-gen icon/model refs). Template-only changes go through `DeadManZone → Content → Regenerate Enemy Templates Only`. Factories still carry every authored value.
- **Save schema v10**; additive-with-defaults or bump+migration+test.
- **Determinism**: new randomness ONLY via `SeedStreams` named sub-streams; difficulty keys off Dread.

## Hard-won gotchas (this session's additions on top of the old list)

- **`RunUiAuthoringLock` gates ALL structural UI migration** on the Run scene's build panel — and `ShouldSkipVisualMigration`'s `isPlaying` parameter is DEAD (alias of ShouldPreserve; don't trust its name). M6 precedent: recolor-only passes run at runtime BEFORE the preserve gate (`RunBuildUiBootstrap.ApplyGrimdarkSkin`) — colors/sprites only, zero geometry. Extend that pass rather than fighting the lock.
- **Canvas sorting map** (violations paint the wrong thing on top): base run canvas ~0 → Front Report overlay **250** → critical-mass drawer island **260** → run meta strip **300**; combat speed control overlay 60; army HUD its own overlay. New shop overlays must slot BELOW 250 or be nested in the base canvas.
- **Overlay canvases must be TOP-LEVEL GameObjects** (rect inheritance collapses them otherwise) — but a *nested* Canvas with `overrideSorting` is fine as a sorting island (the drawer fix).
- **Icon-flatten heuristic**: the M6 strip recolor keeps sprites that are ≤72px and roughly square (icons), flattens the rest. Oblong authored icons will flatten — name-exempt or squarify.
- **Game View screenshots can serve a STALE buffer** after editor-script scene rebuilds — `screenshot-camera` renders fresh; for UI verification, re-enter play mode or take the shot after real interaction.
- **PanelVersion bumps**: RunHudPanelBuilder is at 6; card/shop builders have their own rebuild stamps — bump when output changes or authored panels keep the old build.
- Pre-existing: OneDrive repo = file tools only (bash `rm` OK for whole-file deletes); `script-execute` >15s times out AND the MCP retries it (long editor ops must be idempotent); editor menus via `EditorApplication.ExecuteMenuItem`; `tests-run` throws on dirty scenes; `BoardState.TryPlace` fails silently (assert `.Success`); fresh runs carry the starting loadout (`RemoveStartingLoadout` helper for blank-slate tests); the sandbox git can't read this repo's index (newer index extension) — commit via a desktop shell.
- `Assets/_Recovery*`, `Untitled.blend*`, `*.prebranch.bak` = pre-existing untracked noise; exclude from commits.

## Queued after the shop refresh (unchanged)

Content passes: enemy pools re-authored onto neutral/crimson_legion/ash_wraiths (unlocks real arena-theme keying — drop the `ironmarch_union` entry in `ArenaThemes` — but REQUIRES salvage targeting to survive pools without pieces; see balance-pass director notes), Dust Scourge/Cartel factions (Cartel owns the terror identity; mg nest carries it meanwhile), recon intel ladder UI, 3D building visuals (placeholder cubes since M0), boss commander models, enter-seed UI. Owner-driven tuning: death-shock feel (MoraleRules), DreadRules/RarityWeights constants, dense-blob cascade watch.
