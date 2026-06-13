#if UNITY_EDITOR
using DeadManZone.Data;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class CombatArenaPrefabAssigner
    {
        private const string WehrmachtPrefabPath =
            "Assets/WW2_German_soilders/Soilders/FBX_soilders/Wehrmacht_A/Wehrmacht_A_prefab.prefab";

        private const string ConscriptRiflemanPath =
            "Assets/_Project/Data/Resources/DeadManZone/Pieces/conscript_rifleman.asset";

        [MenuItem("DeadManZone/Combat Arena/Assign Wehrmacht Prefab To Conscript Rifleman")]
        public static void AssignConscriptRiflemanArenaPrefab()
        {
            var piece = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>(ConscriptRiflemanPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WehrmachtPrefabPath);

            if (piece == null)
            {
                Debug.LogError($"Piece not found at {ConscriptRiflemanPath}");
                return;
            }

            if (prefab == null)
            {
                Debug.LogError($"Wehrmacht prefab not found at {WehrmachtPrefabPath}");
                return;
            }

            piece.combatArenaPrefab = prefab;
            piece.combatArenaModelScale = 1f;
            piece.combatArenaModelHeight = 1.6f;
            EditorUtility.SetDirty(piece);
            AssetDatabase.SaveAssets();
            Debug.Log("Assigned Wehrmacht_A_prefab to conscript_rifleman combat arena visual.");
        }
    }
}
#endif
