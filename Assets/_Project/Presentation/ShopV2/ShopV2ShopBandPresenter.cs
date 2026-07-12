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
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadManZone.Presentation.ShopV2
{
    /// <summary>Binds the ShopBand's five offer slots and the reroll plate to RunState.Shop. Attach to `ShopBand`.</summary>
    public sealed class ShopV2ShopBandPresenter : ShopV2PresenterBase
    {
        private const int OfferSlotCount = 5;
        private const float ShapeCellSize = 12f;
        private const float ShapeCellStep = 13f;
        private const float ShapePadding = 4f;

        // Display-only lock tint: dimmed toward the kit's VictoryGold, never a rule.
        private static readonly Color LockedGoldTint = new(0.42f, 0.35f, 0.21f, 1f);
        private static readonly Color ShapeCellColor = new(
            CombatGrimdarkSkin.Bone.r, CombatGrimdarkSkin.Bone.g, CombatGrimdarkSkin.Bone.b, 0.45f);

        private sealed class Slot
        {
            public GameObject Root;
            public Image Background;
            public Image Role;
            public TMP_Text Name;
            public TMP_Text Rarity;
            public TMP_Text CostVal;
            public GameObject LockBanner;
            public ShopV2OfferSlotInput Input;
            public bool MocksCleared;
            public bool OriginalColorStored;
            public Color OriginalColor;
        }

        private readonly List<Slot> _slots = new();
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

                _slots.Add(BuildSlot(root));
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
            var role = root.Find("Role");
            var name = root.Find("Name");
            var rarity = root.Find("Rarity");
            var costVal = root.Find("CostVal");
            var lockBanner = root.Find("LockBanner");

            var input = root.GetComponent<ShopV2OfferSlotInput>();
            if (input == null)
                input = root.gameObject.AddComponent<ShopV2OfferSlotInput>();

            return new Slot
            {
                Root = root.gameObject,
                Background = root.GetComponent<Image>(),
                Role = role != null ? role.GetComponent<Image>() : null,
                Name = name != null ? name.GetComponent<TMP_Text>() : null,
                Rarity = rarity != null ? rarity.GetComponent<TMP_Text>() : null,
                CostVal = costVal != null ? costVal.GetComponent<TMP_Text>() : null,
                LockBanner = lockBanner != null ? lockBanner.gameObject : null,
                Input = input
            };
        }

        protected override void Refresh(RunState state)
        {
            if (state == null)
                return;

            if (_rerollCostVal != null)
                _rerollCostVal.text = (RunOrchestrator.BaseRerollCost + state.RerollCountThisRound).ToString();

            var offers = state.Shop?.Offers != null
                ? state.Shop.Offers.Where(o => o != null).OrderBy(o => o.SlotIndex).ToList()
                : new List<ShopOffer>();

            var registry = ContentRegistryProvider.Build(_database ?? ContentDatabase.Load());

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                ClearAuthoredMockCells(slot);

                if (i >= offers.Count)
                {
                    slot.Input.Bind(null, false);
                    slot.Root.SetActive(false);
                    continue;
                }

                BindSlot(slot, offers[i], state, registry);
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
                slot.Rarity.text = definition != null ? definition.Rarity.ToString().ToUpperInvariant() : string.Empty;

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

            // Display only: lock semantics live in Game/Core; we just mirror LockedOffers.
            bool locked = state.LockedOffers != null
                && state.LockedOffers.Any(r => r != null && r.SlotIndex == offer.SlotIndex);

            if (slot.LockBanner != null)
                slot.LockBanner.SetActive(locked);

            if (slot.Background != null)
            {
                if (!slot.OriginalColorStored)
                {
                    slot.OriginalColor = slot.Background.color;
                    slot.OriginalColorStored = true;
                }

                slot.Background.color = locked ? LockedGoldTint : slot.OriginalColor;
            }

            slot.Input.Bind(offer, locked, definition);
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
    /// hover shows the piece hovercard — all routed straight to RunManager / the hovercard presenter.</summary>
    public sealed class ShopV2OfferSlotInput : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private ShopOffer _offer;
        private bool _locked;
        private PieceDefinition _definition;

        public void Bind(ShopOffer offer, bool locked, PieceDefinition definition = null)
        {
            _offer = offer;
            _locked = locked;
            _definition = definition;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_definition != null)
                ShopV2HovercardPresenter.Instance?.Show(_definition, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData) =>
            ShopV2HovercardPresenter.Instance?.Hide();

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
    }
}
