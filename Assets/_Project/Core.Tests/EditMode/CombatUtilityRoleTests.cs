using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using DeadManZone.Data;
using DeadManZone.Game.Dev;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatUtilityRoleTests
    {
        [Test]
        public void ResolveBias_Utility_UsesNearestFront()
        {
            Assert.AreEqual(
                CombatRoleTargetingBias.NearestFront,
                CombatRoleProfile.ResolveBias(GameTagIds.Utility));
        }

        [Test]
        public void UtilityFieldMarshal_IsNotDeprioritizedTarget()
        {
            var marshal = TestPieces.IroncladFieldMarshal();
            Assert.AreEqual(GameTagIds.Utility, marshal.CombatRole);
            Assert.IsFalse(PieceCombatRules.IsDeprioritizedTarget(marshal));
        }

        [Test]
        public void NonDamagingUtilityBuilding_IsDeprioritizedTarget()
        {
            var depot = new PieceDefinition
            {
                CombatRole = GameTagIds.Utility,
                BaseDamage = 0,
                Category = PieceCategory.Building
            };

            Assert.IsTrue(PieceCombatRules.IsDeprioritizedTarget(depot));
        }

        [Test]
        public void FieldMarshal_UtilityRole_DealsDamageOnCombatBoard()
        {
            var database = ContentDatabase.Load();
            Assert.NotNull(database);
            var registry = ContentRegistryProvider.Build(database);
            // 2026-07-15 faction-roster-v1: ironclad_field_marshal → shock_sergeant, which
            // kept the same Utility combatRole + positive BaseDamage combination this test
            // exercises (utility-role pieces that deal damage must still be targetable/attack).
            Assert.IsTrue(registry.TryGetById("shock_sergeant", out var marshal));
            Assert.AreEqual(GameTagIds.Utility, marshal.CombatRole);

            var player = new BoardState(TestBoards.CombatLayout);
            player.TryPlace(marshal, new GridCoord(1, 3), "marshal_1");

            var enemy = new BoardState(TestBoards.CombatLayout);
            enemy.TryPlace(TestPieces.WeakConscript(), new GridCoord(4, 3), "enemy_1");
            enemy.TryPlace(TestPieces.WeakConscript(), new GridCoord(5, 4), "enemy_2");

            var run = TickCombatRun.Start(player, enemy, seed: 42);
            run.Continue(System.Array.Empty<PhaseCommand>());

            var result = run.Continue(System.Array.Empty<PhaseCommand>());
            while (!run.IsFightOver)
            {
                if (result.Status == CombatAdvanceStatus.AwaitingCommand)
                    result = run.Continue(System.Array.Empty<PhaseCommand>());
                else
                    result = run.Continue(System.Array.Empty<PhaseCommand>());
            }

            Assert.IsTrue(
                run.Log.Events.Any(e =>
                    e.ActorId == "marshal_1" && (e.ActionType == "damage" || e.ActionType == "graze")),
                "Utility-role Field Marshal should attack using the utility combat profile.");
        }
    }
}
