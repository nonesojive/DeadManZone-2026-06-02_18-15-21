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

        /// <summary>Demo shop roster (8 pieces). HQ and legacy pieces register by id only.</summary>
        public static readonly HashSet<string> DemoShopPieceIds = new()
        {
            "conscript_rifleman",
            "grenade_thrower",
            "field_medic",
            "armored_transport",
            "mobile_cannon",
            "rifle_squad",
            "diesel_walker",
            "radio_array"
        };

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
            {
                bool inShop = DemoShopPieceIds.Contains(piece.id);
                registry.Register(piece.ToCore(), piece.shopLane, includeInShopPool: inShop);
            }

            return registry;
        }

        public FactionSO GetFaction(string factionId) =>
            factions.FirstOrDefault(f => f != null && f.factionId == factionId);

        public EnemyTemplateSO GetEnemyTemplate(int fightNumber)
        {
            var direct = enemyTemplates.FirstOrDefault(e => e != null && e.fightNumber == fightNumber);
            if (direct != null)
                return direct;

            var sorted = enemyTemplates
                .Where(e => e != null)
                .OrderBy(e => e.fightNumber)
                .ToArray();
            if (sorted.Length == 0)
                return null;

            int index = (fightNumber - 1) % sorted.Length;
            return sorted[index];
        }

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
