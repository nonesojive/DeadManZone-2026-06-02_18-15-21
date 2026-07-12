using System;
using System.Runtime.CompilerServices;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Data;

[assembly: InternalsVisibleTo("DeadManZone.Presentation.Tests")]

namespace DeadManZone.Presentation.Combat.Arena
{
    internal static class CombatAttackProfileResolver
    {
        public static CombatAttackPresentationProfile Resolve(PieceDefinitionSO piece)
        {
            if (piece == null)
                return CombatAttackPresentationProfile.InfantryRifle;

            if (piece.grantedAbility == GrantedAbility.MortarShot
                || piece.attackType == AttackType.Explosive)
                return CombatAttackPresentationProfile.InfantryGrenade;

            if (piece.attackType == AttackType.Melee)
                return CombatAttackPresentationProfile.InfantryMelee;

            if (IsBuilding(piece))
                return CombatAttackPresentationProfile.BuildingArtillery;

            if (IsVehicle(piece))
                return CombatAttackPresentationProfile.VehicleCannon;

            return CombatAttackPresentationProfile.InfantryRifle;
        }

        internal static bool IsVehicle(PieceDefinitionSO piece) =>
            piece != null
            && string.Equals(piece.primary, GameTagIds.Vehicle, StringComparison.OrdinalIgnoreCase);

        internal static bool IsBuilding(PieceDefinitionSO piece) =>
            piece != null && (
                piece.category == PieceCategory.Building
                || string.Equals(piece.primary, GameTagIds.Building, StringComparison.OrdinalIgnoreCase));
    }
}
