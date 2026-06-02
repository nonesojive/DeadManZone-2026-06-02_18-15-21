using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Content;
using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Content Database")]
    public class ContentDatabase : ScriptableObject
    {
        private const string ResourcesPath = "DeadManZone/ContentDatabase";

        [SerializeField] private PieceDefinitionSO[] pieces = System.Array.Empty<PieceDefinitionSO>();
        [SerializeField] private FactionSO[] factions = System.Array.Empty<FactionSO>();
        [SerializeField] private EnemyTemplateSO[] enemyTemplates = System.Array.Empty<EnemyTemplateSO>();

        public IReadOnlyList<PieceDefinitionSO> Pieces => pieces;
        public IReadOnlyList<FactionSO> Factions => factions;
        public IReadOnlyList<EnemyTemplateSO> EnemyTemplates => enemyTemplates;

        public ContentRegistry BuildRegistry()
        {
            var registry = new ContentRegistry();
            foreach (var piece in pieces.Where(p => p != null))
                registry.Register(piece.ToCore(), piece.shopLane);

            return registry;
        }

        public FactionSO GetFaction(string factionId) =>
            factions.FirstOrDefault(f => f != null && f.factionId == factionId);

        public EnemyTemplateSO GetEnemyTemplate(int fightNumber) =>
            enemyTemplates.FirstOrDefault(e => e != null && e.fightNumber == fightNumber);

        public static ContentDatabase Load()
        {
            var fromResources = Resources.Load<ContentDatabase>(ResourcesPath);
            if (fromResources != null)
                return fromResources;

            var instances = Resources.LoadAll<ContentDatabase>("");
            return instances.Length > 0 ? instances[0] : null;
        }
    }
}
