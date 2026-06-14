using System.Linq;
using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class SandboxArtCoverageTests
    {
        [Test]
        public void SandboxRoster_AllPiecesHaveIcons()
        {
            var database = ContentDatabase.Load();
            Assert.NotNull(database);

            foreach (var pieceId in SandboxArtRoster.AllPieceIds)
            {
                var piece = FindPiece(database, pieceId);
                Assert.NotNull(piece, $"Missing piece '{pieceId}'");
                Assert.NotNull(piece.icon, $"Piece '{pieceId}' has no icon — run Apply Sandbox Art Pass");
            }
        }

        [Test]
        public void SandboxRoster_UnitsAndHybridsHaveArenaPrefabs()
        {
            var database = ContentDatabase.Load();
            Assert.NotNull(database);

            foreach (var pieceId in SandboxArtRoster.AllPieceIds)
            {
                var piece = FindPiece(database, pieceId);
                Assert.NotNull(piece);
                if (!SandboxArtRoster.RequiresCombatArenaPrefab(piece.category))
                    continue;

                Assert.NotNull(piece.combatArenaPrefab,
                    $"Piece '{pieceId}' ({piece.category}) needs combatArenaPrefab");
            }
        }

        [Test]
        public void SandboxArtCatalog_HasEntryForEveryRosterPiece()
        {
            var catalog = SandboxArtCatalogSO.LoadFromResources();
            Assert.NotNull(catalog, "SandboxArtCatalog missing from Resources/DeadManZone/");

            foreach (var pieceId in SandboxArtRoster.AllPieceIds)
                Assert.IsTrue(catalog.TryGetEntry(pieceId, out _), $"Catalog missing entry for '{pieceId}'");
        }

        [Test]
        public void SandboxArtCatalog_NoLegacyThirdPartyPaths()
        {
            var catalog = SandboxArtCatalogSO.LoadFromResources();
            Assert.NotNull(catalog);

            var forbidden = new[] { "Toon_Soldiers", "RTS_Modern", "BunkerSurvivalUI/Sprites/Icons" };
            foreach (var entry in catalog.entries)
            {
                foreach (var bad in forbidden)
                {
                    if (!string.IsNullOrEmpty(entry.combatArenaPrefabPath))
                        Assert.IsFalse(entry.combatArenaPrefabPath.Contains(bad),
                            $"Entry '{entry.pieceId}' combat prefab still references {bad}");

                    if (!string.IsNullOrEmpty(entry.iconAssetPath))
                        Assert.IsFalse(entry.iconAssetPath.Contains(bad),
                            $"Entry '{entry.pieceId}' icon still references {bad}");
                }
            }
        }

        private static PieceDefinitionSO FindPiece(ContentDatabase db, string id) =>
            db.Pieces.FirstOrDefault(p => p != null && p.id == id);
    }
}
