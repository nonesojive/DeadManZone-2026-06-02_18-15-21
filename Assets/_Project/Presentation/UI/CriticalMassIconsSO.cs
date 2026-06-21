using System.Collections.Generic;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using UnityEngine;

namespace DeadManZone.Presentation.UI
{
    [CreateAssetMenu(menuName = "DeadManZone/UI/Critical Mass Icons")]
    public sealed class CriticalMassIconsSO : ScriptableObject
    {
        [SerializeField] private Sprite[] icons = System.Array.Empty<Sprite>();

        private Dictionary<string, Sprite> _byRuleId;

        public Sprite GetIcon(string ruleId)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
                return null;

            EnsureLookup();
            return _byRuleId.TryGetValue(Normalize(ruleId), out var sprite) ? sprite : null;
        }

        public Sprite GetIconForEntry(BuffStripEntry entry)
        {
            if (entry == null)
                return null;

            if (!string.IsNullOrWhiteSpace(entry.RuleId))
            {
                var byRule = GetIcon(entry.RuleId);
                if (byRule != null)
                    return byRule;
            }

            return GetIcon(entry.TagId);
        }

        private void EnsureLookup()
        {
            if (_byRuleId != null)
                return;

            _byRuleId = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);
            if (icons == null)
                return;

            for (int i = 0; i < icons.Length; i++)
            {
                var sprite = icons[i];
                if (sprite == null)
                    continue;

                _byRuleId[Normalize(sprite.name)] = sprite;
            }
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string trimmed = value.Trim();
            if (trimmed.EndsWith("_tempicon", System.StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring(0, trimmed.Length - "_tempicon".Length);

            return trimmed.ToLowerInvariant();
        }
    }
}
