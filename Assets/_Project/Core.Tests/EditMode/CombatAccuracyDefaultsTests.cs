using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatAccuracyDefaultsTests
    {
        [Test]
        public void BallisticInfantry_DefaultIs78()
        {
            var def = TestPieces.With(TestPieces.RifleSquad(), attackType: AttackType.Ballistic);
            Assert.AreEqual(78, CombatAccuracyDefaults.GetBaseAccuracy(def));
        }

        [Test]
        public void SniperRole_OverridesBallisticDefault()
        {
            var def = TestPieces.CreateUnit("s", combatRole: GameTagIds.Sniper, primary: GameTagIds.Infantry);
            def = TestPieces.With(def, attackType: AttackType.Ballistic);
            Assert.AreEqual(88, CombatAccuracyDefaults.GetBaseAccuracy(def));
        }

        [Test]
        public void AccuracyOverride_WinsOverTable()
        {
            var def = new PieceDefinition
            {
                Id = "x",
                DisplayName = "x",
                Shape = TestPieces.RifleSquad().Shape,
                AttackType = AttackType.Ballistic,
                AccuracyOverride = 95
            };
            Assert.AreEqual(95, CombatAccuracyDefaults.GetBaseAccuracy(def));
        }
    }
}
