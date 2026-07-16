using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Shop;
using DeadManZone.Core.Tags;
using DeadManZone.Presentation.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.ShopV2
{
    /// <summary>Populates one hovercard prefab instance (UnitHovercardV2 / BuildingHovercardV2) from a PieceDefinition. Pure display.</summary>
    public sealed class ShopV2HovercardView : MonoBehaviour
    {
        private static readonly Color Dim = new(0.56f, 0.525f, 0.463f, 1f);
        private static readonly Color Brass = new(0.725f, 0.573f, 0.31f, 1f);
        private static readonly Color ChipStrong = new(0.14f, 0.11f, 0.07f, 1f);
        private static readonly Color ChipWeak = new(0.09f, 0.075f, 0.06f, 1f);

        private TMP_Text _name, _subtitle, _cadence, _faction, _flavor, _supVal, _manVal, _rarityTag;
        private TMP_Text _abilities;
        private GameObject _abilitiesLabel;
        private TMP_Text _valHp, _valMorale, _valDamage, _lblDamage, _valTerror;
        private GameObject _terrorIcon, _terrorNote;
        private readonly TMP_Text[] _minorVals = new TMP_Text[3];
        private Image _roleIcon, _ghost, _damageIcon;
        private Sprite _plateSprite;
        private bool _bound;

        private void Awake() => CacheChildren();

        private void CacheChildren()
        {
            if (_bound)
                return;
            _bound = true;

            _name = Text("Name");
            _subtitle = Text("Subtitle");
            _cadence = Text("Cadence");
            _faction = Text("Faction");
            _flavor = Text("Flavor");
            _abilities = Text("Abilities");
            _abilitiesLabel = Find("AbilitiesLabel")?.gameObject;
            _supVal = Text("SupVal");
            _manVal = Text("ManVal");
            _rarityTag = Text("RarityTag");
            _valHp = Text("Val_HP");
            _valMorale = Text("Val_MORALE");
            _valTerror = Text("Val_TERROR");
            _terrorNote = Find("TerrorNote")?.gameObject;
            for (int i = 0; i < 3; i++)
                _minorVals[i] = Text($"MinorVal_{i}");

            // The damage row's authored names embed the mock's attack type — match by prefix.
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Val_DAMAGE", StringComparison.Ordinal))
                    _valDamage = child.GetComponent<TMP_Text>();
                else if (child.name.StartsWith("Lbl_DAMAGE", StringComparison.Ordinal))
                    _lblDamage = child.GetComponent<TMP_Text>();
                else if (child.name.StartsWith("Icon_DAMAGE", StringComparison.Ordinal))
                    _damageIcon = child.GetComponent<Image>();
                else if (child.name.StartsWith("Icon_TERROR", StringComparison.Ordinal))
                    _terrorIcon = child.gameObject;
            }

            var rolePlate = Find("RolePlate");
            _roleIcon = rolePlate != null ? rolePlate.Find("RoleIcon")?.GetComponent<Image>() : null;
            _ghost = Find("Ghost")?.GetComponent<Image>();

            var headerBand = Find("HeaderBand")?.GetComponent<Image>();
            _plateSprite = headerBand != null ? headerBand.sprite : null;
        }

        public void Bind(PieceDefinition def) => Bind(def, null);

        /// <summary>
        /// Binds the card. <paramref name="context"/> is null for a SHOP OFFER (the piece is not
        /// on a board yet, so base stats are the truth) and non-null for a PLACED piece, where
        /// adjacency is already buffing it — the numbers on the card must be what the piece is
        /// actually doing right now, or hover is useless for reading your own board.
        ///
        /// Deltas come from PieceCardViewModelBuilder, the same builder the legacy card uses, so
        /// the two cards can never disagree about a piece.
        /// </summary>
        public void Bind(PieceDefinition def, PieceCardBuildContext context)
        {
            if (def == null)
                return;
            CacheChildren();

            var vm = PieceCardViewModelBuilder.Build(def, context);

            bool rare = def.Rarity.ToString().Equals("Rare", StringComparison.OrdinalIgnoreCase);
            var nameColor = rare ? CombatGrimdarkSkin.VictoryGold : CombatGrimdarkSkin.Bone;

            Set(_name, def.DisplayName?.ToUpperInvariant(), nameColor);
            Set(_subtitle, $"{def.Rarity} · {def.Primary} · {def.CombatRole}".ToUpperInvariant(), Dim);
            Set(_rarityTag, def.Rarity.ToString().ToUpperInvariant(), nameColor);

            var library = ShopV2IconLibrary.Instance;
            var roleSprite = library != null ? library.Get(def.CombatRole) : null;
            if (_roleIcon != null) { _roleIcon.sprite = roleSprite; _roleIcon.enabled = roleSprite != null; }
            if (_ghost != null)
            {
                var ghostSprite = library != null ? library.Get(def.AttackType.ToString()) ?? roleSprite : roleSprite;
                _ghost.sprite = ghostSprite;
                _ghost.enabled = ghostSprite != null;
            }

            Set(_valHp, def.MaxHp.ToString());
            Set(_valMorale, def.MaxMorale.ToString());
            Set(_valDamage, WithBonus(def.BaseDamage, vm.SynergyDamageBonus));
            Set(_lblDamage, $"DAMAGE — {def.AttackType.ToString().ToUpperInvariant()}", Dim);
            if (_damageIcon != null)
            {
                var atkSprite = library != null ? library.Get(def.AttackType.ToString()) : null;
                _damageIcon.sprite = atkSprite;
                _damageIcon.enabled = atkSprite != null;
            }
            Set(_cadence, $"every {def.CooldownTicks} ticks · {def.AttackSpeed.ToString().ToUpperInvariant()}");

            bool hasTerror = def.TerrorDamage > 0 && _valTerror != null;
            if (_valTerror != null) _valTerror.gameObject.SetActive(hasTerror);
            if (_terrorIcon != null) _terrorIcon.SetActive(hasTerror);
            if (_terrorNote != null) _terrorNote.SetActive(hasTerror);
            var lblTerror = Text("Lbl_TERROR");
            if (lblTerror != null) lblTerror.gameObject.SetActive(hasTerror);
            if (hasTerror) Set(_valTerror, def.TerrorDamage.ToString());

            Set(_minorVals[0], def.AttackRange.ToString());
            Set(_minorVals[1], AppendBonus(MoveLabel(def.MovementSpeed), vm.SynergyMoveChargeBonus));
            Set(_minorVals[2], AppendBonus(def.ArmorType.ToString(), vm.SynergyArmorBuffSteps));

            bool ironmarch = def.FactionId != null && def.FactionId.Contains("ironmarch");
            Set(_faction, FactionDisplay(def.FactionId), ironmarch ? Brass : Dim);

            // ABILITIES — what this piece actually DOES. Previously shown nowhere at all, which
            // made two pieces with identical stat lines look identical. Ability lines come from
            // PieceCardViewModelBuilder (active abilities, passive income, granted combat ability),
            // then the board-context lines (synergy firing / critical-mass / salvage) in dim,
            // because "what it does" outranks "what the board is doing to it".
            BindAbilities(vm);
            Set(_supVal, RarityPricing.BaseCost(def.Rarity).ToString());
            Set(_manVal, def.ManpowerCost.ToString());

            RegenerateShape(def);
            RegenerateChips(def);
            ToggleRarityChrome(def, rare);
        }

        /// <summary>
        /// The ABILITIES block: what the piece does, then what the board is doing to it.
        ///
        /// The section header hides when there is nothing to say, so a plain rifleman does not
        /// show an empty "ABILITIES" heading — an empty label reads as a bug, not as "none".
        /// </summary>
        private void BindAbilities(PieceCardViewModel vm)
        {
            var lines = new List<string>();

            if (vm.AbilityLines != null)
                lines.AddRange(vm.AbilityLines.Where(l => !string.IsNullOrWhiteSpace(l)));

            // Board context is dimmed: it is conditional on where the piece is standing, not an
            // intrinsic property of the piece.
            string context = BuildContextLines(vm);
            if (!string.IsNullOrWhiteSpace(context))
            {
                string hex = ColorUtility.ToHtmlStringRGB(Dim);
                foreach (var line in context.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
                    lines.Add($"<color=#{hex}>{line}</color>");
            }

            bool any = lines.Count > 0;

            if (_abilitiesLabel != null)
                _abilitiesLabel.SetActive(any);

            if (_abilities != null)
            {
                _abilities.gameObject.SetActive(any);
                _abilities.text = string.Join("\n", lines);
            }
        }

        /// <summary>"12" when unbuffed, "12 +3" (gold) when adjacency is boosting it.</summary>
        private static string WithBonus(int baseValue, int bonus) =>
            bonus == 0
                ? baseValue.ToString()
                : $"{baseValue} <color=#{BuffHex}>+{bonus}</color>";

        /// <summary>Same, for the label-style minor stats ("Medium +1").</summary>
        private static string AppendBonus(string label, int bonus) =>
            bonus == 0
                ? label
                : $"{label} <color=#{BuffHex}>+{bonus}</color>";

        private static string BuffHex =>
            ColorUtility.ToHtmlStringRGB(CombatGrimdarkSkin.VictoryGold);

        /// <summary>
        /// The board-context block: which synergies are firing, critical-mass progress, and the
        /// salvage note. All empty for a shop offer, so the slot simply collapses to nothing.
        /// </summary>
        private static string BuildContextLines(PieceCardViewModel vm)
        {
            var lines = new List<string>();

            if (vm.SynergyLines != null)
                lines.AddRange(vm.SynergyLines.Where(l => !string.IsNullOrWhiteSpace(l)));

            if (!string.IsNullOrWhiteSpace(vm.CriticalMassHint))
                lines.Add(vm.CriticalMassHint);

            if (!string.IsNullOrWhiteSpace(vm.SalvageContext))
                lines.Add(vm.SalvageContext);

            return string.Join("\n", lines);
        }

        private static string MoveLabel(int movementSpeed) => movementSpeed switch
        {
            <= 0 => "Static",
            1 => "Low",
            2 => "Medium",
            _ => "High"
        };

        private static string FactionDisplay(string factionId) =>
            string.IsNullOrEmpty(factionId) ? "NEUTRAL" : factionId.Replace('_', ' ').ToUpperInvariant();

        private void RegenerateShape(PieceDefinition def)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("Shape_", StringComparison.Ordinal) ||
                    child.name.StartsWith("GenShape_", StringComparison.Ordinal))
                    Destroy(child.gameObject);
            }

            if (def.Shape == null)
                return;

            var cells = def.Shape.GetCells(new GridCoord(0, 0)).ToList();
            if (cells.Count == 0)
                return;
            int minX = cells.Min(c => c.X), minY = cells.Min(c => c.Y);

            foreach (var cell in cells)
            {
                var go = new GameObject($"GenShape_{cell.X}_{cell.Y}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(16f, 16f);
                rect.anchoredPosition = new Vector2(28f + (cell.X - minX) * 19f, -(122f + (cell.Y - minY) * 19f));
                var image = go.GetComponent<Image>();
                image.color = new Color(CombatGrimdarkSkin.Bone.r, CombatGrimdarkSkin.Bone.g, CombatGrimdarkSkin.Bone.b, 0.45f);
                image.raycastTarget = false;
            }
        }

        private void RegenerateChips(PieceDefinition def)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("Chip_", StringComparison.Ordinal) ||
                    child.name.StartsWith("GenChip_", StringComparison.Ordinal))
                    Destroy(child.gameObject);
            }

            var entries = new List<(string label, bool strong)>();
            if (def.SynergyTags != null)
                entries.AddRange(def.SynergyTags.Select(t => (t.ToUpperInvariant(), true)));
            if (def.FlavorTags != null)
                entries.AddRange(def.FlavorTags.Select(t => (t.ToUpperInvariant(), false)));

            var rt = (RectTransform)transform;
            float chipY = rt.sizeDelta.y - 146f;   // authored chips row height from bottom
            float x = 26f;
            foreach (var (label, strong) in entries.Take(4))
            {
                float w = 28f + label.Length * 8.6f;
                if (x + w > rt.sizeDelta.x - 20f)
                    break;

                var chip = new GameObject($"GenChip_{label}", typeof(RectTransform), typeof(Image));
                chip.transform.SetParent(transform, false);
                var chipRt = (RectTransform)chip.transform;
                chipRt.anchorMin = chipRt.anchorMax = new Vector2(0f, 1f);
                chipRt.pivot = new Vector2(0f, 1f);
                chipRt.sizeDelta = new Vector2(w, 26f);
                chipRt.anchoredPosition = new Vector2(x, -chipY);
                var bg = chip.GetComponent<Image>();
                bg.color = strong ? ChipStrong : ChipWeak;
                if (_plateSprite != null) { bg.sprite = _plateSprite; bg.type = Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 4f; }
                bg.raycastTarget = false;

                var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGo.transform.SetParent(chip.transform, false);
                var textRt = (RectTransform)textGo.transform;
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = textRt.offsetMax = Vector2.zero;
                var text = textGo.GetComponent<TextMeshProUGUI>();
                text.text = label;
                text.fontSize = 10f;
                text.color = strong ? CombatGrimdarkSkin.Bone : Dim;
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;

                x += w + 10f;
            }
        }

        /// <summary>Chrome degrades gracefully: each prefab only carries its authored rarity trim;
        /// mismatched rarities still read via name/tag color.</summary>
        private void ToggleRarityChrome(PieceDefinition def, bool rare)
        {
            bool uncommon = def.Rarity.ToString().Equals("Uncommon", StringComparison.OrdinalIgnoreCase);
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("RareTrim", StringComparison.Ordinal) ||
                    child.name.StartsWith("BracketH", StringComparison.Ordinal) ||
                    child.name.StartsWith("BracketV", StringComparison.Ordinal))
                    child.gameObject.SetActive(rare);
                else if (child.name.StartsWith("UncommonTrim", StringComparison.Ordinal) ||
                         child.name.StartsWith("Stud", StringComparison.Ordinal))
                    child.gameObject.SetActive(uncommon);
            }
        }

        private Transform Find(string childName) => transform.Find(childName);

        private TMP_Text Text(string childName)
        {
            var child = transform.Find(childName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private static void Set(TMP_Text text, string value, Color? color = null)
        {
            if (text == null)
                return;
            text.text = value ?? string.Empty;
            if (color.HasValue)
                text.color = color.Value;
        }
    }
}
