# DeadManZone Demo Guide

## Playable Factions

| Faction | ID | Theme |
|---------|-----|-------|
| IronMarch Union | `ironmarch_union` | Industrial brass, heavy armor, command focus (**only selectable faction** in current vertical slice) |
| Dust Scourge | `dust_scourge` | Nomadic scavengers, gas warfare, salvage bonus (hidden until content pass) |
| Cartel of Echoes | `cartel_of_echoes` | Stealth, echo tech, adjacency synergies (hidden until content pass) |

## Enemy Factions (variety)

Enemy fights use compositions from the **17-piece IronMarch roster** (`IronmarchEnemyFactory`). Legacy enemy faction ids may still appear on templates for salvage pool flavor.

| Faction | ID | Theme |
|---------|-----|-------|
| Neutral Militia | `neutral` | Generic trench forces |
| IronMarch Union | `ironmarch_union` | Current enemy template faction id |

## Core Systems

- **10-fight gauntlet** with escalating enemy templates (new piece pool)
- **4 resources**: Supplies, Manpower, Authority, Morale — top bar shows **income previews** (`+N`) and **salvage %** from faction baseline + board
- **Post-combat income**: Supplies = `baseSuppliesPerRound` (+10 IronMarch) + building/critical-mass bonuses — **not** win/loss gated; no `FightRewardTable`
- **Shop**: Unified 8–12 slot offer grid; salvage offers use faction base + combat-board boost (see `2026-07-01-build-hud-economy-design.md`)
- **Combat**: Tick-based sim; HP-triggered pauses at 75%/30% army HP; gas ramp ~30s
- **Movement**: Numeric speed 0–4 on piece data (0 = immobile)
- **Starting tactics**: Hold the Line, Advance, Disciplined Fire
- **Synergies**: Per-tag abilities on HQ + combat boards; adjacent auras on combat board only
- **Critical Mass**: Tag thresholds on combined boards (see content pass spec)
- **HUD**: Army strength / matchup preview; supplies/manpower/authority income labels; salvage %; critical mass drawer tab; build messages in bottom **InfoMessageRegion**
- **Meta**: Achievements, local leaderboard, faction unlocks (Steam stub ready)

## IronMarch Union starts (content pass)

| Field | Value |
|-------|-------|
| Starting supplies | 50 |
| Starting manpower | 15 |
| Supplies income (empty board) | +10 / fight |
| Manpower income (empty board) | +1 / shop |
| Salvage base | 1% |

## Setup

1. Open project in Unity 6
2. **DeadManZone → Content → Generate IronMarch Union Content Pass**
3. **DeadManZone → Create Default UI Theme** (if missing)
4. **DeadManZone → Setup Main Menu & Run Scenes** (if scenes not wired)
5. Play from Main Menu — select **IronMarch Union**

## Known Issues

- Steam achievements/leaderboards require Steamworks SDK wiring
- Emergency Draft button must be assigned in Run scene Inspector if not auto-wired
- Art icons for new faction pieces use category tints until Blender renders are assigned
- Dust Scourge and Cartel factions are hidden until their content passes land
