#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class SyntyArtCatalogFactory
    {
        [MenuItem("DeadManZone/Synty/Create Synty Sandbox Art Catalog")]
        public static void CreateSyntySandboxArtCatalog()
        {
            EnsureFolder(SyntyArtPaths.Icons);

            var catalog = AssetDatabase.LoadAssetAtPath<SandboxArtCatalogSO>(SandboxArtPaths.CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SandboxArtCatalogSO>();
                AssetDatabase.CreateAsset(catalog, SandboxArtPaths.CatalogAssetPath);
            }

            catalog.entries = new[]
            {
                Entry("conscript_rifleman", SyntyArtPaths.UnitRifle, 1f, 1.6f),
                Entry("grenade_thrower", SyntyArtPaths.UnitSupport, 1f, 1.6f),
                Entry("field_medic", SyntyArtPaths.UnitMedic, 1f, 1.6f),
                Entry("armored_transport", SyntyArtPaths.VehicleTruck, 0.85f, 1.2f),
                Entry("mobile_cannon", SyntyArtPaths.VehicleCar, 0.9f, 1.4f),
                Entry("neutral_supply_depot", SyntyArtPaths.BuildingSupplyDepot, 1f, 0f),
                Entry("neutral_field_gun", SyntyArtPaths.BuildingFieldGun, 1f, 0f),
                Entry("shock_trooper", SyntyArtPaths.UnitOfficer, 1f, 1.6f),
                Entry("neutral_mortar_team", SyntyArtPaths.UnitSupport, 1f, 1.6f),
                Entry("marksman_squad", SyntyArtPaths.UnitSniper, 1f, 1.6f),
                Entry("ironmarch_hq", SyntyArtPaths.BuildingHq, 1f, 0f),
                Entry("rifle_squad", SyntyArtPaths.UnitRifle, 1f, 1.6f),
                Entry("diesel_walker", SyntyArtPaths.VehicleMech, 0.75f, 1.4f),
                Entry("radio_array", SyntyArtPaths.BuildingSupplyDepot, 1f, 0f),
                Entry("mg_team", SyntyArtPaths.UnitSupport, 1f, 1.6f),
                Entry("field_gun_nest", SyntyArtPaths.BuildingFieldGun, 1f, 0f),
                Entry("supply_depot", SyntyArtPaths.BuildingSupplyDepot, 1f, 0f),
                Entry("field_workshop", SyntyArtPaths.BuildingSupplyDepot, 1f, 0f),
                Entry("mobile_artillery", SyntyArtPaths.VehicleHalftrack, 0.85f, 1.4f),
                Entry("ironmarch_heavy_tank", SyntyArtPaths.VehicleTank, 0.9f, 1.5f),
                Entry("ironmarch_mortar", SyntyArtPaths.UnitSupport, 1f, 1.6f),
                Entry("ironmarch_engineer", SyntyArtPaths.UnitMedic, 1f, 1.6f),
                Entry("ironmarch_breacher", SyntyArtPaths.UnitOfficer, 1f, 1.6f),
                Entry("ironmarch_sniper", SyntyArtPaths.UnitSniper, 1f, 1.6f),
                Entry("ironmarch_defender", SyntyArtPaths.UnitRifle, 1f, 1.6f)
            };

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"Synty sandbox art catalog ready with {catalog.entries.Length} entries.");
        }

        private static SandboxArtEntry Entry(string pieceId, string prefabPath, float scale, float height)
        {
            return new SandboxArtEntry
            {
                pieceId = pieceId,
                iconAssetPath = SyntyArtPaths.IconPath(pieceId),
                combatArenaPrefabPath = prefabPath,
                combatArenaModelScale = scale,
                combatArenaModelHeight = height,
                snapshotIconFromPrefab = true
            };
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
