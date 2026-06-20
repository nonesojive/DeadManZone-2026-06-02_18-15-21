using DeadManZone.Core.Tags;
using DeadManZone.Core.Tests;
using DeadManZone.Presentation.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class PieceCardViewTests
    {
        [Test]
        public void Bind_PreservesAuthoredAnchors_WhenNameTextIsWired()
        {
            var root = new GameObject("PieceCardViewRoot", typeof(RectTransform));
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.sizeDelta = new Vector2(0f, 20f);

                var view = root.AddComponent<PieceCardView>();
                var name = new GameObject("Name", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                var hp = new GameObject("Hp", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                name.transform.SetParent(root.transform, false);
                hp.transform.SetParent(root.transform, false);
                view.InitializeForTests(name, hp);

                view.Bind(PieceCardViewModelBuilder.Build(TestPieces.RifleSquad()), string.Empty);

                Assert.AreEqual(Vector2.zero, rect.anchorMin);
                Assert.AreEqual(Vector2.zero, rect.anchorMax);
                Assert.AreEqual(0f, rect.sizeDelta.x);
                Assert.AreEqual(20f, rect.sizeDelta.y);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Bind_PreservesAuthoredBackgroundTint_WhenSpriteAssigned()
        {
            var root = new GameObject("PieceCardViewRoot", typeof(RectTransform), typeof(Image));
            try
            {
                var view = root.AddComponent<PieceCardView>();
                var background = root.GetComponent<Image>();
                background.sprite = CreateTestSprite();
                background.color = Color.white;

                var name = new GameObject("Name", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                var hp = new GameObject("Hp", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                name.transform.SetParent(root.transform, false);
                hp.transform.SetParent(root.transform, false);
                view.InitializeForTests(name, hp, background);

                view.Bind(PieceCardViewModelBuilder.Build(TestPieces.RifleSquad()), string.Empty);

                Assert.AreEqual(Color.white, background.color);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Bind_SetsDisplayNameAndHp()
        {
            var root = new GameObject("PieceCardViewRoot");
            try
            {
                var view = root.AddComponent<PieceCardView>();
                var name = new GameObject("Name", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                var hp = new GameObject("Hp", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                name.transform.SetParent(root.transform, false);
                hp.transform.SetParent(root.transform, false);

                view.InitializeForTests(name, hp);

                PieceCardViewModel model = PieceCardViewModelBuilder.Build(TestPieces.RifleSquad());
                view.Bind(model, string.Empty);

                Assert.AreEqual(model.DisplayName, view.NameTextForTests);
                Assert.AreEqual($"HP: {model.Hp}", view.HpTextForTests);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Sprite CreateTestSprite()
        {
            var texture = new Texture2D(4, 4);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white,
                Color.white, Color.white, Color.white, Color.white,
                Color.white, Color.white, Color.white, Color.white,
                Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
        }
    }
}
