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
            FactionShopOverride factionOverride = null,
            int rarePityBatches = 0,
            bool salvageRareBoost = false)
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
                board,
                rarePityBatches,
                salvageRareBoost);

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
            FactionShopOverride factionOverride = null,
            int rarePityBatches = 0,
            bool salvageRareBoost = false)
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
                board,
                rarePityBatches,
                salvageRareBoost);
        }

        /// <summary>Whether a generated batch shows a rare-or-above offer — the pity
        /// counter's reset condition (appearing, not purchased). The orchestrator owns
        /// the RunState mutation at both Generate call sites.</summary>
        public static bool ContainsRareOrAbove(
            IEnumerable<ShopOffer> offers,
            ContentRegistry registry)
        {
            if (offers == null || registry == null)
                return false;

            foreach (var offer in offers)
            {
                if (registry.TryGetById(offer.PieceId, out var piece)
                    && piece.Rarity >= Rarity.Rare)
                    return true;
            }

            return false;
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
            BoardState board,
            int rarePityBatches = 0,
            bool salvageRareBoost = false)
        {
            fixedSlots ??= new Dictionary<int, ShopOffer>();
            var offers = new List<ShopOffer>();
            var consumedPieceIds = new HashSet<string>();

            foreach (var fixedOffer in fixedSlots.Values)
                consumedPieceIds.Add(fixedOffer.PieceId);

            bool hasEnemyFaction = !string.IsNullOrEmpty(lastEnemyFactionId);

            // Pity guarantee (M3): at the cap the FIRST slot whose candidates can host
            // a rare gets its tier forced. If no slot can, nothing is forced and the
            // orchestrator's counter simply keeps climbing.
            bool forceRarePending = RarityWeights.ForcesRare(round, rarePityBatches);

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
                    consumedPieceIds,
                    rarePityBatches,
                    salvageRareBoost,
                    ref forceRarePending);

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
            HashSet<string> consumedPieceIds,
            int rarePityBatches,
            bool salvageRareBoost,
            ref bool forceRarePending)
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

            PieceDefinition picked;
            if (source == ShopOfferSource.Salvage && hasEnemyFaction)
            {
                // Salvage quality rule (M3): salvage spoils skip the Dread tier gate
                // (today's roll, unchanged) — but a hard-front victory upweights the
                // pick by tier (Rare x3 / Uncommon x2 / Common x1; initial, tune in
                // playtest).
                picked = salvageRareBoost
                    ? PickSalvageUpweighted(candidates, rng)
                    : ShopPiecePicker.PickWeighted(
                        candidates,
                        profile.PreferredCombatRoles,
                        profile.PreferredRoleWeight,
                        rng);
            }
            else
            {
                Rarity tier;
                if (forceRarePending && candidates.Any(p => p.Rarity >= Rarity.Rare))
                {
                    tier = Rarity.Rare;
                    forceRarePending = false;
                }
                else
                {
                    tier = RarityWeights.RollTier(rng, round, rarePityBatches);
                }

                picked = ShopPiecePicker.PickWeighted(
                    FilterTierWithFallback(candidates, tier),
                    profile.PreferredCombatRoles,
                    profile.PreferredRoleWeight,
                    rng);
            }

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

        /// <summary>Keeps the offer at its rolled tier when the lane can serve it,
        /// otherwise falls DOWN tiers (Rare→Uncommon→Common) — an offer never promotes
        /// above its rolled tier. Public for tests. Last resort (a lane whose candidates
        /// have nothing at or below the rolled tier, e.g. zero Commons): return the full
        /// candidate list so the slot stays stocked rather than empty.</summary>
        public static List<PieceDefinition> FilterTierWithFallback(
            List<PieceDefinition> candidates,
            Rarity tier)
        {
            for (int t = (int)tier; t >= (int)Rarity.Common; t--)
            {
                var filtered = candidates.Where(p => (int)p.Rarity == t).ToList();
                if (filtered.Count > 0)
                    return filtered;
            }

            return candidates;
        }

        /// <summary>Hard-front victory salvage pick: per-piece weight by rarity tier
        /// (M3 initial: Rare x3 / Uncommon x2 / Common x1 — tune in playtest).</summary>
        private static PieceDefinition PickSalvageUpweighted(
            List<PieceDefinition> candidates,
            Rng rng)
        {
            if (candidates.Count == 0)
                return null;

            if (candidates.Count == 1)
                return candidates[0];

            int total = 0;
            var weights = new int[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                weights[i] = candidates[i].Rarity switch
                {
                    Rarity.Rare => 3,
                    Rarity.Uncommon => 2,
                    _ => 1
                };
                total += weights[i];
            }

            int roll = rng.NextInt(0, total);
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= weights[i];
                if (roll < 0)
                    return candidates[i];
            }

            return candidates[^1];
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
