using System.Collections.Generic;

namespace DeadManZone.Core.Tags
{
    public static class CriticalMassRuleCatalog
    {
        private static readonly CriticalMassRuleDefinition[] DemoRules =
        {
            new()
            {
                TagId = GameTagIds.Infantry,
                CountCategory = CriticalMassCountCategory.Primary,
                Threshold = 3,
                DamageBonus = 2
            },
            new()
            {
                TagId = GameTagIds.Vehicle,
                CountCategory = CriticalMassCountCategory.Primary,
                Threshold = 2,
                ArmorShredSteps = 1
            },
            new()
            {
                TagId = GameTagIds.Artillery,
                CountCategory = CriticalMassCountCategory.CombatRole,
                Threshold = 2,
                DamageBonus = 3
            },
            new()
            {
                TagId = GameTagIds.Assault,
                CountCategory = CriticalMassCountCategory.CombatRole,
                Threshold = 3,
                MoveChargePercentBonus = 10
            }
        };

        public static IReadOnlyList<CriticalMassRuleDefinition> GetRules() => DemoRules;
    }
}
