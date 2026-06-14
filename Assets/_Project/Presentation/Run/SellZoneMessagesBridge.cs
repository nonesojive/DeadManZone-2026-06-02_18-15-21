using DeadManZone.Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeadManZone.Presentation.Run
{
    /// <summary>Shows sell refund hint in the messages panel when hovering the sell zone.</summary>
    public sealed class SellZoneMessagesBridge : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private BuildMessagesView messagesView;

        public void Configure(BuildMessagesView messages) => messagesView = messages;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (messagesView == null)
                messagesView = FindFirstObjectByType<BuildMessagesView>();

            messagesView?.SetSellHoverMessage("Drop a piece here to sell");
        }

        public void OnPointerExit(PointerEventData eventData) => messagesView?.ClearSellHover();
    }
}
