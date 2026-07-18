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

        /// <summary>Mobile + lightly-armed — used to converge on and try to box in the Ark
        /// without one-shotting it, so a "surrounded" test can observe continued movement
        /// rather than an instant kill (HeavyHitter is for the destroyed-in-transit test).</summary>
        private static PieceDefinition Skirmisher() => new()
        {
            Id = "skirmisher",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 30,
            MovementSpeed = 2,
            BaseDamage = 5,
            CooldownTicks = 3,
            AccuracyOverride = 100,
            AttackRange = AttackRangeTier.Short
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

        /// <summary>2026-07-17 owner-diagnosed soft-lock fix: live-playtest evidence showed the
        /// Ark stalling under HOLD THE LINE (StandGround — the doctrine the owner had set).
        /// Root cause: TryMoveSide computed moveChargePerTick from the ACTIVE doctrine/tactic
        /// BEFORE branching into the transport-with-target bypass, so a StandGround 90%
        /// multiplier still applied to a "beelining" transport. For a slow (MovementSpeed 1)
        /// mover that truncates 2*90/100 to 1 charge/tick instead of 2 — a silent 50% cut, not
        /// the intended ~10% — roughly DOUBLING time-to-arrival. Fixed: a transport run now
        /// computes its charge with no tactic argument at all (doctrine/stance ignored
        /// entirely), only MoveChargePercentBonus (the piece's own buff) still applies.</summary>
        [Test]
        public void TransportTarget_UnderHoldTheLineDoctrine_ArrivesAndUnloads()
        {
            var player = new BoardState(TestBoards.Layout);
            var arkAnchor = TestBoards.FrontLineAnchor(); // (7,5)
            player.TryPlace(Transport(movementSpeed: 1), arkAnchor, "ark_1");
            player.TryPlace(Cargo(), TestBoards.SupportLineAnchor(1, 0), "cargo_1");
            Assert.IsTrue(player.TryLoadCargo("cargo_1", "ark_1").Success);

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(HarmlessEnemy(), TestBoards.FrontLineAnchor(), "enemy_1");

            var run = TickCombatRun.Start(player, enemy, seed: 3, authority: 0);
            run.SetPlayerTactic(TacticType.StandGround); // "HOLD THE LINE" in the UI — the doctrine live in the owner's report.

            var targetCell = new GridCoord(arkAnchor.X - 3, arkAnchor.Y); // 3 steps away, same row (no lane-bias cost)
            run.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.TransportTarget,
                    SourcePieceId = "ark_1",
                    TargetCell = targetCell
                }
            });

            var unloadEvent = run.Log.Events.FirstOrDefault(e => e.ActionType == "transport_unload" && e.TargetId == "cargo_1");
            Assert.IsNotNull(unloadEvent, "the Ark must still reach a HOLD THE LINE-gated target and unload");
            // Un-gated math: MovementSpeed 1 -> 2 charge/tick, 100 charge/step, 3 steps -> ~150 ticks.
            // The pre-fix bug's integer-truncated 1 charge/tick needed ~300 ticks for the same
            // trip. 220 comfortably clears the fix and fails the regression.
            Assert.Less(unloadEvent.Tick, 220,
                "HOLD THE LINE must not gate a transport's movement rate — this bound catches the old truncation slowdown");

            var cargo = run.PlayerCombatantsForTests.Single(c => c.InstanceId == "cargo_1");
            Assert.IsFalse(cargo.IsEmbarked, "cargo is on the field after unload");
        }

        /// <summary>Owner spec: "or when enemies close, the Ark stalls." A footprint-aware
        /// mover can only sidestep through cells ShapePathfinder finds free — this pins that a
        /// transport run still finds those cells and keeps making headway with a live, mobile,
        /// attacking escort converging on its path, not just against static furniture.</summary>
        [Test]
        public void TransportTarget_SurroundedByEnemies_StillProgresses()
        {
            var player = new BoardState(TestBoards.Layout);
            var arkAnchor = TestBoards.FrontLineAnchor(); // (7,5)
            player.TryPlace(Transport(), arkAnchor, "ark_1"); // default movementSpeed 3
            player.TryPlace(Cargo(), TestBoards.SupportLineAnchor(1, 0), "cargo_1");
            Assert.IsTrue(player.TryLoadCargo("cargo_1", "ark_1").Success);

            var enemy = new BoardState(TestBoards.Layout);
            // Four mobile, attacking enemies converge along the Ark's straight-line path. The
            // Ark has no attack of its own (BaseDamage 0) so it can only sidestep, same as any
            // other mover — this is the case the fix must not regress.
            enemy.TryPlace(Skirmisher(), TestBoards.FrontLineAnchor(3), "skirmisher_1");
            enemy.TryPlace(Skirmisher(), new GridCoord(5, 5), "skirmisher_2");
            enemy.TryPlace(Skirmisher(), new GridCoord(3, 5), "skirmisher_3");
            enemy.TryPlace(Skirmisher(), new GridCoord(1, 5), "skirmisher_4");

            var run = TickCombatRun.Start(player, enemy, seed: 5, authority: 0);
            var targetCell = new GridCoord(0, 5);
            run.Continue(new[]
            {
                new PhaseCommand
                {
                    AfterCheckpoint = 0,
                    Type = CommandType.TransportTarget,
                    SourcePieceId = "ark_1",
                    TargetCell = targetCell
                }
            });

            var arkMoves = run.Log.Events.Where(e => e.ActorId == "ark_1" && e.ActionType == "move").ToList();
            Assert.Greater(arkMoves.Count, 3,
                "a surrounded transport must keep sidestepping toward its target, not freeze in place");
            Assert.IsTrue(
                run.Log.Events.Any(e => e.ActionType == "transport_unload" && e.TargetId == "cargo_1"),
                "it must still complete the run and unload despite the escort trying to box it in");
        }

        /// <summary>Owner spec: "After unload (or with no target), it behaves as before."
        /// Pins that a transport with nothing registered in _transportTargets is completely
        /// untouched by this fix — same doctrine-affected charge, same RoleEngagement goal.</summary>
        [Test]
        public void Transport_WithNoTarget_MovesNormally_UnaffectedByFix()
        {
            var player = new BoardState(TestBoards.Layout);
            var arkAnchor = TestBoards.FrontLineAnchor();
            player.TryPlace(Transport(), arkAnchor, "ark_1");
            // No cargo loaded and no TransportTarget command submitted.

            var enemy = new BoardState(TestBoards.Layout);
            enemy.TryPlace(HarmlessEnemy(), TestBoards.FrontLineAnchor(3), "enemy_1");

            var run = TickCombatRun.Start(player, enemy, seed: 11, authority: 0);
            run.Continue(System.Array.Empty<PhaseCommand>());

            Assert.IsFalse(run.HasTransportTarget("ark_1"), "no target was ever registered");
            var arkMoves = run.Log.Events.Where(e => e.ActorId == "ark_1" && e.ActionType == "move").ToList();
            Assert.Greater(arkMoves.Count, 0,
                "a targetless transport must still advance under normal engagement rules, unchanged");
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
