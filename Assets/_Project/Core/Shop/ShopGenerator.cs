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
            string lastEnemyFactionId = null,
            int salvageChancePercent = 0,
            bool? specialtyUnlocked = null)
        {
            var rng = new Rng(seed);
            var modifiers = ComputeModifiers(board);
            var slotLayout = ShopSlotLayoutResolver.Resolve(
                board,
                factionId,
                _registry,
                modifiers,
                specialtyUnlocked);

            var offers = RollSlots(
                slotLayout,
                modifiers,
                rng,
                round,
                factionId,
                lastEnemyFactionId,
                salvageChancePercent,
                fixedSlots: null,
                board);

            if (modifiers.GuaranteeEngineerOffer && !offers.Any(o => o.Lane == ShopLane.Defensive))
                InjectDefensiveOffer(offers, modifiers, rng, round, slotLayout);

            return new ShopState
            {
                Offers = offers,
                Modifiers = modifiers,
                Seed = seed
            };
        }

        public List<ShopOffer> RollShopOffers(
            BoardState board,
            string factionId,
            ShopModifiers modifiers,
            int seed,
            int round,
            IReadOnlyDictionary<int, ShopOffer> fixedSlots,
            string lastEnemyFactionId = null,
            int salvageChancePercent = 0,
            bool? specialtyUnlocked = null)
        {
            var slotLayout = ShopSlotLayoutResolver.Resolve(
                board,
                factionId,
                _registry,
                modifiers,
                specialtyUnlocked);
            var rng = new Rng(seed);
            return RollSlots(
                slotLayout,
                modifiers,
                rng,
                round,
                factionId,
                lastEnemyFactionId,
                salvageChancePercent,
                fixedSlots,
                board);
        }

        public List<ShopOffer> RollLaneOffers(
            ShopLane lane,
            int slotCount,
            ShopModifiers modifiers,
            int seed,
            int round,
            string factionId,
            IReadOnlyDictionary<int, ShopOffer> fixedSlots = null,
            BoardState board = null,
            string lastEnemyFactionId = null,
            int salvageChancePercent = 0)
        {
            var rng = new Rng(seed);
            return RollLane(
                lane,
                slotCount,
                modifiers,
                rng,
                round,
                factionId,
                lastEnemyFactionId,
                salvageChancePercent,
                fixedSlots,
                board);
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

        private List<ShopOffer> RollSlots(
            IReadOnlyList<ShopSlotDefinition> slotLayout,
            ShopModifiers modifiers,
            Rng rng,
            int round,
            string factionId,
            string lastEnemyFactionId,
            int salvageChancePercent,
            IReadOnlyDictionary<int, ShopOffer> fixedSlots,
            BoardState board)
        {
            fixedSlots ??= new Dictionary<int, ShopOffer>();
            var offers = new List<ShopOffer>();
            var consumedPieceIds = new HashSet<string>();

            foreach (var fixedOffer in fixedSlots.Values)
                consumedPieceIds.Add(fixedOffer.PieceId);

            foreach (var slot in slotLayout)
            {
                if (fixedSlots.TryGetValue(slot.SlotIndex, out var fixedOffer))
                {
                    offers.Add(CopyFixedOffer(fixedOffer, slot.SlotIndex, slot.Kind));
                    continue;
                }

                var rolled = RollSingleSlot(
                    slot,
                    modifiers,
                    rng,
                    round,
                    factionId,
                    lastEnemyFactionId,
                    salvageChancePercent,
                    consumedPieceIds,
                    board);

                if (rolled != null)
                {
                    offers.Add(rolled);
                    if (!rolled.IsSalvaged)
                        consumedPieceIds.Add(rolled.PieceId);
                }
            }

            return offers;
        }

        private ShopOffer RollSingleSlot(
            ShopSlotDefinition slot,
            ShopModifiers modifiers,
            Rng rng,
            int round,
            string factionId,
            string lastEnemyFactionId,
            int salvageChancePercent,
            HashSet<string> consumedPieceIds,
            BoardState board)
        {
            var lane = slot.PoolLane;
            var pool = _registry.GetPool(lane);
            if (pool.Count == 0)
                return null;

            IEnumerable<PieceDefinition> eligible = pool;
            if (lane == ShopLane.Specialty && board != null)
            {
                var context = SpecialtyLaneRuleCatalog.Resolve(board, _registry);
                eligible = SpecialtyLaneRuleCatalog.FilterPool(pool, context);
            }

            var available = eligible.Where(p => !consumedPieceIds.Contains(p.Id)).ToList();

            if (available.Count == 0)
                return null;

            bool trySalvage = !string.IsNullOrEmpty(lastEnemyFactionId)
                && salvageChancePercent > 0
                && rng.NextInt(0, 100) < salvageChancePercent;

            if (trySalvage)
            {
                var salvagePool = SalvageShopPool.GetPool(_registry, lane, lastEnemyFactionId, factionId, round)
                    .Where(p => !consumedPieceIds.Contains(p.Id))
                    .ToList();
                if (salvagePool.Count > 0)
                {
                    var piece = salvagePool[rng.NextInt(0, salvagePool.Count)];
                    return CreateOffer(lane, slot.Kind, piece, modifiers, rng, round, slot.SlotIndex, isSalvaged: true);
                }
            }

            int index = rng.NextInt(0, available.Count);
            var picked = ShopPoolFilter.PickWeighted(available, round, rng, playerFactionId: factionId) ?? available[index];
            return CreateOffer(lane, slot.Kind, picked, modifiers, rng, round, slot.SlotIndex);
        }

        private List<ShopOffer> RollLane(
            ShopLane lane,
            int slotCount,
            ShopModifiers modifiers,
            Rng rng,
            int round,
            string factionId,
            string lastEnemyFactionId,
            int salvageChancePercent,
            IReadOnlyDictionary<int, ShopOffer> fixedSlots,
            BoardState board = null)
        {
            fixedSlots ??= new Dictionary<int, ShopOffer>();
            var results = new List<ShopOffer>();
            var pool = _registry.GetPool(lane);
            if (pool.Count == 0)
                return results;

            var available = pool.ToList();
            if (lane == ShopLane.Specialty && board != null)
            {
                var context = SpecialtyLaneRuleCatalog.Resolve(board, _registry);
                available = SpecialtyLaneRuleCatalog.FilterPool(available, context).ToList();
            }

            foreach (var fixedOffer in fixedSlots.Values)
                available.RemoveAll(p => p.Id == fixedOffer.PieceId);

            for (int i = 0; i < slotCount; i++)
            {
                if (fixedSlots.TryGetValue(i, out var fixedOffer))
                {
                    results.Add(CopyFixedOffer(fixedOffer, i, MapLaneToSlotKind(lane, i)));
                    continue;
                }

                if (available.Count == 0)
                    break;

                bool trySalvage = !string.IsNullOrEmpty(lastEnemyFactionId)
                    && salvageChancePercent > 0
                    && rng.NextInt(0, 100) < salvageChancePercent;

                if (trySalvage)
                {
                    var salvagePool = SalvageShopPool.GetPool(_registry, lane, lastEnemyFactionId, factionId, round);
                    if (salvagePool.Count > 0)
                    {
                        var piece = salvagePool[rng.NextInt(0, salvagePool.Count)];
                        available.RemoveAll(p => p.Id == piece.Id);
                        results.Add(CreateOffer(lane, MapLaneToSlotKind(lane, i), piece, modifiers, rng, round, i, isSalvaged: true));
                        continue;
                    }
                }

                int index = rng.NextInt(0, available.Count);
                var picked = ShopPoolFilter.PickWeighted(available, round, rng, playerFactionId: factionId) ?? available[index];
                available.RemoveAll(p => p.Id == picked.Id);
                results.Add(CreateOffer(lane, MapLaneToSlotKind(lane, i), picked, modifiers, rng, round, i));
            }

            return results;
        }

        private static ShopSlotKind MapLaneToSlotKind(ShopLane lane, int slotIndex)
        {
            if (lane == ShopLane.Defensive)
                return ShopSlotKind.BaselineDefensive;
            if (lane == ShopLane.Specialty)
                return ShopSlotKind.ExtraSpecialty;
            return slotIndex < ShopSlotLayoutResolver.BaselineSlotCount / 2
                ? ShopSlotKind.BaselineOffensive
                : ShopSlotKind.ExtraOffensive;
        }

        private static ShopOffer CopyFixedOffer(ShopOffer source, int slotIndex, ShopSlotKind kind) =>
            new ShopOffer
            {
                OfferId = source.OfferId,
                Lane = source.Lane,
                SlotIndex = slotIndex,
                SlotKind = kind,
                PieceId = source.PieceId,
                GoldPrice = source.GoldPrice,
                RequisitionPrice = source.RequisitionPrice,
                IsSalvaged = source.IsSalvaged
            };

        private void InjectDefensiveOffer(
            List<ShopOffer> offers,
            ShopModifiers modifiers,
            Rng rng,
            int round,
            IReadOnlyList<ShopSlotDefinition> slotLayout)
        {
            var buildings = _registry.GetBuildings();
            if (buildings.Count == 0)
                return;

            var piece = buildings[rng.NextInt(0, buildings.Count)];
            int slotIndex = slotLayout.FirstOrDefault(s => s.Kind == ShopSlotKind.BaselineDefensive)?.SlotIndex ?? 3;
            offers.Add(CreateOffer(ShopLane.Defensive, ShopSlotKind.BaselineDefensive, piece, modifiers, rng, round, slotIndex));
        }

        private ShopOffer CreateOffer(
            ShopLane lane,
            ShopSlotKind kind,
            PieceDefinition piece,
            ShopModifiers modifiers,
            Rng rng,
            int round,
            int slotIndex,
            bool isSalvaged = false)
        {
            int gold = ApplyGoldDiscount(piece.GoldCost, modifiers.GoldDiscountPercent);
            int requisition = piece.RequisitionCost;

            // Specialty-lane pieces keep authority pricing; any slot may carry requisition cost from data.
            if (lane == ShopLane.Specialty)
                requisition = piece.RequisitionCost;
            else if (kind != ShopSlotKind.ExtraSpecialty)
                requisition = 0;

            gold += Math.Max(0, round - 1);

            return new ShopOffer
            {
                OfferId = $"slot{slotIndex}_{piece.Id}",
                Lane = lane,
                SlotIndex = slotIndex,
                SlotKind = kind,
                PieceId = piece.Id,
                GoldPrice = gold,
                RequisitionPrice = requisition,
                IsSalvaged = isSalvaged
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
