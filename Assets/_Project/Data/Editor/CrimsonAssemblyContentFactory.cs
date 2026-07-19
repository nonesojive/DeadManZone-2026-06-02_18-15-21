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
    /// 2026-07-15 faction-roster-v1 §2.8: Crimson Assembly ("Clinical Optimization").
    /// 6C/3U/3R, 2 buildings, 4 tactics (highest budget), 3 vehicles (highest — 2 Rare tanks
    /// + the one sanctioned Uncommon vehicle exception, Scout Tankette).
    ///
    /// Border rule (§1.8): Crimson is the SOLE owner of enemy-facing debuffs game-wide —
    /// appliesSuppressionOnHit is exclusively theirs. No other faction's content factory may
    /// set that flag true.
    ///
    /// Deliberately NOT wired here (read-only per this pass's scope — no shared-file edits):
    /// - Generate()/[MenuItem]/DemoContentDatabaseWriter — CreatePieces()/SaveFaction() are
    ///   plumbed into the content pass by a later step, not this one.
    /// - Enemy templates (fight_N.assets) — a separate factory's job (mirrors
    ///   IronmarchEnemyFactory, not part of this deliverable).
    /// </summary>
    public static partial class CrimsonAssemblyContentFactory
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
            // 2026-07-15 faction-roster-v1 W1a: the 12 new PieceDefinitionSO fields, exposed
            // here so this factory can set every one explicitly rather than relying on SO
            // defaults. Crimson owns appliesSuppressionOnHit (§1.8); the other 11 belong to
            // later-wave factions (Oathborn/Ashen/Blightborn/Paradox) and stay at their inert
            // defaults for every Crimson piece.
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
            var faction = DemoContentGenerator.SaveFaction(
                FactionIds.CrimsonAssembly,
                "Crimson Assembly",
                // PROVISIONAL — economy pass 2026-07-19: 28 keeps Crimson richer than the
                // 25 baseline (second-wealthiest faction identity); 12/round sustain.
                startingSupplies: 28,
                startingManpower: 15,
                baseSuppliesPerRound: 12,
                baseMusterPerShop: 1,
                startingAuthority: 2,
                baseSalvageChancePercent: 1);

            // 4 tactics — the highest budget in the game (TacticType has exactly 4 values
            // total; every other authored faction's startingTactics is a subset of these).
            faction.startingTactics = new[]
            {
                TacticType.DisciplinedFire,
                TacticType.Advance,
                TacticType.StandGround,
                TacticType.ProtectSupport
            };

            // Starting loadout: economy building + support + two infantry commons, one of
            // which is the Suppression Team count piece (the faction's signature mechanic).
            // Anchors are preferences — the orchestrator scans forward if a cell is illegal.
            faction.startingPieces = new[]
            {
                new FactionSO.StartingPieceEntry { pieceId = "research_annex", anchor = new Vector2Int(0, 0) },
                new FactionSO.StartingPieceEntry { pieceId = "ballistics_analyst", anchor = new Vector2Int(0, 3) },
                new FactionSO.StartingPieceEntry { pieceId = "assembly_trooper", anchor = new Vector2Int(2, 2) },
                new FactionSO.StartingPieceEntry { pieceId = "suppression_team", anchor = new Vector2Int(3, 2) },
            };
            EditorUtility.SetDirty(faction);
            return faction;
        }
    }
}
