using System.Collections;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Presentation.Board;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DeadManZone.PlayMode.Tests
{
    public sealed class BoardViewPlayModeTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        [UnityTest]
        public IEnumerator BuildBoard_CreatesTilesAndMarksSpecial()
        {
            _root = new GameObject("BoardRoot");
            var view = _root.AddComponent<BoardView>();
            var grid = _root.AddComponent<GridLayoutGroup>();

            var tilePrefab = new GameObject("TilePrefab");
            tilePrefab.SetActive(false);
            tilePrefab.transform.SetParent(_root.transform, false);
            tilePrefab.AddComponent<RectTransform>();
            tilePrefab.AddComponent<Image>();
            var tileView = tilePrefab.AddComponent<BoardTileView>();
            tileView.SetOverlay(new Color(0f, 0f, 0f, 0f), false, false);

            view.InitializeForTests(_root.transform, grid, tilePrefab, tileView);
            var layout = BoardLayout.CreateStandard(
                width: 8,
                height: 6,
                rearRows: 2,
                supportRows: 2,
                specialTiles: new[] { new GridCoord(1, 2) });
            view.BuildBoard(layout);

            yield return null;

            Assert.AreEqual(8 * 6, view.TileCount);
            var specialTile = view.GetTile(new GridCoord(1, 2));
            Assert.NotNull(specialTile);
            Assert.IsTrue(specialTile.IsSpecial);
        }
    }
}
