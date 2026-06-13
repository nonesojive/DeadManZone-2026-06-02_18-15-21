using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Spawns static building visuals for non-combatant structures in the combat arena.</summary>
    public sealed class CombatArenaBuildingSpawner
    {
        private const float DefaultBuildingHeight = 1.2f;

        private readonly List<GameObject> _instances = new();
        private readonly Dictionary<string, GameObject> _byInstanceId = new();

        public IReadOnlyDictionary<string, GameObject> Instances => _byInstanceId;

        public void Clear()
        {
            for (int i = 0; i < _instances.Count; i++)
            {
                if (_instances[i] != null)
                    UnityEngine.Object.Destroy(_instances[i]);
            }

            _instances.Clear();
            _byInstanceId.Clear();
        }

        public void SpawnAll(
            BattlefieldState battlefield,
            CombatGridMapper mapper,
            Transform buildingsRoot,
            Func<string, PieceDefinitionSO> resolvePiece)
        {
            Clear();

            if (battlefield == null || mapper == null || buildingsRoot == null)
                return;

            foreach (var cell in battlefield.Cells)
            {
                if (cell?.Definition == null || !ShouldSpawnStaticBuilding(cell.Definition))
                    continue;

                var source = resolvePiece?.Invoke(cell.Definition.Id);
                var instance = SpawnBuilding(cell, mapper, buildingsRoot, source);
                if (instance == null)
                    continue;

                _instances.Add(instance);
                _byInstanceId[cell.InstanceId] = instance;
            }
        }

        public bool HasVisual(string instanceId) =>
            !string.IsNullOrEmpty(instanceId) && _byInstanceId.ContainsKey(instanceId);

        private static bool ShouldSpawnStaticBuilding(PieceDefinition definition)
        {
            if (definition == null)
                return false;

            if (PieceTagQueries.HasTag(definition, GameTagIds.Combatant))
                return false;

            return definition.Category is PieceCategory.Building or PieceCategory.Hybrid
                   || PieceTagQueries.HasPrimaryTag(definition, GameTagIds.Building)
                   || PieceTagQueries.HasTag(definition, GameTagIds.Hq);
        }

        private static GameObject SpawnBuilding(
            BattlefieldCell cell,
            CombatGridMapper mapper,
            Transform buildingsRoot,
            PieceDefinitionSO source)
        {
            var footprint = ComputeFootprintBounds(cell.Position, cell.Definition.Shape, mapper, DefaultBuildingHeight);
            string objectName = $"Building_{cell.InstanceId}";

            GameObject root = source?.combatArenaPrefab != null
                ? SpawnPrefabBuilding(source, footprint, buildingsRoot, objectName)
                : SpawnPlaceholderBuilding(cell.Definition.Id, footprint, buildingsRoot, objectName);

            if (root == null)
                return null;

            root.transform.SetParent(buildingsRoot, false);
            return root;
        }

        private static GameObject SpawnPrefabBuilding(
            PieceDefinitionSO source,
            FootprintBounds footprint,
            Transform buildingsRoot,
            string objectName)
        {
            var instance = UnityEngine.Object.Instantiate(source.combatArenaPrefab, buildingsRoot, false);
            instance.name = objectName;

            float scale = source.combatArenaModelScale > 0f ? source.combatArenaModelScale : 1f;
            float height = source.combatArenaModelHeight > 0f
                ? source.combatArenaModelHeight
                : DefaultBuildingHeight;

            FitInstanceToFootprint(instance.transform, footprint, height, scale);
            return instance;
        }

        private static GameObject SpawnPlaceholderBuilding(
            string pieceId,
            FootprintBounds footprint,
            Transform buildingsRoot,
            string objectName)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            cube.transform.SetParent(buildingsRoot, false);

            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                material.color = CombatArenaBuildingDefaults.ResolveColor(pieceId);
                renderer.sharedMaterial = material;
            }

            var collider = cube.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.Destroy(collider);

            FitInstanceToFootprint(cube.transform, footprint, DefaultBuildingHeight, 1f);
            return cube;
        }

        private static void FitInstanceToFootprint(
            Transform instance,
            FootprintBounds footprint,
            float targetHeight,
            float modelScale)
        {
            instance.localPosition = new Vector3(
                footprint.Center.x,
                targetHeight * 0.5f * modelScale,
                footprint.Center.z);
            instance.localRotation = Quaternion.identity;
            instance.localScale = new Vector3(
                footprint.Width * modelScale,
                targetHeight * modelScale,
                footprint.Depth * modelScale);
        }

        private static FootprintBounds ComputeFootprintBounds(
            GridCoord anchor,
            PieceShape shape,
            CombatGridMapper mapper,
            float height)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;

            foreach (var cell in shape.GetCells(anchor))
            {
                Vector3 world = mapper.ToWorld(cell);
                minX = Mathf.Min(minX, world.x);
                maxX = Mathf.Max(maxX, world.x);
                minZ = Mathf.Min(minZ, world.z);
                maxZ = Mathf.Max(maxZ, world.z);
            }

            float paddingX = mapper.CellWidth * 0.5f;
            float paddingZ = mapper.CellDepth * 0.5f;
            return new FootprintBounds(
                (minX + maxX) * 0.5f,
                (minZ + maxZ) * 0.5f,
                maxX - minX + paddingX,
                maxZ - minZ + paddingZ,
                height);
        }

        private readonly struct FootprintBounds
        {
            public FootprintBounds(float centerX, float centerZ, float width, float depth, float height)
            {
                Center = new Vector3(centerX, 0f, centerZ);
                Width = width;
                Depth = depth;
                Height = height;
            }

            public Vector3 Center { get; }
            public float Width { get; }
            public float Depth { get; }
            public float Height { get; }
        }
    }

    internal static class CombatArenaBuildingDefaults
    {
        public static Color ResolveColor(string pieceId)
        {
            if (string.IsNullOrEmpty(pieceId))
                return new Color(0.45f, 0.42f, 0.38f);

            if (pieceId.Contains("hq", StringComparison.OrdinalIgnoreCase))
                return new Color(0.48f, 0.4f, 0.28f);

            if (pieceId.Contains("supply", StringComparison.OrdinalIgnoreCase))
                return new Color(0.34f, 0.42f, 0.28f);

            if (pieceId.Contains("gun", StringComparison.OrdinalIgnoreCase)
                || pieceId.Contains("artillery", StringComparison.OrdinalIgnoreCase))
                return new Color(0.38f, 0.38f, 0.4f);

            return new Color(0.42f, 0.4f, 0.36f);
        }
    }
}
