using DeadManZone.Core.Board;
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
            config.boardVerticalViewportCenter = 0.44f;
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
                Assert.That(centerY, Is.EqualTo(0.44f).Within(0.04f), "Board should sit in the Top Troops vertical band.");
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
                Object.DestroyImmediate(config);
            }
        }
    }
}
