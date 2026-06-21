using System.Collections.Generic;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Presentation.Board;

namespace DeadManZone.Presentation.UI
{
    public static class PieceCardOverflowTooltip
    {
        public static string Build(PieceDefinition definition, PieceCardViewModel model)
        {
            if (definition == null || model == null || model.OverflowCount <= 0)
                return string.Empty;

            PieceTagQueries.PlayerVisibleTagsResult allVisible = PieceTagQueries.GetPlayerVisibleTags(
                definition,
                maxOptionalChips: int.MaxValue);

            int visibleCount = model.ChipTags.Count;
            if (visibleCount >= allVisible.VisibleTags.Count)
                return string.Empty;

            var hiddenTagNames = new List<string>();
            for (int i = visibleCount; i < allVisible.VisibleTags.Count; i++)
            {
                var hiddenTag = allVisible.VisibleTags[i];
                if (hiddenTag != null && !string.IsNullOrWhiteSpace(hiddenTag.DisplayName))
                    hiddenTagNames.Add(hiddenTag.DisplayName);
            }

            return hiddenTagNames.Count == 0 ? string.Empty : string.Join(", ", hiddenTagNames);
        }
    }
}
