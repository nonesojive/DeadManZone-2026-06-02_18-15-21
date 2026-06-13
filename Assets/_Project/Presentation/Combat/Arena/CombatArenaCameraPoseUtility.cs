using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public struct CombatArenaCameraPose
    {
        public Vector3 WorldPosition;
        public Vector3 LookAt;
        public float FieldOfView;

        public float ElevationDegrees;
        public float AzimuthDegrees;
        public float Distance;

        public static CombatArenaCameraPose FromCamera(Camera camera, Vector3 lookAt)
        {
            var pose = new CombatArenaCameraPose
            {
                WorldPosition = camera.transform.position,
                LookAt = lookAt,
                FieldOfView = camera != null ? camera.fieldOfView : 45f
            };

            pose.SyncOrbitFromPosition();
            return pose;
        }

        public void SyncOrbitFromPosition()
        {
            var offset = WorldPosition - LookAt;
            Distance = offset.magnitude;
            if (Distance < 0.001f)
            {
                Distance = 0.001f;
                offset = new Vector3(0f, 0f, -Distance);
            }

            var direction = offset / Distance;
            ElevationDegrees = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg;
            AzimuthDegrees = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
        }

        public void ApplyOrbitToPosition()
        {
            float elevation = ElevationDegrees * Mathf.Deg2Rad;
            float azimuth = AzimuthDegrees * Mathf.Deg2Rad;
            var offset = new Vector3(
                Mathf.Cos(elevation) * Mathf.Cos(azimuth),
                Mathf.Sin(elevation),
                Mathf.Cos(elevation) * Mathf.Sin(azimuth)) * Distance;

            WorldPosition = LookAt + offset;
        }

        public void ApplyTo(Camera camera)
        {
            if (camera == null)
                return;

            camera.fieldOfView = FieldOfView;
            camera.transform.position = WorldPosition;
            camera.transform.LookAt(LookAt);
        }
    }
}
