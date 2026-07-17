using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    // 2026-07-16 faction-roster-v1-design.md §2.7: Blightborn Pact — "The Rot of Old Houses".
    // 6 Common / 3 Uncommon / 3 Rare = 12 pieces, 3 buildings, 2 tactics, 0 vehicles (their
    // heavy machines are structures, not vehicles — Vitriol Throne). All HP/damage/
    // ManpowerCost numbers below are PROVISIONAL, anchored to IronMarch's own authored
    // numbers by rarity (see IronmarchUnionContentFactory.Pieces.cs for the anchor set), kept
    // on the lower-HP end for commons per the "moth-eaten"/"tarnished" flavor (this is not the
    // tankiest faction). Honest weakness (deliberate, not patched here): gas is weak vs
    // structures/buildings per the existing AttackType damage-triangle.
    public static partial class BlightbornPactContentFactory
    {
        // Straight 3-in-a-row footprint for the two plain 3-cell buildings in this roster
        // (Fumigation Works, The Yellow Autumn) — distinct from IronMarch's L-tromino, same
        // shape Dust Scourge already introduced for its own 3-cell pieces.
        private static readonly Vector2Int[] Triple3 =
        {
            Vector2Int.zero,
            new Vector2Int(1, 0),
            new Vector2Int(2, 0)
        };

        internal static PieceDefinitionSO[] CreatePieces() => new[]
        {
            // ---------------------------------------------------------------
            // COMMON (6)
            // ---------------------------------------------------------------

            // Common — the faction body. Moth-eaten uniforms, family muskets: modest ballistic
            // line infantry, kept on the lower-HP end vs IronMarch's conscript_rifles.
            SavePiece("threadbare_guard", "Threadbare Guard", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Infantry, GameTagIds.Assault, FactionIds.BlightbornPact, 35, 6, 1, AttackType.Ballistic, ArmorType.None,
                AttackSpeedTier.Slow, AttackRangeTier.Medium, 2, flavorTags: new[] { GameTagIds.SmallArms },
                rarity: Rarity.Common),

            // Common — gas count piece. combatRole reuses the Gas attack-type tag id per the
            // established convention (TagRegistry validation only checks existence, not
            // category-correctness; same pattern Dust Scourge's gasflinger already uses) — no
            // ability of its own, just a board-countable gas source for Gas Alchemist's aura
            // and (nominally) Vitriol Throne's tactic below.
            SavePiece("censer_carrier", "Censer Carrier", PieceCategory.Unit, DemoSandboxShapes.VerticalPair,
                GameTagIds.Infantry, GameTagIds.Gas, FactionIds.BlightbornPact, 30, 6, 1, AttackType.Gas, ArmorType.None,
                AttackSpeedTier.Slow, AttackRangeTier.Medium, 2,
                rarity: Rarity.Common),

            // Common — halberdiers in tarnished plate. Defender line-holder; medium armor
            // despite the faction's generally lower-HP flavor (this piece is the exception —
            // it's the one common built to hold a line).
            SavePiece("iron_veil_guard", "Iron Veil Guard", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Infantry, GameTagIds.Defender, FactionIds.BlightbornPact, 50, 7, 2, AttackType.Melee, ArmorType.Medium,
                AttackSpeedTier.Slow, AttackRangeTier.Melee, 1,
                rarity: Rarity.Common),

            // Common — adjacent allies +HP. Same tech/magnitude as IronMarch's/Neutral's
            // field_medic (AdjacentAura, MaxHp, Flat +10, neighborFilter Infantry).
            SavePiece("court_physician", "Court Physician", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Support, FactionIds.BlightbornPact, 25, 0, 1, AttackType.None, ArmorType.None,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 2,
                synergyTags: new[] { GameTagIds.Medic },
                customAbilities: new[]
                {
                    Ability("court_physician_adjacent_infantry_hp", PieceAbilityTrigger.AdjacentAura, SynergyStat.MaxHp, SynergyModType.Flat, 10,
                        neighborFilter: new NeighborFilter { PrimaryTagId = GameTagIds.Infantry })
                },
                rarity: Rarity.Common),

            // Common — "adjacent allies deal +morale damage on attacks".
            // TODO: checked SynergyStat (Assets/_Project/Core/Tags/SynergyStat.cs) — it has
            // Damage, AttackRange, AttackSpeedSteps, MovementSpeed, ArmorType,
            // MoveChargePercent, MaxHp, MoraleResistancePercent. There is NO Terror/morale-
            // damage-dealt entry: TerrorDamage is a flat per-piece PieceDefinition field
            // (set once at authoring time), not something PieceAbilityEngine's AdjacentAura
            // system can currently grant/modify on a neighbor. This aura has no seam today —
            // authored as a plain support body with a thematic Inspiring synergy tag instead
            // of a faked ability, flagged here rather than invented.
            SavePiece("dirge_piper", "Dirge Piper", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Support, FactionIds.BlightbornPact, 25, 0, 1, AttackType.None, ArmorType.None,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 2,
                synergyTags: new[] { GameTagIds.Inspiring },
                rarity: Rarity.Common),

            // Common — HQ building, +Supplies/round. Mirrors supply_depot/scavengers_cache
            // pattern (synergyTags SupplyLine + flavorTags Logistics); the actual flat
            // +Supplies/round hookup is wired centrally elsewhere (Core/Run/
            // BuildingIncomeRules.cs), not touched here.
            SavePiece("poison_garden", "Poison Garden", PieceCategory.Building, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.BlightbornPact, 45, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0,
                synergyTags: new[] { GameTagIds.SupplyLine }, flavorTags: new[] { GameTagIds.Logistics },
                rarity: Rarity.Common),

            // ---------------------------------------------------------------
            // UNCOMMON (3)
            // ---------------------------------------------------------------

            // Uncommon — "adjacent gas pieces: +damage". NeighborFilter
            // (Assets/_Project/Core/Tags/NeighborFilter.cs) only matches PrimaryTagId/
            // CombatRoleTagId/SystemTagId/SynergyTagId/AbilityTagId — it has no AttackType
            // field, so it cannot match "gas-typed attacker" directly. Since Censer Carrier's
            // combatRole is set to GameTagIds.Gas per this roster's convention, filtering on
            // CombatRoleTagId = GameTagIds.Gas correctly catches gas-role neighbors (this is
            // exactly the fallback the task anticipated, not a more-precise AttackType match).
            SavePiece("gas_alchemist", "Gas Alchemist", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Support, FactionIds.BlightbornPact, 25, 0, 1, AttackType.None, ArmorType.None,
                AttackSpeedTier.Medium, AttackRangeTier.Short, 2,
                customAbilities: new[]
                {
                    Ability("gas_alchemist_adjacent_gas_damage", PieceAbilityTrigger.AdjacentAura, SynergyStat.Damage, SynergyModType.Flat, 3,
                        neighborFilter: new NeighborFilter { CombatRoleTagId = GameTagIds.Gas })
                },
                rarity: Rarity.Uncommon),

            // Uncommon — command piece. combatRole is Utility (not a literal "command" role —
            // GameTagIds has no such CombatRole value) + synergyTags [Command], mirroring
            // IronMarch's Shock Sergeant / Dust Scourge's Raid Captain pattern. First half of
            // her text ("attacks deal terror") is a real terrorDamage value (~2x baseDamage,
            // matching Breakthrough Tank's/Machine Gun Nest's own terror-to-damage ratio).
            // TODO: second half ("adjacent allies' terror strengthened") has the SAME
            // SynergyStat gap as Dirge Piper above — no terror/morale-damage SynergyStat entry
            // exists for an aura to grant. Not re-investigated here; same finding applies.
            SavePiece("widow_of_the_house", "Widow of the House", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.BlightbornPact, 45, 7, 2, AttackType.Ballistic, ArmorType.Light,
                AttackSpeedTier.Medium, AttackRangeTier.Medium, 2, synergyTags: new[] { GameTagIds.Command },
                rarity: Rarity.Uncommon, terrorDamage: 14),

            // Uncommon — HQ building. Tactic: Creeping Cloud (gas area). Reuses the existing
            // small-area GrantedAbility.MortarShot (flat, unscaled) as the generic small-area-
            // effect carrier — stands in for the bespoke Creeping Cloud gas-area flavor, same
            // reuse Dust Scourge's Fume Still already makes for its own "Gas Cloud" tactic.
            SavePiece("fumigation_works", "Fumigation Works", PieceCategory.Building, Triple3,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.BlightbornPact, 65, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, grantedAbility: GrantedAbility.MortarShot,
                synergyTags: new[] { GameTagIds.GasCloud },
                rarity: Rarity.Uncommon),

            // ---------------------------------------------------------------
            // RARE (3)
            // ---------------------------------------------------------------

            // Rare — the rule-bend. Plain HQ building, no attack, high-ish HP for a Rare.
            // hijacksAmbientGas: true is read generically by TickCombatRun (confirmed by
            // reading its fight-start block, not GasDamageSystem.cs directly — the flag never
            // reaches GasDamageSystem itself): `_playerAmbientGasHijack =
            // _playerCombatants.Any(c => c.Definition.HijacksAmbientGas)` (and the enemy-side
            // mirror), OR'd together to compute `_effectiveGasStartTick` via
            // GasHijackRules.GetEffectiveGasStartTick (earlier start for the whole fight once
            // EITHER side fields the piece), and each side's own combatants are only immune
            // if THEIR OWN side fielded the hijacker (TickCombatRun.cs lines ~118-120 and
            // ~432-434). Side-wide, active-piece-gated, exactly as specced — nothing further
            // needed from this factory.
            SavePiece("the_yellow_autumn", "The Yellow Autumn", PieceCategory.Building, Triple3,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.BlightbornPact, 110, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0,
                rarity: Rarity.Rare, hijacksAmbientGas: true),

            // Rare — the gas→morale fusion. gasDealsMoraleDamage: true. Confirmed via
            // TickCombatRun.cs's ResolveAttacks (search "gasMoraleFusion"): the flag is
            // checked ONCE PER SIDE PER VOLLEY, not per-attacker —
            // `bool gasMoraleFusion = attackers.Any(c => c.IsActive && c.Definition.
            // GasDealsMoraleDamage);` — then every attacker on that side whose own
            // AttackType == Gas also deals equal morale damage that volley
            // (`if (gasMoraleFusion && actor.Definition.AttackType == AttackType.Gas)
            // ApplyMoraleDamage(...)`). So yes: as long as Duchess of Sighs is alive/active,
            // ALL Blightborn gas attackers on her side get the bonus, not just her own hits —
            // an army-wide granted rule, exactly as the doc comment on GasDealsMoraleDamage
            // describes. Command-tier stats, AttackType.Gas per the roster table.
            SavePiece("duchess_of_sighs", "Duchess of Sighs", PieceCategory.Unit, DemoSandboxShapes.VerticalPair,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.BlightbornPact, 55, 9, 3, AttackType.Gas, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Medium, 2, synergyTags: new[] { GameTagIds.Command },
                rarity: Rarity.Rare, gasDealsMoraleDamage: true),

            // Rare — combat-board structure (primary: structure → Combat board per §5's
            // machine_gun_nest gotcha; Category.Unit here, NOT Building). Tactic: Vitriol
            // Rain, huge gas bombardment "scaling with gas count" — reuses
            // GrantedAbility.RollingBarrage per spec instruction, same mechanism Grand
            // Battery uses for its own artillery count.
            // NOTE (checked per instruction, same finding Dust Scourge's Stormcaller entry
            // already flagged): TickCombatRun.cs computes
            // `_playerArtilleryCount = BuildBoardTagCounter.Count(playerBuildBoards, GameTagIds.Artillery)`
            // ONCE at fight start and passes it through as the single `artilleryCount`
            // parameter into CombatAbilityExecutor.Execute → ExecuteRollingBarrage, which
            // multiplies damage by that parameter directly (RollingBarrageBaseDamage +
            // artilleryCount * RollingBarragePerArtilleryDamage). The count source is
            // HARDCODED to the `artillery` board-tag, not a generic "count some tag"
            // parameter — there is no wiring today for a `gas`-tag count to feed it. For
            // Vitriol Throne (as for Dust Scourge's Stormcaller and, per the task, Ashen's own
            // rare reusing this ability), Rolling Barrage will currently scale off the army's
            // Artillery-tagged piece count, NOT gas count, until CombatAbilityExecutor/
            // TickCombatRun are extended to take a configurable scaling-tag (or a parallel
            // Gas-scaled ability is added). Flagging as a shared follow-up, not fixed here —
            // out of scope (read-only on TickCombatRun.cs/CombatAbilityExecutor.cs per task).
            SavePiece("vitriol_throne", "Vitriol Throne", PieceCategory.Unit, DemoSandboxShapes.Square2x2,
                GameTagIds.Structure, GameTagIds.Artillery, FactionIds.BlightbornPact, 120, 10, 4, AttackType.Gas, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Long, 0, grantedAbility: GrantedAbility.RollingBarrage,
                flavorTags: new[] { GameTagIds.GasCloud },
                rarity: Rarity.Rare)
        };
    }
}
