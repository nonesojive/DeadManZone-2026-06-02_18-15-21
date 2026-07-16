using System;
using System.Linq;
using DeadManZone.Data.UnitCreation;
using DeadManZone.Game.Dev;
using UnityEngine;

namespace DeadManZone.Presentation.Dev
{
    /// <summary>Play-mode dev panel for prototyping units. Toggle with F9.</summary>
    public sealed class UnitCreatorRuntimePanel : MonoBehaviour
    {
        public static UnitCreatorRuntimePanel Instance { get; private set; }

        public static Func<UnitCreationDraft, (bool ok, string error)> SaveToProjectHandler { get; set; }

        private UnitCreationDraft _draft = UnitCreationDraft.CreateDefault();
        private bool _visible;
        private Vector2 _scroll;
        private string _statusMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
                return;

            var host = new GameObject(nameof(UnitCreatorRuntimePanel));
            DontDestroyOnLoad(host);
            host.AddComponent<UnitCreatorRuntimePanel>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SessionContentOverlay.Ensure();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            SessionContentOverlay.ClearInstance();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible)
                return;

            const float width = 360f;
            var rect = new Rect(Screen.width - width - 12f, 12f, width, Screen.height - 24f);
            GUILayout.BeginArea(rect, GUI.skin.window);
            GUILayout.Label("Unit Creator (Dev)", EditorLikeBoldLabel());

            _scroll = GUILayout.BeginScrollView(_scroll);
            DrawForm();
            GUILayout.EndScrollView();

            DrawActions();
            if (!string.IsNullOrEmpty(_statusMessage))
                GUILayout.Label(_statusMessage, GUI.skin.box);

            GUILayout.EndArea();
        }

        private void DrawForm()
        {
            _draft.id = LabeledTextField("Id", _draft.id);
            _draft.displayName = LabeledTextField("Display Name", _draft.displayName);
            _draft.factionId = LabeledTextField("Faction Id", _draft.factionId);
            _draft.combatRole = LabeledTextField("Combat Role", _draft.combatRole);
            _draft.primary = LabeledTextField("Primary Tag", _draft.primary);
            _draft.maxHp = LabeledInt("Max HP", _draft.maxHp);
            _draft.baseDamage = LabeledInt("Damage", _draft.baseDamage);
            _draft.includeInShopPool = LabeledToggle("Shop Pool", _draft.includeInShopPool);
            _draft.addToContentDatabase = LabeledToggle("Add to DB on Save", _draft.addToContentDatabase);
            GUILayout.Label($"Shop Lane: {_draft.ComputedShopLane}");

            GUILayout.Label("Shape Preset");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1x1")) _draft.shapeCells = new[] { Vector2Int.zero };
            if (GUILayout.Button("1x2")) _draft.shapeCells = new[] { Vector2Int.zero, Vector2Int.up };
            if (GUILayout.Button("2x1")) _draft.shapeCells = new[] { Vector2Int.zero, Vector2Int.right };
            GUILayout.EndHorizontal();
        }

        private void DrawActions()
        {
            if (GUILayout.Button("Spawn Prototype"))
            {
                if (SessionContentOverlay.Ensure().TryAdd(_draft, out var error))
                    _statusMessage = $"Prototype '{_draft.id}' spawned in session.";
                else
                    _statusMessage = error;
            }

            if (GUILayout.Button("Save to Project"))
            {
                if (SaveToProjectHandler == null)
                {
                    _statusMessage = "Save to project is only available in the Unity Editor.";
                    return;
                }

                var (ok, error) = SaveToProjectHandler(_draft);
                _statusMessage = ok ? $"Saved '{_draft.id}' to project." : error;
            }

            if (GUILayout.Button("Clear Session Prototypes"))
            {
                SessionContentOverlay.Ensure();
                foreach (var entry in SessionContentOverlay.Instance.Prototypes.ToArray())
                    SessionContentOverlay.Instance.Remove(entry.Id);
                _statusMessage = "Session prototypes cleared.";
            }
        }

        private static string LabeledTextField(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(110));
            var next = GUILayout.TextField(value ?? string.Empty);
            GUILayout.EndHorizontal();
            return next;
        }

        private static int LabeledInt(string label, int value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(110));
            var text = GUILayout.TextField(value.ToString());
            GUILayout.EndHorizontal();
            return int.TryParse(text, out var parsed) ? parsed : value;
        }

        private static bool LabeledToggle(string label, bool value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(110));
            var next = GUILayout.Toggle(value, "");
            GUILayout.EndHorizontal();
            return next;
        }

        private static GUIStyle EditorLikeBoldLabel()
        {
            var style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            return style;
        }
    }
}
