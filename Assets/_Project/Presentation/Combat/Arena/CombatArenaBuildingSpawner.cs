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

            var building2D = CombatArena2DBuildingVisual.Spawn(cell.InstanceId, footprint.Center, source, cell.Side);
            building2D.name = $"Building_{cell.InstanceId}";
            building2D.transform.SetParent(buildingsRoot, false);
            return building2D;
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
