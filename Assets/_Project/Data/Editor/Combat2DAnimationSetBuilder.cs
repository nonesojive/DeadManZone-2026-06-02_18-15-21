#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    /// <summary>Builds CombatUnit2DAnimationSetSO assets from repacked horizontal strips and
    /// assigns them to their piece definitions. Strips are 16-frame, 128px/frame rows named
    /// "{pieceId}_{state}.png" under Animations/{pieceId}/.</summary>
    public static class Combat2DAnimationSetBuilder
    {
        private const int FrameCount = 16;
        private const string AnimRoot = "Assets/_Project/Art/Combat2D/Units/Animations";
        private const string PieceRoot = "Assets/_Project/Data/Resources/DeadManZone/Pieces";

        // Strip-file prefixes can differ from the piece id (e.g. the medic uses "medic_").
        private static readonly (string pieceId, string stripPrefix)[] Pieces =
        {
            ("field_medic", "medic"),
            ("conscript_rifleman", "conscript_rifleman"),
            ("rifle_squad", "rifle_squad"),
            ("grenade_thrower", "grenade_thrower"),
            ("shock_trooper", "shock_trooper"),
            ("marksman_squad", "marksman_squad"),
            ("ironmarch_engineer", "ironmarch_engineer"),
            ("ironmarch_sniper", "ironmarch_sniper"),
            ("ironmarch_breacher", "ironmarch_breacher"),
        };

        [MenuItem("DeadManZone/Combat Arena/Build Field Medic 2D Anim Set")]
        public static void BuildFieldMedic() => BuildOne("field_medic", "medic");

        [MenuItem("DeadManZone/Combat Arena/Build All Unit 2D Anim Sets")]
        public static void BuildAll()
        {
            int built = 0;
            foreach (var (pieceId, prefix) in Pieces)
                built += BuildOne(pieceId, prefix) ? 1 : 0;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Built {built} combat 2D anim sets.");
        }

        private static bool BuildOne(string pieceId, string stripPrefix)
        {
            string dir = $"{AnimRoot}/{pieceId}";
            string setPath = $"{dir}/{pieceId}_anim_set.asset";

            var set = AssetDatabase.LoadAssetAtPath<CombatUnit2DAnimationSetSO>(setPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<CombatUnit2DAnimationSetSO>();
                AssetDatabase.CreateAsset(set, setPath);
            }

            set.idle = Strip(dir, stripPrefix, "idle", 10f, true);
            set.walk = Strip(dir, stripPrefix, "walk", 14f, true);
            set.run = Strip(dir, stripPrefix, "run", 18f, true);
            set.shoot = Strip(dir, stripPrefix, "shoot", 18f, false);
            set.hurt = Strip(dir, stripPrefix, "hurt", 14f, false);
            set.hitReact = Strip(dir, stripPrefix, "hit_react", 14f, false);
            set.die = Strip(dir, stripPrefix, "die", 12f, false);

            if (!set.HasAny)
            {
                Debug.LogWarning($"No strips found for {pieceId} under {dir}; skipped.");
                return false;
            }

            EditorUtility.SetDirty(set);

            var piece = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>($"{PieceRoot}/{pieceId}.asset");
            if (piece != null)
            {
                piece.combatArena2DAnimations = set;
                EditorUtility.SetDirty(piece);
            }
            else
            {
                Debug.LogWarning($"Piece asset not found for {pieceId}");
            }

            return true;
        }

        private static CombatUnit2DStrip Strip(string dir, string prefix, string state, float fps, bool loop)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{dir}/{prefix}_{state}.png");
            return new CombatUnit2DStrip
            {
                sheet = sprite,
                frameCount = FrameCount,
                framesPerSecond = fps,
                loop = loop
            };
        }
    }
}
#endif
