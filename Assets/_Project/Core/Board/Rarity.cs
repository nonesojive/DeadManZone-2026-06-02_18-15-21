namespace DeadManZone.Core.Board
{
    /// <summary>
    /// A piece's DESIGN ROLE, not its raw power (CONTEXT.md): Common = line units,
    /// Uncommon = synergy enablers and support, Rare = build-arounds (ability
    /// granters, vehicles, commanders). Gates shop offer odds (Dread-weighted,
    /// see <see cref="Shop.RarityWeights"/>), salvage quality, and the Supplies
    /// price (see <see cref="Shop.RarityPricing"/> — a piece's price is derived
    /// from this tier, not authored per piece).
    /// APPEND-ONLY: assets serialize these as ints. A future 4th tier appends
    /// after Rare — never reorder or renumber existing values.
    /// </summary>
    public enum Rarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2
    }
}
