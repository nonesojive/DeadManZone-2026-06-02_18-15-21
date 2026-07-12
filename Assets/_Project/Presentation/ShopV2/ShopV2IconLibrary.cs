using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadManZone.Presentation.ShopV2
{
    /// <summary>Scene-level id → sprite lookup for ShopV2 icons (roles, tags, resources); display wiring only.</summary>
    public sealed class ShopV2IconLibrary : MonoBehaviour
    {
        [Serializable]
        private sealed class Entry
        {
            public string id;
            public Sprite sprite;
        }

        public static ShopV2IconLibrary Instance { get; private set; }

        [SerializeField] private List<Entry> entries = new();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public Sprite Get(string id)
        {
            if (string.IsNullOrEmpty(id) || entries == null)
                return null;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null && string.Equals(entry.id, id, StringComparison.OrdinalIgnoreCase))
                    return entry.sprite;
            }

            return null;
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Populate From Icons Folder")]
        private void AutoPopulateFromIconsFolder()
        {
            const string folder = "Assets/_Project/Art/UI/Icons/64";
            entries.Clear();

            var guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { folder });
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                    continue;

                string id = System.IO.Path.GetFileNameWithoutExtension(path);
                if (id.StartsWith("icon_", StringComparison.OrdinalIgnoreCase))
                    id = id.Substring("icon_".Length);

                entries.Add(new Entry { id = id, sprite = sprite });
            }

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"ShopV2IconLibrary: populated {entries.Count} icons from {folder}.", this);
        }
#endif
    }
}
