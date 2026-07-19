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
    // 2026-07-16 faction-roster-v1 Wave 2 (§2.3): Dust Scourge — "Scavengers of the Wastes".
    // 6C / 3U / 3R. Mirrors IronmarchUnionContentFactory's own SavePiece/Ability/SaveFaction
    // pattern verbatim (see that file for the house convention this copies). The integration
    // lead owns folder setup / deletion-once / DemoContentDatabaseWriter wiring elsewhere —
    // this factory only ever produces PieceDefinitionSO/FactionSO instances via CreatePieces()
    // and SaveFaction().
    public static partial class DustScourgeContentFactory
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
            int salvageChanceBonus = 0,
            // 2026-07-15 faction-roster-v1 W1a tentpole fields — all default false/0, safe to
            // leave at default for Dust Scourge (none of its pieces need them this wave).
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
                salvageChanceBonus: salvageChanceBonus,
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
            piece.tags = PieceTagQueries.BuildLegacyTags(
                piece.category,
                piece.baseDamage,
                piece.primary,
                piece.combatRole,
                piece.systemTag,
                piece.synergyTags,
                piece.abilityTags,
                piece.flavorTags);
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

        internal static FactionSO SaveFaction()
        {
            // PROVISIONAL: anchored near IronMarch's own SaveIronmarchFaction() call.
            // Dust Scourge leans into a scrappier, salvage-driven economy — slightly lower
            // starting Supplies/Manpower than IronMarch's baseline, same starting Authority.
            var faction = DemoContentGenerator.SaveFaction(
                FactionIds.DustScourge,
                "Dust Scourge",
                // PROVISIONAL — economy pass 2026-07-19: 22/11 keeps Dust a notch below
                // the 25/12 baseline (salvage-driven economy makes up the difference).
                startingSupplies: 22,
                startingManpower: 14,
                baseSuppliesPerRound: 11,
                baseMusterPerShop: 1,
                startingAuthority: 2,
                baseSalvageChancePercent: 3);

            faction.startingTactics = new[]
            {
                TacticType.Advance,
                TacticType.ProtectSupport
            };

            // Starting loadout: opening hand nudges toward the salvage/gas identity —
            // a Cache for economy, a Raider for a body, a Gasflinger for the gas count.
            // Anchors are preferences — the orchestrator scans forward if a cell is illegal.
            faction.startingPieces = new[]
            {
                new FactionSO.StartingPieceEntry { pieceId = "scavengers_cache", anchor = new Vector2Int(0, 0) },
                new FactionSO.StartingPieceEntry { pieceId = "waste_raider", anchor = new Vector2Int(0, 3) },
                new FactionSO.StartingPieceEntry { pieceId = "gasflinger", anchor = new Vector2Int(2, 2) },
                new FactionSO.StartingPieceEntry { pieceId = "outrider", anchor = new Vector2Int(3, 2) },
            };
            EditorUtility.SetDirty(faction);
            return faction;
        }
    }
}
