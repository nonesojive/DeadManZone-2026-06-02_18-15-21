using System.Collections.Generic;
using System.Linq;

namespace DeadManZone.Core.Shop
{
    public sealed class FactionShopOverride
    {
        public string FactionId { get; init; }
        public IReadOnlyDictionary<int, ShopSlotProfile> SlotOverrides { get; init; }
            = new Dictionary<int, ShopSlotProfile>();

        public ShopSlotProfile TryGetOverride(int slotIndex) =>
            SlotOverrides != null && SlotOverrides.TryGetValue(slotIndex, out var profile)
                ? profile
                : null;
    }

    public static class ShopSlotProfileResolver
    {
        public static ShopSlotProfile Resolve(
            ShopConfig config,
            ShopSlotProfile template,
            FactionShopOverride factionOverride)
        {
            if (template == null)
                return null;

            var overrideProfile = factionOverride?.TryGetOverride(template.SlotIndex);
            return overrideProfile ?? template;
        }

        public static IReadOnlyList<ShopSlotProfile> ResolveActiveSlots(
            ShopConfig config,
            IShopSlotUnlockRegistry unlockRegistry,
            ShopUnlockContext context,
            FactionShopOverride factionOverride = null)
        {
            var slots = new List<ShopSlotProfile>(ShopSlotLayoutResolver.MaxSlotCount);

            for (int i = 0; i < config.BaselineProfiles.Count; i++)
            {
                var template = config.BaselineProfiles[i];
                slots.Add(Resolve(config, template, factionOverride));
            }

            var unlocks = unlockRegistry?.Evaluate(context) ?? System.Array.Empty<ShopSlotUnlock>();
            foreach (var unlock in unlocks.OrderBy(u => u.SlotIndex))
            {
                if (unlock.SlotIndex < ShopSlotLayoutResolver.BaselineSlotCount)
                    continue;

                var template = unlock.Profile
                    ?? config.GetBonusProfile(unlock.SlotIndex);
                if (template == null)
                    continue;

                slots.Add(Resolve(config, template, factionOverride));
            }

            return slots;
        }
    }
}
