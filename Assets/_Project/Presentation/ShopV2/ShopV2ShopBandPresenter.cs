using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Content;
using DeadManZone.Core.Run;
using DeadManZone.Core.Shop;
using DeadManZone.Data;
using DeadManZone.Game;
using DeadManZone.Game.Dev;
using DeadManZone.Presentation.Combat;
using DeadManZone.Presentation.DragDrop;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadManZone.Presentation.ShopV2
{
    /// <summary>Binds the ShopBand's five offer slots (plus the optional Cartel mercenary slot,
    /// `OfferSlot_5`) and the reroll plate to RunState.Shop. Attach to `ShopBand`.</summary>
    public sealed class ShopV2ShopBandPresenter : ShopV2PresenterBase
    {
        private const int OfferSlotCount = 5;
        private const float ShapeCellSize = 12f;
        private const float ShapeCellStep = 13f;
        private const float ShapePadding = 4f;

        // ---------------------------------------------------------------------------------
        // Slot visual vocabulary. The ShopV2 layout was authored as a MOCK: each slot was
        // hand-painted to showcase a different state (slot 1 = locked, slots 2/3 = hover),
        // and those colors are decoration, not data. The presenter therefore owns EVERY
        // stateful color on a slot and writes all of them on every bind. Never seed a
        // baseline from the authored color — slot 1's authored background IS LockedGoldTint,
        // so "remember the original and restore it" would pin that slot to looking locked
        // forever. Display only: lock semantics live in Game/Core; we mirror LockedOffers.
        // ---------------------------------------------------------------------------------
        private static readonly Color SlotBase = new(0.08f, 0.07f, 0.06f, 1f);
        private static readonly Color LockedGoldTint = new(0.42f, 0.35f, 0.21f, 1f);

        private static readonly Color BorderIdle = new(0.17f, 0.14f, 0.10f, 1f);
        private static readonly Color BorderHover = new(0.90f, 0.78f, 0.50f, 0.80f);
        private static readonly Color BorderLocked = new(0.90f, 0.78f, 0.50f, 1f);

        private static readonly Color NameIdle = new(0.92f, 0.87f, 0.74f, 1f);
        private static readonly Color NameLocked = new(0.90f, 0.78f, 0.50f, 1f);

        // kit-sprite slots multiply-tint the metal frame; flat colors are for legacy spriteless borders, 2026-07-18
        private static readonly Color KitBorderIdle = new(0.82f, 0.80f, 0.76f, 1f);
        private static readonly Color KitBorderHover = Color.white;
        private static readonly Color KitBorderLocked = new(0.45f, 0.42f, 0.38f, 1f);

        private static readonly Color ShapeCellColor = new(
            CombatGrimdarkSkin.Bone.r, CombatGrimdarkSkin.Bone.g, CombatGrimdarkSkin.Bone.b, 0.45f);

        private sealed class Slot
        {
            public GameObject Root;
            public Image Background;
            public Image Border;
            public Image Role;
            public TMP_Text Name;
            public TMP_Text Rarity;
            public TMP_Text CostVal;
            public GameObject LockBanner;
            public ShopV2OfferSlotInput Input;
            public bool MocksCleared;

            /// <summary>Live state the visuals are derived from — never read back off the graphics.</summary>
            public bool Locked;
            public bool Hovered;
        }

        private readonly List<Slot> _slots = new();

        /// <summary>The Cartel mercenary slot (`OfferSlot_5`, offer SlotIndex 9). Null in scenes
        /// authored before the slot existed — every use is null-guarded.</summary>
        private Slot _mercSlot;

        private Button _rerollButton;
        private TMP_Text _rerollCostVal;
        private ContentDatabase _database;

        private void Awake()
        {
            _database = ContentDatabase.Load();

            var missing = new List<string>();
            for (int i = 0; i < OfferSlotCount; i++)
            {
                var root = transform.Find($"OfferSlot_{i}");
                if (root == null)
                {
                    missing.Add($"OfferSlot_{i}");
                    continue;
                }

                var slot = BuildSlot(root);
                slot.Input.HoverChanged += hovered =>
                {
                    slot.Hovered = hovered;
                    ApplyVisualState(slot);
                };
                _slots.Add(slot);
            }

            // Cartel mercenary slot: hand-authored, inactive by default, absent in older scenes —
            // transform.Find sees inactive children, and absence is NOT an error.
            var mercRoot = transform.Find("OfferSlot_5");
            if (mercRoot != null)
            {
                _mercSlot = BuildSlot(mercRoot);
                _mercSlot.Input.HoverChanged += hovered =>
                {
                    _mercSlot.Hovered = hovered;
                    ApplyVisualState(_mercSlot);
                };
            }

            var rerollPlate = transform.Find("RerollPlate");
            if (rerollPlate != null)
            {
                _rerollButton = rerollPlate.GetComponentInChildren<Button>(true);
                var costVal = rerollPlate.Find("CostVal");
                _rerollCostVal = costVal != null ? costVal.GetComponent<TMP_Text>() : null;
            }

            if (_rerollButton != null)
                _rerollButton.onClick.AddListener(OnRerollClicked);
            else
                missing.Add("RerollPlate (Button)");

            if (missing.Count > 0)
                Debug.LogWarning($"ShopV2ShopBandPresenter: missing children: {string.Join(", ", missing)}", this);
        }

        private static Slot BuildSlot(Transform root)
        {
            var border = root.Find("Border");
            var role = root.Find("Role");
            var name = root.Find("Name");
            var rarity = root.Find("Rarity");
            var costVal = root.Find("CostVal");
            var lockBanner = root.Find("LockBanner");

            var input = root.GetComponent<ShopV2OfferSlotInput>();
            if (input == null)
                input = root.gameObject.AddComponent<ShopV2OfferSlotInput>();

            // NOTE: the slot's Image must have raycastTarget=true or this input component never
            // fires and the slot is silently dead. That flag is AUTHORED in the scene — see the
            // "mock decoration is not state" section of docs/shopv2-flip-checklist.md.
            var background = root.GetComponent<Image>();

            return new Slot
            {
                Root = root.gameObject,
                Background = background,
                Border = border != null ? border.GetComponent<Image>() : null,
                Role = role != null ? role.GetComponent<Image>() : null,
                Name = name != null ? name.GetComponent<TMP_Text>() : null,
                Rarity = rarity != null ? rarity.GetComponent<TMP_Text>() : null,
                CostVal = costVal != null ? costVal.GetComponent<TMP_Text>() : null,
                LockBanner = lockBanner != null ? lockBanner.gameObject : null,
                Input = input
            };
        }

        /// <summary>
        /// The single place a slot's stateful colors are written. Locked outranks hover:
        /// a locked slot always reads as locked, so hovering it can't disguise the state
        /// you are about to toggle off.
        /// </summary>
        private static void ApplyVisualState(Slot slot)
        {
            if (slot.Background != null)
                slot.Background.color = slot.Locked ? LockedGoldTint : SlotBase;

            if (slot.Border != null)
                slot.Border.color = ResolveBorderTint(slot);

            if (slot.Name != null)
                slot.Name.color = slot.Locked ? NameLocked : NameIdle;

            if (slot.LockBanner != null)
                slot.LockBanner.SetActive(slot.Locked);
        }

        /// <summary>Flat palette for plain borders; white-based tints when the Border image carries
        /// a metal-kit sprite (name prefixed "mg_") so the tint doesn't mud the art.</summary>
        private static Color ResolveBorderTint(Slot slot)
        {
            bool kitArt = slot.Border.sprite != null
                && slot.Border.sprite.name.StartsWith("mg_", StringComparison.Ordinal);

            if (kitArt)
                return slot.Locked ? KitBorderLocked : (slot.Hovered ? KitBorderHover : KitBorderIdle);

            return slot.Locked ? BorderLocked : (slot.Hovered ? BorderHover : BorderIdle);
        }

        protected override void Refresh(RunState state)
        {
            if (state == null)
                return;

            if (_rerollCostVal != null)
                _rerollCostVal.text = (RunOrchestrator.BaseRerollCost + state.RerollCountThisRound).ToString();

            // Key by SlotIndex, NOT list position. An offer's SlotIndex is its identity — it is
            // what the lock is stored against and what the shop generator rolls per slot. Binding
            // offers[i] to slot i meant that the moment ONE offer was consumed the list shortened
            // and every remaining offer SHIFTED LEFT into the wrong slot (which is how a 6th
            // offer could appear in a 5-slot band, and how a lock could appear to jump slots).
            var offersBySlot = new Dictionary<int, ShopOffer>();
            if (state.Shop?.Offers != null)
            {
                foreach (var offer in state.Shop.Offers)
                {
                    if (offer != null)
                        offersBySlot[offer.SlotIndex] = offer;
                }
            }

            var registry = ContentRegistryProvider.Build(_database ?? ContentDatabase.Load());

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                ClearAuthoredMockCells(slot);

                if (!offersBySlot.TryGetValue(i, out var slotOffer))
                {
                    slot.Input.Bind(null, false);
                    slot.Root.SetActive(false);
                    continue;
                }

                BindSlot(slot, slotOffer, state, registry);
            }

            // Cartel mercenary offer (SlotIndex 9, IsMercenary) binds to the hand-authored
            // OfferSlot_5 through the exact same routine as slots 0-4; buy/lock already route by
            // the offer's own SlotIndex, so 9 flows through untouched. No merc offer → slot hidden.
            if (_mercSlot != null)
            {
                ClearAuthoredMockCells(_mercSlot);

                var mercOffer = state.Shop?.Offers?.FirstOrDefault(o => o != null && o.IsMercenary);
                if (mercOffer == null)
                {
                    _mercSlot.Input.Bind(null, false);
                    _mercSlot.Root.SetActive(false);
                }
                else
                {
                    BindSlot(_mercSlot, mercOffer, state, registry);
                }
            }
        }

        private void BindSlot(Slot slot, ShopOffer offer, RunState state, ContentRegistry registry)
        {
            slot.Root.SetActive(true);

            PieceDefinition definition = null;
            registry?.TryGetById(offer.PieceId, out definition);

            if (slot.Name != null)
                slot.Name.text = (definition?.DisplayName ?? offer.PieceId ?? string.Empty).ToUpperInvariant();

            if (slot.Rarity != null)
                slot.Rarity.text = offer.IsMercenary
                    ? "MERCENARY"
                    : (definition != null ? definition.Rarity.ToString().ToUpperInvariant() : string.Empty);

            if (slot.CostVal != null)
                slot.CostVal.text = offer.GoldPrice.ToString();

            if (slot.Role != null)
            {
                var library = ShopV2IconLibrary.Instance;
                var sprite = library != null && definition != null ? library.Get(definition.CombatRole) : null;
                slot.Role.sprite = sprite;
                slot.Role.enabled = sprite != null;
            }

            RegenerateShapeCells(slot, definition);

            slot.Locked = state.LockedOffers != null
                && state.LockedOffers.Any(r => r != null && r.SlotIndex == offer.SlotIndex);

            ApplyVisualState(slot);
            slot.Input.Bind(offer, slot.Locked, definition);
        }

        private static void ClearAuthoredMockCells(Slot slot)
        {
            if (slot.MocksCleared)
                return;

            slot.MocksCleared = true;
            for (int i = slot.Root.transform.childCount - 1; i >= 0; i--)
            {
                var child = slot.Root.transform.GetChild(i);
                if (child.name.StartsWith("Shape_", StringComparison.Ordinal))
                    Destroy(child.gameObject);
            }
        }

        private static void RegenerateShapeCells(Slot slot, PieceDefinition definition)
        {
            for (int i = slot.Root.transform.childCount - 1; i >= 0; i--)
            {
                var child = slot.Root.transform.GetChild(i);
                if (child.name.StartsWith("GenShape_", StringComparison.Ordinal))
                    Destroy(child.gameObject);
            }

            if (definition?.Shape == null)
                return;

            var cells = definition.Shape.GetCells(new GridCoord(0, 0)).ToList();
            if (cells.Count == 0)
                return;

            int minY = cells.Min(c => c.Y);
            int maxX = cells.Max(c => c.X);

            foreach (var cell in cells)
            {
                var go = new GameObject($"GenShape_{cell.X}_{cell.Y}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(slot.Root.transform, false);

                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.one;
                rect.anchorMax = Vector2.one;
                rect.pivot = Vector2.one;
                rect.sizeDelta = new Vector2(ShapeCellSize, ShapeCellSize);
                rect.anchoredPosition = new Vector2(
                    -ShapePadding - (maxX - cell.X) * ShapeCellStep,
                    -ShapePadding - (cell.Y - minY) * ShapeCellStep);

                var image = go.GetComponent<Image>();
                image.color = ShapeCellColor;
                image.raycastTarget = false;
            }
        }

        private static void OnRerollClicked()
        {
            RunManager.Instance?.TryRerollShop();
        }
    }

    /// <summary>Per-slot input: left-click acquires the offer to reserves, right-click toggles its lock,
    /// DRAG buys it straight onto a board (or reserves) via the shared DragDropController, and hover
    /// shows the piece hovercard — all routed straight to RunManager / the hovercard presenter.
    ///
    /// Click and drag coexist: Unity only fires OnPointerClick when the pointer did not move past the
    /// drag threshold, so a click still means "buy to reserves" and a drag still means "place it here".</summary>
    public sealed class ShopV2OfferSlotInput : MonoBehaviour,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private ShopOffer _offer;
        private bool _locked;
        private PieceDefinition _definition;

        /// <summary>Raised on enter (true) / exit (false) so the presenter can light the border.
        /// The slot's highlight used to be baked into the mock art; it is state now.</summary>
        public event Action<bool> HoverChanged;

        public void Bind(ShopOffer offer, bool locked, PieceDefinition definition = null)
        {
            _offer = offer;
            _locked = locked;
            _definition = definition;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            HoverChanged?.Invoke(true);

            if (_definition != null)
                ShopV2HovercardPresenter.Instance?.Show(_definition, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HoverChanged?.Invoke(false);
            ShopV2HovercardPresenter.Instance?.Hide();
        }

        private void OnDisable()
        {
            // A slot hidden mid-hover never gets its exit event; drop the highlight so it
            // does not come back lit on the next reroll.
            HoverChanged?.Invoke(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_offer == null || RunManager.Instance == null)
                return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                RunManager.Instance.SetLockedOffer(_offer, !_locked);
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            // Scan reserves anchors (0,0)..(5,1); Game decides whether each placement is legal.
            for (int y = 0; y <= 1; y++)
            {
                for (int x = 0; x <= 5; x++)
                {
                    if (RunManager.Instance.TryAcquireOfferToReserves(_offer.OfferId, new GridCoord(x, y)))
                        return;
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_offer == null || DragDropController.Instance == null)
                return;

            // The hovercard would otherwise follow the ghost around the screen.
            ShopV2HovercardPresenter.Instance?.Hide();

            var payload = new DragPayload
            {
                SourceKind = DragSourceKind.ShopOffer,
                OfferId = _offer.OfferId,
                PieceId = _offer.PieceId,
                Offer = _offer,
                Definition = _definition,
                Rotation = PieceRotation.R0
            };

            // pieceOnlyGhost: drag the PIECE, not a copy of the shop card — the ghost has to
            // read as the footprint you are about to drop onto the grid.
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

        public void OnEndDrag(PointerEventData eventData) =>
            DragDropController.Instance?.EndDrag(eventData);
    }
}
