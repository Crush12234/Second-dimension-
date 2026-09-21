using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Shared presentation-only helpers for the phone-readable Guild menus. These
    /// prefer the same standing sprites used by combat, with explicitly compatible
    /// full-body cutouts for unauthored procedural applicants. Menu art never alters
    /// a recruit's identity, equipment, or combat artwork authority.
    /// </summary>
    public sealed partial class M1FlowPresenter
    {
        private static readonly Dictionary<string, Sprite> MenuTileSprites090 =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);

        private static void DecorateLivingGuildFacility090(
            Button button,
            string artworkResource,
            string category,
            bool storyTarget)
        {
            if (button == null) return;

            var artClip = new GameObject(
                "Facility Artwork Window 090",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask));
            artClip.transform.SetParent(button.transform, false);
            var artClipRect = artClip.GetComponent<RectTransform>();
            AnchorMenuRect090(artClipRect, new Rect(0.018f, 0.325f, 0.964f, 0.655f));
            ConfigureMenuArtworkMask091(artClip.GetComponent<Image>(), false);
            artClip.transform.SetSiblingIndex(0);

            var sprite = ResolveMenuTileSprite090(artworkResource);
            var artObject = new GameObject(
                "Facility Illustrated Scene 090",
                typeof(RectTransform),
                typeof(Image));
            artObject.transform.SetParent(artClip.transform, false);
            var artRect = artObject.GetComponent<RectTransform>();
            AnchorMenuRect090(artRect, new Rect(-0.015f, -0.015f, 1.030f, 1.030f));
            var art = artObject.GetComponent<Image>();
            art.raycastTarget = false;
            art.sprite = sprite;
            art.type = Image.Type.Simple;
            art.preserveAspect = true;
            if (sprite != null)
            {
                var fit = artObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            }
            art.color = sprite == null
                ? new Color(0.08f, 0.12f, 0.16f, 1f)
                : new Color(0.95f, 0.96f, 1f, 1f);

            var vignette = RuntimeUi.AddPanel(
                artClip.transform,
                "Facility Artwork Readability Vignette 090",
                new Color(0.005f, 0.010f, 0.018f, 0.24f));
            Stretch(vignette.rectTransform);
            vignette.raycastTarget = false;

            var caption = RuntimeUi.AddPanel(
                button.transform,
                "Facility Caption Plate 090",
                storyTarget
                    ? new Color(0.20f, 0.115f, 0.025f, 0.97f)
                    : new Color(0.010f, 0.025f, 0.042f, 0.97f));
            AnchorMenuRect090(caption.rectTransform, new Rect(0.018f, 0.018f, 0.964f, 0.310f));
            caption.raycastTarget = false;
            caption.sprite = M1PremiumUi.RoundedMask091;
            caption.type = Image.Type.Sliced;
            caption.transform.SetSiblingIndex(1);

            var tagBacking = RuntimeUi.AddPanel(
                button.transform,
                "Facility Category Tag Backing 090",
                storyTarget
                    ? new Color(0.96f, 0.76f, 0.30f, 0.96f)
                    : new Color(0.035f, 0.13f, 0.19f, 0.94f));
            AnchorMenuRect090(tagBacking.rectTransform, new Rect(0.055f, 0.865f, 0.36f, 0.090f));
            tagBacking.raycastTarget = false;
            tagBacking.sprite = M1PremiumUi.RoundedMask091;
            tagBacking.type = Image.Type.Sliced;
            var tag = RuntimeUi.AddText(
                tagBacking.transform,
                "Facility Category Tag 090",
                string.IsNullOrWhiteSpace(category) ? "GUILD" : category.ToUpperInvariant(),
                14,
                TextAnchor.MiddleCenter,
                storyTarget ? new Color(0.10f, 0.055f, 0.015f, 1f) : RuntimeUi.Text,
                FontStyle.Bold);
            Stretch(tag.rectTransform);
            tag.resizeTextForBestFit = true;
            tag.resizeTextMinSize = 10;
            tag.resizeTextMaxSize = 14;
            tag.raycastTarget = false;

            if (storyTarget)
            {
                var nextBacking = RuntimeUi.AddPanel(
                    button.transform,
                    "Facility Story Target Badge Backing 090",
                    new Color(0.96f, 0.76f, 0.30f, 0.96f));
                AnchorMenuRect090(nextBacking.rectTransform, new Rect(0.765f, 0.865f, 0.18f, 0.090f));
                nextBacking.raycastTarget = false;
                nextBacking.sprite = M1PremiumUi.RoundedMask091;
                nextBacking.type = Image.Type.Sliced;
                var next = RuntimeUi.AddText(
                    nextBacking.transform,
                    "Facility Story Target Badge 090",
                    "NEXT",
                    14,
                    TextAnchor.MiddleCenter,
                    new Color(0.10f, 0.055f, 0.015f, 1f),
                    FontStyle.Bold);
                Stretch(next.rectTransform);
                next.resizeTextForBestFit = true;
                next.resizeTextMinSize = 10;
                next.resizeTextMaxSize = 14;
                next.raycastTarget = false;
            }

            var motion = button.gameObject.GetComponent<M1MenuCardMotion090>();
            if (motion == null) motion = button.gameObject.AddComponent<M1MenuCardMotion090>();
            motion.Configure090(storyTarget);

            // The original RuntimeUi label remains the first Text found by existing
            // automation and keeps its exact player-facing wording.
            var label = FindMenuButtonLabel091(button);
            if (label != null) label.transform.SetSiblingIndex(2);
        }

        private static Sprite ResolveMenuTileSprite090(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath)) return null;
            if (MenuTileSprites090.TryGetValue(resourcePath, out var cached) && cached != null)
                return cached;

            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f,
                        0u,
                        SpriteMeshType.FullRect);
                    sprite.name = "Menu Tile Sprite 090 " + texture.name;
                }
            }
            if (sprite != null) MenuTileSprites090[resourcePath] = sprite;
            return sprite;
        }

        private static void BuildMenuBackdrop090(
            Transform parent,
            string objectName,
            string artworkResource,
            float shadeAlpha)
        {
            if (parent == null) return;
            var sprite = ResolveMenuTileSprite090(artworkResource);
            if (sprite == null) return;

            var background = RuntimeUi.AddPanel(parent, objectName, Color.white);
            Stretch(background.rectTransform);
            background.sprite = sprite;
            background.type = Image.Type.Simple;
            background.preserveAspect = false;
            background.raycastTarget = false;
            background.transform.SetSiblingIndex(0);

            var shade = RuntimeUi.AddPanel(
                parent,
                objectName + " Readability Shade",
                new Color(0.004f, 0.010f, 0.020f, Mathf.Clamp01(shadeAlpha)));
            Stretch(shade.rectTransform);
            shade.raycastTarget = false;
            shade.transform.SetSiblingIndex(1);
        }

        private static void AddMenuApplicantStandeeToButton090(
            Button button,
            GuildCity017D.GuildCityApplicantView017D applicant)
        {
            if (button == null || applicant == null) return;
            var label = FindMenuButtonLabel091(button);
            if (label != null)
            {
                label.rectTransform.anchorMin = new Vector2(0.35f, 0.06f);
                label.rectTransform.anchorMax = new Vector2(0.95f, 0.94f);
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
                label.alignment = TextAnchor.MiddleLeft;
                ConfigureAuthoredCompactText076(label, 16, 27);
                label.verticalOverflow = VerticalWrapMode.Truncate;
            }

            var frame = RuntimeUi.AddPanel(
                button.transform,
                "Applicant Standee Thumbnail 090 " + applicant.RecruitId,
                RuntimeUi.Accent);
            frame.raycastTarget = false;
            AnchorMenuRect090(frame.rectTransform, new Rect(0.035f, 0.08f, 0.275f, 0.84f));
            PopulateMenuStandeeFrame091(
                frame,
                applicant.RecruitId,
                applicant.VisualSeed,
                applicant.RaceId,
                applicant.PortraitAuthorityId,
                applicant.DisplayName,
                null,
                applicant.ClassTendencyId,
                applicant.EquipmentSummary);
            label?.transform.SetAsLastSibling();
        }

        private static void AddMenuStandeeToButton090(
            Button button,
            M1RecruitLoadoutView recruit,
            bool large)
        {
            if (button == null || recruit == null) return;
            var label = FindMenuButtonLabel091(button);
            if (label != null)
            {
                label.rectTransform.anchorMin = large
                    ? new Vector2(0.04f, 0.025f)
                    : new Vector2(0.34f, 0.04f);
                label.rectTransform.anchorMax = large
                    ? new Vector2(0.96f, 0.33f)
                    : new Vector2(0.97f, 0.96f);
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
                label.alignment = large ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            }

            var frame = RuntimeUi.AddPanel(
                button.transform,
                "Portrait Frame " + recruit.RecruitId,
                RuntimeUi.Accent);
            frame.raycastTarget = false;
            frame.rectTransform.anchorMin = large
                ? new Vector2(0.05f, 0.35f)
                : new Vector2(0.035f, 0.10f);
            frame.rectTransform.anchorMax = large
                ? new Vector2(0.95f, 0.96f)
                : new Vector2(0.30f, 0.90f);
            frame.rectTransform.offsetMin = Vector2.zero;
            frame.rectTransform.offsetMax = Vector2.zero;
            PopulateMenuStandeeFrame091(
                frame,
                recruit.RecruitId,
                recruit.VisualSeed,
                recruit.RaceId,
                recruit.PortraitAuthorityId,
                recruit.DisplayName,
                null,
                recruit.ObservedClass,
                string.Join(" ", (recruit.Slots ?? Array.Empty<M1EquipmentSlotView>())
                    .Where(slot => slot != null)
                    .Select(slot => slot.EquippedItemName)));
        }

        private static void PopulateMenuStandeeFrame090(
            Image frame,
            string recruitId,
            string visualSeed,
            string raceId,
            string portraitAuthorityId,
            string displayName,
            string identityLabel,
            string roleIdentity)
        {
            PopulateMenuStandeeFrame091(frame, recruitId, visualSeed, raceId,
                portraitAuthorityId, displayName, identityLabel, roleIdentity);
        }

        private static void PopulateMenuStandeeFrame091(
            Image frame,
            string recruitId,
            string visualSeed,
            string raceId,
            string portraitAuthorityId,
            string displayName,
            string identityLabel,
            string roleIdentity,
            string equipmentIdentity = null,
            bool sceneIntegrated = false)
        {
            if (frame == null) return;
            frame.raycastTarget = false;
            if (!sceneIntegrated) M1PremiumUi.StylePortraitFrame(frame, selected: true);
            else { frame.sprite = null; frame.color = Color.clear; }

            var hasIdentity = !string.IsNullOrWhiteSpace(identityLabel);
            var backing = RuntimeUi.AddPanel(
                frame.transform,
                "Standee Backing 090",
                sceneIntegrated ? Color.white : new Color(0.010f, 0.026f, 0.043f, 1f));
            backing.raycastTarget = false;
            backing.rectTransform.anchorMin = hasIdentity ? new Vector2(0f, 0.15f) : Vector2.zero;
            backing.rectTransform.anchorMax = Vector2.one;
            backing.rectTransform.offsetMin = new Vector2(7f, 7f);
            backing.rectTransform.offsetMax = new Vector2(-7f, -7f);

            ConfigureMenuArtworkMask091(backing, !sceneIntegrated);

            if (M1VisualAssets.TryResolveMenuStandee091(
                    recruitId,
                    visualSeed,
                    raceId,
                    portraitAuthorityId,
                    roleIdentity,
                    equipmentIdentity,
                    out var sprite,
                    out _) && sprite != null)
            {
                var artwork = RuntimeUi.AddPanel(
                    backing.transform,
                    "Standing Hero Sprite 090",
                    Color.white);
                artwork.sprite = sprite;
                artwork.type = Image.Type.Simple;
                artwork.preserveAspect = true;
                artwork.raycastTarget = false;
                // Every hero shares one padded, aspect-preserving display window.
                // Sprite authority handles transparent source padding; no stretching
                // or arbitrary per-character scale is applied by this screen.
                AnchorMenuRect090(artwork.rectTransform, new Rect(0.07f, 0.04f, 0.86f, 0.92f));
                artwork.rectTransform.pivot = new Vector2(0.5f, 0f);
            }
            else
            {
                var silhouette = RuntimeUi.AddText(
                    backing.transform,
                    "Standing Hero Fallback 090",
                    "◆\n" + M1VisualAssets.Initials(displayName),
                    64,
                    TextAnchor.MiddleCenter,
                    M1VisualAssets.FallbackPortraitColor(raceId, visualSeed, recruitId),
                    FontStyle.Bold);
                AnchorMenuRect090(silhouette.rectTransform, new Rect(0.10f, 0.12f, 0.80f, 0.76f));
                silhouette.resizeTextForBestFit = true;
                silhouette.resizeTextMinSize = 24;
                silhouette.resizeTextMaxSize = 64;
                silhouette.raycastTarget = false;
            }

            if (!hasIdentity) return;
            var identityPlate = RuntimeUi.AddPanel(
                frame.transform,
                "Standee Identity Plate 090",
                sceneIntegrated ? Color.clear : new Color(0.010f, 0.025f, 0.042f, 0.98f));
            identityPlate.raycastTarget = false;
            identityPlate.rectTransform.anchorMin = Vector2.zero;
            identityPlate.rectTransform.anchorMax = new Vector2(1f, 0.15f);
            identityPlate.rectTransform.offsetMin = new Vector2(7f, 7f);
            identityPlate.rectTransform.offsetMax = new Vector2(-7f, -2f);
            identityPlate.sprite = M1PremiumUi.RoundedMask091;
            identityPlate.type = Image.Type.Sliced;

            var identity = RuntimeUi.AddText(
                identityPlate.transform,
                "Standee Identity 090",
                identityLabel,
                26,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorMenuRect090(identity.rectTransform, new Rect(0.04f, 0.05f, 0.92f, 0.90f));
            identity.resizeTextForBestFit = true;
            identity.resizeTextMinSize = 15;
            identity.resizeTextMaxSize = 26;
            identity.raycastTarget = false;
        }

        private static void AnchorMenuRect090(RectTransform rect, Rect anchors)
        {
            if (rect == null) return;
            rect.anchorMin = anchors.min;
            rect.anchorMax = anchors.max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Text FindMenuButtonLabel091(Button button)
        {
            if (button == null) return null;
            // Responsive/accessibility setup appends metadata to the Label name.
            // Match only the direct label, never a nested art caption or role badge.
            foreach (Transform child in button.transform)
                if (child.name == "Label" || child.name.StartsWith("Label [", StringComparison.Ordinal))
                    return child.GetComponent<Text>();
            return null;
        }

        private static void ConfigureMenuArtworkMask091(Image image, bool showBacking)
        {
            image.sprite = M1PremiumUi.RoundedMask091;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            var mask = image.GetComponent<Mask>();
            if (mask == null) mask = image.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = showBacking;
        }
    }

    /// <summary>Small unscaled hover/focus lift; it owns no button action or state.</summary>
    internal sealed class M1MenuCardMotion090 : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private bool _hovered;
        private bool _focused;
        private bool _featured;
        private bool _pressed;
        private Button _button;
        private Vector3 _homeScale = Vector3.one;

        public void Configure090(bool featured)
        {
            _featured = featured;
            _homeScale = transform.localScale;
            _button = GetComponent<Button>();
        }

        private void Update()
        {
            var enabled = _button == null || _button.IsInteractable();
            var active = enabled && (_hovered || _focused);
            var lift = enabled && _pressed ? 0.984f : active ? 1.018f : _featured ? 1.004f : 1f;
            var target = _homeScale * lift;
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                target,
                1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        }

        private void OnDisable()
        {
            transform.localScale = _homeScale;
            _hovered = false;
            _focused = false;
            _pressed = false;
        }

        public void OnPointerEnter(PointerEventData eventData) => _hovered = true;
        public void OnPointerExit(PointerEventData eventData) { _hovered = false; _pressed = false; }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left &&
                (_button == null || _button.IsInteractable())) _pressed = true;
        }
        public void OnPointerUp(PointerEventData eventData) => _pressed = false;
        public void OnSelect(BaseEventData eventData) => _focused = true;
        public void OnDeselect(BaseEventData eventData) { _focused = false; _pressed = false; }
    }
}
