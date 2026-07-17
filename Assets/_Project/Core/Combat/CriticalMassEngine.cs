using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    /// <summary>Board-wide tag thresholds that grant team-wide bonuses at fight start.</summary>
    public static class CriticalMassEngine
    {
        public static CriticalMassSnapshot Evaluate(BoardState board) =>
            Evaluate(board, CriticalMassRuleSource.GetRules());

        public static CriticalMassSnapshot Evaluate(BuildBoardSet boards) =>
            Evaluate(boards?.ToAggregateBoard(), CriticalMassRuleSource.GetRules());

        public static CriticalMassSnapshot Evaluate(
            BoardState board,
            IReadOnlyList<CriticalMassRuleDefinition> rules)
        {
            if (board == null || rules == null || rules.Count == 0)
                return CriticalMassSnapshot.Empty;

            var evaluatedRules = new List<EvaluatedCriticalMassRule>(rules.Count);
            var modifiersById = new Dictionary<string, CriticalMassCombatModifiers>(StringComparer.Ordinal);
            int authorityBonus = 0;
            int suppliesFlatBonus = 0;
            int suppliesPercentBonus = 0;

            foreach (var piece in board.Pieces)
                modifiersById[piece.InstanceId] = default;

            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (string.IsNullOrWhiteSpace(rule.CountTagId) || rule.Tiers == null || rule.Tiers.Length == 0)
                    continue;

                int count = CountMatchingPieces(board, rule);
                if (!TryResolveTier(count, rule.Tiers, out int tierIndex, out CriticalMassTier tier))
                {
                    int nearMissThreshold = rule.Tiers[0].Threshold;
                    if (count >= nearMissThreshold - 1 && count > 0)
                    {
                        evaluatedRules.Add(new EvaluatedCriticalMassRule
                        {
                            Rule = rule,
                            Count = count,
                            ActiveTierIndex = -1,
                            ActiveTier = default
                        });
                    }

                    continue;
                }

                evaluatedRules.Add(new EvaluatedCriticalMassRule
                {
                    Rule = rule,
                    Count = count,
                    ActiveTierIndex = tierIndex,
                    ActiveTier = tier
                });

                if (rule.Scope == CriticalMassScope.RunResources)
                {
                    ApplyRunResourceBonus(rule, tier.Magnitude, ref authorityBonus, ref suppliesFlatBonus, ref suppliesPercentBonus);
                    continue;
                }

                ApplyCombatRule(board, rule, tier.Magnitude, modifiersById);
            }

            return new CriticalMassSnapshot(
                evaluatedRules,
                authorityBonus,
                suppliesFlatBonus,
                suppliesPercentBonus,
                modifiersById);
        }

        public static void ApplyToCombatants(
            CriticalMassSnapshot snapshot,
            IList<CombatantState> combatants)
        {
            if (snapshot == null || combatants == null)
                return;

            for (int i = 0; i < combatants.Count; i++)
            {
                var combatant = combatants[i];
                if (combatant == null || !combatant.IsAlive)
                    continue;

                if (!snapshot.ModifiersByInstanceId.TryGetValue(combatant.InstanceId, out var mods)
                    || mods.IsEmpty)
                {
                    continue;
                }

                int maxHp = combatant.Definition.MaxHp + mods.MaxHpFlat;
                if (mods.MaxHpPercent != 0)
                    maxHp += (int)System.Math.Round(maxHp * (mods.MaxHpPercent / 100f));

                combatant.CurrentHp = maxHp;
                combatant.DamageBonus += mods.DamageFlat;
                combatant.DamagePercentBonus += mods.DamagePercent;
                combatant.AccuracyPercentBonus += mods.AccuracyPercent;
                combatant.AttackSpeedSteps += mods.AttackSpeedSteps;
                combatant.AttackRangeSteps += mods.AttackRangeSteps;
                combatant.MoveChargePercentBonus += mods.MoveChargePercentBonus;

                if (mods.MaxMoraleFlat != 0 && combatant.Definition.MaxMorale > 0)
                {
                    int maxMorale = combatant.Definition.MaxMorale + mods.MaxMoraleFlat;
                    combatant.CurrentMorale = System.Math.Max(0, maxMorale);
                }

                combatant.SuppressionDurationBonusTicks += mods.SuppressionDurationTicksBonus;
                combatant.LowStateDamageBonusPercentFromCM += mods.LowStateDamageBonusPercent;
            }
        }

        public static int ResolveEffectiveMaxHp(PieceDefinition definition, CriticalMassCombatModifiers mods)
        {
            int maxHp = definition.MaxHp + mods.MaxHpFlat;
            if (mods.MaxHpPercent != 0)
                maxHp += (int)System.Math.Round(maxHp * (mods.MaxHpPercent / 100f));

            return maxHp;
        }

        public static bool TryResolveTier(
            int count,
            IReadOnlyList<CriticalMassTier> tiers,
            out int tierIndex,
            out CriticalMassTier tier)
        {
            tierIndex = -1;
            tier = default;
            if (tiers == null || tiers.Count == 0)
                return false;

            int bestThreshold = -1;
            for (int i = 0; i < tiers.Count; i++)
            {
                if (count < tiers[i].Threshold || tiers[i].Threshold <= bestThreshold)
                    continue;

                bestThreshold = tiers[i].Threshold;
                tierIndex = i;
                tier = tiers[i];
            }

            return tierIndex >= 0;
        }

        private static void ApplyRunResourceBonus(
            CriticalMassRuleDefinition rule,
            int magnitude,
            ref int authorityBonus,
            ref int suppliesFlatBonus,
            ref int suppliesPercentBonus)
        {
            switch (rule.Stat)
            {
                case CriticalMassStat.Authority when rule.ModType == SynergyModType.Flat:
                    authorityBonus += magnitude;
                    break;
                case CriticalMassStat.Supplies when rule.ModType == SynergyModType.Flat:
                    suppliesFlatBonus += magnitude;
                    break;
                case CriticalMassStat.Supplies when rule.ModType == SynergyModType.Percent:
                    suppliesPercentBonus += magnitude;
                    break;
            }
        }

        private static void ApplyCombatRule(
            BoardState board,
            CriticalMassRuleDefinition rule,
            int magnitude,
            Dictionary<string, CriticalMassCombatModifiers> modifiersById)
        {
            foreach (var piece in board.Pieces)
            {
                if (piece.Definition == null || !rule.Target.Matches(piece.Definition))
                    continue;

                if (rule.Target.RequireSalvage && !OffFactionRules.IsSalvage(piece, rule.CountTagId))
                    continue;

                if (!modifiersById.TryGetValue(piece.InstanceId, out var mods))
                    mods = default;

                mods.Add(rule.Stat, rule.ModType, magnitude);
                modifiersById[piece.InstanceId] = mods;
            }
        }

        private static int CountMatchingPieces(BoardState board, CriticalMassRuleDefinition rule)
        {
            // SalvageForFaction's CountTagId is "the faction whose salvage-count identity rule
            // this is" (e.g. dust_scourge), NOT "the piece's own faction" — OffFactionRules
            // .IsSalvage(piece, playerFactionId) treats that id as the ACTUAL PLAYER'S faction
            // and returns true for every piece that ISN'T it. Fired unguarded, this rule would
            // trigger on ANY board with >=2 non-Dust, non-neutral, non-mercenary pieces — i.e.
            // virtually every board in the game, including every other faction's own runs and
            // every plain IronMarch test fixture. Gate it: only count/apply salvage-for-X when
            // the board actually has an X piece on it (a cheap, parameter-free proxy for "the
            // player is actually running faction X", since that faction's own purchases are the
            // only way its pieces land on a board at all).
            if (rule.CountCategory == CriticalMassCountCategory.SalvageForFaction
                && !BoardHasFactionPiece(board, rule.CountTagId))
            {
                return 0;
            }

            int count = 0;
            foreach (var piece in board.Pieces)
            {
                if (piece.Definition == null)
                    continue;

                if (MatchesCountTag(piece, rule.CountTagId, rule.CountCategory))
                    count++;
            }

            return count;
        }

        private static bool BoardHasFactionPiece(BoardState board, string factionId)
        {
            foreach (var piece in board.Pieces)
            {
                if (piece.Definition != null
                    && string.Equals(piece.Definition.FactionId, factionId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool MatchesCountTag(
            PlacedPiece piece,
            string tagId,
            CriticalMassCountCategory countCategory)
        {
            var definition = piece.Definition;
            return countCategory switch
            {
                CriticalMassCountCategory.Primary => PieceTagQueries.HasPrimaryTag(definition, tagId),
                CriticalMassCountCategory.CombatRole => PieceTagQueries.HasCombatRoleTag(definition, tagId),
                CriticalMassCountCategory.Synergy => PieceTagQueries.HasSynergyTag(definition, tagId),
                CriticalMassCountCategory.Ability => PieceTagQueries.HasAbilityTag(definition, tagId),
                CriticalMassCountCategory.Flavor => PieceTagQueries.HasFlavorTag(definition, tagId),
                CriticalMassCountCategory.AttackType => MatchesAttackType(definition, tagId),
                CriticalMassCountCategory.Faction => MatchesFaction(definition, tagId),
                CriticalMassCountCategory.SalvageForFaction => OffFactionRules.IsSalvage(piece, tagId),
                _ => false
            };
        }

        private static bool MatchesAttackType(PieceDefinition definition, string tagId)
        {
            if (!Enum.TryParse<AttackType>(tagId, ignoreCase: true, out var attackType))
                return false;

            return definition.AttackType == attackType;
        }

        private static bool MatchesFaction(PieceDefinition definition, string tagId) =>
            string.Equals(definition.FactionId, tagId, StringComparison.OrdinalIgnoreCase);
    }
}
