using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Combat
{
    public static class CombatAbilityExecutor
    {
        public const int MortarShotDamage = 30;
        public const int CannonBlastPrimaryDamage = 50;
        public const int CannonBlastSplashDamage = 25;

        // PROVISIONAL — 2026-07-15 faction-roster-v1 §2.2 Grand Battery's Rolling Barrage:
        // bigger radius than MortarShot, damage scales with the army's artillery-tag count
        // (TickCombatRun._playerArtilleryCount, computed once at fight start).
        public const int RollingBarrageBaseDamage = 40;
        public const int RollingBarragePerArtilleryDamage = 8;
        public const int RollingBarrageRadius = 2;

        public static bool CanUseAtPause(GrantedAbility ability, int checkpointIndex) =>
            ability switch
            {
                GrantedAbility.CannonBlast => checkpointIndex == 1,
                _ => checkpointIndex == 0 || checkpointIndex == 1
            };

        public static int GetAuthorityCost(GrantedAbility ability, int checkpointIndex) =>
            ability switch
            {
                GrantedAbility.MortarShot when checkpointIndex == 0 => 2,
                GrantedAbility.MortarShot => 3,
                GrantedAbility.ShieldAllies => 2,
                GrantedAbility.CannonBlast => 4,
                GrantedAbility.RollingBarrage when checkpointIndex == 0 => 3,
                GrantedAbility.RollingBarrage => 4,
                // Echo (2026-07-15 faction-roster-v1 §2.6): "repeat the last order/tactic
                // issued this fight, free." CommandProcessor handles Echo's own dispatch
                // separately (it never reaches Execute() directly), but keep the cost table
                // honest for anything that reads it (e.g. GetAvailableCommands pricing).
                GrantedAbility.Echo => 0,
                _ => 0
            };

        /// <param name="hqBoard">2026-07-15 faction-roster-v1 §4 (🟡 ledger): HQ-board buildings
        /// (Artillery Park) can grant pause-window abilities too. HQ pieces are never spawned as
        /// combatants, so a source not found in <paramref name="playerCombatants"/> is looked up
        /// here instead and treated as always-active (buildings can't be attacked off-board).</param>
        /// <param name="artilleryCount">Army-wide artillery-tag count at fight start, read
        /// directly per the roster spec's "tactic-scaling may read counts directly" (§3) —
        /// consumed by RollingBarrage only.</param>
        public static CommandResult Execute(
            GrantedAbility ability,
            string sourcePieceId,
            BoardState board,
            IList<CombatantState> playerCombatants,
            IList<CombatantState> enemyCombatants,
            CombatEventLog log,
            int logSegment,
            int logTick,
            GridCoord? targetCell = null,
            BoardState hqBoard = null,
            int artilleryCount = 0)
        {
            var sourceCombatant = playerCombatants.FirstOrDefault(c =>
                c.InstanceId == sourcePieceId && c.IsActive);

            CombatantState sourceForAbility;
            if (sourceCombatant != null)
            {
                if (sourceCombatant.Definition.GrantedAbility != ability)
                    return CommandResult.Fail("Source cannot grant ability");

                sourceForAbility = sourceCombatant;
            }
            else
            {
                var hqPiece = hqBoard?.Pieces.FirstOrDefault(p => p.InstanceId == sourcePieceId);
                if (hqPiece == null || hqPiece.Definition.GrantedAbility != ability)
                    return CommandResult.Fail("Ability source not alive");

                sourceForAbility = new CombatantState
                {
                    InstanceId = hqPiece.InstanceId,
                    Side = CombatSide.Player,
                    Definition = hqPiece.Definition,
                    CurrentHp = 1
                };
            }

            switch (ability)
            {
                case GrantedAbility.MortarShot:
                    return ExecuteMortarShot(sourceForAbility, enemyCombatants, log, logSegment, logTick, targetCell);
                case GrantedAbility.ShieldAllies:
                    return ExecuteShieldAllies(sourceForAbility, playerCombatants, log, logSegment, logTick);
                case GrantedAbility.CannonBlast:
                    return ExecuteCannonBlast(sourceForAbility, enemyCombatants, log, logSegment, logTick, targetCell);
                case GrantedAbility.RollingBarrage:
                    return ExecuteRollingBarrage(sourceForAbility, enemyCombatants, log, logSegment, logTick, targetCell, artilleryCount);
                default:
                    return CommandResult.Fail("Unknown ability");
            }
        }

        private static CommandResult ExecuteMortarShot(
            CombatantState source,
            IList<CombatantState> enemies,
            CombatEventLog log,
            int logSegment,
            int logTick,
            GridCoord? targetCell)
        {
            var target = ResolveTargetCell(enemies, targetCell);
            if (target == null)
                return CommandResult.Fail("No valid mortar target");

            ApplyAreaDamage(
                source,
                enemies,
                target.Value,
                radius: 1,
                MortarShotDamage,
                AttackType.Explosive,
                log,
                logSegment,
                logTick,
                "mortar_shot");
            return CommandResult.Ok();
        }

        private static CommandResult ExecuteRollingBarrage(
            CombatantState source,
            IList<CombatantState> enemies,
            CombatEventLog log,
            int logSegment,
            int logTick,
            GridCoord? targetCell,
            int artilleryCount)
        {
            var target = ResolveTargetCell(enemies, targetCell);
            if (target == null)
                return CommandResult.Fail("No valid rolling barrage target");

            int damage = RollingBarrageBaseDamage + System.Math.Max(0, artilleryCount) * RollingBarragePerArtilleryDamage;
            ApplyAreaDamage(
                source,
                enemies,
                target.Value,
                radius: RollingBarrageRadius,
                damage,
                AttackType.Explosive,
                log,
                logSegment,
                logTick,
                "rolling_barrage");
            return CommandResult.Ok();
        }

        private static CommandResult ExecuteShieldAllies(
            CombatantState source,
            IList<CombatantState> allies,
            CombatEventLog log,
            int logSegment,
            int logTick)
        {
            foreach (var ally in allies.Where(a => a.IsActive && IsAdjacent(source.AnchorPosition, a.AnchorPosition)))
            {
                if (!HasInfantryTag(ally.Definition))
                    continue;

                ally.PauseArmorBuffSteps += 1;
                log.Append(logSegment, logTick, source.InstanceId, "shield_allies", ally.InstanceId, 1);
            }

            return CommandResult.Ok();
        }

        private static CommandResult ExecuteCannonBlast(
            CombatantState source,
            IList<CombatantState> enemies,
            CombatEventLog log,
            int logSegment,
            int logTick,
            GridCoord? targetCell)
        {
            var primary = ResolvePrimaryTarget(enemies, targetCell);
            if (primary == null)
                return CommandResult.Fail("No valid cannon target");

            ApplyDamage(source, primary, CannonBlastPrimaryDamage, AttackType.Explosive, log, logSegment, logTick, "cannon_blast");
            foreach (var splash in enemies.Where(e => e.IsActive && e.InstanceId != primary.InstanceId && IsAdjacent(primary.AnchorPosition, e.AnchorPosition)))
                ApplyDamage(source, splash, CannonBlastSplashDamage, AttackType.Explosive, log, logSegment, logTick, "cannon_blast_splash");

            return CommandResult.Ok();
        }

        /// <summary>The exact rule an explicit target cell must satisfy to be honored
        /// (a live enemy occupies it). UI target pickers must defer to this so their
        /// valid-cell highlighting can never drift from what execution accepts.</summary>
        public static bool IsValidTargetCell(IEnumerable<CombatantState> enemies, GridCoord cell) =>
            enemies != null && enemies.Any(e => e != null && e.IsActive && OccupiesCell(e, cell));

        private static GridCoord? ResolveTargetCell(IList<CombatantState> enemies, GridCoord? targetCell)
        {
            if (targetCell.HasValue && IsValidTargetCell(enemies, targetCell.Value))
                return targetCell;

            return enemies.Where(e => e.IsActive).OrderBy(e => e.AnchorPosition.X).ThenBy(e => e.InstanceId).FirstOrDefault()?.AnchorPosition;
        }

        private static CombatantState ResolvePrimaryTarget(IList<CombatantState> enemies, GridCoord? targetCell)
        {
            if (targetCell.HasValue)
            {
                var atCell = enemies.FirstOrDefault(e => e.IsActive && OccupiesCell(e, targetCell.Value));
                if (atCell != null)
                    return atCell;
            }

            return enemies.Where(e => e.IsActive).OrderBy(e => e.CurrentHp).ThenBy(e => e.InstanceId).FirstOrDefault();
        }

        private static void ApplyAreaDamage(
            CombatantState source,
            IList<CombatantState> targets,
            GridCoord center,
            int radius,
            int baseDamage,
            AttackType attackType,
            CombatEventLog log,
            int logSegment,
            int logTick,
            string actionType)
        {
            foreach (var target in targets.Where(t => t.IsActive && Manhattan(center, t.AnchorPosition) <= radius))
            {
                var tempAttacker = new PieceDefinition
                {
                    Id = source.Definition.Id,
                    BaseDamage = baseDamage,
                    AttackType = attackType
                };
                int damage = CombatDamageResolver.ComputeDamage(tempAttacker, target.Definition, 1f, target.TotalArmorSteps);
                target.CurrentHp -= damage;
                source.DamageDealtThisFight += damage;
                target.DamageTakenThisFight += damage;
                log.Append(logSegment, logTick, source.InstanceId, actionType, target.InstanceId, damage);
                if (!target.IsAlive)
                    log.Append(logSegment, logTick, target.InstanceId, "destroyed", source.InstanceId, 0);
            }
        }

        private static void ApplyDamage(
            CombatantState source,
            CombatantState target,
            int baseDamage,
            AttackType attackType,
            CombatEventLog log,
            int logSegment,
            int logTick,
            string actionType)
        {
            var tempAttacker = new PieceDefinition
            {
                Id = source.Definition.Id,
                BaseDamage = baseDamage,
                AttackType = attackType
            };
            int damage = CombatDamageResolver.ComputeDamage(tempAttacker, target.Definition, 1f, target.TotalArmorSteps);
            target.CurrentHp -= damage;
            source.DamageDealtThisFight += damage;
            target.DamageTakenThisFight += damage;
            log.Append(logSegment, logTick, source.InstanceId, actionType, target.InstanceId, damage);
            if (!target.IsAlive)
                log.Append(logSegment, logTick, target.InstanceId, "destroyed", source.InstanceId, 0);
        }

        private static bool HasInfantryTag(PieceDefinition definition) =>
            definition.Tags?.Any(t => string.Equals(t, "infantry", System.StringComparison.OrdinalIgnoreCase)) == true;

        private static bool IsAdjacent(GridCoord a, GridCoord b) => Manhattan(a, b) == 1;

        private static bool OccupiesCell(CombatantState combatant, GridCoord cell) =>
            combatant.AnchorPosition.Equals(cell);

        private static int Manhattan(GridCoord a, GridCoord b) =>
            System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Y - b.Y);
    }
}
