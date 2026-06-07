using DeadManZone.Core.Tags;
using DeadManZone.Data;
using DeadManZone.Data.Editor;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class DemoPieceTagMigrationTests
    {
        [Test]
        public void RifleSquad_HasCategorizedTags()
        {
            TagContentMigrator.MigratePieceTags();

            var rifleSo = Resources.Load<PieceDefinitionSO>("DeadManZone/Pieces/rifle_squad");
            Assert.NotNull(rifleSo, "rifle_squad asset should be loadable from Resources.");
            Assert.AreEqual(GameTagIds.Infantry, rifleSo.primary);
            Assert.AreEqual(GameTagIds.Assault, rifleSo.combatRole);
            Assert.AreEqual(GameTagIds.Combatant, rifleSo.systemTag);

            var rifle = rifleSo.ToCore();
            Assert.IsTrue(PieceTagQueries.HasTag(rifle, GameTagIds.Vanguard));
        }
    }
}
