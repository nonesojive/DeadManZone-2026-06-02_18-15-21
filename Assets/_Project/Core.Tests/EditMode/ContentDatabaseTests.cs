using DeadManZone.Core.Board;
using DeadManZone.Core.Shop;
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
            piece.id = "rifle_squad";
            piece.displayName = "Rifle Squad";
            piece.category = PieceCategory.Unit;
            piece.shapeCells = new[] { Vector2Int.zero };
            piece.tags = new[] { "Infantry" };
            piece.maxHp = 10;
            piece.baseDamage = 2;
            piece.goldCost = 5;
            piece.shopLane = ShopLane.General;

            var core = piece.ToCore();

            Assert.AreEqual("rifle_squad", core.Id);
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
            rifle.id = "rifle_squad";
            rifle.displayName = "Rifle Squad";
            rifle.category = PieceCategory.Unit;
            rifle.shapeCells = new[] { Vector2Int.zero };
            rifle.shopLane = ShopLane.General;

            var bunker = ScriptableObject.CreateInstance<Data.PieceDefinitionSO>();
            bunker.id = "command_bunker";
            bunker.displayName = "Command Bunker";
            bunker.category = PieceCategory.Building;
            bunker.shapeCells = new[] { Vector2Int.zero };
            bunker.shopLane = ShopLane.Engineers;

            typeof(Data.ContentDatabase)
                .GetField("pieces", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(database, new[] { rifle, bunker });

            var registry = database.BuildRegistry();

            Assert.AreEqual("rifle_squad", registry.GetById("rifle_squad").Id);
            Assert.AreEqual(1, registry.GetPool(ShopLane.Engineers).Count);

            Object.DestroyImmediate(rifle);
            Object.DestroyImmediate(bunker);
            Object.DestroyImmediate(database);
        }
    }
}
