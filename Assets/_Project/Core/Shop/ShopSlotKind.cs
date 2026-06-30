namespace DeadManZone.Core.Shop
{
    public enum ShopSlotKind
    {
        BaselineOffensive,
        BaselineDefensive,
        /// <summary>Bottom-row slot reserved for future ability unlocks; not rolled yet.</summary>
        ReservedAbility,
        Bonus,
        SpecialRule,
        ExtraOffensive = Bonus,
        ExtraSpecialty = Bonus
    }
}
