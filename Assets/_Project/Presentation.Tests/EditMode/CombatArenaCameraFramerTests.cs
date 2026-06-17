using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tests;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatArenaCameraFramerTests
    {
        [Test]
        public void Frame_AutoWidth_FillsHorizontalViewport()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(
                BoardLayout.CreateHorizontalZones(
                    TestBoards.DefaultWidth,
                    6,
                    TestBoards.DefaultRearCols,
                    TestBoards.DefaultSupportCols,
                    specialTiles: new[]
                    {
                        new GridCoord(1, 4),
                        new GridCoord(4, 4),
                        new GridCoord(7, 4)
                    }));
            var config = ScriptableObject.CreateInstance<CombatArenaConfigSO>();
            config.autoFrameWidth = true;
            config.autoFrameVerticalPosition = true;
            config.horizontalViewportPadding = 0f;
            config.boardVerticalViewportCenter = 0.50f;
            config.autoFrameVerticalFill = true;
            config.verticalViewportFill = 0.58f;
            config.cameraDistanceScale = 1f;
            config.cameraElevationDegrees = 50f;
            config.cameraAzimuthDegrees = 270f;
            config.fieldOfView = 38f;
            config.cellWidth = 1.8f;
            config.cellDepth = 1.8f;

            var cameraGo = new GameObject("TestArenaCamera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.aspect = 16f / 9f;

            try
            {
                CombatArenaCameraFramer.Frame(camera, layout, config);
                var points = CombatArenaCameraFramer.GetGroundSamplePoints(layout, config.cellWidth, config.cellDepth);
                float span = CombatArenaCameraFramer.MeasureHorizontalViewportSpan(camera, points);

                float centerY = CombatArenaCameraFramer.MeasureVerticalViewportCenter(camera, points);

                Assert.Greater(span, 0.95f, "Board should nearly fill the horizontal viewport.");
                Assert.Less(span, 1.01f, "Board should not exceed the horizontal viewport.");
                Assert.That(centerY, Is.EqualTo(0.50f).Within(0.04f), "Board should sit in the Top Troops vertical band.");
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Frame_CombatWidthBoard_FillsViewportWithProductionDefaults()
        {
            var layout = BattlefieldLayout.FromPlayerBoard(
                BoardLayout.CreateHorizontalZones(
                    TestBoards.DefaultWidth,
                    TestBoards.DefaultHeight,
                    TestBoards.DefaultRearCols,
                    TestBoards.DefaultSupportCols,
                    specialTiles: new GridCoord[0]));
            Assert.AreEqual(
                TestBoards.DefaultWidth + CombatBattlefieldConfig.NeutralColumnCount + TestBoards.DefaultWidth,
                layout.TotalWidth,
                "Combat layout should include neutral no-man's-land columns.");

            var config = ScriptableObject.CreateInstance<CombatArenaConfigSO>();
            config.autoFrameWidth = true;
            config.autoFrameVerticalPosition = true;
            config.autoFrameVerticalFill = true;
            config.boardVerticalViewportCenter = 0.50f;
            config.verticalViewportFill = 0.58f;
            config.cameraDistanceScale = 0.92f;
            config.cameraElevationDegrees = 50f;
            config.cameraAzimuthDegrees = 270f;
            config.fieldOfView = 36f;
            config.cellWidth = 1.8f;
            config.cellDepth = 1.8f;

            var cameraGo = new GameObject("TestArenaCamera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.aspect = 16f / 9f;

            try
            {
                CombatArenaCameraFramer.Frame(camera, layout, config);
                var points = CombatArenaCameraFramer.GetGroundSamplePoints(layout, config.cellWidth, config.cellDepth);
                float horizontalSpan = CombatArenaCameraFramer.MeasureHorizontalViewportSpan(camera, points);
                float verticalSpan = CombatArenaCameraFramer.MeasureVerticalViewportSpan(camera, points);
                float centerY = CombatArenaCameraFramer.MeasureVerticalViewportCenter(camera, points);

                Assert.Greater(horizontalSpan, 0.94f, "Full combat board should nearly fill horizontal viewport.");
                Assert.Less(horizontalSpan, 1.02f);
                Assert.Greater(verticalSpan, 0.50f, "Vertical fill should frame most of the trench depth.");
                Assert.That(centerY, Is.EqualTo(0.50f).Within(0.05f));
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
                Object.DestroyImmediate(config);
            }
        }
    }
}
