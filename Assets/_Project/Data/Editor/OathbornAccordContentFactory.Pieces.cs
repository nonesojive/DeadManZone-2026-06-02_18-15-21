using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    // 2026-07-15 faction-roster-v1-design.md §2.5: Oathborn Accord ("Peacekeepers Turned
    // Crusaders") — 6C/3U/3R. Design spine: "how do we cross the field without breaking?
    // Morale so they don't rout en route, medics so they survive arriving, one transport
    // that skips the walk." All HP/damage/ManpowerCost numbers are PROVISIONAL (balance pass
    // pending), anchored to IronMarch's numbers by rarity per the spec's own instruction.
    public static partial class OathbornAccordContentFactory
    {
        // Plain (non-L) 3-cell straight line — Pilgrim Spears' footprint per §2.5's Cells column.
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

            // Common — riot-shield peacekeepers, the faction's melee assault line body.
            SavePiece("truncheon_line", "Truncheon Line", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Infantry, GameTagIds.Assault, FactionIds.OathbornAccord, 55, 6, 1, AttackType.Melee, ArmorType.Light,
                AttackSpeedTier.Medium, AttackRangeTier.Melee, 2,
                rarity: Rarity.Common),

            // Common — cheap swarm, melee count piece. Glassiest/cheapest common per the spec.
            SavePiece("pilgrim_spears", "Pilgrim Spears", PieceCategory.Unit, Triple3,
                GameTagIds.Infantry, GameTagIds.Assault, FactionIds.OathbornAccord, 35, 4, 1, AttackType.Melee, ArmorType.None,
                AttackSpeedTier.Medium, AttackRangeTier.Melee, 2,
                rarity: Rarity.Common),

            // Common — shield wall anchor, tankiest common per the spec.
            SavePiece("vow_warden", "Vow Warden", PieceCategory.Unit, DemoSandboxShapes.VerticalPair,
                GameTagIds.Infantry, GameTagIds.Defender, FactionIds.OathbornAccord, 75, 5, 2, AttackType.Melee, ArmorType.Medium,
                AttackSpeedTier.Slow, AttackRangeTier.Melee, 1,
                rarity: Rarity.Common),

            // Common — "Adjacent allies +morale". TODO: PieceAbilityEngine/SynergyStat has no
            // per-adjacency morale-restore or max-morale stat (SynergyStat only covers Damage,
            // AttackRange, AttackSpeedSteps, MovementSpeed, ArmorType, MoveChargePercent, MaxHp,
            // MoraleResistancePercent — confirmed in SynergyStat.cs). The army-wide morale bonus
            // system lives in CriticalMassCombatModifiers, a different seam than per-adjacency
            // auras. Authored as a plain support body until a MaxMorale/MoraleRestore SynergyStat
            // lands; do not invent a new field for this pass.
            SavePiece("banner_bearer", "Banner Bearer", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Support, FactionIds.OathbornAccord, 30, 0, 1, AttackType.None, ArmorType.None,
                AttackSpeedTier.Slow, AttackRangeTier.Melee, 2, synergyTags: new[] { GameTagIds.Inspiring },
                rarity: Rarity.Common),

            // Common — heals nearby allies during combat. Primary carrier (with Field
            // Chirurgeon) of Oathborn's in-combat healing identity: HealPulseRules reads
            // HealPulseAmount/Radius/IntervalTicks directly off this piece, no further wiring.
            SavePiece("mercy_sister", "Mercy Sister", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Support, FactionIds.OathbornAccord, 30, 0, 1, AttackType.None, ArmorType.None,
                AttackSpeedTier.Slow, AttackRangeTier.Melee, 2, synergyTags: new[] { GameTagIds.Medic },
                rarity: Rarity.Common,
                healPulseAmount: 6, healPulseRadius: 2, healPulseIntervalTicks: 25),

            // Common — +Muster/shop economy building.
            SavePiece("oathhall", "Oathhall", PieceCategory.Building, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.OathbornAccord, 45, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, musterPerShop: 1, flavorTags: new[] { GameTagIds.Logistics },
                rarity: Rarity.Common),

            // ---------------------------------------------------------------
            // UNCOMMON (3)
            // ---------------------------------------------------------------

            // Uncommon — "adjacent allies take reduced morale damage", the anti-terror tech.
            // Copies Breakthrough Tank's exact pattern (breakthrough_tank_infantry_morale_resist):
            // AdjacentAura + SynergyStat.MoraleResistancePercent. combatRole is Utility (not a
            // literal "command" role — GameTagIds has no such CombatRole value) + synergyTags
            // [Command] per this pass's mapping rule.
            SavePiece("confessor", "Confessor", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.OathbornAccord, 40, 5, 2, AttackType.Melee, ArmorType.Light,
                AttackSpeedTier.Medium, AttackRangeTier.Melee, 2, synergyTags: new[] { GameTagIds.Command },
                customAbilities: new[]
                {
                    Ability("confessor_adjacent_morale_resist", PieceAbilityTrigger.AdjacentAura, SynergyStat.MoraleResistancePercent, SynergyModType.Percent, 20,
                        radius: 2)
                },
                rarity: Rarity.Uncommon),

            // Uncommon — stronger in-combat healing aura than Mercy Sister (bigger amount,
            // faster cadence) per the spec's "stronger" sketch.
            SavePiece("field_chirurgeon", "Field Chirurgeon", PieceCategory.Unit, DemoSandboxShapes.VerticalPair,
                GameTagIds.Infantry, GameTagIds.Support, FactionIds.OathbornAccord, 50, 0, 2, AttackType.None, ArmorType.None,
                AttackSpeedTier.Slow, AttackRangeTier.Melee, 2, synergyTags: new[] { GameTagIds.Medic },
                rarity: Rarity.Uncommon,
                healPulseAmount: 12, healPulseRadius: 2, healPulseIntervalTicks: 20),

            // Uncommon — HQ building. "Rally — restore morale to all units": no bespoke Rally
            // GrantedAbility exists, so this reuses GrantedAbility.ShieldAllies as the army-wide
            // restorative buff carrier (stand-in for the "Rally" morale-restore flavor per this
            // pass's mapping rule).
            SavePiece("sanctum_command", "Sanctum Command", PieceCategory.Building, Triple3,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.OathbornAccord, 80, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, grantedAbility: GrantedAbility.ShieldAllies,
                rarity: Rarity.Uncommon),

            // ---------------------------------------------------------------
            // RARE (3)
            // ---------------------------------------------------------------

            // Rare — the Armored Ark: Oathborn's ONE new-system mechanic (§1.8/§2.5 transport
            // tentpole) and the only transport-role piece in the game. isTransport/transportCapacity
            // are the only two fields TransportRules reads (confirmed in TransportRules.cs — it's
            // pure data manipulation over CombatantState.EmbarkedCargoIds/IsEmbarked, which the
            // sim populates elsewhere; no additional PieceDefinition field is missing). No attack
            // per the spec's Attack="—"; heavy armor, high HP like Grand Battery's scale.
            // 2026-07-17 round-2 playtest fix: cargo hold is a real 2x2 mini board
            // (BoardState.CargoGridWidth/Height) — 4 total cells, footprint-fit gated, not a
            // flat piece count. transportCapacity now documents that total (was 3, a stale
            // piece-count value from before the fit rule existed).
            SavePiece("armored_ark", "Armored Ark", PieceCategory.Unit, DemoSandboxShapes.Square2x2,
                GameTagIds.Vehicle, GameTagIds.Transport, FactionIds.OathbornAccord, 150, 0, 5, AttackType.None, ArmorType.Heavy,
                AttackSpeedTier.Slow, AttackRangeTier.Melee, 1,
                rarity: Rarity.Rare, maxMorale: 50,
                isTransport: true, transportCapacity: 4),

            // Rare — "Units at full morale +damage; all morale damage taken halved". The halved
            // half maps to the piece's own MoraleDamageResistancePercent (50%, like Iron Guard's
            // stat but bigger). The "all units" (whole-army) part has no whole-board
            // PieceAbilityTrigger (PieceAbilityTrigger only has AdjacentAura/FightStart/
            // BoardPerTagCount — no board-wide-apply-to-every-ally option to reuse the way
            // marksman_doctrine_officer's BoardPerTagCount does for counting); approximated here
            // as an AdjacentAura at a radius (10) larger than the 6x6 combat board so it reaches
            // every ally in practice, rather than inventing a new trigger. TODO: "at full morale
            // +damage" has no seam — nothing in the sim reads "is this piece currently at 100%
            // morale" (MoraleResistancePercent only discounts incoming damage, it doesn't gate a
            // damage bonus on current morale state).
            // §1.7 tactics budget: Oathborn needs >=2 tactics pieces (sanctum_command was the
            // only one). No bespoke "Rally"/commander-shout GrantedAbility exists, so — same
            // stand-in convention as sanctum_command — High Exarch also carries ShieldAllies as
            // its pause-window tactic, fitting a commander's "hold the line" activation on top
            // of its always-on morale-resist aura.
            SavePiece("high_exarch", "High Exarch", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.OathbornAccord, 70, 8, 3, AttackType.Melee, ArmorType.Medium,
                AttackSpeedTier.Medium, AttackRangeTier.Melee, 2, synergyTags: new[] { GameTagIds.Command },
                grantedAbility: GrantedAbility.ShieldAllies,
                customAbilities: new[]
                {
                    Ability("high_exarch_army_morale_resist", PieceAbilityTrigger.AdjacentAura, SynergyStat.MoraleResistancePercent, SynergyModType.Percent, 20,
                        radius: 10)
                },
                rarity: Rarity.Rare, moraleDamageResistancePercent: 50),

            // Rare — "healing scales with support count": no seam exists (SynergyStat has no
            // HealPulse entry to scale via BoardPerTagCount, confirmed above), so this piece
            // gets its own strong flat heal-pulse numbers instead of count-scaled ones.
            // TODO: support-count-scaled healing has no seam yet — SynergyStat would need a
            // HealPulseAmount entry and HealPulseRules would need to read an aura-modified value
            // instead of the flat Definition field.
            // "Wounded survivors cost less Manpower": no seam either — ManpowerCalculator.
            // ComputeCasualties hardcodes a check for the literal piece id "field_hospital"
            // (see ManpowerCalculator.cs / FieldHospitalSurvivorCasualtyReductionPercent) rather
            // than scanning for a synergy tag or piece flag, so Hospitaller-General can't hook in
            // without a Core change.
            // TODO: post-fight Manpower-casualty reduction for Hospitaller-General has no seam —
            // would need ManpowerCalculator extended beyond its single hardcoded field_hospital id.
            SavePiece("hospitaller_general", "Hospitaller-General", PieceCategory.Unit, DemoSandboxShapes.VerticalPair,
                GameTagIds.Infantry, GameTagIds.Support, FactionIds.OathbornAccord, 60, 0, 3, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Melee, 2, synergyTags: new[] { GameTagIds.Medic },
                rarity: Rarity.Rare,
                healPulseAmount: 18, healPulseRadius: 2, healPulseIntervalTicks: 20)
        };
    }
}
