using System.Text;
using DeadManZone.Core.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat
{
    /// <summary>Scrollable combat event log for replay and tuning.</summary>
    public sealed class CombatLogPresenter : MonoBehaviour
    {
        private const int MaxLines = 250;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text logText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private CombatDirector combatDirector;

        private readonly StringBuilder _buffer = new();
        private int _lineCount;

        private void Awake()
        {
            if (combatDirector == null)
                combatDirector = GetComponentInParent<CombatDirector>();
        }

        private void OnEnable()
        {
            if (combatDirector != null)
                combatDirector.EventReplayed += OnEventReplayed;
            Hide();
        }

        private void OnDisable()
        {
            if (combatDirector != null)
                combatDirector.EventReplayed -= OnEventReplayed;
        }

        public void Show()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        public void Clear()
        {
            _buffer.Clear();
            _lineCount = 0;
            if (logText != null)
                logText.text = "Combat log\n—";
        }

        private void OnEventReplayed(DeadManZone.Core.Combat.CombatEvent combatEvent)
        {
            string line = CombatLogFormatter.Format(combatEvent);
            if (string.IsNullOrEmpty(line))
                return;

            if (_lineCount == 0)
                _buffer.Clear();
            else
                _buffer.AppendLine();

            _buffer.Append(line);
            _lineCount++;

            while (_lineCount > MaxLines)
                TrimOldestLine();

            if (logText != null)
                logText.text = _buffer.ToString();

            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void TrimOldestLine()
        {
            int newline = _buffer.ToString().IndexOf('\n');
            if (newline < 0)
            {
                _buffer.Clear();
                _lineCount = 0;
                return;
            }

            _buffer.Remove(0, newline + 1);
            _lineCount--;
        }
    }
}
