using System.Collections.Generic;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    /// <summary>Holds runtime critical mass rules loaded from <see cref="Data.CriticalMassDatabaseSO"/>.</summary>
    public static class CriticalMassRuleCatalog
    {
        private static IReadOnlyList<CriticalMassRuleDefinition> _registeredRules =
            System.Array.Empty<CriticalMassRuleDefinition>();

        public static void Register(IReadOnlyList<CriticalMassRuleDefinition> rules) =>
            _registeredRules = rules ?? System.Array.Empty<CriticalMassRuleDefinition>();

        public static IReadOnlyList<CriticalMassRuleDefinition> GetRules() => _registeredRules;
    }
}
