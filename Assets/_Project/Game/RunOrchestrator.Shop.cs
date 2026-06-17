using System;
using System.Collections.Generic;
using System.Linq;
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

        public bool CanRerollShop()
        {
            int goldCost = BaseRerollCost + State.RerollCountThisRound;
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

            State.LockedOffer = null;
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

            var place = reserves.TryPlace(piece, anchor, Guid.NewGuid().ToString("N"), rotation);
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
            return GetPlayerBoard().CanPlace(piece, anchor);
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
            var board = GetPlayerBoard();
            if (!board.CanPlace(piece, anchor, rotation))
                return false;

            instanceId ??= Guid.NewGuid().ToString("N");

            PayOffer(offer);

            var place = board.TryPlace(piece, anchor, instanceId, rotation);
            if (!place.Success)
            {
                RefundOffer(offer);
                return false;
            }

            SavePlayerBoard(board);
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

            var board = GetPlayerBoard();
            var place = board.TryPlace(removed.Definition, boardAnchor, removed.InstanceId, rotation);
            if (!place.Success)
            {
                reserves.TryPlace(removed.Definition, removed.Anchor, removed.InstanceId, removed.Rotation);
                return false;
            }

            SaveReserves(reserves);
            SavePlayerBoard(board);
            return true;
        }

        public bool TrySellFromReserves(string instanceId)
        {
            if (State.Phase != RunPhase.Build)
                return false;

            var reserves = GetReserves();
            if (!reserves.TryRemove(instanceId, out var removed))
                return false;

            var refund = SalvageCalculator.Compute(removed.Definition, State.FactionId);
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

            var board = GetPlayerBoard();
            if (!board.TryRemove(boardInstanceId, out var removed))
                return false;

            var reserves = GetReserves();
            var place = reserves.TryPlace(
                removed.Definition,
                reservesAnchor,
                removed.InstanceId,
                rotation);
            if (!place.Success)
            {
                board.TryPlace(removed.Definition, removed.Anchor, removed.InstanceId, removed.Rotation);
                return false;
            }

            SavePlayerBoard(board);
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

        private List<ShopOfferRecord> GetLockedOffers()
        {
            if (State.LockedOffers != null && State.LockedOffers.Count > 0)
                return State.LockedOffers;

            if (State.LockedOffer != null)
                return new List<ShopOfferRecord> { State.LockedOffer };

            return new List<ShopOfferRecord>();
        }

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
            var board = GetPlayerBoard();
            int shopSeed = State.RunSeed + State.FightIndex * 100 + State.RerollCountThisRound;
            var shop = _shopGenerator.Generate(
                board,
                State.FactionId,
                State.FightIndex,
                shopSeed,
                State.LastEnemyFactionId,
                State.SalvageChancePercent,
                _content.GetShopOverride(State.FactionId));
            ApplyLockedSlots(shop);
            State.Shop = shop;
        }

        private void ApplyMuster()
        {
            var board = GetPlayerBoard();
            int gained = MusterCalculator.Compute(board, Faction.baseMusterPerShop);
            State.Manpower += gained;
            State.LastMusterGained = gained;
        }

        private void RerollShopOffers()
        {
            if (State.Shop?.Offers == null)
                return;

            var board = GetPlayerBoard();
            var modifiers = State.Shop.Modifiers ?? ShopGenerator.ComputeModifiers(board);
            var fixedSlots = new Dictionary<int, ShopOffer>();

            foreach (var lockedRecord in GetLockedOffers())
            {
                var locked = lockedRecord.ToOffer();
                fixedSlots[locked.SlotIndex] = locked;
            }

            int shopSeed = State.RunSeed + State.FightIndex * 100 + State.RerollCountThisRound;
            var rerolled = _shopGenerator.RollShopOffers(
                board,
                State.FactionId,
                modifiers,
                shopSeed,
                State.FightIndex,
                fixedSlots,
                State.LastEnemyFactionId,
                State.SalvageChancePercent,
                _content.GetShopOverride(State.FactionId));

            State.Shop.Offers = rerolled;
        }
    }
}
