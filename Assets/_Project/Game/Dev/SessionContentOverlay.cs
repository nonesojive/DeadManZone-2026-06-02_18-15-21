using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Content;
using DeadManZone.Core.Shop;
using DeadManZone.Data;
using DeadManZone.Data.UnitCreation;

namespace DeadManZone.Game.Dev
{
    public sealed class SessionPrototypeEntry
    {
        public string Id;
        public PieceDefinition Definition;
        public ShopLane Lane;
        public bool IncludeInShopPool;
    }

    /// <summary>Play-mode-only unit prototypes merged into the content registry.</summary>
    public sealed class SessionContentOverlay
    {
        public static SessionContentOverlay Instance { get; private set; }

        private readonly Dictionary<string, SessionPrototypeEntry> _prototypes = new();

        public static SessionContentOverlay Ensure()
        {
            Instance ??= new SessionContentOverlay();
            return Instance;
        }

        public static void ClearInstance() => Instance = null;

        public bool IsEmpty => _prototypes.Count == 0;

        public IReadOnlyCollection<SessionPrototypeEntry> Prototypes => _prototypes.Values;

        public bool TryAdd(UnitCreationDraft draft, out string error)
        {
            error = null;
            var validation = UnitCreationValidator.Validate(
                draft,
                idExistsInProject: false,
                idRegisteredInDatabase: false);

            if (validation.HasErrors)
            {
                error = validation.Messages.FirstOrDefault(m => m.Severity == ValidationSeverity.Error).Message
                        ?? "Validation failed.";
                return false;
            }

            var temp = draft.ToTemporaryPiece();
            var core = temp.ToCore();
            UnityEngine.Object.DestroyImmediate(temp);

            _prototypes[core.Id] = new SessionPrototypeEntry
            {
                Id = core.Id,
                Definition = core,
                Lane = draft.ComputedShopLane,
                IncludeInShopPool = draft.includeInShopPool
            };

            return true;
        }

        public void Remove(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            _prototypes.Remove(id);
        }

        public void ApplyTo(ContentRegistry registry)
        {
            if (registry == null || _prototypes.Count == 0)
                return;

            foreach (var entry in _prototypes.Values)
            {
                registry.Register(entry.Definition, entry.Lane, entry.IncludeInShopPool);
            }
        }
    }
}
