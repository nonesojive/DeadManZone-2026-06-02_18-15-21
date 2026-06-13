using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class SandboxArtDefaultCatalogFactory
    {
        [MenuItem("DeadManZone/Art/Create Default Sandbox Art Catalog")]
        public static void CreateDefaultSandboxArtCatalog()
        {
            EnsureFolder(SandboxArtPaths.ResourcesFolder);
            EnsureFolder(SandboxArtPaths.SandboxIconsFolder);

            var catalog = AssetDatabase.LoadAssetAtPath<SandboxArtCatalogSO>(SandboxArtPaths.CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SandboxArtCatalogSO>();
                AssetDatabase.CreateAsset(catalog, SandboxArtPaths.CatalogAssetPath);
            }

            catalog.entries = new[]
            {
                Entry("conscript_rifleman", SandboxArtPaths.GermanInfantry, SandboxArtPaths.GrokIcon("conscript_rifleman"), 1f, 1.6f, false),
                Entry("grenade_thrower", SandboxArtPaths.GermanSupport, SandboxArtPaths.GrokIcon("grenade_thrower"), 1f, 1.6f, false),
                Entry("field_medic", SandboxArtPaths.GermanMedic, SandboxArtPaths.GrokIcon("field_medic"), 1f, 1.6f, false),
                Entry("armored_transport", SandboxArtPaths.AtvColor0, SandboxArtPaths.GrokIcon("armored_transport"), 0.9f, 1.2f, false),
                Entry("mobile_cannon", SandboxArtPaths.MshColor0, SandboxArtPaths.GrokIcon("mobile_cannon"), 0.85f, 1.4f, false),
                Entry("neutral_supply_depot", SandboxArtPaths.BuildingSupplyDepot, SandboxArtPaths.IconFuelCanister, 1f, 0f, false),
                Entry("neutral_field_gun", SandboxArtPaths.BuildingFieldGun, SandboxArtPaths.IconGeneratorPart, 1f, 0f, false),
                Entry("shock_trooper", SandboxArtPaths.GermanOfficer, SandboxArtPaths.SandboxSnapshotIcon("shock_trooper"), 1f, 1.6f, true),
                Entry("neutral_mortar_team", SandboxArtPaths.GermanSupport, SandboxArtPaths.SandboxSnapshotIcon("neutral_mortar_team"), 1f, 1.6f, true),
                Entry("marksman_squad", SandboxArtPaths.GermanSniper, SandboxArtPaths.SandboxSnapshotIcon("marksman_squad"), 1f, 1.6f, true),
                Entry("ironmarch_hq", SandboxArtPaths.BuildingHq, SandboxArtPaths.IconBunkerMap, 1f, 0f, false),
                Entry("rifle_squad", SandboxArtPaths.GermanInfantry, SandboxArtPaths.SandboxSnapshotIcon("rifle_squad"), 1f, 1.6f, true),
                Entry("diesel_walker", SandboxArtPaths.FaColor0, SandboxArtPaths.SandboxSnapshotIcon("diesel_walker"), 0.9f, 1.4f, true),
                Entry("radio_array", string.Empty, SandboxArtPaths.IconEmergencyRadio, 1f, 0f, false),
                Entry("mg_team", SandboxArtPaths.GermanSupport, SandboxArtPaths.SandboxSnapshotIcon("mg_team"), 1f, 1.6f, true),
                Entry("field_gun_nest", SandboxArtPaths.BuildingFieldGun, SandboxArtPaths.IconGeneratorPart, 1f, 0f, false),
                Entry("supply_depot", SandboxArtPaths.BuildingSupplyDepot, SandboxArtPaths.IconFuelCanister, 1f, 0f, false),
                Entry("field_workshop", SandboxArtPaths.BuildingSupplyDepot, SandboxArtPaths.IconToolbox, 1f, 0f, false),
                Entry("mobile_artillery", SandboxArtPaths.MshColor1, SandboxArtPaths.SandboxSnapshotIcon("mobile_artillery"), 0.85f, 1.4f, true),
                Entry("ironmarch_heavy_tank", SandboxArtPaths.FaColor1, SandboxArtPaths.SandboxSnapshotIcon("ironmarch_heavy_tank"), 0.95f, 1.5f, true),
                Entry("ironmarch_mortar", SandboxArtPaths.GermanSupport, SandboxArtPaths.SandboxSnapshotIcon("ironmarch_mortar"), 1f, 1.6f, true),
                Entry("ironmarch_engineer", SandboxArtPaths.GermanMedic, SandboxArtPaths.SandboxSnapshotIcon("ironmarch_engineer"), 1f, 1.6f, true),
                Entry("ironmarch_breacher", SandboxArtPaths.GermanOfficer, SandboxArtPaths.SandboxSnapshotIcon("ironmarch_breacher"), 1f, 1.6f, true),
                Entry("ironmarch_sniper", SandboxArtPaths.GermanSniper, SandboxArtPaths.SandboxSnapshotIcon("ironmarch_sniper"), 1f, 1.6f, true),
                Entry("ironmarch_defender", SandboxArtPaths.GermanInfantry, SandboxArtPaths.SandboxSnapshotIcon("ironmarch_defender"), 1f, 1.6f, true)
            };

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"Sandbox art catalog ready with {catalog.entries.Length} entries at {SandboxArtPaths.CatalogAssetPath}.");
        }

        private static SandboxArtEntry Entry(
            string pieceId,
            string prefabPath,
            string iconPath,
            float scale,
            float height,
            bool snapshot)
        {
            return new SandboxArtEntry
            {
                pieceId = pieceId,
                iconAssetPath = iconPath,
                combatArenaPrefabPath = prefabPath,
                combatArenaModelScale = scale,
                combatArenaModelHeight = height,
                snapshotIconFromPrefab = snapshot
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
