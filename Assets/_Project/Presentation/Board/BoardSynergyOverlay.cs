using System.Collections.Generic;
using UnityEngine;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;

namespace DeadManZone.Presentation.Board
{
    /// <summary>
    /// Visualizes synergy links between pieces on the board.
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

            var snapshot = SynergyEngine.EvaluateFightStart(board);
            _activeCount = 0;

            foreach (var link in snapshot.Links)
            {
                if (visualMap.TryGetValue(link.SourceInstanceId, out var sourceVisual) &&
                    visualMap.TryGetValue(link.TargetInstanceId, out var targetVisual))
                {
                    var color = GetLinkColor(link.SourceTagId);
                    var linkView = GetOrCreateLink();
                    linkView.gameObject.SetActive(true);
                    linkView.SetColor(color);
                    linkView.UpdatePoints(sourceVisual.Center, targetVisual.Center);
                    _activeCount++;
                }
            }

            for (int i = _activeCount; i < _linkPool.Count; i++)
            {
                _linkPool[i].gameObject.SetActive(false);
            }
        }

        private void ClearLinks()
        {
            foreach (var link in _linkPool)
            {
                link.gameObject.SetActive(false);
            }
            _activeCount = 0;
        }

        private SynergyLinkView GetOrCreateLink()
        {
            if (_activeCount < _linkPool.Count)
            {
                var existing = _linkPool[_activeCount];
                return existing;
            }

            var link = SynergyLinkView.Create((RectTransform)transform, Vector2.zero, Vector2.zero, Color.white);
            _linkPool.Add(link);
            return link;
        }

        private Color GetLinkColor(string tagId)
        {
            if (string.IsNullOrEmpty(tagId)) return Color.white;

            return tagId switch
            {
                "Supply" => Color.yellow,
                "Medic" => Color.green,
                "Command" => Color.cyan,
                "Vanguard" => Color.red,
                _ => Color.white
            };
        }
    }
}
