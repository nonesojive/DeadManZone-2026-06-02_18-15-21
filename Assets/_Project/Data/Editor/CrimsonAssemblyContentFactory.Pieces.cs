using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    // 2026-07-15 faction-roster-v1-design.md §2.8: Crimson Assembly ("Clinical Optimization").
    // 6C / 3U / 3R = 12 pieces. All HP/damage/ManpowerCost numbers are PROVISIONAL (balance
    // pass pending), leaning at or above IronMarch's numbers per the spec's "best-equipped
    // faction" framing. requisitionCost left at SO default (0) everywhere per instructions.
    public static partial class CrimsonAssemblyContentFactory
    {
        // Local straight-3 footprint for Operations Bunker (plain, building, footprint 3) —
        // matches the spec's Cells column exactly; not reused elsewhere so it isn't worth
        // promoting to DemoSandboxShapes.
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

            // "Best-equipped common in the game" — highest common HP (80) of any faction's
            // roster authored so far (beats IronMarch's Iron Guard at 70), plus the top of the
            // Common ManpowerCost band (2). Armor stays at Light exactly per the spec's Cells
            // table (not upgraded to Medium/Heavy — that's an explicit table value, not a
            // provisional stat).
            SavePiece("assembly_trooper", "Assembly Trooper", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Infantry, GameTagIds.Assault, FactionIds.CrimsonAssembly, 80, 9, 2, AttackType.Ballistic, ArmorType.Light,
                AttackSpeedTier.Medium, AttackRangeTier.Medium, 2, flavorTags: new[] { GameTagIds.SmallArms },
                rarity: Rarity.Common),

            // The count piece for Crimson's ONE tentpole debuff family. "Weak suppression" is
            // flavor only: SuppressionRules has a single fixed duration/magnitude dial
            // (SuppressionDurationTicks + the attacker's CM-granted bonus) — there's no
            // per-piece "weak vs strong" seam yet, so this piece's on-hit application is
            // identical in mechanical strength to Vanquisher's and Stiller's. abilityTags
            // carries the existing `suppression` keyword tag (KeywordTagCatalog) for
            // tooltip/UI discoverability only — appliesSuppressionOnHit is what actually fires.
            SavePiece("suppression_team", "Suppression Team", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Infantry, GameTagIds.Assault, FactionIds.CrimsonAssembly, 40, 6, 1, AttackType.Ballistic, ArmorType.None,
                AttackSpeedTier.Medium, AttackRangeTier.Medium, 2, abilityTags: new[] { GameTagIds.Suppression },
                rarity: Rarity.Common, appliesSuppressionOnHit: true),

            // Sealed-suit line troops — defender body, no ability (identity carried by armor
            // tier + role, same "no custom ability needed" pattern as IronMarch's line_grenadiers).
            SavePiece("hazmat_vanguard", "Hazmat Vanguard", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Infantry, GameTagIds.Defender, FactionIds.CrimsonAssembly, 65, 5, 2, AttackType.Ballistic, ArmorType.Medium,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 1, flavorTags: new[] { GameTagIds.SmallArms },
                rarity: Rarity.Common),

            // "Adjacent ranged pieces: +accuracy" — TODO: SynergyStat (Core/Tags/SynergyStat.cs)
            // has no Accuracy-related member (Damage, AttackRange, AttackSpeedSteps,
            // MovementSpeed, ArmorType, MoveChargePercent, MaxHp, MoraleResistancePercent only).
            // Authored as a plain support body with no customAbilities rather than faking the
            // effect onto an unrelated stat. Add a real accuracy aura once a SynergyStat.Accuracy
            // (or similar) member exists.
            SavePiece("ballistics_analyst", "Ballistics Analyst", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Support, FactionIds.CrimsonAssembly, 30, 0, 1, AttackType.None, ArmorType.None,
                AttackSpeedTier.Medium, AttackRangeTier.Short, 2,
                rarity: Rarity.Common),

            // "+Supplies/round" — TODO: BuildingIncomeRules.SumSuppliesFlatBonus
            // (Core/Run/BuildingIncomeRules.cs) hardcodes id == "supply_depot" for the +5/round
            // bonus; it has no hook for any other id or for a synergy tag like SupplyLine. This
            // piece's stated effect has no wiring seam yet and does nothing mechanically until
            // that rule is generalized (out of scope for this pass — a shared Core file).
            SavePiece("research_annex", "Research Annex", PieceCategory.Building, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.CrimsonAssembly, 50, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, synergyTags: new[] { GameTagIds.SupplyLine }, flavorTags: new[] { GameTagIds.Logistics },
                rarity: Rarity.Common),

            // Combat-board strongpoint — primary "structure" places it on the Combat board
            // (PieceCategory.Unit), mirroring machine_gun_nest's §5 gotcha. No custom ability;
            // identity is armor/role alone, same as Hazmat Vanguard.
            SavePiece("bunker_emplacement", "Bunker Emplacement", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Structure, GameTagIds.Defender, FactionIds.CrimsonAssembly, 80, 6, 2, AttackType.Ballistic, ArmorType.Medium,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, flavorTags: new[] { GameTagIds.Fortification },
                rarity: Rarity.Common),

            // ---------------------------------------------------------------
            // Uncommon (3)
            // ---------------------------------------------------------------

            // The one sanctioned Uncommon vehicle exception in the entire game. combatRole is
            // GameTagIds.Tank directly (plugs into the existing `tank` CM rule automatically,
            // per the design decision — no Utility+synergy indirection needed here). "Small
            // terror" = a modest terrorDamage/maxMorale pair, well under the Rares' numbers.
            // Tactic *Smoke Discharge* ("drops enemy accuracy in an area") uses
            // GrantedAbility.MortarShot as a PROVISIONAL stand-in — CombatAbilityExecutor's
            // ExecuteMortarShot (see CombatAbilityExecutor.cs) is damage-only (ApplyAreaDamage);
            // it does not touch accuracy or any debuff. This is a flat unscaled area strike
            // wearing Smoke Discharge's name until a real accuracy-debuff ability seam exists.
            SavePiece("scout_tankette", "Scout Tankette", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Vehicle, GameTagIds.Tank, FactionIds.CrimsonAssembly, 55, 6, 2, AttackType.Ballistic, ArmorType.Light,
                AttackSpeedTier.Medium, AttackRangeTier.Medium, 3, grantedAbility: GrantedAbility.MortarShot,
                rarity: Rarity.Uncommon, maxMorale: 35, terrorDamage: 8),

            // combatRole is Utility (not a literal "command" GameTagIds value) + synergyTags
            // [Command] for the flavor/CM identity — same pattern IronMarch's Shock Sergeant and
            // Marksman-Doctrine Officer use. "Adjacent ballistic pieces: +damage" mirrors Shock
            // Sergeant's ability exactly, with the same approximation Shock Sergeant already
            // makes: NeighborFilter (Core/Tags/NeighborFilter.cs) has no AttackType-matching
            // field at all (PrimaryTagId/CombatRoleTagId/SystemTagId/SynergyTagId/AbilityTagId
            // only) — so "ballistic pieces" is approximated as PrimaryTagId = Infantry (broad),
            // not filtered to AttackType.Ballistic specifically. Flagged here, not a regression
            // introduced by this pass.
            SavePiece("fire_plan_officer", "Fire-Plan Officer", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.CrimsonAssembly, 30, 4, 1, AttackType.Ballistic, ArmorType.Light,
                AttackSpeedTier.Medium, AttackRangeTier.Medium, 2, synergyTags: new[] { GameTagIds.Command },
                customAbilities: new[]
                {
                    Ability("fire_plan_officer_adjacent_ballistic_damage", PieceAbilityTrigger.AdjacentAura, SynergyStat.Damage, SynergyModType.Flat, 2,
                        neighborFilter: new NeighborFilter { PrimaryTagId = GameTagIds.Infantry })
                },
                rarity: Rarity.Uncommon),

            // HQ building, footprint 3 (Triple3, matches §2.8's Cells column exactly). Tactic
            // *Suppressive Sweep* ("suppress the enemy front rank") uses GrantedAbility.MortarShot
            // as the same damage-only PROVISIONAL stand-in as Scout Tankette's Smoke Discharge —
            // see CombatAbilityExecutor.ExecuteMortarShot, confirmed damage-only, no Suppression
            // touch anywhere in CombatAbilityExecutor.cs.
            //
            // TODO: setting appliesSuppressionOnHit on THIS piece would not make Suppressive
            // Sweep genuinely apply Suppression. Two independent reasons: (1) Buildings are
            // never spawned as combatants — CombatAbilityExecutor.Execute's hqBoard branch
            // stubs HQ-sourced pieces as a CurrentHp=1 CombatantState purely to authorize the
            // granted ability; it never enters the regular attack-resolution loop where
            // AppliesSuppressionOnHit is actually checked (TickCombatRun, "on-hit only" per
            // SuppressionRules' header). Operations Bunker, being PieceCategory.Building, has no
            // on-hit attack cycle at all to hang the flag on. (2) Even for a piece that DID
            // attack, GrantedAbility execution is a wholly separate code path from the per-tick
            // attack resolution that checks the flag — CombatAbilityExecutor never reads
            // AppliesSuppressionOnHit. A genuine "apply Suppression via tactic" ability has no
            // seam yet; wiring one is out of scope here (CombatAbilityExecutor.cs is a shared,
            // read-only file for this pass).
            SavePiece("operations_bunker", "Operations Bunker", PieceCategory.Building, Triple3,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.CrimsonAssembly, 90, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, grantedAbility: GrantedAbility.MortarShot,
                rarity: Rarity.Uncommon),

            // ---------------------------------------------------------------
            // Rare (3)
            // ---------------------------------------------------------------

            // Terror >= 2x damage (terrorDamage 20 = exactly 2x the 10 baseDamage, mirrors
            // Breakthrough Tank's 16/8 = 2x convention). Suppresses on hit — one of Crimson's
            // three appliesSuppressionOnHit pieces. Tactic *Cannon Blast* adopts the orphaned
            // GrantedAbility.CannonBlast exactly per the spec's own note — CombatAbilityExecutor
            // already implements ExecuteCannonBlast (primary + splash Explosive damage) and
            // gates it to checkpointIndex == 1 (2nd pause window only, cost 4 Authority) via
            // CanUseAtPause/GetAuthorityCost; no further wiring needed here.
            SavePiece("vanquisher_doctrine_tank", "\"Vanquisher\" Doctrine Tank", PieceCategory.Unit, DemoSandboxShapes.Square2x2,
                GameTagIds.Vehicle, GameTagIds.Tank, FactionIds.CrimsonAssembly, 140, 10, 5, AttackType.Ballistic, ArmorType.Heavy,
                AttackSpeedTier.Slow, AttackRangeTier.Medium, 1, grantedAbility: GrantedAbility.CannonBlast,
                flavorTags: new[] { GameTagIds.Shells },
                rarity: Rarity.Rare, maxMorale: 50, terrorDamage: 20, appliesSuppressionOnHit: true),

            // "Low damage, high terror": baseDamage (4) kept below Vanquisher's (10);
            // terrorDamage (24) is still >= 2x its own baseDamage but pushed well past the
            // minimum per the "high terror" flavor call. "Area suppression every volley" is
            // simplified to on-hit single-target Suppression (appliesSuppressionOnHit) firing
            // every attack tick — the wired mechanic is on-hit, not area; no radius parameter
            // exists on SuppressionRules.Apply to model "every volley hits an area" literally.
            // No grantedAbility: the spec only requires this for the pause-window Tactics, and
            // Stiller's "every volley" behavior is exactly what the on-hit flag already gives it
            // on its regular attacks, with no pause-window ability required.
            SavePiece("stiller_suppression_platform", "\"Stiller\" Suppression Platform", PieceCategory.Unit, DemoSandboxShapes.Square2x2,
                GameTagIds.Vehicle, GameTagIds.Tank, FactionIds.CrimsonAssembly, 130, 4, 5, AttackType.Ballistic, ArmorType.Heavy,
                AttackSpeedTier.Fast, AttackRangeTier.Medium, 1,
                flavorTags: new[] { GameTagIds.Shells },
                rarity: Rarity.Rare, maxMorale: 50, terrorDamage: 24, appliesSuppressionOnHit: true),

            // combatRole Utility + synergyTags [Command], same pattern as Fire-Plan Officer.
            // Tactic *Fire Mission* ("suppress a targeted area, deliberately NOT army-wide") uses
            // GrantedAbility.MortarShot as the area-targeting stand-in — same damage-only
            // limitation as Scout Tankette's Smoke Discharge and Operations Bunker's Suppressive
            // Sweep (CombatAbilityExecutor.ExecuteMortarShot never touches Suppression).
            //
            // TODO: "+1 Authority pool" has no generic wiring seam either.
            // AuthorityCalculator.ComputeRoundPool(BuildBoardSet) delegates entirely to
            // BuildingIncomeRules.SumAuthorityFromBuildings, which hardcodes
            // `piece.Definition.Id == "command_outpost"` (IronMarch's building) as the only
            // Authority-granting id in the game; it has no hook for any other id, and Director
            // of Programs is Infantry (not even PieceCategory.Building), so it wouldn't qualify
            // even if the id check were widened to a tag. This piece's stated Authority bonus
            // does nothing mechanically until that rule is generalized — out of scope here
            // (Core/Run/BuildingIncomeRules.cs and AuthorityCalculator.cs are shared, read-only
            // files for this pass).
            SavePiece("director_of_programs", "Director of Programs", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.CrimsonAssembly, 40, 0, 2, AttackType.None, ArmorType.None,
                AttackSpeedTier.Medium, AttackRangeTier.Short, 1, synergyTags: new[] { GameTagIds.Command },
                grantedAbility: GrantedAbility.MortarShot,
                rarity: Rarity.Rare)
        };
    }
}
