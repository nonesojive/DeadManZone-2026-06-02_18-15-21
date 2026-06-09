using System;
using System.Collections.Generic;

namespace DeadManZone.Core.Tags
{
    public sealed class SynergyTraitThresholds
    {
        public int[] Thresholds { get; init; }
        public string Description { get; init; }
    }

    public static class SynergyTraitRegistry
    {
        private static readonly IReadOnlyDictionary<string, SynergyTraitThresholds> Catalog =
            new Dictionary<string, SynergyTraitThresholds>(StringComparer.OrdinalIgnoreCase);

        public static bool TryGet(string tagId, out SynergyTraitThresholds thresholds)
        {
            if (string.IsNullOrWhiteSpace(tagId))
            {
                thresholds = null;
                return false;
            }
            return Catalog.TryGetValue(tagId, out thresholds);
        }

        public static IEnumerable<string> GetAllTraitTagIds() => Catalog.Keys;
    }
}
