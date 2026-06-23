using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Resolves combat unit sprite: combatArenaSprite → role silhouette → icon → procedural.</summary>
    public static class CombatUnitSpriteResolver
    {
        public const int MinIconPixelSize = 48;

        public static Sprite Resolve(PieceDefinitionSO piece, CombatSide side)
        {
            if (piece == null)
                return CombatArena2DPlaceholderSprites.DefaultSilhouette;

            if (piece.combatArenaSprite != null)
                return piece.combatArenaSprite;

            if (IsBuilding(piece))
            {
                if (piece.icon != null && IconReadsAtCombatScale(piece.icon))
                    return piece.icon;
                return CombatArena2DPlaceholderSprites.DefaultSilhouette;
            }

            // ponytail: shared role silhouettes beat shop icons until per-piece combatArenaSprite lands
            var roleSprite = CombatArena2DSilhouetteArt.ForRole(MapRole(piece));
            if (roleSprite != null)
                return roleSprite;

            if (piece.icon != null && IconReadsAtCombatScale(piece.icon))
                return piece.icon;

            return CombatArena2DPlaceholderSprites.ForRole(piece, side, piece.categoryTint);
        }

        public static Color ResolveTint(PieceDefinitionSO piece, CombatSide side)
        {
            if (piece == null)
                return Color.white;

            if (IsBuilding(piece))
            {
                if (piece.categoryTint != Color.white)
                    return piece.categoryTint;
                return Color.white;
            }

            // Neutral silhouettes read best with a light side wash, not full category multiply.
            if (CombatArena2DSilhouetteArt.ForRole(MapRole(piece)) != null
                && piece.combatArenaSprite == null)
            {
                return side == CombatSide.Player
                    ? new Color(0.92f, 0.88f, 0.82f)
                    : new Color(0.78f, 0.74f, 0.70f);
            }

            if (piece.categoryTint != Color.white)
                return piece.categoryTint;

            return side == CombatSide.Player
                ? new Color(0.85f, 0.78f, 0.65f)
                : new Color(0.72f, 0.68f, 0.62f);
        }

        private static bool IsBuilding(PieceDefinitionSO piece)
        {
            if (piece == null)
                return false;

            if (piece.category is PieceCategory.Building or PieceCategory.Hybrid)
                return true;

            if (piece.tags == null)
                return false;

            for (int i = 0; i < piece.tags.Length; i++)
            {
                string tag = piece.tags[i];
                if (string.IsNullOrEmpty(tag))
                    continue;

                if (tag.Equals("Building", System.StringComparison.OrdinalIgnoreCase)
                    || tag.Equals("HQ", System.StringComparison.OrdinalIgnoreCase)
                    || tag.Equals("Headquarters", System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static bool IconReadsAtCombatScale(Sprite icon)
        {
            if (icon == null || icon.texture == null)
                return false;

            var rect = icon.rect;
            return rect.width >= MinIconPixelSize && rect.height >= MinIconPixelSize;
        }

        public static CombatArena2DSilhouetteRole MapRole(PieceDefinitionSO piece)
        {
            if (piece == null)
                return CombatArena2DSilhouetteRole.Generic;

            if (piece.combatRole == GameTagIds.Artillery)
                return CombatArena2DSilhouetteRole.Artillery;

            if (piece.combatRole == GameTagIds.Sniper || piece.combatRole == GameTagIds.Support)
                return CombatArena2DSilhouetteRole.Ranged;

            if (piece.combatRole == GameTagIds.Assault || piece.combatRole == GameTagIds.Defender)
                return CombatArena2DSilhouetteRole.Assault;

            if (CombatAttackProfileResolver.IsVehicle(piece))
                return CombatArena2DSilhouetteRole.Vehicle;

            return CombatArena2DSilhouetteRole.Generic;
        }
    }

    public enum CombatArena2DSilhouetteRole
    {
        Generic,
        Assault,
        Ranged,
        Artillery,
        Vehicle
    }
}
