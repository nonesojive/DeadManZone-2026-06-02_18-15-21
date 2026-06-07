using System;
using System.Collections.Generic;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public static class CombatRoleTargeting
    {
        public static CombatantState SelectTarget(CombatantState attacker, IReadOnlyList<CombatantState> inRangeEnemies)
        {
            if (attacker == null || attacker.Definition == null || inRangeEnemies == null || inRangeEnemies.Count == 0)
                return null;

            var aliveEnemies = new List<CombatantState>(inRangeEnemies.Count);
            for (int i = 0; i < inRangeEnemies.Count; i++)
            {
                var enemy = inRangeEnemies[i];
                if (enemy != null && enemy.IsAlive)
                {
                    aliveEnemies.Add(enemy);
                }
            }

            if (aliveEnemies.Count == 0)
                return null;

            return CombatRoleProfile.ResolveBias(attacker.Definition.CombatRole) switch
            {
                CombatRoleTargetingBias.NoAttack => null,
                CombatRoleTargetingBias.HighestHp => SelectHighestCurrentHp(PreferLineTroops(aliveEnemies)),
                CombatRoleTargetingBias.Furthest => SelectFurthest(attacker.Position, aliveEnemies),
                CombatRoleTargetingBias.NearestFront => SelectNearestFront(attacker.Position, aliveEnemies),
                CombatRoleTargetingBias.LowestMaxHpRearPreferred => SelectLowestMaxHpRearPreferred(aliveEnemies),
                _ => SelectFirstByInstanceId(aliveEnemies)
            };
        }

        /// <summary>Assault/sniper roles skip HQ and non-combatants when frontline targets exist.</summary>
        private static List<CombatantState> PreferLineTroops(IReadOnlyList<CombatantState> candidates)
        {
            var lineTroops = new List<CombatantState>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (!IsDeprioritizedTarget(candidate))
                    lineTroops.Add(candidate);
            }

            return lineTroops.Count > 0 ? lineTroops : new List<CombatantState>(candidates);
        }

        private static bool IsDeprioritizedTarget(CombatantState combatant)
        {
            if (combatant?.Definition == null)
                return true;

            if (combatant.HasTag(GameTagIds.Hq) || combatant.HasTag(GameTagIds.NonCombatant))
                return true;

            return CombatRoleProfile.ResolveBias(combatant.Definition.CombatRole) == CombatRoleTargetingBias.NoAttack;
        }

        private static CombatantState SelectFirstByInstanceId(IReadOnlyList<CombatantState> candidates)
        {
            CombatantState best = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (best == null || CompareByInstanceId(candidate, best) < 0)
                {
                    best = candidate;
                }
            }

            return best;
        }

        private static CombatantState SelectHighestCurrentHp(IReadOnlyList<CombatantState> candidates)
        {
            CombatantState best = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (best == null
                    || candidate.CurrentHp > best.CurrentHp
                    || (candidate.CurrentHp == best.CurrentHp && CompareByInstanceId(candidate, best) < 0))
                {
                    best = candidate;
                }
            }

            return best;
        }

        private static CombatantState SelectFurthest(GridCoord attackerPosition, IReadOnlyList<CombatantState> candidates)
        {
            CombatantState best = null;
            int bestDistance = int.MinValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                int distance = CombatRange.Manhattan(attackerPosition, candidate.Position);
                if (best == null
                    || distance > bestDistance
                    || (distance == bestDistance && CompareByInstanceId(candidate, best) < 0))
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static CombatantState SelectNearestFront(GridCoord attackerPosition, IReadOnlyList<CombatantState> candidates)
        {
            int frontColumn = GetFrontColumnX(candidates);
            var frontCandidates = new List<CombatantState>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate.Position.X == frontColumn)
                {
                    frontCandidates.Add(candidate);
                }
            }

            if (frontCandidates.Count > 0)
                return SelectNearest(attackerPosition, frontCandidates);

            return SelectNearest(attackerPosition, candidates);
        }

        private static CombatantState SelectNearest(GridCoord attackerPosition, IReadOnlyList<CombatantState> candidates)
        {
            CombatantState best = null;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                int distance = CombatRange.Manhattan(attackerPosition, candidate.Position);
                if (best == null
                    || distance < bestDistance
                    || (distance == bestDistance && CompareByInstanceId(candidate, best) < 0))
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static CombatantState SelectLowestMaxHpRearPreferred(IReadOnlyList<CombatantState> candidates)
        {
            int rearColumn = GetRearColumnX(candidates);
            CombatantState bestRear = null;
            CombatantState bestOverall = null;

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (IsBetterLowMaxHp(candidate, bestOverall))
                {
                    bestOverall = candidate;
                }

                if (candidate.Position.X == rearColumn && IsBetterLowMaxHp(candidate, bestRear))
                {
                    bestRear = candidate;
                }
            }

            return bestRear ?? bestOverall;
        }

        private static bool IsBetterLowMaxHp(CombatantState candidate, CombatantState currentBest)
        {
            if (candidate == null)
                return false;

            if (currentBest == null)
                return true;

            int candidateMaxHp = GetMaxHp(candidate);
            int currentBestMaxHp = GetMaxHp(currentBest);
            if (candidateMaxHp != currentBestMaxHp)
                return candidateMaxHp < currentBestMaxHp;

            return CompareByInstanceId(candidate, currentBest) < 0;
        }

        private static int GetFrontColumnX(IReadOnlyList<CombatantState> candidates)
        {
            CombatSide side = candidates[0].Side;
            int frontColumn = side == CombatSide.Player ? int.MinValue : int.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                int x = candidates[i].Position.X;
                if (side == CombatSide.Player)
                {
                    if (x > frontColumn)
                        frontColumn = x;
                }
                else
                {
                    if (x < frontColumn)
                        frontColumn = x;
                }
            }

            return frontColumn;
        }

        private static int GetRearColumnX(IReadOnlyList<CombatantState> candidates)
        {
            CombatSide side = candidates[0].Side;
            int rearColumn = side == CombatSide.Player ? int.MaxValue : int.MinValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                int x = candidates[i].Position.X;
                if (side == CombatSide.Player)
                {
                    if (x < rearColumn)
                        rearColumn = x;
                }
                else
                {
                    if (x > rearColumn)
                        rearColumn = x;
                }
            }

            return rearColumn;
        }

        private static int GetMaxHp(CombatantState combatant) => combatant.Definition?.MaxHp ?? int.MaxValue;

        private static int CompareByInstanceId(CombatantState left, CombatantState right) =>
            StringComparer.Ordinal.Compare(left?.InstanceId ?? string.Empty, right?.InstanceId ?? string.Empty);
    }
}
