using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Shop
{
    public sealed class ShopConfig
    {
        private static readonly string[] OffensiveRoles =
        {
            GameTagIds.Assault,
            GameTagIds.Sniper,
            GameTagIds.Tank
        };

        private static readonly string[] DefensiveRoles =
        {
            GameTagIds.Support,
            GameTagIds.Utility,
            GameTagIds.Defender
        };

        public IReadOnlyList<ShopSlotProfile> BaselineProfiles { get; init; } = Array.Empty<ShopSlotProfile>();
        public IReadOnlyList<ShopSlotProfile> BonusProfiles { get; init; } = Array.Empty<ShopSlotProfile>();

        public ShopSlotProfile GetBaselineProfile(int slotIndex) =>
            BaselineProfiles.FirstOrDefault(p => p.SlotIndex == slotIndex);

        public ShopSlotProfile GetBonusProfile(int slotIndex) =>
            BonusProfiles.FirstOrDefault(p => p.SlotIndex == slotIndex);

        public static ShopConfig CreateDefault()
        {
            var baseline = new List<ShopSlotProfile>(ShopSlotLayoutResolver.BaselineSlotCount);
            for (int i = 0; i < 3; i++)
            {
                baseline.Add(new ShopSlotProfile
                {
                    SlotIndex = i,
                    Kind = ShopSlotKind.BaselineOffensive,
                    PoolBias = ShopPoolBias.Offensive,
                    PreferredCombatRoles = OffensiveRoles
                });
            }

            for (int i = 3; i < ShopSlotLayoutResolver.ReservedSlotStartIndex; i++)
            {
                baseline.Add(new ShopSlotProfile
                {
                    SlotIndex = i,
                    Kind = ShopSlotKind.BaselineDefensive,
                    PoolBias = ShopPoolBias.Defensive,
                    PreferredCombatRoles = DefensiveRoles
                });
            }

            for (int i = ShopSlotLayoutResolver.ReservedSlotStartIndex;
                 i < ShopSlotLayoutResolver.BaselineSlotCount;
                 i++)
            {
                baseline.Add(new ShopSlotProfile
                {
                    SlotIndex = i,
                    Kind = ShopSlotKind.ReservedAbility,
                    PoolBias = ShopPoolBias.Defensive,
                    PreferredCombatRoles = DefensiveRoles
                });
            }

            var bonus = new List<ShopSlotProfile>(ShopSlotLayoutResolver.BonusSlotCount);
            for (int i = ShopSlotLayoutResolver.BaselineSlotCount; i < ShopSlotLayoutResolver.MaxSlotCount; i++)
            {
                bool offensive = (i - ShopSlotLayoutResolver.BaselineSlotCount) % 2 == 0;
                bonus.Add(new ShopSlotProfile
                {
                    SlotIndex = i,
                    Kind = ShopSlotKind.Bonus,
                    PoolBias = offensive ? ShopPoolBias.Offensive : ShopPoolBias.Defensive,
                    PreferredCombatRoles = offensive ? OffensiveRoles : DefensiveRoles
                });
            }

            return new ShopConfig
            {
                BaselineProfiles = baseline,
                BonusProfiles = bonus
            };
        }
    }
}
