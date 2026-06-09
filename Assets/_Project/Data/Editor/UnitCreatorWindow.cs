using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Data.UnitCreation;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public sealed class UnitCreatorWindow : EditorWindow
    {
        private UnitCreationDraft _draft = UnitCreationDraft.CreateDefault();
        private UnitCreationValidationResult _lastValidation;
        private ContentDatabase _database;
        private Vector2 _scroll;
        private string[] _editPieceLabels = System.Array.Empty<string>();
        private PieceDefinitionSO[] _editPieces = System.Array.Empty<PieceDefinitionSO>();
        private int _editIndex = -1;

        [MenuItem("DeadManZone/Unit Creator")]
        public static void Open()
        {
            var window = GetWindow<UnitCreatorWindow>("Unit Creator");
            window.minSize = new Vector2(420f, 600f);
            window.Show();
        }

        private void OnEnable()
        {
            _database = ContentDatabase.Load();
            RefreshEditPieceList();
            ApplyDefaultShopFlags();
        }

        private void OnGUI()
        {
            _database ??= ContentDatabase.Load();

            DrawHeader();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            UnitCreatorFormSections.DrawIdentity(_draft, _database);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);
            UnitCreatorShapeGridDrawer.DrawPresets(_draft);
            UnitCreatorShapeGridDrawer.Draw(_draft);

            EditorGUILayout.Space(8);
            UnitCreatorFormSections.DrawTags(_draft);

            EditorGUILayout.Space(8);
            UnitCreatorFormSections.DrawStats(_draft);

            EditorGUILayout.Space(8);
            UnitCreatorFormSections.DrawVisuals(_draft);

            EditorGUILayout.Space(8);
            UnitCreatorFormSections.DrawRegistration(_draft);

            EditorGUILayout.Space(8);
            UnitCreatorFormSections.DrawValidation(_lastValidation);

            EditorGUILayout.EndScrollView();
            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New", EditorStyles.toolbarButton))
            {
                _draft = UnitCreationDraft.CreateDefault();
                _editIndex = -1;
                ApplyDefaultShopFlags();
            }

            DrawEditDropdown();

            if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton) && _editIndex >= 0)
            {
                _draft = UnitCreationDraft.FromPiece(_editPieces[_editIndex], editMode: false).CloneForDuplicate();
                _editIndex = -1;
                ApplyDefaultShopFlags();
            }

            if (GUILayout.Button("Reset", EditorStyles.toolbarButton))
            {
                _draft = UnitCreationDraft.CreateDefault();
                _editIndex = -1;
                ApplyDefaultShopFlags();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEditDropdown()
        {
            if (_editPieces.Length == 0)
            {
                EditorGUILayout.LabelField("Edit", "(no pieces)", EditorStyles.toolbarButton);
                return;
            }

            int next = EditorGUILayout.Popup(_editIndex, _editPieceLabels, EditorStyles.toolbarPopup);
            if (next == _editIndex)
                return;

            _editIndex = next;
            if (_editIndex >= 0 && _editIndex < _editPieces.Length)
            {
                _draft = UnitCreationDraft.FromPiece(_editPieces[_editIndex], editMode: true);
                _draft.SourceAssetPath = UnitPersistenceService.GetPieceAssetPath(_draft.id);
            }
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate"))
                RunValidation();

            var saveLabel = _draft.Mode == UnitCreatorMode.Edit ? "Save Changes" : "Create Unit";
            if (GUILayout.Button(saveLabel))
            {
                RunValidation();
                if (_lastValidation != null && !_lastValidation.HasErrors)
                {
                    if (UnitPersistenceService.TrySave(_draft, out var error))
                    {
                        RefreshEditPieceList();
                        if (_draft.Mode == UnitCreatorMode.Create)
                        {
                            _draft.Mode = UnitCreatorMode.Edit;
                            _editIndex = System.Array.FindIndex(_editPieces, p => p != null && p.id == _draft.id);
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Save Failed", error, "OK");
                    }
                }
            }

            GUI.enabled = _draft.Mode == UnitCreatorMode.Edit;
            if (GUILayout.Button("Delete"))
            {
                if (UnitPersistenceService.TryDelete(_draft.id, out _))
                {
                    _draft = UnitCreationDraft.CreateDefault();
                    _editIndex = -1;
                    RefreshEditPieceList();
                    ApplyDefaultShopFlags();
                }
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void RunValidation()
        {
            _lastValidation = UnitCreationValidator.Validate(
                _draft,
                idExistsInProject: _draft.Mode == UnitCreatorMode.Create && UnitPersistenceService.IdAssetExists(_draft.id),
                idRegisteredInDatabase: UnitPersistenceService.IsRegisteredInDatabase(_draft.id, _database));
        }

        private void RefreshEditPieceList()
        {
            _database = ContentDatabase.Load();
            _editPieces = _database?.Pieces?.Where(p => p != null).OrderBy(p => p.displayName).ToArray()
                          ?? System.Array.Empty<PieceDefinitionSO>();
            _editPieceLabels = _editPieces.Select(p => $"{p.displayName} ({p.id})").ToArray();
        }

        private void ApplyDefaultShopFlags()
        {
            _draft.includeInShopPool = _draft.category == PieceCategory.Unit;
            _draft.addToContentDatabase = true;
        }
    }
}
