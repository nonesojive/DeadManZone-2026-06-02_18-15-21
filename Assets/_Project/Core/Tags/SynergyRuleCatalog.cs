using System;
using System.Collections.Generic;

namespace DeadManZone.Core.Tags
{
    public static class SynergyRuleCatalog
    {
        private static readonly SynergyEffectDefinition[] Rules = Array.Empty<SynergyEffectDefinition>();

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
