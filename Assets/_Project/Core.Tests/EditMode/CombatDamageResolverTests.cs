using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatDamageResolverTests
    {
        [Test]
        public void Ballistic_BonusVsLightArmor()
        {
            var attacker = TestPieces.With(
                TestPieces.RifleSquad(),
                baseDamage: 100,
                attackType: AttackType.Ballistic);
            var defender = TestPieces.With(
                TestPieces.RifleSquad(),
                armorType: ArmorType.Light);

            int damage = CombatDamageResolver.ComputeDamage(attacker, defender, damageScale: 1f, armorBuffSteps: 0);
            Assert.AreEqual(125, damage);
        }

        [Test]
        public void Piercing_BonusVsHeavyArmor()
        {
            var attacker = TestPieces.With(
                TestPieces.RifleSquad(),
                baseDamage: 100,
                attackType: AttackType.Piercing);
            var defender = TestPieces.With(
                TestPieces.RifleSquad(),
                armorType: ArmorType.Heavy);

            int damage = CombatDamageResolver.ComputeDamage(attacker, defender, damageScale: 1f, armorBuffSteps: 0);
            Assert.AreEqual(94, damage);
        }

        [Test]
        public void ShieldAllies_BumpsArmorOneTier()
        {
            var attacker = TestPieces.With(
                TestPieces.RifleSquad(),
                baseDamage: 100,
                attackType: AttackType.Ballistic);
            var defender = TestPieces.With(
                TestPieces.RifleSquad(),
                armorType: ArmorType.Light);

            int buffed = CombatDamageResolver.ComputeDamage(attacker, defender, damageScale: 1f, armorBuffSteps: 1);
            Assert.Less(buffed, 125);
            Assert.AreEqual(85, buffed);
        }
    }
}
