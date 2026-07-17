using System;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    /// <summary>
    /// 2026-07-15 faction-roster-v1-design.md §2.9: Ashen Covenant ("The Revolution of Cinders"),
    /// Wave 2 of the Faction Roster v1 rollout. 6C / 3U / 3R, 2 buildings, 2 tactics, 0 vehicles.
    /// Mirrors IronmarchUnionContentFactory's SavePiece/Ability/SaveFaction pattern; deliberately
    /// has no [MenuItem] of its own — wiring this into a generator pass and the ContentDatabase is
    /// a separate task from authoring the roster.
    /// </summary>
    public static partial class AshenCovenantContentFactory
    {
        private static PieceDefinitionSO SavePiece(
            string id,
            string displayName,
            PieceCategory category,
            Vector2Int[] shape,
            string primary,
            string combatRole,
            string factionId,
            int maxHp,
            int baseDamage,
            int manpowerCost,
            AttackType attackType,
            ArmorType armorType,
            AttackSpeedTier attackSpeed,
            AttackRangeTier attackRange,
            int movementSpeed,
            int musterPerShop = 0,
            int requisitionCost = 0,
            GrantedAbility grantedAbility = GrantedAbility.None,
            string[] synergyTags = null,
            string[] abilityTags = null,
            string[] flavorTags = null,
            PieceAbilityInlineEntry[] customAbilities = null,
            Rarity rarity = Rarity.Common,
            int? maxMorale = null,
            int terrorDamage = 0,
            int moraleDamageResistancePercent = 0,
            // 2026-07-15 faction-roster-v1 W1a tentpole fields (PieceDefinitionSO). Ashen is the
            // primary user of lowStateDamageBonus/lowStateAttackSpeedSteps (§2.9); the other nine
            // are exposed here only so this factory has the same surface as a "full" piece author
            // would need — every value defaults to off/zero and is left unset except where the
            // roster explicitly calls for it.
            bool appliesSuppressionOnHit = false,
            bool isTransport = false,
            int transportCapacity = 0,
            int lowStateDamageBonus = 0,
            int lowStateAttackSpeedSteps = 0,
            int healPulseAmount = 0,
            int healPulseRadius = 0,
            int healPulseIntervalTicks = 0,
            bool gasDealsMoraleDamage = false,
            bool hijacksAmbientGas = false,
            bool addsPauseWindow = false,
            bool repeatsPauseAbilities = false)
        {
            var piece = DemoContentGenerator.SavePiece(
                id,
                displayName,
                category,
                ShopLaneResolver.ResolveLane(combatRole),
                shape,
                primary: primary,
                combatRole: combatRole,
                systemTag: string.Empty,
                synergyTags: synergyTags ?? Array.Empty<string>(),
                factionId: factionId,
                maxHp: maxHp,
                baseDamage: baseDamage,
                cooldownTicks: 3,
                manpowerCost: manpowerCost,
                requisitionCost: requisitionCost,
                musterPerShop: musterPerShop,
                grantedAbility: grantedAbility,
                attackType: attackType,
                armorType: armorType,
                attackSpeed: attackSpeed,
                attackRange: attackRange,
                movementSpeed: movementSpeed,
                abilityTags: abilityTags ?? Array.Empty<string>(),
                rarity: rarity,
                maxMorale: maxMorale,
                terrorDamage: terrorDamage,
                moraleDamageResistancePercent: moraleDamageResistancePercent);

            piece.flavorTags = flavorTags ?? Array.Empty<string>();
            piece.catalogAbilities = Array.Empty<AbilityDefinitionSO>();
            piece.customAbilities = customAbilities ?? Array.Empty<PieceAbilityInlineEntry>();
            piece.accuracyOverride = 0;
            piece.tags = PieceTagQueries.BuildLegacyTags(
                piece.category,
                piece.baseDamage,
                piece.primary,
                piece.combatRole,
                piece.systemTag,
                piece.synergyTags,
                piece.abilityTags,
                piece.flavorTags);

            piece.appliesSuppressionOnHit = appliesSuppressionOnHit;
            piece.isTransport = isTransport;
            piece.transportCapacity = transportCapacity;
            piece.lowStateDamageBonus = lowStateDamageBonus;
            piece.lowStateAttackSpeedSteps = lowStateAttackSpeedSteps;
            piece.healPulseAmount = healPulseAmount;
            piece.healPulseRadius = healPulseRadius;
            piece.healPulseIntervalTicks = healPulseIntervalTicks;
            piece.gasDealsMoraleDamage = gasDealsMoraleDamage;
            piece.hijacksAmbientGas = hijacksAmbientGas;
            piece.addsPauseWindow = addsPauseWindow;
            piece.repeatsPauseAbilities = repeatsPauseAbilities;

            EditorUtility.SetDirty(piece);
            return piece;
        }

        private static PieceAbilityInlineEntry Ability(
            string id,
            PieceAbilityTrigger trigger,
            SynergyStat stat,
            SynergyModType modType,
            int magnitude,
            string countTagId = null,
            NeighborFilter neighborFilter = default,
            bool applyToSelf = false,
            int radius = 1) => new()
            {
                id = id,
                trigger = trigger,
                stat = stat,
                modType = modType,
                magnitude = magnitude,
                countTagId = countTagId,
                neighborFilter = neighborFilter,
                applyToSelf = applyToSelf,
                radius = radius
            };

        /// <summary>
        /// §2.9: "the faction baseline gets high Muster" — chosen value 4, vs IronMarch's
        /// baseMusterPerShop: 1. Ashen's combat pieces all cost 1 Manpower (the hard rule for this
        /// roster), so a high Muster lets players re-muster fanatics quickly without the run's
        /// Manpower economy and its Muster economy fighting each other.
        /// startingTactics/startingPieces added afterward (§2.9 "2 buildings, 2 tactics") so
        /// Ashen Covenant is actually startable via RunOrchestrator.StartNewRun — every other
        /// Wave 2 faction's SaveFaction() sets these; leaving Ashen's empty would silently
        /// place it on an empty opening board, unlike its 7 siblings.
        /// </summary>
        internal static FactionSO SaveFaction()
        {
            var faction = DemoContentGenerator.SaveFaction(
                FactionIds.AshenCovenant,
                "Ashen Covenant",
                startingSupplies: 50,
                startingManpower: 15,
                baseSuppliesPerRound: 10,
                baseMusterPerShop: 4,
                startingAuthority: 2,
                baseSalvageChancePercent: 1);

            // StandGround (the martyrdom faction holds ground while cheap bodies feed the low-
            // state payoffs) + ProtectSupport (keeps Hymnal Leader/Reliquary Bearer alive to
            // matter) — same 2-tactic pattern as Dust Scourge/Blightborn's own SaveFaction().
            faction.startingTactics = new[]
            {
                TacticType.StandGround,
                TacticType.ProtectSupport
            };

            // Starting loadout: the Muster building, a swarm body, and the fanatic-synergy
            // support piece — nudges the opening hand toward the low-state/fanatic identity.
            // Anchors are preferences; the orchestrator scans forward if a cell is illegal.
            faction.startingPieces = new[]
            {
                new FactionSO.StartingPieceEntry { pieceId = "shrine_of_ash", anchor = new Vector2Int(0, 0) },
                new FactionSO.StartingPieceEntry { pieceId = "zealot_mob", anchor = new Vector2Int(0, 3) },
                new FactionSO.StartingPieceEntry { pieceId = "ash_acolyte", anchor = new Vector2Int(3, 2) },
                new FactionSO.StartingPieceEntry { pieceId = "hymnal_leader", anchor = new Vector2Int(3, 3) },
            };
            EditorUtility.SetDirty(faction);
            return faction;
        }
    }
}
