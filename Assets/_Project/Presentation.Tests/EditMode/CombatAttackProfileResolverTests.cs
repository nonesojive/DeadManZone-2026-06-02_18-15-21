using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatAttackProfileResolverTests
    {
        [Test]
        public void BallisticInfantry_ReturnsRifleStandShoot()
        {
            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            piece.category = PieceCategory.Unit;
            piece.primary = GameTagIds.Infantry;
            piece.attackType = AttackType.Ballistic;

            var profile = CombatAttackProfileResolver.Resolve(piece);

            Assert.AreEqual(CombatAttackPresentationKind.InfantryRifle, profile.Kind);
            Assert.IsFalse(profile.UseForwardStep, "Gun units must not step toward target");
        }

        [Test]
        public void ExplosiveInfantry_ReturnsGrenadeThrow()
        {
            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            piece.category = PieceCategory.Unit;
            piece.primary = GameTagIds.Infantry;
            piece.attackType = AttackType.Explosive;

            var profile = CombatAttackProfileResolver.Resolve(piece);

            Assert.AreEqual(CombatAttackPresentationKind.InfantryGrenade, profile.Kind);
        }

        [Test]
        public void MeleeInfantry_UsesForwardStep()
        {
            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            piece.category = PieceCategory.Unit;
            piece.primary = GameTagIds.Infantry;
            piece.attackType = AttackType.Melee;

            var profile = CombatAttackProfileResolver.Resolve(piece);

            Assert.AreEqual(CombatAttackPresentationKind.InfantryMelee, profile.Kind);
            Assert.IsTrue(profile.UseForwardStep);
        }

        [Test]
        public void VehicleBallistic_ReturnsCannon()
        {
            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            piece.category = PieceCategory.Unit;
            piece.primary = GameTagIds.Vehicle;
            piece.attackType = AttackType.Ballistic;

            var profile = CombatAttackProfileResolver.Resolve(piece);

            Assert.AreEqual(CombatAttackPresentationKind.VehicleCannon, profile.Kind);
        }
    }
}
