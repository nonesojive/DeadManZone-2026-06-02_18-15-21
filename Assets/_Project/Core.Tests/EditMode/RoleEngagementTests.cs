using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class RoleEngagementTests
    {
        private static readonly BattlefieldLayout Layout = new(
            playerHalfWidth: 7,
            neutralWidth: 2,
            enemyHalfWidth: 7,
            height: 10);

        [Test]
        public void AssaultRole_GoalIsNearestFrontLineEnemy()
        {
            var assault = CreateCombatant(
                "assault",
                GameTagIds.Assault,
                CombatSide.Player,
                new GridCoord(2, 5));
            var frontEnemy = CreateEnemy("enemy_front", new GridCoord(10, 5));
            var rearEnemy = CreateEnemy("enemy_rear", new GridCoord(12, 5));

            var goal = RoleEngagement.ComputeGoal(
                assault,
                allies: new[] { assault },
                enemies: new[] { rearEnemy, frontEnemy },
                Layout);

            Assert.AreEqual(new GridCoord(9, 5), goal);
        }

        [Test]
        public void InfantryRole_GoalIsNearestFrontLineEnemy()
        {
            var infantry = CreateCombatant(
                "infantry",
                combatRole: null,
                CombatSide.Player,
                new GridCoord(2, 3),
                primary: GameTagIds.Infantry);
            var frontEnemy = CreateEnemy("enemy_front", new GridCoord(10, 3));
            var rearEnemy = CreateEnemy("enemy_rear", new GridCoord(12, 3));

            var goal = RoleEngagement.ComputeGoal(
                infantry,
                allies: new[] { infantry },
                enemies: new[] { rearEnemy, frontEnemy },
                Layout);

            Assert.AreEqual(new GridCoord(9, 3), goal);
        }

        [Test]
        public void ArtilleryRole_GoalHoldsMaxRangeBehindFriendlyFront()
        {
            var artillery = CreateCombatant(
                "artillery",
                GameTagIds.Artillery,
                CombatSide.Player,
                new GridCoord(1, 5),
                attackRange: AttackRangeTier.Long);
            var allyFront = CreateCombatant("ally_front", GameTagIds.Assault, CombatSide.Player, new GridCoord(7, 5));
            var enemy = CreateEnemy("enemy", new GridCoord(12, 5));

            var goal = RoleEngagement.ComputeGoal(
                artillery,
                allies: new[] { artillery, allyFront },
                enemies: new[] { enemy },
                Layout);

            Assert.AreEqual(new GridCoord(4, 5), goal, "Long range (8) from enemy X=12, capped behind friendly front X=7.");
        }

        [Test]
        public void ArtilleryRole_GoalSpreadsYAcrossRearBand()
        {
            var artyA = CreateCombatant(
                "arty_a",
                GameTagIds.Artillery,
                CombatSide.Player,
                new GridCoord(1, 3),
                attackRange: AttackRangeTier.Long);
            var artyB = CreateCombatant(
                "arty_b",
                GameTagIds.Artillery,
                CombatSide.Player,
                new GridCoord(1, 7),
                attackRange: AttackRangeTier.Long);
            var allyFront = CreateCombatant("ally_front", GameTagIds.Assault, CombatSide.Player, new GridCoord(7, 5));
            var enemy = CreateEnemy("enemy", new GridCoord(12, 5));

            var goalA = RoleEngagement.ComputeGoal(
                artyA,
                allies: new[] { artyA, artyB, allyFront },
                enemies: new[] { enemy },
                Layout);
            var goalB = RoleEngagement.ComputeGoal(
                artyB,
                allies: new[] { artyA, artyB, allyFront },
                enemies: new[] { enemy },
                Layout);

            Assert.AreEqual(4, goalA.X);
            Assert.AreEqual(4, goalB.X);
            Assert.AreNotEqual(goalA.Y, goalB.Y);
        }

        [Test]
        public void SniperRole_GoalPrefersRearLowMaxHpEnemy()
        {
            var sniper = CreateCombatant(
                "sniper",
                GameTagIds.Sniper,
                CombatSide.Player,
                new GridCoord(8, 5),
                attackRange: AttackRangeTier.Long);
            var frontTank = CreateEnemy("enemy_front", new GridCoord(10, 5), maxHp: 20);
            var rearTarget = CreateEnemy("enemy_rear", new GridCoord(12, 5), maxHp: 5);

            var goal = RoleEngagement.ComputeGoal(
                sniper,
                allies: new[] { sniper },
                enemies: new[] { frontTank, rearTarget },
                Layout);

            Assert.AreEqual(new GridCoord(12, 5), goal);
        }

        [Test]
        public void SupportRole_GoalStaysBehindFriendlyFront()
        {
            var support = CreateCombatant(
                "support",
                GameTagIds.Support,
                CombatSide.Player,
                new GridCoord(8, 4));
            var allyFront = CreateCombatant("ally_front", GameTagIds.Assault, CombatSide.Player, new GridCoord(7, 5));
            var allyRear = CreateCombatant("ally_rear", GameTagIds.Assault, CombatSide.Player, new GridCoord(2, 4));
            var enemy = CreateEnemy("enemy", new GridCoord(12, 5));

            var goal = RoleEngagement.ComputeGoal(
                support,
                allies: new[] { support, allyFront, allyRear },
                enemies: new[] { enemy },
                Layout);

            Assert.AreEqual(new GridCoord(2, 4), goal, "Support ahead of front should fall back to friendly rear line.");
        }

        [Test]
        public void SupportRole_HoldsWhenAlreadyBehindFront()
        {
            var support = CreateCombatant(
                "support",
                GameTagIds.Support,
                CombatSide.Player,
                new GridCoord(3, 4));
            var allyFront = CreateCombatant("ally_front", GameTagIds.Assault, CombatSide.Player, new GridCoord(7, 5));
            var enemy = CreateEnemy("enemy", new GridCoord(12, 5));

            var goal = RoleEngagement.ComputeGoal(
                support,
                allies: new[] { support, allyFront },
                enemies: new[] { enemy },
                Layout);

            Assert.AreEqual(new GridCoord(3, 4), goal);
        }

        [Test]
        public void DefaultRole_GoalIsNearestEnemyAnchor()
        {
            var mover = CreateCombatant(
                "vehicle",
                combatRole: null,
                CombatSide.Player,
                new GridCoord(2, 5),
                primary: GameTagIds.Vehicle);
            var nearEnemy = CreateEnemy("enemy_near", new GridCoord(10, 5));
            var farEnemy = CreateEnemy("enemy_far", new GridCoord(12, 1));

            var goal = RoleEngagement.ComputeGoal(
                mover,
                allies: new[] { mover },
                enemies: new[] { farEnemy, nearEnemy },
                Layout);

            Assert.AreEqual(new GridCoord(9, 5), goal);
        }

        private static CombatantState CreateCombatant(
            string id,
            string combatRole,
            CombatSide side,
            GridCoord position,
            string primary = null,
            AttackRangeTier attackRange = AttackRangeTier.Medium)
        {
            var definition = TestPieces.With(
                TestPieces.CreateUnit(id, primary: primary, combatRole: combatRole),
                attackRange: attackRange);

            return new CombatantState
            {
                InstanceId = id,
                Side = side,
                Definition = definition,
                CurrentHp = definition.MaxHp,
                AnchorPosition = position,
                SpawnAnchorY = position.Y
            };
        }

        private static CombatantState CreateEnemy(string id, GridCoord position, int maxHp = 10)
        {
            var baseDefinition = TestPieces.CreateUnit(id, combatRole: GameTagIds.Assault);
            var definition = new PieceDefinition
            {
                Id = baseDefinition.Id,
                DisplayName = baseDefinition.DisplayName,
                Category = baseDefinition.Category,
                Shape = baseDefinition.Shape,
                Primary = baseDefinition.Primary,
                CombatRole = baseDefinition.CombatRole,
                SystemTag = baseDefinition.SystemTag,
                SynergyTags = baseDefinition.SynergyTags,
                AbilityTags = baseDefinition.AbilityTags,
                FlavorTags = baseDefinition.FlavorTags,
                Tags = baseDefinition.Tags,
                MaxHp = maxHp,
                BaseDamage = baseDefinition.BaseDamage,
                CooldownTicks = baseDefinition.CooldownTicks,
                RequisitionCost = baseDefinition.RequisitionCost,
                ManpowerCost = baseDefinition.ManpowerCost,
                ShopModifiers = baseDefinition.ShopModifiers,
                CommandActions = baseDefinition.CommandActions,
                AttackRange = baseDefinition.AttackRange,
                MovementSpeed = baseDefinition.MovementSpeed,
                AttackSpeed = baseDefinition.AttackSpeed,
                ArmorType = baseDefinition.ArmorType,
                AttackType = baseDefinition.AttackType,
                GrantedAbility = baseDefinition.GrantedAbility,
                FactionId = baseDefinition.FactionId
            };

            return new CombatantState
            {
                InstanceId = id,
                Side = CombatSide.Enemy,
                Definition = definition,
                CurrentHp = maxHp,
                AnchorPosition = position,
                SpawnAnchorY = position.Y
            };
        }
    }
}
