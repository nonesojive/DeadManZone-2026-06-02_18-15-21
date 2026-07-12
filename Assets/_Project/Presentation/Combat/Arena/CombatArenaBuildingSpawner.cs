using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Board;
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
            Func<string, PieceDefinitionSO> resolvePiece,
            CombatArenaConfigSO config)
        {
            Clear();

            if (battlefield == null || mapper == null || buildingsRoot == null)
                return;

            foreach (var cell in battlefield.Cells)
            {
                if (cell?.Definition == null || !ShouldSpawnStaticBuilding(cell.Definition))
                    continue;

                var source = resolvePiece?.Invoke(cell.Definition.Id);
                var instance = SpawnBuilding(cell, mapper, buildingsRoot, source, config);
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

            if (PieceCombatRules.ParticipatesInCombat(definition))
                return false;

            return definition.Category is PieceCategory.Building or PieceCategory.Hybrid
                   || PieceTagQueries.HasPrimaryTag(definition, GameTagIds.Building);
        }

        private static GameObject SpawnBuilding(
            BattlefieldCell cell,
            CombatGridMapper mapper,
            Transform buildingsRoot,
            PieceDefinitionSO source,
            CombatArenaConfigSO config)
        {
            var footprint = ComputeFootprintBounds(cell.Position, cell.Definition.Shape, mapper, DefaultBuildingHeight);

            // Footprint-sized placeholder block until non-combatant structures get a real
            // 3D treatment (the 2D billboard sprite path was deleted with the 2D arena).
            var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = $"Building_{cell.InstanceId}";
            var collider = building.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.Destroy(collider);

            building.transform.SetParent(buildingsRoot, false);
            building.transform.localScale = new Vector3(footprint.Width, footprint.Height, footprint.Depth);
            building.transform.position = footprint.Center + Vector3.up * (footprint.Height * 0.5f);
            return building;
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

}
