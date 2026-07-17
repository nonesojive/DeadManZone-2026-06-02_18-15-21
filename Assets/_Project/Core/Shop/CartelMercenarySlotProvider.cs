using System;
using System.Collections.Generic;
using DeadManZone.Core;

namespace DeadManZone.Core.Shop
{
    /// <summary>
    /// 2026-07-15 faction-roster-v1 §1.9/§2.4/§4: Cartel of Echoes' 6th shop offer slot —
    /// a variant of the ExtraGeneralSlot seam, wired through the (previously dormant)
    /// IShopSlotUnlockProvider mechanism instead of the board-piece-driven modifier flags,
    /// since this slot is gated on FACTION IDENTITY, not something a board piece grants.
    /// Always present in the registry; a no-op unless the run's FactionId is Cartel
    /// (FactionPassives.HasMercenarySlot) — mirrors the "passives are no-ops for factions
    /// that lack them" gating used everywhere else in this wave.
    /// </summary>
    public sealed class CartelMercenarySlotProvider : IShopSlotUnlockProvider
    {
        /// <summary>First bonus slot (9) — baseline (0-8) is reserved for the existing 5
        /// live + 4 dormant-ability slots.</summary>
        public const int SlotIndex = ShopSlotLayoutResolver.BaselineSlotCount;

        public IReadOnlyList<ShopSlotUnlock> Evaluate(ShopUnlockContext context)
        {
            if (!FactionPassives.HasMercenarySlot(context?.FactionId))
                return Array.Empty<ShopSlotUnlock>();

            var profile = new ShopSlotProfile
            {
                SlotIndex = SlotIndex,
                // SpecialRule: rolled by ShopGenerator's bespoke mercenary path, not the
                // normal rarity/source pipeline (RollMercenarySlot).
                Kind = ShopSlotKind.SpecialRule,
                PoolBias = ShopPoolBias.Offensive
            };

            return new[] { new ShopSlotUnlock { SlotIndex = SlotIndex, Profile = profile } };
        }
    }
}
