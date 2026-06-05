using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    public static class CombatAbilityExecutor
    {
        public const int GrenadeLobDamage = 30;
        public const int CannonBlastPrimaryDamage = 50;
        public const int CannonBlastSplashDamage = 25;

        public static bool CanUseAtPause(GrantedAbility ability, CombatPhase completedPhase) =>
            ability switch
            {
                GrantedAbility.CannonBlast => completedPhase == CombatPhase.Grind,
                _ => completedPhase == CombatPhase.Deployment || completedPhase == CombatPhase.Grind
            };

        public static int GetAuthorityCost(GrantedAbility ability, CombatPhase completedPhase) =>
            ability switch
            {
                GrantedAbility.GrenadeLob when completedPhase == CombatPhase.Deployment => 2,
                GrantedAbility.GrenadeLob => 3,
                GrantedAbility.ShieldAllies => 2,
                GrantedAbility.CannonBlast => 4,
                _ => 0
            };

        public static CommandResult Execute(
            GrantedAbility ability,
            string sourcePieceId,
            BoardState board,
            IList<CombatantState> playerCombatants,
            IList<CombatantState> enemyCombatants,
            CombatEventLog log,
            CombatPhase phase,
            GridCoord? targetCell = null)
        {
            var sourceCombatant = playerCombatants.FirstOrDefault(c =>
                c.InstanceId == sourcePieceId && c.IsAlive);
            if (sourceCombatant == null)
                return CommandResult.Fail("Ability source not alive");

            if (sourceCombatant.Definition.GrantedAbility != ability)
                return CommandResult.Fail("Source cannot grant ability");

            switch (ability)
            {
                case GrantedAbility.GrenadeLob:
                    return ExecuteGrenadeLob(sourceCombatant, enemyCombatants, log, phase, targetCell);
                case GrantedAbility.ShieldAllies:
                    return ExecuteShieldAllies(sourceCombatant, playerCombatants, log, phase);
                case GrantedAbility.CannonBlast:
                    return ExecuteCannonBlast(sourceCombatant, enemyCombatants, log, phase, targetCell);
                default:
                    return CommandResult.Fail("Unknown ability");
            }
        }

        private static CommandResult ExecuteGrenadeLob(
            CombatantState source,
            IList<CombatantState> enemies,
            CombatEventLog log,
            CombatPhase phase,
            GridCoord? targetCell)
        {
            var target = ResolveTargetCell(enemies, targetCell);
            if (target == null)
                return CommandResult.Fail("No valid grenade target");

            ApplyAreaDamage(
                source,
                enemies,
                target.Value,
                radius: 1,
                GrenadeLobDamage,
                AttackType.Explosive,
                log,
                phase,
                "grenade_lob");
            return CommandResult.Ok();
        }

        private static CommandResult ExecuteShieldAllies(
            CombatantState source,
            IList<CombatantState> allies,
            CombatEventLog log,
            CombatPhase phase)
        {
            foreach (var ally in allies.Where(a => a.IsAlive && IsAdjacent(source.Position, a.Position)))
            {
                if (!HasInfantryTag(ally.Definition))
                    continue;

                ally.ArmorBuffSteps += 1;
                log.Append(phase, tick: -1, source.InstanceId, "shield_allies", ally.InstanceId, 1);
            }

            return CommandResult.Ok();
        }

        private static CommandResult ExecuteCannonBlast(
            CombatantState source,
            IList<CombatantState> enemies,
            CombatEventLog log,
            CombatPhase phase,
            GridCoord? targetCell)
        {
            var primary = ResolvePrimaryTarget(enemies, targetCell);
            if (primary == null)
                return CommandResult.Fail("No valid cannon target");

            ApplyDamage(source, primary, CannonBlastPrimaryDamage, AttackType.Explosive, log, phase, "cannon_blast");
            foreach (var splash in enemies.Where(e => e.IsAlive && e.InstanceId != primary.InstanceId && IsAdjacent(primary.Position, e.Position)))
                ApplyDamage(source, splash, CannonBlastSplashDamage, AttackType.Explosive, log, phase, "cannon_blast_splash");

            return CommandResult.Ok();
        }

        private static GridCoord? ResolveTargetCell(IList<CombatantState> enemies, GridCoord? targetCell)
        {
            if (targetCell.HasValue &&
                enemies.Any(e => e.IsAlive && OccupiesCell(e, targetCell.Value)))
                return targetCell;

            return enemies.Where(e => e.IsAlive).OrderBy(e => e.Position.X).ThenBy(e => e.InstanceId).FirstOrDefault()?.Position;
        }

        private static CombatantState ResolvePrimaryTarget(IList<CombatantState> enemies, GridCoord? targetCell)
        {
            if (targetCell.HasValue)
            {
                var atCell = enemies.FirstOrDefault(e => e.IsAlive && OccupiesCell(e, targetCell.Value));
                if (atCell != null)
                    return atCell;
            }

            return enemies.Where(e => e.IsAlive).OrderBy(e => e.CurrentHp).ThenBy(e => e.InstanceId).FirstOrDefault();
        }

        private static void ApplyAreaDamage(
            CombatantState source,
            IList<CombatantState> targets,
            GridCoord center,
            int radius,
            int baseDamage,
            AttackType attackType,
            CombatEventLog log,
            CombatPhase phase,
            string actionType)
        {
            foreach (var target in targets.Where(t => t.IsAlive && Manhattan(center, t.Position) <= radius))
            {
                var tempAttacker = new PieceDefinition
                {
                    Id = source.Definition.Id,
                    BaseDamage = baseDamage,
                    AttackType = attackType
                };
                int damage = CombatDamageResolver.ComputeDamage(tempAttacker, target.Definition, 1f, target.ArmorBuffSteps);
                target.CurrentHp -= damage;
                source.DamageDealtThisFight += damage;
                target.DamageTakenThisFight += damage;
                log.Append(phase, tick: -1, source.InstanceId, actionType, target.InstanceId, damage);
                if (!target.IsAlive)
                    log.Append(phase, tick: -1, target.InstanceId, "destroyed", source.InstanceId, 0);
            }
        }

        private static void ApplyDamage(
            CombatantState source,
            CombatantState target,
            int baseDamage,
            AttackType attackType,
            CombatEventLog log,
            CombatPhase phase,
            string actionType)
        {
            var tempAttacker = new PieceDefinition
            {
                Id = source.Definition.Id,
                BaseDamage = baseDamage,
                AttackType = attackType
            };
            int damage = CombatDamageResolver.ComputeDamage(tempAttacker, target.Definition, 1f, target.ArmorBuffSteps);
            target.CurrentHp -= damage;
            source.DamageDealtThisFight += damage;
            target.DamageTakenThisFight += damage;
            log.Append(phase, tick: -1, source.InstanceId, actionType, target.InstanceId, damage);
            if (!target.IsAlive)
                log.Append(phase, tick: -1, target.InstanceId, "destroyed", source.InstanceId, 0);
        }

        private static bool HasInfantryTag(PieceDefinition definition) =>
            definition.Tags?.Any(t => string.Equals(t, "infantry", System.StringComparison.OrdinalIgnoreCase)) == true;

        private static bool IsAdjacent(GridCoord a, GridCoord b) => Manhattan(a, b) == 1;

        private static bool OccupiesCell(CombatantState combatant, GridCoord cell) =>
            combatant.Position.Equals(cell);

        private static int Manhattan(GridCoord a, GridCoord b) =>
            System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Y - b.Y);
    }
}
