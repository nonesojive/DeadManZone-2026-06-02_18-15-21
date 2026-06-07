namespace DeadManZone.Core.Tags
{
    public sealed class TagDefinition
    {
        public string Id { get; init; }
        public string DisplayName { get; init; }
        public TagCategory Category { get; init; }
        public bool PlayerVisible { get; init; } = true;
        public string Tooltip { get; init; }
        public int DisplayPriority { get; init; }
    }
}
