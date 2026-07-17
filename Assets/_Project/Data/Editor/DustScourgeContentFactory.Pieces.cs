using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    // 2026-07-16 faction-roster-v1-design.md §2.3: Dust Scourge — "Scavengers of the Wastes".
    // 6 Common / 3 Uncommon / 3 Rare = 12 pieces, 3 buildings, 2 tactics, 0 native vehicles.
    // All HP/damage/ManpowerCost numbers below are PROVISIONAL, anchored to IronMarch's own
    // authored numbers by rarity (see IronmarchUnionContentFactory.Pieces.cs for the anchor set).
    public static partial class DustScourgeContentFactory
    {
        // Straight 3-in-a-row footprint for the two 3-cell pieces in this roster (Chop-Shop,
        // Corpse-Tithe Caravan) — per spec instruction, distinct from IronMarch's L-tromino.
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

            // Common — the faction body. Scrap-shotgun: short-range shredding damage.
            SavePiece("waste_raider", "Waste Raider", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Infantry, GameTagIds.Assault, FactionIds.DustScourge, 50, 8, 1, AttackType.Shredding, ArmorType.None,
                AttackSpeedTier.Medium, AttackRangeTier.Short, 2,
                rarity: Rarity.Common),

            // Common — high-movement harasser. Fastest piece in the roster (movementSpeed 4).
            SavePiece("outrider", "Outrider", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Assault, FactionIds.DustScourge, 30, 5, 1, AttackType.Ballistic, ArmorType.None,
                AttackSpeedTier.Fast, AttackRangeTier.Medium, 4,
                rarity: Rarity.Common),

            // Common — gas count piece. combatRole reuses the Gas attack-type tag id per the
            // established convention (TagRegistry validation only checks existence, not
            // category-correctness) — no ability of its own, just a board-countable gas source
            // for Stormcaller of the Yellow Wind's Rolling Barrage below.
            SavePiece("gasflinger", "Gasflinger", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Infantry, GameTagIds.Gas, FactionIds.DustScourge, 35, 6, 1, AttackType.Gas, ArmorType.None,
                AttackSpeedTier.Slow, AttackRangeTier.Medium, 2,
                rarity: Rarity.Common),

            // Common — scrap-plated line-holder.
            SavePiece("rust_spear", "Rust Spear", PieceCategory.Unit, DemoSandboxShapes.VerticalPair,
                GameTagIds.Infantry, GameTagIds.Defender, FactionIds.DustScourge, 65, 5, 2, AttackType.Melee, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Melee, 1,
                rarity: Rarity.Common),

            // Common — +salvage chance % while fielded. No attack (support skirmisher, per
            // the spec's "—" Attack column); salvageChanceBonus is the existing SO/Core field.
            SavePiece("vulture_crew", "Vulture Crew", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Support, FactionIds.DustScourge, 30, 0, 1, AttackType.None, ArmorType.None,
                AttackSpeedTier.Medium, AttackRangeTier.Short, 2, salvageChanceBonus: 5,
                rarity: Rarity.Common),

            // Common — +Supplies/round, small +salvage chance. Mirrors neutral's supply_depot
            // pattern (synergyTags SupplyLine + flavorTags Logistics); the actual flat
            // +Supplies/round hookup is wired centrally by the integration lead afterward in
            // Core/Run/BuildingIncomeRules.cs (not touched here). Exact id: "scavengers_cache".
            SavePiece("scavengers_cache", "Scavenger's Cache", PieceCategory.Building, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.DustScourge, 50, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0,
                synergyTags: new[] { GameTagIds.SupplyLine }, flavorTags: new[] { GameTagIds.Logistics },
                rarity: Rarity.Common),

            // ---------------------------------------------------------------
            // UNCOMMON (3)
            // ---------------------------------------------------------------

            // Uncommon — command piece. combatRole is Utility (not a literal "command" role —
            // GameTagIds has no such CombatRole value) + synergyTags [Command], mirroring
            // IronMarch's Shock Sergeant pattern.
            // TODO: "Adjacent salvage-tagged pieces: +damage" — salvage is a DERIVED runtime
            // concept (OffFactionRules.IsSalvage), never an authored tag on a piece, and there is
            // no NeighborFilter/ability-trigger seam today for "is this neighbor salvage" — this
            // aura is authored against adjacent Infantry generally as the closest existing
            // primitive until that seam exists.
            SavePiece("raid_captain", "Raid Captain", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.DustScourge, 40, 6, 2, AttackType.Ballistic, ArmorType.Light,
                AttackSpeedTier.Medium, AttackRangeTier.Medium, 2, synergyTags: new[] { GameTagIds.Command },
                customAbilities: new[]
                {
                    Ability("raid_captain_adjacent_damage", PieceAbilityTrigger.AdjacentAura, SynergyStat.Damage, SynergyModType.Flat, 2,
                        neighborFilter: new NeighborFilter { PrimaryTagId = GameTagIds.Infantry })
                },
                rarity: Rarity.Uncommon),

            // Uncommon — HQ building. Footprint matches the spec's "3" exactly (Triple3).
            // TODO: "Salvage-tagged pieces +HP while it stands" has no ability seam today
            // (same salvage-is-derived-at-runtime gap as Raid Captain above) — authored here as
            // a plain building with no ability, flagged rather than faked.
            SavePiece("chop_shop", "Chop-Shop", PieceCategory.Building, Triple3,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.DustScourge, 70, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0,
                rarity: Rarity.Uncommon),

            // Uncommon — HQ building. Tactic: Gas Cloud, small area gas. Reuses IronMarch's
            // existing small-area GrantedAbility.MortarShot (flat, unscaled) per spec instruction.
            SavePiece("fume_still", "Fume Still", PieceCategory.Building, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.DustScourge, 55, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, grantedAbility: GrantedAbility.MortarShot,
                synergyTags: new[] { GameTagIds.GasCloud },
                rarity: Rarity.Uncommon),

            // ---------------------------------------------------------------
            // RARE (3)
            // ---------------------------------------------------------------

            // Rare — combat-board structure (primary: structure → Combat board, per §5's
            // machine_gun_nest gotcha; Category.Unit here, NOT Building). Footprint matches the
            // spec's "3" exactly (Triple3).
            // TODO: "Routed enemies count as kills for salvage share" is a rule-bend with no
            // PieceDefinition field — it's a run-economy/loot rule (salvage-share accounting),
            // not sim data, and has no seam today. Authored as a plain structure/support body
            // with no ability, flagged per the task's explicit allowance for genuinely-unseamed
            // effects.
            SavePiece("corpse_tithe_caravan", "Corpse-Tithe Caravan", PieceCategory.Unit, Triple3,
                GameTagIds.Structure, GameTagIds.Support, FactionIds.DustScourge, 120, 0, 3, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0,
                rarity: Rarity.Rare),

            // Rare — gas count piece + tactic. Tactic: Yellow Wind, wide gas storm scaling with
            // gas count. Reuses GrantedAbility.RollingBarrage per spec instruction.
            // NOTE (checked per instruction): TickCombatRun.cs builds
            // `_playerArtilleryCount = BuildBoardTagCounter.Count(playerBuildBoards, GameTagIds.Artillery)`
            // and passes it through as the single `artilleryCount` parameter into
            // CombatAbilityExecutor.Execute(...) → ExecuteRollingBarrage, which multiplies
            // damage by that parameter directly (RollingBarrageBaseDamage +
            // artilleryCount * RollingBarragePerArtilleryDamage). RollingBarrage is HARDCODED to
            // the `artillery` board-tag count, not generic — there is no existing wiring for a
            // `gas`-tag count to feed it. For Stormcaller, this ability will currently scale off
            // the army's Artillery-tagged piece count (likely near-zero for Dust Scourge), NOT
            // gas count. Flagging as a follow-up wire-up needed in TickCombatRun/
            // CombatAbilityExecutor to make the scaling-source tag configurable (or add a
            // parallel Gas-scaled ability) before this piece's tactic text is true in play.
            SavePiece("stormcaller_of_the_yellow_wind", "Stormcaller of the Yellow Wind", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Gas, FactionIds.DustScourge, 45, 8, 2, AttackType.Gas, ArmorType.None,
                AttackSpeedTier.Slow, AttackRangeTier.Medium, 2, grantedAbility: GrantedAbility.RollingBarrage,
                rarity: Rarity.Rare),

            // Rare — command piece; big stat buffs "per distinct faction represented" (neutral
            // excluded, tuned around 3 banners, ceiling ~4-5).
            // TODO: there is no existing primitive to count distinct non-neutral factions
            // fielded — PieceAbilityTrigger.BoardPerTagCount counts occurrences of a single tag,
            // not distinct faction ids, and PieceAbilityEngine/the Critical-Mass system has no
            // counting seam for this today. This is a genuine gap, not invented here. Authored
            // instead as a strong standalone Rare body (good stats for its rarity, no ability).
            SavePiece("warlord_of_many_banners", "Warlord of Many Banners", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.DustScourge, 100, 10, 3, AttackType.Melee, ArmorType.Medium,
                AttackSpeedTier.Medium, AttackRangeTier.Melee, 2, synergyTags: new[] { GameTagIds.Command },
                rarity: Rarity.Rare)
        };
    }
}
