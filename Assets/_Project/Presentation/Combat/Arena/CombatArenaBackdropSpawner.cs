using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    internal static class CombatArenaBackdropSpawner
    {
        public static void SpawnPoint(
            ICombatArenaBackdropRing ring,
            Transform parent,
            CombatArenaBackdropSpawnPoint point)
        {
            if (ring == null || parent == null)
                return;

            var path = ring.ResolvePrefabPath(point.CatalogIndex);
            var prefab = SyntyRuntimeAssetLoader.LoadPrefab(path);
            if (prefab == null)
                return;

            var instance = Object.Instantiate(prefab, parent, false);
            instance.name = $"{point.Ring}_{parent.childCount}";
            instance.transform.localPosition = point.LocalPosition;
            instance.transform.localRotation = Quaternion.Euler(0f, point.YawDegrees, 0f);
            instance.transform.localScale = Vector3.one * point.UniformScale;
            CombatArenaMaterialUtility.FixBuiltInMaterials(instance);
            CombatArenaFxCull.RemoveTransparentFxRenderers(instance);
        }
    }
}
