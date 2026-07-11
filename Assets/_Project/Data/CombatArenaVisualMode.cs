namespace DeadManZone.Data
{
    /// <summary>Which combat arena presentation backend is active.</summary>
    public enum CombatArenaVisualMode
    {
        Legacy3D = 0,
        TopTroops2D = 1,
        /// <summary>Rigged toon-ink 3D actors (Combat3D demo); scene authors its own camera/lighting.</summary>
        ToonInk3D = 2
    }
}
