using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatDamageResolverTests
    {
        [Test]
        public void Ballistic_StrongVsMediumArmor()
        {
            var attacker = TestPieces.With(TestPieces.RifleSquad(), baseDamage: 100, attackType: AttackType.Ballistic);
            var defender = TestPieces.With(TestPieces.RifleSquad(), armorType: ArmorType.Medium);
            int damage = CombatDamageResolver.ComputeDamage(attacker, defender, damageScale: 1f, armorBuffSteps: 0);
            Assert.AreEqual(106, damage);
        }

        [Test]
        public void Ballistic_WeakVsHeavyArmor()
        {
            var attacker = TestPieces.With(TestPieces.RifleSquad(), baseDamage: 100, attackType: AttackType.Ballistic);
            var defender = TestPieces.With(TestPieces.RifleSquad(), armorType: ArmorType.Heavy);
            int damage = CombatDamageResolver.ComputeDamage(attacker, defender, damageScale: 1f, armorBuffSteps: 0);
            Assert.AreEqual(59, damage);
        }

        [Test]
        public void Piercing_StrongVsHeavy_WeakVsLight()
        {
            var vsHeavy = CombatDamageResolver.ComputeDamage(
                TestPieces.With(TestPieces.RifleSquad(), baseDamage: 100, attackType: AttackType.Piercing),
                TestPieces.With(TestPieces.RifleSquad(), armorType: ArmorType.Heavy),
                1f,
                0);
            var vsLight = CombatDamageResolver.ComputeDamage(
                TestPieces.With(TestPieces.RifleSquad(), baseDamage: 100, attackType: AttackType.Piercing),
                TestPieces.With(TestPieces.RifleSquad(), armorType: ArmorType.Light),
                1f,
                0);
            Assert.AreEqual(94, vsHeavy);
            Assert.AreEqual(85, vsLight);
        }

        [Test]
        public void Shredding_StrongVsLight_WeakVsMedium()
        {
            var vsLight = CombatDamageResolver.ComputeDamage(
                TestPieces.With(TestPieces.RifleSquad(), baseDamage: 100, attackType: AttackType.Shredding),
                TestPieces.With(TestPieces.RifleSquad(), armorType: ArmorType.Light),
                1f,
                0);
            var vsMedium = CombatDamageResolver.ComputeDamage(
                TestPieces.With(TestPieces.RifleSquad(), baseDamage: 100, attackType: AttackType.Shredding),
                TestPieces.With(TestPieces.RifleSquad(), armorType: ArmorType.Medium),
                1f,
                0);
            Assert.AreEqual(125, vsLight);
            Assert.AreEqual(72, vsMedium);
        }

        [Test]
        public void Explosive_StrongVsStructurePrimary()
        {
            var attacker = TestPieces.With(TestPieces.RifleSquad(), baseDamage: 100, attackType: AttackType.Explosive);
            var structure = TestPieces.With(
                TestPieces.CreateUnit("nest", primary: GameTagIds.Structure, systemTag: GameTagIds.Combatant),
                armorType: ArmorType.Light);
            int damage = CombatDamageResolver.ComputeDamage(attacker, structure, 1f, 0);
            Assert.AreEqual(130, damage);
        }

        [Test]
        public void Gas_StrongVsInfantry_WeakVsBuilding()
        {
            var attacker = TestPieces.With(TestPieces.RifleSquad(), baseDamage: 100, attackType: AttackType.Gas);
            var infantry = TestPieces.CreateUnit("inf", primary: GameTagIds.Infantry, systemTag: GameTagIds.Combatant);
            var building = TestPieces.CreateUnit("depot", primary: GameTagIds.Building, systemTag: GameTagIds.NonCombatant);
            Assert.AreEqual(125, CombatDamageResolver.ComputeDamage(attacker, infantry, 1f, 0));
            Assert.AreEqual(85, CombatDamageResolver.ComputeDamage(attacker, building, 1f, 0));
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

            int unbuffed = CombatDamageResolver.ComputeDamage(attacker, defender, damageScale: 1f, armorBuffSteps: 0);
            int buffed = CombatDamageResolver.ComputeDamage(attacker, defender, damageScale: 1f, armorBuffSteps: 1);
            Assert.AreEqual(100, unbuffed);
            Assert.AreEqual(106, buffed);
        }
    }
}
