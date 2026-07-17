using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public struct CriticalMassCombatModifiers
    {
        public int MaxHpFlat;
        public int MaxHpPercent;
        public int DamageFlat;
        public int DamagePercent;
        public int AccuracyPercent;
        public int AttackSpeedSteps;
        public int MovementSpeedSteps;
        public int AttackRangeSteps;
        public int MoveChargePercentBonus;
        /// <summary>Oathborn faction CM rule: flat bonus to a combatant's max morale pool.</summary>
        public int MaxMoraleFlat;
        /// <summary>Crimson faction CM rule: extra ticks folded into this combatant's on-hit
        /// Suppression applications (SuppressionRules.Apply), harmless on non-suppressing pieces.</summary>
        public int SuppressionDurationTicksBonus;
        /// <summary>Ashen faction CM rule: percent uplift to this combatant's own
        /// LowStateDamageBonus while in low-state.</summary>
        public int LowStateDamageBonusPercent;

        public void Add(CriticalMassStat stat, SynergyModType modType, int magnitude)
        {
            if (magnitude == 0)
                return;

            switch (stat)
            {
                case CriticalMassStat.MaxMorale when modType == SynergyModType.Flat:
                    MaxMoraleFlat += magnitude;
                    break;
                case CriticalMassStat.SuppressionDuration when modType == SynergyModType.Flat:
                    SuppressionDurationTicksBonus += magnitude;
                    break;
                case CriticalMassStat.LowStateDamageBonus when modType == SynergyModType.Percent:
                    LowStateDamageBonusPercent += magnitude;
                    break;
                case CriticalMassStat.MaxHp when modType == SynergyModType.Flat:
                    MaxHpFlat += magnitude;
                    break;
                case CriticalMassStat.MaxHp when modType == SynergyModType.Percent:
                    MaxHpPercent += magnitude;
                    break;
                case CriticalMassStat.Damage when modType == SynergyModType.Flat:
                    DamageFlat += magnitude;
                    break;
                case CriticalMassStat.Damage when modType == SynergyModType.Percent:
                    DamagePercent += magnitude;
                    break;
                case CriticalMassStat.Accuracy when modType == SynergyModType.Percent:
                    AccuracyPercent += magnitude;
                    break;
                case CriticalMassStat.AttackSpeed when modType == SynergyModType.TierStep:
                    AttackSpeedSteps += magnitude;
                    break;
                case CriticalMassStat.MovementSpeed when modType == SynergyModType.TierStep:
                    MovementSpeedSteps += magnitude;
                    MoveChargePercentBonus += magnitude * 5;
                    break;
                case CriticalMassStat.AttackRange when modType == SynergyModType.TierStep:
                    AttackRangeSteps += magnitude;
                    break;
            }
        }

        public bool IsEmpty =>
            MaxHpFlat == 0
            && MaxHpPercent == 0
            && DamageFlat == 0
            && DamagePercent == 0
            && AccuracyPercent == 0
            && AttackSpeedSteps == 0
            && MovementSpeedSteps == 0
            && AttackRangeSteps == 0
            && MoveChargePercentBonus == 0
            && MaxMoraleFlat == 0
            && SuppressionDurationTicksBonus == 0
            && LowStateDamageBonusPercent == 0;
    }
}
