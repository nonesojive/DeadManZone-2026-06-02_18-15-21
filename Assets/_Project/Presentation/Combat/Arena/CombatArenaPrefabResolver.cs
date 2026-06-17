using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Resolves combat arena prefabs with Synty wrapper fallbacks when piece assets are unassigned.</summary>
    internal static class CombatArenaPrefabResolver
    {
        public static GameObject ResolveUnitPrefab(PieceDefinitionSO piece, CombatArenaConfigSO config)
        {
            if (config != null && config.useProceduralUnitVisuals)
                return null;

            if (piece?.combatArenaPrefab != null)
                return piece.combatArenaPrefab;

            return SyntyRuntimeAssetLoader.LoadPrefab(config?.fallbackUnitPrefabPath);
        }

        public static GameObject ResolveBuildingPrefab(
            PieceDefinitionSO piece,
            PieceDefinition definition,
            CombatArenaConfigSO config)
        {
            if (piece?.combatArenaPrefab != null)
                return piece.combatArenaPrefab;

            string path = config?.fallbackBuildingPrefabPath;
            if (definition != null && PieceTagQueries.HasTag(definition, GameTagIds.Hq))
                path = config?.fallbackHqPrefabPath ?? path;

            return SyntyRuntimeAssetLoader.LoadPrefab(path);
        }

        public static float ResolveUnitScale(PieceDefinitionSO piece, CombatArenaConfigSO config)
        {
            float scale = piece != null && piece.combatArenaModelScale > 0f
                ? piece.combatArenaModelScale
                : 1f;

            if (config != null && config.unitModelScaleMultiplier > 0f)
                scale *= config.unitModelScaleMultiplier;

            return scale;
        }

        public static float ResolveUnitHeight(PieceDefinitionSO piece, CombatArenaConfigSO config)
        {
            if (piece != null && piece.combatArenaModelHeight > 0f)
                return piece.combatArenaModelHeight;

            if (CombatAttackProfileResolver.IsVehicle(piece))
                return config != null && config.defaultVehicleModelHeight > 0f
                    ? config.defaultVehicleModelHeight
                    : 1.8f;

            return config != null && config.defaultUnitModelHeight > 0f
                ? config.defaultUnitModelHeight
                : 2.1f;
        }
    }
}
