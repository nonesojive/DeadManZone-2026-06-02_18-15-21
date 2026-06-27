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
            Assert.NotNull(piece.combatArenaSprite, "field_medic should reference combat2d_unit_field_medic");

            Assert.AreSame(piece.combatArenaSprite, CombatUnitSpriteResolver.Resolve(piece, CombatSide.Player));
            Assert.AreEqual(Color.white, CombatUnitSpriteResolver.ResolveTint(piece, CombatSide.Player));
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
            piece.combatRole = GameTagIds.Assault;

            var dedicated = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 64f);
            piece.combatArenaSprite = dedicated;
            Assert.AreSame(dedicated, CombatUnitSpriteResolver.Resolve(piece, CombatSide.Player));

            piece.combatArenaSprite = null;
            var icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 64f);
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
            var sprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, 256, 256),
                Vector2.one * 0.5f,
                64f);
            var repeat = CombatArena2DSpriteMaterial.ComputeTileRepeat(sprite, new Vector2(36f, 18f));
            Assert.AreEqual(9f, repeat.x, 0.01f);
            Assert.AreEqual(4.5f, repeat.y, 0.01f);
        }

        [Test]
        public void SpriteResolver_BuildingsSkipUnitSilhouettes()
        {
            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            piece.category = PieceCategory.Building;
            piece.combatRole = GameTagIds.Assault;
            var icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 64f);
            piece.icon = icon;

            CombatArena2DSilhouetteArt.ClearCacheForTests();
            Assert.AreSame(icon, CombatUnitSpriteResolver.Resolve(piece, CombatSide.Player));
        }

        [Test]
        public void VfxArt_SlicesHorizontalStripIntoFrames()
        {
            var sheet = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, 256, 64),
                new Vector2(0.5f, 0.5f),
                64f);
            var frames = CombatArena2DVfxArt.SliceStrip(sheet, 4);
            Assert.AreEqual(4, frames.Length);
            Assert.AreEqual(64f, frames[0].rect.width, 0.01f);
        }

        [Test]
        public void VfxArt_WiredStripsLoadFromResources()
        {
            CombatArena2DVfxArt.ClearCacheForTests();
            var art = CombatArena2DVfxArt.Load();
            Assert.IsNotNull(art);
            Assert.IsTrue(art.HasAny);
            Assert.AreEqual(4, CombatArena2DVfxArt.RifleImpactFrames.Length);
        }

        [Test]
        public void SilhouetteArt_WiredSpritesLoadFromResources()
        {
            CombatArena2DSilhouetteArt.ClearCacheForTests();
            var art = CombatArena2DSilhouetteArt.Load();
            Assert.IsNotNull(art);
            Assert.IsTrue(art.HasAny);
            Assert.IsNotNull(CombatArena2DSilhouetteArt.ForRole(CombatArena2DSilhouetteRole.Assault));
            Assert.IsNotNull(CombatArena2DSilhouetteArt.ForRole(CombatArena2DSilhouetteRole.Generic));
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
        public void EnvironmentArt_WiredSpritesLoadFromResources()
        {
            CombatArena2DEnvironmentArt.ClearCacheForTests();
            Assert.IsNotNull(CombatArena2DEnvironmentArt.Load());
            Assert.IsTrue(CombatArena2DEnvironmentArt.HasGridArt);
            Assert.IsNotNull(CombatArena2DEnvironmentArt.UnitShadow);
        }

        [Test]
        public void SpriteMesh_ResolveUnitUvs_AlwaysReturnsFourCorners()
        {
            CombatArena2DSilhouetteArt.ClearCacheForTests();
            var sprite = CombatArena2DSilhouetteArt.ForRole(CombatArena2DSilhouetteRole.Assault);
            if (sprite == null)
                Assert.Ignore("Silhouette art not wired in test project.");

            var uvs = CombatArena2DSpriteMesh.ResolveUnitUvs(sprite);
            Assert.AreEqual(4, uvs.Length);
            Assert.Greater(uvs[1].x, uvs[0].x);
            Assert.Greater(uvs[2].y, uvs[0].y);
        }

        [Test]
        public void SpriteMaterial_CreateSprite_UsesAlphaAwareShader()
        {
            CombatArena2DSilhouetteArt.ClearCacheForTests();
            var sprite = CombatArena2DSilhouetteArt.ForRole(CombatArena2DSilhouetteRole.Assault);
            if (sprite == null)
                Assert.Ignore("Silhouette art not wired in test project.");

            var material = CombatArena2DSpriteMaterial.CreateSprite(sprite, Color.white, 2600);
            Assert.IsNotNull(material);
            Assert.IsTrue(material.shader.name.Contains("Sprites") || material.IsKeywordEnabled("_ALPHATEST_ON"));
        }

        [Test]
        public void SpriteQuad_GroundBottomOffset_PlacesTextureBottomAtAnchor()
        {
            var sprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, 64, 128),
                new Vector2(0.5f, 0.15f),
                64f);
            var offset = CombatArena2DSpriteQuad.GroundBottomOffset(sprite, 1f);
            Assert.AreEqual(1f, offset.y, 0.001f);
        }

        [Test]
        public void SpriteQuad_PivotCenterOffset_PlacesPivotAtFeet()
        {
            var sprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, 64, 128),
                new Vector2(0.5f, 0.15f),
                64f);
            var offset = CombatArena2DSpriteQuad.PivotCenterOffset(sprite, 1f);
            Assert.AreEqual(0f, offset.x, 0.001f);
            Assert.AreEqual(0.7f, offset.y, 0.001f);
        }

        [Test]
        public void PresentationMode_DetectsTopTroops2D()
        {
            var config = ScriptableObject.CreateInstance<CombatArenaConfigSO>();
            config.visualMode = CombatArenaVisualMode.Legacy3D;
            Assert.IsFalse(CombatArenaPresentationMode.IsTopTroops2D(config));

            config.visualMode = CombatArenaVisualMode.TopTroops2D;
            Assert.IsTrue(CombatArenaPresentationMode.IsTopTroops2D(config));

            Object.DestroyImmediate(config);
        }
    }
}
