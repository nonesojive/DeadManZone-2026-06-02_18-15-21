using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>2026-07-15 faction-roster-v1 §1.8/§2.5 transport tentpole (Armored Ark):
    /// "load pieces in Build. At the opening window the player targets a cell; the transport
    /// drives there and unloads on arrival ... if destroyed in transit, cargo spills out at the
    /// wreck with a morale shock — never dies inside."</summary>
    public sealed class TickCombatRunTransportTests
    {
        private static PieceDefinition Transport(int maxHp = 100, int movementSpeed = 3) => new()
        {
            Id = "armored_ark",
            DisplayName = "Armored Ark",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = maxHp,
            ArmorType = ArmorType.None,
            IsTransport = true,
            TransportCapacity = 1,
            MovementSpeed = movementSpeed
        };

        private static PieceDefinition Cargo() => new()
        {
            Id = "truncheon_line",
            DisplayName = "Truncheon Line",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 40,
            MaxMorale = 40,
            BaseDamage = 50,
            CooldownTicks = 1,
            AccuracyOverride = 100,
            AttackRange = AttackRangeTier.Long,
            MovementSpeed = 3
        };

        private static PieceDefinition HarmlessEnemy() => new()
        {
            Id = "harmless",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 5,
            MovementSpeed = 0
        };

        private static PieceDefinition HeavyHitter() => new()
        {
            Id = "heavy_hitter",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 100,
            BaseDamage = 50,
            CooldownTicks = 1,
            AccuracyOverride = 100,
            AttackRange = AttackRangeTier.Long
        };

        [Test]
        public void SpawnCombatants_LoadedCargo_SpawnsEmbarkedAndOffOccupancyGrid()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(Transport(), TestBoards.FrontLineAnchor(), "ark_1");
            player.TryPlace(Cargo(), TestBoards.SupportLineAnchor(1, 0), "cargo_1");
            Assert.IsTrue(player.TryLoadCargo("cargo_1", "ark_1").Success);

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(HarmlessEnemy(), TestBoards.FrontLineAnchor(), "enemy_1");

            var run = TickCombatRun.Start(player, enemy, seed: 1, authority: 0);

            var cargo = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "cargo_1");
            var transport = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "ark_1");

            Assert.IsTrue(cargo.IsEmbarked, "Build-loaded cargo must spawn embarked, not independent");
            Assert.IsFalse(cargo.IsActive, "embarked cargo is off the field — untargetable, can't move/attack");
            Assert.Contains("cargo_1", transport.EmbarkedCargoIds.ToList());
            Assert.IsFalse(
                run.OccupancySnapshotForTests.Values.Contains("cargo_1"),
                "embarked cargo must not occupy a battlefield cell");
        }

        [Test]
        public void TransportTarget_AtOwnSpawnCell_UnloadsImmediately()
        {
            var player = new BoardState(TestBoards.Layout);
            var arkAnchor = TestBoards.FrontLineAnchor();
            player.TryPlace(Transport(), arkAnchor, "ark_1");
            player.TryPlace(Cargo(), TestBoards.SupportLineAnchor(1, 0), "cargo_1");
            Assert.IsTrue(player.TryLoadCargo("cargo_1", "ark_1").Success);

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(HarmlessEnemy(), TestBoards.FrontLineAnchor(), "enemy_1");

            var run = TickCombatRun.Start(player, enemy, seed: 1, authority: 0);
            var transportAnchor = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "ark_1").AnchorPosition;

            run.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.TransportTarget,
                    SourcePieceId = "ark_1",
                    TargetCell = transportAnchor
                }
            });

            Assert.IsTrue(run.Log.Events.Any(e => e.ActionType == "transport_target" && e.ActorId == "ark_1"));
            Assert.IsTrue(
                run.Log.Events.Any(e => e.ActionType == "transport_unload" && e.TargetId == "cargo_1"),
                "a transport already at its target must unload on the very next movement tick");

            var cargo = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "cargo_1");
            Assert.IsFalse(cargo.IsEmbarked, "cargo is on the field after unload");
        }

        [Test]
        public void TransportDestroyedInTransit_SpillsCargoWithMoraleShockAndNeverKillsCargoDirectly()
        {
            var player = new BoardState(TestBoards.Layout);
            player.TryPlace(Transport(maxHp: 10, movementSpeed: 0), TestBoards.FrontLineAnchor(), "ark_1");
            player.TryPlace(Cargo(), TestBoards.SupportLineAnchor(1, 0), "cargo_1");
            Assert.IsTrue(player.TryLoadCargo("cargo_1", "ark_1").Success);

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(HeavyHitter(), TestBoards.FrontLineAnchor(), "enemy_1");

            var run = TickCombatRun.Start(player, enemy, seed: 2, authority: 0);
            run.Continue(System.Array.Empty<PhaseCommand>());

            Assert.IsTrue(
                run.Log.Events.Any(e => e.ActionType == "transport_spill" && e.TargetId == "cargo_1"),
                "the fragile transport must be destroyed and spill its cargo");
            Assert.IsTrue(
                run.Log.Events.Any(e =>
                    e.ActionType == "morale_damage" && e.ActorId == "ark_1" && e.TargetId == "cargo_1"
                    && e.Value == TransportRules.SpillMoraleShock),
                "spill must apply the morale shock, sourced from the wreck");
            Assert.IsTrue(
                run.Log.Events.Any(e => e.ActionType == "move" && e.ActorId == "cargo_1"),
                "spilled cargo must be placed on the field (replay-visible via a move event)");

            var arkDestroyedTick = run.Log.Events.Single(e => e.ActionType == "destroyed" && e.ActorId == "ark_1").Tick;
            var cargoSpillTick = run.Log.Events.Single(e => e.ActionType == "transport_spill" && e.TargetId == "cargo_1").Tick;
            Assert.AreEqual(arkDestroyedTick, cargoSpillTick,
                "spill happens atomically with the transport's own destruction — cargo is never embarked-but-doomed for even one tick");
        }
    }
}
