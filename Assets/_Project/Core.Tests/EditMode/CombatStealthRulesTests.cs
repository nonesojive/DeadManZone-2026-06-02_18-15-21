using System.Linq;
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
        public void IroncladMarksman_NotTargetableBeforeMidFightPause()
        {
            var marksman = CreateMarksman("player_marksman", new GridCoord(8, 5));
            var rifle = CreateRifle("player_rifle", new GridCoord(8, 4), currentHp: 50);

            Assert.IsFalse(CombatStealthRules.IsTargetableByEnemies(marksman, tacticsCheckpointIndex: 0));

            var attacker = CreateEnemyAttacker(new GridCoord(10, 5));
            var target = TacticTargeting.SelectTarget(
                attacker,
                new[] { marksman, rifle },
                TacticType.DisciplinedFire,
                tacticsCheckpointIndex: 0);

            Assert.AreEqual("player_rifle", target.InstanceId);
        }

        [Test]
        public void Marksman_TargetableAfterMidFightPause()
        {
            var marksman = CreateMarksman("player_marksman", new GridCoord(8, 5));
            var rifle = CreateRifle("player_rifle", new GridCoord(8, 4), currentHp: 50);

            // CheckpointsFired maxes at PauseThresholds.Length (1) in a real fight,
            // so expiry must be reachable at index 1.
            Assert.IsTrue(CombatStealthRules.IsTargetableByEnemies(marksman, tacticsCheckpointIndex: 1));

            var attacker = CreateEnemyAttacker(new GridCoord(10, 5));
            var target = TacticTargeting.SelectTarget(
                attacker,
                new[] { marksman, rifle },
                TacticType.DisciplinedFire,
                tacticsCheckpointIndex: 1);

            Assert.AreEqual("player_marksman", target.InstanceId);
        }

        [Test]
        public void Marksman_StealthExpiresInRealFight_AttackedOnlyAfterMidFightPause()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(3), "player_rifle_1");
            player.TryPlace(MarksmanPiece(), TestBoards.FrontLineAnchor(6), "player_marksman");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(3), "enemy_rifle_1");
            enemy.TryPlace(TestPieces.RifleSquad(), TestBoards.FrontLineAnchor(6), "enemy_rifle_2");

            var run = TickCombatRun.Start(player, enemy, seed: 42);

            var result = run.Continue(System.Array.Empty<PhaseCommand>());
            Assert.AreEqual(
                CombatAdvanceStatus.AwaitingCommand,
                result.Status,
                "fight should reach the mid-fight pause with the marksman untargetable");

            bool AttacksMarksman(CombatEvent e) =>
                e.TargetId == "player_marksman" &&
                e.ActionType is "damage" or "graze" or "miss";

            Assert.IsFalse(
                run.Log.Events.Any(e => e.Segment == 0 && AttacksMarksman(e)),
                "marksman must not be attacked while stealthed (segment 0)");

            while (result.Status == CombatAdvanceStatus.AwaitingCommand)
                result = run.Continue(System.Array.Empty<PhaseCommand>());

            Assert.IsTrue(
                run.Log.Events.Any(e => e.Segment >= 1 && AttacksMarksman(e)),
                "marksman stealth must expire after the mid-fight pause in a real fight");
        }

        private static PieceDefinition MarksmanPiece() => new()
        {
            Id = CombatStealthRules.MarksmanPieceId,
            DisplayName = "Ironclad Marksman",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 35,
            BaseDamage = 6,
            CooldownTicks = 3,
            AttackRange = AttackRangeTier.Long,
            AbilityTags = new[] { GameTagIds.Stealth },
        };

        private static CombatantState CreateMarksman(string instanceId, GridCoord position) => new()
        {
            InstanceId = instanceId,
            Side = CombatSide.Player,
            Definition = new PieceDefinition
            {
                Id = CombatStealthRules.MarksmanPieceId,
                DisplayName = "Marksman-Doctrine Officer",
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
