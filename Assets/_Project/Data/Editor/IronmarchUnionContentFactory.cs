using System;
using System.Collections.Generic;
using System.IO;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static partial class IronmarchUnionContentFactory
    {
        private const string Root = "Assets/_Project/Data/Resources/DeadManZone";
        private const string PiecesRoot = Root + "/Pieces";
        private const string FactionsRoot = Root + "/Factions";
        private const string EnemiesRoot = Root + "/Enemies";

        private static readonly string[] PieceIds =
        {
            "supply_depot",
            "field_hospital",
            "officer_quarters",
            "command_outpost",
            "surgical_center",
            "recruitment_office",
            "field_medic",
            "conscript_rifleman",
            "armored_transport",
            "ironmarch_surgeon",
            "bulwark_squad",
            "enlisted_rifleman",
            "ironmarch_iron_horse",
            "ironclad_mortars",
            "ironclad_marksman",
            "ironclad_field_marshal",
            "machine_gun_nest"
        };

        [MenuItem(DeadManZoneEditorMenus.Content + "Generate IronMarch Union Content Pass")]
        public static void Generate()
        {
            EnsureFolder(Root);
            EnsureFolder(PiecesRoot);
            EnsureFolder(FactionsRoot);
            EnsureFolder(EnemiesRoot);

            DeleteExistingPieces();

            var pieces = CreatePieces();
            ValidatePieceRoster(pieces);
            var faction = SaveIronmarchFaction();
            var enemies = IronmarchEnemyFactory.CreateAll(pieces);

            DemoContentDatabaseWriter.Write(pieces, new[] { faction }, enemies);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"IronMarch Union content pass generated ({pieces.Length} pieces, 1 faction, {enemies.Length} fights).");
        }

        private static void DeleteExistingPieces()
        {
            string[] existing = AssetDatabase.FindAssets("t:PieceDefinitionSO", new[] { PiecesRoot });
            for (int i = 0; i < existing.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(existing[i]);
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static void ValidatePieceRoster(PieceDefinitionSO[] pieces)
        {
            if (pieces == null || pieces.Length != PieceIds.Length)
                throw new InvalidOperationException($"Expected {PieceIds.Length} pieces but generated {pieces?.Length ?? 0}.");

            var expected = new HashSet<string>(PieceIds, StringComparer.Ordinal);
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] == null || string.IsNullOrWhiteSpace(pieces[i].id))
                    throw new InvalidOperationException($"Generated piece at index {i} is missing an id.");

                if (!expected.Remove(pieces[i].id))
                    throw new InvalidOperationException($"Unexpected or duplicate piece id generated: {pieces[i].id}");
            }

            if (expected.Count > 0)
                throw new InvalidOperationException($"Missing piece ids: {string.Join(", ", expected)}");
        }

        private static PieceDefinitionSO SavePiece(
            string id,
            string displayName,
            PieceCategory category,
            Vector2Int[] shape,
            string primary,
            string combatRole,
            string factionId,
            int goldCost,
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
            string[] synergyTags = null,
            string[] abilityTags = null,
            string[] flavorTags = null,
            PieceAbilityInlineEntry[] customAbilities = null)
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
                goldCost: goldCost,
                manpowerCost: manpowerCost,
                requisitionCost: requisitionCost,
                musterPerShop: musterPerShop,
                attackType: attackType,
                armorType: armorType,
                attackSpeed: attackSpeed,
                attackRange: attackRange,
                movementSpeed: movementSpeed,
                abilityTags: abilityTags ?? Array.Empty<string>());

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
            bool applyToSelf = false) => new()
            {
                id = id,
                trigger = trigger,
                stat = stat,
                modType = modType,
                magnitude = magnitude,
                countTagId = countTagId,
                neighborFilter = neighborFilter,
                applyToSelf = applyToSelf
            };

        private static FactionSO SaveIronmarchFaction()
        {
            var faction = DemoContentGenerator.SaveFaction(
                FactionIds.IronmarchUnion,
                "IronMarch Union",
                startingSupplies: 50,
                startingManpower: 15,
                baseSuppliesPerRound: 10,
                baseMusterPerShop: 1,
                startingAuthority: 2,
                startingMorale: 30,
                baseSalvageChancePercent: 1);

            faction.startingTactics = new[]
            {
                TacticType.StandGround,
                TacticType.Advance,
                TacticType.DisciplinedFire
            };
            EditorUtility.SetDirty(faction);
            return faction;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
