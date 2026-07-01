using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatArena2DHelpersTests
    {
        [Test]
        public void SpriteResolver_FieldMedic_UsesDedicatedCombatSprite()
        {
            var piece = Resources.Load<PieceDefinitionSO>("DeadManZone/Pieces/field_medic");
            Assert.NotNull(piece, "field_medic piece asset missing from Resources");
            var resolved = CombatUnitSpriteResolver.Resolve(piece, CombatSide.Player);
            Assert.NotNull(resolved, "field_medic must resolve to a combat sprite via dedicated sprite or fallback.");
            if (piece.combatArenaSprite != null)
                Assert.AreSame(piece.combatArenaSprite, resolved);

            var tint = CombatUnitSpriteResolver.ResolveTint(piece, CombatSide.Player);
            if (piece.combatArenaSprite != null)
                Assert.AreEqual(Color.white, tint);
            else
                Assert.AreNotEqual(Color.clear, tint);
        }

        [Test]
        public void SortOrder_RenderQueue_BackRowStaysAboveGround()
        {
            int backRow = CombatArena2DSortOrder.RenderQueueFromWorldZ(7f);
            int frontRow = CombatArena2DSortOrder.RenderQueueFromWorldZ(-7f);
            Assert.Greater(backRow, CombatArena2DSortOrder.GroundRenderQueue);
            Assert.Greater(frontRow, backRow);
        }

        [Test]
        public void SortOrder_LowerWorldZ_DrawsInFront()
        {
            int south = CombatArena2DSortOrder.FromWorldZ(-2f);
            int north = CombatArena2DSortOrder.FromWorldZ(2f);
            Assert.Greater(south, north);
        }

        [Test]
        public void ProjectileArc_Midpoint_RisesAboveEndpoints()
        {
            var from = new Vector3(0f, 0f, -2f);
            var to = new Vector3(0f, 0f, 2f);
            float midY = CombatArena2DProjectileArc.MidpointHeight(from, to, arcHeight: 0.8f);
            Assert.Greater(midY, from.y);
            Assert.Greater(midY, to.y);
        }

        [Test]
        public void SpriteResolver_PrefersCombatArenaSprite_ThenSilhouette_ThenIcon()
        {
            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            piece.category = PieceCategory.Unit;
            piece.combatRole = GameTagIds.Assault;

            var dedicated = Sprite.Create(CreateTestTexture(64, 64), new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 64f);
            piece.combatArenaSprite = dedicated;
            Assert.AreSame(dedicated, CombatUnitSpriteResolver.Resolve(piece, CombatSide.Player));

            piece.combatArenaSprite = null;
            var icon = Sprite.Create(CreateTestTexture(64, 64), new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 64f);
            piece.icon = icon;

            CombatArena2DSilhouetteArt.ClearCacheForTests();
            var assaultSilhouette = CombatArena2DSilhouetteArt.ForRole(CombatArena2DSilhouetteRole.Assault);
            if (assaultSilhouette != null)
                Assert.AreSame(assaultSilhouette, CombatUnitSpriteResolver.Resolve(piece, CombatSide.Player));
            else
                Assert.AreSame(icon, CombatUnitSpriteResolver.Resolve(piece, CombatSide.Player));

            piece.icon = null;
            Assert.IsNotNull(CombatUnitSpriteResolver.Resolve(piece, CombatSide.Player));
        }

        [Test]
        public void PlaceholderSprites_WhitePixel_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var sprite = CombatArena2DPlaceholderSprites.WhitePixel;
                Assert.NotNull(sprite);
            });
        }

        [Test]
        public void OrthographicFramer_SetsOrthographicSizeFromLayout()
        {
            var camGo = new GameObject("TestCam");
            var camera = camGo.AddComponent<Camera>();
            camera.aspect = 16f / 9f;

            var config = ScriptableObject.CreateInstance<CombatArenaConfigSO>();
            config.cellWidth = 2f;
            config.cellDepth = 2f;
            config.cameraDistanceScale = 1f;
            config.orthoCameraElevationDegrees = 52f;
            config.orthoCameraAzimuthDegrees = 270f;

            var layout = BattlefieldLayout.FromPlayerBoard(TestBoards.Layout);
            CombatArenaOrthographicFramer.Frame(camera, layout, config);

            Assert.IsTrue(camera.orthographic);
            Assert.Greater(camera.orthographicSize, 0f);

            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void BattlefieldView_LightCheckerCell_AlternatesByCoord()
        {
            Assert.IsTrue(CombatArena2DBattlefieldView.IsLightCheckerCell(0, 0));
            Assert.IsFalse(CombatArena2DBattlefieldView.IsLightCheckerCell(1, 0));
            Assert.IsFalse(CombatArena2DBattlefieldView.IsLightCheckerCell(0, 1));
            Assert.IsTrue(CombatArena2DBattlefieldView.IsLightCheckerCell(1, 1));
        }

        [Test]
        public void SpriteMaterial_ComputesBackdropTileRepeat_FromWorldSize()
        {
            var texture = CreateTestTexture(256, 256);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 256, 256), Vector2.one * 0.5f, 64f);
            var repeat = CombatArena2DSpriteMaterial.ComputeTileRepeat(sprite, new Vector2(36f, 18f));
            Assert.AreEqual(9f, repeat.x, 0.01f);
            Assert.AreEqual(4.5f, repeat.y, 0.01f);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void SpriteResolver_BuildingsSkipUnitSilhouettes()
        {
            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            piece.category = PieceCategory.Building;
            piece.combatRole = GameTagIds.Assault;
            var icon = Sprite.Create(CreateTestTexture(64, 64), new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 64f);
            piece.icon = icon;

            CombatArena2DSilhouetteArt.ClearCacheForTests();
            Assert.AreSame(icon, CombatUnitSpriteResolver.Resolve(piece, CombatSide.Player));
        }

        [Test]
        public void VfxArt_SlicesHorizontalStripIntoFrames()
        {
            var texture = CreateTestTexture(256, 64);
            var sheet = Sprite.Create(texture, new Rect(0, 0, 256, 64), new Vector2(0.5f, 0.5f), 64f);
            var frames = CombatArena2DVfxArt.SliceStrip(sheet, 4);
            Assert.AreEqual(4, frames.Length);
            Assert.AreEqual(64f, frames[0].rect.width, 0.01f);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void BattlefieldView_HorizonSkyColor_BlendsGridPalette()
        {
            var config = ScriptableObject.CreateInstance<CombatArenaConfigSO>();
            config.gridLightCellColor = new Color(0.72f, 0.56f, 0.38f);
            config.gridDarkCellColor = new Color(0.5f, 0.37f, 0.26f);
            var sky = CombatArena2DBattlefieldView.ResolveHorizonSkyColor(config);
            Assert.Greater(sky.r, config.gridDarkCellColor.r);
            Assert.Less(sky.r, config.gridLightCellColor.r);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void SpriteQuad_GroundBottomOffset_PlacesTextureBottomAtAnchor()
        {
            var texture = CreateTestTexture(64, 128);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 64, 128), new Vector2(0.5f, 0.15f), 64f);
            var offset = CombatArena2DSpriteQuad.GroundBottomOffset(sprite, 1f);
            Assert.AreEqual(1f, offset.y, 0.001f);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void SpriteQuad_PivotCenterOffset_PlacesPivotAtFeet()
        {
            var texture = CreateTestTexture(64, 128);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 64, 128), new Vector2(0.5f, 0.15f), 64f);
            var offset = CombatArena2DSpriteQuad.PivotCenterOffset(sprite, 1f);
            Assert.AreEqual(0f, offset.x, 0.001f);
            Assert.AreEqual(0.7f, offset.y, 0.001f);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PresentationMode_2DIsCanonical()
        {
            var config = ScriptableObject.CreateInstance<CombatArenaConfigSO>();
            Assert.IsTrue(CombatArenaPresentationMode.IsTopTroops2D(config));
            Object.DestroyImmediate(config);
        }

        private static Texture2D CreateTestTexture(int width, int height)
        {
            var texture = new Texture2D(width, height);
            texture.SetPixels(new Color[width * height]);
            texture.Apply();
            return texture;
        }
    }
}
