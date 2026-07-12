using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Combat
{
    /// <summary>THE game-wide grimdark UI kit (M6): the FIGHT banner's language (smoky
    /// dark bands, bone lettering, warm accents) applied to every screen — main menu,
    /// pause/options, run HUD, front report, run end, battle report — so the whole game
    /// reads as one kit instead of mixed sci-fi/theme leftovers.
    /// Deliberately NOT renamed/moved out of the Combat namespace: the kit was born in
    /// the fight loop, a dozen combat call sites already use this name, and a rename or
    /// alias buys no behavior — laziest option that reads clean (see ponytail rules).</summary>
    public static class CombatGrimdarkSkin
    {
        public static readonly Color Bone = new(0.92f, 0.87f, 0.74f, 1f);
        public static readonly Color BodyText = new(0.82f, 0.78f, 0.70f, 1f);
        public static readonly Color BandDark = new(0.055f, 0.048f, 0.04f, 0.88f);
        public static readonly Color CardBody = new(0.075f, 0.066f, 0.055f, 0.96f);
        public static readonly Color ButtonLeather = new(0.17f, 0.135f, 0.10f, 0.98f);
        public static readonly Color VictoryGold = new(0.9f, 0.78f, 0.5f, 1f);
        public static readonly Color DefeatRed = new(0.78f, 0.30f, 0.24f, 1f);

        public static void StyleTitle(TMP_Text text, float characterSpacing = 8f)
        {
            if (text == null)
                return;

            text.color = Bone;
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = characterSpacing;
        }

        public static void StyleBody(TMP_Text text)
        {
            if (text != null)
                text.color = BodyText;
        }

        public static void StyleButton(Button button)
        {
            if (button == null)
                return;

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = null;
                image.color = ButtonLeather;
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.2f, 1.1f, 1f);
            colors.pressedColor = new Color(0.75f, 0.72f, 0.68f, 1f);
            colors.selectedColor = new Color(1.35f, 1.28f, 1.05f, 1f);
            button.colors = colors;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.color = Bone;
        }

        /// <summary>Kit typography sweep for a scene-authored panel: big bold labels read
        /// as titles (bone + letter-spacing), everything else outside buttons as body text.
        /// Button labels are left to <see cref="StyleButton"/>.</summary>
        public static void StylePanelText(GameObject panelRoot, float titleFontSize = 30f)
        {
            if (panelRoot == null)
                return;

            foreach (var label in panelRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.GetComponentInParent<Button>(true) != null)
                    continue;

                if (label.fontSize >= titleFontSize)
                    StyleTitle(label);
                else
                    StyleBody(label);
            }
        }

        /// <summary>Flatten a single panel/frame image into the kit's smoky card surface
        /// (for scene-authored cards where a blanket <see cref="StyleCard"/> would also
        /// eat sliders or dim overlays).</summary>
        public static void StyleFrame(Image image)
        {
            if (image == null)
                return;

            image.sprite = null;
            image.color = CardBody;
        }

        /// <summary>Kit treatment for sliders (options volume rows): leather track,
        /// bone fill/handle. Finds parts through the Slider's own references plus the
        /// conventional "Background" child.</summary>
        public static void StyleSlider(Slider slider)
        {
            if (slider == null)
                return;

            var background = slider.transform.Find("Background");
            var backgroundImage = background != null ? background.GetComponent<Image>() : null;
            if (backgroundImage != null)
            {
                backgroundImage.sprite = null;
                backgroundImage.color = ButtonLeather;
            }

            var fillImage = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
            if (fillImage != null)
            {
                fillImage.sprite = null;
                fillImage.color = Bone;
            }

            var handleImage = slider.handleRect != null ? slider.handleRect.GetComponent<Image>() : null;
            if (handleImage != null)
            {
                handleImage.sprite = null;
                handleImage.color = Bone;
            }
        }

        /// <summary>Insert a full-width dark band behind a vertical anchor slice —
        /// the same visual element as the FIGHT banner's backdrop.</summary>
        public static void AddBand(Transform parent, float anchorMinY, float anchorMaxY, string name = "GrimdarkBand")
        {
            if (parent == null || parent.Find(name) != null)
                return;

            var band = new GameObject(name, typeof(RectTransform), typeof(Image));
            band.transform.SetParent(parent, false);
            band.transform.SetAsFirstSibling();
            var rect = band.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, anchorMinY);
            rect.anchorMax = new Vector2(1f, anchorMaxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = band.GetComponent<Image>();
            image.color = BandDark;
            image.raycastTarget = false;
        }

        /// <summary>Flatten a themed/sci-fi card into the grimdark kit: every backdrop
        /// image becomes smoky flat dark; buttons get the leather treatment.</summary>
        public static void StyleCard(GameObject panelRoot)
        {
            if (panelRoot == null)
                return;

            foreach (var image in panelRoot.GetComponentsInChildren<Image>(true))
            {
                if (image.GetComponent<Button>() != null)
                    continue;

                image.sprite = null;
                image.color = CardBody;
            }

            foreach (var button in panelRoot.GetComponentsInChildren<Button>(true))
                StyleButton(button);
        }
    }
}
