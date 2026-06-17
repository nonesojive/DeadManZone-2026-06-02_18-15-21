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

            var tier = piece != null ? piece.movementSpeed : MovementSpeedTier.Medium;
            return ResolveWorldSpeed(tier, config.cellWidth, config.moveSpeedPresentationScale);
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
