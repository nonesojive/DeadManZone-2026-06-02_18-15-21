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
            new Dictionary<string, SynergyTraitThresholds>(StringComparer.OrdinalIgnoreCase)
            {
                [GameTagIds.Supply] = new() { Thresholds = new[] { 2, 4, 6 }, Description = "Grants bonus damage to adjacent allies." },
                [GameTagIds.Medic] = new() { Thresholds = new[] { 2, 4 }, Description = "Provides armor buff steps to adjacent infantry." },
                [GameTagIds.Command] = new() { Thresholds = new[] { 1, 2, 3 }, Description = "Greatly increases damage for nearby artillery." },
                [GameTagIds.Vanguard] = new() { Thresholds = new[] { 2, 4 }, Description = "Increases movement charge for forward units." },
                [GameTagIds.Mechanical] = new() { Thresholds = new[] { 3, 6 }, Description = "Boosts performance of mechanical units." },
                [GameTagIds.Gas] = new() { Thresholds = new[] { 2, 4 }, Description = "Applies attrition effects to enemies." },
                [GameTagIds.Stealth] = new() { Thresholds = new[] { 2, 4 }, Description = "Grants concealment-based bonuses." },
                [GameTagIds.Echo] = new() { Thresholds = new[] { 2 }, Description = "Mirrors tactical bonuses." }
            };

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
