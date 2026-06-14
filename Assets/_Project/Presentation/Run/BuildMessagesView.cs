using DeadManZone.Core.Board;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using TMPro;
using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    /// <summary>Priority-stack message line: alerts &gt; sell hover &gt; unit flavor.</summary>
    public sealed class BuildMessagesView : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private float alertDurationSeconds = 4f;

        private string _sellHoverMessage = string.Empty;
        private string _rerollHoverMessage = string.Empty;
        private string _buffHoverMessage = string.Empty;
        private string _flavorMessage = string.Empty;
        private string _alertMessage = string.Empty;
        private float _alertClearTime;

        public void ShowAlert(string message)
        {
            _alertMessage = message ?? string.Empty;
            _alertClearTime = Time.unscaledTime + alertDurationSeconds;
            Refresh();
        }

        public void SetSellHoverMessage(string message)
        {
            _sellHoverMessage = message ?? string.Empty;
            Refresh();
        }

        public void ClearSellHover() => SetSellHoverMessage(string.Empty);

        public void SetRerollHoverMessage(string message)
        {
            _rerollHoverMessage = message ?? string.Empty;
            Refresh();
        }

        public void ClearRerollHover() => SetRerollHoverMessage(string.Empty);

        public void SetBuffHoverMessage(string message)
        {
            _buffHoverMessage = message ?? string.Empty;
            Refresh();
        }

        public void ClearBuffHover() => SetBuffHoverMessage(string.Empty);

        public void SetFlavorMessage(string message)
        {
            _flavorMessage = message ?? string.Empty;
            Refresh();
        }

        public void ClearFlavor() => SetFlavorMessage(string.Empty);

        public void SetFlavorFromPiece(PieceDefinition piece)
        {
            if (piece == null)
            {
                ClearFlavor();
                return;
            }

            string flavor = PieceCardTooltipFormatter.BuildAbilityText(piece.GrantedAbility);
            if (string.IsNullOrWhiteSpace(flavor))
                flavor = piece.DisplayName;
            SetFlavorMessage(flavor);
        }

        public void SetSellHoverFromPiece(PieceDefinition piece, string factionId)
        {
            if (piece == null)
            {
                ClearSellHover();
                return;

            }

            var refund = SalvageCalculator.Compute(piece, factionId);
            SetSellHoverMessage($"Sell: +{refund.Supplies} Supplies");
        }

        private void Update()
        {
            if (!string.IsNullOrEmpty(_alertMessage) && Time.unscaledTime >= _alertClearTime)
            {
                _alertMessage = string.Empty;
                Refresh();
            }
        }

        private void Refresh()
        {
            if (messageText == null)
                return;

            if (!string.IsNullOrWhiteSpace(_alertMessage))
                messageText.text = _alertMessage;
            else if (!string.IsNullOrWhiteSpace(_sellHoverMessage))
                messageText.text = _sellHoverMessage;
            else if (!string.IsNullOrWhiteSpace(_rerollHoverMessage))
                messageText.text = _rerollHoverMessage;
            else if (!string.IsNullOrWhiteSpace(_buffHoverMessage))
                messageText.text = _buffHoverMessage;
            else if (!string.IsNullOrWhiteSpace(_flavorMessage))
                messageText.text = _flavorMessage;
            else
                messageText.text = string.Empty;
        }
    }
}
