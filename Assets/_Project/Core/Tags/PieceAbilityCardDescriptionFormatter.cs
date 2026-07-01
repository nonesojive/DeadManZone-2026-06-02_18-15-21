using System;
using System.Text;

namespace DeadManZone.Core.Tags
{
    public static class PieceAbilityCardDescriptionFormatter
    {
        public static string Format(PieceAbilityDefinition ability)
        {
            if (ability.Magnitude == 0 && ability.Trigger != PieceAbilityTrigger.BoardPerTagCount)
                return string.Empty;

            string target = FormatTarget(ability.NeighborFilter);
            string bonus = FormatBonus(ability.Stat, ability.ModType, ability.Magnitude);
            if (string.IsNullOrEmpty(bonus))
                return string.Empty;

            return ability.Trigger switch
            {
                PieceAbilityTrigger.FightStart =>
                    $"At fight start: {target} gain {bonus}.",
                PieceAbilityTrigger.AdjacentAura when ability.ApplyToSelf =>
                    $"Gain {bonus} per adjacent {target}.",
                PieceAbilityTrigger.AdjacentAura =>
                    $"Adjacent {target} gain {bonus}.",
                PieceAbilityTrigger.BoardPerTagCount =>
                    FormatBoardPerTagLine(ability, target, bonus),
                _ => string.Empty
            };
        }

        private static string FormatBoardPerTagLine(
            PieceAbilityDefinition ability,
            string target,
            string bonus)
        {
            string countLabel = FormatTagLabel(ability.CountTagId);
            if (string.IsNullOrEmpty(countLabel))
                return string.Empty;

            return $"{bonus} to {target} per {countLabel} on your boards.";
        }

        private static string FormatTarget(NeighborFilter filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.PrimaryTagId))
                return FormatTagLabel(filter.PrimaryTagId);
            if (!string.IsNullOrWhiteSpace(filter.CombatRoleTagId))
                return FormatTagLabel(filter.CombatRoleTagId);
            if (!string.IsNullOrWhiteSpace(filter.SynergyTagId))
                return FormatTagLabel(filter.SynergyTagId);
            if (!string.IsNullOrWhiteSpace(filter.AbilityTagId))
                return FormatTagLabel(filter.AbilityTagId);
            if (!string.IsNullOrWhiteSpace(filter.SystemTagId))
                return FormatTagLabel(filter.SystemTagId);

            return "allies";
        }

        private static string FormatTagLabel(string tagId)
        {
            if (string.IsNullOrWhiteSpace(tagId))
                return string.Empty;

            return TagRegistry.TryGet(tagId, out var tag)
                ? tag.DisplayName
                : HumanizeTagId(tagId);
        }

        private static string HumanizeTagId(string tagId)
        {
            var parts = tagId.Split('_');
            var builder = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Length == 0)
                    continue;

                if (builder.Length > 0)
                    builder.Append(' ');

                builder.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                    builder.Append(part.Substring(1));
            }

            return builder.Length == 0 ? tagId : builder.ToString();
        }

        private static string FormatBonus(SynergyStat stat, SynergyModType modType, int magnitude)
        {
            if (magnitude == 0)
                return string.Empty;

            string signed = magnitude > 0 ? $"+{magnitude}" : magnitude.ToString();
            return stat switch
            {
                SynergyStat.Damage => $"{signed} damage",
                SynergyStat.MaxHp when modType == SynergyModType.Percent => $"{signed}% max HP",
                SynergyStat.MaxHp => $"{signed} max HP",
                SynergyStat.AttackSpeedSteps => $"{signed} attack speed tier",
                SynergyStat.MovementSpeed => $"{signed} movement speed",
                SynergyStat.ArmorType => $"{signed} armor",
                SynergyStat.MoveChargePercent => $"{signed}% move charge",
                SynergyStat.AttackRange => $"{signed} attack range",
                _ => string.Empty
            };
        }
    }
}
