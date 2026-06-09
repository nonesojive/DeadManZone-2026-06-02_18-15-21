using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Data.UnitCreation;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public class UnitCreationValidatorTests
    {
        [Test]
        public void Validate_ValidDraft_PassesWithoutErrors()
        {
            var draft = UnitCreationDraft.CreateDefault();
            draft.id = "test_unit";
            draft.displayName = "Test Unit";
            draft.primary = GameTagIds.Infantry;
            draft.combatRole = GameTagIds.Assault;

            var result = UnitCreationValidator.Validate(draft, idExistsInProject: false, idRegisteredInDatabase: false);

            Assert.IsFalse(result.HasErrors);
        }

        [Test]
        public void Validate_DuplicateIdOnCreate_Fails()
        {
            var draft = UnitCreationDraft.CreateDefault();
            draft.id = "existing_unit";
            draft.displayName = "Existing";
            draft.primary = GameTagIds.Infantry;

            var result = UnitCreationValidator.Validate(draft, idExistsInProject: true, idRegisteredInDatabase: false);

            Assert.IsTrue(result.HasErrors);
        }

        [Test]
        public void Validate_MissingPrimary_Fails()
        {
            var draft = UnitCreationDraft.CreateDefault();
            draft.id = "no_primary";
            draft.displayName = "No Primary";
            draft.primary = string.Empty;

            var result = UnitCreationValidator.Validate(draft, idExistsInProject: false, idRegisteredInDatabase: false);

            Assert.IsTrue(result.HasErrors);
        }

        [Test]
        public void Validate_EmptyShape_Fails()
        {
            var draft = UnitCreationDraft.CreateDefault();
            draft.id = "no_shape";
            draft.displayName = "No Shape";
            draft.primary = GameTagIds.Infantry;
            draft.shapeCells = System.Array.Empty<UnityEngine.Vector2Int>();

            var result = UnitCreationValidator.Validate(draft, idExistsInProject: false, idRegisteredInDatabase: false);

            Assert.IsTrue(result.HasErrors);
        }
    }
}
