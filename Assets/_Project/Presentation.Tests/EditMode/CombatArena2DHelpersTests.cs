using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using DeadManZone.Data;
using DeadManZone.Presentation.Board;
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
        public void SpriteResolver_BulwarkSquad_UsesDedicatedCombatSpriteAndAnimations()
        {
            var piece = Resources.Load<PieceDefinitionSO>("DeadManZone/Pieces/bulwark_squad");
            Assert.NotNull(piece, "bulwark_squad piece asset missing from Resources");
            Assert.NotNull(piece.combatArena2DAnimations, "bulwark_squad needs a combat 2D animation set");
            Assert.IsTrue(piece.combatArena2DAnimations.HasAny);
            // combatvisualv2 AutoSprite sheets: 7×7 grid @ 512px cells = 49 frames.
            Assert.GreaterOrEqual(piece.combatArena2DAnimations.walk.frameCount, 49);
            Assert.AreEqual(7, piece.combatArena2DAnimations.walk.columns);

            var resolved = CombatUnitSpriteResolver.Resolve(piece, CombatSide.Player);
            Assert.NotNull(resolved);
            if (piece.combatArenaSprite != null)
                Assert.AreSame(piece.combatArenaSprite, resolved);
        }

        [Test]
        public void SpriteResolver_ConscriptRifleman_UsesFullResolutionAnimationLayout()
        {
            var piece = Resources.Load<PieceDefinitionSO>("DeadManZone/Pieces/conscript_rifleman");
            Assert.NotNull(piece, "conscript_rifleman piece asset missing from Resources");
            Assert.NotNull(piece.combatArena2DAnimations, "conscript_rifleman needs a combat 2D animation set");
            Assert.AreEqual(49, piece.combatArena2DAnimations.idle.frameCount);
            Assert.AreEqual(7, piece.combatArena2DAnimations.idle.columns);
            Assert.AreEqual(49, piece.combatArena2DAnimations.walk.frameCount);
            Assert.AreEqual(7, piece.combatArena2DAnimations.walk.columns);
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
        public void VisualScale_NormalizesDifferentSpriteHeightsToSharedInfantryTarget()
        {
            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            piece.id = "conscript_rifleman";

            var tinyTexture = CreateTestTexture(96, 160);
            var tallTexture = CreateTestTexture(256, 512);
            var tiny = Sprite.Create(tinyTexture, new Rect(0, 0, 96, 160), new Vector2(0.5f, 0.05f), 256f);
            var tall = Sprite.Create(tallTexture, new Rect(0, 0, 256, 512), new Vector2(0.5f, 0.05f), 256f);

            float tinyScale = CombatUnit2DVisualScale.ResolveUniformScale(piece, tiny);
            float tallScale = CombatUnit2DVisualScale.ResolveUniformScale(piece, tall);
            float tinyHeight = tiny.rect.height / tiny.pixelsPerUnit * tinyScale;
            float tallHeight = tall.rect.height / tall.pixelsPerUnit * tallScale;

            Assert.AreEqual(tinyHeight, tallHeight, 0.001f);
            // Infantry target height (CombatUnit2DVisualScale.InfantryHeight).
            Assert.AreEqual(1.85f, tinyHeight, 0.001f);

            Object.DestroyImmediate(tinyTexture);
            Object.DestroyImmediate(tallTexture);
            Object.DestroyImmediate(piece);
        }

        [Test]
        public void VisualScale_NormalizesVisibleAlphaHeightNotTransparentPadding()
        {
            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            piece.id = "conscript_rifleman";

            var paddedTexture = CreateTestTexture(256, 512);
            var filledTexture = CreateTestTexture(256, 512);
            PaintRect(paddedTexture, 96, 140, 64, 180, Color.white);
            PaintRect(filledTexture, 96, 40, 64, 360, Color.white);
            paddedTexture.Apply();
            filledTexture.Apply();

            var padded = Sprite.Create(paddedTexture, new Rect(0, 0, 256, 512), new Vector2(0.5f, 0.05f), 256f);
            var filled = Sprite.Create(filledTexture, new Rect(0, 0, 256, 512), new Vector2(0.5f, 0.05f), 256f);

            float paddedScale = CombatUnit2DVisualScale.ResolveUniformScale(piece, padded);
            float filledScale = CombatUnit2DVisualScale.ResolveUniformScale(piece, filled);
            float paddedVisibleHeight = CombatArena2DSpriteMetrics.VisibleHeightUnits(padded) * paddedScale;
            float filledVisibleHeight = CombatArena2DSpriteMetrics.VisibleHeightUnits(filled) * filledScale;

            Assert.AreEqual(paddedVisibleHeight, filledVisibleHeight, 0.001f);
            // Infantry target height (CombatUnit2DVisualScale.InfantryHeight).
            Assert.AreEqual(1.85f, paddedVisibleHeight, 0.001f);

            Object.DestroyImmediate(paddedTexture);
            Object.DestroyImmediate(filledTexture);
            Object.DestroyImmediate(piece);
        }

        [Test]
        public void VisualScale_ReusingIdleScaleOnLargerCrop_Pops_ReResolveKeepsHeight()
        {
            // Per-strip content crops (idle tight, shoot/die wide) fed SetFrame with the
            // idle build-time scale → units grew/shrank on state swap. Locking quad height
            // via rect (what SetFrame actually sizes) keeps world size constant.
            var idleTexture = CreateTestTexture(256, 256);
            var shootTexture = CreateTestTexture(256, 256);
            PaintRect(idleTexture, 96, 60, 64, 140, Color.white);
            PaintRect(shootTexture, 40, 20, 176, 220, Color.white);
            idleTexture.Apply();
            shootTexture.Apply();

            // Different rect heights simulate different shared content crops per strip.
            var idle = Sprite.Create(idleTexture, new Rect(80, 50, 96, 160), new Vector2(0.5f, 0.05f), 64f);
            var shoot = Sprite.Create(shootTexture, new Rect(30, 10, 196, 236), new Vector2(0.5f, 0.05f), 64f);

            float idleScale = 1.2f;
            float targetQuadHeight = CombatUnit2DVisualScale.RectWorldHeight(idle, idleScale);
            float poppedHeight = CombatUnit2DVisualScale.RectWorldHeight(shoot, idleScale);
            float fixedScale = CombatUnit2DVisualScale.ResolveScaleForRectHeight(shoot, targetQuadHeight);
            float fixedHeight = CombatUnit2DVisualScale.RectWorldHeight(shoot, fixedScale);

            Assert.Greater(poppedHeight, targetQuadHeight + 0.2f, "idle scale on a larger crop must pop (documents the bug)");
            Assert.AreEqual(targetQuadHeight, fixedHeight, 0.001f, "rect-height lock must hold quad world height");

            Object.DestroyImmediate(idleTexture);
            Object.DestroyImmediate(shootTexture);
        }

        [Test]
        public void VisualScale_AlphaBasedScaleOnSparseDieFrame_ExplodesRect_RectLockDoesNot()
        {
            // Die strips share one large crop; late frames leave most of it empty.
            // ResolveUniformScale (alpha height) then SetFrame(rect) → gigantic corpse.
            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            piece.id = "conscript_rifleman";

            var texture = CreateTestTexture(256, 256);
            // Small crumpled body inside a large crop rect (shared die bounds).
            PaintRect(texture, 100, 40, 56, 48, Color.white);
            texture.Apply();

            var sparseDie = Sprite.Create(texture, new Rect(20, 20, 216, 216), new Vector2(0.5f, 0.05f), 64f);
            float targetQuadHeight = 2.5f; // ~idle quad world height

            float alphaScale = CombatUnit2DVisualScale.ResolveUniformScale(piece, sparseDie);
            float explodedHeight = CombatUnit2DVisualScale.RectWorldHeight(sparseDie, alphaScale);
            float rectScale = CombatUnit2DVisualScale.ResolveScaleForRectHeight(sparseDie, targetQuadHeight);
            float lockedHeight = CombatUnit2DVisualScale.RectWorldHeight(sparseDie, rectScale);

            Assert.Greater(explodedHeight, targetQuadHeight * 2f, "alpha scale on sparse die frame must explode (documents the bug)");
            Assert.AreEqual(targetQuadHeight, lockedHeight, 0.001f);

            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(piece);
        }

        [Test]
        public void PieceArtResolver_FreshIconOverridesCompleteCellArt()
        {
            var source = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            var iconTexture = CreateTestTexture(64, 64);
            var cellTexture = CreateTestTexture(64, 64);
            source.icon = Sprite.Create(iconTexture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 64f);
            var cellSprite = Sprite.Create(cellTexture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 64f);
            source.cellSprites = new[]
            {
                new PieceCellSprite { localCell = Vector2Int.zero, sprite = cellSprite },
                new PieceCellSprite { localCell = Vector2Int.right, sprite = cellSprite }
            };

            var definition = new PieceDefinition
            {
                Shape = new PieceShape(new[] { new GridCoord(0, 0), new GridCoord(1, 0) })
            };

            Assert.IsTrue(PieceArtResolver.AllCellsHaveSprites(source, new GridCoord(0, 0), PieceRotation.R0, definition));
            Assert.IsTrue(PieceArtResolver.ShouldUseFootprintIcon(source, new GridCoord(0, 0), PieceRotation.R0, definition));

            Object.DestroyImmediate(source.icon.texture);
            Object.DestroyImmediate(cellSprite.texture);
            Object.DestroyImmediate(source);
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
            Assert.AreEqual(1f, offset.y, 0.001f);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void SpriteQuad_PivotCenterOffset_UsesVisibleAlphaBottomAsFeet()
        {
            var texture = CreateTestTexture(64, 128);
            PaintRect(texture, 20, 24, 24, 80, Color.white);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 64, 128), new Vector2(0.5f, 0.15f), 64f);

            var offset = CombatArena2DSpriteQuad.PivotCenterOffset(sprite, 1f);

            Assert.AreEqual(0.625f, offset.y, 0.001f);
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

        private static void PaintRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                    texture.SetPixel(px, py, color);
            }
        }
    }
}
