using System;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    internal static class CombatUnit2DVisualScale
    {
        // Bumped again per art review — bolder unit presence on the field.
        private const float InfantryHeight = 1.85f;
        private const float HeavyInfantryHeight = 2.05f;
        private const float StructureHeight = 2.0f;
        private const float VehicleHeight = 2.15f;

        public static float ResolveUniformScale(PieceDefinitionSO piece, Sprite sprite)
        {
            if (sprite == null || sprite.pixelsPerUnit <= 0f || sprite.rect.height <= 0f)
                return 1f;

            float sourceHeight = CombatArena2DSpriteMetrics.VisibleHeightUnits(sprite);
            if (sourceHeight <= 0f)
                return 1f;

            return Mathf.Clamp(TargetHeight(piece) / sourceHeight, 0.2f, 3f);
        }

        private static float TargetHeight(PieceDefinitionSO piece)
        {
            if (piece == null || string.IsNullOrWhiteSpace(piece.id))
                return InfantryHeight;

            return piece.id switch
            {
                "armored_transport" => VehicleHeight,
                "ironmarch_iron_horse" => 2.35f,
                "ironclad_mortars" => 2.1f,
                "machine_gun_nest" => StructureHeight,
                "bulwark_squad" => HeavyInfantryHeight,
                "ironclad_field_marshal" => HeavyInfantryHeight,
                _ when IsVehicle(piece) => VehicleHeight,
                _ when IsStructure(piece) => StructureHeight,
                _ => InfantryHeight
            };
        }

        private static bool IsVehicle(PieceDefinitionSO piece) =>
            string.Equals(piece.primary, "vehicle", StringComparison.OrdinalIgnoreCase);

        private static bool IsStructure(PieceDefinitionSO piece) =>
            string.Equals(piece.primary, "structure", StringComparison.OrdinalIgnoreCase)
            || string.Equals(piece.primary, "building", StringComparison.OrdinalIgnoreCase);
    }
}
