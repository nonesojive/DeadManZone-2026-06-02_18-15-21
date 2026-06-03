using System.IO;
using DeadManZone.Core.Board;
using DeadManZone.Core.Shop;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class VerticalSliceContentGenerator
    {
        private const string Root = "Assets/_Project/Data/Resources/DeadManZone";

        [MenuItem("DeadManZone/Generate Vertical Slice Content")]
        public static void Generate()
        {
            EnsureFolder(Root);
            EnsureFolder($"{Root}/Pieces");
            EnsureFolder($"{Root}/Factions");
            EnsureFolder($"{Root}/Enemies");

            var pieces = CreatePieces();
            var faction = CreateFaction();
            var enemies = CreateEnemies(pieces);
            CreateDatabase(pieces, faction, enemies);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("DeadManZone vertical slice content generated.");
        }

        private static PieceDefinitionSO[] CreatePieces()
        {
            return new[]
            {
                SavePiece("hq_command", "Command HQ", PieceCategory.Building, ShopLane.Defensive,
                    new[] { Vector2Int.zero, Vector2Int.right }, new[] { "HQ" },
                    maxHp: 25, goldCost: 0, manpowerCost: 0),
                SavePiece("command_bunker", "Command Bunker", PieceCategory.Building, ShopLane.Defensive,
                    new[] { Vector2Int.zero, Vector2Int.right }, new[] { "Command" },
                    maxHp: 20, goldCost: 8, shopModifiers: ShopModifierFlags.ExtraGeneralSlot,
                    commandActions: CommandActionFlags.ChangeStance),
                SavePiece("supply_depot", "Supply Depot", PieceCategory.Building, ShopLane.Defensive,
                    new[] { Vector2Int.zero }, new[] { "Supply" },
                    maxHp: 15, goldCost: 6, shopModifiers: ShopModifierFlags.GoldDiscount10,
                    commandActions: CommandActionFlags.SpendRequisitionBuff),
                SavePiece("field_gun_nest", "Field Gun Nest", PieceCategory.Building, ShopLane.Defensive,
                    new[] { Vector2Int.zero, new Vector2Int(0, 1) }, new[] { "Artillery", "Combatant" },
                    maxHp: 18, baseDamage: 3, cooldownTicks: 4, goldCost: 9),
                SavePiece("radio_array", "Radio Array", PieceCategory.Building, ShopLane.Defensive,
                    new[] { Vector2Int.zero }, new[] { "Command" },
                    maxHp: 12, goldCost: 7, shopModifiers: ShopModifierFlags.EnemyTagPreview),
                SavePiece("field_workshop", "Field Workshop", PieceCategory.Building, ShopLane.Defensive,
                    new[] { Vector2Int.zero }, new[] { "Mechanical" },
                    maxHp: 12, goldCost: 7, shopModifiers: ShopModifierFlags.GuaranteeEngineerOffer),
                SavePiece("rifle_squad", "Rifle Squad", PieceCategory.Unit, ShopLane.Offensive,
                    new[] { Vector2Int.zero }, new[] { "Infantry", "Vanguard", "Combatant" },
                    maxHp: 10, baseDamage: 2, cooldownTicks: 3, goldCost: 5),
                SavePiece("mg_team", "MG Team", PieceCategory.Unit, ShopLane.Offensive,
                    new[] { Vector2Int.zero, Vector2Int.right }, new[] { "Infantry", "Combatant" },
                    maxHp: 14, baseDamage: 3, cooldownTicks: 4, goldCost: 8),
                SavePiece("trench_raider", "Trench Raider", PieceCategory.Unit, ShopLane.Offensive,
                    new[] { Vector2Int.zero }, new[] { "Infantry", "Combatant" },
                    maxHp: 8, baseDamage: 3, cooldownTicks: 2, goldCost: 6),
                SavePiece("diesel_walker", "Diesel Walker", PieceCategory.Unit, ShopLane.Offensive,
                    new[] { Vector2Int.zero, Vector2Int.right, new Vector2Int(0, 1), new Vector2Int(1, 1) },
                    new[] { "Mechanical", "Vanguard", "Combatant" },
                    maxHp: 25, baseDamage: 4, cooldownTicks: 5, goldCost: 12),
                SavePiece("mortar_crew", "Mortar Crew", PieceCategory.Unit, ShopLane.Offensive,
                    new[] { Vector2Int.zero, Vector2Int.right }, new[] { "Artillery", "Combatant" },
                    maxHp: 12, baseDamage: 4, cooldownTicks: 5, goldCost: 9),
                SavePiece("mobile_artillery", "Mobile Artillery", PieceCategory.Hybrid, ShopLane.Specialty,
                    new[] { Vector2Int.zero, Vector2Int.right }, new[] { "Artillery", "Mechanical" },
                    maxHp: 16, baseDamage: 5, cooldownTicks: 5, goldCost: 10, requisitionCost: 2),
                SavePiece("gas_drone", "Gas Drone", PieceCategory.Hybrid, ShopLane.Specialty,
                    new[] { Vector2Int.zero }, new[] { "Gas" },
                    maxHp: 8, baseDamage: 4, cooldownTicks: 4, goldCost: 8, requisitionCost: 3,
                    commandActions: CommandActionFlags.CallStrike),
                SavePiece("armored_sapper", "Armored Sapper", PieceCategory.Hybrid, ShopLane.Specialty,
                    new[] { Vector2Int.zero, new Vector2Int(0, 1) }, new[] { "Mechanical", "Infantry" },
                    maxHp: 20, baseDamage: 3, cooldownTicks: 4, goldCost: 11, requisitionCost: 2),
                SavePiece("weak_conscript", "Weak Conscript", PieceCategory.Unit, ShopLane.Offensive,
                    new[] { Vector2Int.zero }, new[] { "Infantry" },
                    maxHp: 3, baseDamage: 1, cooldownTicks: 4, goldCost: 2)
            };
        }

        private static PieceDefinitionSO SavePiece(
            string id,
            string displayName,
            PieceCategory category,
            ShopLane lane,
            Vector2Int[] shape,
            string[] tags,
            int maxHp = 10,
            int baseDamage = 0,
            int cooldownTicks = 3,
            int goldCost = 5,
            int requisitionCost = 0,
            int manpowerCost = 1,
            ShopModifierFlags shopModifiers = ShopModifierFlags.None,
            CommandActionFlags commandActions = CommandActionFlags.None)
        {
            var path = $"{Root}/Pieces/{id}.asset";
            var asset = LoadOrCreate<PieceDefinitionSO>(path);
            asset.id = id;
            asset.displayName = displayName;
            asset.category = category;
            asset.shopLane = lane;
            asset.shapeCells = shape;
            asset.tags = tags;
            asset.maxHp = maxHp;
            asset.baseDamage = baseDamage;
            asset.cooldownTicks = cooldownTicks;
            asset.goldCost = goldCost;
            asset.requisitionCost = requisitionCost;
            asset.manpowerCost = manpowerCost;
            asset.shopModifiers = shopModifiers;
            asset.commandActions = commandActions;
            asset.categoryTint = category switch
            {
                PieceCategory.Unit => new Color(0.35f, 0.42f, 0.55f, 1f),
                PieceCategory.Building => new Color(0.48f, 0.4f, 0.28f, 1f),
                PieceCategory.Hybrid => new Color(0.38f, 0.32f, 0.48f, 1f),
                _ => Color.gray
            };
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static FactionSO CreateFaction()
        {
            var path = $"{Root}/Factions/iron_vanguard.asset";
            var asset = LoadOrCreate<FactionSO>(path);
            asset.factionId = "iron_vanguard";
            asset.displayName = "Iron Vanguard";
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static EnemyTemplateSO[] CreateEnemies(PieceDefinitionSO[] pieces)
        {
            var byId = pieces.ToDictionary(p => p.id);

            return new[]
            {
                SaveEnemy(1, "Rifle Line", "Infantry",
                    new[] { Placement(byId["rifle_squad"], 6, 4), Placement(byId["rifle_squad"], 8, 4) }),
                SaveEnemy(2, "MG Nest", "Heavy Fire",
                    new[] { Placement(byId["mg_team"], 6, 4), Placement(byId["rifle_squad"], 4, 4) }),
                SaveEnemy(3, "Artillery Barrage", "Artillery",
                    new[] { Placement(byId["field_gun_nest"], 0, 0), Placement(byId["mortar_crew"], 6, 3) }),
                SaveEnemy(4, "Gas And Armor", "Gas",
                    new[] { Placement(byId["gas_drone"], 7, 4), Placement(byId["diesel_walker"], 5, 4) }),
                SaveEnemy(5, "Siege Crawler", "Boss",
                    new[] { Placement(byId["diesel_walker"], 7, 4), Placement(byId["mg_team"], 6, 4), Placement(byId["field_gun_nest"], 0, 0) }),
                SaveEnemy(6, "Reinforced Line", "Infantry",
                    new[] { Placement(byId["mg_team"], 6, 4), Placement(byId["rifle_squad"], 7, 4), Placement(byId["rifle_squad"], 5, 4) }),
                SaveEnemy(7, "Heavy Battery", "Artillery",
                    new[] { Placement(byId["field_gun_nest"], 0, 0), Placement(byId["mortar_crew"], 6, 3), Placement(byId["mortar_crew"], 5, 4) }),
                SaveEnemy(8, "Armored Push", "Mechanical",
                    new[] { Placement(byId["diesel_walker"], 7, 4), Placement(byId["diesel_walker"], 5, 4), Placement(byId["mg_team"], 6, 4) }),
                SaveEnemy(9, "Chemical Front", "Gas",
                    new[] { Placement(byId["gas_drone"], 6, 4), Placement(byId["gas_drone"], 4, 4), Placement(byId["armored_sapper"], 5, 4) }),
                SaveEnemy(10, "Final Assault", "Boss",
                    new[] { Placement(byId["diesel_walker"], 7, 4), Placement(byId["mobile_artillery"], 6, 3), Placement(byId["mg_team"], 5, 4), Placement(byId["mortar_crew"], 8, 3) })
            };
        }

        private static EnemyPiecePlacement Placement(PieceDefinitionSO piece, int x, int y) =>
            new EnemyPiecePlacement { piece = piece, anchor = new Vector2Int(x, y) };

        private static EnemyTemplateSO SaveEnemy(int fightNumber, string displayName, string previewTag, EnemyPiecePlacement[] placements)
        {
            var path = $"{Root}/Enemies/fight_{fightNumber}.asset";
            var asset = LoadOrCreate<EnemyTemplateSO>(path);
            asset.fightNumber = fightNumber;
            asset.displayName = displayName;
            asset.previewTag = previewTag;
            asset.placements = placements;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void CreateDatabase(PieceDefinitionSO[] pieces, FactionSO faction, EnemyTemplateSO[] enemies)
        {
            var path = $"{Root}/ContentDatabase.asset";
            var asset = LoadOrCreate<ContentDatabase>(path);
            var so = new SerializedObject(asset);
            so.FindProperty("pieces").arraySize = pieces.Length;
            for (int i = 0; i < pieces.Length; i++)
                so.FindProperty("pieces").GetArrayElementAtIndex(i).objectReferenceValue = pieces[i];
            so.FindProperty("factions").arraySize = 1;
            so.FindProperty("factions").GetArrayElementAtIndex(0).objectReferenceValue = faction;
            so.FindProperty("enemyTemplates").arraySize = enemies.Length;
            for (int i = 0; i < enemies.Length; i++)
                so.FindProperty("enemyTemplates").GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
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

    internal static class PieceArrayExtensions
    {
        public static System.Collections.Generic.Dictionary<string, PieceDefinitionSO> ToDictionary(
            this PieceDefinitionSO[] pieces,
            System.Func<PieceDefinitionSO, string> keySelector)
        {
            var dict = new System.Collections.Generic.Dictionary<string, PieceDefinitionSO>();
            foreach (var piece in pieces)
                dict[keySelector(piece)] = piece;
            return dict;
        }
    }
}
