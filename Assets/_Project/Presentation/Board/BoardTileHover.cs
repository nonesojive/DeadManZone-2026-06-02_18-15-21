using DeadManZone.Presentation.Visual;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeadManZone.Presentation.Board
{
  [RequireComponent(typeof(BoardTileView))]
  public sealed class BoardTileHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
  {
    private BoardTileView _tile;
    private Color _baseColor;
    private bool _hasBase;

    private void Awake() => _tile = GetComponent<BoardTileView>();

    public void SetBaseColor(Color color)
    {
      _baseColor = color;
      _hasBase = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
      if (_tile == null || !_hasBase)
        return;

      var hover = UiThemeProvider.Current.tileHoverColor;
      _tile.SetBaseColor(Color.Lerp(_baseColor, hover, 0.45f));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      if (_tile == null || !_hasBase)
        return;

      _tile.SetBaseColor(_baseColor);
    }
  }
}
