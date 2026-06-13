using System;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class AttackArmorMatrixTests
    {
        [Test]
        public void Catalog_DefinesProfileForEveryCombatAttackType()
        {
            foreach (AttackType attackType in Enum.GetValues(typeof(AttackType)))
            {
                if (attackType == AttackType.None)
                    continue;

                Assert.NotNull(
                    AttackTypeProfileCatalog.Get(attackType),
                    $"Missing attack profile for {attackType}.");
            }
        }

        [Test]
        public void ArmorMatrix_EveryAttackAndArmorCombo_HasPositiveMultiplier()
        {
            foreach (AttackType attackType in Enum.GetValues(typeof(AttackType)))
            {
                if (attackType == AttackType.None)
                    continue;

                foreach (ArmorType armorType in Enum.GetValues(typeof(ArmorType)))
                {
                    float multiplier = AttackTypeProfileCatalog.GetArmorMatrixMultiplier(attackType, armorType);
                    Assert.Greater(
                        multiplier,
                        0f,
                        $"{attackType} vs {armorType} should have a defined multiplier > 0.");
                }
            }
        }

        [Test]
        public void ComputeDamage_EveryAttackAndArmorCombo_DealsAtLeastOneDamage()
        {
            foreach (AttackType attackType in Enum.GetValues(typeof(AttackType)))
            {
                if (attackType == AttackType.None)
                    continue;

                foreach (ArmorType armorType in Enum.GetValues(typeof(ArmorType)))
                {
                    var defender = TestPieces.With(
                        TestPieces.CreateUnit(
                            "vehicle_target",
                            primary: GameTagIds.Vehicle,
                            systemTag: GameTagIds.Combatant),
                        armorType: armorType);
                    var attacker = TestPieces.With(
                        TestPieces.RifleSquad(),
                        baseDamage: 10,
                        attackType: attackType);

                    int damage = CombatDamageResolver.ComputeDamage(attacker, defender, 1f, 0);
                    Assert.GreaterOrEqual(
                        damage,
                        1,
                        $"{attackType} vs {armorType} should deal at least 1 damage.");
                }
            }
        }

        [Test]
        public void Ballistic_ArmorMatrix_MatchesExpectedMatchups()
        {
            Assert.AreEqual(1.25f, AttackTypeProfileCatalog.GetArmorMatrixMultiplier(AttackType.Ballistic, ArmorType.Medium));
            Assert.AreEqual(0.85f, AttackTypeProfileCatalog.GetArmorMatrixMultiplier(AttackType.Ballistic, ArmorType.Heavy));
            Assert.AreEqual(1.0f, AttackTypeProfileCatalog.GetArmorMatrixMultiplier(AttackType.Ballistic, ArmorType.Light));
        }

        [Test]
        public void FireAndMelee_ArmorMatrix_DefineLightAndHeavyMatchups()
        {
            Assert.AreEqual(1.20f, AttackTypeProfileCatalog.GetArmorMatrixMultiplier(AttackType.Fire, ArmorType.Light));
            Assert.AreEqual(0.85f, AttackTypeProfileCatalog.GetArmorMatrixMultiplier(AttackType.Fire, ArmorType.Heavy));
            Assert.AreEqual(1.25f, AttackTypeProfileCatalog.GetArmorMatrixMultiplier(AttackType.Melee, ArmorType.Light));
            Assert.AreEqual(0.80f, AttackTypeProfileCatalog.GetArmorMatrixMultiplier(AttackType.Melee, ArmorType.Heavy));
        }
    }
}
