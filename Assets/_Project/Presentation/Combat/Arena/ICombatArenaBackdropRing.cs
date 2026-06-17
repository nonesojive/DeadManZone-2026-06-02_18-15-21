using DeadManZone.Data;

namespace DeadManZone.Presentation.Combat.Arena
{
    public interface ICombatArenaBackdropRing
    {
        CombatArenaBackdropRing RingType { get; }
        string ChildRootName { get; }
        bool IsEnabled { get; }
        int PrefabCount { get; }
        string ResolvePrefabPath(int catalogIndex);
    }
}
