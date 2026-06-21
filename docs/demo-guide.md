# DeadManZone Demo Guide

## Playable Factions

| Faction | ID | Theme |
|---------|-----|-------|
| IronMarch Union | `iron_vanguard` | Industrial brass, heavy armor, command focus |
| Dust Scourge | `dust_scourge` | Nomadic scavengers, gas warfare, salvage bonus |
| Cartel of Echoes | `cartel_of_echoes` | Stealth, echo tech, adjacency synergies |

## Enemy Factions (variety)

| Faction | ID | Theme |
|---------|-----|-------|
| Neutral Militia | `neutral` | Generic trench forces |
| Crimson Legion | `crimson_legion` | Heavy assault, tanks and elites |
| Ash Wraiths | `ash_wraiths` | Gas phantoms, stealth ambush |

## Core Systems

- **10-fight gauntlet** with escalating enemy templates
- **4 resources**: Supplies, Manpower, Authority, Morale
- **Shop**: Unified 8–12 slot offer grid with salvage refunds (lane weighting in data)
- **Combat**: Tick-based sim; HP-triggered pauses at 75%/30% army HP; gas ramp ~30s
- **Synergies**: Medic→Infantry, Command→Artillery, Echo→Stealth ability, Inspiring→any
- **Critical Mass**: 3+ Infantry, 2+ Vehicles, 2+ Artillery, 3+ Assault thresholds
- **HUD**: Army strength / matchup preview vs next enemy
- **Meta**: Achievements, local leaderboard, faction unlocks (Steam stub ready)

## Setup

1. Open project in Unity 6
2. **DeadManZone → Generate Demo Content (5 Factions)**
3. **DeadManZone → Create Default UI Theme**
4. **DeadManZone → Setup Main Menu & Run Scenes**
5. Play from Main Menu

## Known Issues

- Steam achievements/leaderboards require Steamworks SDK wiring
- Emergency Draft button must be assigned in Run scene Inspector if not auto-wired
- Art icons for new faction pieces use category tints until Blender renders are assigned
