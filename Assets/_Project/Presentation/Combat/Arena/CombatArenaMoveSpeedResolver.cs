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

            int movementSpeed = piece != null ? piece.movementSpeed : 2;

            if (config.useTopTroopsFreeChaseMovement)
                return ResolveFreeChaseSpeed(movementSpeed, config);

            return ResolveWorldSpeed(movementSpeed, config.cellWidth, config.moveSpeedPresentationScale);
        }

        public static float ResolveWorldSpeed(int movementSpeed, CombatArenaConfigSO config)
        {
            if (config == null)
                return ResolveWorldSpeed(movementSpeed, 1.8f, 1f);

            if (config.useTopTroopsFreeChaseMovement)
                return ResolveFreeChaseSpeed(movementSpeed, config);

            return ResolveWorldSpeed(movementSpeed, config.cellWidth, config.moveSpeedPresentationScale);
        }

        private static float ResolveFreeChaseSpeed(int movementSpeed, CombatArenaConfigSO config)
        {
            float cellWidth = config.cellWidth > 0f ? config.cellWidth : 1.8f;
            float presentationScale = config.moveSpeedPresentationScale > 0f
                ? config.moveSpeedPresentationScale
                : 1f;
            float boost = config.topTroopsChaseSpeedMultiplier > 0f
                ? config.topTroopsChaseSpeedMultiplier
                : 1.2f;

            float simMatchedSpeed = ResolveWorldSpeed(movementSpeed, cellWidth, presentationScale);
            return simMatchedSpeed * boost;
        }

        public static float ResolveWorldSpeed(
            int movementSpeed,
            float cellWidth,
            float presentationScale)
        {
            if (movementSpeed <= 0 || cellWidth <= 0f)
                return 0f;

            float secondsPerCell = CombatMovementSpeed.NormalStepChargeCost
                / (float)CombatMovementSpeed.GetChargePerTick(movementSpeed)
                / CombatPacingConfig.TicksPerSecond;

            if (secondsPerCell <= 0f)
                return 0f;

            float scale = presentationScale > 0f ? presentationScale : 1f;
            return cellWidth / secondsPerCell * scale;
        }
    }
}
