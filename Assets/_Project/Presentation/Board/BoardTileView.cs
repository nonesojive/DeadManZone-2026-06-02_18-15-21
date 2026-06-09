using System;
using System.Collections;
using DeadManZone.Core.Common;
using DeadManZone.Presentation.Visual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    [RequireComponent(typeof(Image))]
    public sealed class BoardTileView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image baseImage;
        [SerializeField] private Image specialOverlay;
        [SerializeField] private Image placementOverlay;

        private Color _baseColor;
        private Sprite _terrainSprite;
        private bool _useBackdropMode;
        private Coroutine _pulseRoutine;

        public GridCoord Coord { get; private set; }
        public bool IsSpecial { get; private set; }
        public string OccupyingInstanceId { get; private set; }

        public event Action<GridCoord> Clicked;

        private void Awake()
        {
            if (baseImage == null)
                baseImage = GetComponent<Image>();
            if (baseImage != null)
                baseImage.raycastTarget = true;
        }

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(Coord);

        public void Initialize(
            GridCoord coord,
            Color baseColor,
            bool isSpecial,
            Sprite terrainSprite = null,
            bool useBackdropMode = false)
        {
            Coord = coord;
            IsSpecial = isSpecial;
            _baseColor = baseColor;
            _useBackdropMode = useBackdropMode;
            if (terrainSprite != null)
                _terrainSprite = terrainSprite;

            SetOverlay(baseColor, isSpecial, false);

            var hover = GetComponent<BoardTileHover>();
            if (hover != null)
                hover.SetBaseColor(baseColor);
        }

        public void SetBaseColor(Color color)
        {
            _baseColor = color;
            ApplyBaseVisual();
        }

        public void SetOverlay(Color baseColor, bool isSpecial, bool isInvalidPlacement)
        {
            _baseColor = baseColor;
            ApplyBaseVisual();

            var theme = UiThemeProvider.Current;
            if (specialOverlay != null)
            {
                specialOverlay.enabled = isSpecial;
                if (isSpecial)
                    specialOverlay.color = theme.specialTileColor;
            }

            if (placementOverlay != null)
            {
                placementOverlay.enabled = isInvalidPlacement;
                placementOverlay.color = theme.invalidPlacementColor;
            }
        }

        public void SetOccupied(string instanceId, bool occupied)
        {
            OccupyingInstanceId = occupied ? instanceId : null;
        }

        public void SetInvalidPreview(bool invalid)
        {
            SetOverlay(_baseColor, IsSpecial, invalid);
        }

        public void PulseHighlight(Color flashColor)
        {
            if (_pulseRoutine != null)
                StopCoroutine(_pulseRoutine);

            _pulseRoutine = StartCoroutine(PulseRoutine(flashColor));
        }

        private IEnumerator PulseRoutine(Color flashColor)
        {
            if (baseImage == null)
                yield break;

            var theme = UiThemeProvider.Current;
            var original = ResolveTileColor(theme);
            baseImage.color = Color.Lerp(original, flashColor, 0.65f);
            yield return new WaitForSeconds(0.12f);
            ApplyBaseVisual();
            _pulseRoutine = null;
        }

        private void ApplyBaseVisual()
        {
            if (baseImage == null)
                return;

            var theme = UiThemeProvider.Current;
            if (_useBackdropMode)
            {
                baseImage.sprite = null;
                baseImage.color = theme.GetBoardCellOverlayColor(_baseColor);
                return;
            }

            if (_terrainSprite != null)
            {
                baseImage.sprite = _terrainSprite;
                baseImage.type = Image.Type.Simple;
                baseImage.preserveAspect = false;
                baseImage.color = theme.GetTerrainTileTint(_baseColor);
                return;
            }

            baseImage.sprite = null;
            baseImage.color = theme.GetTileDisplayColor(_baseColor);
        }

        private Color ResolveTileColor(UiThemeSO theme)
        {
            if (_useBackdropMode)
                return theme.GetBoardCellOverlayColor(_baseColor);

            return _terrainSprite != null
                ? theme.GetTerrainTileTint(_baseColor)
                : theme.GetTileDisplayColor(_baseColor);
        }
    }
}
