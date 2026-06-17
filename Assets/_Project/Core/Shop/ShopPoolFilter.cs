using System;
using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;

namespace DeadManZone.Core.Shop
{
    [Obsolete("Use ShopPiecePicker and ShopOfferPoolBuilder instead.")]
    public static class ShopPoolFilter
    {
        public static PieceDefinition PickWeighted(
            IReadOnlyList<PieceDefinition> pool,
            int fightIndex,
            Rng rng,
            string playerFactionId = "iron_vanguard") =>
            ShopPiecePicker.PickWeighted(pool, Array.Empty<string>(), preferredRoleWeight: 1f, rng);
    }
}
