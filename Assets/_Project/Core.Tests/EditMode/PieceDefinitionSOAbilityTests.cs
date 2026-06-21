using DeadManZone.Core.Tags;
using DeadManZone.Data;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class PieceDefinitionSOAbilityTests
    {
        [Test]
        public void ToCore_MergesCatalogAndInlineAbilities()
        {
            var pieceSo = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            var catalog = ScriptableObject.CreateInstance<AbilityDefinitionSO>();
            catalog.id = "test_aura";
            catalog.cardDescription = "Adjacent allies move faster.";
            catalog.trigger = PieceAbilityTrigger.AdjacentAura;
            catalog.neighborFilter = NeighborFilter.Any;
            catalog.stat = SynergyStat.MoveChargePercent;
            catalog.modType = SynergyModType.Flat;
            catalog.magnitude = 5;

            pieceSo.catalogAbilities = new[] { catalog };
            pieceSo.customAbilities = new[]
            {
                new PieceAbilityInlineEntry
                {
                    id = "custom_fight_start",
                    cardDescription = "Deal bonus damage at fight start.",
                    trigger = PieceAbilityTrigger.FightStart,
                    neighborFilter = NeighborFilter.Any,
                    stat = SynergyStat.Damage,
                    modType = SynergyModType.Flat,
                    magnitude = 10
                }
            };

            var core = pieceSo.ToCore();

            Assert.AreEqual(2, core.Abilities.Count);
            Assert.AreEqual("test_aura", core.Abilities[0].Id);
            Assert.AreEqual("custom_fight_start", core.Abilities[1].Id);
            Assert.AreEqual(5, core.Abilities[0].Magnitude);
            Assert.AreEqual(10, core.Abilities[1].Magnitude);

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(pieceSo);
        }

        [Test]
        public void ToCore_WithNoAbilities_ReturnsEmptyList()
        {
            var pieceSo = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            pieceSo.id = "empty_piece";

            var core = pieceSo.ToCore();

            Assert.AreEqual(0, core.Abilities.Count);

            Object.DestroyImmediate(pieceSo);
        }
    }
}
