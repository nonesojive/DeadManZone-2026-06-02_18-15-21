namespace DeadManZone.Core.Shop
{
    public enum ShopSlotKind
    {
        BaselineOffensive,
        BaselineDefensive,
        /// <summary>Bottom-row slot reserved for future ability unlocks; not rolled yet.</summary>
        ReservedAbility,
        Bonus,
        /// <summary>A slot with bespoke roll logic outside the normal rarity/source
        /// pipeline. First user (2026-07-15 faction-roster-v1 §1.9): the Cartel mercenary
        /// slot (CartelMercenarySlotProvider, ShopGenerator.RollMercenarySlot).</summary>
        SpecialRule,
        ExtraOffensive = Bonus,
        ExtraSpecialty = Bonus
    }
}
