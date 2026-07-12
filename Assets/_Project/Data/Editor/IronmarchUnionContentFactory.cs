using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        /// <summary>
        /// Rewrites ONLY the fight_N.asset enemy templates from the current
        /// IronmarchEnemyFactory compositions, stamping over the existing template
        /// assets in place. Loads the EXISTING piece assets by id — no
        /// DeleteExistingPieces, no piece regeneration — so post-gen icon/model
        /// references on pieces survive (the stamp-don't-regen rule; full Generate()
        /// wipes them). The ContentDatabase keeps its already-registered pieces and
        /// factions verbatim; only the enemyTemplates array is refreshed.
        /// </summary>
        [MenuItem(DeadManZoneEditorMenus.Content + "Regenerate Enemy Templates Only")]
        public static void RegenerateEnemyTemplatesOnly()
        {
            var pieces = new PieceDefinitionSO[PieceIds.Length];
            for (int i = 0; i < PieceIds.Length; i++)
            {
                pieces[i] = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>($"{PiecesRoot}/{PieceIds[i]}.asset");
                if (pieces[i] == null)
                    throw new InvalidOperationException(
                        $"Missing piece asset '{PieceIds[i]}' under {PiecesRoot}. " +
                        "Run 'Generate IronMarch Union Content Pass' once before regenerating templates.");
            }

            var database = AssetDatabase.LoadAssetAtPath<ContentDatabase>($"{Root}/ContentDatabase.asset");
            if (database == null)
                throw new InvalidOperationException(
                    $"No ContentDatabase at {Root}. Run 'Generate IronMarch Union Content Pass' first.");

            var enemies = IronmarchEnemyFactory.CreateAll(pieces);
            DemoContentDatabaseWriter.Write(
                database.Pieces.ToArray(),
                database.Factions.ToArray(),
                enemies);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"IronMarch enemy templates regenerated in place ({enemies.Length} fights; pieces and factions untouched).");
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
            GrantedAbility grantedAbility = GrantedAbility.None,
            string[] synergyTags = null,
            string[] abilityTags = null,
            string[] flavorTags = null,
            PieceAbilityInlineEntry[] customAbilities = null,
            Rarity rarity = Rarity.Common,
            int? maxMorale = null,
            int terrorDamage = 0)
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
                grantedAbility: grantedAbility,
                attackType: attackType,
                armorType: armorType,
                attackSpeed: attackSpeed,
                attackRange: attackRange,
                movementSpeed: movementSpeed,
                abilityTags: abilityTags ?? Array.Empty<string>(),
                rarity: rarity,
                maxMorale: maxMorale,
                terrorDamage: terrorDamage);

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
                baseSalvageChancePercent: 1);

            faction.startingTactics = new[]
            {
                TacticType.StandGround,
                TacticType.Advance,
                TacticType.DisciplinedFire
            };

            // Starting loadout: the faction's opening hand, nudging players toward its
            // identity (economy + supported infantry line). Anchors are preferences —
            // the orchestrator scans forward if a cell turns out illegal.
            faction.startingPieces = new[]
            {
                new FactionSO.StartingPieceEntry { pieceId = "supply_depot", anchor = new Vector2Int(0, 0) },
                new FactionSO.StartingPieceEntry { pieceId = "command_outpost", anchor = new Vector2Int(0, 3) },
                new FactionSO.StartingPieceEntry { pieceId = "field_medic", anchor = new Vector2Int(2, 2) },
                new FactionSO.StartingPieceEntry { pieceId = "conscript_rifleman", anchor = new Vector2Int(3, 2) },
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
