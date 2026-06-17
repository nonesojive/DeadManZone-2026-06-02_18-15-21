using DeadManZone.Data;

namespace DeadManZone.Presentation.Combat.Arena
{
    public readonly struct CombatArenaBackdropCatalogLengths
    {
        public CombatArenaBackdropCatalogLengths(
            int trenchDressing,
            int skyline,
            int atmosphereFx)
        {
            TrenchDressing = trenchDressing;
            Skyline = skyline;
            AtmosphereFx = atmosphereFx;
        }

        public int TrenchDressing { get; }
        public int Skyline { get; }
        public int AtmosphereFx { get; }

        public int ForRing(CombatArenaBackdropRing ring) => ring switch
        {
            CombatArenaBackdropRing.TrenchDressing => TrenchDressing,
            CombatArenaBackdropRing.Skyline => Skyline,
            CombatArenaBackdropRing.AtmosphereFx => AtmosphereFx,
            _ => 0
        };

        public static CombatArenaBackdropCatalogLengths FromLegacyCatalog() =>
            new(
                CombatArenaBackdropCatalog.GetPrefabCount(CombatArenaBackdropRing.TrenchDressing),
                CombatArenaBackdropCatalog.GetPrefabCount(CombatArenaBackdropRing.Skyline),
                CombatArenaBackdropCatalog.GetPrefabCount(CombatArenaBackdropRing.AtmosphereFx));
    }
}
