using UnityEngine;

namespace DeadManZone.Presentation.Run
{
    /// <summary>Scene/prefab stamp so RunHudPanelBuilder can detect stale HUD layouts.</summary>
    public sealed class RunHudPanelVersion : MonoBehaviour
    {
        [SerializeField] private int version = RunHudPanelBuilder.PanelVersion;

        public int Version => version;

        public void SetVersion(int value) => version = value;
    }
}
