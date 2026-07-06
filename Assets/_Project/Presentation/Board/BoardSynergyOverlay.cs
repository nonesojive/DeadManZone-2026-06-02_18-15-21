using System;
using System.Collections.Generic;
using UnityEngine;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;

namespace DeadManZone.Presentation.Board
{
    /// <summary>
    /// Visualizes ability aura links between pieces on the board.
    /// </summary>
    public sealed class BoardSynergyOverlay : MonoBehaviour
    {
        private readonly List<SynergyLinkView> _linkPool = new();
        private int _activeCount = 0;

        public void RefreshLinks(BoardState board, IReadOnlyDictionary<string, PieceShapeVisual> visualMap)
        {
            if (board == null || visualMap == null)
            {
                ClearLinks();
                return;
            }

            var snapshot = PieceAbilityEngine.EvaluateFightStart(board);
            _activeCount = 0;

            foreach (var link in snapshot.Links)
            {
                if (!visualMap.TryGetValue(link.SourceInstanceId, out var sourceVisual)
                    || !visualMap.TryGetValue(link.TargetInstanceId, out var targetVisual))
                {
                    continue;
                }

                string sourceAbilityId = link.SourceTagId;
                var color = GetLinkColor(sourceAbilityId, link.Stat);
                var linkView = GetOrCreateLink();
                linkView.gameObject.name = string.IsNullOrWhiteSpace(sourceAbilityId)
                    ? "AbilityLink"
                    : $"AbilityLink_{sourceAbilityId}";
                linkView.gameObject.SetActive(true);
                linkView.SetColor(color);
                linkView.UpdatePoints(sourceVisual.Center, targetVisual.Center);
                // Piece visuals are destroyed and recreated each refresh, landing as
                // later siblings; the pooled links would otherwise stay buried behind
                // them. Re-float active links to the top so adjacency cues stay visible.
                linkView.transform.SetAsLastSibling();
                _activeCount++;
            }

            for (int i = _activeCount; i < _linkPool.Count; i++)
                _linkPool[i].gameObject.SetActive(false);
        }

        private void ClearLinks()
        {
            foreach (var link in _linkPool)
                link.gameObject.SetActive(false);

            _activeCount = 0;
        }

        private SynergyLinkView GetOrCreateLink()
        {
            if (_activeCount < _linkPool.Count)
                return _linkPool[_activeCount];

            var link = SynergyLinkView.Create((RectTransform)transform, Vector2.zero, Vector2.zero, Color.white);
            _linkPool.Add(link);
            return link;
        }

        private static Color GetLinkColor(string sourceAbilityId, SynergyStat stat)
        {
            if (!string.IsNullOrWhiteSpace(sourceAbilityId))
            {
                if (sourceAbilityId.IndexOf("armor", StringComparison.OrdinalIgnoreCase) >= 0)
                    return Color.green;
                if (sourceAbilityId.IndexOf("artillery", StringComparison.OrdinalIgnoreCase) >= 0
                    || sourceAbilityId.IndexOf("command", StringComparison.OrdinalIgnoreCase) >= 0)
                    return Color.cyan;
                if (sourceAbilityId.IndexOf("move", StringComparison.OrdinalIgnoreCase) >= 0
                    || sourceAbilityId.IndexOf("inspiring", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new Color(0f, 0.85f, 1f);
                if (sourceAbilityId.IndexOf("supply", StringComparison.OrdinalIgnoreCase) >= 0)
                    return Color.yellow;
                if (sourceAbilityId.IndexOf("damage", StringComparison.OrdinalIgnoreCase) >= 0)
                    return Color.red;
            }

            return stat switch
            {
                SynergyStat.Damage => Color.red,
                SynergyStat.ArmorType => Color.green,
                SynergyStat.MoveChargePercent => Color.cyan,
                _ => Color.white
            };
        }
    }
}
