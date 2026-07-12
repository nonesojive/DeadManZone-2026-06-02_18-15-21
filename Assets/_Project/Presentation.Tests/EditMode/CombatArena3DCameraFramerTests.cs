using System.Collections.Generic;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatArena3DCameraFramerTests
    {
        private GameObject _cameraGo;
        private Camera _camera;

        // The authored CombatArena3D home pose (Combat3DDemoSceneBootstrap.CreateCamera).
        private static readonly Vector3 Home = new(0f, 10f, -14f);

        [SetUp]
        public void SetUp()
        {
            _cameraGo = new GameObject("FramerTestCamera");
            _camera = _cameraGo.AddComponent<Camera>();
            _camera.fieldOfView = 42f;
            _camera.nearClipPlane = 0.3f;
            _camera.farClipPlane = 200f;
            _camera.aspect = 16f / 9f; // deterministic regardless of test-runner game view
            _cameraGo.transform.SetPositionAndRotation(Home, Quaternion.Euler(29f, 0f, 0f));
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_cameraGo);

        private static List<Vector3> WithHeads(params Vector3[] groundPoints)
        {
            var points = new List<Vector3>();
            foreach (var p in groundPoints)
            {
                points.Add(p);
                points.Add(p + Vector3.up * 2.2f);
            }

            return points;
        }

        private bool AllVisibleFrom(Vector3 cameraPosition, List<Vector3> points, float margin)
        {
            _camera.transform.position = cameraPosition;
            foreach (var p in points)
            {
                var vp = _camera.WorldToViewportPoint(p);
                if (vp.z <= _camera.nearClipPlane ||
                    vp.x < margin || vp.x > 1f - margin ||
                    vp.y < margin || vp.y > 1f - margin)
                    return false;
            }

            return true;
        }

        [Test]
        public void SmallCenterFight_KeepsAuthoredPose()
        {
            // Demo-sized 3v3 around mid-field — the authored framing already covers it.
            var points = WithHeads(
                new Vector3(-7.2f, 0f, -1.8f), new Vector3(-7.2f, 0f, 1.8f),
                new Vector3(7.2f, 0f, 0f));

            var framed = CombatArena3DCameraFramer.ComputeFramedPosition(_camera, Home, points);

            Assert.AreEqual(Home.y, framed.y, 0.001f, "no pullback needed → authored height kept");
            Assert.AreEqual(Home.z, framed.z, 0.001f, "no pullback needed → authored depth kept");
            Assert.AreEqual(0f, framed.x, 0.001f, "symmetric hull stays centered");
        }

        [Test]
        public void FullStripDeployment_PullsBackUntilEveryUnitIsFramed()
        {
            // Rear-corner support + far enemy corner — the case the authored pose clips.
            var points = WithHeads(
                new Vector3(-14.4f, 0f, -4.5f), new Vector3(-14.4f, 0f, 4.5f),
                new Vector3(14.4f, 0f, -4.5f), new Vector3(14.4f, 0f, 4.5f));

            var framed = CombatArena3DCameraFramer.ComputeFramedPosition(_camera, Home, points);

            Assert.IsTrue(AllVisibleFrom(framed, points, 0.05f),
                "framed pose must show every occupied point inside the margin");

            var backward = -_cameraGo.transform.forward;
            float pullback = Vector3.Dot(framed - new Vector3(0f, Home.y, Home.z), backward);
            Assert.Greater(pullback, 0.5f, "a full-strip deployment needs an actual pullback");
        }

        [Test]
        public void AsymmetricHull_ShiftsLaterallyTowardTheAction()
        {
            // Fight-1-like: player rear-left, enemy just right of center.
            var points = WithHeads(
                new Vector3(-14.4f, 0f, -2.7f),
                new Vector3(-10.8f, 0f, 0.9f),
                new Vector3(6.3f, 0f, -2.7f));

            var framed = CombatArena3DCameraFramer.ComputeFramedPosition(_camera, Home, points);

            Assert.AreEqual((-14.4f + 6.3f) * 0.5f, framed.x, 0.001f,
                "camera centers on the occupied hull, not the strip");
            Assert.IsTrue(AllVisibleFrom(framed, points, 0.05f));
        }

        [Test]
        public void CameraTransformIsRestored_WhenOnlyComputing()
        {
            var points = WithHeads(new Vector3(-14.4f, 0f, 0f), new Vector3(14.4f, 0f, 0f));
            CombatArena3DCameraFramer.ComputeFramedPosition(_camera, Home, points);

            Assert.AreEqual(Home, _camera.transform.position,
                "ComputeFramedPosition must not leave the probe position on the camera");
        }
    }
}
