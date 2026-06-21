using System.Collections.Generic;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public readonly struct EvaluatedCriticalMassRule
    {
        public CriticalMassRuleDefinition Rule { get; init; }
        public int Count { get; init; }
        public int ActiveTierIndex { get; init; }
        public CriticalMassTier ActiveTier { get; init; }
        public bool IsActive => ActiveTierIndex >= 0;
    }

    public sealed class CriticalMassSnapshot
    {
        public static CriticalMassSnapshot Empty { get; } = new(
            System.Array.Empty<EvaluatedCriticalMassRule>(),
            0,
            0,
            0,
            new Dictionary<string, CriticalMassCombatModifiers>(System.StringComparer.Ordinal));

        public IReadOnlyList<EvaluatedCriticalMassRule> Rules { get; }
        public int AuthorityBonus { get; }
        public int SuppliesFlatBonus { get; }
        public int SuppliesPercentBonus { get; }
        public IReadOnlyDictionary<string, CriticalMassCombatModifiers> ModifiersByInstanceId { get; }

        public bool HasAnyActiveRule
        {
            get
            {
                for (int i = 0; i < Rules.Count; i++)
                {
                    if (Rules[i].IsActive)
                        return true;
                }

                return false;
            }
        }

        internal CriticalMassSnapshot(
            IReadOnlyList<EvaluatedCriticalMassRule> rules,
            int authorityBonus,
            int suppliesFlatBonus,
            int suppliesPercentBonus,
            IReadOnlyDictionary<string, CriticalMassCombatModifiers> modifiersByInstanceId)
        {
            Rules = rules;
            AuthorityBonus = authorityBonus;
            SuppliesFlatBonus = suppliesFlatBonus;
            SuppliesPercentBonus = suppliesPercentBonus;
            ModifiersByInstanceId = modifiersByInstanceId;
        }
    }
}
