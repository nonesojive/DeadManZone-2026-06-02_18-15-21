using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Shop;
using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Shop/Config")]
    public class ShopConfigSO : ScriptableObject
    {
        private const string ResourcesPath = "DeadManZone/ShopConfig";

        [SerializeField] private ShopSlotProfileSO[] baselineProfiles = Array.Empty<ShopSlotProfileSO>();
        [SerializeField] private ShopSlotProfileSO[] bonusProfiles = Array.Empty<ShopSlotProfileSO>();

        public IReadOnlyList<ShopSlotProfileSO> BaselineProfiles => baselineProfiles;
        public IReadOnlyList<ShopSlotProfileSO> BonusProfiles => bonusProfiles;

        public ShopConfig ToCore()
        {
            if (baselineProfiles == null || baselineProfiles.Length == 0)
                return ShopConfig.CreateDefault();

            return new ShopConfig
            {
                BaselineProfiles = baselineProfiles
                    .Where(p => p != null)
                    .Select(p => p.ToCore())
                    .ToArray(),
                BonusProfiles = bonusProfiles?
                    .Where(p => p != null)
                    .Select(p => p.ToCore())
                    .ToArray() ?? Array.Empty<ShopSlotProfile>()
            };
        }

        public static ShopConfigSO LoadOrDefault()
        {
            var fromResources = Resources.Load<ShopConfigSO>(ResourcesPath);
            return fromResources;
        }
    }
}
