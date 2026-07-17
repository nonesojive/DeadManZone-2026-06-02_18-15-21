using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    // 2026-07-15 faction-roster-v1-design.md §2.4: Cartel of Echoes ("War as Profit").
    // 6 Common / 3 Uncommon / 3 Rare. All HP/damage/ManpowerCost numbers are PROVISIONAL
    // (balance pass pending), anchored to IronMarch's own numbers by rarity per the spec's own
    // instruction, but leaning toward the higher end of each band — Cartel is explicitly
    // "better-equipped, pricier" (Company Rifleman/Strikebreaker get ArmorType.Light at Common,
    // already above IronMarch's ArmorType.None commons). requisitionCost left at 0 throughout
    // (RarityPricing handles shop price at runtime — matches IronMarch convention).
    public static partial class CartelOfEchoesContentFactory
    {
        // 3-cell footprint for the two Cartel buildings the spec's Cells column marks "3"
        // (Freight Depot, Executive Suite) — a plain horizontal tromino, distinct from
        // IronmarchUnionContentFactory's L-shaped Tromino3.
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

            // PMC trooper — better-equipped, pricier line body. ArmorType.Light at Common is
            // the faction's stated identity marker versus IronMarch's ArmorType.None commons.
            SavePiece("company_rifleman", "Company Rifleman", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Infantry, GameTagIds.Assault, FactionIds.CartelOfEchoes, 65, 7, 2, AttackType.Ballistic, ArmorType.Light,
                AttackSpeedTier.Medium, AttackRangeTier.Medium, 2, flavorTags: new[] { GameTagIds.SmallArms },
                rarity: Rarity.Common),

            // Riot-shield muscle — high HP/armor, low damage, melee. Identity carried entirely
            // by stats; no custom ability needed.
            SavePiece("strikebreaker", "Strikebreaker", PieceCategory.Unit, DemoSandboxShapes.VerticalPair,
                GameTagIds.Infantry, GameTagIds.Defender, FactionIds.CartelOfEchoes, 80, 4, 2, AttackType.Melee, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Melee, 1,
                rarity: Rarity.Common),

            // Close-range collections — glass-cannon Shredding body, no armor. Identity carried
            // by the Shredding attack-type triangle, no custom ability needed.
            SavePiece("repo_crew", "Repo Crew", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Assault, FactionIds.CartelOfEchoes, 35, 9, 1, AttackType.Shredding, ArmorType.None,
                AttackSpeedTier.Fast, AttackRangeTier.Short, 2,
                rarity: Rarity.Common),

            // Non-combat support body, `supply_line` tagged. "Forms Muster pairs" is NOT a
            // TODO: MusterCalculator.CountSupplySynergyBonus (Core/Run/MusterCalculator.cs)
            // already counts touching-pair adjacency between any two Supplier/SupplyLine
            // synergy-tagged pieces on a board and grants +2 Muster/shop per pair — this piece
            // plugs straight into that existing mechanic, no new Core wiring needed. Pair it
            // with Freight Depot (also tagged, see below) or another supply_line piece.
            SavePiece("paymasters_aide", "Paymaster's Aide", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Support, FactionIds.CartelOfEchoes, 30, 0, 1, AttackType.None, ArmorType.None,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 2, synergyTags: new[] { GameTagIds.SupplyLine },
                rarity: Rarity.Common),

            // `supplier` Critical-Mass tag (Core/Tags/CriticalMassDefaultRules.cs "supplier" rule,
            // RunResources scope, flat Supplies at 2/4/6/8 thresholds) — tagging alone plugs into
            // it, no new CM work. The actual guaranteed flat "+Supplies/round" per copy is wired
            // centrally by BuildingIncomeRules.cs (id-hardcoded there, not edited by this file) —
            // id must stay exactly "freight_depot" for that hookup.
            SavePiece("freight_depot", "Freight Depot", PieceCategory.Building, Triple3,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.CartelOfEchoes, 60, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, synergyTags: new[] { GameTagIds.Supplier },
                rarity: Rarity.Common),

            // +Muster/shop via the generic, data-driven musterPerShop param — no Core wiring
            // needed (unlike Freight Depot's Supplies-flat case above).
            SavePiece("company_store", "Company Store", PieceCategory.Building, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.CartelOfEchoes, 40, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, musterPerShop: 1,
                rarity: Rarity.Common),

            // ---------------------------------------------------------------
            // Uncommon (3)
            // ---------------------------------------------------------------

            // Command body: combatRole is Utility (GameTagIds has no literal "command" combat
            // role) + synergyTags [Command] for the flavor/CM identity — mirrors IronMarch's
            // Shock Sergeant pattern. "Adjacent mercenaries: +damage" has NO ability seam today:
            // NeighborFilter (Core/Tags/NeighborFilter.cs) only matches PieceDefinition-level
            // tags (Primary/CombatRole/SystemTag/SynergyTags/AbilityTags); mercenary status is
            // PlacedPiece.IsMercenary, a runtime acquisition flag never surfaced as a tag
            // (OffFactionRules.IsMercenary) — there is nothing for a NeighborFilter to match on.
            // TODO: needs a NeighborFilter.IsMercenary (or equivalent) seam before this ability
            // can be authored; given a strong flat Uncommon command body instead in the interim.
            SavePiece("contract_officer", "Contract Officer", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.CartelOfEchoes, 40, 6, 2, AttackType.Ballistic, ArmorType.Light,
                AttackSpeedTier.Medium, AttackRangeTier.Medium, 2, synergyTags: new[] { GameTagIds.Command },
                rarity: Rarity.Uncommon),

            // "+1 Authority per `command` piece" is EXACTLY the existing "command" Critical-Mass
            // rule (Core/Tags/CriticalMassDefaultRules.cs, RunResources scope, counts
            // GameTagIds.Command synergy tag globally, thresholds 2/4/6/8 → Authority) — already
            // wired and faction-agnostic. This piece needs no custom ability; it's a plain
            // utility building that rides that generic rule alongside every other Command-tagged
            // piece on the board (Contract Officer, Freelance Colonel, Echo Chairman included).
            SavePiece("executive_suite", "Executive Suite", PieceCategory.Building, Triple3,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.CartelOfEchoes, 70, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0,
                rarity: Rarity.Uncommon),

            // Tactic: Overtime Bonus — "all units +1 attack-speed tier for a stretch". No
            // bespoke GrantedAbility exists for this; reuses GrantedAbility.ShieldAllies (the
            // existing team-wide temporary-buff carrier) as a stand-in for the Overtime Bonus
            // flavor, per the spec's explicit instruction.
            SavePiece("munitions_exchange", "Munitions Exchange", PieceCategory.Building, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.CartelOfEchoes, 55, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, grantedAbility: GrantedAbility.ShieldAllies,
                rarity: Rarity.Uncommon),

            // ---------------------------------------------------------------
            // Rare (3)
            // ---------------------------------------------------------------

            // Command body, same Utility+Command pattern as Contract Officer. "Merc surcharge
            // 25%→10%" — checked Core/FactionPassives.cs: MercenarySurchargeFor(factionId) is an
            // unconditional flat 25% for the whole Cartel faction (FactionPassives.
            // MercenarySurchargePercent), with a comment saying Freelance Colonel is meant to
            // hook in later but NO conditional branch on this specific piece being fielded
            // exists yet. TODO: FactionPassives.MercenarySurchargeFor needs a piece-fielded
            // check (e.g. "is freelance_colonel on the board") before the 25→10 reduction is
            // actually Freelance-Colonel-conditional; today every Cartel run gets the discount
            // whether or not this piece is in play — flagging, not fixing (read-only file).
            // "All mercenaries +HP/+damage" also has no seam: mercenary is PlacedPiece.
            // IsMercenary, a runtime flag, not a tag NeighborFilter can match (same gap as
            // Contract Officer above) — TODO: needs a NeighborFilter.IsMercenary equivalent.
            // Given strong flat stats as a Rare command body in the interim.
            SavePiece("freelance_colonel", "Freelance Colonel", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.CartelOfEchoes, 95, 10, 3, AttackType.Ballistic, ArmorType.Medium,
                AttackSpeedTier.Medium, AttackRangeTier.Medium, 2, synergyTags: new[] { GameTagIds.Command },
                rarity: Rarity.Rare),

            // Command body, non-combatant (no attack, no armor). Flavor only: "+2 Authority
            // pool; orders cost 1 less" — there is no PieceDefinition field for a flat Authority-
            // pool grant or an order-cost discount, so nothing to author beyond the Command tag
            // riding the same generic CM rule as the other command pieces. TODO: needs new
            // PieceDefinition fields (or a dedicated rule) for "Authority pool +N" and
            // "order cost -N" — genuinely unseamed today.
            // Tactic: Executive Order — "one free order at any window". No existing
            // GrantedAbility maps to granting a free command action (every enum value —
            // MortarShot/ShieldAllies/CannonBlast/RollingBarrage/Echo — is an attack/buff effect
            // fired at a pause window, not a CommandAction-grant mechanism). Left at
            // GrantedAbility.None. TODO: needs new tech beyond the current GrantedAbility ledger
            // (a CommandAction-grant trigger) — flagged, not worked around.
            SavePiece("echo_chairman", "Echo Chairman", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.CartelOfEchoes, 40, 0, 3, AttackType.None, ArmorType.None,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 1, synergyTags: new[] { GameTagIds.Command },
                grantedAbility: GrantedAbility.None,
                rarity: Rarity.Rare),

            // Combat-board utility body. "+1 damage per 25 Supplies held at fight start, capped
            // ~+4" has no read seam: no existing PieceAbilityTrigger reads Supplies-at-fight-
            // start (all triggers are board-adjacency/count-based, not economy-state-based).
            // TODO: needs a new PieceAbilityTrigger (or an equivalent fight-start hook) that
            // reads RunResources.Supplies at combat start and applies a capped scaling damage
            // buff — genuinely unseamed today. Given a strong flat Rare body in the interim.
            SavePiece("war_profiteer", "War Profiteer", PieceCategory.Unit, DemoSandboxShapes.VerticalPair,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.CartelOfEchoes, 75, 9, 3, AttackType.Ballistic, ArmorType.Light,
                AttackSpeedTier.Medium, AttackRangeTier.Medium, 2,
                rarity: Rarity.Rare)
        };
    }
}
