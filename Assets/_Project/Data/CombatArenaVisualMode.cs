using System;

namespace DeadManZone.Data
{
    /// <summary>Which combat arena presentation backend is active. ToonInk3D is the only
    /// live renderer; the obsolete values are kept so assets serialized with the old ints
    /// keep deserializing (enum values are stored as ints in .asset files).</summary>
    public enum CombatArenaVisualMode
    {
        [Obsolete("The Synty legacy 3D renderer was removed; ToonInk3D is the only backend.")]
        Legacy3D = 0,
        [Obsolete("The 2D sprite renderer was removed; ToonInk3D is the only backend.")]
        TopTroops2D = 1,
        /// <summary>Rigged toon-ink 3D actors; scene authors its own camera/lighting.</summary>
        ToonInk3D = 2
    }
}
