using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Shop;
using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Shop/Faction Override")]
    public class FactionShopOverrideSO : ScriptableObject
    {
        public string factionId;
        public ShopSlotProfileSO[] slotOverrides = System.Array.Empty<ShopSlotProfileSO>();

        public FactionShopOverride ToCore()
        {
            var map = new Dictionary<int, ShopSlotProfile>();
            if (slotOverrides != null)
            {
                foreach (var profile in slotOverrides.Where(p => p != null))
                    map[profile.slotIndex] = profile.ToCore();
            }

            return new FactionShopOverride
            {
                FactionId = factionId,
                SlotOverrides = map
            };
        }
    }
}
