using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadManZone.Presentation.MainMenu
{
    /// <summary>
    /// Wave 4 faction-select overhaul: 4x2 crest grid (left) + detail pane (right) + MARCH
    /// confirm button, replacing MainMenuController's placeholder 3-button panel. Reads
    /// FactionSO + PieceDefinitionSO straight from ContentDatabase — no game rules live here,
    /// only display/flavor plumbing (identity copy table below is flavor text, not a rule).
    /// Built by Presentation/Editor/FactionSelectPanelBuilder; this class only wires behavior.
    /// </summary>
    public sealed class FactionSelectView : MonoBehaviour
    {
        [Header("Grid (index order == ContentDatabase.PlayableFactionIds)")]
        [SerializeField] private Button[] cardButtons = Array.Empty<Button>();
        [SerializeField] private Image[] cardCrestImages = Array.Empty<Image>();
        [SerializeField] private TMP_Text[] cardNameTexts = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] cardTaglineTexts = Array.Empty<TMP_Text>();
        [SerializeField] private GameObject[] cardLockOverlays = Array.Empty<GameObject>();

        [Header("Detail pane")]
        [SerializeField] private Image detailFrameImage;
        [SerializeField] private TMP_Text detailNameText;
        [SerializeField] private TMP_Text detailTaglineText;
        [SerializeField] private TMP_Text detailCmRuleText;
        [SerializeField] private TMP_Text detailEconomyText;
        [SerializeField] private TMP_Text detailTentpoleText;
        [SerializeField] private TMP_Text detailPlaystyleText;
        [SerializeField] private TMP_Text detailLockText;
        [SerializeField] private Transform rosterStripContainer;

        [Header("Confirm / back")]
        [SerializeField] private Button marchButton;
        [SerializeField] private Button backButton;

        private string _focusedFactionId;
        private Action<string> _onConfirm;
        private ContentDatabase _db;

        // ---- roster icon sizing (small, fits 12-across in the strip; layout group spacing
        // that pitches these apart lives in FactionSelectPanelBuilder, a different assembly) ----
        private const float RosterIconSize = 44f;

        private static readonly Color UncommonRing = new(0.78f, 0.80f, 0.84f);
        private static readonly Color RareRing = new(0.85f, 0.68f, 0.28f);
        private static readonly Color CommonRing = new(0.32f, 0.28f, 0.24f);

        /// <summary>
        /// Unlock seam (Wave 4): all 8 factions are unlocked today per faction-roster-v1 §1.9/§4
        /// (commit 50fe0a6b). MetaProgressionService.IsFactionUnlocked still exists for the old
        /// campaign-win achievement/unlock flow but is deliberately NOT consulted here — that
        /// gating is superseded. Flip this to a real check if per-faction locking returns; the
        /// locked visual state below (greyed card + lock line) already exists and just needs
        /// this to start returning false for something.
        /// </summary>
        private static bool IsFactionUnlocked(string factionId) => true;

        private void Awake()
        {
            if (marchButton != null)
                marchButton.onClick.AddListener(OnMarchClicked);

            for (int i = 0; i < cardButtons.Length; i++)
            {
                if (cardButtons[i] == null)
                    continue;

                int index = i;
                var relay = cardButtons[i].gameObject.AddComponent<CardFocusRelay>();
                relay.Owner = this;
                relay.Index = index;
            }

            ApplyGrimdarkSkin();
        }

        /// <summary>Self-populates whenever this panel is activated, regardless of which code
        /// path flips it on (MainMenuController.OnNewRunClicked today; any future caller, or an
        /// editor/test harness that just SetActive(true)s the panel, tomorrow). Population must
        /// not depend on a caller remembering to invoke Show() after activating the GameObject —
        /// that's exactly the trap that left the grid/detail pane blank in Wave 4's first pass.
        /// Show() is idempotent, so this is safe alongside any explicit caller too.</summary>
        private void OnEnable() => Show();

        /// <summary>Targeted grimdark pass — deliberately NOT CombatGrimdarkSkin.StyleCard/
        /// StylePanelText over the whole panel: those blanket helpers null out every child
        /// Image's sprite, which would wipe the crest and roster-icon sprites this view relies
        /// on. Touch only the elements that are safe (button root graphics, plain text, the one
        /// dedicated frame image).</summary>
        private void ApplyGrimdarkSkin()
        {
            // KeepSprite variants: the faction screen is hand-authored with MuckGrind kit art
            // (card frames, panel, buttons). Falls back to leather when no sprite is assigned.
            foreach (var button in cardButtons)
                CombatGrimdarkSkin.StyleButtonKeepSprite(button);
            CombatGrimdarkSkin.StyleButtonKeepSprite(marchButton);
            CombatGrimdarkSkin.StyleButtonKeepSprite(backButton);
            if (detailFrameImage == null || detailFrameImage.sprite == null)
                CombatGrimdarkSkin.StyleFrame(detailFrameImage);

            foreach (var text in cardNameTexts)
                CombatGrimdarkSkin.StyleTitle(text, characterSpacing: 1f);
            foreach (var text in cardTaglineTexts)
                CombatGrimdarkSkin.StyleBody(text);

            CombatGrimdarkSkin.StyleTitle(detailNameText);
            CombatGrimdarkSkin.StyleBody(detailTaglineText);
            CombatGrimdarkSkin.StyleBody(detailCmRuleText);
            CombatGrimdarkSkin.StyleBody(detailEconomyText);
            CombatGrimdarkSkin.StyleBody(detailTentpoleText);
            CombatGrimdarkSkin.StyleBody(detailPlaystyleText);
            if (detailLockText != null)
                detailLockText.color = CombatGrimdarkSkin.DefeatRed;
        }

        public void SetConfirmHandler(Action<string> onConfirm) => _onConfirm = onConfirm;

        public void SetBackHandler(UnityEngine.Events.UnityAction handler)
        {
            if (backButton == null)
                return;

            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(handler);
        }

        /// <summary>Populates the grid from ContentDatabase and focuses the first faction.
        /// Runs automatically on every OnEnable (see that method's doc comment); public so a
        /// caller can force an explicit refresh too, but nothing needs to call this directly
        /// for the panel to work.</summary>
        public void Show()
        {
            _db = ContentDatabase.Load();
            var ids = ContentDatabase.PlayableFactionIds;

            for (int i = 0; i < cardButtons.Length; i++)
            {
                bool hasFaction = i < ids.Length;
                if (cardButtons[i] != null)
                    cardButtons[i].gameObject.SetActive(hasFaction);
                if (!hasFaction)
                    continue;

                string factionId = ids[i];
                var faction = _db != null ? _db.GetFaction(factionId) : null;
                bool unlocked = IsFactionUnlocked(factionId);

                if (cardNameTexts[i] != null)
                    cardNameTexts[i].text = faction != null ? faction.displayName : factionId;
                if (cardTaglineTexts[i] != null)
                    cardTaglineTexts[i].text = GetIdentity(factionId).Tagline;
                if (cardCrestImages[i] != null)
                    cardCrestImages[i].sprite = ResolveCrest(faction, factionId);
                if (cardLockOverlays[i] != null)
                    cardLockOverlays[i].SetActive(!unlocked);
                if (cardButtons[i] != null)
                    cardButtons[i].interactable = unlocked;
            }

            string defaultId = ids.FirstOrDefault(IsFactionUnlocked) ?? (ids.Length > 0 ? ids[0] : null);
            int defaultIndex = Array.IndexOf(ids, defaultId);
            Focus(defaultId);

            if (defaultIndex >= 0 && defaultIndex < cardButtons.Length && cardButtons[defaultIndex] != null
                && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(cardButtons[defaultIndex].gameObject);
            }
        }

        /// <summary>Called by CardFocusRelay on select/hover — updates the detail pane preview
        /// without committing to a run (MARCH does that).</summary>
        internal void Focus(int cardIndex)
        {
            var ids = ContentDatabase.PlayableFactionIds;
            if (cardIndex < 0 || cardIndex >= ids.Length)
                return;

            Focus(ids[cardIndex]);
        }

        private void Focus(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
                return;

            _focusedFactionId = factionId;
            RefreshDetailPane();
        }

        private void RefreshDetailPane()
        {
            if (string.IsNullOrEmpty(_focusedFactionId))
                return;

            var faction = _db != null ? _db.GetFaction(_focusedFactionId) : null;
            var identity = GetIdentity(_focusedFactionId);
            bool unlocked = IsFactionUnlocked(_focusedFactionId);

            if (detailNameText != null)
                detailNameText.text = faction != null ? faction.displayName : _focusedFactionId;
            if (detailTaglineText != null)
                detailTaglineText.text = identity.Tagline;
            if (detailCmRuleText != null)
                detailCmRuleText.text = identity.CmRule;
            if (detailEconomyText != null)
                detailEconomyText.text = identity.Economy;
            if (detailTentpoleText != null)
                detailTentpoleText.text = identity.Tentpole;
            if (detailPlaystyleText != null)
                detailPlaystyleText.text = identity.Playstyle;
            if (detailLockText != null)
                detailLockText.gameObject.SetActive(!unlocked);
            if (marchButton != null)
                marchButton.interactable = unlocked;

            BuildRosterStrip(_focusedFactionId);
        }

        private void BuildRosterStrip(string factionId)
        {
            if (rosterStripContainer == null)
                return;

            for (int i = rosterStripContainer.childCount - 1; i >= 0; i--)
                Destroy(rosterStripContainer.GetChild(i).gameObject);

            if (_db == null)
                return;

            var pieces = _db.Pieces
                .Where(p => p != null && p.factionId == factionId)
                .OrderBy(p => (int)p.rarity)
                .ThenBy(p => p.displayName)
                .Take(12);

            foreach (var piece in pieces)
                BuildRosterIcon(piece);
        }

        private void BuildRosterIcon(PieceDefinitionSO piece)
        {
            var cellGo = new GameObject($"Roster_{piece.id}", typeof(RectTransform));
            cellGo.transform.SetParent(rosterStripContainer, false);
            var cellRect = (RectTransform)cellGo.transform;
            cellRect.sizeDelta = new Vector2(RosterIconSize, RosterIconSize);

            var ringImage = cellGo.AddComponent<Image>();
            ringImage.color = RarityRingColor(piece.rarity);

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(cellGo.transform, false);
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            float inset = 3f;
            iconRect.offsetMin = new Vector2(inset, inset);
            iconRect.offsetMax = new Vector2(-inset, -inset);

            var iconImage = iconGo.AddComponent<Image>();
            iconImage.sprite = piece.icon;
            iconImage.preserveAspect = true;

            var layout = cellGo.AddComponent<LayoutElement>();
            layout.preferredWidth = RosterIconSize;
            layout.preferredHeight = RosterIconSize;
        }

        private static Color RarityRingColor(Rarity rarity) => rarity switch
        {
            Rarity.Uncommon => UncommonRing,
            Rarity.Rare => RareRing,
            _ => CommonRing
        };

        private Sprite ResolveCrest(FactionSO faction, string factionId)
        {
            if (faction != null && faction.crest != null)
                return faction.crest;

            // Belt-and-suspenders fallback (see class doc): FactionCrestAssigner populates
            // FactionSO.crest from this same path, but the screen must not go sprite-less if
            // that editor pass hasn't run yet (e.g. a fresh checkout before Unity opens once).
            return Resources.Load<Sprite>($"DeadManZone/Icons/Crests/icon_{factionId}");
        }

        private void OnMarchClicked()
        {
            if (string.IsNullOrEmpty(_focusedFactionId) || !IsFactionUnlocked(_focusedFactionId))
                return;

            _onConfirm?.Invoke(_focusedFactionId);
        }

        // ---------------------------------------------------------------- identity copy table

        private readonly struct Identity
        {
            public readonly string Tagline;
            public readonly string CmRule;
            public readonly string Economy;
            public readonly string Tentpole;
            public readonly string Playstyle;

            public Identity(string tagline, string cmRule, string economy, string tentpole, string playstyle)
            {
                Tagline = tagline;
                CmRule = cmRule;
                Economy = economy;
                Tentpole = tentpole;
                Playstyle = playstyle;
            }
        }

        /// <summary>Flavor copy only (no game rules) — accurate to docs/GDD.md §11.10 and the
        /// per-faction sections above it. Smallest thing that works per Wave 4 scope: a hardcoded
        /// table beats a ScriptableObject for 8 rows of static text nobody else consumes.</summary>
        private static readonly Dictionary<string, Identity> IdentityTable = new()
        {
            [FactionIds.IronmarchUnion] = new Identity(
                "The relentless war machine — mass infantry, dumb reliable steel.",
                "Critical Mass: +flat damage to infantry at 5/7/10 fielded.",
                "Economy: none, deliberately — win through numbers, not tricks.",
                "Tentpole: none — the baseline every other faction bends away from.",
                "Playstyle: cheap infantry blob, backed by artillery and snipers."),

            [FactionIds.DustScourge] = new Identity(
                "Scavengers of the wastes — thrive on what everyone else discards.",
                "Critical Mass: counts salvage-tagged pieces (any faction) — buffs the strays.",
                "Economy: x1.25 salvage refund; salvage pity trips at 2 dry batches, not 4.",
                "Tentpole: none yet — the salvage-counting Critical Mass rule is the hook.",
                "Playstyle: gas and shredding raiders riding an opportunistic salvage engine."),

            [FactionIds.CartelOfEchoes] = new Identity(
                "War as profit — the smallest native roster, the deepest pockets.",
                "Critical Mass: +Supplies at 3/5/7 fielded (run-resource scope).",
                "Economy: a 6th mercenary shop slot, at a +25% surcharge.",
                "Tentpole: none — the mercenary slot and Supplies engine are the identity.",
                "Playstyle: 7 native fighters, filled out with rented mercenaries."),

            [FactionIds.OathbornAccord] = new Identity(
                "Peacekeepers turned crusaders — shields, banners, the wounded kept upright.",
                "Critical Mass: +max Morale, army-wide, at 5/7/10 fielded.",
                "Economy: medic and heal-pulse hook (soft-TBD).",
                "Tentpole: transport load/unload — the Armored Ark carries pieces into the fight.",
                "Playstyle: shield-wall infantry backed by morale resistance and healers."),

            [FactionIds.ParadoxEngine] = new Identity(
                "The experiment that won't end — tempo bent back on itself.",
                "Critical Mass: +attack-speed tier steps at 5/7/10 fielded.",
                "Economy: the first shop reroll each Build is free.",
                "Tentpole: repeat activations — Doctor Recursion fires pause-window abilities twice.",
                "Playstyle: self-tempo attack-speed stacking, zero randomness."),

            [FactionIds.BlightbornPact] = new Identity(
                "The rot of old houses — gas, despair, and a court in tatters.",
                "Critical Mass: +% gas damage at 5/7/10 fielded.",
                "Economy: Despair Dividend — +1 Supply per enemy unit routed, any faction.",
                "Tentpole: none yet — an honest weakness instead (gas is weak vs structures).",
                "Playstyle: gas-role attackers pressuring morale, countered by structures."),

            [FactionIds.CrimsonAssembly] = new Identity(
                "Clinical optimization — every debuff in the game answers to this faction.",
                "Critical Mass: +suppression duration at 5/7/10 fielded (potency TBD).",
                "Economy: Ahead of Schedule — shop rarity odds roll as FightEquivalent+1.",
                "Tentpole: suppression — on-hit attack-speed and movement-charge debuffs.",
                "Playstyle: vehicles and suppression fire, sanctioned Uncommon tank included."),

            [FactionIds.AshenCovenant] = new Identity(
                "The revolution of cinders — strength drawn from the edge of death.",
                "Critical Mass: low-state trigger bonuses strengthen at 5/7/10 fielded.",
                "Economy: inverted death-shock — a death grants nearby allies morale, not loss.",
                "Tentpole: low-state triggers — bonuses fire below 50% HP or morale, per-unit.",
                "Playstyle: fanatic melee/fire swarm that gets stronger the more it bleeds."),
        };

        private static Identity GetIdentity(string factionId) =>
            IdentityTable.TryGetValue(factionId, out var identity)
                ? identity
                : new Identity(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        /// <summary>Tiny relay so keyboard nav (EventSystem selection) and mouse hover both
        /// preview a card without committing — Unity's Selectable navigation already handles
        /// arrow-key movement between cards and down to MARCH; this only reacts to the
        /// resulting focus change. Nested here rather than a separate file: single call site,
        /// no reuse elsewhere.</summary>
        private sealed class CardFocusRelay : MonoBehaviour, ISelectHandler, IPointerEnterHandler
        {
            public FactionSelectView Owner;
            public int Index;

            public void OnSelect(BaseEventData eventData) => Owner.Focus(Index);
            public void OnPointerEnter(PointerEventData eventData) => Owner.Focus(Index);
        }
    }
}
