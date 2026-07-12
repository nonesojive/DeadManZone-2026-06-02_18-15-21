using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Run;
using DeadManZone.Core.Tags;
using DeadManZone.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.ShopV2
{
    /// <summary>Binds the BuffsRail's four authored slots to active critical-mass entries. Attach to `BuffsRail`.</summary>
    public sealed class ShopV2BuffsRailPresenter : ShopV2PresenterBase
    {
        private const int SlotCount = 4;

        private sealed class Slot
        {
            public GameObject Root;
            public Image Icon;
            public TMP_Text Count;
        }

        private readonly List<Slot> _slots = new();

        private void Awake()
        {
            var missing = new List<string>();
            for (int i = 0; i < SlotCount; i++)
            {
                var root = transform.Find($"Buff_{i}");
                if (root == null)
                {
                    missing.Add($"Buff_{i}");
                    continue;
                }

                var icon = root.Find("Icon");
                var count = root.Find("Count");
                _slots.Add(new Slot
                {
                    Root = root.gameObject,
                    Icon = icon != null ? icon.GetComponent<Image>() : null,
                    Count = count != null ? count.GetComponent<TMP_Text>() : null
                });
            }

            if (missing.Count > 0)
                Debug.LogWarning($"ShopV2BuffsRailPresenter: missing children: {string.Join(", ", missing)}", this);
        }

        protected override void Refresh(RunState state)
        {
            var manager = RunManager.Instance;
            if (manager == null || manager.Orchestrator == null || !manager.HasActiveRun)
            {
                DeactivateFrom(0);
                return;
            }

            var active = BuffStripEvaluator.Evaluate(manager.Orchestrator.GetBuildBoards())
                .Where(entry => entry.IsActive)
                .OrderByDescending(entry => entry.CurrentCount)
                .Take(_slots.Count)
                .ToList();

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (i >= active.Count)
                {
                    slot.Root.SetActive(false);
                    continue;
                }

                var entry = active[i];
                slot.Root.SetActive(true);

                if (slot.Icon != null)
                {
                    var library = ShopV2IconLibrary.Instance;
                    var sprite = library != null
                        ? library.Get(entry.TagId) ?? library.Get(entry.RuleId)
                        : null;
                    slot.Icon.sprite = sprite;
                    slot.Icon.enabled = sprite != null;
                }

                if (slot.Count != null)
                    slot.Count.text = entry.CurrentCount.ToString();
            }
        }

        private void DeactivateFrom(int index)
        {
            for (int i = index; i < _slots.Count; i++)
                _slots[i].Root.SetActive(false);
        }
    }
}
