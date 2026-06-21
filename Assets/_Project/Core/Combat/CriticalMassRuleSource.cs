using System;
using System.Collections.Generic;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public static class CriticalMassRuleSource
    {
        private static IReadOnlyList<CriticalMassRuleDefinition> _overrideRules;

        public static void SetRulesForTests(IReadOnlyList<CriticalMassRuleDefinition> rules) =>
            _overrideRules = rules;

        public static void ClearTestOverride() => _overrideRules = null;

        public static IReadOnlyList<CriticalMassRuleDefinition> GetRules() =>
            _overrideRules ?? CriticalMassRuleCatalog.GetRules();
    }
}
