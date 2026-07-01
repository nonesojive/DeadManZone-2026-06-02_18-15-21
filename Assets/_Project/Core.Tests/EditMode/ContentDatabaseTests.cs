using DeadManZone.Core.Board;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Core.Tests
{
    public class ContentDatabaseTests
    {
        [Test]
        public void PieceDefinitionSO_ToCore_MapsFields()
        {
            var piece = ScriptableObject.CreateInstance<Data.PieceDefinitionSO>();
            piece.id = "conscript_rifleman";
            piece.displayName = "Conscript Rifleman";
            piece.category = PieceCategory.Unit;
            piece.shapeCells = new[] { Vector2Int.zero };
            piece.tags = new[] { "Infantry" };
            piece.maxHp = 10;
            piece.baseDamage = 2;
            piece.goldCost = 5;
            piece.shopLane = ShopLane.Offensive;

            var core = piece.ToCore();

            Assert.AreEqual("conscript_rifleman", core.Id);
            Assert.AreEqual(PieceCategory.Unit, core.Category);
            Assert.AreEqual(10, core.MaxHp);
            Assert.AreEqual(2, core.BaseDamage);

            Object.DestroyImmediate(piece);
        }

        [Test]
        public void ContentDatabase_BuildRegistry_RegistersAllPieces()
        {
            var database = ScriptableObject.CreateInstance<Data.ContentDatabase>();
            var rifle = ScriptableObject.CreateInstance<Data.PieceDefinitionSO>();
            rifle.id = "conscript_rifleman";
            rifle.displayName = "Conscript Rifleman";
            rifle.category = PieceCategory.Unit;
            rifle.shapeCells = new[] { Vector2Int.zero };
            rifle.combatRole = GameTagIds.Assault;
            rifle.includeInShopPool = true;

            var radio = ScriptableObject.CreateInstance<Data.PieceDefinitionSO>();
            radio.id = "command_outpost";
            radio.displayName = "Command Outpost";
            radio.category = PieceCategory.Building;
            radio.shapeCells = new[] { Vector2Int.zero, Vector2Int.up };
            radio.combatRole = GameTagIds.Utility;
            radio.includeInShopPool = true;

            typeof(Data.ContentDatabase)
                .GetField("pieces", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(database, new[] { rifle, radio });

            var registry = database.BuildRegistry();

            Assert.AreEqual("conscript_rifleman", registry.GetById("conscript_rifleman").Id);
            Assert.AreEqual(1, registry.GetPool(ShopLane.Defensive).Count);

            Object.DestroyImmediate(rifle);
            Object.DestroyImmediate(radio);
            Object.DestroyImmediate(database);
        }

        [Test]
        public void ContentDatabase_BuildRegistry_RespectsIncludeInShopPoolFlag()
        {
            var database = ScriptableObject.CreateInstance<Data.ContentDatabase>();
            var inShop = ScriptableObject.CreateInstance<Data.PieceDefinitionSO>();
            inShop.id = "in_shop";
            inShop.displayName = "In Shop";
            inShop.category = PieceCategory.Unit;
            inShop.shapeCells = new[] { Vector2Int.zero };
            inShop.combatRole = GameTagIds.Assault;
            inShop.includeInShopPool = true;

            var hidden = ScriptableObject.CreateInstance<Data.PieceDefinitionSO>();
            hidden.id = "hidden_building";
            hidden.displayName = "Hidden Building";
            hidden.category = PieceCategory.Building;
            hidden.shapeCells = new[] { Vector2Int.zero };
            hidden.combatRole = GameTagIds.Utility;
            hidden.includeInShopPool = false;

            typeof(Data.ContentDatabase)
                .GetField("pieces", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(database, new[] { inShop, hidden });

            var registry = database.BuildRegistry();

            Assert.AreEqual(1, registry.GetPool(ShopLane.Offensive).Count);
            Assert.IsTrue(registry.TryGetById("hidden_building", out _));

            Object.DestroyImmediate(inShop);
            Object.DestroyImmediate(hidden);
            Object.DestroyImmediate(database);
        }
    }
}
