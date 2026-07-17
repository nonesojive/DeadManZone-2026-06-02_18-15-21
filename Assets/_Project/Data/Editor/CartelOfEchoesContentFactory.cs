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
    // 2026-07-15 faction-roster-v1-design.md §2.4: Cartel of Echoes ("War as Profit").
    // 6 Common / 3 Uncommon / 3 Rare = 12 pieces. 4 buildings (Cartel's identity cost — fewest
    // native fighters, only 7, backfilled by the mercenary shop slot; see
    // FactionPassives.HasMercenarySlot / OffFactionRules.IsFighter), 2 tactic-flavored pieces,
    // 0 native vehicles.
    //
    // Mirrors IronmarchUnionContentFactory's SavePiece/Ability/SaveFaction shape exactly so the
    // two factories stay interchangeable for the integration pass. Deliberately NOT a full
    // Generate() pass: no [MenuItem], no folder creation, no DeleteExistingPieces, no
    // DemoContentDatabaseWriter call — a separate integration pass calls CreatePieces() /
    // SaveFaction() directly and wires them into the ContentDatabase.
    public static partial class CartelOfEchoesContentFactory
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
            // W1a tentpole fields (PieceDefinitionSO.cs "2026-07-15 faction-roster-v1 new-tech
            // fields" block). None of this faction's pieces use these — every call site below
            // leaves them at default (false/0); wired through only so this factory's SavePiece
            // signature matches the full field set other factories may need.
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
                FactionIds.CartelOfEchoes,
                "Cartel of Echoes",
                startingSupplies: 60,
                startingManpower: 15,
                baseSuppliesPerRound: 8,
                baseMusterPerShop: 1,
                startingAuthority: 2,
                baseSalvageChancePercent: 1);

            // 3 of the 4 existing TacticType values (Combat/TacticType.cs) — no new tactic
            // types authored here. Picked for economy/mercenary flavor: hold the line
            // (StandGround) while the shop backfills mercenaries, push with the assault
            // commons (Advance), and keep the support/building-heavy backline alive
            // (ProtectSupport) given Cartel fields only 7 native fighters.
            faction.startingTactics = new[]
            {
                TacticType.StandGround,
                TacticType.Advance,
                TacticType.ProtectSupport
            };

            // Starting loadout leans into the economy identity: a Supplier building, the
            // shop-muster building, and two cheap native fighters to hold the line until the
            // mercenary slot backfills the roster. Anchors are preferences — the orchestrator
            // scans forward if a cell turns out illegal.
            faction.startingPieces = new[]
            {
                new FactionSO.StartingPieceEntry { pieceId = "freight_depot", anchor = new Vector2Int(0, 0) },
                new FactionSO.StartingPieceEntry { pieceId = "company_store", anchor = new Vector2Int(0, 3) },
                new FactionSO.StartingPieceEntry { pieceId = "company_rifleman", anchor = new Vector2Int(2, 2) },
                new FactionSO.StartingPieceEntry { pieceId = "strikebreaker", anchor = new Vector2Int(3, 2) },
            };
            EditorUtility.SetDirty(faction);
            return faction;
        }
    }
}
