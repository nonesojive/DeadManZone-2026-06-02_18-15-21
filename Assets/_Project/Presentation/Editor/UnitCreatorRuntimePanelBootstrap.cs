using DeadManZone.Data.Editor;
using DeadManZone.Data.UnitCreation;
using DeadManZone.Presentation.Dev;
using UnityEditor;

namespace DeadManZone.Presentation.Editor
{
    [InitializeOnLoad]
    internal static class UnitCreatorRuntimePanelBootstrap
    {
        static UnitCreatorRuntimePanelBootstrap()
        {
            UnitCreatorRuntimePanel.SaveToProjectHandler = TrySave;
        }

        private static (bool ok, string error) TrySave(UnitCreationDraft draft)
        {
            var success = UnitPersistenceService.TrySave(draft, out var error);
            return (success, error);
        }
    }
}
