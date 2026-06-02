using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;

namespace DeadManZone.Core.Shop
{
    public sealed class ShopGenerator
    {
        private const int MaxGoldDiscountPercent = 25;
        private const int BaseGeneralSlots = 5;
        private const int EngineersSlots = 4;
        private const int RequisitionSlots = 4;

        private readonly ContentRegistry _registry;

        public ShopGenerator(ContentRegistry registry)
        {
            _registry = registry;
        }

        public ShopState Generate(BoardState board, string factionId, int round, int seed)
        {
            var rng = new Rng(seed);
            var modifiers = ComputeModifiers(board);
            var offers = new List<ShopOffer>();

            RollLane(ShopLane.General, BaseGeneralSlots + modifiers.ExtraGeneralSlots, modifiers, rng, offers, round);
            RollLane(ShopLane.Engineers, EngineersSlots, modifiers, rng, offers, round);

            if (modifiers.GuaranteeEngineerOffer && !offers.Any(o => o.Lane == ShopLane.Engineers))
                InjectEngineerOffer(offers, modifiers, rng, round);

            RollLane(ShopLane.Requisition, RequisitionSlots, modifiers, rng, offers, round);

            return new ShopState
            {
                Offers = offers,
                Modifiers = modifiers,
                Seed = seed
            };
        }

        public static ShopModifiers ComputeModifiers(BoardState board)
        {
            int discount = 0;
            int extraGeneralSlots = 0;
            bool preview = false;
            bool guaranteeEngineer = false;

            foreach (var piece in board.Pieces)
            {
                var flags = piece.Definition.ShopModifiers;
                if (flags.HasFlag(ShopModifierFlags.GoldDiscount10))
                    discount += 10;
                if (flags.HasFlag(ShopModifierFlags.ExtraGeneralSlot))
                    extraGeneralSlots += 1;
                if (flags.HasFlag(ShopModifierFlags.EnemyTagPreview))
                    preview = true;
                if (flags.HasFlag(ShopModifierFlags.GuaranteeEngineerOffer))
                    guaranteeEngineer = true;
            }

            discount = Math.Min(discount, MaxGoldDiscountPercent);

            return new ShopModifiers
            {
                GoldDiscountPercent = discount,
                ExtraGeneralSlots = extraGeneralSlots,
                EnemyTagPreview = preview,
                GuaranteeEngineerOffer = guaranteeEngineer
            };
        }

        private void RollLane(
            ShopLane lane,
            int slotCount,
            ShopModifiers modifiers,
            Rng rng,
            List<ShopOffer> offers,
            int round)
        {
            var pool = _registry.GetPool(lane);
            if (pool.Count == 0)
                return;

            var available = pool.ToList();
            for (int i = 0; i < slotCount && available.Count > 0; i++)
            {
                int index = rng.NextInt(0, available.Count);
                var piece = available[index];
                available.RemoveAt(index);
                offers.Add(CreateOffer(lane, piece, modifiers, rng, round, offers.Count));
            }
        }

        private void InjectEngineerOffer(
            List<ShopOffer> offers,
            ShopModifiers modifiers,
            Rng rng,
            int round)
        {
            var buildings = _registry.GetBuildings();
            if (buildings.Count == 0)
                return;

            var piece = buildings[rng.NextInt(0, buildings.Count)];
            offers.Add(CreateOffer(ShopLane.Engineers, piece, modifiers, rng, round, offers.Count));
        }

        private ShopOffer CreateOffer(
            ShopLane lane,
            PieceDefinition piece,
            ShopModifiers modifiers,
            Rng rng,
            int round,
            int offerIndex)
        {
            int gold = ApplyGoldDiscount(piece.GoldCost, modifiers.GoldDiscountPercent);
            int requisition = piece.RequisitionCost;

            // Slight price scaling by round for MVP variety.
            gold += Math.Max(0, round - 1);

            return new ShopOffer
            {
                OfferId = $"{lane}_{piece.Id}_{offerIndex}",
                Lane = lane,
                PieceId = piece.Id,
                GoldPrice = gold,
                RequisitionPrice = requisition
            };
        }

        private static int ApplyGoldDiscount(int baseGold, int discountPercent)
        {
            if (baseGold <= 0)
                return 0;

            int discounted = baseGold * (100 - discountPercent) / 100;
            return Math.Max(1, discounted);
        }
    }
}
