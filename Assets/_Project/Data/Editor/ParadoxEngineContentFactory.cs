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
    // 2026-07-16 faction-roster-v1-design.md §2.6: Paradox Engine — "The Experiment That
    // Won't End". Mirrors IronmarchUnionContentFactory's SavePiece/Ability helper shape
    // (see IronmarchUnionContentFactory.cs) so the two rosters stay diffable against each
    // other. This wave only authors piece/faction data — no [MenuItem], no folder/asset
    // deletion, no DemoContentDatabaseWriter call; those are owned by the integration pass
    // that stitches every faction's content factory together.
    public static partial class ParadoxEngineContentFactory
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
            // Paradox is the sole author of these two W1a fields (PieceDefinitionSO.cs):
            // The Second Hand is the ONLY piece in the game with addsPauseWindow = true;
            // Doctor Recursion is the ONLY piece with repeatsPauseAbilities = true.
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
                FactionIds.ParadoxEngine,
                "Paradox Engine",
                startingSupplies: 50,
                startingManpower: 15,
                baseSuppliesPerRound: 10,
                baseMusterPerShop: 1,
                startingAuthority: 2,
                baseSalvageChancePercent: 1);

            // Paradox's tactics budget is 3 of the 4 TacticType values (highest besides
            // Crimson's 4, per spec) — Advance + DisciplinedFire read as tempo-manipulation
            // (movement/fire cadence); StandGround is the one omitted as the odd one out for
            // a faction whose whole identity is "never stand still".
            faction.startingTactics = new[]
            {
                TacticType.Advance,
                TacticType.DisciplinedFire,
                TacticType.ProtectSupport
            };

            // Starting loadout: economy + tempo anchors, matching IronMarch's
            // "opening hand nudges toward identity" convention.
            faction.startingPieces = new[]
            {
                new FactionSO.StartingPieceEntry { pieceId = "chrono_lab", anchor = new Vector2Int(0, 0) },
                new FactionSO.StartingPieceEntry { pieceId = "chronometry_station", anchor = new Vector2Int(0, 3) },
                new FactionSO.StartingPieceEntry { pieceId = "field_dynamo", anchor = new Vector2Int(2, 2) },
                new FactionSO.StartingPieceEntry { pieceId = "chrono_fusilier", anchor = new Vector2Int(3, 2) },
            };
            EditorUtility.SetDirty(faction);
            return faction;
        }
    }
}
