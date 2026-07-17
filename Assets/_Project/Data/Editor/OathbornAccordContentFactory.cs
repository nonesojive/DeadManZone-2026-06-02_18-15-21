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
    // 2026-07-15 faction-roster-v1 §2.5: Oathborn Accord ("Peacekeepers Turned Crusaders").
    // 6C/3U/3R, 2 buildings (Oathhall, Sanctum Command), 2 starting tactics, 1 vehicle
    // (Armored Ark — the faction's sole new-system tentpole: transport). Mirrors
    // IronmarchUnionContentFactory's SavePiece/Ability/SaveFaction helper shape exactly.
    // No [MenuItem] here per this wave's instructions — wiring into a menu command and the
    // ContentDatabase is a later pass's job.
    public static partial class OathbornAccordContentFactory
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
            // Oathborn tentpole fields (§1.8/§2.5/§4) — this faction is the sole author of
            // non-zero values for these; DemoContentGenerator.SavePiece doesn't know about them
            // yet, so they're wired onto the returned asset here instead.
            bool isTransport = false,
            int transportCapacity = 0,
            int healPulseAmount = 0,
            int healPulseRadius = 0,
            int healPulseIntervalTicks = 0)
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
            piece.isTransport = isTransport;
            piece.transportCapacity = transportCapacity;
            piece.healPulseAmount = healPulseAmount;
            piece.healPulseRadius = healPulseRadius;
            piece.healPulseIntervalTicks = healPulseIntervalTicks;
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
                FactionIds.OathbornAccord,
                "Oathborn Accord",
                startingSupplies: 50,
                startingManpower: 15,
                baseSuppliesPerRound: 10,
                baseMusterPerShop: 1,
                startingAuthority: 2,
                baseSalvageChancePercent: 1);

            // 2 starting tactics per §2.5's composition ("2 buildings, 2 tactics, 1 vehicle") —
            // StandGround (crossing the field without breaking) + ProtectSupport (medics survive
            // arriving), matching the design spine in the class header.
            faction.startingTactics = new[]
            {
                TacticType.StandGround,
                TacticType.ProtectSupport
            };

            // Starting loadout: economy/command building, a medic, and a line body — mirrors
            // IronMarch's anchor spread. Anchors are preferences; the orchestrator scans forward
            // if a cell turns out illegal.
            faction.startingPieces = new[]
            {
                new FactionSO.StartingPieceEntry { pieceId = "oathhall", anchor = new Vector2Int(0, 0) },
                new FactionSO.StartingPieceEntry { pieceId = "mercy_sister", anchor = new Vector2Int(2, 2) },
                new FactionSO.StartingPieceEntry { pieceId = "truncheon_line", anchor = new Vector2Int(3, 2) },
                new FactionSO.StartingPieceEntry { pieceId = "vow_warden", anchor = new Vector2Int(3, 3) },
            };
            EditorUtility.SetDirty(faction);
            return faction;
        }
    }
}
