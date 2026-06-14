using UnityEngine;
using UnityEngine.EventSystems;

namespace DeadManZone.Presentation.Run
{
    /// <summary>Routes buff-strip icon pointer events to <see cref="BuffIconStripView"/>.</summary>
    public sealed class BuffIconHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private BuffIconStripView strip;
        [SerializeField] private int iconIndex;

        public void Configure(BuffIconStripView owner, int index)
        {
            strip = owner;
            iconIndex = index;
        }

        public void OnPointerEnter(PointerEventData eventData) => strip?.ShowDetail(iconIndex);

        public void OnPointerExit(PointerEventData eventData) => strip?.ClearDetail();
    }
}
