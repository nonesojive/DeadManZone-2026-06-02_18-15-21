using System.Collections.Generic;
using DeadManZone.Core.Tags;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public sealed class TagCreatorWindow : EditorWindow
    {
        private readonly List<CustomTagRecord> _tags = new();
        private CustomTagRecord _draft = new();
        private int _selectedIndex = -1;
        private string _statusMessage;
        private MessageType _statusType = MessageType.Info;
        private Vector2 _scroll;

        [MenuItem(DeadManZoneEditorMenus.Content + "Tag Creator")]
        public static void Open()
        {
            var window = GetWindow<TagCreatorWindow>("Tag Creator");
            window.minSize = new Vector2(420f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            Reload();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Tag Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Custom tags are saved to custom_tags.json and codegen into CustomTagCatalog.Generated.cs. "
                + "Unit Creator pickers read from TagRegistry automatically after save.",
                MessageType.Info);

            DrawTagList();
            EditorGUILayout.Space(8f);
            DrawDraftForm();
            EditorGUILayout.Space(8f);
            DrawActions();

            if (!string.IsNullOrEmpty(_statusMessage))
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
        }

        private void DrawTagList()
        {
            EditorGUILayout.LabelField("Custom Tags", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(140f));
            for (int i = 0; i < _tags.Count; i++)
            {
                var tag = _tags[i];
                bool selected = i == _selectedIndex;
                bool next = EditorGUILayout.ToggleLeft($"{tag.DisplayName} ({tag.Id}) — {tag.Category}", selected);
                if (next && !selected)
                {
                    _selectedIndex = i;
                    _draft = Clone(tag);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawDraftForm()
        {
            EditorGUILayout.LabelField(_selectedIndex >= 0 ? "Edit Tag" : "New Tag", EditorStyles.boldLabel);
            _draft.Id = EditorGUILayout.TextField("Id", _draft.Id);
            _draft.DisplayName = EditorGUILayout.TextField("Display Name", _draft.DisplayName);
            _draft.Category = (TagCategory)EditorGUILayout.EnumPopup("Category", _draft.Category);
            _draft.Tooltip = EditorGUILayout.TextField("Tooltip", _draft.Tooltip);
            _draft.DisplayPriority = EditorGUILayout.IntField("Display Priority", _draft.DisplayPriority);
            _draft.PlayerVisible = EditorGUILayout.Toggle("Player Visible", _draft.PlayerVisible);

            if (_draft.Category == TagCategory.AttackType)
                EditorGUILayout.HelpBox("Attack type tags are managed by AttackTypeProfileCatalog.", MessageType.Warning);
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New"))
            {
                _selectedIndex = -1;
                _draft = new CustomTagRecord();
            }

            GUI.enabled = _selectedIndex >= 0;
            if (GUILayout.Button("Delete Selected"))
            {
                _tags.RemoveAt(_selectedIndex);
                _selectedIndex = -1;
                _draft = new CustomTagRecord();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(_selectedIndex >= 0 ? "Apply Changes" : "Add Tag"))
                ApplyDraft();

            if (GUILayout.Button("Save && Regenerate"))
                SaveAll();
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Reload From Disk"))
                Reload();
        }

        private void ApplyDraft()
        {
            var working = Clone(_draft);
            var listForValidation = new List<CustomTagRecord>(_tags);
            if (_selectedIndex >= 0)
                listForValidation[_selectedIndex] = working;
            else
                listForValidation.Add(working);

            if (!CustomTagValidator.TryValidate(working, listForValidation, out string error))
            {
                SetStatus(error, MessageType.Error);
                return;
            }

            if (_selectedIndex >= 0)
                _tags[_selectedIndex] = working;
            else
                _tags.Add(working);

            _selectedIndex = _tags.FindIndex(t => t.Id == working.Id);
            SetStatus($"Staged '{working.Id}'. Click Save && Regenerate to publish.", MessageType.Info);
        }

        private void SaveAll()
        {
            if (!TagCreatorPersistence.TrySave(_tags, out string error))
            {
                SetStatus(error, MessageType.Error);
                return;
            }

            Reload();
            RefreshUnitCreatorWindows();
            SetStatus($"Saved {_tags.Count} custom tag(s) and regenerated catalog.", MessageType.Info);
        }

        private void Reload()
        {
            _tags.Clear();
            _tags.AddRange(TagCreatorPersistence.Load());
            _selectedIndex = -1;
            _draft = new CustomTagRecord();
            _statusMessage = null;
        }

        private static void RefreshUnitCreatorWindows()
        {
            var windows = Resources.FindObjectsOfTypeAll<UnitCreatorWindow>();
            foreach (var window in windows)
                window.Repaint();
        }

        private static CustomTagRecord Clone(CustomTagRecord source) => new()
        {
            Id = source.Id,
            DisplayName = source.DisplayName,
            Category = source.Category,
            Tooltip = source.Tooltip,
            DisplayPriority = source.DisplayPriority,
            PlayerVisible = source.PlayerVisible
        };

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
        }
    }
}
