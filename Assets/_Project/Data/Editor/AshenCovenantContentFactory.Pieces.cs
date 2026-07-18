using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    // 2026-07-15 faction-roster-v1-design.md §2.9: Ashen Covenant ("The Revolution of Cinders").
    // 6 Common / 3 Uncommon / 3 Rare, 2 buildings, 2 tactics, 0 vehicles.
    //
    // HARD RULE (§2.9, "the martyrdom faction must not be at war with the run's health bar"):
    // every combat piece (all 6 commons + both non-building uncommons + all 3 rares) carries
    // manpowerCost: 1. Only the buildings (Shrine of Ash, Pyre Altar, Pyre Cathedral) use the
    // normal building convention of manpowerCost: 0 — buildings never cost Manpower.
    //
    // HP/damage are PROVISIONAL, same convention as IronmarchUnionContentFactory: a low-HP,
    // moderate-to-high-damage glass-cannon swarm identity (fragile force-multiplier rares, not
    // statball rares), per §2.9.
    // PROVISIONAL — melee pace pass 2026-07-18: AttackType.Melee pieces bumped movementSpeed
    // 1→2 / 2→3 (3+ left alone) so they close to range 1 faster than rifle lines.
    public static partial class AshenCovenantContentFactory
    {
        // Straight 3-cell footprint for Zealot Mob (plain 3-cell swarm body, no reason to reuse
        // IronMarch's L-tromino here).
        private static readonly Vector2Int[] Triple3 =
        {
            Vector2Int.zero,
            new Vector2Int(1, 0),
            new Vector2Int(2, 0)
        };

        internal static PieceDefinitionSO[] CreatePieces() => new[]
        {
            // ---------------------------------------------------------------
            // Common (6)
            // ---------------------------------------------------------------

            // Zealot Mob — cheap fervent swarm body, `fanatic` tag plugs directly into the
            // existing fanatic Critical-Mass rule (CriticalMassDefaultRules: 3/5/7 board-count
            // thresholds → AttackSpeed tier steps). No custom ability needed.
            SavePiece("zealot_mob", "Zealot Mob", PieceCategory.Unit, Triple3,
                GameTagIds.Infantry, GameTagIds.Assault, FactionIds.AshenCovenant, 30, 6, 1,
                AttackType.Melee, ArmorType.None, AttackSpeedTier.Fast, AttackRangeTier.Melee, 3,
                synergyTags: new[] { GameTagIds.Fanatic },
                rarity: Rarity.Common),

            // Ash Acolyte — the low-state tentpole taught at Common tier: lowStateDamageBonus is a
            // Definition-level field read live by LowStateRules.GetDamageBonus whenever this piece
            // drops below 50% HP or morale in combat (LowStateRules.cs) — no ability wiring
            // required, just the field. Also `fanatic` for the swarm identity.
            SavePiece("ash_acolyte", "Ash Acolyte", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Assault, FactionIds.AshenCovenant, 18, 5, 1,
                AttackType.Melee, ArmorType.None, AttackSpeedTier.Medium, AttackRangeTier.Melee, 3,
                synergyTags: new[] { GameTagIds.Fanatic },
                rarity: Rarity.Common,
                lowStateDamageBonus: 3),

            // Torchbearer — flamethrower common. Tagged `abilityTags: [Flamethrower]` (an existing
            // GameTagIds constant) purely so Firebrand Vicar's neighbor filter downstream can catch
            // "fire-flavored" neighbors without NeighborFilter needing an AttackType match (it only
            // matches tag ids — confirmed in NeighborFilter.cs). Also `fanatic`.
            SavePiece("torchbearer", "Torchbearer", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Infantry, GameTagIds.Assault, FactionIds.AshenCovenant, 25, 7, 1,
                AttackType.Fire, ArmorType.None, AttackSpeedTier.Medium, AttackRangeTier.Short, 2,
                synergyTags: new[] { GameTagIds.Fanatic },
                abilityTags: new[] { GameTagIds.Flamethrower },
                rarity: Rarity.Common),

            // Penitent — no armor, unusually high HP for a Common (highest common HP in this
            // roster, per the spec's explicit callout).
            SavePiece("penitent", "Penitent", PieceCategory.Unit, DemoSandboxShapes.VerticalPair,
                GameTagIds.Infantry, GameTagIds.Defender, FactionIds.AshenCovenant, 75, 4, 1,
                AttackType.Melee, ArmorType.None, AttackSpeedTier.Slow, AttackRangeTier.Melee, 2,
                rarity: Rarity.Common),

            // Hymnal Leader — "adjacent allies +morale". SynergyStat (Tags/SynergyStat.cs) has no
            // Morale/MaxMorale entry — only Damage, AttackRange, AttackSpeedSteps, MovementSpeed,
            // ArmorType, MoveChargePercent, MaxHp, MoraleResistancePercent — so a custom
            // PieceAbilityInlineEntry literally cannot target Morale today. Same SynergyStat-gap
            // other factions' morale auras have hit; authored as a plain support body.
            // TODO: no SynergyStat.Morale seam exists yet for "adjacent allies +morale" — needs a
            // new SynergyStat case (and PieceAbilityEngine/CombatantState wiring) before this
            // piece's stated ability can be implemented.
            SavePiece("hymnal_leader", "Hymnal Leader", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Support, FactionIds.AshenCovenant, 20, 0, 1,
                AttackType.None, ArmorType.None, AttackSpeedTier.Slow, AttackRangeTier.Short, 2,
                rarity: Rarity.Common),

            // Shrine of Ash — building, +Muster/shop. Buildings keep manpowerCost: 0 per the
            // faction-wide hard rule's carve-out.
            SavePiece("shrine_of_ash", "Shrine of Ash", PieceCategory.Building, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.AshenCovenant, 40, 0, 0,
                AttackType.None, ArmorType.Light, AttackSpeedTier.Slow, AttackRangeTier.Short, 0,
                musterPerShop: 2,
                rarity: Rarity.Common),

            // ---------------------------------------------------------------
            // Uncommon (3)
            // ---------------------------------------------------------------

            // Reliquary Bearer — "+damage per adjacent fanatic". Investigated PieceAbilityEngine
            // (ApplyAdjacentAuras): for each ability, it walks every reachable neighbor within
            // `radius`, and for each one matching neighborFilter it applies the effect once, to
            // targetId = applyToSelf ? source : neighbor. ApplyEffect *accumulates* onto whatever
            // the target already has (result.DamageBonus + amount), so with applyToSelf: true a
            // magnitude-2 ability firing once per matching adjacent Fanatic naturally stacks: 1
            // adjacent fanatic = +2, 2 adjacent = +4, etc. This is an exact fit, not an
            // approximation — no BoardPerTagCount substitution or imprecision flag needed here.
            // AttackType note: the spec's Attack column shows "—" for this piece (support role,
            // no themed attack type), but its entire kit is "+damage to itself" — a literal
            // AttackType.None would make that ability permanently inert. Following the same
            // precedent as IronMarch's Field Medic (support role, small Ballistic sidearm despite
            // no themed attack identity), Reliquary Bearer gets a modest baseDamage/Ballistic so
            // the ability has something to apply to.
            SavePiece("reliquary_bearer", "Reliquary Bearer", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Support, FactionIds.AshenCovenant, 22, 2, 1,
                AttackType.Ballistic, ArmorType.None, AttackSpeedTier.Medium, AttackRangeTier.Short, 2,
                customAbilities: new[]
                {
                    Ability("reliquary_bearer_adjacent_fanatic_damage", PieceAbilityTrigger.AdjacentAura, SynergyStat.Damage, SynergyModType.Flat, 2,
                        neighborFilter: new NeighborFilter { SynergyTagId = GameTagIds.Fanatic },
                        applyToSelf: true)
                },
                rarity: Rarity.Uncommon),

            // Firebrand Vicar — "adjacent fire pieces: +damage". "command" role per the roster
            // table maps to combatRole = Utility + synergyTags: [Command] (same mapping used for
            // Saint of the Embers below). NeighborFilter is tag-based, not AttackType-based
            // (confirmed in NeighborFilter.cs — it only matches Primary/CombatRole/System/Synergy/
            // Ability tag ids), so this filters on AbilityTagId = Flamethrower to catch Torchbearer
            // (and any other flamethrower-tagged piece) as a "fire-flavored" neighbor.
            SavePiece("firebrand_vicar", "Firebrand Vicar", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.AshenCovenant, 30, 6, 1,
                AttackType.Fire, ArmorType.Light, AttackSpeedTier.Medium, AttackRangeTier.Short, 2,
                synergyTags: new[] { GameTagIds.Command },
                customAbilities: new[]
                {
                    Ability("firebrand_vicar_adjacent_fire_damage", PieceAbilityTrigger.AdjacentAura, SynergyStat.Damage, SynergyModType.Flat, 3,
                        neighborFilter: new NeighborFilter { AbilityTagId = GameTagIds.Flamethrower })
                },
                rarity: Rarity.Uncommon),

            // Pyre Altar — HQ building granting the Fervor tactic ("all units +morale & attack
            // speed for a stretch"). Reuses GrantedAbility.ShieldAllies as the team-wide buff
            // carrier, same reuse pattern as IronMarch's Artillery Park reusing MortarShot.
            SavePiece("pyre_altar", "Pyre Altar", PieceCategory.Building, DemoSandboxShapes.VerticalPair,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.AshenCovenant, 45, 0, 0,
                AttackType.None, ArmorType.Light, AttackSpeedTier.Slow, AttackRangeTier.Short, 0,
                grantedAbility: GrantedAbility.ShieldAllies,
                rarity: Rarity.Uncommon),

            // ---------------------------------------------------------------
            // Rare (3)
            // ---------------------------------------------------------------

            // Saint of the Embers — the big low-state payoff. "command" role → combatRole =
            // Utility + synergyTags: [Command]. Investigated PieceAbilityTrigger (Tags/
            // PieceAbilityTrigger.cs): only three values exist — AdjacentAura, FightStart,
            // BoardPerTagCount. None of them is conditional on the TARGET's own live low-state
            // (below-50%-HP-or-morale) status; AdjacentAura/BoardPerTagCount always fire
            // unconditionally against whatever NeighborFilter matches, and low-state is evaluated
            // per-combatant live in LowStateRules, which nothing in PieceAbilityEngine reads. So
            // there genuinely is no seam for "make an ARMY-WIDE aura that only turns on for allies
            // who are individually below 50%" today.
            // TODO: army-wide low-state conditional aura has no PieceAbilityTrigger seam yet —
            // this piece only strengthens its OWN low-state bonus (LowStateDamageBonus/
            // LowStateAttackSpeedSteps below), not allies'; flagging per the task's allowance for
            // genuinely unseamed effects rather than inventing a new trigger type here.
            SavePiece("saint_of_the_embers", "Saint of the Embers", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.AshenCovenant, 35, 10, 1,
                AttackType.Melee, ArmorType.None, AttackSpeedTier.Medium, AttackRangeTier.Melee, 3,
                synergyTags: new[] { GameTagIds.Command },
                rarity: Rarity.Rare,
                lowStateDamageBonus: 10,
                lowStateAttackSpeedSteps: 2),

            // The Ash Martyr — "on death: all allies +damage and morale restores to full."
            // "utility" role per the table (not "command"-flavor) → combatRole = Utility directly,
            // no Command synergy tag. The morale half is already handled for free: MoraleRules.
            // IsDeathShockInverted(factionId) is keyed off FactionIds.AshenCovenant (Combat/
            // MoraleRules.cs) and TickCombatRun's death-shock path (around line 678) grants morale
            // to allies within 2 cells instead of draining it whenever an Ashen piece dies — this
            // fires automatically the moment factionId is set correctly, no extra field needed.
            // TODO: the "+damage to all allies" half has no seam — there is no PieceDefinition
            // field or ability trigger for "grant a permanent army-wide damage buff on this
            // specific piece's death" (PieceAbilityTrigger has no OnDeath case). Flagging rather
            // than inventing one. Given cheap (manpowerCost: 1) and low HP so it's genuinely
            // "fielded in order to be lost," with a real baseDamage so it's a combat body while
            // alive, not just a walking death trigger.
            SavePiece("the_ash_martyr", "The Ash Martyr", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.AshenCovenant, 25, 9, 1,
                AttackType.Melee, ArmorType.None, AttackSpeedTier.Medium, AttackRangeTier.Melee, 3,
                rarity: Rarity.Rare),

            // Pyre Cathedral — HQ building granting the Firestorm tactic ("huge fire barrage
            // scaling with fire count"). Investigated TickCombatRun's RollingBarrage execution:
            // _playerArtilleryCount is built via BuildBoardTagCounter.Count(playerBuildBoards,
            // GameTagIds.Artillery) and threaded straight into the barrage's scaling parameter —
            // the count source is hardcoded to the `artillery` tag specifically, there is no
            // equivalent `_playerFireCount`/fire-AttackType counter today.
            // TODO: RollingBarrage's scaling count is hardcoded to GameTagIds.Artillery in
            // TickCombatRun; reusing GrantedAbility.RollingBarrage here means Pyre Cathedral will
            // actually scale with the army's ARTILLERY count, not its fire-AttackType count, until
            // that seam is generalized. Flagging rather than adding a new GrantedAbility/counter.
            SavePiece("pyre_cathedral", "Pyre Cathedral", PieceCategory.Building, DemoSandboxShapes.Square2x2,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.AshenCovenant, 70, 0, 0,
                AttackType.None, ArmorType.Light, AttackSpeedTier.Slow, AttackRangeTier.Short, 0,
                grantedAbility: GrantedAbility.RollingBarrage,
                rarity: Rarity.Rare)
        };
    }
}
