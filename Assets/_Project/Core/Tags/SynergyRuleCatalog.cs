using System;
using System.Collections.Generic;

namespace DeadManZone.Core.Tags
{
    public static class SynergyRuleCatalog
    {
        private static readonly SynergyEffectDefinition[] Rules =
        {
            new()
            {
                SourceSynergyTagId = GameTagIds.Medic,
                Direction = SynergyDirection.Outbound,
                NeighborFilter = new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
                Stat = SynergyStat.ArmorType,
                ModType = SynergyModType.Flat,
                Magnitude = 1
            },
            new()
            {
                SourceSynergyTagId = GameTagIds.Command,
                Direction = SynergyDirection.Outbound,
                NeighborFilter = new NeighborFilter { CombatRoleTagId = GameTagIds.Artillery },
                Stat = SynergyStat.Damage,
                ModType = SynergyModType.Flat,
                Magnitude = 2
            },
            new()
            {
                SourceSynergyTagId = GameTagIds.Echo,
                Direction = SynergyDirection.Outbound,
                NeighborFilter = new NeighborFilter { AbilityTagId = GameTagIds.Stealth },
                Stat = SynergyStat.Damage,
                ModType = SynergyModType.Flat,
                Magnitude = 1
            },
            new()
            {
                SourceSynergyTagId = GameTagIds.Inspiring,
                Direction = SynergyDirection.Outbound,
                NeighborFilter = NeighborFilter.Any,
                Stat = SynergyStat.MoveChargePercent,
                ModType = SynergyModType.Flat,
                Magnitude = 5
            }
        };

        private static readonly IReadOnlyDictionary<string, IReadOnlyList<SynergyEffectDefinition>> RulesBySourceTag =
            BuildBySourceTag(Rules);

        public static IReadOnlyList<SynergyEffectDefinition> GetRulesForSourceTag(string sourceSynergyTagId)
        {
            if (string.IsNullOrWhiteSpace(sourceSynergyTagId))
                return Array.Empty<SynergyEffectDefinition>();

            return RulesBySourceTag.TryGetValue(sourceSynergyTagId.Trim(), out var rulesForTag)
                ? rulesForTag
                : Array.Empty<SynergyEffectDefinition>();
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<SynergyEffectDefinition>> BuildBySourceTag(
            IReadOnlyList<SynergyEffectDefinition> rules)
        {
            var buckets = new Dictionary<string, List<SynergyEffectDefinition>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (string.IsNullOrWhiteSpace(rule.SourceSynergyTagId))
                    continue;

                string sourceTagId = rule.SourceSynergyTagId.Trim();
                if (!buckets.TryGetValue(sourceTagId, out var list))
                {
                    list = new List<SynergyEffectDefinition>();
                    buckets[sourceTagId] = list;
                }

                list.Add(rule);
            }

            var result = new Dictionary<string, IReadOnlyList<SynergyEffectDefinition>>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in buckets)
            {
                result[pair.Key] = pair.Value;
            }

            return result;
        }
    }
}
