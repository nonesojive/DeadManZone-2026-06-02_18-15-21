using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;

namespace DeadManZone.Core.Tags
{
    /// <summary>Planning-doc defaults; seeded into <see cref="Data.CriticalMassDatabaseSO"/> by editor generator.</summary>
    public static class CriticalMassDefaultRules
    {
        public static CriticalMassRuleDefinition[] Build() => new[]
        {
            PrimaryRule("infantry", GameTagIds.Infantry, T(5, 10, 7, 15, 10, 20), CriticalMassStat.MaxHp, SynergyModType.Flat, TargetPrimary(GameTagIds.Infantry)),
            PrimaryRule("vehicle", GameTagIds.Vehicle, T(3, 5, 5, 8, 7, 12), CriticalMassStat.Accuracy, SynergyModType.Percent, TargetPrimary(GameTagIds.Vehicle)),
            PrimaryRule("structure", GameTagIds.Structure, T(3, 15, 5, 25, 7, 40), CriticalMassStat.MaxHp, SynergyModType.Flat, TargetPrimary(GameTagIds.Structure)),
            RoleRule("assault", GameTagIds.Assault, T(5, 1, 7, 2, 10, 3), CriticalMassStat.Damage, SynergyModType.Flat, TargetPrimary(GameTagIds.Infantry)),
            RoleRule("tank", GameTagIds.Tank, T(3, 1, 5, 2, 7, 3), CriticalMassStat.Damage, SynergyModType.Flat, TargetPrimary(GameTagIds.Vehicle)),
            RoleRule("artillery", GameTagIds.Artillery, T(3, 1, 5, 2, 7, 3), CriticalMassStat.AttackSpeed, SynergyModType.TierStep, TargetAttackType(AttackType.Explosive)),
            RoleRule("sniper", GameTagIds.Sniper, T(3, 1, 5, 2, 7, 3), CriticalMassStat.AttackSpeed, SynergyModType.TierStep, TargetAttackRange(AttackRangeTier.Long)),
            RoleRule("defender", GameTagIds.Defender, T(3, 5, 5, 10, 7, 15), CriticalMassStat.MaxHp, SynergyModType.Percent, TargetPrimary(GameTagIds.Infantry, GameTagIds.Vehicle)),
            RoleRule("utility", GameTagIds.Utility, T(3, 1, 5, 2, 7, 3), CriticalMassStat.MovementSpeed, SynergyModType.TierStep, CriticalMassTargetFilter.Any),
            RoleRule("support", GameTagIds.Support, T(3, 1, 5, 2, 7, 3), CriticalMassStat.AttackSpeed, SynergyModType.TierStep, TargetPrimary(GameTagIds.Infantry, GameTagIds.Vehicle)),
            AttackTypeRule("explosive", AttackType.Explosive, T(5, 5, 7, 10, 10, 15), CriticalMassStat.Damage, SynergyModType.Percent),
            AttackTypeRule("melee", AttackType.Melee, T(3, 5, 5, 10, 7, 15), CriticalMassStat.MaxHp, SynergyModType.Percent),
            AttackTypeRule("gas", AttackType.Gas, T(3, 5, 5, 10, 7, 15), CriticalMassStat.Damage, SynergyModType.Percent),
            AttackTypeRule("fire", AttackType.Fire, T(3, 5, 5, 10, 7, 15), CriticalMassStat.Damage, SynergyModType.Percent),
            AttackTypeRule("ballistic", AttackType.Ballistic, T(5, 5, 7, 10, 10, 15), CriticalMassStat.Damage, SynergyModType.Percent),
            AttackTypeRule("piercing", AttackType.Piercing, T(5, 5, 7, 10, 10, 15), CriticalMassStat.Damage, SynergyModType.Percent),
            AttackTypeRule("shredding", AttackType.Shredding, T(5, 5, 7, 10, 10, 15), CriticalMassStat.Damage, SynergyModType.Percent),
            SynergyRule("phalanx", GameTagIds.Phalanx, T(5, 5, 7, 10, 10, 15), CriticalMassStat.MaxHp, SynergyModType.Percent, TargetSynergy(GameTagIds.Phalanx)),
            RunRule("command", GameTagIds.Command, T4(2, 1, 4, 3, 6, 6, 8, 10), CriticalMassStat.Authority, SynergyModType.Flat),
            RunRule("supplier", GameTagIds.Supplier, T4(2, 20, 4, 45, 6, 70, 8, 100), CriticalMassStat.Supplies, SynergyModType.Flat),
            SynergyRule("convoy", GameTagIds.Convoy, T(3, 1, 5, 2, 7, 3), CriticalMassStat.MovementSpeed, SynergyModType.TierStep, TargetSynergy(GameTagIds.Convoy)),
            SynergyRule("medic", GameTagIds.Medic, T(3, 5, 5, 10, 7, 15), CriticalMassStat.MaxHp, SynergyModType.Percent, TargetPrimary(GameTagIds.Infantry)),
            SynergyRule("mechanic", GameTagIds.Mechanic, T(3, 5, 5, 10, 7, 15), CriticalMassStat.MaxHp, SynergyModType.Percent, TargetPrimary(GameTagIds.Vehicle)),
            SynergyRule("fanatic", GameTagIds.Fanatic, T(3, 1, 5, 2, 7, 3), CriticalMassStat.AttackSpeed, SynergyModType.TierStep, TargetSynergy(GameTagIds.Fanatic)),
            SynergyRule("entrenched", GameTagIds.Entrenched, T(3, 5, 5, 10, 7, 15), CriticalMassStat.MaxHp, SynergyModType.Percent, TargetRole(GameTagIds.Defender)),
            AbilityRule("berserk", GameTagIds.Berserk, T(3, 1, 5, 2, 7, 3), CriticalMassStat.AttackSpeed, SynergyModType.TierStep, TargetRole(GameTagIds.Assault)),
            AbilityRule("grenadier", GameTagIds.Grenadier, T(3, 1, 5, 2, 7, 3), CriticalMassStat.AttackRange, SynergyModType.TierStep, TargetAbility(GameTagIds.Grenadier)),
            FlavorRule("siege", GameTagIds.Siege, T(2, 1, 4, 2, 6, 3), CriticalMassStat.AttackSpeed, SynergyModType.TierStep, TargetRole(GameTagIds.Artillery)),
            RunRule("logistics", GameTagIds.Logistics, T(3, 5, 5, 10, 7, 15), CriticalMassStat.Supplies, SynergyModType.Percent, CriticalMassCountCategory.Flavor),
            FactionRule("ironmarch_union", FactionIds.IronmarchUnion, T(5, 1, 7, 2, 10, 3), TargetPrimary(GameTagIds.Infantry))
        };

        public static void RegisterWithCatalog() =>
            CriticalMassRuleCatalog.Register(Build());

        private static CriticalMassTier[] T(int t1, int m1, int t2, int m2, int t3, int m3) => new[]
        {
            new CriticalMassTier { Threshold = t1, Magnitude = m1 },
            new CriticalMassTier { Threshold = t2, Magnitude = m2 },
            new CriticalMassTier { Threshold = t3, Magnitude = m3 }
        };

        private static CriticalMassTier[] T4(int t1, int m1, int t2, int m2, int t3, int m3, int t4, int m4) => new[]
        {
            new CriticalMassTier { Threshold = t1, Magnitude = m1 },
            new CriticalMassTier { Threshold = t2, Magnitude = m2 },
            new CriticalMassTier { Threshold = t3, Magnitude = m3 },
            new CriticalMassTier { Threshold = t4, Magnitude = m4 }
        };

        private static CriticalMassRuleDefinition PrimaryRule(
            string id, string tag, CriticalMassTier[] tiers, CriticalMassStat stat, SynergyModType modType, CriticalMassTargetFilter target) =>
            Rule(id, tag, CriticalMassCountCategory.Primary, tiers, stat, modType, target);

        private static CriticalMassRuleDefinition RoleRule(
            string id, string tag, CriticalMassTier[] tiers, CriticalMassStat stat, SynergyModType modType, CriticalMassTargetFilter target) =>
            Rule(id, tag, CriticalMassCountCategory.CombatRole, tiers, stat, modType, target);

        private static CriticalMassRuleDefinition SynergyRule(
            string id, string tag, CriticalMassTier[] tiers, CriticalMassStat stat, SynergyModType modType, CriticalMassTargetFilter target) =>
            Rule(id, tag, CriticalMassCountCategory.Synergy, tiers, stat, modType, target);

        private static CriticalMassRuleDefinition AbilityRule(
            string id, string tag, CriticalMassTier[] tiers, CriticalMassStat stat, SynergyModType modType, CriticalMassTargetFilter target) =>
            Rule(id, tag, CriticalMassCountCategory.Ability, tiers, stat, modType, target);

        private static CriticalMassRuleDefinition FlavorRule(
            string id, string tag, CriticalMassTier[] tiers, CriticalMassStat stat, SynergyModType modType, CriticalMassTargetFilter target) =>
            Rule(id, tag, CriticalMassCountCategory.Flavor, tiers, stat, modType, target);

        private static CriticalMassRuleDefinition AttackTypeRule(
            string id, AttackType attackType, CriticalMassTier[] tiers, CriticalMassStat stat, SynergyModType modType) =>
            Rule(id, attackType.ToString().ToLowerInvariant(), CriticalMassCountCategory.AttackType, tiers, stat, modType, TargetAttackType(attackType));

        private static CriticalMassRuleDefinition RunRule(
            string id, string tag, CriticalMassTier[] tiers, CriticalMassStat stat, SynergyModType modType,
            CriticalMassCountCategory countCategory = CriticalMassCountCategory.Synergy) =>
            Rule(id, tag, countCategory, tiers, stat, modType, CriticalMassTargetFilter.Any, CriticalMassScope.RunResources);

        private static CriticalMassRuleDefinition FactionRule(string id, string factionId, CriticalMassTier[] tiers, CriticalMassTargetFilter target) =>
            Rule(id, factionId, CriticalMassCountCategory.Faction, tiers, CriticalMassStat.Damage, SynergyModType.Flat, target);

        private static CriticalMassRuleDefinition Rule(
            string id,
            string countTagId,
            CriticalMassCountCategory countCategory,
            CriticalMassTier[] tiers,
            CriticalMassStat stat,
            SynergyModType modType,
            CriticalMassTargetFilter target,
            CriticalMassScope scope = CriticalMassScope.FightCombat) => new()
        {
            Id = id,
            CountTagId = countTagId,
            CountCategory = countCategory,
            Tiers = tiers,
            Stat = stat,
            ModType = modType,
            Scope = scope,
            Target = target
        };

        private static CriticalMassTargetFilter TargetPrimary(params string[] tags) =>
            new() { PrimaryTagIds = tags };

        private static CriticalMassTargetFilter TargetSynergy(string tag) =>
            new() { SynergyTagId = tag };

        private static CriticalMassTargetFilter TargetRole(string tag) =>
            new() { CombatRoleTagId = tag };

        private static CriticalMassTargetFilter TargetAbility(string tag) =>
            new() { AbilityTagId = tag };

        private static CriticalMassTargetFilter TargetAttackType(AttackType attackType) =>
            new() { AttackType = attackType };

        private static CriticalMassTargetFilter TargetAttackRange(AttackRangeTier range) =>
            new() { AttackRange = range };
    }
}
