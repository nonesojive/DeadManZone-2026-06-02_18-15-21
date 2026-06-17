using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Combat Arena Backdrop Ring")]
    public sealed class CombatArenaBackdropRingSO : ScriptableObject
    {
        public CombatArenaBackdropRing ring = CombatArenaBackdropRing.TrenchDressing;
        public bool enabled = true;
        public string childRootName = "TrenchDressing";
        public string[] prefabPaths = System.Array.Empty<string>();

        public int PrefabCount => prefabPaths?.Length ?? 0;

        public string ResolvePrefabPath(int catalogIndex)
        {
            if (PrefabCount == 0)
                return null;

            int index = catalogIndex % PrefabCount;
            if (index < 0)
                index += PrefabCount;

            return prefabPaths[index];
        }
    }
}
