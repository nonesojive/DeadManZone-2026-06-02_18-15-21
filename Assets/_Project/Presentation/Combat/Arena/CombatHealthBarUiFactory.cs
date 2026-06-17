using DeadManZone.Data;
using DeadManZone.Presentation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Builds the army health bar UI hierarchy under the combat panel.</summary>
    public static class CombatHealthBarUiFactory
    {
        private static readonly Color PlayerFill = new(0.34f, 0.48f, 0.30f, 1f);
        private static readonly Color EnemyFill = new(0.62f, 0.28f, 0.22f, 1f);

        public static ArmyHealthBarPresenter CreateUnder(Transform combatPanel)
        {
            if (combatPanel == null)
                return null;

            var existing = combatPanel.GetComponentInChildren<ArmyHealthBarPresenter>(true);
            if (existing != null && UsesSyntyBars(existing))
                return existing;

            if (existing != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(existing.gameObject);
                else
                    Object.DestroyImmediate(existing.gameObject);
            }

            var root = new GameObject("ArmyHealthBars", typeof(RectTransform));
            root.transform.SetParent(combatPanel, false);
            StretchTopBand(root.GetComponent<RectTransform>());

            var bandLayout = root.AddComponent<LayoutElement>();
            bandLayout.minHeight = 64f;
            bandLayout.preferredHeight = 64f;
            bandLayout.flexibleHeight = 0f;

            var layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 28f;
            layout.padding = new RectOffset(8, 8, 4, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var playerBar = CreateBar(root.transform, "PlayerArmyBar", PlayerFill, "ALLIED");
            var enemyBar = CreateBar(root.transform, "EnemyArmyBar", EnemyFill, "HOSTILE");

            var presenter = root.AddComponent<ArmyHealthBarPresenter>();
            presenter.BindViews(playerBar, enemyBar);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(root.GetComponent<RectTransform>());
            return presenter;
        }

        public static bool UsesSyntyBars(ArmyHealthBarPresenter presenter)
        {
            if (presenter == null)
                return false;

            var player = presenter.transform.Find("PlayerArmyBar");
            return player != null && player.Find("SliderBox") != null;
        }

        private static void StretchTopBand(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.05f, 0.905f);
            rect.anchorMax = new Vector2(0.95f, 0.985f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static ArmyHealthBarView CreateBar(
            Transform parent,
            string name,
            Color fillColor,
            string sideLabel)
        {
            var hudAssets = Resources.Load<CombatHudAssetsSO>(CombatApocalypseHudPaths.HudAssetsResourcePath);
            var prefab = hudAssets?.armyHealthBarPrefab
                ?? SyntyRuntimeAssetLoader.LoadPrefab(CombatApocalypseHudPaths.PlayerHealthBar02);

            if (prefab != null)
                return CreateSyntyBar(parent, name, prefab, fillColor, sideLabel);

            return CreateFallbackBar(parent, name, fillColor, sideLabel);
        }

        private static ArmyHealthBarView CreateSyntyBar(
            Transform parent,
            string name,
            GameObject prefab,
            Color fillColor,
            string sideLabel)
        {
            var instance = Object.Instantiate(prefab, parent, false);
            instance.name = name;

            var rect = instance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            var layoutElement = instance.GetComponent<LayoutElement>() ?? instance.AddComponent<LayoutElement>();
            layoutElement.minHeight = 48f;
            layoutElement.preferredHeight = 56f;
            layoutElement.flexibleWidth = 1f;
            layoutElement.minWidth = 300f;
            layoutElement.flexibleHeight = 0f;

            CombatHudChromeBuilder.HideIconBox(instance.transform);
            CombatHudChromeBuilder.AddCheckpointNotches(instance.transform);
            CombatHudChromeBuilder.AddSideLabel(instance.transform, sideLabel, alignLeft: name.Contains("Player"));

            var view = instance.GetComponent<ArmyHealthBarView>() ?? instance.AddComponent<ArmyHealthBarView>();
            view.ConfigureSyntyBar(fillColor);
            return view;
        }

        private static ArmyHealthBarView CreateFallbackBar(
            Transform parent,
            string name,
            Color fillColor,
            string sideLabel)
        {
            var barRoot = new GameObject(name, typeof(RectTransform));
            barRoot.transform.SetParent(parent, false);

            var barRect = barRoot.GetComponent<RectTransform>();
            barRect.anchorMin = Vector2.zero;
            barRect.anchorMax = Vector2.one;
            barRect.offsetMin = Vector2.zero;
            barRect.offsetMax = Vector2.zero;

            var layoutElement = barRoot.AddComponent<LayoutElement>();
            layoutElement.minHeight = 22f;
            layoutElement.preferredHeight = 28f;
            layoutElement.flexibleWidth = 1f;

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

            CombatHudChromeBuilder.AddCheckpointNotches(barRoot.transform);
            CombatHudChromeBuilder.AddSideLabel(barRoot.transform, sideLabel, alignLeft: name.Contains("Player"));

            var view = barRoot.AddComponent<ArmyHealthBarView>();
            view.BindFillImage(fillImage);
            return view;
        }
    }
}
