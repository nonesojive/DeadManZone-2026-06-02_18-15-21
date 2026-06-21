using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Data
{
    [Serializable]
    public sealed class CriticalMassRuleEntry
    {
        public string id;
        public string countTagId;
        public CriticalMassCountCategory countCategory;
        public CriticalMassTierEntry[] tiers = Array.Empty<CriticalMassTierEntry>();
        public CriticalMassStat stat;
        public SynergyModType modType;
        public CriticalMassScope scope;
        public CriticalMassTargetEntry target = new();
    }

    [Serializable]
    public sealed class CriticalMassTierEntry
    {
        public int threshold;
        public int magnitude;
    }

    [Serializable]
    public sealed class CriticalMassTargetEntry
    {
        public string[] primaryTagIds = Array.Empty<string>();
        public string combatRoleTagId;
        public string synergyTagId;
        public string abilityTagId;
        public string flavorTagId;
        public AttackType attackType = AttackType.None;
        public bool useAttackType;
        public AttackRangeTier attackRange = AttackRangeTier.Medium;
        public bool useAttackRange;
        public string factionId;
    }
}
