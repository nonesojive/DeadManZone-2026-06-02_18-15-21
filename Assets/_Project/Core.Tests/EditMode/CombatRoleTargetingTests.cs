using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatRoleTargetingTests
    {
        [Test]
        public void ArtilleryRole_PrefersFurthestTargetInRange()
        {
            var attacker = CreateAttacker("attacker_artillery", GameTagIds.Artillery, AttackRangeTier.Long, new GridCoord(0, 0));
            var near = CreateEnemy("enemy_near", hp: 1, maxHp: 6, new GridCoord(2, 0));
            var far = CreateEnemy("enemy_far", hp: 10, maxHp: 10, new GridCoord(5, 0));

            var target = TacticTargeting.SelectTarget(
                attacker,
                new[] { near, far },
                TacticType.DisciplinedFire);

            Assert.NotNull(target);
            Assert.AreEqual("enemy_far", target.InstanceId);
        }

        [Test]
        public void AssaultRole_PrefersCombatantOverHq()
        {
            var attacker = CreateAttacker("attacker_assault", GameTagIds.Assault, AttackRangeTier.Long, new GridCoord(0, 0));
            var hq = CreateEnemy("enemy_hq", hp: 25, maxHp: 25, new GridCoord(2, 0));
            hq = WithSystemTag(hq, GameTagIds.Hq);
            var conscript = CreateEnemy("enemy_conscript", hp: 8, maxHp: 8, new GridCoord(3, 0));

            var target = TacticTargeting.SelectTarget(
                attacker,
                new[] { hq, conscript },
                TacticType.Advance);

            Assert.NotNull(target);
            Assert.AreEqual("enemy_conscript", target.InstanceId);
        }

        [Test]
        public void UtilityRole_DoesNotSelectAttackTarget()
        {
            var attacker = CreateAttacker("attacker_utility", GameTagIds.Utility, AttackRangeTier.Long, new GridCoord(0, 0));
            var enemy = CreateEnemy("enemy", hp: 8, maxHp: 8, new GridCoord(2, 0));

            var target = TacticTargeting.SelectTarget(
                attacker,
                new[] { enemy },
                TacticType.Advance);

            Assert.IsNull(target);
        }

        private static CombatantState CreateAttacker(string id, string combatRole, AttackRangeTier attackRange, GridCoord position)
        {
            var baseDefinition = TestPieces.CreateUnit(id, combatRole: combatRole);
            var definition = TestPieces.With(baseDefinition, attackRange: attackRange);
            return new CombatantState
            {
                InstanceId = id,
                Side = CombatSide.Player,
                Definition = definition,
                CurrentHp = definition.MaxHp,
                AnchorPosition = position
            };
        }

        private static CombatantState WithSystemTag(CombatantState combatant, string systemTag)
        {
            var definition = combatant.Definition;
            definition = new PieceDefinition
            {
                Id = definition.Id,
                DisplayName = definition.DisplayName,
                Category = definition.Category,
                Shape = definition.Shape,
                Primary = definition.Primary,
                CombatRole = definition.CombatRole,
                SystemTag = systemTag,
                SynergyTags = definition.SynergyTags,
                AbilityTags = definition.AbilityTags,
                FlavorTags = definition.FlavorTags,
                Tags = definition.Tags,
                MaxHp = definition.MaxHp,
                BaseDamage = definition.BaseDamage,
                CooldownTicks = definition.CooldownTicks,
                GoldCost = definition.GoldCost,
                RequisitionCost = definition.RequisitionCost,
                ManpowerCost = definition.ManpowerCost,
                ShopModifiers = definition.ShopModifiers,
                CommandActions = definition.CommandActions,
                AttackRange = definition.AttackRange,
                MovementSpeed = definition.MovementSpeed,
                AttackSpeed = definition.AttackSpeed,
                ArmorType = definition.ArmorType,
                AttackType = definition.AttackType,
                GrantedAbility = definition.GrantedAbility,
                FactionId = definition.FactionId
            };

            return new CombatantState
            {
                InstanceId = combatant.InstanceId,
                Side = combatant.Side,
                Definition = definition,
                CurrentHp = combatant.CurrentHp,
                AnchorPosition = combatant.AnchorPosition
            };
        }

        private static CombatantState CreateEnemy(string id, int hp, int maxHp, GridCoord position)
        {
            var definition = TestPieces.With(
                TestPieces.CreateUnit(id),
                attackRange: AttackRangeTier.Long);

            definition = new PieceDefinition
            {
                Id = definition.Id,
                DisplayName = definition.DisplayName,
                Category = definition.Category,
                Shape = definition.Shape,
                Primary = definition.Primary,
                CombatRole = definition.CombatRole,
                SystemTag = definition.SystemTag,
                SynergyTags = definition.SynergyTags,
                AbilityTags = definition.AbilityTags,
                FlavorTags = definition.FlavorTags,
                Tags = definition.Tags,
                MaxHp = maxHp,
                BaseDamage = definition.BaseDamage,
                CooldownTicks = definition.CooldownTicks,
                GoldCost = definition.GoldCost,
                RequisitionCost = definition.RequisitionCost,
                ManpowerCost = definition.ManpowerCost,
                ShopModifiers = definition.ShopModifiers,
                CommandActions = definition.CommandActions,
                AttackRange = definition.AttackRange,
                MovementSpeed = definition.MovementSpeed,
                AttackSpeed = definition.AttackSpeed,
                ArmorType = definition.ArmorType,
                AttackType = definition.AttackType,
                GrantedAbility = definition.GrantedAbility,
                FactionId = definition.FactionId
            };

            return new CombatantState
            {
                InstanceId = id,
                Side = CombatSide.Enemy,
                Definition = definition,
                CurrentHp = hp,
                AnchorPosition = position
            };
        }
    }
}
