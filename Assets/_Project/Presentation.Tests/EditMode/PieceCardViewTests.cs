using System.Linq;
using DeadManZone.Core.Board;
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
        public void Bind_NormalizesOversizedTagChipPrefab_OnSpawn()
        {
            var root = new GameObject("PieceCardViewRoot", typeof(RectTransform));
            try
            {
                var view = root.AddComponent<PieceCardView>();
                var containerGo = new GameObject("TagChips", typeof(RectTransform));
                containerGo.transform.SetParent(root.transform, false);
                var container = containerGo.GetComponent<RectTransform>();

                var chipPrefab = CreateOversizedTagChipPrefab();
                var name = new GameObject("Name", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                var hp = new GameObject("Hp", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                name.transform.SetParent(root.transform, false);
                hp.transform.SetParent(root.transform, false);

                view.InitializeForTests(name, hp, chipContainer: container, chipPrefab: chipPrefab);
                view.Bind(PieceCardViewModelBuilder.Build(TestPieces.RifleSquad()), string.Empty);

                var spawned = container.GetChild(0) as RectTransform;
                Assert.NotNull(spawned);
                Assert.LessOrEqual(spawned.sizeDelta.y, 26f);
                Assert.LessOrEqual(spawned.sizeDelta.x, 132f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Bind_SpawnsTagChipsFromPrefab_WhenTagChipPrefabAssigned()
        {
            var root = new GameObject("PieceCardViewRoot", typeof(RectTransform));
            try
            {
                var view = root.AddComponent<PieceCardView>();
                var containerGo = new GameObject("TagChips", typeof(RectTransform));
                containerGo.transform.SetParent(root.transform, false);
                var container = containerGo.GetComponent<RectTransform>();

                var chipPrefab = CreateTagChipPrefab();
                var name = new GameObject("Name", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                var hp = new GameObject("Hp", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                name.transform.SetParent(root.transform, false);
                hp.transform.SetParent(root.transform, false);

                view.InitializeForTests(name, hp, chipContainer: container, chipPrefab: chipPrefab);
                view.Bind(PieceCardViewModelBuilder.Build(TestPieces.RifleSquad()), string.Empty);

                Assert.GreaterOrEqual(container.childCount, 1);
                Assert.NotNull(container.GetComponentInChildren<Image>(true));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Bind_SetsDisplayNameAndNumericStats()
        {
            var root = new GameObject("PieceCardViewRoot");
            try
            {
                var view = root.AddComponent<PieceCardView>();
                var name = new GameObject("Name", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                var hp = new GameObject("Hp", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                var damage = new GameObject("Damage", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                var movement = new GameObject("Movement", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                var attackSpeed = new GameObject("AttackSpeed", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                var primaryTag = new GameObject("PrimaryTag", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                name.transform.SetParent(root.transform, false);
                hp.transform.SetParent(root.transform, false);
                damage.transform.SetParent(root.transform, false);
                movement.transform.SetParent(root.transform, false);
                attackSpeed.transform.SetParent(root.transform, false);
                primaryTag.transform.SetParent(root.transform, false);

                view.InitializeForTests(
                    name,
                    hp,
                    damage: damage,
                    movementSpeed: movement,
                    attackSpeed: attackSpeed,
                    primaryTag: primaryTag);

                PieceCardViewModel model = PieceCardViewModelBuilder.Build(
                    TestPieces.CreateUnit(
                        "rifle",
                        primary: GameTagIds.Infantry,
                        combatRole: GameTagIds.Assault,
                        systemTag: GameTagIds.Combatant));
                view.Bind(model, string.Empty);

                Assert.AreEqual(model.DisplayName, view.NameTextForTests);
                Assert.AreEqual(model.Hp.ToString(), view.HpTextForTests);
                Assert.AreEqual(model.BaseDamage.ToString(), view.DamageTextForTests);
                Assert.AreEqual(model.MovementSpeedValue.ToString(), view.MovementSpeedTextForTests);
                Assert.AreEqual(model.AttackSpeedValue.ToString(), view.AttackSpeedTextForTests);
                Assert.AreEqual(model.PrimaryTag.DisplayName, view.PrimaryTagTextForTests);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Bind_SetsArmorIconFromCatalog()
        {
            var root = new GameObject("PieceCardViewRoot");
            try
            {
                var view = root.AddComponent<PieceCardView>();
                var name = new GameObject("Name", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                var hp = new GameObject("Hp", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                var armor = new GameObject("Armor", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                name.transform.SetParent(root.transform, false);
                hp.transform.SetParent(root.transform, false);
                armor.transform.SetParent(root.transform, false);

                var icons = ScriptableObject.CreateInstance<UnitCardIconsSO>();
                icons.hideFlags = HideFlags.HideAndDontSave;
                var heavyShield = CreateTestSprite();
                icons.AssignArmorIconsForTests(null, null, heavyShield);

                view.InitializeForTests(name, hp, armor: armor, cardIcons: icons);
                view.Bind(
                    PieceCardViewModelBuilder.Build(TestPieces.With(TestPieces.RifleSquad(), armorType: ArmorType.Heavy)),
                    string.Empty);

                Assert.AreEqual(heavyShield, view.ArmorIconSpriteForTests);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Bind_UsesChipTagsOnly_ForTagRow()
        {
            var root = new GameObject("PieceCardViewRoot");
            try
            {
                var view = root.AddComponent<PieceCardView>();
                var containerGo = new GameObject("TagChips", typeof(RectTransform));
                containerGo.transform.SetParent(root.transform, false);
                var container = containerGo.GetComponent<RectTransform>();

                var chipPrefab = CreateTagChipPrefab();
                var name = new GameObject("Name", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                var hp = new GameObject("Hp", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                name.transform.SetParent(root.transform, false);
                hp.transform.SetParent(root.transform, false);

                view.InitializeForTests(name, hp, chipContainer: container, chipPrefab: chipPrefab);

                var piece = TestPieces.CreateUnit(
                    "rifle",
                    primary: GameTagIds.Infantry,
                    combatRole: GameTagIds.Assault,
                    systemTag: GameTagIds.Combatant);
                PieceCardViewModel model = PieceCardViewModelBuilder.Build(piece);
                view.Bind(model, string.Empty);

                Assert.IsFalse(model.ChipTags.Any(t => t.Id == GameTagIds.Infantry));
                Assert.IsFalse(model.ChipTags.Any(t => t.Id == GameTagIds.Assault));
                Assert.IsTrue(model.ChipTags.Any(t => t.Id == "neutral"));
                Assert.GreaterOrEqual(view.TagChipCountForTests, 1);
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

        [Test]
        public void Bind_SetsAbilityTextOnAuthoredSlot()
        {
            var root = new GameObject("PieceCardViewRoot", typeof(RectTransform));
            try
            {
                var view = root.AddComponent<PieceCardView>();
                var name = new GameObject("Name", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                var hp = new GameObject("Hp", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                name.transform.SetParent(root.transform, false);
                hp.transform.SetParent(root.transform, false);

                var abilityFrame = new GameObject("AbilityFrame_UnitCard", typeof(RectTransform));
                abilityFrame.transform.SetParent(root.transform, false);
                var ability = new GameObject("AbilityText_UnitCard", typeof(TextMeshProUGUI));
                ability.transform.SetParent(abilityFrame.transform, false);
                var abilityText = ability.GetComponent<TextMeshProUGUI>();

                view.InitializeForTests(name, hp, ability: abilityText);

                var aura = new PieceAbilityDefinition
                {
                    Id = "adjacent_infantry_armor_plus_one",
                    CardDescription = "Adjacent infantry gain +1 armor."
                };
                var piece = TestPieces.With(TestPieces.RifleSquad(), abilities: new[] { aura });
                view.Bind(PieceCardViewModelBuilder.Build(piece), string.Empty);

                Assert.AreEqual("Adjacent infantry gain +1 armor.", view.AbilityTextForTests);
                Assert.IsTrue(abilityFrame.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateTagChipPrefab()
        {
            var chip = new GameObject("TagChip", typeof(RectTransform), typeof(Image));
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(chip.transform, false);
            return chip;
        }

        private static GameObject CreateOversizedTagChipPrefab()
        {
            var chip = new GameObject("TagChip", typeof(RectTransform), typeof(Image));
            chip.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 225f);
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(chip.transform, false);
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            label.enableAutoSizing = true;
            label.fontSize = 72f;
            return chip;
        }
    }
}
