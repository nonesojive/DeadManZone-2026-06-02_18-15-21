namespace DeadManZone.Core.Combat
{
    public enum MatchupLabel
    {
        Favorable,
        Even,
        Dangerous
    }

    public readonly struct MatchupAssessment
    {
        public ArmyStrengthSnapshot Player { get; init; }
        public ArmyStrengthSnapshot Enemy { get; init; }
        public float Ratio { get; init; }
        public MatchupLabel Label { get; init; }

        public static MatchupAssessment Compare(ArmyStrengthSnapshot player, ArmyStrengthSnapshot enemy)
        {
            float ratio = enemy.EffectiveTotal <= 0
                ? 1f
                : player.EffectiveTotal / (float)enemy.EffectiveTotal;

            return new MatchupAssessment
            {
                Player = player,
                Enemy = enemy,
                Ratio = ratio,
                Label = ResolveLabel(ratio)
            };
        }

        public static MatchupLabel ResolveLabel(float ratio)
        {
            if (ratio >= CombatStrengthConfig.FavorableRatioThreshold)
                return MatchupLabel.Favorable;
            if (ratio < CombatStrengthConfig.DangerousRatioThreshold)
                return MatchupLabel.Dangerous;
            return MatchupLabel.Even;
        }

        public static string FormatLabel(MatchupLabel label) => label switch
        {
            MatchupLabel.Favorable => "Favorable",
            MatchupLabel.Even => "Even",
            MatchupLabel.Dangerous => "Dangerous",
            _ => "Even"
        };
    }
}
