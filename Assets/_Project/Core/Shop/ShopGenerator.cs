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
        public const int OffersPerLane = 3;

        private readonly ContentRegistry _registry;

        public ShopGenerator(ContentRegistry registry)
        {
            _registry = registry;
        }

        public ShopState Generate(
            BoardState board,
            string factionId,
            int round,
            int seed,
            bool? specialtyUnlocked = null)
        {
            var rng = new Rng(seed);
            var modifiers = ComputeModifiers(board);
            var offers = new List<ShopOffer>();
            bool specialtyOpen = specialtyUnlocked ?? SpecialtyLaneUnlock.IsUnlocked(board, factionId, _registry);

            int offensiveSlots = OffersPerLane + modifiers.ExtraGeneralSlots;
            RollLane(ShopLane.Offensive, offensiveSlots, modifiers, rng, offers, round);
            RollLane(ShopLane.Defensive, OffersPerLane, modifiers, rng, offers, round);

            if (modifiers.GuaranteeEngineerOffer && !offers.Any(o => o.Lane == ShopLane.Defensive))
                InjectDefensiveOffer(offers, modifiers, rng, round);

            if (specialtyOpen)
                RollLane(ShopLane.Specialty, OffersPerLane, modifiers, rng, offers, round);

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

        public List<ShopOffer> RollLaneOffers(
            ShopLane lane,
            int slotCount,
            ShopModifiers modifiers,
            int seed,
            int round,
            IReadOnlyDictionary<int, ShopOffer> fixedSlots = null)
        {
            var rng = new Rng(seed);
            return RollLane(lane, slotCount, modifiers, rng, round, fixedSlots);
        }

        private void RollLane(
            ShopLane lane,
            int slotCount,
            ShopModifiers modifiers,
            Rng rng,
            List<ShopOffer> offers,
            int round)
        {
            foreach (var rolled in RollLane(lane, slotCount, modifiers, rng, round, fixedSlots: null))
                offers.Add(rolled);
        }

        private List<ShopOffer> RollLane(
            ShopLane lane,
            int slotCount,
            ShopModifiers modifiers,
            Rng rng,
            int round,
            IReadOnlyDictionary<int, ShopOffer> fixedSlots)
        {
            fixedSlots ??= new Dictionary<int, ShopOffer>();
            var results = new List<ShopOffer>();
            var pool = _registry.GetPool(lane);
            if (pool.Count == 0)
                return results;

            var available = pool.ToList();
            foreach (var fixedOffer in fixedSlots.Values)
                available.RemoveAll(p => p.Id == fixedOffer.PieceId);

            for (int i = 0; i < slotCount; i++)
            {
                if (fixedSlots.TryGetValue(i, out var fixedOffer))
                {
                    results.Add(CopyFixedOffer(fixedOffer, i));
                    continue;
                }

                if (available.Count == 0)
                    break;

                int index = rng.NextInt(0, available.Count);
                var piece = available[index];
                available.RemoveAt(index);
                results.Add(CreateOffer(lane, piece, modifiers, rng, round, i));
            }

            return results;
        }

        private static ShopOffer CopyFixedOffer(ShopOffer source, int slotIndex) =>
            new ShopOffer
            {
                OfferId = source.OfferId,
                Lane = source.Lane,
                SlotIndex = slotIndex,
                PieceId = source.PieceId,
                GoldPrice = source.GoldPrice,
                RequisitionPrice = source.RequisitionPrice
            };

        private void InjectDefensiveOffer(
            List<ShopOffer> offers,
            ShopModifiers modifiers,
            Rng rng,
            int round)
        {
            var buildings = _registry.GetBuildings();
            if (buildings.Count == 0)
                return;

            var piece = buildings[rng.NextInt(0, buildings.Count)];
            offers.Add(CreateOffer(ShopLane.Defensive, piece, modifiers, rng, round, slotIndex: 0));
        }

        private ShopOffer CreateOffer(
            ShopLane lane,
            PieceDefinition piece,
            ShopModifiers modifiers,
            Rng rng,
            int round,
            int slotIndex)
        {
            int gold = ApplyGoldDiscount(piece.GoldCost, modifiers.GoldDiscountPercent);
            int requisition = piece.RequisitionCost;

            // Slight price scaling by round for MVP variety.
            gold += Math.Max(0, round - 1);

            return new ShopOffer
            {
                OfferId = $"{lane}_{piece.Id}_{slotIndex}",
                Lane = lane,
                SlotIndex = slotIndex,
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
