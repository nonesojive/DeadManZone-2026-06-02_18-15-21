using DeadManZone.Data.Editor;
using DeadManZone.Presentation.Run;
using DeadManZone.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    public static class UnitCardPanelAuthoring
    {
        [MenuItem(DeadManZoneEditorMenus.Ui + "Wire Building Card On Unit Card Panel")]
        public static void WireBuildingCardOnUnitCardPanel()
        {
            var panel = Object.FindFirstObjectByType<UnitCardPanelView>(FindObjectsInactive.Include);
            if (panel == null)
            {
                Debug.LogWarning("No UnitCardPanelView found in the open scene.");
                return;
            }

            var host = panel.transform;
            var existing = host.Find(UnitCardPanelView.BuildingCardName);
            if (existing == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPaths.BuildingPrefab);
                if (prefab == null)
                {
                    Debug.LogError($"Building card prefab not found at '{CardPrefabPaths.BuildingPrefab}'.");
                    return;
                }

                var cardGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, host);
                cardGo.name = UnitCardPanelView.BuildingCardName;
                UnitCardPanelView.CenterCardInPanel(cardGo.GetComponent<RectTransform>());
                cardGo.SetActive(false);
            }

            panel.EnsureCardView();
            UnitCardPanelView.CenterCardInPanel(host.Find(UnitCardPanelView.BuildingCardName)?.GetComponent<RectTransform>());

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("buildingCardView").objectReferenceValue =
                host.Find(UnitCardPanelView.BuildingCardName)?.GetComponent<PieceCardView>();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(panel);
            if (panel.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);

            Debug.Log("DeadManZone: Building card wired on UnitCardPanelView.");
        }
    }
}
