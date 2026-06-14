using DeadManZone.Data;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatArenaArtCoverageTests
    {
        [Test]
        public void SandboxArtCatalog_UnitsAndHybridsHaveArenaPrefabs()
        {
            var catalog = SandboxArtCatalogSO.LoadFromResources();
            Assert.NotNull(catalog, "SandboxArtCatalog missing from Resources/DeadManZone/");

            foreach (var entry in catalog.entries)
            {
                var piece = LoadPiece(entry.pieceId);
                Assert.NotNull(piece, $"Catalog entry '{entry.pieceId}' has no matching PieceDefinitionSO");

                if (!SandboxArtRoster.RequiresCombatArenaPrefab(piece.category))
                    continue;

                Assert.IsFalse(string.IsNullOrEmpty(entry.combatArenaPrefabPath),
                    $"Catalog entry '{entry.pieceId}' ({piece.category}) needs combatArenaPrefabPath");

                Assert.NotNull(piece.combatArenaPrefab,
                    $"Piece '{entry.pieceId}' ({piece.category}) needs combatArenaPrefab — run Apply Sandbox Art Pass");
            }
        }

        [Test]
        public void SandboxArtCatalog_FactionPiecesAreRegistered()
        {
            var catalog = SandboxArtCatalogSO.LoadFromResources();
            Assert.NotNull(catalog);

            var factionPieceIds = new[]
            {
                "ironmarch_rifle", "wraith_stalker", "wraith_phantom", "wraith_bombard",
                "phantom_agent", "resonance_cannon", "toxin_launcher", "scrap_rig",
                "crimson_elite", "crimson_tank", "crimson_artillery", "sand_raider",
                "dust_hq", "echo_hq", "signal_relay"
            };

            foreach (var pieceId in factionPieceIds)
                Assert.IsTrue(catalog.TryGetEntry(pieceId, out _), $"Catalog missing faction entry for '{pieceId}'");
        }

        private static PieceDefinitionSO LoadPiece(string pieceId) =>
            Resources.Load<PieceDefinitionSO>($"DeadManZone/Pieces/{pieceId}");
    }
}
