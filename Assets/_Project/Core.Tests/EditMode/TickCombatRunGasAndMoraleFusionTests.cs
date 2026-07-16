using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>2026-07-15 faction-roster-v1 §2.7 Blightborn rares: the ambient-gas hijack
    /// (Yellow Autumn) and the gas→morale fusion (Duchess of Sighs). Both are army-wide rules
    /// granted by a single fielded piece, checked live each tick/volley in TickCombatRun.</summary>
    public sealed class TickCombatRunGasAndMoraleFusionTests
    {
        private static PieceDefinition YellowAutumn() => new()
        {
            Id = "yellow_autumn",
            // Unit, not Building: this test places it directly on a Combat-kind board
            // (TestBoards.Layout) — a Building-category piece there would fail placement
            // (Buildings resolve to the HQ board per BoardPlacementRules). The real Yellow
            // Autumn is a combat-board structure per the roster spec anyway.
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 100,
            BaseDamage = 0,
            MovementSpeed = 0,
            HijacksAmbientGas = true
        };

        private static PieceDefinition HarmlessStationary(string id, int maxHp = 100) => new()
        {
            Id = id,
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = maxHp,
            BaseDamage = 0,
            MovementSpeed = 0
        };

        [Test]
        public void AmbientGasHijack_StartsEarlyAndOnlyPlayerSideIsImmune()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(YellowAutumn(), TestBoards.SupportLineAnchor(1, 0), "player_yellow_autumn");
            player.TryPlace(HarmlessStationary("player_ally"), TestBoards.SupportLineAnchor(2, 0), "player_ally");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(HarmlessStationary("enemy_foe"), TestBoards.FrontLineAnchor(), "enemy_foe");

            var run = TickCombatRun.Start(player, enemy, seed: 1);
            run.Continue(System.Array.Empty<PhaseCommand>());

            var enemyGasTicks = run.Log.Events
                .Where(e => e.ActionType == "gas_damage" && e.TargetId == "enemy_foe")
                .Select(e => e.Tick)
                .ToList();
            Assert.IsNotEmpty(enemyGasTicks, "the enemy must still take ambient gas damage");
            Assert.Less(enemyGasTicks.Min(), CombatPacingConfig.GasStartTick,
                "Yellow Autumn must pull the ambient gas start earlier than the default tick 300");
            Assert.GreaterOrEqual(enemyGasTicks.Min(), GasHijackRules.EarlyGasStartTick);

            Assert.IsFalse(
                run.Log.Events.Any(e => e.ActionType == "gas_damage"
                    && (e.TargetId == "player_yellow_autumn" || e.TargetId == "player_ally")),
                "\"your units are immune to it\" — the hijacking side must never take the ambient gas");
        }

        [Test]
        public void WithoutYellowAutumn_GasStillStartsAtDefaultTick()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(HarmlessStationary("player_ally"), TestBoards.SupportLineAnchor(1, 0), "player_ally");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(HarmlessStationary("enemy_foe"), TestBoards.FrontLineAnchor(), "enemy_foe");

            var run = TickCombatRun.Start(player, enemy, seed: 1);
            run.Continue(System.Array.Empty<PhaseCommand>());

            var earliestGasTick = run.Log.Events.Where(e => e.ActionType == "gas_damage").Select(e => e.Tick).Min();
            Assert.GreaterOrEqual(earliestGasTick, CombatPacingConfig.GasStartTick);
        }

        private static PieceDefinition Duchess() => new()
        {
            Id = "duchess_of_sighs",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 50,
            BaseDamage = 0,
            MovementSpeed = 0,
            GasDealsMoraleDamage = true
        };

        private static PieceDefinition GasAttacker() => new()
        {
            Id = "gas_attacker",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 50,
            BaseDamage = 10,
            CooldownTicks = 3,
            AccuracyOverride = 100,
            AttackRange = AttackRangeTier.Long,
            AttackType = AttackType.Gas,
            MovementSpeed = 0
        };

        private static PieceDefinition DurableEnemy() => new()
        {
            Id = "durable_enemy",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 500,
            MaxMorale = 500,
            BaseDamage = 0,
            ArmorType = ArmorType.None
        };

        [Test]
        public void DuchessFielded_GasAttacksAlsoDealEqualMoraleDamage()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(Duchess(), TestBoards.SupportLineAnchor(1, 0), "duchess_1");
            player.TryPlace(GasAttacker(), TestBoards.FrontLineAnchor(), "gas_attacker_1");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(DurableEnemy(), TestBoards.FrontLineAnchor(), "enemy_durable");

            var run = TickCombatRun.Start(player, enemy, seed: 1);
            run.Continue(System.Array.Empty<PhaseCommand>());

            var damageEvt = run.Log.Events.First(e => e.ActionType == "damage" && e.ActorId == "gas_attacker_1");
            var moraleEvt = run.Log.Events.FirstOrDefault(e =>
                e.ActionType == "morale_damage" && e.ActorId == "gas_attacker_1"
                && e.TargetId == damageEvt.TargetId && e.Tick == damageEvt.Tick);

            Assert.NotNull(moraleEvt, "while Duchess of Sighs stands, gas damage must also deal equal morale damage");
            Assert.AreEqual(damageEvt.Value, moraleEvt.Value);
        }

        [Test]
        public void WithoutDuchess_GasAttacksDoNotDealMoraleDamage()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(GasAttacker(), TestBoards.FrontLineAnchor(), "gas_attacker_1");

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(DurableEnemy(), TestBoards.FrontLineAnchor(), "enemy_durable");

            var run = TickCombatRun.Start(player, enemy, seed: 1);
            run.Continue(System.Array.Empty<PhaseCommand>());

            Assert.IsTrue(run.Log.Events.Any(e => e.ActionType == "damage" && e.ActorId == "gas_attacker_1"));
            Assert.IsFalse(run.Log.Events.Any(e => e.ActionType == "morale_damage" && e.ActorId == "gas_attacker_1"));
        }
    }
}
