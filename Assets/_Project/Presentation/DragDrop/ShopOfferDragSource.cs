using DeadManZone.Core.Board;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Game.Dev;
using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Shop;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeadManZone.Presentation.DragDrop
{
    public sealed class ShopOfferDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private ShopOffer _offer;
        private PieceHoverCardController _hoverCardController;
        private ContentDatabase _database;

        public void SetOffer(ShopOffer offer) => _offer = offer;

        private void Awake()
        {
            _database = ContentDatabase.Load();
            ResolveHoverController();
        }

        private void OnEnable() => ResolveHoverController();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_offer == null)
                return;

            ResolveHoverController();
            if (_hoverCardController == null)
                return;

            var registry = ContentRegistryProvider.Build(_database ?? ContentDatabase.Load());
            if (registry == null || !registry.TryGetById(_offer.PieceId, out var definition) || definition == null)
                return;

            var context = BuildShopContext();
            _hoverCardController.NotifyPieceHoverEnter(_offer.OfferId, definition, context, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_offer == null)
            {
                _hoverCardController?.Hide();
                return;
            }

            _hoverCardController?.NotifyPieceHoverExit(_offer.OfferId);
        }

        private void OnDisable() => _hoverCardController?.Hide();

        public void OnBeginDrag(PointerEventData eventData)
        {
            _hoverCardController?.Hide();

            if (_offer == null || DragDropController.Instance == null)
                return;

            var view = GetComponentInParent<ShopOfferView>();
            view?.SetPreviewVisible(false);

            var registry = ContentRegistryProvider.Build(ContentDatabase.Load());
            var payload = new DragPayload
            {
                SourceKind = DragSourceKind.ShopOffer,
                OfferId = _offer.OfferId,
                PieceId = _offer.PieceId,
                Offer = _offer,
                Definition = registry?.GetById(_offer.PieceId),
                Rotation = PieceRotation.R0
            };

            ShopDragMetrics.Resolve(out float cellSize, out float spacing);
            DragDropController.Instance.BeginDrag(
                payload,
                transform,
                eventData,
                cellSize,
                spacing,
                pieceOnlyGhost: true);
        }

        public void OnDrag(PointerEventData eventData) =>
            DragDropController.Instance?.UpdateDrag(eventData);

        public void OnEndDrag(PointerEventData eventData)
        {
            DragDropController.Instance?.EndDrag(eventData);
            GetComponentInParent<ShopOfferView>()?.SetPreviewVisible(true);
        }

        private PieceCardBuildContext BuildShopContext()
        {
            var orchestrator = RunManager.Instance?.Orchestrator;
            var state = orchestrator?.State;
            string lastEnemyFactionId = state?.LastEnemyFactionId;

            return new PieceCardBuildContext
            {
                IsSalvaged = _offer?.IsSalvaged ?? false,
                LastEnemyFactionId = lastEnemyFactionId,
                LastEnemyFactionDisplayName = string.IsNullOrEmpty(lastEnemyFactionId)
                    ? null
                    : ResolveFactionDisplayName(lastEnemyFactionId),
                Board = orchestrator?.GetPlayerBoard()
            };
        }

        private string ResolveFactionDisplayName(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                return string.Empty;

            _database ??= ContentDatabase.Load();
            var faction = _database?.GetFaction(factionId);
            return faction != null && !string.IsNullOrEmpty(faction.displayName)
                ? faction.displayName
                : factionId;
        }

        private void ResolveHoverController()
        {
            if (_hoverCardController != null)
                return;

            _hoverCardController = FindFirstObjectByType<PieceHoverCardController>();
        }
    }
}
