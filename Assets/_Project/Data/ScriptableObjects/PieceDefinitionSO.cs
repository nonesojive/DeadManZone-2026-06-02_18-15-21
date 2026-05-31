using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Shop;
using System.Collections.Generic;
using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Piece Definition")]
    public class PieceDefinitionSO : ScriptableObject
    {
        public string id;
        public string displayName;
        public PieceCategory category;
        public Vector2Int[] shapeCells = { Vector2Int.zero };
        public string[] tags;
        public int maxHp = 10;
        public int baseDamage;
        public int cooldownTicks = 3;
        public int goldCost = 5;
        public int requisitionCost;
        public ShopModifierFlags shopModifiers;
        public CommandActionFlags commandActions;
        public ShopLane shopLane = ShopLane.General;

        public PieceDefinition ToCore()
        {
            var cells = new List<GridCoord>();
            foreach (var cell in shapeCells)
                cells.Add(new GridCoord(cell.x, cell.y));

            return new PieceDefinition
            {
                Id = id,
                DisplayName = displayName,
                Category = category,
                Shape = new PieceShape(cells),
                Tags = tags ?? System.Array.Empty<string>(),
                MaxHp = maxHp,
                BaseDamage = baseDamage,
                CooldownTicks = cooldownTicks,
                GoldCost = goldCost,
                RequisitionCost = requisitionCost,
                ShopModifiers = shopModifiers,
                CommandActions = commandActions
            };
        }
    }
}
