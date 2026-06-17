using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Data;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>
    /// Maps sim movement charge pacing to world units/sec so units walk continuously
    /// between sparse grid-step events instead of lerping one cell then idling.
    /// </summary>
    public static class CombatArenaMoveSpeedResolver
    {
        public static float ResolveWorldSpeed(PieceDefinitionSO piece, CombatArenaConfigSO config)
        {
            if (config == null)
                return 2f;

            if (config.useTopTroopsFreeChaseMovement)
            {
                var tier = piece != null ? piece.movementSpeed : MovementSpeedTier.Medium;
                return ResolveFreeChaseSpeed(tier, config);
            }

            var movementTier = piece != null ? piece.movementSpeed : MovementSpeedTier.Medium;
            return ResolveWorldSpeed(movementTier, config.cellWidth, config.moveSpeedPresentationScale);
        }

        public static float ResolveWorldSpeed(MovementSpeedTier tier, CombatArenaConfigSO config)
        {
            if (config == null)
                return ResolveWorldSpeed(tier, 1.8f, 1f);

            if (config.useTopTroopsFreeChaseMovement)
                return ResolveFreeChaseSpeed(tier, config);

            return ResolveWorldSpeed(tier, config.cellWidth, config.moveSpeedPresentationScale);
        }

        private static float ResolveFreeChaseSpeed(MovementSpeedTier tier, CombatArenaConfigSO config)
        {
            float cellWidth = config.cellWidth > 0f ? config.cellWidth : 1.8f;
            float presentationScale = config.moveSpeedPresentationScale > 0f
                ? config.moveSpeedPresentationScale
                : 1f;
            float boost = config.topTroopsChaseSpeedMultiplier > 0f
                ? config.topTroopsChaseSpeedMultiplier
                : 1.2f;

            float simMatchedSpeed = ResolveWorldSpeed(tier, cellWidth, presentationScale);
            return simMatchedSpeed * boost;
        }

        public static float ResolveWorldSpeed(
            MovementSpeedTier tier,
            float cellWidth,
            float presentationScale)
        {
            if (tier == MovementSpeedTier.None || cellWidth <= 0f)
                return 0f;

            float secondsPerCell = CombatMovementSpeed.NormalStepChargeCost
                / (float)CombatMovementSpeed.GetChargePerTick(tier)
                / CombatPacingConfig.TicksPerSecond;

            if (secondsPerCell <= 0f)
                return 0f;

            float scale = presentationScale > 0f ? presentationScale : 1f;
            return cellWidth / secondsPerCell * scale;
        }
    }
}
