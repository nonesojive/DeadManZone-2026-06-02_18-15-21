using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Content;

namespace DeadManZone.Core.Shop
{
    public static class ShopSlotLayoutResolver
    {
        public const int BaselineSlotCount = 6;
        public const int MaxSlotCount = 12;
        public const int SpecialtySlotCount = 3;

        public static IReadOnlyList<ShopSlotDefinition> Resolve(
            BoardState board,
            string factionId,
            ContentRegistry registry,
            ShopModifiers modifiers,
            bool? specialtyUnlocked = null)
        {
            var slots = new List<ShopSlotDefinition>(MaxSlotCount);

            for (int i = 0; i < 3; i++)
                slots.Add(new ShopSlotDefinition(i, ShopSlotKind.BaselineOffensive, ShopLane.Offensive));

            for (int i = 3; i < BaselineSlotCount; i++)
                slots.Add(new ShopSlotDefinition(i, ShopSlotKind.BaselineDefensive, ShopLane.Defensive));

            int nextIndex = BaselineSlotCount;
            int extraOffensive = modifiers?.ExtraGeneralSlots ?? 0;
            for (int i = 0; i < extraOffensive && nextIndex < MaxSlotCount; i++)
            {
                slots.Add(new ShopSlotDefinition(
                    nextIndex,
                    ShopSlotKind.ExtraOffensive,
                    ShopLane.Offensive));
                nextIndex++;
            }

            bool specialtyOpen = specialtyUnlocked ?? SpecialtyLaneUnlock.IsUnlocked(board, factionId, registry);
            if (specialtyOpen)
            {
                for (int i = 0; i < SpecialtySlotCount && nextIndex < MaxSlotCount; i++)
                {
                    slots.Add(new ShopSlotDefinition(
                        nextIndex,
                        ShopSlotKind.ExtraSpecialty,
                        ShopLane.Specialty));
                    nextIndex++;
                }
            }

            return slots;
        }

        public static (int columns, int rows) GetGridShape(int slotCount)
        {
            if (slotCount <= 6)
                return (3, 2);

            return (4, 3);
        }
    }
}
