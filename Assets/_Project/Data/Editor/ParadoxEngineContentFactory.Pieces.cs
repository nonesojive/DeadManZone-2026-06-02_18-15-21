using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    // 2026-07-16 faction-roster-v1-design.md §2.6: Paradox Engine — "The Experiment That
    // Won't End". 6 Common / 3 Uncommon / 3 Rare = 12 pieces, 3 buildings, 3 tactics, 0
    // vehicles (spec's own composition line). Design border rule: Paradox manipulates ONLY
    // its own tempo (speed, echoes, extra moments) and uses ZERO randomness — every ability
    // below is deterministic, no "% chance" effects anywhere in this roster.
    // All HP/damage/ManpowerCost numbers below are PROVISIONAL, anchored to IronMarch's own
    // authored numbers by rarity (see IronmarchUnionContentFactory.Pieces.cs for the anchor
    // set), nudged slightly speed-favored per the spec's "self-tempo faction" instruction
    // (attackSpeed one tier faster and/or movementSpeed +1 vs. the closest IronMarch role).
    public static partial class ParadoxEngineContentFactory
    {
        // Straight 3-in-a-row footprint for Chronometry Station's plain 3-cell footprint —
        // a local copy (not IronMarch's Tromino3 L-shape or Dust Scourge's Triple3), per
        // spec instruction that this faction's straight-3 shape is authored independently.
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

            // Common — the faction body. Anchored to conscript_rifles/line_grenadiers but
            // bumped one attack-speed tier and +1 movementSpeed per the self-tempo nudge.
            SavePiece("chrono_fusilier", "Chrono-Fusilier", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Infantry, GameTagIds.Assault, FactionIds.ParadoxEngine, 50, 6, 1, AttackType.Ballistic, ArmorType.None,
                AttackSpeedTier.Medium, AttackRangeTier.Medium, 3,
                rarity: Rarity.Common),

            // Common — line-holder. Anchored to Iron Guard's defender pattern but lighter
            // armor (per spec's Armor column) and faster (Medium attack speed, movement 2
            // vs Iron Guard's Slow/1).
            SavePiece("phase_vanguard", "Phase Vanguard", PieceCategory.Unit, DemoSandboxShapes.VerticalPair,
                GameTagIds.Infantry, GameTagIds.Defender, FactionIds.ParadoxEngine, 70, 4, 2, AttackType.Ballistic, ArmorType.Light,
                AttackSpeedTier.Medium, AttackRangeTier.Short, 2,
                rarity: Rarity.Common),

            // Common — beam-rifle marksman. combatRole = Sniper directly (plugs into the
            // existing sniper_accuracy/sniper_damage CM rules automatically, per spec).
            // Glassiest common: lower HP, higher damage than sharpshooter's anchor numbers.
            SavePiece("arc_lancer", "Arc Lancer", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Sniper, FactionIds.ParadoxEngine, 25, 9, 1, AttackType.Piercing, ArmorType.None,
                AttackSpeedTier.Medium, AttackRangeTier.Long, 3,
                rarity: Rarity.Common),

            // Common — adjacent allies +1 attack-speed tier. Exact copy of IronMarch's
            // Forward Observer pattern, retargeted to Infantry broadly (Field Dynamo isn't
            // artillery-specific).
            SavePiece("field_dynamo", "Field Dynamo", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Support, FactionIds.ParadoxEngine, 30, 0, 1, AttackType.None, ArmorType.None,
                AttackSpeedTier.Fast, AttackRangeTier.Short, 3,
                customAbilities: new[]
                {
                    Ability("field_dynamo_adjacent_attack_speed", PieceAbilityTrigger.AdjacentAura, SynergyStat.AttackSpeedSteps, SynergyModType.TierStep, 1,
                        neighborFilter: new NeighborFilter { PrimaryTagId = GameTagIds.Infantry })
                },
                rarity: Rarity.Common),

            // Common — +Supplies/round. NOTE: BuildingIncomeRules.SumSuppliesFlatBonus hardcodes
            // id == "supply_depot" only (read-only shared file, not touched here) — this piece's
            // own flat Supplies bonus needs the same central id-based wiring added later, mirroring
            // Dust Scourge's scavengers_cache precedent (DustScourgeContentFactory.Pieces.cs).
            // Authored here as a plain +Supplies-flavored building; synergyTags/flavorTags match
            // the established SupplyLine/Logistics convention.
            SavePiece("chrono_lab", "Chrono-Lab", PieceCategory.Building, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.ParadoxEngine, 50, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0,
                synergyTags: new[] { GameTagIds.SupplyLine }, flavorTags: new[] { GameTagIds.Logistics },
                rarity: Rarity.Common),

            // Common — +Muster/shop. MusterPerShop is a generic summed field
            // (MusterCalculator.Compute over BoardState.Pieces) — no id-specific wiring gap,
            // unlike Chrono-Lab's Supplies bonus above.
            SavePiece("assembly_loop", "Assembly Loop", PieceCategory.Building, DemoSandboxShapes.VerticalPair,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.ParadoxEngine, 35, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, musterPerShop: 1,
                rarity: Rarity.Common),

            // ---------------------------------------------------------------
            // UNCOMMON (3)
            // ---------------------------------------------------------------

            // Uncommon — adjacent piece: +movement charge rate. SynergyStat.MoveChargePercent
            // exists (Core/Tags/SynergyStat.cs) and is already wired end-to-end in
            // PieceAbilityEngine (SynergyResult.MoveChargeBonus -> CombatantState.MoveCharge),
            // so this is a real seam, not a TODO stub. neighborFilter is deliberately unfiltered
            // (any adjacent piece), matching the spec's "Adjacent piece" wording (no role
            // restriction, unlike Field Dynamo's Infantry-only aura).
            SavePiece("overclock_engineer", "Overclock Engineer", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Support, FactionIds.ParadoxEngine, 30, 0, 1, AttackType.None, ArmorType.None,
                AttackSpeedTier.Medium, AttackRangeTier.Short, 2,
                customAbilities: new[]
                {
                    Ability("overclock_engineer_adjacent_move_charge", PieceAbilityTrigger.AdjacentAura, SynergyStat.MoveChargePercent, SynergyModType.Percent, 20,
                        neighborFilter: new NeighborFilter())
                },
                rarity: Rarity.Uncommon),

            // Uncommon — HQ building. Tactic: Time Dilation ("all units +movement & attack
            // speed for a stretch"). GrantedAbility.ShieldAllies reused as the team-wide buff
            // carrier per spec instruction — stands in for the bespoke Time Dilation flavor;
            // the underlying effect (CombatAbilityExecutor.ExecuteShieldAllies) is a pause-
            // window team buff, the closest existing primitive to "everyone gets faster for a
            // stretch."
            SavePiece("chronometry_station", "Chronometry Station", PieceCategory.Building, Triple3,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.ParadoxEngine, 80, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, grantedAbility: GrantedAbility.ShieldAllies,
                rarity: Rarity.Uncommon),

            // Uncommon — combat-board structure (primary: structure -> Combat board, per §5's
            // machine_gun_nest gotcha). Tactic: Echo ("repeat the last order/tactic issued this
            // fight, free"). GrantedAbility.Echo is an EXACT match — its doc comment in
            // CombatStatEnums.cs cites this exact piece/effect, used verbatim, no substitution.
            SavePiece("resonance_coil", "Resonance Coil", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Structure, GameTagIds.Utility, FactionIds.ParadoxEngine, 60, 0, 2, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, grantedAbility: GrantedAbility.Echo,
                rarity: Rarity.Uncommon),

            // ---------------------------------------------------------------
            // RARE (3)
            // ---------------------------------------------------------------

            // Rare — HQ building. 🟡 THE ONLY piece in the game with addsPauseWindow = true
            // (PieceDefinition.AddsPauseWindow doc comment names this exact piece) — a fielded
            // copy appends one extra threshold to the fight's pause-window list
            // (TickCombatRun's per-instance pause-threshold list). Big HP for an HQ building
            // (~100), since HQ buildings run lower than combat-board structures.
            SavePiece("the_second_hand", "The Second Hand", PieceCategory.Building, DemoSandboxShapes.Square2x2,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.ParadoxEngine, 100, 0, 3, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0,
                rarity: Rarity.Rare, addsPauseWindow: true),

            // Rare — 🔴 Paradox's ONE new-system tentpole: repeatsPauseAbilities = true (the
            // ONLY piece in the game with this flag — PieceDefinition.RepeatsPauseAbilities
            // doc comment names this exact piece). combatRole = Utility + synergyTags [Command]
            // for the "command" role mapping (GameTagIds has no literal Command combat-role
            // value, same convention as IronMarch's Shock Sergeant / Dust Scourge's Raid
            // Captain). Small 1-cell glass-cannon command body: low HP, small attack.
            SavePiece("doctor_recursion", "Doctor Recursion", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.ParadoxEngine, 35, 3, 3, AttackType.Ballistic, ArmorType.None,
                AttackSpeedTier.Medium, AttackRangeTier.Medium, 2, synergyTags: new[] { GameTagIds.Command },
                rarity: Rarity.Rare, repeatsPauseAbilities: true),

            // Rare — combat-board machine (primary: structure -> Combat board, per §5's
            // machine_gun_nest gotcha; this piece's own flavor text explicitly calls it a
            // "Combat-board machine"). "ALL your units +1 attack-speed tier" is a passive
            // army-wide aura, NOT a pause-window tactic (no "Tactic:" line in its spec row,
            // unlike Chronometry Station/Resonance Coil above) — so it gets NO grantedAbility.
            // PieceAbilityTrigger only has AdjacentAura/FightStart/BoardPerTagCount (confirmed
            // via Core/Tags/PieceAbilityTrigger.cs) — there is no true whole-board/army-wide
            // trigger. PROVISIONAL SIMPLIFICATION: AdjacentAura at radius: 12 (PieceAbilityEngine's
            // BFS-over-adjacency hop count) to approximate "ALL your units" on the 6x6 combat
            // board, the same pattern IronMarch's Breakthrough Tank uses at radius: 2 for its
            // "within 2 cells" text. NeighborFilter is unfiltered (splash-friendly / faction-blind
            // per spec's own wording).
            SavePiece("perpetual_engine", "Perpetual Engine", PieceCategory.Unit, DemoSandboxShapes.Square2x2,
                GameTagIds.Structure, GameTagIds.Utility, FactionIds.ParadoxEngine, 130, 0, 4, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0,
                customAbilities: new[]
                {
                    Ability("perpetual_engine_army_attack_speed", PieceAbilityTrigger.AdjacentAura, SynergyStat.AttackSpeedSteps, SynergyModType.TierStep, 1,
                        neighborFilter: new NeighborFilter(), radius: 12)
                },
                rarity: Rarity.Rare)
        };
    }
}
