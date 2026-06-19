using DeadManZone.Presentation.Board;
using DeadManZone.Presentation.Run;
using DeadManZone.Presentation.UI;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class UnitCardPanelBootstrapTests
    {
        [Test]
        public void EnsureCardView_ReplacesLegacyPieceHoverCardChild()
        {
            var buildPanel = new GameObject("BuildPanel");
            var panelGo = new GameObject("UnitCardPanel", typeof(RectTransform));
            panelGo.transform.SetParent(buildPanel.transform, false);

            var legacyGo = new GameObject("PieceHoverCard", typeof(RectTransform));
            legacyGo.transform.SetParent(panelGo.transform, false);
            legacyGo.AddComponent<PieceHoverCard>();

            var panelView = panelGo.AddComponent<UnitCardPanelView>();

            try
            {
                UnitCardPanelBootstrap.EnsureOnBuildPanel(buildPanel.transform);

                Assert.IsNull(panelGo.GetComponentInChildren<PieceHoverCard>(true));
                Assert.NotNull(panelGo.GetComponentInChildren<PieceCardView>(true));
                panelView.EnsureCardView();
                Assert.NotNull(panelGo.GetComponentInChildren<PieceCardView>(true));
            }
            finally
            {
                Object.DestroyImmediate(buildPanel);
            }
        }
    }
}
