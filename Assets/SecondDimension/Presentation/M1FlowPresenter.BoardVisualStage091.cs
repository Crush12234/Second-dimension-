using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        static void StyleNeutralBoardSurface091(Image panel, float opacity = 0.88f)
        {
            if (panel == null) return;
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.EtchedGlass);
            panel.color = new Color(0.76f, 0.82f, 0.94f, opacity);
        }

        /// <summary>
        /// The revealed card is an illustration with a compact reading column,
        /// not a category-colored spreadsheet. Existing controls and values are
        /// reparented intact; this helper has no gameplay callback or state.
        /// </summary>
        static void ArrangeIllustratedBoardCard091(RectTransform face,
            string artNamePrefix, bool expandFromDraftSlot)
        {
            if (face == null) return;
            var art = face.GetComponentsInChildren<Image>(true).FirstOrDefault(value =>
                value.name.StartsWith(artNamePrefix, StringComparison.Ordinal));
            if (art == null) return;
            var oldLayout = face.GetComponent<VerticalLayoutGroup>();
            if (oldLayout != null) oldLayout.enabled = false;
            StyleNeutralBoardSurface091(face.GetComponent<Image>());
            var children = Enumerable.Range(0, face.childCount)
                .Select(index => face.GetChild(index)).ToArray();
            if (expandFromDraftSlot)
            {
                face.anchorMin = new Vector2(-0.30f, 0f);
                face.anchorMax = new Vector2(1.30f, 1f);
                face.offsetMin = Vector2.zero;
                face.offsetMax = Vector2.zero;
            }
            var illustration = new GameObject("Board Card Illustration Window 091",
                typeof(RectTransform)).GetComponent<RectTransform>();
            illustration.SetParent(face, false);
            illustration.gameObject.AddComponent<RectMask2D>();
            SetAnchors074(illustration, new Vector2(0.018f, 0.025f),
                new Vector2(0.485f, 0.975f));
            art.transform.SetParent(illustration, false);
            Stretch(art.rectTransform);
            art.preserveAspect = true;
            art.color = Color.white;
            art.raycastTarget = false;
            if (art.sprite != null && art.sprite.rect.height > 0f)
            {
                var artAspect = art.gameObject.GetComponent<AspectRatioFitter>();
                if (artAspect == null) artAspect = art.gameObject.AddComponent<AspectRatioFitter>();
                artAspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                artAspect.aspectRatio = art.sprite.rect.width / art.sprite.rect.height;
            }

            var copy = new GameObject("Board Card Reading Column 091",
                typeof(RectTransform)).GetComponent<RectTransform>();
            copy.SetParent(face, false);
            SetAnchors074(copy, new Vector2(0.515f, 0.06f),
                new Vector2(0.97f, 0.94f));
            RuntimeUi.AddVerticalLayout(copy, new RectOffset(4, 4, 8, 8),
                10f, TextAnchor.MiddleLeft);
            foreach (var child in children)
            {
                if (child == art.transform) continue;
                var element = child.GetComponent<LayoutElement>();
                if (element != null && element.ignoreLayout) continue;
                child.SetParent(copy, false);
                var text = child.GetComponent<Text>();
                if (text != null)
                {
                    text.alignment = TextAnchor.MiddleLeft;
                    ConfigureAuthoredCompactText076(text, 18,
                        text.name.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0 ? 30 : 24);
                    var title = text.name.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0;
                    var lines = (text.text ?? string.Empty).Split('\n')
                        .Sum(line => Math.Max(1, Mathf.CeilToInt(line.Length / 40f)));
                    RuntimeUi.SetLayout(text, preferredHeight: title ? 66f :
                        Mathf.Clamp(lines * 24f + 8f, 38f, 180f));
                }
                var button = child.GetComponent<Button>();
                if (button != null)
                {
                    RuntimeUi.SetLayout(button, preferredHeight: 64f);
                    ConfigureAuthoredCompactText076(button.GetComponentInChildren<Text>(true), 18, 25);
                }
                if (child.name == "Expedition Companion Story Beat 076")
                    ArrangeBoardSpeakerStrip091(child as RectTransform);
            }
        }

        // A full-width identity header and quote replace the old fixed 340px
        // dialogue indent. Both roster companions and authored story identities
        // keep their original artwork and words in the narrower reading column.
        static void ArrangeBoardSpeakerStrip091(RectTransform panel)
        {
            if (panel == null) return;
            var quote = panel.GetComponentsInChildren<Text>(true).FirstOrDefault(value =>
                value.name.StartsWith("Expedition Companion Story Beat Text 076", StringComparison.Ordinal));
            if (quote == null) return;
            var identity = panel.Cast<Transform>().FirstOrDefault(value =>
                value.name == "Expedition Companion Identity Chip 078" ||
                value.name.StartsWith("Chapter Two Story Identity ", StringComparison.Ordinal));
            RuntimeUi.SetLayout(panel, preferredHeight: identity == null ? 112f : 206f)
                .minHeight = identity == null ? 112f : 206f;
            StyleNeutralBoardSurface091(panel.GetComponent<Image>(), 0.68f);
            SetAnchors074(quote.rectTransform, Vector2.zero, Vector2.one);
            quote.rectTransform.offsetMin = new Vector2(12f, 10f);
            quote.rectTransform.offsetMax = new Vector2(-12f, identity == null ? -10f : -98f);
            quote.alignment = TextAnchor.MiddleLeft;
            ConfigureAuthoredCompactText076(quote, 18, 22);
            quote.horizontalOverflow = HorizontalWrapMode.Wrap;
            quote.verticalOverflow = VerticalWrapMode.Truncate;
            quote.lineSpacing = 1f;
            if (identity == null) return;

            var identityRect = identity as RectTransform;
            SetAnchors074(identityRect, new Vector2(0f, 1f), Vector2.one);
            identityRect.offsetMin = new Vector2(10f, -90f);
            identityRect.offsetMax = new Vector2(-10f, -8f);
            StyleNeutralBoardSurface091(identity.GetComponent<Image>(), 0.65f);
            var portrait = identity.Cast<Transform>().FirstOrDefault(value =>
                value.name.StartsWith("Portrait Frame ", StringComparison.Ordinal) ||
                value.name.StartsWith("Chapter Two Story Portrait ", StringComparison.Ordinal))
                as RectTransform;
            if (portrait != null)
            {
                SetAnchors074(portrait, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
                portrait.pivot = new Vector2(0f, 0.5f);
                portrait.sizeDelta = new Vector2(58f, 58f);
                portrait.anchoredPosition = new Vector2(5f, 0f);
            }
            var name = identity.GetComponentsInChildren<Text>(true).FirstOrDefault(value =>
                value.transform.parent == identity);
            if (name != null)
            {
                // Legacy story chips stack one role word on each of five lines.
                // Keep every word, but use the width of the new header.
                var identityLines = (name.text ?? string.Empty).Split('\n');
                if (identityLines.Length > 2)
                    name.text = identityLines[0] + "\n" +
                                string.Join(" ", identityLines.Skip(1));
                SetAnchors074(name.rectTransform, Vector2.zero, Vector2.one);
                name.rectTransform.offsetMin = new Vector2(74f, 5f);
                name.rectTransform.offsetMax = new Vector2(-10f, -5f);
                name.alignment = TextAnchor.MiddleLeft;
                ConfigureAuthoredCompactText076(name, 18, 23);
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                name.verticalOverflow = VerticalWrapMode.Truncate;
                name.lineSpacing = 1f;
            }
        }

        static string BoardRoomIllustrationResource091(string roomKind)
        {
            var category = (roomKind ?? string.Empty).ToUpperInvariant();
            switch (category)
            {
                case "TREASURE": category = "CHEST"; break;
                case "BLESSING": case "SKILL": case "XP": category = "BUFF"; break;
                case "MONSTER": category = "BATTLE"; break;
                case "FATE": category = "CHANCE"; break;
                case "CAMPFIRE": category = "CAMP"; break;
                case "RECRUIT": break;
                default: category = "STORY"; break;
            }
            return "SecondDimension/Art/Board086/CardFaces/CARD_FACE_" + category + "_089";
        }
    }
}
