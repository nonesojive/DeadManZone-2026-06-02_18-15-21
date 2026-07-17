using System.Collections.Generic;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Critical Mass Database")]
    public sealed class CriticalMassDatabaseSO : ScriptableObject
    {
        private const string ResourcesPath = "DeadManZone/CriticalMassDatabase";

        [SerializeField] private CriticalMassRuleEntry[] rules = System.Array.Empty<CriticalMassRuleEntry>();

        public IReadOnlyList<CriticalMassRuleEntry> Rules => rules;

        public IReadOnlyList<CriticalMassRuleDefinition> BuildRuleDefinitions()
        {
            if (rules == null || rules.Length == 0)
                return System.Array.Empty<CriticalMassRuleDefinition>();

            var built = new CriticalMassRuleDefinition[rules.Length];
            for (int i = 0; i < rules.Length; i++)
                built[i] = ToDefinition(rules[i]);

            return built;
        }

        public static CriticalMassDatabaseSO LoadDefault()
        {
            var asset = Resources.Load<CriticalMassDatabaseSO>(ResourcesPath);
            return asset;
        }

        public void RegisterWithCatalog()
        {
            CriticalMassRuleCatalog.Register(BuildRuleDefinitions());
        }

        internal static CriticalMassRuleDefinition ToDefinition(CriticalMassRuleEntry entry)
        {
            if (entry == null)
                return default;

            var tiers = new CriticalMassTier[entry.tiers?.Length ?? 0];
            for (int i = 0; i < tiers.Length; i++)
            {
                tiers[i] = new CriticalMassTier
                {
                    Threshold = entry.tiers[i].threshold,
                    Magnitude = entry.tiers[i].magnitude
                };
            }

            return new CriticalMassRuleDefinition
            {
                Id = entry.id,
                CountTagId = entry.countTagId,
                CountCategory = entry.countCategory,
                Tiers = tiers,
                Stat = entry.stat,
                ModType = entry.modType,
                Scope = entry.scope,
                Target = ToTargetFilter(entry.target)
            };
        }

        private static CriticalMassTargetFilter ToTargetFilter(CriticalMassTargetEntry target)
        {
            if (target == null)
                return CriticalMassTargetFilter.Any;

            return new CriticalMassTargetFilter
            {
                PrimaryTagIds = target.primaryTagIds,
                CombatRoleTagId = target.combatRoleTagId,
                SynergyTagId = target.synergyTagId,
                AbilityTagId = target.abilityTagId,
                FlavorTagId = target.flavorTagId,
                AttackType = target.useAttackType ? target.attackType : null,
                AttackRange = target.useAttackRange ? target.attackRange : null,
                FactionId = target.factionId,
                RequireSalvage = target.requireSalvage
            };
        }
    }
}
