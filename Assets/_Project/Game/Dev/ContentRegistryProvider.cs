using DeadManZone.Core.Content;
using DeadManZone.Data;

namespace DeadManZone.Game.Dev
{
    public static class ContentRegistryProvider
    {
        public static ContentRegistry Build(ContentDatabase database)
        {
            if (database == null)
                return null;

            var registry = database.BuildRegistry();
            SessionContentOverlay.Instance?.ApplyTo(registry);
            return registry;
        }
    }
}
