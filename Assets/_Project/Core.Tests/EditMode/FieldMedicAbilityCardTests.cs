using DeadManZone.Core.Tags;
using DeadManZone.Data;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class FieldMedicAbilityCardTests
    {
        [Test]
        public void FieldMedic_FromResources_IncludesCatalogAbilityLine()
        {
            var pieceSo = Resources.Load<PieceDefinitionSO>("DeadManZone/Pieces/field_medic");
            Assert.NotNull(pieceSo, "field_medic piece asset missing from Resources");

            var piece = pieceSo.ToCore();
            Assert.Greater(piece.Abilities.Count, 0, "field_medic should have catalog abilities assigned");

            var model = PieceCardViewModelBuilder.Build(piece);

            Assert.That(
                model.AbilityLines,
                Does.Contain("Adjacent infantry gain +1 armor."),
                "Medic aura should appear in AbilityLines, not tag chips");
        }
    }
}
