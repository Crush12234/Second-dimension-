using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    internal static class RuntimeUi
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(2796f, 1290f);
        public const float MinimumTouchPixels = 132f;
        public const float PrimaryTouchPixels = 156f;
        public const int BodyFontPixels = 54;
        public const int SmallBodyFontPixels = 48;
        public const int HeadingFontPixels = 78;
        public const int CriticalFontPixels = 66;

        public static readonly Color Background = FromHex(0x050914FF);
        public static readonly Color Panel = FromHex(0x101827F2);
        // Normal pages retain a strong dark reading surface while allowing the
        // authored backdrop to remain materially visible behind the controls.
        public static readonly Color PanelOverlay = FromHex(0x101827A6);
        public static readonly Color PanelRaised = FromHex(0x18243AF7);
        public static readonly Color ButtonNormal = FromHex(0x1C3554FF);
        public static readonly Color ButtonHighlighted = FromHex(0x28547FFF);
        public static readonly Color ButtonPressed = FromHex(0x14283FFF);
        public static readonly Color Accent = FromHex(0xD1AE66FF);
        public static readonly Color Text = FromHex(0xEDF3FFFF);
        public static readonly Color MutedText = FromHex(0xB2BED0FF);
        public static readonly Color Positive = FromHex(0x79D49CFF);
        public static readonly Color Warning = FromHex(0xF1C56EFF);
        public static readonly Color Error = FromHex(0xF08A8AFF);

        public static Font Font => M1PremiumUi.UiFont;
        public static Font DisplayFont => M1PremiumUi.DisplayFont;

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        public static Canvas CreateCanvas(string name)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform AddStretchRect(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static RectTransform AddSafeArea(Transform parent)
        {
            var rect = AddStretchRect(parent, "Safe Area");
            rect.gameObject.AddComponent<SafeAreaFitter>();
            return rect;
        }

        public static Image AddPanel(Transform parent, string name, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text AddText(
            Transform parent,
            string name,
            string value,
            int fontSize = BodyFontPixels,
            TextAnchor alignment = TextAnchor.MiddleLeft,
            Color? color = null,
            FontStyle style = FontStyle.Normal)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color ?? Text;
            text.text = value ?? string.Empty;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false;
            text.resizeTextForBestFit = false;
            return text;
        }

        public static Button AddButton(
            Transform parent,
            string name,
            string label,
            Action onClick,
            float preferredHeight = PrimaryTouchPixels,
            Color? normalColor = null)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            // Selectable color tint multiplies the target graphic color. Keep the
            // base white so authored theme colors are rendered exactly once.
            image.color = Color.white;

            var button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = normalColor ?? ButtonNormal;
            colors.highlightedColor = ButtonHighlighted;
            colors.selectedColor = ButtonHighlighted;
            colors.pressedColor = ButtonPressed;
            colors.disabledColor = FromHex(0x273040B2);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var layout = gameObject.GetComponent<LayoutElement>();
            layout.minHeight = MinimumTouchPixels;
            layout.preferredHeight = Mathf.Max(MinimumTouchPixels, preferredHeight);
            layout.minWidth = MinimumTouchPixels;
            gameObject.AddComponent<AdaptiveTouchTarget164>();

            var labelText = AddText(gameObject.transform, "Label", label, BodyFontPixels, TextAnchor.MiddleCenter, Text, FontStyle.Bold);
            var labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(24f, 12f);
            labelRect.offsetMax = new Vector2(-24f, -12f);
            labelText.raycastTarget = false;
            var requestedColor = normalColor ?? ButtonNormal;
            var role = ColorsApproximatelyEqual(requestedColor, Error)
                ? M1PremiumUi.ButtonRole.Destructive
                : ColorsApproximatelyEqual(requestedColor, Warning)
                    ? M1PremiumUi.ButtonRole.Warning
                    : ColorsApproximatelyEqual(requestedColor, Accent)
                        ? M1PremiumUi.ButtonRole.Selected
                        : M1PremiumUi.ButtonRole.Primary;
            M1PremiumUi.StyleButton(button, role);
            return button;
        }

        public static InputField AddInputField(Transform parent, string name, string placeholder, int characterLimit)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = PanelRaised;

            var layout = gameObject.GetComponent<LayoutElement>();
            layout.minHeight = MinimumTouchPixels;
            layout.preferredHeight = PrimaryTouchPixels;

            var inputText = AddText(gameObject.transform, "Text", string.Empty, BodyFontPixels, TextAnchor.MiddleLeft, Text);
            inputText.rectTransform.anchorMin = Vector2.zero;
            inputText.rectTransform.anchorMax = Vector2.one;
            inputText.rectTransform.offsetMin = new Vector2(36f, 12f);
            inputText.rectTransform.offsetMax = new Vector2(-36f, -12f);

            var placeholderText = AddText(gameObject.transform, "Placeholder", placeholder, BodyFontPixels, TextAnchor.MiddleLeft, MutedText);
            placeholderText.fontStyle = FontStyle.Italic;
            placeholderText.rectTransform.anchorMin = Vector2.zero;
            placeholderText.rectTransform.anchorMax = Vector2.one;
            placeholderText.rectTransform.offsetMin = new Vector2(36f, 12f);
            placeholderText.rectTransform.offsetMax = new Vector2(-36f, -12f);

            var input = gameObject.GetComponent<InputField>();
            input.textComponent = inputText;
            input.placeholder = placeholderText;
            input.characterLimit = characterLimit;
            input.lineType = InputField.LineType.SingleLine;
            input.contentType = InputField.ContentType.Standard;
            input.targetGraphic = image;
            M1PremiumUi.StyleInput(input);
            return input;
        }

        public static VerticalLayoutGroup AddVerticalLayout(Transform parent, RectOffset padding, float spacing, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(Transform parent, RectOffset padding, float spacing, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var layout = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static LayoutElement SetLayout(Component component, float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f, float flexibleHeight = -1f)
        {
            var layout = component.GetComponent<LayoutElement>();
            if (layout == null) layout = component.gameObject.AddComponent<LayoutElement>();
            if (preferredWidth >= 0f) layout.preferredWidth = preferredWidth;
            if (preferredHeight >= 0f) layout.preferredHeight = preferredHeight;
            if (flexibleWidth >= 0f) layout.flexibleWidth = flexibleWidth;
            if (flexibleHeight >= 0f) layout.flexibleHeight = flexibleHeight;
            return layout;
        }

        public static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }

        private static Color FromHex(uint rgba)
        {
            return new Color(
                ((rgba >> 24) & 0xFF) / 255f,
                ((rgba >> 16) & 0xFF) / 255f,
                ((rgba >> 8) & 0xFF) / 255f,
                (rgba & 0xFF) / 255f);
        }

        private static bool ColorsApproximatelyEqual(Color left, Color right)
        {
            return Mathf.Abs(left.r - right.r) < 0.01f &&
                   Mathf.Abs(left.g - right.g) < 0.01f &&
                   Mathf.Abs(left.b - right.b) < 0.01f &&
                   Mathf.Abs(left.a - right.a) < 0.01f;
        }
    }
}
