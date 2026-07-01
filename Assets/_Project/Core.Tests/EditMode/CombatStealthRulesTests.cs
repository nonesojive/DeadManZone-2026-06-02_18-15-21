using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatStealthRulesTests
    {
        [Test]
        public void IroncladMarksman_NotTargetableBeforeSecondTacticsWindow()
        {
            var marksman = CreateMarksman("player_marksman", new GridCoord(8, 5));
            var rifle = CreateRifle("player_rifle", new GridCoord(8, 4), currentHp: 50);

            Assert.IsFalse(CombatStealthRules.IsTargetableByEnemies(marksman, tacticsCheckpointIndex: 0));
            Assert.IsFalse(CombatStealthRules.IsTargetableByEnemies(marksman, tacticsCheckpointIndex: 1));

            var attacker = CreateEnemyAttacker(new GridCoord(10, 5));
            var target = TacticTargeting.SelectTarget(
                attacker,
                new[] { marksman, rifle },
                TacticType.DisciplinedFire,
                tacticsCheckpointIndex: 1);

            Assert.AreEqual("player_rifle", target.InstanceId);
        }

        [Test]
        public void Marksman_TargetableAfterCheckpoint2()
        {
            var marksman = CreateMarksman("player_marksman", new GridCoord(8, 5));
            var rifle = CreateRifle("player_rifle", new GridCoord(8, 4), currentHp: 50);

            Assert.IsTrue(CombatStealthRules.IsTargetableByEnemies(marksman, tacticsCheckpointIndex: 2));

            var attacker = CreateEnemyAttacker(new GridCoord(10, 5));
            var target = TacticTargeting.SelectTarget(
                attacker,
                new[] { marksman, rifle },
                TacticType.DisciplinedFire,
                tacticsCheckpointIndex: 2);

            Assert.AreEqual("player_marksman", target.InstanceId);
        }

        private static CombatantState CreateMarksman(string instanceId, GridCoord position) => new()
        {
            InstanceId = instanceId,
            Side = CombatSide.Player,
            Definition = new PieceDefinition
            {
                Id = "ironclad_marksman",
                DisplayName = "Ironclad Marksman",
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                MaxHp = 35,
                BaseDamage = 6,
                AttackRange = AttackRangeTier.Long,
                AbilityTags = new[] { GameTagIds.Stealth },
            },
            AnchorPosition = position,
            CurrentHp = 35,
        };

        private static CombatantState CreateRifle(string instanceId, GridCoord position, int currentHp) => new()
        {
            InstanceId = instanceId,
            Side = CombatSide.Player,
            Definition = TestPieces.RifleSquad(),
            AnchorPosition = position,
            CurrentHp = currentHp,
        };

        private static CombatantState CreateEnemyAttacker(GridCoord position) => new()
        {
            InstanceId = "enemy_rifle",
            Side = CombatSide.Enemy,
            Definition = TestPieces.With(TestPieces.RifleSquad(), attackRange: AttackRangeTier.Long),
            AnchorPosition = position,
            CurrentHp = 100,
        };
    }
}
