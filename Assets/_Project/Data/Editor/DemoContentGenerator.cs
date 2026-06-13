using System.IO;
using DeadManZone.Core.Board;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    /// <summary>Generates full demo content: 5 factions, expanded piece roster, 10-fight enemies.</summary>
    public static class DemoContentGenerator
    {
        private const string Root = "Assets/_Project/Data/Resources/DeadManZone";

        [MenuItem("DeadManZone/Generate Demo Content (5 Factions)")]
        public static void Generate()
        {
            EnsureFolder(Root);
            EnsureFolder($"{Root}/Pieces");
            EnsureFolder($"{Root}/Factions");
            EnsureFolder($"{Root}/Enemies");

            var pieces = DemoPieceFactory.CreateAll();
            var factions = DemoFactionFactory.CreateAll();
            var enemies = DemoEnemyFactory.CreateAll(pieces);
            DemoContentDatabaseWriter.Write(pieces, factions, enemies);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("DeadManZone demo content generated (5 factions, expanded roster).");
        }

        internal static PieceDefinitionSO SavePiece(
            string id,
            string displayName,
            PieceCategory category,
            ShopLane lane,
            Vector2Int[] shape,
            string primary,
            string combatRole,
            string systemTag,
            string[] synergyTags,
            string factionId = "neutral",
            int maxHp = 10,
            int baseDamage = 0,
            int cooldownTicks = 3,
            int goldCost = 5,
            int requisitionCost = 0,
            int manpowerCost = 1,
            int musterPerShop = 0,
            ShopModifierFlags shopModifiers = ShopModifierFlags.None,
            CommandActionFlags commandActions = CommandActionFlags.None,
            GrantedAbility grantedAbility = GrantedAbility.None)
        {
            var path = $"{Root}/Pieces/{id}.asset";
            var asset = LoadOrCreate<PieceDefinitionSO>(path);
            asset.id = id;
            asset.displayName = displayName;
            asset.category = category;
            asset.shopLane = lane;
            asset.shapeCells = shape;
            asset.primary = primary;
            asset.combatRole = combatRole;
            asset.systemTag = systemTag;
            asset.synergyTags = synergyTags ?? System.Array.Empty<string>();
            asset.tags = PieceTagQueries.BuildLegacyTags(
                category,
                baseDamage,
                primary,
                combatRole,
                systemTag,
                asset.synergyTags,
                asset.abilityTags ?? System.Array.Empty<string>(),
                asset.flavorTags ?? System.Array.Empty<string>());
            asset.factionId = factionId;
            asset.maxHp = maxHp;
            asset.baseDamage = baseDamage;
            asset.cooldownTicks = cooldownTicks;
            asset.goldCost = goldCost;
            asset.requisitionCost = requisitionCost;
            asset.manpowerCost = manpowerCost;
            asset.musterPerShop = musterPerShop;
            asset.shopModifiers = shopModifiers;
            asset.commandActions = commandActions;
            asset.grantedAbility = grantedAbility;
            asset.categoryTint = category switch
            {
                PieceCategory.Unit => FactionTint(factionId, 0.35f, 0.42f, 0.55f),
                PieceCategory.Building => FactionTint(factionId, 0.48f, 0.4f, 0.28f),
                PieceCategory.Hybrid => FactionTint(factionId, 0.38f, 0.32f, 0.48f),
                _ => Color.gray
            };
            EditorUtility.SetDirty(asset);
            return asset;
        }

        internal static FactionSO SaveFaction(
            string id,
            string displayName,
            string hqPieceId,
            int startingSupplies = 100,
            int startingManpower = 100,
            int baseMusterPerShop = 12,
            int startingAuthority = 2,
            int startingMorale = 100,
            int baseSalvageChancePercent = 10)
        {
            var path = $"{Root}/Factions/{id}.asset";
            var asset = LoadOrCreate<FactionSO>(path);
            asset.factionId = id;
            asset.displayName = displayName;
            asset.hqPieceId = hqPieceId;
            asset.startingSupplies = startingSupplies;
            asset.startingManpower = startingManpower;
            asset.baseMusterPerShop = baseMusterPerShop;
            asset.startingAuthority = startingAuthority;
            asset.startingMorale = startingMorale;
            asset.baseSalvageChancePercent = baseSalvageChancePercent;
            asset.tokenBackgroundColor = DefaultFactionTokenBackground(id);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static Color DefaultFactionTokenBackground(string factionId)
        {
            return factionId switch
            {
                "iron_vanguard" => new Color(0.22f, 0.28f, 0.38f, 0.45f),
                "dust_scourge" => new Color(0.42f, 0.34f, 0.24f, 0.45f),
                "cartel_of_echoes" => new Color(0.32f, 0.26f, 0.42f, 0.45f),
                "crimson_legion" => new Color(0.45f, 0.20f, 0.18f, 0.45f),
                "ash_wraiths" => new Color(0.28f, 0.28f, 0.30f, 0.45f),
                "neutral" => new Color(0.32f, 0.33f, 0.36f, 0.42f),
                _ => new Color(0f, 0f, 0f, 0f)
            };
        }

        internal static EnemyTemplateSO SaveEnemy(
            int fightNumber,
            string displayName,
            string previewTag,
            string enemyFactionId,
            EnemyPiecePlacement[] placements)
        {
            var path = $"{Root}/Enemies/fight_{fightNumber}.asset";
            var asset = LoadOrCreate<EnemyTemplateSO>(path);
            asset.fightNumber = fightNumber;
            asset.displayName = displayName;
            asset.previewTag = previewTag;
            asset.enemyFactionId = enemyFactionId;
            asset.placements = placements;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        internal static EnemyPiecePlacement Placement(PieceDefinitionSO piece, int x, int y) =>
            new EnemyPiecePlacement { piece = piece, anchor = new Vector2Int(x, y) };

        private static Color FactionTint(string factionId, float r, float g, float b)
        {
            return factionId switch
            {
                "dust_scourge" => new Color(r + 0.1f, g + 0.05f, b - 0.1f, 1f),
                "cartel_of_echoes" => new Color(r - 0.05f, g, b + 0.15f, 1f),
                "crimson_legion" => new Color(r + 0.15f, g - 0.1f, b - 0.1f, 1f),
                "ash_wraiths" => new Color(r - 0.05f, g - 0.05f, b - 0.05f, 1f),
                _ => new Color(r, g, b, 1f)
            };
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
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
