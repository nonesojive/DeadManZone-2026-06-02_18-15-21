namespace DeadManZone.Core.Tags
{
    public enum SynergyStat
    {
        Damage,
        AttackRange,
        AttackSpeedSteps,
        MovementSpeed,
        ArmorType,
        MoveChargePercent,
        MaxHp,
        /// <summary>Percent reduction applied to incoming morale damage (MoraleRules.ApplyResistance).
        /// 2026-07-15 faction-roster-v1: Iron Guard's own stat + Breakthrough Tank's aura.</summary>
        MoraleResistancePercent
    }
}
