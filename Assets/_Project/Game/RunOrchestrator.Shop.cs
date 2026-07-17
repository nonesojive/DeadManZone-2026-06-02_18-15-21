using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;

namespace DeadManZone.Game
{
    public sealed partial class RunOrchestrator
    {
        public bool CanAffordOffer(string offerId)
        {
            var offer = FindOffer(offerId);
            if (offer == null)
                return false;

            if (State.Supplies < offer.GoldPrice)
                return false;

            if (offer.RequisitionPrice > 0 && State.Authority < offer.RequisitionPrice)
                return false;

            return true;
        }

        public int ComputeRerollLockAuthorityCost()
        {
            int lockCount = GetLockedOffers().Count;
            return Math.Max(0, lockCount - 1);
        }

        /// <summary>Paradox Engine's passive (§1.9): the first reroll each Build phase is
        /// free (Supplies only — lock Authority costs are untouched). No-op for every
        /// other faction.</summary>
        public int ComputeRerollGoldCost()
        {
            if (FactionPassives.HasFreeFirstReroll(State.FactionId) && State.RerollCountThisRound == 0)
                return 0;

            return BaseRerollCost + State.RerollCountThisRound;
        }

        public bool CanRerollShop()
        {
            int goldCost = ComputeRerollGoldCost();
            if (State.Supplies < goldCost)
                return false;

            int authorityCost = ComputeRerollLockAuthorityCost();
            return State.Authority >= authorityCost;
        }

        public bool IsOfferLocked(ShopOffer offer) =>
            offer != null &&
            GetLockedOffers().Any(locked => locked.SlotIndex == offer.SlotIndex);

        public void SetLockedOffer(ShopOffer offer, bool locked)
        {
            if (State.LockedOffers == null)
                State.LockedOffers = new List<ShopOfferRecord>();

            if (!locked || offer == null)
            {
                State.LockedOffers.RemoveAll(l => l.SlotIndex == offer?.SlotIndex);
            }
            else
            {
                State.LockedOffers.RemoveAll(l => l.SlotIndex == offer.SlotIndex);
                State.LockedOffers.Add(ShopOfferRecord.FromOffer(offer));
            }

            State.FrozenOfferId = null;
            Persist();
        }

        public bool TryAcquireOfferToReserves(string offerId, GridCoord anchor, PieceRotation rotation = PieceRotation.R0)
        {
            if (State.Phase != RunPhase.Build || !CanAffordOffer(offerId))
                return false;

            var offer = FindOffer(offerId);
            if (offer == null)
                return false;

            var piece = _registry.GetById(offer.PieceId);
            var reserves = GetReserves();
            if (!reserves.CanPlace(piece, anchor, rotation))
                return false;

            PayOffer(offer);

            var place = reserves.TryPlace(piece, anchor, Guid.NewGuid().ToString("N"), rotation, offer.IsMercenary);
            if (!place.Success)
            {
                RefundOffer(offer);
                return false;
            }

            State.Reserves = ReservesSnapshotMapper.FromReserves(reserves);
            RemoveOffer(offerId);
            Persist();
            return true;
        }

        public bool CanAcquireOfferToBoard(string offerId, GridCoord anchor)
        {
            var offer = FindOffer(offerId);
            if (offer == null || !CanAffordOffer(offerId))
                return false;

            var piece = _registry.GetById(offer.PieceId);
            return GetBoardForPiece(piece).CanPlace(piece, anchor);
        }

        public bool TryAcquireOfferToBoard(
            string offerId,
            GridCoord anchor,
            string instanceId = null,
            PieceRotation rotation = PieceRotation.R0)
        {
            if (State.Phase != RunPhase.Build)
                return false;

            var offer = FindOffer(offerId);
            if (offer == null || !CanAffordOffer(offerId))
                return false;

            var piece = _registry.GetById(offer.PieceId);
            var board = GetBoardForPiece(piece);
            if (!board.CanPlace(piece, anchor, rotation))
                return false;

            instanceId ??= Guid.NewGuid().ToString("N");

            PayOffer(offer);

            var place = board.TryPlace(piece, anchor, instanceId, rotation, offer.IsMercenary);
            if (!place.Success)
            {
                RefundOffer(offer);
                return false;
            }

            SaveBoardForPiece(piece, board);
            RemoveOffer(offerId);
            Persist();
            return true;
        }

        public bool TryPlaceFromReserves(
            string instanceId,
            GridCoord boardAnchor,
            PieceRotation rotation = PieceRotation.R0)
        {
            if (State.Phase != RunPhase.Build)
                return false;

            var reserves = GetReserves();
            if (!reserves.TryRemove(instanceId, out var removed))
                return false;

            var piece = removed.Definition;
            var board = GetBoardForPiece(piece);
            var place = board.TryPlace(piece, boardAnchor, removed.InstanceId, rotation, removed.IsMercenary);
            if (!place.Success)
            {
                reserves.TryPlace(removed.Definition, removed.Anchor, removed.InstanceId, removed.Rotation, removed.IsMercenary);
                return false;
            }

            SaveReserves(reserves);
            SaveBoardForPiece(piece, board);
            return true;
        }

        public bool TrySellFromReserves(string instanceId)
        {
            if (State.Phase != RunPhase.Build)
                return false;

            var reserves = GetReserves();
            if (!reserves.TryRemove(instanceId, out var removed))
                return false;

            var refund = SalvageCalculator.Compute(removed.Definition, State.FactionId, removed.IsMercenary);
            State.Supplies += refund.Supplies;
            State.Authority += refund.Authority;
            State.Manpower += refund.Manpower;
            SaveReserves(reserves);
            Persist();
            return true;
        }

        public bool TryMoveBoardToReserves(
            string boardInstanceId,
            GridCoord reservesAnchor,
            PieceRotation rotation = PieceRotation.R0)
        {
            if (State.Phase != RunPhase.Build)
                return false;

            if (!TryFindPlacedPiece(boardInstanceId, out var board, out var removed))
                return false;

            // 2026-07-17 round-3 playtest fix: a transport carrying cargo has nowhere on
            // ReservesState to keep its hold — refuse rather than orphan the cargo's
            // CarrierInstanceId tags. Sell it (evicts cargo to reserves) instead.
            if (removed.Definition.IsTransport && board.GetCargo(boardInstanceId).Count > 0)
                return false;

            if (!board.TryRemove(boardInstanceId, out removed))
                return false;

            var reserves = GetReserves();
            var place = reserves.TryPlace(
                removed.Definition,
                reservesAnchor,
                removed.InstanceId,
                rotation,
                removed.IsMercenary);
            if (!place.Success)
            {
                board.TryPlace(removed.Definition, removed.Anchor, removed.InstanceId, removed.Rotation, removed.IsMercenary);
                return false;
            }

            SaveBoardForPiece(removed.Definition, board);
            SaveReserves(reserves);
            return true;
        }

        public bool TryPurchaseOffer(string offerId) =>
            TryAcquireOfferToReserves(offerId, new GridCoord(0, 0), PieceRotation.R0);

        public void SetFrozenOffer(string offerId)
        {
            var offer = FindOffer(offerId);
            if (offer != null)
                SetLockedOffer(offer, locked: true);
            else
            {
                State.FrozenOfferId = offerId;
                Persist();
            }
        }

        private ShopOffer FindOffer(string offerId) =>
            State.Shop?.Offers?.FirstOrDefault(o => o.OfferId == offerId);

        private List<ShopOfferRecord> GetLockedOffers() =>
            State.LockedOffers ?? new List<ShopOfferRecord>();

        private void PayOffer(ShopOffer offer)
        {
            State.Supplies -= offer.GoldPrice;
            if (offer.RequisitionPrice > 0)
                State.Authority -= offer.RequisitionPrice;
        }

        private void RefundOffer(ShopOffer offer)
        {
            State.Supplies += offer.GoldPrice;
            if (offer.RequisitionPrice > 0)
                State.Authority += offer.RequisitionPrice;
        }

        private void RemoveOffer(string offerId)
        {
            if (State.Shop?.Offers == null)
                return;

            var offer = State.Shop.Offers.FirstOrDefault(o => o.OfferId == offerId);
            if (offer != null && IsOfferLocked(offer))
                State.LockedOffers?.RemoveAll(l => l.SlotIndex == offer.SlotIndex);

            State.Shop.Offers.RemoveAll(o => o.OfferId == offerId);
        }

        private void ApplyLockedSlots(ShopState shop)
        {
            if (shop?.Offers == null)
                return;

            foreach (var lockedRecord in GetLockedOffers())
            {
                var locked = lockedRecord.ToOffer();
                shop.Offers.RemoveAll(o => o.SlotIndex == locked.SlotIndex);
                shop.Offers.Add(locked);
            }
        }

        private void RefreshShop()
        {
            SyncSalvageChancePercent();
            var board = GetShopBoard();
            // Seed indexes on FightIndex (counter semantics: a fresh roll per round);
            // the generator's round parameter is DIFFICULTY (price scaling), so it keys
            // on the Dread clock (M1, ADR-0004).
            int shopSeed = SeedStreams.Derive(
                State.RunSeed, "shop", State.FightIndex, State.RerollCountThisRound);
            var shop = _shopGenerator.Generate(
                board,
                State.FactionId,
                DreadRules.FightEquivalent(State.Dread),
                shopSeed,
                State.LastEnemyFactionId,
                State.SalvageChancePercent,
                _content.GetShopOverride(State.FactionId),
                State.RarePityBatches,
                State.SalvageHardBoost,
                State.SalvagePityBatches);
            ApplyLockedSlots(shop);
            State.Shop = shop;
            UpdateRarePity(shop.Offers);
            UpdateSalvagePity(shop.Offers);
        }

        /// <summary>M3 pity, appear-reset: every generated batch (round roll or reroll)
        /// either resets the counter (a rare-or-above is SHOWN — locked slots included)
        /// or climbs it. Pity is state-derived, no extra randomness, so seeded runs
        /// with the same reroll sequence stay identical.</summary>
        private void UpdateRarePity(IEnumerable<ShopOffer> offers)
        {
            State.RarePityBatches = ShopGenerator.ContainsRareOrAbove(offers, _registry)
                ? 0
                : State.RarePityBatches + 1;
        }

        /// <summary>§1.5 edge case: while the salvage pool is empty (no last-fought enemy
        /// yet, or that faction has no registered pieces), the counter HOLDS — it neither
        /// resets nor climbs, so a fresh run's early rounds don't burn through the pity
        /// clock before there's anything to salvage.</summary>
        private void UpdateSalvagePity(IEnumerable<ShopOffer> offers)
        {
            if (SalvagePoolAvailability.IsEmpty(_registry, State.LastEnemyFactionId, State.FactionId))
                return;

            State.SalvagePityBatches = SalvagePityRules.ContainsSalvageOffer(offers)
                ? 0
                : State.SalvagePityBatches + 1;
        }

        private void ApplyMuster()
        {
            var board = GetShopBoard();
            int gained = MusterCalculator.Compute(board, Faction.baseMusterPerShop);
            State.Manpower += gained;
            State.LastMusterGained = gained;
        }

        private void RerollShopOffers()
        {
            if (State.Shop?.Offers == null)
                return;

            var board = GetShopBoard();
            var modifiers = State.Shop.Modifiers ?? ShopGenerator.ComputeModifiers(board);
            var fixedSlots = new Dictionary<int, ShopOffer>();

            foreach (var lockedRecord in GetLockedOffers())
            {
                var locked = lockedRecord.ToOffer();
                fixedSlots[locked.SlotIndex] = locked;
            }

            int shopSeed = SeedStreams.Derive(
                State.RunSeed, "shop", State.FightIndex, State.RerollCountThisRound);
            var rerolled = _shopGenerator.RollShopOffers(
                board,
                State.FactionId,
                modifiers,
                shopSeed,
                DreadRules.FightEquivalent(State.Dread),
                fixedSlots,
                State.LastEnemyFactionId,
                State.SalvageChancePercent,
                _content.GetShopOverride(State.FactionId),
                State.RarePityBatches,
                State.SalvageHardBoost,
                State.SalvagePityBatches);

            State.Shop.Offers = rerolled;
            UpdateRarePity(rerolled);
            UpdateSalvagePity(rerolled);
        }
    }
}
