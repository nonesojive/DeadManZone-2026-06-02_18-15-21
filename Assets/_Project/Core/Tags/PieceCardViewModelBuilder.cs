using System;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;

namespace DeadManZone.Core.Tags
{
    public static class PieceCardViewModelBuilder
    {
        public static PieceCardViewModel Build(PieceDefinition piece, PieceCardBuildContext context = null)
        {
            if (piece == null)
                throw new ArgumentNullException(nameof(piece));

            PieceTagQueries.PlayerVisibleTagsResult visibleTags =
                PieceTagQueries.GetPlayerVisibleTags(piece, maxOptionalChips: 4);

            SynergyEngine.SynergyResult? synergy = context?.Synergy;
            var attackProfile = AttackTypeProfileCatalog.Get(piece.AttackType);

            return new PieceCardViewModel
            {
                DisplayName = piece.DisplayName ?? string.Empty,
                Hp = piece.MaxHp,
                BaseDamage = piece.BaseDamage,
                MovementSpeed = piece.MovementSpeed,
                MovementSpeedValue = CombatMovementSpeed.GetChargePerTick(piece.MovementSpeed),
                AttackSpeed = piece.AttackSpeed,
                AttackSpeedValue = CombatAttackSpeed.GetEffectiveCooldown(
                    piece.CooldownTicks,
                    piece.AttackSpeed),
                AttackType = piece.AttackType,
                ArmorType = piece.ArmorType,
                PrimaryTag = PieceCardTagLayout.ResolvePrimaryTag(visibleTags),
                CombatRoleTag = PieceCardTagLayout.ResolveCombatRoleTag(visibleTags),
                IdentityTags = visibleTags.IdentityTags,
                OptionalTags = visibleTags.OptionalTags,
                ChipTags = PieceCardTagLayout.BuildChipTags(visibleTags),
                OverflowCount = visibleTags.OverflowCount,                SynergyDamageBonus = synergy?.DamageBonus ?? 0,
                SynergyArmorBuffSteps = synergy?.ArmorBuffSteps ?? 0,
                SynergyMoveChargeBonus = synergy?.MoveChargeBonus ?? 0,
                SynergyLines = PieceCardTooltipFormatter.BuildSynergyLines(
                    context?.SynergySnapshot,
                    context?.Board,
                    context?.InstanceId),
                SalvageContext = PieceCardTooltipFormatter.BuildSalvageContext(
                    context?.IsSalvaged ?? false,
                    context?.LastEnemyFactionId,
                    context?.LastEnemyFactionDisplayName),
                AttackTypeTooltip = attackProfile?.Tooltip ?? string.Empty,
                ArmorTypeTooltip = ArmorTypeTooltipCatalog.GetTooltip(piece.ArmorType),
                AbilityText = PieceCardTooltipFormatter.BuildAbilityText(piece.GrantedAbility)
            };
        }

        public static PieceCardViewModel Build(PieceDefinition piece, SynergyEngine.SynergyResult? synergy)
        {
            if (!synergy.HasValue)
                return Build(piece);

            return Build(piece, new PieceCardBuildContext { Synergy = synergy });
        }
    }
}
