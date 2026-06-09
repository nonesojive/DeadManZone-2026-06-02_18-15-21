using System;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;

namespace DeadManZone.Core.Tags
{
    public static class PieceCardViewModelBuilder
    {
        public static PieceCardViewModel Build(PieceDefinition piece, SynergyEngine.SynergyResult? synergy = null)
        {
            if (piece == null)
                throw new ArgumentNullException(nameof(piece));

            PieceTagQueries.PlayerVisibleTagsResult visibleTags =
                PieceTagQueries.GetPlayerVisibleTags(piece, maxOptionalChips: 4);

            return new PieceCardViewModel
            {
                DisplayName = piece.DisplayName ?? string.Empty,
                Hp = piece.MaxHp,
                BaseDamage = piece.BaseDamage,
                MovementSpeed = piece.MovementSpeed,
                AttackSpeed = piece.AttackSpeed,
                AttackType = piece.AttackType,
                ArmorType = piece.ArmorType,
                IdentityTags = visibleTags.IdentityTags,
                OptionalTags = visibleTags.OptionalTags,
                OverflowCount = visibleTags.OverflowCount,
                SynergyDamageBonus = synergy?.DamageBonus ?? 0,
                SynergyArmorBuffSteps = synergy?.ArmorBuffSteps ?? 0
            };
        }
    }
}
