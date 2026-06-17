using DeadManZone.Data;

namespace DeadManZone.Presentation.Combat.Arena
{
    internal sealed class ScriptableCombatArenaBackdropRing : ICombatArenaBackdropRing
    {
        private readonly CombatArenaBackdropRingSO _asset;

        public ScriptableCombatArenaBackdropRing(CombatArenaBackdropRingSO asset) =>
            _asset = asset;

        public CombatArenaBackdropRing RingType => _asset.ring;
        public string ChildRootName => _asset.childRootName;
        public bool IsEnabled => _asset.enabled;
        public int PrefabCount => _asset.PrefabCount;
        public string ResolvePrefabPath(int catalogIndex) => _asset.ResolvePrefabPath(catalogIndex);
    }

    internal sealed class LegacyCatalogCombatArenaBackdropRing : ICombatArenaBackdropRing
    {
        private readonly CombatArenaBackdropRing _ring;

        public LegacyCatalogCombatArenaBackdropRing(CombatArenaBackdropRing ring) => _ring = ring;

        public CombatArenaBackdropRing RingType => _ring;
        public string ChildRootName => _ring switch
        {
            CombatArenaBackdropRing.TrenchDressing => "TrenchDressing",
            CombatArenaBackdropRing.Skyline => "SkylineBackdrop",
            _ => "AtmosphereFx"
        };
        public bool IsEnabled => true;
        public int PrefabCount => CombatArenaBackdropCatalog.GetPrefabCount(_ring);
        public string ResolvePrefabPath(int catalogIndex) =>
            CombatArenaBackdropCatalog.ResolvePath(_ring, catalogIndex);
    }
}
