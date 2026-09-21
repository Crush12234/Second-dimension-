using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private static Sprite _kiriProloguePortraitCrop076;

        private static readonly Rect[] GuildCharterCardRegionValues074 =
        {
            new Rect(0.055f, 0.820f, 0.890f, 0.100f), // Role.
            new Rect(0.055f, 0.650f, 0.890f, 0.160f), // Crisis.
            new Rect(0.055f, 0.505f, 0.890f, 0.135f), // Kiri's charge.
            new Rect(0.055f, 0.360f, 0.890f, 0.130f), // First objective.
            new Rect(0.055f, 0.310f, 0.890f, 0.045f), // Signature label.
            new Rect(0.055f, 0.215f, 0.890f, 0.090f), // Guildmaster name.
            new Rect(0.055f, 0.105f, 0.890f, 0.100f), // Enter Hall action.
            new Rect(0.055f, 0.020f, 0.890f, 0.075f)  // Optional status.
        };

        private static readonly IReadOnlyList<Rect> GuildCharterCardRegionsReadOnly074 =
            Array.AsReadOnly(GuildCharterCardRegionValues074);

        public static IReadOnlyList<Rect> GuildCharterCardRegionsForVerification074 =>
            GuildCharterCardRegionsReadOnly074;

        private void BuildGuildCharterPrologue074()
        {
            if (string.IsNullOrWhiteSpace(_guildmasterDraft))
                _guildmasterDraft = "Skyhome Guildmaster";

            RuntimeUi.ClearChildren(_screenRoot);
            _activePage = null;
            _activeContent = null;
            _activeScroll = null;

            var root = RuntimeUi.AddPanel(
                _screenRoot,
                "Guild Charter Prologue 074",
                new Color(0.018f, 0.026f, 0.034f, 1f));
            Stretch(root.rectTransform);

            var artViewport = RuntimeUi.AddPanel(
                root.transform,
                "The Bell Beneath Skyhome Art Viewport 074",
                Color.white);
            artViewport.rectTransform.anchorMin = Vector2.zero;
            artViewport.rectTransform.anchorMax = new Vector2(0.665f, 1f);
            artViewport.rectTransform.offsetMin = Vector2.zero;
            artViewport.rectTransform.offsetMax = Vector2.zero;
            artViewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var keyArt = RuntimeUi.AddPanel(
                artViewport.transform,
                "The Bell Beneath Skyhome Key Art 074",
                Color.white);
            Stretch(keyArt.rectTransform);
            keyArt.raycastTarget = false;
            keyArt.sprite = ResolveVisualSliceSprite062(FirstContractKeyArtResource062);
            keyArt.type = Image.Type.Simple;
            keyArt.preserveAspect = false;
            if (keyArt.sprite != null)
            {
                var fitter = keyArt.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = keyArt.sprite.rect.width / Mathf.Max(1f, keyArt.sprite.rect.height);
            }

            var artShade = RuntimeUi.AddPanel(
                artViewport.transform,
                "Key Art Readability Shade 074",
                new Color(0.005f, 0.01f, 0.015f, 0.16f));
            Stretch(artShade.rectTransform);
            artShade.raycastTarget = false;

            var kiriPortrait = RuntimeUi.AddPanel(
                root.transform,
                "Kiri Aetherheart Prologue Portrait 076",
                RuntimeUi.Accent);
            kiriPortrait.rectTransform.anchorMin = new Vector2(0.035f, 0.135f);
            kiriPortrait.rectTransform.anchorMax = new Vector2(0.255f, 0.485f);
            kiriPortrait.rectTransform.offsetMin = Vector2.zero;
            kiriPortrait.rectTransform.offsetMax = Vector2.zero;
            PopulatePortraitFrame(
                kiriPortrait,
                "CANON_KIRI_AETHERHEART",
                "CANON_KIRI_AETHERHEART",
                "HUMAN",
                "CANON_KIRI_AETHERHEART",
                "Kiri Aetherheart",
                "Skyhome",
                "KIRI AETHERHEART\nFOUNDER OF SKYHOME");
            ApplyKiriProloguePortraitCrop076(kiriPortrait);

            var chapter = RuntimeUi.AddPanel(
                root.transform,
                "Prologue Chapter Ribbon 074",
                new Color(0.025f, 0.045f, 0.055f, 0.94f));
            chapter.rectTransform.anchorMin = new Vector2(0.025f, 0.82f);
            chapter.rectTransform.anchorMax = new Vector2(0.62f, 0.965f);
            chapter.rectTransform.offsetMin = Vector2.zero;
            chapter.rectTransform.offsetMax = Vector2.zero;
            var chapterText = RuntimeUi.AddText(
                chapter.transform,
                "Prologue Chapter Text 074",
                "CHAPTER 1  •  THE BELL BENEATH SKYHOME\nTHE OLD GUILD IS GONE. TEN LIVES ARE STILL BELOW.",
                28,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            Stretch(chapterText.rectTransform);
            chapterText.rectTransform.offsetMin = new Vector2(26f, 8f);
            chapterText.rectTransform.offsetMax = new Vector2(-26f, -8f);
            ConfigureResponsiveText062(chapterText, 19, 30);

            var back = RuntimeUi.AddButton(
                root.transform,
                "Prologue Back 074",
                "←  TITLE",
                () =>
                {
                    _firstHourCharterRoute071 = false;
                    Navigate(M1Screen.MainMenu);
                },
                76f,
                new Color(0.025f, 0.055f, 0.07f, 0.96f));
            var backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.025f, 0.035f);
            backRect.anchorMax = new Vector2(0.18f, 0.125f);
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;
            ConfigureResponsiveText062(back.GetComponentInChildren<Text>(), 17, 24);

            var card = RuntimeUi.AddPanel(
                root.transform,
                "Emergency Charter Card 074",
                new Color(0.025f, 0.035f, 0.042f, 0.975f));
            card.rectTransform.anchorMin = new Vector2(0.64f, 0.025f);
            card.rectTransform.anchorMax = new Vector2(0.985f, 0.975f);
            card.rectTransform.offsetMin = Vector2.zero;
            card.rectTransform.offsetMax = Vector2.zero;
            M1PremiumUi.StylePanel(card, M1PremiumUi.Surface.WorldRibbon);

            var role = AddResponsiveText062(
                card.transform,
                "Prologue Role 074",
                "YOU ARE THE NEW GUILDMASTER",
                25,
                38,
                70f,
                RuntimeUi.Accent,
                FontStyle.Bold,
                TextAnchor.MiddleLeft);
            AnchorGuildCharterCard074(role.rectTransform, GuildCharterCardRegionValues074[0]);
            role.verticalOverflow = VerticalWrapMode.Truncate;

            var crisis = AddResponsiveText062(
                card.transform,
                "Prologue Crisis 074",
                "Three nights ago, an impossible bell rang beneath Skyhome. The old Guildmaster vanished. Zorin's Lantern Patrol carried the only Wayglass below the city. None returned.",
                19,
                29,
                155f,
                RuntimeUi.Text,
                FontStyle.Bold,
                TextAnchor.UpperLeft);
            AnchorGuildCharterCard074(crisis.rectTransform, GuildCharterCardRegionValues074[1]);
            crisis.verticalOverflow = VerticalWrapMode.Truncate;

            var steward = AddResponsiveText062(
                card.transform,
                "Prologue Steward 074",
                "KIRI AETHERHEART\n“The Founders must hold the gates. Take the ruined annex. Lead the volunteers. Bring Zorin's people home.”",
                18,
                27,
                125f,
                RuntimeUi.Warning,
                FontStyle.Italic,
                TextAnchor.UpperLeft);
            AnchorGuildCharterCard074(steward.rectTransform, GuildCharterCardRegionValues074[2]);
            steward.verticalOverflow = VerticalWrapMode.Truncate;

            var next = RuntimeUi.AddPanel(
                card.transform,
                "Prologue First Objective 074",
                new Color(0.07f, 0.12f, 0.13f, 0.96f));
            AnchorGuildCharterCard074(next.rectTransform, GuildCharterCardRegionValues074[3]);
            M1PremiumUi.StylePanel(next, M1PremiumUi.Surface.WorldGlass);
            var nextText = RuntimeUi.AddText(
                next.transform,
                "Prologue First Objective Text 074",
                "FIRST ORDER\nMeet the six founders who answered your charter. Then take the rescue contract at the Hall.",
                21,
                TextAnchor.MiddleLeft,
                RuntimeUi.Positive,
                FontStyle.Bold);
            Stretch(nextText.rectTransform);
            nextText.rectTransform.offsetMin = new Vector2(20f, 8f);
            nextText.rectTransform.offsetMax = new Vector2(-20f, -8f);
            ConfigureResponsiveText062(nextText, 17, 24);
            nextText.verticalOverflow = VerticalWrapMode.Truncate;

            var signature = AddResponsiveText062(
                card.transform,
                "Guildmaster Signature Label 074",
                "YOUR NAME ON THE CHARTER",
                20,
                28,
                42f,
                RuntimeUi.MutedText,
                FontStyle.Bold,
                TextAnchor.MiddleLeft);
            AnchorGuildCharterCard074(signature.rectTransform, GuildCharterCardRegionValues074[4]);
            signature.verticalOverflow = VerticalWrapMode.Truncate;

            var name = RuntimeUi.AddInputField(
                card.transform,
                "Guildmaster Name 074",
                "Enter the Guildmaster name",
                25);
            AnchorGuildCharterCard074(name.GetComponent<RectTransform>(), GuildCharterCardRegionValues074[5]);
            name.text = _guildmasterDraft;
            name.onValueChanged.AddListener(value => _guildmasterDraft = value ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(_localStatus))
            {
                AddStatus(card.transform, _localStatus, _localStatusPositive);
                var status = card.transform.GetChild(card.transform.childCount - 1) as RectTransform;
                AnchorGuildCharterCard074(status, GuildCharterCardRegionValues074[7]);
            }

            var enter = RuntimeUi.AddButton(
                card.transform,
                "Sign Charter And Enter Hall 074",
                "SIGN THE CHARTER",
                () =>
                {
                    _firstHourCharterRoute071 = true;
                    BeginPlayableGuild069();
                },
                104f,
                RuntimeUi.Accent);
            AnchorGuildCharterCard074(enter.GetComponent<RectTransform>(), GuildCharterCardRegionValues074[6]);
            ConfigureResponsiveText062(enter.GetComponentInChildren<Text>(), 20, 30);
            enter.Select();
        }

        private static void AnchorGuildCharterCard074(RectTransform rect, Rect region)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(region.xMin, region.yMin);
            rect.anchorMax = new Vector2(region.xMax, region.yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ApplyKiriProloguePortraitCrop076(Image frame)
        {
            var artwork = frame?.transform
                .Find("Portrait Backing/Portrait Artwork")
                ?.GetComponent<Image>();
            var source = artwork?.sprite;
            if (source == null || source.texture == null) return;

            if (_kiriProloguePortraitCrop076 == null ||
                !ReferenceEquals(_kiriProloguePortraitCrop076.texture, source.texture))
            {
                var sourceRect = source.rect;
                var crop = new Rect(
                    sourceRect.x + sourceRect.width * 0.07f,
                    sourceRect.y + sourceRect.height * 0.51f,
                    sourceRect.width * 0.86f,
                    sourceRect.height * 0.44f);
                _kiriProloguePortraitCrop076 = Sprite.Create(
                    source.texture,
                    crop,
                    new Vector2(0.5f, 0.5f),
                    source.pixelsPerUnit,
                    0u,
                    SpriteMeshType.FullRect);
                _kiriProloguePortraitCrop076.name = "KIRI_AETHERHEART_PROLOGUE_CROP_076";
            }

            artwork.sprite = _kiriProloguePortraitCrop076;
            artwork.preserveAspect = false;
        }
    }
}
