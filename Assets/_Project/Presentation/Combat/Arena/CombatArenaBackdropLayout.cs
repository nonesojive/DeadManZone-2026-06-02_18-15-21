using System.Collections.Generic;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Deterministic spawn layout for trench dressing, skyline, and atmosphere FX rings.</summary>
    public static class CombatArenaBackdropLayout
    {
        private const float DressingOffset = 2.8f;
        private const float SkylineOffset = 18f;
        private const float SkylineInnerScale = 0.55f;

        public static IReadOnlyList<CombatArenaBackdropSpawnPoint> BuildLayout(
            float halfWidth,
            float halfDepth,
            int seed,
            bool includeAtmosphereFx = false) =>
            BuildLayout(
                halfWidth,
                halfDepth,
                seed,
                CombatArenaBackdropCatalogLengths.FromLegacyCatalog(),
                includeAtmosphereFx);

        public static IReadOnlyList<CombatArenaBackdropSpawnPoint> BuildLayout(
            float halfWidth,
            float halfDepth,
            int seed,
            CombatArenaBackdropCatalogLengths catalogLengths,
            bool includeAtmosphereFx = false)
        {
            var rng = new System.Random(seed);
            var points = new List<CombatArenaBackdropSpawnPoint>(32);

            AddDressingRing(points, halfWidth, halfDepth, rng, catalogLengths.TrenchDressing);
            AddSkylineRing(points, halfWidth, halfDepth, rng, catalogLengths.Skyline);
            if (includeAtmosphereFx)
                AddAtmosphereFx(points, halfWidth, halfDepth, rng, catalogLengths.AtmosphereFx);

            return points;
        }

        private static void AddDressingRing(
            List<CombatArenaBackdropSpawnPoint> points,
            float halfWidth,
            float halfDepth,
            System.Random rng,
            int catalogLength)
        {
            float nearX = halfWidth + DressingOffset;
            float nearZ = halfDepth + DressingOffset;
            int safeLength = Mathf.Max(catalogLength, 1);

            var corners = new[]
            {
                new Vector3(-nearX, 0f, nearZ),
                new Vector3(0f, 0f, nearZ),
                new Vector3(nearX, 0f, nearZ),
                new Vector3(nearX, 0f, 0f),
                new Vector3(nearX, 0f, -nearZ),
                new Vector3(0f, 0f, -nearZ),
                new Vector3(-nearX, 0f, -nearZ),
                new Vector3(-nearX, 0f, 0f)
            };

            for (int i = 0; i < corners.Length; i++)
            {
                float jitterX = (float)(rng.NextDouble() * 0.8 - 0.4);
                float jitterZ = (float)(rng.NextDouble() * 0.8 - 0.4);
                var position = corners[i] + new Vector3(jitterX, 0f, jitterZ);
                float scale = 0.85f + (float)rng.NextDouble() * 0.35f;
                points.Add(new CombatArenaBackdropSpawnPoint(
                    CombatArenaBackdropRing.TrenchDressing,
                    position,
                    i * 45f + (float)rng.NextDouble() * 20f,
                    scale,
                    i % safeLength));
            }
        }

        private static void AddSkylineRing(
            List<CombatArenaBackdropSpawnPoint> points,
            float halfWidth,
            float halfDepth,
            System.Random rng,
            int catalogLength)
        {
            float farX = halfWidth + SkylineOffset;
            float farZ = halfDepth + SkylineOffset * 0.65f;
            int safeLength = Mathf.Max(catalogLength, 1);

            var skylineSlots = new[]
            {
                new Vector3(-farX, 0f, farZ),
                new Vector3(-farX * SkylineInnerScale, 0f, farZ + 2f),
                new Vector3(farX * SkylineInnerScale, 0f, farZ + 2f),
                new Vector3(farX, 0f, farZ),
                new Vector3(farX, 0f, -farZ * 0.65f),
                new Vector3(-farX, 0f, -farZ * 0.65f)
            };

            for (int i = 0; i < skylineSlots.Length; i++)
            {
                float scale = 1.1f + (float)rng.NextDouble() * 0.5f;
                points.Add(new CombatArenaBackdropSpawnPoint(
                    CombatArenaBackdropRing.Skyline,
                    skylineSlots[i],
                    (float)rng.NextDouble() * 360f,
                    scale,
                    i % safeLength));
            }
        }

        private static void AddAtmosphereFx(
            List<CombatArenaBackdropSpawnPoint> points,
            float halfWidth,
            float halfDepth,
            System.Random rng,
            int catalogLength)
        {
            if (catalogLength <= 0)
                return;

            for (int i = 0; i < 4; i++)
            {
                float x = (float)(rng.NextDouble() * 2.0 - 1.0) * (halfWidth + DressingOffset + 1f);
                float z = halfDepth + DressingOffset + (float)rng.NextDouble() * 3f;
                points.Add(new CombatArenaBackdropSpawnPoint(
                    CombatArenaBackdropRing.AtmosphereFx,
                    new Vector3(x, 0.2f, z),
                    0f,
                    0.9f + (float)rng.NextDouble() * 0.5f,
                    i % catalogLength));
            }
        }
    }
}
