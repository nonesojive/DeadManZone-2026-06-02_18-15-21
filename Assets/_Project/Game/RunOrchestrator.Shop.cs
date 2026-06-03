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

            if (offer.Lane == ShopLane.Specialty && offer.RequisitionPrice > 0)
                return State.Authority >= offer.RequisitionPrice;

            return true;
        }

        public bool IsOfferLocked(ShopOffer offer) =>
            offer != null &&
            State.LockedOffer != null &&
            State.LockedOffer.Lane == offer.Lane &&
            State.LockedOffer.PieceId == offer.PieceId;

        public void SetLockedOffer(ShopOffer offer, bool locked)
        {
            State.LockedOffer = locked && offer != null ? ShopOfferRecord.FromOffer(offer) : null;
            State.FrozenOfferId = null;
            Persist();
        }

        public bool TryAcquireOfferToBench(string offerId)
        {
            if (State.Phase != RunPhase.Build)
                return false;

            if (State.BenchPieceIds.Count >= BenchLimit || !CanAffordOffer(offerId))
                return false;

            var offer = FindOffer(offerId);
            if (offer == null)
                return false;

            PayOffer(offer);
            State.BenchPieceIds.Add(offer.PieceId);
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

        public bool TryAcquireOfferToBoard(string offerId, GridCoord anchor, string instanceId = null)
        {
            if (State.Phase != RunPhase.Build)
                return false;

            if (!CanAcquireOfferToBoard(offerId, anchor))
                return false;

            var offer = FindOffer(offerId);
            var piece = _registry.GetById(offer.PieceId);
            var board = GetPlayerBoard();
            instanceId ??= Guid.NewGuid().ToString("N");

            PayOffer(offer);

            var place = board.TryPlace(piece, anchor, instanceId);
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

        public bool TryPlaceFromBench(int benchIndex, GridCoord anchor)
        {
            if (State.Phase != RunPhase.Build)
                return false;

            if (benchIndex < 0 || benchIndex >= State.BenchPieceIds.Count)
                return false;

            var pieceId = State.BenchPieceIds[benchIndex];
            var piece = _registry.GetById(pieceId);
            var board = GetPlayerBoard();
            var place = board.TryPlace(piece, anchor, Guid.NewGuid().ToString("N"));
            if (!place.Success)
                return false;

            State.BenchPieceIds.RemoveAt(benchIndex);
            SavePlayerBoard(board);
            Persist();
            return true;
        }

        public bool TrySellFromBench(int benchIndex)
        {
            if (State.Phase != RunPhase.Build)
                return false;

            if (benchIndex < 0 || benchIndex >= State.BenchPieceIds.Count)
                return false;

            var pieceId = State.BenchPieceIds[benchIndex];
            var piece = _registry.GetById(pieceId);
            int refund = Math.Max(0, piece.GoldCost / 2);
            State.BenchPieceIds.RemoveAt(benchIndex);
            State.Supplies += refund;
            Persist();
            return true;
        }

        public bool TryPurchaseOffer(string offerId) => TryAcquireOfferToBench(offerId);

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

        private void PayOffer(ShopOffer offer)
        {
            State.Supplies -= offer.GoldPrice;
            if (offer.Lane == ShopLane.Specialty && offer.RequisitionPrice > 0)
                State.Authority -= offer.RequisitionPrice;
        }

        private void RefundOffer(ShopOffer offer)
        {
            State.Supplies += offer.GoldPrice;
            if (offer.Lane == ShopLane.Specialty && offer.RequisitionPrice > 0)
                State.Authority += offer.RequisitionPrice;
        }

        private void RemoveOffer(string offerId)
        {
            if (State.Shop?.Offers == null)
                return;

            var offer = State.Shop.Offers.FirstOrDefault(o => o.OfferId == offerId);
            if (offer != null && IsOfferLocked(offer))
                State.LockedOffer = null;

            State.Shop.Offers.RemoveAll(o => o.OfferId == offerId);
        }

        private void ApplyLockedOffer(ShopState shop)
        {
            if (shop?.Offers == null || State.LockedOffer == null)
                return;

            var locked = State.LockedOffer.ToOffer();
            shop.Offers.RemoveAll(o => o.Lane == locked.Lane && o.PieceId == locked.PieceId);

            var laneOffers = shop.Offers.Where(o => o.Lane == locked.Lane).ToList();
            shop.Offers.RemoveAll(o => o.Lane == locked.Lane);

            shop.Offers.Add(locked);
            foreach (var offer in laneOffers)
                shop.Offers.Add(offer);
        }

        private void RefreshShop()
        {
            var board = GetPlayerBoard();
            int shopSeed = State.RunSeed + State.FightIndex * 100 + State.RerollCountThisRound;
            var shop = _shopGenerator.Generate(board, State.FactionId, State.FightIndex, shopSeed);
            ApplyLockedOffer(shop);
            State.Shop = shop;
        }

        private void ReplaceNonRerolledLanes(ShopState previousShop, ShopLane rerolledLane)
        {
            if (previousShop?.Offers == null || State.Shop?.Offers == null)
                return;

            var rerolledOffers = State.Shop.Offers
                .Where(o => o.Lane == rerolledLane)
                .ToList();

            var preserved = previousShop.Offers
                .Where(o => o.Lane != rerolledLane)
                .ToList();

            preserved.AddRange(rerolledOffers);
            State.Shop.Offers = preserved;
            ApplyLockedOffer(State.Shop);
        }
    }
}
