using System;
using UnityEngine;

namespace DeadManZone.Data
{
    [Serializable]
    public struct SandboxArtEntry
    {
        public string pieceId;
        public string iconAssetPath;
        [Tooltip("Empty = no prefab assignment (radio_array uses runtime placeholder).")]
        public string combatArenaPrefabPath;
        public float combatArenaModelScale;
        public float combatArenaModelHeight;
        public bool snapshotIconFromPrefab;
    }

    [CreateAssetMenu(menuName = "DeadManZone/Sandbox Art Catalog")]
    public sealed class SandboxArtCatalogSO : ScriptableObject
    {
        public SandboxArtEntry[] entries = Array.Empty<SandboxArtEntry>();

        private const string ResourcesPath = "DeadManZone/SandboxArtCatalog";

        public static SandboxArtCatalogSO LoadFromResources() =>
            Resources.Load<SandboxArtCatalogSO>(ResourcesPath);

        public bool TryGetEntry(string pieceId, out SandboxArtEntry entry)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (string.Equals(entries[i].pieceId, pieceId, StringComparison.Ordinal))
                {
                    entry = entries[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }
    }
}
