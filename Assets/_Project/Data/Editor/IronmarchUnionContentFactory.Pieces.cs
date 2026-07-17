using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static partial class IronmarchUnionContentFactory
    {
        // 2026-07-15 faction-roster-v1-design.md §2.1 (Neutral) + §2.2 (IronMarch Union).
        // All HP/damage/ManpowerCost numbers below are PROVISIONAL (balance pass pending) —
        // anchored to the closest pre-existing piece per the spec's own instruction (roughly
        // C 10-15 Supplies / U 15-25 / R 20-30, priced by rarity via RarityPricing, not authored
        // here). Two footprints deviate from the spec's authored Cells column and are called out
        // below; every other piece's shape matches the spec exactly.
        //
        // Reused-shape 3-cell L-tromino (matches DemoSandboxShapes.TransportL) for pieces the
        // spec marks as footprint 3.
        private static readonly Vector2Int[] Tromino3 =
        {
            Vector2Int.zero,
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        };

        internal static PieceDefinitionSO[] CreatePieces() => new[]
        {
            // ---------------------------------------------------------------
            // NEUTRAL — "The War's Flotsam" (§2.1): 4C / 3U / 0R, no vehicles/tactics/rares.
            // ---------------------------------------------------------------

            // Common — baseline body, no text.
            SavePiece("militia_squad", "Militia Squad", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Infantry, GameTagIds.Assault, "neutral", 45, 5, 1, AttackType.Ballistic, ArmorType.None,
                AttackSpeedTier.Medium, AttackRangeTier.Medium, 2,
                rarity: Rarity.Common),

            // Common — adjacent allies +HP, the 1-cell gap-filler. Same id/ability as before;
            // downgraded from Uncommon to Common per §2.1.
            SavePiece("field_medic", "Field Medic", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Support, "neutral", 30, 3, 1, AttackType.Ballistic, ArmorType.None,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 2,
                synergyTags: new[] { GameTagIds.Medic }, flavorTags: new[] { GameTagIds.SmallArms },
                customAbilities: new[]
                {
                    Ability("field_medic_adjacent_infantry_hp", PieceAbilityTrigger.AdjacentAura, SynergyStat.MaxHp, SynergyModType.Flat, 10,
                        neighborFilter: new NeighborFilter { PrimaryTagId = GameTagIds.Infantry })
                },
                rarity: Rarity.Common),

            // Common — +5 Supplies/round. Id kept identical: BuildingIncomeRules hardcodes
            // "supply_depot" for the flat bonus. Downgraded from Uncommon to Common per §2.1.
            SavePiece("supply_depot", "Supply Depot", PieceCategory.Building, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Building, GameTagIds.Utility, "neutral", 50, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, synergyTags: new[] { GameTagIds.SupplyLine }, flavorTags: new[] { GameTagIds.Logistics },
                rarity: Rarity.Common),

            // Common — +Muster/shop. Footprint grown from 1 to 2 cells to match §2.1's Cells column.
            SavePiece("recruitment_office", "Recruitment Office", PieceCategory.Building, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Building, GameTagIds.Utility, "neutral", 35, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, musterPerShop: 1, flavorTags: new[] { GameTagIds.Logistics },
                rarity: Rarity.Common),

            // Uncommon — small terror ping, the mechanic's teaser before a tank. Id/footprint/
            // terror kept identical to the old rare version; role reclassified Utility→Defender
            // and armor Heavy→Light per §2.1 (both changes are combat-neutral or spec-authored).
            SavePiece("machine_gun_nest", "Machine Gun Nest", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Structure, GameTagIds.Defender, "neutral", 100, 2, 2, AttackType.Ballistic, ArmorType.Light,
                AttackSpeedTier.Medium, AttackRangeTier.Medium, 0, synergyTags: new[] { GameTagIds.Entrenched }, flavorTags: new[] { GameTagIds.Fortification },
                rarity: Rarity.Uncommon, maxMorale: 40, terrorDamage: 4),

            // Uncommon — HP wall; footprint matches §2.1's "3 (L)" exactly. "Slows adjacent
            // enemy movement": a live tick-sim proximity check (TickCombatRun.TryMoveSide +
            // GameTagIds.MovementSlowAura), NOT the Suppression tag — §1.8's border rule
            // reserves Suppression for Crimson; this is a narrower, permanent-while-adjacent
            // movement debuff only.
            SavePiece("trench_works", "Trench Works", PieceCategory.Unit, Tromino3,
                GameTagIds.Structure, GameTagIds.Defender, "neutral", 140, 0, 2, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0,
                abilityTags: new[] { GameTagIds.MovementSlowAura },
                flavorTags: new[] { GameTagIds.Fortification },
                rarity: Rarity.Uncommon),

            // Uncommon — "priced painfully": Neutral has no rares to push this into, and price
            // is rarity-derived (GDD §14 principle 8), so the spec's pricing intent can't be
            // represented; flagged here rather than faked with an authored price override.
            // Post-fight "reduces Manpower lost to damaged survivors" wired via
            // ManpowerCalculator.ComputeCasualties(playerCombatants, hqBoard) — field_hospital
            // is detected by id on the HQ board (Building-primary pieces always live there).
            SavePiece("field_hospital", "Field Hospital", PieceCategory.Building, Tromino3,
                GameTagIds.Building, GameTagIds.Support, "neutral", 60, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, synergyTags: new[] { GameTagIds.Medic },
                rarity: Rarity.Uncommon),

            // ---------------------------------------------------------------
            // IRONMARCH UNION — "The Relentless War Machine" (§2.2): 6C / 3U / 3R.
            // ---------------------------------------------------------------

            // Common — the faction body. PROVISIONAL: footprint kept at 1 cell (spec says 2)
            // to preserve the hand-authored anchors in BossRoster.cs / IronmarchEnemyFactory.cs
            // (both packed tightly around the old 1-cell conscript_rifleman) — smallest-diff
            // call for this pass; a later footprint-and-rebalance pass can grow it to 2 and
            // re-tune those anchors. Stats otherwise anchored 1:1 to the old conscript_rifleman.
            SavePiece("conscript_rifles", "Conscript Rifles", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Assault, FactionIds.IronmarchUnion, 50, 5, 1, AttackType.Ballistic, ArmorType.None,
                AttackSpeedTier.Slow, AttackRangeTier.Medium, 2, flavorTags: new[] { GameTagIds.SmallArms },
                rarity: Rarity.Common),

            // Common — anti-structure/anti-heavy common; identity carried entirely by the
            // Explosive attack-type triangle (+30% vs Heavy armor and structures/buildings),
            // no custom ability needed.
            SavePiece("line_grenadiers", "Line Grenadiers", PieceCategory.Unit, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Infantry, GameTagIds.Assault, FactionIds.IronmarchUnion, 45, 8, 1, AttackType.Explosive, ArmorType.None,
                AttackSpeedTier.Slow, AttackRangeTier.Medium, 2,
                rarity: Rarity.Common),

            // Common — artillery count piece. Downgraded from the old Rare ironclad_mortars
            // (no grantedAbility at Common tier — commons are counts, not ability granters).
            SavePiece("field_mortar_team", "Field Mortar Team", PieceCategory.Unit, DemoSandboxShapes.VerticalPair,
                GameTagIds.Infantry, GameTagIds.Artillery, FactionIds.IronmarchUnion, 30, 7, 2, AttackType.Explosive, ArmorType.None,
                AttackSpeedTier.Slow, AttackRangeTier.Long, 1, flavorTags: new[] { GameTagIds.Shells, GameTagIds.Siege },
                rarity: Rarity.Common),

            // Common — sniper count piece. Downgraded from the old Uncommon ironclad_marksman;
            // the stealth identity moved to the Rare marksman_doctrine_officer.
            SavePiece("sharpshooter", "Sharpshooter", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Sniper, FactionIds.IronmarchUnion, 30, 6, 1, AttackType.Piercing, ArmorType.None,
                AttackSpeedTier.Slow, AttackRangeTier.Long, 2,
                rarity: Rarity.Common),

            // Common — defender. PROVISIONAL: footprint kept at 1 cell (spec says 3), same
            // smallest-diff reasoning as conscript_rifles (preserves the old bulwark_squad
            // anchors in BossRoster.cs / IronmarchEnemyFactory.cs).
            // "Takes reduced morale damage" wired via PieceDefinition.MoraleDamageResistancePercent
            // + MoraleRules.ApplyResistance (TickCombatRun.ApplyMoraleDamage). PROVISIONAL magnitude.
            SavePiece("iron_guard", "Iron Guard", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Defender, FactionIds.IronmarchUnion, 70, 4, 2, AttackType.Ballistic, ArmorType.Medium,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 1,
                rarity: Rarity.Common, moraleDamageResistancePercent: 40),

            // Common — +1 Authority/round, `command`. Id/ability/footprint kept identical;
            // combat role reclassified Support→Utility to match §2.2's Role column exactly
            // (no functional change: ShopLaneResolver maps both to the Defensive lane).
            SavePiece("command_outpost", "Command Outpost", PieceCategory.Building, DemoSandboxShapes.HorizontalPair,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.IronmarchUnion, 40, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, synergyTags: new[] { GameTagIds.Command },
                rarity: Rarity.Common),

            // Uncommon — adjacent artillery: +1 attack-speed tier.
            SavePiece("forward_observer", "Forward Observer", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Support, FactionIds.IronmarchUnion, 25, 0, 1, AttackType.None, ArmorType.None,
                AttackSpeedTier.Medium, AttackRangeTier.Short, 2,
                customAbilities: new[]
                {
                    Ability("forward_observer_adjacent_artillery_attack_speed", PieceAbilityTrigger.AdjacentAura, SynergyStat.AttackSpeedSteps, SynergyModType.TierStep, 1,
                        neighborFilter: new NeighborFilter { CombatRoleTagId = GameTagIds.Artillery })
                },
                rarity: Rarity.Uncommon),

            // Uncommon — adjacent assault infantry: +damage. combatRole is Utility (not a
            // literal "command" role — GameTagIds has no such CombatRole value) + synergyTags
            // [Command] for the flavor/CM identity, mirroring the old ironclad_field_marshal
            // pattern (Utility role + Command synergy tag).
            SavePiece("shock_sergeant", "Shock Sergeant", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Utility, FactionIds.IronmarchUnion, 35, 5, 1, AttackType.Ballistic, ArmorType.Light,
                AttackSpeedTier.Medium, AttackRangeTier.Medium, 2, synergyTags: new[] { GameTagIds.Command },
                customAbilities: new[]
                {
                    Ability("shock_sergeant_adjacent_assault_damage", PieceAbilityTrigger.AdjacentAura, SynergyStat.Damage, SynergyModType.Flat, 2,
                        neighborFilter: new NeighborFilter { PrimaryTagId = GameTagIds.Infantry, CombatRoleTagId = GameTagIds.Assault })
                },
                rarity: Rarity.Uncommon),

            // Uncommon — HQ building. Footprint matches §2.2's "3" exactly.
            // *Ranging Barrage* now fires from here: CommandProcessor/CombatAbilityExecutor were
            // extended to also scan the HQ board for GrantedAbility sources (§4 ledger 🟡, now
            // wired — see TickCombatRun._playerHqBoard). Reuses the existing small-area
            // GrantedAbility.MortarShot (unscaled) — Grand Battery's Rolling Barrage is the
            // scaled, bigger sibling on its own enum value.
            SavePiece("artillery_park", "Artillery Park", PieceCategory.Building, Tromino3,
                GameTagIds.Building, GameTagIds.Utility, FactionIds.IronmarchUnion, 90, 0, 0, AttackType.None, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Short, 0, grantedAbility: GrantedAbility.MortarShot,
                rarity: Rarity.Uncommon),

            // Rare — terror ≥2x damage (🟢 existing seam, wired: terrorDamage = 2x baseDamage;
            // 16 = 2x the 8 baseDamage). "Infantry within 2 cells gain morale resistance" wired
            // via an AdjacentAura at radius 2 (PieceAbilityEngine BFS over board-adjacency hops)
            // targeting SynergyStat.MoraleResistancePercent — same resistance stat as Iron Guard.
            SavePiece("breakthrough_tank", "Breakthrough Tank", PieceCategory.Unit, DemoSandboxShapes.Square2x2,
                GameTagIds.Vehicle, GameTagIds.Tank, FactionIds.IronmarchUnion, 90, 8, 4, AttackType.Ballistic, ArmorType.Heavy,
                AttackSpeedTier.Slow, AttackRangeTier.Medium, 1, flavorTags: new[] { GameTagIds.Shells },
                customAbilities: new[]
                {
                    Ability("breakthrough_tank_infantry_morale_resist", PieceAbilityTrigger.AdjacentAura, SynergyStat.MoraleResistancePercent, SynergyModType.Percent, 25,
                        neighborFilter: new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
                        radius: 2)
                },
                rarity: Rarity.Rare, maxMorale: 50, terrorDamage: 16),

            // Rare — combat-board structure (primary: structure → Combat board per §5's
            // machine_gun_nest gotcha). *Rolling Barrage* is its own GrantedAbility (bigger
            // radius than MortarShot) scaling with the army's artillery-tag count, read directly
            // from TickCombatRun._playerArtilleryCount (BuildBoardTagCounter over both boards) —
            // no new Critical-Mass rule needed, per §3's "tactic-scaling may read counts directly".
            SavePiece("grand_battery", "Grand Battery", PieceCategory.Unit, DemoSandboxShapes.Square2x2,
                GameTagIds.Structure, GameTagIds.Artillery, FactionIds.IronmarchUnion, 110, 10, 3, AttackType.Explosive, ArmorType.Light,
                AttackSpeedTier.Slow, AttackRangeTier.Long, 0, grantedAbility: GrantedAbility.RollingBarrage,
                flavorTags: new[] { GameTagIds.Shells, GameTagIds.Siege },
                rarity: Rarity.Rare),

            // Rare — stealth until the 2nd tactics window (🟢 existing seam: renamed
            // CombatStealthRules.MarksmanPieceId to this id). "Snipers +damage per sniper in
            // army" wired via the existing BoardPerTagCount ability (same tech ironmarch_surgeon
            // used for its Medic-count HP%), counting the `sniper` combat-role tag across both
            // boards and applying +1 flat damage per sniper to every sniper-role piece.
            SavePiece("marksman_doctrine_officer", "Marksman-Doctrine Officer", PieceCategory.Unit, DemoSandboxShapes.Single,
                GameTagIds.Infantry, GameTagIds.Sniper, FactionIds.IronmarchUnion, 35, 6, 2, AttackType.Piercing, ArmorType.None,
                AttackSpeedTier.Slow, AttackRangeTier.Long, 3,
                abilityTags: new[] { GameTagIds.Stealth }, synergyTags: new[] { GameTagIds.Command },
                customAbilities: new[]
                {
                    Ability("marksman_doctrine_officer_sniper_count_damage", PieceAbilityTrigger.BoardPerTagCount, SynergyStat.Damage, SynergyModType.Flat, 1,
                        countTagId: GameTagIds.Sniper,
                        neighborFilter: new NeighborFilter { CombatRoleTagId = GameTagIds.Sniper })
                },
                rarity: Rarity.Rare)
        };
    }
}
