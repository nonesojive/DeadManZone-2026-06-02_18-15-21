using DeadManZone.Core.Board;
using DeadManZone.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Top Troops-style checkerboard battlefield tiles (brown dirt palette).</summary>
    public sealed class CombatArenaGridView : MonoBehaviour
    {
        private const string RootName = "CombatArenaGrid";

        public static CombatArenaGridView Build(
            Transform arenaRoot,
            BattlefieldLayout layout,
            CombatArenaConfigSO config)
        {
            if (arenaRoot == null || layout == null || config == null || !config.showCheckerboardGrid)
                return null;

            var existing = arenaRoot.Find(RootName);
            if (existing != null)
            {
                if (Application.isPlaying)
                    Destroy(existing.gameObject);
                else
                    DestroyImmediate(existing.gameObject);
            }

            var rootGo = new GameObject(RootName);
            rootGo.transform.SetParent(arenaRoot, false);
            var gridView = rootGo.AddComponent<CombatArenaGridView>();
            gridView.Populate(layout, config);
            return gridView;
        }

        private void Populate(BattlefieldLayout layout, CombatArenaConfigSO config)
        {
            var mesh = CombatArenaGridMeshBuilder.Build(
                layout,
                config.cellWidth,
                config.cellDepth,
                config.gridLightCellColor,
                config.gridDarkCellColor,
                config.gridCellInset,
                config.gridYOffset,
                out var materials);

            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = materials;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = true;
        }
    }
}
