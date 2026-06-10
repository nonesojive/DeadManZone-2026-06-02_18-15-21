using System;
using System.Collections.Generic;

namespace DeadManZone.Core.Tags
{
    public sealed class SynergyTraitThresholds
    {
        public int[] Thresholds { get; init; } = { 2, 4 };
        public string Description { get; init; }
    }

    public static class SynergyTraitRegistry
    {
        private static readonly IReadOnlyDictionary<string, SynergyTraitThresholds> Catalog =
            new Dictionary<string, SynergyTraitThresholds>(StringComparer.OrdinalIgnoreCase)
            {
                [GameTagIds.Phalanx] = new() { Description = "Gains +1 damage and +10 HP for each adjacent infantry." },
                [GameTagIds.Inspiring] = new() { Description = "Adjacent units get +1 Move." },
                [GameTagIds.Medic] = new() { Description = "Adjacent infantry get +10 HP." },
                [GameTagIds.Mechanic] = new() { Description = "Gives adjacent vehicles the Repair tag." },
                [GameTagIds.Spotter] = new() { Description = "Adjacent snipers get +1 range." },
                [GameTagIds.Fortify] = new() { Description = "Adjacent units get +1 armor." },
                [GameTagIds.Jammer] = new() { Description = "Stealth and Ambush of adjacent units trigger twice." },
                [GameTagIds.Bunker] = new() { Description = "Adjacent infantry get +1 armor." },
                [GameTagIds.Fanatic] = new() { Description = "Gains +1 damage for each adjacent Fanatic." },
                [GameTagIds.Supplier] = new() { Description = "Doubles adjacent Logistics." },
                [GameTagIds.Entrenched] = new() { Description = "Gains +1 armor for each adjacent Fortification." },
                [GameTagIds.Bombard] = new() { Description = "Grants Bombard Tactic." },
                [GameTagIds.GasCloud] = new() { Description = "Gain Gas Cloud attack." },
                [GameTagIds.Convoy] = new() { Description = "Logistics convoy support." },
                [GameTagIds.SupplyLine] = new() { Description = "Extended supply network." },
                [GameTagIds.GasDivision] = new() { Description = "Coordinated gas warfare unit." },
                [GameTagIds.ChemicalCorps] = new() { Description = "Specialized chemical warfare corps." }
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
