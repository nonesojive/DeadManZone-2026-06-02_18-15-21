using System.Collections.Generic;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Run;
using DeadManZone.Game;
using DeadManZone.Presentation.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Run
{
    /// <summary>
    /// The Front Report (CONTEXT.md): three Fight Option cards shown through the Build
    /// phase — pool, strength band vs. the player's current army, and the tier's stakes;
    /// hard shows its Battle Condition up front (consent, not gotcha). Click chooses;
    /// COMBAT stays gated until a front is chosen. Boss rounds replace the cards with a
    /// single red boss report. Runtime-built on a TOP-LEVEL overlay canvas (nested
    /// canvases inherit the parent rect — the recurring lesson), grimdark kit (M6).
    /// </summary>
    public sealed class FrontReportPanel : MonoBehaviour
    {
        private static readonly Color CardBody = CombatGrimdarkSkin.CardBody;
        private static readonly Color SelectedLeather = new(0.30f, 0.245f, 0.16f, 0.98f);
        private static readonly Color BossRed = new(0.45f, 0.16f, 0.14f, 0.96f);
        private static readonly Color WarningRed = new(1f, 0.42f, 0.38f);
        private static readonly Color Amber = new(0.85f, 0.65f, 0.30f);

        private GameObject _canvasRoot;
        private readonly List<(Button button, Image body, TMP_Text text)> _cards = new();
        private TMP_Text _bossText;
        private Image _bossBody;

        public void Refresh(RunState state)
        {
            if (state == null || state.Phase != RunPhase.Build ||
                RunManager.Instance?.Orchestrator == null)
            {
                Hide();
                return;
            }

            EnsureUi();
            _canvasRoot.SetActive(true);

            var orchestrator = RunManager.Instance.Orchestrator;
            bool bossRound = orchestrator.IsBossFightPending;

            _bossBody.gameObject.SetActive(bossRound);
            foreach (var (button, body, _) in _cards)
                button.gameObject.SetActive(!bossRound);

            if (bossRound)
            {
                var boss = orchestrator.GetPendingBoss();
                _bossText.text =
                    $"BOSS FIGHT — {boss.DisplayName.ToUpperInvariant()}\n" +
                    $"<size=13>{FormatPool(boss.EnemyFactionId)} · THE ENEMY COMES TO YOU\n" +
                    "NO FRONT TO CHOOSE — PREPARE YOUR LINES</size>";
                return;
            }

            int playerStrength = ArmyStrengthCalculator
                .Evaluate(orchestrator.GetCombatBoard()).EffectiveTotal;

            for (int i = 0; i < _cards.Count; i++)
            {
                var (button, body, text) = _cards[i];
                if (i >= state.FightOptions.Count)
                {
                    button.gameObject.SetActive(false);
                    continue;
                }

                var option = state.FightOptions[i];
                bool selected = state.ChosenFightOption == i;
                bool choosable = orchestrator.CanChooseOption(i, out string reason);

                body.color = selected ? SelectedLeather : CardBody;
                button.interactable = choosable || selected;
                text.text = BuildCardText(option, playerStrength, choosable, reason);
                text.color = choosable || selected
                    ? CombatGrimdarkSkin.BodyText
                    : new Color(CombatGrimdarkSkin.BodyText.r, CombatGrimdarkSkin.BodyText.g,
                        CombatGrimdarkSkin.BodyText.b, 0.45f);
            }
        }

        public void Hide()
        {
            if (_canvasRoot != null)
                _canvasRoot.SetActive(false);
        }

        private static string BuildCardText(
            FightOptionRecord option, int playerStrength, bool choosable, string blockedReason)
        {
            string tier = option.Tier switch
            {
                FightOptionTier.Easy => "EASY ASSAULT",
                FightOptionTier.Hard => "HARD ASSAULT",
                _ => "STANDARD ASSAULT"
            };

            string stakes = option.Tier switch
            {
                FightOptionTier.Easy => $"COSTS {DreadRules.EasyAuthorityCost} AUTHORITY · +1 DREAD",
                FightOptionTier.Hard =>
                    $"+3 DREAD · SPOILS +{DreadRules.HardVictorySupplies}S +{DreadRules.HardVictoryManpower}M",
                _ => "+2 DREAD"
            };

            string condition = option.Tier == FightOptionTier.Hard &&
                               !string.IsNullOrEmpty(option.ConditionId)
                ? $"\n<color=#{ColorUtility.ToHtmlStringRGB(Amber)}>{FormatConditionName(option.ConditionId)}</color>"
                : string.Empty;

            string blocked = !choosable && !string.IsNullOrEmpty(blockedReason)
                ? $"\n<size=11>{blockedReason.ToUpperInvariant()}</size>"
                : string.Empty;

            return $"<b>{tier}</b>\n" +
                   $"<size=13>{FormatPool(option.EnemyFactionId)} · {StrengthBand(option.StrengthPreview, playerStrength)}\n" +
                   $"{stakes}</size>{condition}{blocked}";
        }

        /// <summary>Coarse band, never exact numbers — exact composition is recon's job.</summary>
        private static string StrengthBand(int enemyStrength, int playerStrength)
        {
            if (playerStrength <= 0)
                return "STRENGTH UNKNOWN";

            float ratio = enemyStrength / (float)playerStrength;
            if (ratio < 0.8f) return "OUTMATCHED FOE";
            if (ratio <= 1.25f) return "EVEN MATCH";
            return "SUPERIOR FOE";
        }

        private static string FormatPool(string factionId) =>
            string.IsNullOrEmpty(factionId)
                ? "UNKNOWN POOL"
                : factionId.Replace('_', ' ').ToUpperInvariant();

        private static string FormatConditionName(string conditionId) =>
            "CONDITION: " + conditionId.Replace('_', ' ').ToUpperInvariant();

        private void EnsureUi()
        {
            if (_canvasRoot != null)
                return;

            _canvasRoot = new GameObject("FrontReportPanel");
            var canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250; // under the run meta strip (300) and combat layers
            var scaler = _canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            _canvasRoot.AddComponent<GraphicRaycaster>();

            // Three cards across the empty bottom band left of the COMBAT button.
            const float left = 0.508f, right = 0.855f, bottom = 0.028f, top = 0.185f, gap = 0.008f;
            float width = (right - left - 2f * gap) / 3f;
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                float x0 = left + i * (width + gap);
                var card = CreateCard($"OptionCard_{i}", new Vector2(x0, bottom), new Vector2(x0 + width, top),
                    () => RunManager.Instance?.ChooseFightOption(index));
                _cards.Add(card);
            }

            // Boss report card spans the whole band; hidden on normal rounds.
            var bossGo = new GameObject("BossReport", typeof(RectTransform), typeof(Image));
            bossGo.transform.SetParent(_canvasRoot.transform, false);
            var bossRect = bossGo.GetComponent<RectTransform>();
            bossRect.anchorMin = new Vector2(left, bottom);
            bossRect.anchorMax = new Vector2(right, top);
            bossRect.offsetMin = Vector2.zero;
            bossRect.offsetMax = Vector2.zero;
            _bossBody = bossGo.GetComponent<Image>();
            _bossBody.color = BossRed;
            _bossText = CreateCardLabel(bossGo.transform);
            _bossText.color = new Color(0.95f, 0.88f, 0.82f);
            _bossText.fontSize = 17f;
        }

        private (Button, Image, TMP_Text) CreateCard(
            string name, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_canvasRoot.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var body = go.GetComponent<Image>();
            body.color = CardBody;

            var button = go.GetComponent<Button>();
            button.targetGraphic = body;
            button.onClick.AddListener(onClick);

            var label = CreateCardLabel(go.transform);
            return (button, body, label);
        }

        private static TMP_Text CreateCardLabel(Transform parent)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.05f);
            rect.anchorMax = new Vector2(0.96f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var label = go.AddComponent<TextMeshProUGUI>();
            label.fontSize = 15f;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.raycastTarget = false;
            label.color = CombatGrimdarkSkin.BodyText;
            return label;
        }
    }
}
