using System;

namespace DeadManZone.Core.Tags
{
    [Serializable]
    public sealed class CustomTagRecord
    {
        public string Id;
        public string DisplayName;
        public TagCategory Category;
        public string Tooltip;
        public int DisplayPriority = 20;
        public bool PlayerVisible = true;

        public KeywordTagEntry ToKeywordEntry() => new()
        {
            Id = Id?.Trim(),
            DisplayName = DisplayName?.Trim(),
            Category = Category,
            Tooltip = Tooltip?.Trim() ?? string.Empty,
            DisplayPriority = DisplayPriority
        };
    }
}
