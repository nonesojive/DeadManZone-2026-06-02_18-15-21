using System;
using DeadManZone.Core.Shop;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.DragDrop;
using DeadManZone.Presentation.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Shop
{
    public sealed class ShopOfferView : MonoBehaviour
    {
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text pieceIdText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Button lockButton;
        [SerializeField] private Image lockedIndicator;
        [SerializeField] private ShopOfferDragSource dragSource;

        private ShopOffer _offer;
        private bool _isLocked;

        public string OfferId => _offer?.OfferId;
        public ShopLane Lane => _offer?.Lane ?? ShopLane.Offensive;

        public event Action<ShopOffer, bool> LockToggled;

        private void Awake()
        {
            if (lockButton != null)
                lockButton.onClick.AddListener(OnLockClicked);

            if (pieceIdText != null)
                pieceIdText.raycastTarget = false;
            if (priceText != null)
                priceText.raycastTarget = false;

            if (dragSource == null)
                dragSource = GetComponent<ShopOfferDragSource>();
        }

        public void Bind(ShopOffer offer, bool isLocked)
        {
            _offer = offer;
            _isLocked = isLocked;

            var source = PieceVisualLookup.GetSource(offer.PieceId);
            if (cardBackground != null)
                UiThemeApplicator.ApplyCard(cardBackground);

            if (iconImage != null)
            {
                iconImage.sprite = source?.icon;
                iconImage.enabled = source?.icon != null;
            }

            if (pieceIdText != null)
                pieceIdText.text = source != null && !string.IsNullOrEmpty(source.displayName)
                    ? source.displayName
                    : offer.PieceId;

            if (priceText != null)
                priceText.text = BuildPriceLabel(offer);

            if (lockedIndicator != null)
            {
                lockedIndicator.enabled = isLocked;
                if (isLocked)
                    lockedIndicator.color = new Color(
                        UiThemeProvider.Current.accentColor.r,
                        UiThemeProvider.Current.accentColor.g,
                        UiThemeProvider.Current.accentColor.b,
                        0.35f);
            }

            if (lockButton != null)
            {
                var label = lockButton.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = isLocked ? "Unlock" : "Lock";
            }

            if (dragSource != null)
                dragSource.SetOffer(offer);
        }

        private static string BuildPriceLabel(ShopOffer offer)
        {
            if (offer.RequisitionPrice > 0 && offer.GoldPrice > 0)
                return $"{offer.GoldPrice}G + {offer.RequisitionPrice}R";
            if (offer.RequisitionPrice > 0)
                return $"{offer.RequisitionPrice}R";
            return $"{offer.GoldPrice}G";
        }

        private void OnLockClicked()
        {
            if (_offer == null)
                return;

            _isLocked = !_isLocked;
            LockToggled?.Invoke(_offer, _isLocked);
            Bind(_offer, _isLocked);
        }
    }
}
