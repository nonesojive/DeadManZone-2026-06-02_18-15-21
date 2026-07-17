using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Content;
using DeadManZone.Core.Shop;
using UnityEngine;

namespace DeadManZone.Data
{
    [CreateAssetMenu(menuName = "DeadManZone/Content Database")]
    public class ContentDatabase : ScriptableObject
    {
        private const string ResourcesPath = "DeadManZone/ContentDatabase";

        /// <summary>Playable factions shown in faction select. 2026-07-15 faction-roster-v1
        /// Wave 2: all 8 factions now have a full content pass — see FactionIds.Playable
        /// (kept in sync with this array by the same commit).</summary>
        public static readonly string[] PlayableFactionIds =
        {
            FactionIds.IronmarchUnion,
            FactionIds.DustScourge,
            FactionIds.CartelOfEchoes,
            FactionIds.OathbornAccord,
            FactionIds.ParadoxEngine,
            FactionIds.BlightbornPact,
            FactionIds.CrimsonAssembly,
            FactionIds.AshenCovenant
        };

        [SerializeField] private PieceDefinitionSO[] pieces = System.Array.Empty<PieceDefinitionSO>();
        [SerializeField] private FactionSO[] factions = System.Array.Empty<FactionSO>();
        [SerializeField] private EnemyTemplateSO[] enemyTemplates = System.Array.Empty<EnemyTemplateSO>();
        [SerializeField] private ShopConfigSO shopConfig;

        public IReadOnlyList<PieceDefinitionSO> Pieces => pieces;
        public IReadOnlyList<FactionSO> Factions => factions;
        public IReadOnlyList<EnemyTemplateSO> EnemyTemplates => enemyTemplates;
        public ShopConfigSO ShopConfigAsset => shopConfig;

        public ContentRegistry BuildRegistry()
        {
            var registry = new ContentRegistry();
            foreach (var piece in pieces.Where(p => p != null))
            {
                var lane = ShopLaneResolver.ResolveLane(piece.combatRole);
                if (lane == ShopLane.Specialty)
                    lane = ShopLane.Offensive;
                registry.Register(piece.ToCore(), lane, includeInShopPool: piece.includeInShopPool);
            }

            return registry;
        }

        public ShopConfig BuildShopConfig()
        {
            if (shopConfig != null)
                return shopConfig.ToCore();

            var loaded = ShopConfigSO.LoadOrDefault();
            return loaded != null ? loaded.ToCore() : ShopConfig.CreateDefault();
        }

        public FactionShopOverride GetShopOverride(string factionId)
        {
            var faction = GetFaction(factionId);
            return faction?.shopOverride != null ? faction.shopOverride.ToCore() : null;
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

        /// <summary>Faction-aware lookup — Wave 5 (2026-07-17): once more than one faction's
        /// enemy templates share the same fightNumber, <see cref="GetEnemyTemplate(int)"/>'s
        /// plain FirstOrDefault can no longer tell which pool a Fight Option actually rolled;
        /// every call site that already knows the chosen option's/boss's EnemyFactionId
        /// (RunOrchestrator.GetOptionEnemyBoard/BeginCombat/GetNextEnemyPreviewTag) must use
        /// this overload instead. Falls back to the plain lookup if the faction has no
        /// templates of its own (keeps legacy/partial content working).</summary>
        public EnemyTemplateSO GetEnemyTemplate(int fightNumber, string enemyFactionId)
        {
            if (string.IsNullOrEmpty(enemyFactionId))
                return GetEnemyTemplate(fightNumber);

            var ofFaction = enemyTemplates
                .Where(e => e != null && e.enemyFactionId == enemyFactionId)
                .ToArray();
            if (ofFaction.Length == 0)
                return GetEnemyTemplate(fightNumber);

            var exact = ofFaction.FirstOrDefault(e => e.fightNumber == fightNumber);
            if (exact != null)
                return exact;

            // Nearest fight number WITHIN the same faction — never cross pools.
            return ofFaction
                .OrderBy(e => Math.Abs(e.fightNumber - fightNumber))
                .ThenBy(e => e.fightNumber)
                .First();
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
