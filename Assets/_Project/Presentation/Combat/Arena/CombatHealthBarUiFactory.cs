using DeadManZone.Presentation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Builds the army health bar UI hierarchy under the combat panel.</summary>
    public static class CombatHealthBarUiFactory
    {
        public static ArmyHealthBarPresenter CreateUnder(Transform combatPanel)
        {
            var existing = combatPanel.GetComponentInChildren<ArmyHealthBarPresenter>(true);
            if (existing != null)
                return existing;

            var root = new GameObject("ArmyHealthBars", typeof(RectTransform));
            root.transform.SetParent(combatPanel, false);
            StretchTopBand(root.GetComponent<RectTransform>());

            var layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var playerBar = CreateBar(root.transform, "PlayerArmyBar", new Color(0.2f, 0.75f, 0.35f));
            var enemyBar = CreateBar(root.transform, "EnemyArmyBar", new Color(0.85f, 0.25f, 0.2f));

            var presenter = root.AddComponent<ArmyHealthBarPresenter>();
            presenter.BindViews(playerBar, enemyBar);
            return presenter;
        }

        private static void StretchTopBand(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.08f, 0.88f);
            rect.anchorMax = new Vector2(0.92f, 0.96f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static ArmyHealthBarView CreateBar(Transform parent, string name, Color fillColor)
        {
            var barRoot = new GameObject(name, typeof(RectTransform));
            barRoot.transform.SetParent(parent, false);
            barRoot.AddComponent<LayoutElement>().preferredHeight = 22f;

            var background = barRoot.AddComponent<Image>();
            background.color = new Color(0.08f, 0.1f, 0.12f, 0.85f);
            background.raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(barRoot.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0.02f, 0.2f);
            fillRect.anchorMax = new Vector2(0.98f, 0.8f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var fillImage = fillGo.AddComponent<Image>();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.color = fillColor;
            fillImage.fillAmount = 1f;
            fillImage.raycastTarget = false;
            if (fillImage.sprite == null)
                fillImage.sprite = UiWhiteSprite.Get();

            AddNotch(fillGo.transform, 0.75f, "Notch75");
            AddNotch(fillGo.transform, 0.30f, "Notch30");

            var view = barRoot.AddComponent<ArmyHealthBarView>();
            view.BindFillImage(fillImage);
            return view;
        }

        private static void AddNotch(Transform parent, float normalizedX, string name)
        {
            const float notchWidth = 0.008f;
            var notchGo = new GameObject(name, typeof(RectTransform));
            notchGo.transform.SetParent(parent, false);
            var notch = notchGo.GetComponent<RectTransform>();
            notch.anchorMin = new Vector2(normalizedX - notchWidth * 0.5f, 0f);
            notch.anchorMax = new Vector2(normalizedX + notchWidth * 0.5f, 1f);
            notch.offsetMin = Vector2.zero;
            notch.offsetMax = Vector2.zero;

            var image = notchGo.AddComponent<Image>();
            image.color = new Color(0.65f, 0.62f, 0.56f, 0.65f);
            image.raycastTarget = false;
        }
    }
}
