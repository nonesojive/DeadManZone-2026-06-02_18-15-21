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

        private readonly ContentRegistry _registry;
        private readonly ShopConfig _shopConfig;
        private readonly IShopSlotUnlockRegistry _unlockRegistry;

        public ShopGenerator(
            ContentRegistry registry,
            ShopConfig shopConfig = null,
            IShopSlotUnlockRegistry unlockRegistry = null)
        {
            _registry = registry;
            _shopConfig = shopConfig ?? ShopConfig.CreateDefault();
            _unlockRegistry = unlockRegistry ?? ShopSlotUnlockRegistry.Empty;
        }

        public ShopState Generate(
            BoardState board,
            string factionId,
            int round,
            int seed,
            string lastEnemyFactionId = null,
            int salvageChancePercent = 0,
            FactionShopOverride factionOverride = null)
        {
            var rng = new Rng(seed);
            var modifiers = ComputeModifiers(board);
            var slotLayout = ResolveSlotLayout(board, factionId, modifiers, factionOverride);

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
            FactionShopOverride factionOverride = null)
        {
            var slotLayout = ResolveSlotLayout(board, factionId, modifiers, factionOverride);
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

        private IReadOnlyList<ShopSlotProfile> ResolveSlotLayout(
            BoardState board,
            string factionId,
            ShopModifiers modifiers,
            FactionShopOverride factionOverride)
        {
            var context = new ShopUnlockContext
            {
                Board = board,
                FactionId = factionId,
                Registry = _registry,
                Modifiers = modifiers
            };

            return ShopSlotProfileResolver.ResolveActiveSlots(
                _shopConfig,
                _unlockRegistry,
                context,
                factionOverride);
        }

        private List<ShopOffer> RollSlots(
            IReadOnlyList<ShopSlotProfile> slotLayout,
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

            bool hasEnemyFaction = !string.IsNullOrEmpty(lastEnemyFactionId);

            foreach (var profile in slotLayout)
            {
                if (!ShopSlotLayoutResolver.RollsOffers(profile))
                    continue;

                if (fixedSlots.TryGetValue(profile.SlotIndex, out var fixedOffer))
                {
                    offers.Add(CopyFixedOffer(fixedOffer, profile));
                    continue;
                }

                var rolled = RollSingleSlot(
                    profile,
                    modifiers,
                    rng,
                    round,
                    factionId,
                    lastEnemyFactionId,
                    salvageChancePercent,
                    hasEnemyFaction,
                    consumedPieceIds);

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
            ShopSlotProfile profile,
            ShopModifiers modifiers,
            Rng rng,
            int round,
            string factionId,
            string lastEnemyFactionId,
            int salvageChancePercent,
            bool hasEnemyFaction,
            HashSet<string> consumedPieceIds)
        {
            var weights = ShopOfferWeightResolver.Resolve(
                profile,
                salvageChancePercent,
                hasEnemyFaction);

            var source = ShopOfferSourceRoller.Roll(weights, rng);
            var candidates = BuildCandidates(profile, source, factionId, lastEnemyFactionId, consumedPieceIds);

            if (candidates.Count == 0)
            {
                foreach (var fallback in new[] { ShopOfferSource.Faction, ShopOfferSource.Neutral, ShopOfferSource.Salvage })
                {
                    if (fallback == source)
                        continue;

                    candidates = BuildCandidates(profile, fallback, factionId, lastEnemyFactionId, consumedPieceIds);
                    if (candidates.Count > 0)
                    {
                        source = fallback;
                        break;
                    }
                }
            }

            if (candidates.Count == 0)
                return null;

            var picked = ShopPiecePicker.PickWeighted(
                candidates,
                profile.PreferredCombatRoles,
                profile.PreferredRoleWeight,
                rng);

            if (picked == null)
                return null;

            return CreateOffer(
                profile,
                picked,
                modifiers,
                round,
                isSalvaged: source == ShopOfferSource.Salvage && hasEnemyFaction);
        }

        private static ShopOffer CopyFixedOffer(ShopOffer source, ShopSlotProfile profile) =>
            new ShopOffer
            {
                OfferId = source.OfferId,
                Lane = profile.PoolLane,
                SlotIndex = profile.SlotIndex,
                SlotKind = profile.Kind,
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
            IReadOnlyList<ShopSlotProfile> slotLayout)
        {
            var buildings = _registry.GetBuildings();
            if (buildings.Count == 0)
                return;

            var piece = buildings[rng.NextInt(0, buildings.Count)];
            var profile = slotLayout.FirstOrDefault(s => s.Kind == ShopSlotKind.BaselineDefensive)
                ?? slotLayout.FirstOrDefault(s => s.PoolBias == ShopPoolBias.Defensive);
            if (profile == null)
                return;

            offers.Add(CreateOffer(profile, piece, modifiers, round));
        }

        private ShopOffer CreateOffer(
            ShopSlotProfile profile,
            PieceDefinition piece,
            ShopModifiers modifiers,
            int round,
            bool isSalvaged = false)
        {
            int gold = ApplyGoldDiscount(piece.GoldCost, modifiers.GoldDiscountPercent);
            int requisition = piece.RequisitionCost;
            gold += Math.Max(0, round - 1);

            return new ShopOffer
            {
                OfferId = $"slot{profile.SlotIndex}_{piece.Id}",
                Lane = profile.PoolLane,
                SlotIndex = profile.SlotIndex,
                SlotKind = profile.Kind,
                PieceId = piece.Id,
                GoldPrice = gold,
                RequisitionPrice = requisition,
                IsSalvaged = isSalvaged
            };
        }

        private List<PieceDefinition> BuildCandidates(
            ShopSlotProfile profile,
            ShopOfferSource source,
            string factionId,
            string lastEnemyFactionId,
            HashSet<string> consumedPieceIds) =>
            ShopOfferPoolBuilder.BuildCandidates(
                    _registry,
                    profile.PoolBias,
                    source,
                    factionId,
                    lastEnemyFactionId)
                .Where(p => !consumedPieceIds.Contains(p.Id))
                .ToList();

        private static int ApplyGoldDiscount(int baseGold, int discountPercent)
        {
            if (baseGold <= 0)
                return 0;

            int discounted = baseGold * (100 - discountPercent) / 100;
            return Math.Max(1, discounted);
        }
    }
}
