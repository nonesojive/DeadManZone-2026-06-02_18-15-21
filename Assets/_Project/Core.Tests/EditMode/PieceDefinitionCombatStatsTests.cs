using DeadManZone.Core.Board;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class PieceDefinitionCombatStatsTests
    {
        [Test]
        public void PieceDefinition_DefaultsToMediumBaseline()
        {
            var piece = TestPieces.RifleSquad();
            Assert.AreEqual(AttackSpeedTier.Medium, piece.AttackSpeed);
            Assert.AreEqual(AttackRangeTier.Medium, piece.AttackRange);
            Assert.AreEqual(MovementSpeedTier.Medium, piece.MovementSpeed);
            Assert.AreEqual(ArmorType.Light, piece.ArmorType);
            Assert.AreEqual(AttackType.Ballistic, piece.AttackType);
            Assert.AreEqual(GrantedAbility.None, piece.GrantedAbility);
            Assert.AreEqual("iron_vanguard", piece.FactionId);
        }
    }
}
