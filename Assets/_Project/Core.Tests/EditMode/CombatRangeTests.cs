using DeadManZone.Core.Board;

using DeadManZone.Core.Combat;

using DeadManZone.Core.Common;

using DeadManZone.Core.Tests;

using NUnit.Framework;



namespace DeadManZone.Core.Tests.EditMode

{

    public sealed class CombatRangeTests

    {

        [Test]

        public void GetRangeCells_MeleeShortMediumLong()

        {

            Assert.AreEqual(1, CombatRange.GetRangeCells(AttackRangeTier.Melee));

            Assert.AreEqual(3, CombatRange.GetRangeCells(AttackRangeTier.Short));

            Assert.AreEqual(5, CombatRange.GetRangeCells(AttackRangeTier.Medium));

            Assert.AreEqual(8, CombatRange.GetRangeCells(AttackRangeTier.Long));

        }



        [Test]

        public void IsInRange_RespectsMeleeVersusShort()

        {

            var from = new GridCoord(0, 0);

            Assert.IsTrue(CombatRange.IsInRange(from, new GridCoord(1, 0), AttackRangeTier.Melee));

            Assert.IsFalse(CombatRange.IsInRange(from, new GridCoord(2, 0), AttackRangeTier.Melee));

            Assert.IsTrue(CombatRange.IsInRange(from, new GridCoord(2, 0), AttackRangeTier.Short));

        }



        [Test]

        public void SelectTarget_SkipsOutOfRangeEnemies()

        {

            var attacker = new CombatantState

            {

                InstanceId = "a1",

                Definition = TestPieces.With(

                    TestPieces.RifleSquad(),

                    attackRange: AttackRangeTier.Melee),

                AnchorPosition = new GridCoord(0, 0),

                CurrentHp = 10

            };

            var near = new CombatantState

            {

                InstanceId = "e1",

                Definition = TestPieces.RifleSquad(),

                AnchorPosition = new GridCoord(1, 0),

                CurrentHp = 10

            };

            var far = new CombatantState

            {

                InstanceId = "e2",

                Definition = TestPieces.RifleSquad(),

                AnchorPosition = new GridCoord(5, 0),

                CurrentHp = 3

            };



            var target = TacticTargeting.SelectTarget(

                attacker,

                new[] { far, near },

                TacticType.DisciplinedFire);



            Assert.AreEqual("e1", target.InstanceId);

        }

    }

}


