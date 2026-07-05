using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class ArmyHealthTrackerTests
    {
        private static CombatantState MakeCombatant(string id, int maxHp, int currentHp, bool fightsInCombat = true)
        {
            return new CombatantState
            {
                InstanceId = id,
                Definition = new PieceDefinition
                {
                    Id = id,
                    MaxHp = maxHp,
                    Category = fightsInCombat ? PieceCategory.Unit : PieceCategory.Building,
                    BaseDamage = fightsInCombat ? 1 : 0
                },
                CurrentHp = currentHp
            };
        }

        [Test]
        public void Evaluate_SumsCombatantHpOnly()
        {
            var army = new List<CombatantState>
            {
                MakeCombatant("a", 100, 60),
                MakeCombatant("b", 50, 50),
                MakeCombatant("hq", 200, 200, fightsInCombat: false)
            };

            var health = ArmyHealthTracker.Evaluate(army);

            Assert.AreEqual(110, health.CurrentHp);
            Assert.AreEqual(150, health.StartingHp);
            Assert.AreEqual(110f / 150f, health.Fraction, 0.0001f);
        }

        [Test]
        public void Evaluate_ClampsNegativeHpToZero()
        {
            var army = new List<CombatantState> { MakeCombatant("a", 100, -25) };

            var health = ArmyHealthTracker.Evaluate(army);

            Assert.AreEqual(0, health.CurrentHp);
            Assert.AreEqual(0f, health.Fraction);
        }

        [Test]
        public void Evaluate_EmptyArmyHasZeroFraction()
        {
            var health = ArmyHealthTracker.Evaluate(new List<CombatantState>());
            Assert.AreEqual(0f, health.Fraction);
        }

        [Test]
        public void ReplayTracker_TracksDamageAndDestroyedEvents()
        {
            var tracker = new ArmyHealthReplayTracker();
            tracker.RegisterUnit("p1", CombatSide.Player, maxHp: 100);
            tracker.RegisterUnit("p2", CombatSide.Player, maxHp: 100);
            tracker.RegisterUnit("e1", CombatSide.Enemy, maxHp: 80);

            tracker.ApplyEvent(new CombatEvent { ActionType = "damage", TargetId = "p1", Value = 40 });
            Assert.AreEqual(160f / 200f, tracker.GetFraction(CombatSide.Player), 0.0001f);

            tracker.ApplyEvent(new CombatEvent { ActionType = "destroyed", ActorId = "p2" });
            Assert.AreEqual(60f / 200f, tracker.GetFraction(CombatSide.Player), 0.0001f);

            Assert.AreEqual(1f, tracker.GetFraction(CombatSide.Enemy), 0.0001f);
        }

        [Test]
        public void ReplayTracker_TryGetUnitFraction_TracksSingleUnit()
        {
            var tracker = new ArmyHealthReplayTracker();
            tracker.RegisterUnit("p1", CombatSide.Player, maxHp: 80);

            Assert.IsTrue(tracker.TryGetUnitFraction("p1", out float full));
            Assert.AreEqual(1f, full, 0.0001f);

            tracker.ApplyEvent(new CombatEvent { ActionType = "damage", TargetId = "p1", Value = 20 });
            Assert.IsTrue(tracker.TryGetUnitFraction("p1", out float hurt));
            Assert.AreEqual(0.75f, hurt, 0.0001f);

            tracker.ApplyEvent(new CombatEvent { ActionType = "destroyed", ActorId = "p1" });
            Assert.IsTrue(tracker.TryGetUnitFraction("p1", out float dead));
            Assert.AreEqual(0f, dead, 0.0001f);

            Assert.IsFalse(tracker.TryGetUnitFraction("ghost", out _));
            Assert.IsFalse(tracker.TryGetUnitFraction(null, out _));
        }

        [Test]
        public void ReplayTracker_IgnoresUnknownTargetsAndNonDamageEvents()
        {
            var tracker = new ArmyHealthReplayTracker();
            tracker.RegisterUnit("p1", CombatSide.Player, maxHp: 100);

            tracker.ApplyEvent(new CombatEvent { ActionType = "move", ActorId = "p1", TargetId = "3,2" });
            tracker.ApplyEvent(new CombatEvent { ActionType = "damage", TargetId = "ghost", Value = 10 });

            Assert.AreEqual(1f, tracker.GetFraction(CombatSide.Player), 0.0001f);
        }
    }
}
