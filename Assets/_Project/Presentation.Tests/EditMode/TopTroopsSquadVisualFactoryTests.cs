using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class TopTroopsSquadVisualFactoryTests
    {
        [Test]
        public void BuildSquad_AssaultRole_CreatesSoldierMeshes()
        {
            var root = new GameObject("SquadRoot");
            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            piece.combatRole = GameTagIds.Assault;
            piece.categoryTint = new Color(0.2f, 0.35f, 0.55f);

            try
            {
                TopTroopsSquadVisualFactory.BuildSquad(root.transform, piece, CombatSide.Player);
                Assert.Greater(CountRenderers(root.transform), 0);
            }
            finally
            {
                Object.DestroyImmediate(piece);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BuildSquad_ArtilleryRole_CreatesArtilleryPiece()
        {
            var root = new GameObject("SquadRoot");
            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            piece.combatRole = GameTagIds.Artillery;
            piece.categoryTint = Color.gray;

            try
            {
                TopTroopsSquadVisualFactory.BuildSquad(root.transform, piece, CombatSide.Enemy);
                var artillery = root.transform.Find("ArtilleryBase");
                Assert.NotNull(artillery);
            }
            finally
            {
                Object.DestroyImmediate(piece);
                Object.DestroyImmediate(root);
            }
        }

        private static int CountRenderers(Transform root)
        {
            int count = 0;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                count++;
            return count;
        }
    }
}
