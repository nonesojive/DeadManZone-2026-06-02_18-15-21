# Run-rework handoff — session end 2026-07-12

**State:** branch `combatvisualv5`, working tree clean. Session commits, in order: `3820482f` (main-build 3D port + fixes + vehicle units), `6d30ce8e` (M0), `1f57a81c` (M1), `4627bdb7` (M2), `212c4498` (M3), `fc3d2080` (M3.5). Suites green: **408 EditMode / 14 PlayMode**. Owner playtested the full new loop and signed off: systems and loops feel good; a **balance pass is wanted** (enemy templates + some piece stats) but is not urgent-blocking.

**Read first, in order:** CLAUDE.md → `docs/roadmap-2026-07-12-run-rework.md` (the plan of record — every milestone has a DONE section with judgment calls and flags) → `CONTEXT.md` (domain glossary: Dread, Boss Fight, Fight Option, Front Report, Battle Condition, Twist, Break/Rout, Manpower, Rarity, Starting Loadout, Arena Theme) → ADR-0004 (Dread run clock) and ADR-0005 (per-unit morale/rout — designed, NOT yet built).

## Where the game is now

The run is the ADR-0004 design, live and playtested: variable-length, player-paced. Each build round shows a **Front Report** (three seeded fronts: easy costs 2 Authority from that fight's command pool / normal / hard grants +3 Dread + spoils and discloses its **Battle Condition**); winning banks Dread (1/2/3 by tier); thresholds 6/12/18 trigger mandatory **Boss Fights** (3 pool bosses × 3 stage loadouts, seeded hidden order, each with a Twist through the shared `ICombatRuleModifier` seam); third boss = Victory. Shop rolls rarity from a Dread-keyed weight table with an appear-reset **pity** timer (+4%/rare-less batch, guarantee at 9) and a hard-victory salvage boost. Factions open with a **Starting Loadout** (IronMarch: depot + outpost / medic + rifleman). Seeded runs are real end-to-end: same seed ⇒ same shops, fronts, conditions, boss order (all randomness flows through `SeedStreams` named sub-streams; golden-value locked — changing the hash breaks saves/seeds). Combat renders only through the ToonInk3D arena (2D path deleted in M0); vehicles (iron horse / transport / MG nest) have static-mesh visuals via `CombatUnitVisual3DVehicle`.

## Next up (in intended order)

1. **M4 — Arena Themes wave 1** (spec in the roadmap): pool → home-theme-set keying, theme rolls seeded from the chosen option's pool, bosses on signature grounds. Scene-per-theme via `GameScenes.ResolveCombatArenaScene` (the marked branch point) + a parameterized `CombatEnvironmentBuilder`; ship 3–4 of the canonical six (Trenchline exists). Screenshot-heavy visual iteration — budget accordingly.
2. **Balance pass** (owner-requested, can interleave with M4): enemy templates re-authored to actually form synergies (the engines run; content never triggers them — normal tier ≈ easy until fixed), boss stage loadouts run HOT (a probed max-strength board only drew vs stage 3), piece stat tuning per playtests, and decide whether mutual annihilation should keep counting as a boss kill (`PlayerWon=true` on draws — pre-existing semantics). Tuning constants are all commented "M3 initial/M2 initial, tune in playtest": `DreadRules` (thresholds, tier dread, easy cost, hard package), `RarityWeights` (table + pity).
3. **M5 — Morale & rout** (ADR-0005, biggest Core rework — own milestone).
4. **M6 — UI restyle sweep** (promote `CombatGrimdarkSkin` game-wide; cards/shop rarity chrome already shipped in M3).
5. Content passes queued in roadmap notes: recon pieces (intel ladder seam exists; UI shows coarse band only), non-IronMarch pool pieces (bosses wear IronMarch pieces + empty salvage pools until then), Dust Scourge/Cartel starting loadouts, 3D building visuals (buildings render as placeholder cubes since M0), boss commander models (rifleman fallback), enter-seed UI (display shipped; entry deferred to M6).

## Working agreements that produced this session (keep them)

- **Agent split**: subagents do Core/Game/Data-code file work with tight specs (no Unity MCP, no bash edits, no .asset edits); the parent session owns the editor — compile via `assets-refresh`, suites via `tests-run`, live smoke via `script-execute` driving `RunManager` (reflection-invoke the orders window's private `TryResume` to advance pauses), screenshots to verify.
- **Stamp, don't regen**: content factories carry every authored value (the SavePiece dropped-parameter lesson), but shipped .asset changes are applied directly via editor script — full `Generate()` wipes post-gen asset touches.
- **Save schema**: still v9; every addition this session was additive-with-defaults. Bump + migration + test the moment a field changes meaning or shape.
- **Determinism invariants**: new randomness ONLY via `SeedStreams.Derive/Stream(runSeed, streamName, index, subIndex)`; anything difficulty-curved keys off Dread (`DreadRules.FightEquivalent`), never FightIndex (a plain counter now).

## Hard-won gotchas (cost real time this session)

- **Overlay canvases must be TOP-LEVEL GameObjects** — nesting under any UI transform inherits the parent rect (bit us twice: army HUD invisible, Dread strip wedged into the top bar). See `CombatArmyHealthHud` / `RunHudView.EnsureMetaStrip` comments.
- OneDrive repo: **file tools only** for reads/edits; bash `rm` acceptable for whole-file deletes; never bash cat/sed/python edits.
- `script-execute` >15 s times out AND the MCP retries it — long editor operations must be idempotent (scene builds are; they ran 5× harmlessly).
- Editor menus via `EditorApplication.ExecuteMenuItem` inside script-execute (reflection-method-call can't find static editor methods).
- `BoardState.TryPlace` fails SILENTLY — always assert `.Success` in fixtures (empty-fixture bug hit again this session in a twist test).
- **Blank-slate test premises are dead**: fresh runs now carry the starting loadout — use `RunOrchestratorTests.RemoveStartingLoadout()` (or filter `start_` instance ids) before staging boards.
- Boss/steamroller fixtures: mono-unit walls LOSE to late armies — mirror `GetUpcomingEnemyBoard()`'s comp ×2 + rifleman filler (see `DreadBossRunTests.SaveSteamrollerBoard`), and set Dread/BossesDefeated BEFORE building the fixture so it mirrors the right board.
- The player's 6×6 build board has no zone restrictions; the 17×6 combat STRIP (TestBoards.Layout) rejects Units in Rear columns (≤3) — different boards, different rules.
- `Assets/_Recovery*`, `Untitled.blend`, `*.prebranch.bak` are pre-existing untracked noise — excluded from every commit; delete or gitignore when convenient. `SalvageShopPool.cs` is confirmed dead code awaiting deletion.
