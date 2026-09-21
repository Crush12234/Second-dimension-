using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        public const float BoardAdventureRevealDuration084 = 0.18f;
        public const float BoardAdventureCardFlipDuration084 = 0.46f;
        public const float BoardAdventurePawnTravelDuration084 = 0.34f;
        public const float BoardAdventureRewardRevealDuration084 = 0.42f;
        // The card must always read as a card. A narrow edge can look like a
        // rendering glitch in a captured frame, so this is deliberately a
        // bounded perspective squash rather than a literal zero-width turn.
        public const float BoardAdventureCardFlipMinimumHorizontalScale084 = 0.72f;
        public const float BoardAdventureDiceRollDuration084 = CommittedQuestDice132.Duration132;
        // The dice appear just after the room card crosses its midpoint. A
        // committed check's reward surface stays fully hidden until the saved
        // physical dice have finished settling, plus one small readability beat.
        public const float BoardAdventureDiceRollLeadIn084 =
            BoardAdventurePawnTravelDuration084 +
            BoardAdventureCardFlipDuration084 * 0.55f;
        public const float BoardAdventureRewardAfterDicePause084 = 0.08f;
        public const float BoardAdventureCommittedCheckRewardDelay084 =
            BoardAdventureDiceRollLeadIn084 +
            BoardAdventureDiceRollDuration084 +
            BoardAdventureRewardAfterDicePause084;
        readonly HashSet<string> _settledBoardAdventureTiles084 =
            new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> _settledBoardAdventureCards084 =
            new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> _settledBoardAdventureDice084 =
            new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> _settledBoardAdventurePawns084 =
            new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> _settledBoardAdventureRewards084 =
            new HashSet<string>(StringComparer.Ordinal);

        const string BoardAdventureCardFrameResource084 =
            "SecondDimension/Art/Board086/SECOND_DIMENSION_CARD_BACK_088";
        const string BoardAdventureCardFaceResource084 =
            "SecondDimension/Art/Battle011/UI/UI_COMMAND_CARD_NORMAL";

        static Sprite _boardAdventureDiePipSprite084;

        // A saved board command raises Changed synchronously. The short guard lets
        // the board own its reveal sequence instead of having the generic refresh
        // destroy the card or dice halfway through the animation.
        bool _suppressBoardAdventureCoordinatorRefresh084;

        sealed class BoardDieFace084
        {
            public RectTransform Root;
            public GameObject[] Pips;
        }

        sealed class BoardRewardToken084
        {
            public RectTransform Root;
            public RectTransform Lid;
            public CanvasGroup Glow;
            public CanvasGroup Surface;
        }

        /// <summary>
        /// AddMessagePanel normally owns a fixed preferred height for static copy.
        /// Board panels append images, result cards, and actions after creation, so
        /// that fixed value would exclude those children from the production
        /// ScrollRect's reachable content range. Let the panel's VerticalLayoutGroup
        /// report its complete preferred height instead.
        /// </summary>
        static void UseContentDrivenBoardPanelHeight084(Transform panel)
        {
            if (panel == null) return;
            var layout = panel.GetComponent<LayoutElement>();
            if (layout != null) layout.preferredHeight = -1f;
        }

        void AnimateBoardAdventureTile084(RectTransform tile, string revealKey)
        {
            if (tile == null) return;
            var group = tile.gameObject.GetComponent<CanvasGroup>();
            if (group == null)
                group = tile.gameObject.AddComponent<CanvasGroup>();
            var firstReveal = !string.IsNullOrWhiteSpace(revealKey) &&
                              _settledBoardAdventureTiles084.Add(revealKey);
            if (_reducedMotion || !firstReveal || !Application.isPlaying ||
                !isActiveAndEnabled)
            {
                group.alpha = 1f;
                tile.localScale = Vector3.one;
                return;
            }
            StartCoroutine(RevealBoardAdventureTile084(tile, group));
        }

        IEnumerator RevealBoardAdventureTile084(RectTransform tile, CanvasGroup group)
        {
            if (tile == null || group == null) yield break;
            if (_reducedMotion)
            {
                group.alpha = 1f;
                tile.localScale = Vector3.one;
                yield break;
            }
            var home = tile.anchoredPosition;
            var start = new Vector3(0.08f, 1f, 1f);
            tile.localScale = start;
            tile.anchoredPosition = home + new Vector2(0f, 10f);
            group.alpha = 0.42f;
            var elapsed = 0f;
            while (elapsed < BoardAdventureRevealDuration084 && tile != null &&
                   group != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / BoardAdventureRevealDuration084));
                tile.localScale = Vector3.Lerp(start, Vector3.one, t);
                tile.anchoredPosition = Vector2.Lerp(
                    home + new Vector2(0f, 10f), home, t);
                group.alpha = Mathf.Lerp(0.42f, 1f, t);
                yield return null;
            }
            if (tile != null)
            {
                tile.localScale = Vector3.one;
                tile.anchoredPosition = home;
            }
            if (group != null) group.alpha = 1f;
        }

        /// <summary>
        /// Gives the revealed room the same authored brass-and-ink language as
        /// the battle command cards. This is presentation-only: the supplied
        /// room kind selects a tint but never changes the room or its result.
        /// </summary>
        void DecorateBoardAdventureRevealedCard084(
            RectTransform card,
            string roomKind,
            string rewardCopy = null,
            string visualLabelOverride091 = null)
        {
            if (card == null) return;
            var visualKind = BoardAdventureVisualKind087(
                roomKind, rewardCopy, true);
            var frame = RuntimeUi.AddPanel(
                card,
                "Board Adventure Revealed Card Library Frame 086",
                Color.white);
            var frameLayout = frame.gameObject.AddComponent<LayoutElement>();
            frameLayout.ignoreLayout = true;
            Stretch(frame.rectTransform);
            frame.transform.SetAsFirstSibling();
            frame.raycastTarget = false;
            frame.sprite = Resources.Load<Sprite>(BoardAdventureCardFaceResource084);
            frame.type = frame.sprite == null ? Image.Type.Simple : Image.Type.Sliced;
            var danger = StringComparer.Ordinal.Equals(visualKind, "MONSTER") ||
                         StringComparer.Ordinal.Equals(visualKind, "DEBUFF");
            var boon = StringComparer.Ordinal.Equals(visualKind, "TREASURE") ||
                       StringComparer.Ordinal.Equals(visualKind, "SKILL") ||
                       StringComparer.Ordinal.Equals(visualKind, "XP") ||
                       StringComparer.Ordinal.Equals(visualKind, "RECRUIT") ||
                       StringComparer.Ordinal.Equals(visualKind, "BLESSING") ||
                       StringComparer.Ordinal.Equals(visualKind, "CAMPFIRE");
            // Category color belongs on the small ribbon, not an opaque slab
            // covering the illustrated room and its entire reading surface.
            frame.color = new Color(0.48f, 0.53f, 0.62f, 0.24f);

            var shadow = card.gameObject.GetComponent<Shadow>();
            if (shadow == null) shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.88f);
            shadow.effectDistance = new Vector2(0f, -10f);
            shadow.useGraphicAlpha = false;

            // A revealed room should read at a glance before the player reads its
            // story copy. This corner ribbon is presentation-only and deliberately
            // derives from the authored room kind instead of inventing an outcome.
            var ribbon = RuntimeUi.AddPanel(
                card,
                "Board Adventure Revealed Card Type Ribbon 087 " + visualKind,
                danger
                    ? new Color(0.46f, 0.10f, 0.08f, 0.96f)
                    : boon
                        ? new Color(0.35f, 0.23f, 0.055f, 0.96f)
                        : new Color(0.035f, 0.16f, 0.24f, 0.96f));
            var ribbonLayout = ribbon.gameObject.AddComponent<LayoutElement>();
            ribbonLayout.ignoreLayout = true;
            ribbon.rectTransform.anchorMin = new Vector2(0.025f, 0.885f);
            ribbon.rectTransform.anchorMax = new Vector2(0.42f, 0.985f);
            ribbon.rectTransform.offsetMin = Vector2.zero;
            ribbon.rectTransform.offsetMax = Vector2.zero;
            ribbon.transform.SetAsLastSibling();
            ribbon.raycastTarget = false;
            M1PremiumUi.StylePanel(
                ribbon,
                danger
                    ? M1PremiumUi.Surface.Warning
                    : boon
                        ? M1PremiumUi.Surface.Positive
                        : M1PremiumUi.Surface.WorldRibbon);
            var ribbonCopy = RuntimeUi.AddText(
                ribbon.transform,
                "Board Adventure Revealed Card Type Copy 087",
                visualLabelOverride091 ?? (BoardAdventureVisualGlyph087(visualKind) + "  " +
                BoardAdventureVisualLabel087(visualKind)),
                17,
                TextAnchor.MiddleCenter,
                danger ? RuntimeUi.Warning : boon ? RuntimeUi.Positive : RuntimeUi.Accent,
                FontStyle.Bold);
            Stretch(ribbonCopy.rectTransform);
            ribbonCopy.rectTransform.offsetMin = new Vector2(8f, 2f);
            ribbonCopy.rectTransform.offsetMax = new Vector2(-8f, -2f);
            ConfigureAuthoredCompactText076(ribbonCopy, 12, 18);
            ribbonCopy.raycastTarget = false;
        }

        /// <summary>
        /// A compact physical card on the action side of the board. It makes the
        /// next interaction legible before the player presses the one action.
        /// </summary>
        RectTransform BuildBoardAdventureNextRoomPreview084(
            Transform parent,
            string roomLabel)
        {
            var preview = RuntimeUi.AddPanel(
                parent,
                "Board Adventure Next Face Down Card Preview 086",
                new Color(0.006f, 0.016f, 0.028f, 0.96f));
            RuntimeUi.SetLayout(preview, preferredHeight: 132f);
            M1PremiumUi.StylePanel(preview, M1PremiumUi.Surface.WorldGlass);
            RuntimeUi.AddHorizontalLayout(
                preview.transform,
                new RectOffset(12, 14, 8, 8),
                14f,
                TextAnchor.MiddleLeft);

            var card = RuntimeUi.AddPanel(
                preview.transform,
                "Board Adventure Next Face Down Physical Card 086",
                new Color(0.025f, 0.055f, 0.095f, 1f));
            RuntimeUi.SetLayout(card, preferredWidth: 78f, preferredHeight: 112f);
            card.sprite = Resources.Load<Sprite>(BoardAdventureCardFrameResource084);
            card.type = Image.Type.Simple;
            card.preserveAspect = true;
            card.color = Color.white;
            card.raycastTarget = false;
            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.82f);
            shadow.effectDistance = new Vector2(0f, -6f);
            shadow.useGraphicAlpha = false;

            var seal = RuntimeUi.AddText(
                card.transform,
                "Board Adventure Next Card Seal 086",
                "◆\nSEALED",
                25,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.84f, 0.43f, 1f),
                FontStyle.Bold);
            Stretch(seal.rectTransform);
            seal.rectTransform.offsetMin = new Vector2(10f, 8f);
            seal.rectTransform.offsetMax = new Vector2(-10f, -8f);
            ConfigureAuthoredCompactText076(seal, 14, 26);
            seal.raycastTarget = false;

            var copy = RuntimeUi.AddText(
                preview.transform,
                "Board Adventure Next Card Preview Copy 086",
                (string.IsNullOrWhiteSpace(roomLabel) ? "NEXT ROOM" : roomLabel) +
                "\nFACE DOWN\n\nTAP ONCE TO MOVE + FLIP",
                19,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            RuntimeUi.SetLayout(copy, preferredHeight: 112f, flexibleWidth: 1f);
            ConfigureAuthoredCompactText076(copy, 13, 20);
            copy.raycastTarget = false;
            return preview.rectTransform;
        }

        /// <summary>
        /// Builds a small, constructed board-game pawn. The token travels in
        /// from the preceding edge and lands with one restrained bounce whenever
        /// a new saved room becomes current.
        /// </summary>
        RectTransform BuildBoardAdventurePawn084(
            Transform parent,
            string travelKey,
            bool compact = false)
        {
            var pawn = RuntimeUi.AddPanel(
                parent,
                "Board Adventure Guild Pawn 086",
                Color.clear);
            var layout = pawn.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            pawn.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            pawn.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            pawn.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            pawn.rectTransform.sizeDelta = compact
                ? new Vector2(42f, 62f)
                : new Vector2(50f, 70f);
            pawn.rectTransform.anchoredPosition = new Vector2(
                compact ? 25f : 30f, 0f);
            pawn.raycastTarget = false;
            var group = pawn.gameObject.AddComponent<CanvasGroup>();

            var aura = RuntimeUi.AddPanel(
                pawn.transform,
                "Board Adventure Pawn Landing Glow 086",
                new Color(0.25f, 0.82f, 1f, 0.22f));
            aura.rectTransform.anchorMin = new Vector2(0.08f, 0.02f);
            aura.rectTransform.anchorMax = new Vector2(0.92f, 0.32f);
            aura.rectTransform.offsetMin = Vector2.zero;
            aura.rectTransform.offsetMax = Vector2.zero;
            aura.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            aura.raycastTarget = false;

            var body = RuntimeUi.AddPanel(
                pawn.transform,
                "Board Adventure Pawn Body 086",
                new Color(0.93f, 0.65f, 0.22f, 1f));
            body.rectTransform.anchorMin = new Vector2(0.27f, 0.24f);
            body.rectTransform.anchorMax = new Vector2(0.73f, 0.68f);
            body.rectTransform.offsetMin = Vector2.zero;
            body.rectTransform.offsetMax = Vector2.zero;
            M1PremiumUi.StylePanel(body, M1PremiumUi.Surface.Warning);
            body.raycastTarget = false;

            var head = RuntimeUi.AddPanel(
                pawn.transform,
                "Board Adventure Pawn Head 086",
                new Color(1f, 0.85f, 0.44f, 1f));
            head.rectTransform.anchorMin = new Vector2(0.34f, 0.64f);
            head.rectTransform.anchorMax = new Vector2(0.66f, 0.88f);
            head.rectTransform.offsetMin = Vector2.zero;
            head.rectTransform.offsetMax = Vector2.zero;
            head.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            head.raycastTarget = false;

            var footing = RuntimeUi.AddPanel(
                pawn.transform,
                "Board Adventure Pawn Base 086",
                new Color(0.80f, 0.50f, 0.14f, 1f));
            footing.rectTransform.anchorMin = new Vector2(0.10f, 0.13f);
            footing.rectTransform.anchorMax = new Vector2(0.90f, 0.28f);
            footing.rectTransform.offsetMin = Vector2.zero;
            footing.rectTransform.offsetMax = Vector2.zero;
            footing.raycastTarget = false;
            var shadow = footing.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(0f, -4f);
            shadow.useGraphicAlpha = false;

            var firstTravel = !string.IsNullOrWhiteSpace(travelKey) &&
                              _settledBoardAdventurePawns084.Add(travelKey);
            if (_reducedMotion || !firstTravel || !Application.isPlaying ||
                !isActiveAndEnabled)
            {
                group.alpha = 1f;
                pawn.rectTransform.localScale = Vector3.one;
                return pawn.rectTransform;
            }
            StartCoroutine(TravelBoardAdventurePawn084(
                pawn.rectTransform, group));
            return pawn.rectTransform;
        }

        IEnumerator TravelBoardAdventurePawn084(
            RectTransform pawn,
            CanvasGroup group)
        {
            if (pawn == null || group == null) yield break;
            var home = pawn.anchoredPosition;
            var start = home + new Vector2(-72f, 0f);
            pawn.anchoredPosition = start;
            pawn.localScale = new Vector3(0.82f, 0.82f, 1f);
            group.alpha = 0.35f;
            var elapsed = 0f;
            while (elapsed < BoardAdventurePawnTravelDuration084 &&
                   pawn != null && group != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / BoardAdventurePawnTravelDuration084);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                pawn.anchoredPosition = Vector2.Lerp(start, home, eased) +
                                        Vector2.up * Mathf.Sin(t * Mathf.PI) * 10f;
                var landing = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.045f;
                pawn.localScale = Vector3.one * landing;
                group.alpha = Mathf.Lerp(0.35f, 1f, eased);
                yield return null;
            }
            if (pawn != null)
            {
                pawn.anchoredPosition = home;
                pawn.localScale = Vector3.one;
            }
            if (group != null) group.alpha = 1f;
        }

        /// <summary>
        /// Presents a real two-sided room card: the player first sees a sealed,
        /// physical Guild card with a frame, cast shadow, and crest. It performs
        /// a bounded perspective squash, the face is swapped at the midpoint, and
        /// the authored room expands into view. Reduced motion settles at once.
        /// </summary>
        void AnimateBoardAdventureCardFlip084(
            RectTransform card,
            string revealKey,
            string backLabel,
            Action revealComplete = null,
            float additionalLeadIn = 0f,
            string stageCueCopy = null)
        {
            if (card == null) return;
            var firstReveal = !string.IsNullOrWhiteSpace(revealKey) &&
                              _settledBoardAdventureCards084.Add(revealKey);
            if (_reducedMotion || !firstReveal || !Application.isPlaying ||
                !isActiveAndEnabled)
            {
                card.localScale = Vector3.one;
                revealComplete?.Invoke();
                return;
            }

            var stage = RuntimeUi.AddPanel(
                card,
                "Board Adventure Card Flip Stage 086",
                // This is deliberately opaque. The authoritative destination
                // room is already saved and built behind it, but no part of that
                // face may leak through before the physical card reaches its
                // midpoint and the stage is removed.
                new Color(0.018f, 0.040f, 0.072f, 1f));
            var stageLayout = stage.gameObject.AddComponent<LayoutElement>();
            stageLayout.ignoreLayout = true;
            Stretch(stage.rectTransform);
            stage.transform.SetAsLastSibling();
            stage.raycastTarget = false;
            stage.gameObject.AddComponent<RectMask2D>();
            // A muted, fixed Guild sigil dresses the table without borrowing
            // any image or text from the still-confidential destination face.
            var tablePattern = RuntimeUi.AddPanel(stage.transform,
                "Board Card Privacy Table Pattern 091", new Color(0.30f, 0.40f, 0.58f, 0.20f));
            Stretch(tablePattern.rectTransform);
            tablePattern.sprite = Resources.Load<Sprite>(BoardAdventureCardFrameResource084);
            tablePattern.type = Image.Type.Simple;
            tablePattern.preserveAspect = true;
            tablePattern.raycastTarget = false;
            var tableAspect = tablePattern.gameObject.AddComponent<AspectRatioFitter>();
            tableAspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            tableAspect.aspectRatio = 2f / 3f;

            var stageCue = RuntimeUi.AddText(
                stage.transform,
                "Board Adventure Card Flip Stage Cue 086",
                string.IsNullOrWhiteSpace(stageCueCopy)
                    ? "PAWN LANDED  •  TURNING THE ROOM CARD"
                    : stageCueCopy,
                19,
                TextAnchor.MiddleCenter,
                RuntimeUi.Accent,
                FontStyle.Bold);
            stageCue.rectTransform.anchorMin = new Vector2(0.10f, 0.87f);
            stageCue.rectTransform.anchorMax = new Vector2(0.90f, 0.97f);
            stageCue.rectTransform.offsetMin = Vector2.zero;
            stageCue.rectTransform.offsetMax = Vector2.zero;
            ConfigureAuthoredCompactText076(stageCue, 13, 20);
            stageCue.raycastTarget = false;

            var back = RuntimeUi.AddPanel(
                stage.transform,
                "Face Down Room Card Back 084",
                new Color(0.025f, 0.055f, 0.095f, 1f));
            back.rectTransform.anchorMin = new Vector2(0.27f, 0.08f);
            back.rectTransform.anchorMax = new Vector2(0.73f, 0.87f);
            back.rectTransform.offsetMin = Vector2.zero;
            back.rectTransform.offsetMax = Vector2.zero;
            M1PremiumUi.StylePanel(back, M1PremiumUi.Surface.WorldRibbon);
            back.raycastTarget = false;
            var shadow = back.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.82f);
            shadow.effectDistance = new Vector2(0f, -12f);
            shadow.useGraphicAlpha = false;

            var inner = RuntimeUi.AddPanel(
                back.transform,
                "Face Down Room Card Frame 085",
                new Color(0.045f, 0.090f, 0.135f, 0.98f));
            inner.rectTransform.anchorMin = new Vector2(0.055f, 0.075f);
            inner.rectTransform.anchorMax = new Vector2(0.945f, 0.925f);
            inner.rectTransform.offsetMin = Vector2.zero;
            inner.rectTransform.offsetMax = Vector2.zero;
            M1PremiumUi.StylePanel(inner, M1PremiumUi.Surface.Iron);
            inner.raycastTarget = false;

            var libraryFrame = RuntimeUi.AddPanel(
                inner.transform,
                "Face Down Room Card Library Frame 086",
                Color.white);
            Stretch(libraryFrame.rectTransform);
            libraryFrame.transform.SetAsFirstSibling();
            libraryFrame.sprite = Resources.Load<Sprite>(
                BoardAdventureCardFrameResource084);
            libraryFrame.type = Image.Type.Simple;
            libraryFrame.preserveAspect = false;
            libraryFrame.color = Color.white;
            libraryFrame.raycastTarget = false;

            var header = RuntimeUi.AddText(
                inner.transform,
                "Face Down Room Card Guild Mark 085",
                "GUILD OF WORLDS",
                25,
                TextAnchor.MiddleCenter,
                RuntimeUi.Accent,
                FontStyle.Bold);
            header.rectTransform.anchorMin = new Vector2(0.10f, 0.77f);
            header.rectTransform.anchorMax = new Vector2(0.90f, 0.91f);
            header.rectTransform.offsetMin = Vector2.zero;
            header.rectTransform.offsetMax = Vector2.zero;
            ConfigureAuthoredCompactText076(header, 16, 25);
            header.raycastTarget = false;

            var crest = RuntimeUi.AddPanel(
                inner.transform,
                "Face Down Room Card Crest 085",
                new Color(0.12f, 0.088f, 0.034f, 1f));
            crest.rectTransform.anchorMin = new Vector2(0.365f, 0.285f);
            crest.rectTransform.anchorMax = new Vector2(0.635f, 0.695f);
            crest.rectTransform.offsetMin = Vector2.zero;
            crest.rectTransform.offsetMax = Vector2.zero;
            M1PremiumUi.StylePanel(crest, M1PremiumUi.Surface.Warning);
            crest.raycastTarget = false;
            var crestShadow = crest.gameObject.AddComponent<Shadow>();
            crestShadow.effectColor = new Color(0f, 0f, 0f, 0.68f);
            crestShadow.effectDistance = new Vector2(0f, -5f);
            crestShadow.useGraphicAlpha = false;

            var crestGlyph = RuntimeUi.AddText(
                crest.transform,
                "Face Down Room Card Crest Glyph 085",
                "◆",
                72,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.86f, 0.47f, 1f),
                FontStyle.Bold);
            Stretch(crestGlyph.rectTransform);
            crestGlyph.rectTransform.offsetMin = new Vector2(6f, 2f);
            crestGlyph.rectTransform.offsetMax = new Vector2(-6f, -2f);
            crestGlyph.raycastTarget = false;

            var seal = RuntimeUi.AddText(
                inner.transform,
                "Face Down Room Card Seal Copy 085",
                "SEALED ROOM",
                22,
                TextAnchor.MiddleCenter,
                RuntimeUi.MutedText,
                FontStyle.Bold);
            seal.rectTransform.anchorMin = new Vector2(0.10f, 0.175f);
            seal.rectTransform.anchorMax = new Vector2(0.90f, 0.285f);
            seal.rectTransform.offsetMin = Vector2.zero;
            seal.rectTransform.offsetMax = Vector2.zero;
            ConfigureAuthoredCompactText076(seal, 16, 22);
            seal.raycastTarget = false;

            var prompt = RuntimeUi.AddText(
                inner.transform,
                "Face Down Room Card Flip Prompt 085",
                (string.IsNullOrWhiteSpace(backLabel) ? "NEXT ROOM" : backLabel) +
                "  •  FLIP TO REVEAL",
                19,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Bold);
            prompt.rectTransform.anchorMin = new Vector2(0.08f, 0.055f);
            prompt.rectTransform.anchorMax = new Vector2(0.92f, 0.165f);
            prompt.rectTransform.offsetMin = Vector2.zero;
            prompt.rectTransform.offsetMax = Vector2.zero;
            ConfigureAuthoredCompactText076(prompt, 14, 19);
            prompt.raycastTarget = false;

            // Reuse the same intact portrait back as the blind three-card deal.
            // The library art already contains its crest and frame; stamping
            // another diamond, label stack, and nested panels over it obscures it.
            header.gameObject.SetActive(false);
            crest.gameObject.SetActive(false);
            seal.gameObject.SetActive(false);
            prompt.gameObject.SetActive(false);
            foreach (var owner in new[] { back.transform, inner.transform })
                for (var index = 0; index < owner.childCount; index++)
                    if (owner.GetChild(index).name.StartsWith("Premium ", StringComparison.Ordinal))
                        owner.GetChild(index).gameObject.SetActive(false);
            back.sprite = null;
            back.color = Color.clear;
            inner.sprite = null;
            inner.color = Color.clear;
            Stretch(inner.rectTransform);
            libraryFrame.preserveAspect = true;
            var portraitViewport = new GameObject("Room Card Portrait Viewport 091",
                typeof(RectTransform)).GetComponent<RectTransform>();
            portraitViewport.SetParent(stage.transform, false);
            SetAnchors074(portraitViewport, new Vector2(0.05f, 0.045f),
                new Vector2(0.95f, 0.855f));
            back.transform.SetParent(portraitViewport, false);
            var portraitAspect = back.gameObject.AddComponent<AspectRatioFitter>();
            portraitAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            portraitAspect.aspectRatio = 2f / 3f;

            StartCoroutine(FlipBoardAdventureCard084(
                card, stage.gameObject, back.rectTransform,
                revealComplete, additionalLeadIn));
        }

        IEnumerator FlipBoardAdventureCard084(
            RectTransform card,
            GameObject cardStage,
            RectTransform physicalCard,
            Action revealComplete,
            float additionalLeadIn)
        {
            if (card == null || cardStage == null || physicalCard == null)
                yield break;
            if (_reducedMotion)
            {
                card.localScale = Vector3.one;
                card.localEulerAngles = Vector3.zero;
                cardStage.SetActive(false);
                revealComplete?.Invoke();
                yield break;
            }

            // The pause lets the pawn visibly finish its one-space travel before
            // the physical card turns. It is deliberately short enough to keep
            // the phone-simple loop brisk.
            yield return new WaitForSecondsRealtime(
                BoardAdventurePawnTravelDuration084 +
                Mathf.Max(0f, additionalLeadIn));
            if (card == null || cardStage == null || physicalCard == null)
                yield break;

            var half = BoardAdventureCardFlipDuration084 * 0.5f;
            var elapsed = 0f;
            while (elapsed < half && card != null && cardStage != null &&
                   physicalCard != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / half));
                physicalCard.localScale = new Vector3(
                    Mathf.Lerp(
                        1f,
                        BoardAdventureCardFlipMinimumHorizontalScale084,
                        t),
                    Mathf.Lerp(1f, 0.985f, Mathf.Sin(t * Mathf.PI)),
                    1f);
                physicalCard.localEulerAngles = new Vector3(
                    0f,
                    Mathf.Lerp(0f, 78f, t),
                    Mathf.Sin(t * Mathf.PI) * -2.5f);
                yield return null;
            }
            if (card == null || cardStage == null || physicalCard == null)
                yield break;

            cardStage.SetActive(false);
            card.localScale = new Vector3(
                BoardAdventureCardFlipMinimumHorizontalScale084, 0.985f, 1f);
            card.localEulerAngles = new Vector3(0f, -78f, 0f);
            elapsed = 0f;
            while (elapsed < half && card != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / half));
                card.localScale = new Vector3(
                    Mathf.Lerp(
                        BoardAdventureCardFlipMinimumHorizontalScale084,
                        1f,
                        t),
                    Mathf.Lerp(0.985f, 1f, Mathf.Sin(t * Mathf.PI)),
                    1f);
                card.localEulerAngles = new Vector3(
                    0f,
                    Mathf.Lerp(-78f, 0f, t),
                    Mathf.Sin(t * Mathf.PI) * 1.6f);
                yield return null;
            }
            if (card != null)
            {
                card.localScale = Vector3.one;
                card.localEulerAngles = Vector3.zero;
                revealComplete?.Invoke();
            }
        }

        /// <summary>
        /// Builds two visible square dice with physical pips. Cosmetic faces cycle
        /// without touching gameplay RNG, then settle on the already-saved values.
        /// </summary>
        RectTransform BuildAuthoritativeDiceRoll084(
            Transform parent,
            int dieOne,
            int dieTwo,
            int modifier,
            int total,
            string revealKey,
            bool positive)
        {
            var rowImage = RuntimeUi.AddPanel(
                parent,
                "Authoritative Dice Roll 084",
                new Color(0.008f, 0.020f, 0.032f, 0.97f));
            RuntimeUi.SetLayout(rowImage, preferredHeight: 236f).minHeight = 236f;
            M1PremiumUi.StylePanel(rowImage, M1PremiumUi.Surface.WorldRibbon);
            var row = rowImage.rectTransform;

            var firstReveal = !string.IsNullOrWhiteSpace(revealKey) &&
                              !_settledBoardAdventureDice084.Contains(revealKey);
            var animate = firstReveal && Application.isPlaying && !_reducedMotion;
            var motion = RuntimeUi.AddText(
                row,
                "Authoritative Dice Motion Status 086",
                animate ? "YOUR ROLL  •  TOSS BOTH DICE" : "DICE SETTLED  •  SAVED RESULT",
                16,
                TextAnchor.MiddleCenter,
                positive ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold);
            motion.rectTransform.anchorMin = new Vector2(0.04f, 0.81f);
            motion.rectTransform.anchorMax = new Vector2(0.96f, 0.97f);
            motion.rectTransform.offsetMin = Vector2.zero;
            motion.rectTransform.offsetMax = Vector2.zero;
            ConfigureAuthoredCompactText076(motion, 22, 28);
            motion.raycastTarget = false;

            var diceLine = new GameObject(
                    "Authoritative Dice Physical Tray 086",
                    typeof(RectTransform))
                .GetComponent<RectTransform>();
            diceLine.SetParent(row, false);
            diceLine.anchorMin = new Vector2(0.03f, 0.24f);
            diceLine.anchorMax = new Vector2(0.97f, 0.80f);
            diceLine.offsetMin = Vector2.zero;
            diceLine.offsetMax = Vector2.zero;
            var layout = RuntimeUi.AddHorizontalLayout(
                diceLine,
                new RectOffset(4, 4, 0, 0),
                12f,
                TextAnchor.MiddleCenter);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var first = CreateBoardDieFace084(
                diceLine, "First Authoritative Die 084", dieOne);
            var plus = RuntimeUi.AddText(
                diceLine,
                "Authoritative Dice Plus 084",
                "+",
                34,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Bold);
            RuntimeUi.SetLayout(plus, preferredWidth: 28f, preferredHeight: 84f);
            var second = CreateBoardDieFace084(
                diceLine, "Second Authoritative Die 084", dieTwo);

            var equation = RuntimeUi.AddText(
                diceLine,
                "Authoritative Dice Equation 084",
                SignedBoardAdventure084(modifier) + "  =  " + total,
                31,
                TextAnchor.MiddleCenter,
                positive ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold);
            RuntimeUi.SetLayout(equation, preferredWidth: 190f, preferredHeight: 84f);

            var visual = row.gameObject.AddComponent<CommittedQuestDice132>();
            var copyScope = parent.name == "Board Quest Dice Result 081" ? parent.parent : parent;
            visual.Configure132(first.Root, second.Root, motion, equation,
                dieOne, dieTwo, modifier, total, positive, !animate,
                (one, two) => { SetBoardDieValue084(first, one); SetBoardDieValue084(second, two); },
                () => { if (!string.IsNullOrWhiteSpace(revealKey)) _settledBoardAdventureDice084.Add(revealKey); },
                copyScope, BoardAdventureDiePipSprite084());
            return row;
        }

        static BoardDieFace084 CreateBoardDieFace084(
            Transform parent,
            string name,
            int value)
        {
            var slot = new GameObject(
                    name + " Physical Slot 086",
                    typeof(RectTransform),
                    typeof(LayoutElement))
                .GetComponent<RectTransform>();
            slot.SetParent(parent, false);
            RuntimeUi.SetLayout(slot, preferredWidth: 116f, preferredHeight: 116f);
            var image = RuntimeUi.AddPanel(
                slot,
                name,
                new Color(0.94f, 0.95f, 0.98f, 1f));
            image.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            image.rectTransform.sizeDelta = new Vector2(108f, 108f);
            image.rectTransform.anchoredPosition = Vector2.zero;
            M1PremiumUi.StylePanel(image, M1PremiumUi.Surface.WorldPaper);
            // WorldPaper normally uses a dark parchment tint. Dice need a bright
            // physical face so their saved pips remain the focal point in motion.
            image.color = new Color(0.94f, 0.95f, 0.98f, 1f);
            var dieShadow = image.gameObject.AddComponent<Shadow>();
            dieShadow.effectColor = new Color(0f, 0f, 0f, 0.82f);
            dieShadow.effectDistance = new Vector2(0f, -6f);
            dieShadow.useGraphicAlpha = false;
            var dieOutline = image.gameObject.AddComponent<Outline>();
            dieOutline.effectColor = new Color(0.73f, 0.56f, 0.24f, 0.9f);
            dieOutline.effectDistance = new Vector2(2f, -2f);
            dieOutline.useGraphicAlpha = false;

            var inset = RuntimeUi.AddPanel(
                image.transform,
                "Board Die Beveled Face 086",
                new Color(0.82f, 0.86f, 0.91f, 0.30f));
            inset.rectTransform.anchorMin = new Vector2(0.07f, 0.07f);
            inset.rectTransform.anchorMax = new Vector2(0.93f, 0.93f);
            inset.rectTransform.offsetMin = Vector2.zero;
            inset.rectTransform.offsetMax = Vector2.zero;
            inset.raycastTarget = false;
            var face = new BoardDieFace084
            {
                Root = image.rectTransform,
                Pips = new GameObject[9]
            };
            var positions = new[]
            {
                new Vector2(0.24f, 0.76f), new Vector2(0.50f, 0.76f),
                new Vector2(0.76f, 0.76f), new Vector2(0.24f, 0.50f),
                new Vector2(0.50f, 0.50f), new Vector2(0.76f, 0.50f),
                new Vector2(0.24f, 0.24f), new Vector2(0.50f, 0.24f),
                new Vector2(0.76f, 0.24f)
            };
            for (var index = 0; index < face.Pips.Length; index++)
            {
                // Use a generated circular sprite instead of a font glyph. The
                // production UI font can legitimately omit bullet characters,
                // which made otherwise-correct dice look blank in the player.
                var pipObject = new GameObject(
                    "Die Pip " + index + " 084",
                    typeof(RectTransform),
                    typeof(Image));
                pipObject.transform.SetParent(image.transform, false);
                var pip = pipObject.GetComponent<Image>();
                pip.sprite = BoardAdventureDiePipSprite084();
                pip.preserveAspect = true;
                pip.color = new Color(0.025f, 0.040f, 0.060f, 1f);
                pip.rectTransform.anchorMin = positions[index];
                pip.rectTransform.anchorMax = positions[index];
                pip.rectTransform.sizeDelta = new Vector2(23f, 23f);
                pip.rectTransform.anchoredPosition = Vector2.zero;
                pip.raycastTarget = false;
                face.Pips[index] = pipObject;
            }
            SetBoardDieValue084(face, value);
            return face;
        }

        static Sprite BoardAdventureDiePipSprite084()
        {
            if (_boardAdventureDiePipSprite084 != null)
                return _boardAdventureDiePipSprite084;

            const int size = 32;
            var texture = new Texture2D(
                size, size, TextureFormat.RGBA32, false, false)
            {
                name = "Board Adventure Physical Die Pip 084",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            const float radius = 13.5f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(
                    new Vector2(x, y), new Vector2(center, center));
                var alpha = Mathf.Clamp01(radius + 1f - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            _boardAdventureDiePipSprite084 = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            _boardAdventureDiePipSprite084.name =
                "Board Adventure Physical Die Pip Sprite 084";
            _boardAdventureDiePipSprite084.hideFlags = HideFlags.HideAndDontSave;
            return _boardAdventureDiePipSprite084;
        }

        static void SetBoardDieValue084(BoardDieFace084 face, int value)
        {
            if (face?.Pips == null) return;
            value = Mathf.Clamp(value, 1, 6);
            for (var index = 0; index < face.Pips.Length; index++)
                if (face.Pips[index] != null) face.Pips[index].SetActive(false);

            switch (value)
            {
                case 1:
                    ShowBoardDiePips084(face, 4);
                    break;
                case 2:
                    ShowBoardDiePips084(face, 0, 8);
                    break;
                case 3:
                    ShowBoardDiePips084(face, 0, 4, 8);
                    break;
                case 4:
                    ShowBoardDiePips084(face, 0, 2, 6, 8);
                    break;
                case 5:
                    ShowBoardDiePips084(face, 0, 2, 4, 6, 8);
                    break;
                default:
                    ShowBoardDiePips084(face, 0, 2, 3, 5, 6, 8);
                    break;
            }
        }

        static void ShowBoardDiePips084(BoardDieFace084 face, params int[] indices)
        {
            if (face?.Pips == null || indices == null) return;
            for (var index = 0; index < indices.Length; index++)
            {
                var pipIndex = indices[index];
                if (pipIndex >= 0 && pipIndex < face.Pips.Length &&
                    face.Pips[pipIndex] != null)
                    face.Pips[pipIndex].SetActive(true);
            }
        }

        RectTransform BuildBoardAdventureRewardToken084(
            Transform parent,
            string roomKind,
            string revealKey,
            bool positive,
            bool waitForAuthoritativeDice,
            CanvasGroup rewardSurface)
        {
            var kind = (roomKind ?? string.Empty).ToUpperInvariant();
            var token = RuntimeUi.AddPanel(
                parent,
                "Board Adventure Reward Token 086 " + kind,
                Color.white);
            var tokenLayout = token.gameObject.AddComponent<LayoutElement>();
            tokenLayout.ignoreLayout = true;
            token.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            token.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            token.rectTransform.pivot = new Vector2(0f, 0.5f);
            token.rectTransform.sizeDelta = new Vector2(72f, 48f);
            token.rectTransform.anchoredPosition = new Vector2(12f, 0f);
            M1PremiumUi.StylePanel(token,
                positive ? M1PremiumUi.Surface.Positive : M1PremiumUi.Surface.Warning);
            token.raycastTarget = false;
            var shadow = token.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.78f);
            shadow.effectDistance = new Vector2(0f, -4f);
            shadow.useGraphicAlpha = false;

            var glowImage = RuntimeUi.AddPanel(
                token.transform,
                "Board Adventure Reward Glow 086",
                positive
                    ? new Color(0.20f, 1f, 0.60f, 0.26f)
                    : new Color(1f, 0.42f, 0.20f, 0.24f));
            Stretch(glowImage.rectTransform);
            glowImage.rectTransform.offsetMin = new Vector2(-5f, -5f);
            glowImage.rectTransform.offsetMax = new Vector2(5f, 5f);
            glowImage.transform.SetAsFirstSibling();
            glowImage.raycastTarget = false;
            var glow = glowImage.gameObject.AddComponent<CanvasGroup>();

            RectTransform lid = null;
            if (StringComparer.Ordinal.Equals(kind, "TREASURE"))
            {
                var chestBody = RuntimeUi.AddPanel(
                    token.transform,
                    "Board Reward Chest Body 086",
                    new Color(0.44f, 0.22f, 0.07f, 1f));
                chestBody.rectTransform.anchorMin = new Vector2(0.20f, 0.18f);
                chestBody.rectTransform.anchorMax = new Vector2(0.80f, 0.62f);
                chestBody.rectTransform.offsetMin = Vector2.zero;
                chestBody.rectTransform.offsetMax = Vector2.zero;
                var band = RuntimeUi.AddPanel(
                    chestBody.transform,
                    "Board Reward Chest Brass Band 086",
                    new Color(0.95f, 0.68f, 0.20f, 1f));
                band.rectTransform.anchorMin = new Vector2(0.43f, 0f);
                band.rectTransform.anchorMax = new Vector2(0.57f, 1f);
                band.rectTransform.offsetMin = Vector2.zero;
                band.rectTransform.offsetMax = Vector2.zero;
                band.raycastTarget = false;
                chestBody.raycastTarget = false;

                var chestLid = RuntimeUi.AddPanel(
                    token.transform,
                    "Board Reward Chest Lid 086",
                    new Color(0.67f, 0.35f, 0.09f, 1f));
                chestLid.rectTransform.anchorMin = new Vector2(0.17f, 0.60f);
                chestLid.rectTransform.anchorMax = new Vector2(0.83f, 0.80f);
                chestLid.rectTransform.offsetMin = Vector2.zero;
                chestLid.rectTransform.offsetMax = Vector2.zero;
                chestLid.rectTransform.pivot = new Vector2(0.15f, 0f);
                chestLid.raycastTarget = false;
                lid = chestLid.rectTransform;
            }
            else
            {
                var glyph = StringComparer.Ordinal.Equals(kind, "SKILL") ? "ART" :
                            StringComparer.Ordinal.Equals(kind, "XP") ? "XP" :
                            StringComparer.Ordinal.Equals(kind, "RECRUIT") ? "HERO" :
                            StringComparer.Ordinal.Equals(kind, "BLESSING") ? "+" :
                            StringComparer.Ordinal.Equals(kind, "CAMPFIRE") ? "REST" :
                            StringComparer.Ordinal.Equals(kind, "MONSTER") ? "!" :
                            StringComparer.Ordinal.Equals(kind, "RETURN") ? "HOME" :
                            StringComparer.Ordinal.Equals(kind, "FATE") ? "2D6" : "CLUE";
                var sigil = RuntimeUi.AddText(
                    token.transform,
                    "Board Adventure Reward Sigil 086",
                    glyph,
                    glyph.Length > 2 ? 15 : 28,
                    TextAnchor.MiddleCenter,
                    positive ? RuntimeUi.Positive : RuntimeUi.Warning,
                    FontStyle.Bold);
                Stretch(sigil.rectTransform);
                sigil.rectTransform.offsetMin = new Vector2(5f, 3f);
                sigil.rectTransform.offsetMax = new Vector2(-5f, -3f);
                ConfigureAuthoredCompactText076(sigil, 11, 28);
                sigil.raycastTarget = false;
            }

            var visual = new BoardRewardToken084
            {
                Root = token.rectTransform,
                Lid = lid,
                Glow = glow,
                Surface = rewardSurface == null
                    ? token.gameObject.AddComponent<CanvasGroup>()
                    : rewardSurface
            };
            var dice132 = waitForAuthoritativeDice ? CommittedQuestDice132.Find132(parent) : null;
            var firstReveal = (dice132 != null && !dice132.IsSettled132) ||
                              (!string.IsNullOrWhiteSpace(revealKey) &&
                               _settledBoardAdventureRewards084.Add(revealKey));
            if (_reducedMotion || !firstReveal || !Application.isPlaying ||
                !isActiveAndEnabled)
            {
                visual.Root.localScale = Vector3.one;
                visual.Surface.alpha = 1f;
                if (visual.Lid != null)
                {
                    visual.Lid.localEulerAngles = new Vector3(0f, 0f, -13f);
                    visual.Lid.anchoredPosition += new Vector2(0f, 4f);
                }
                visual.Glow.alpha = 0.45f;
                return token.rectTransform;
            }
            // Hide the complete reward strip immediately, including its copy,
            // rather than leaving a closed chest or resolved reward visible
            // while the physical dice are still moving.
            visual.Root.localScale = new Vector3(0.76f, 0.76f, 1f);
            visual.Surface.alpha = 0f;
            visual.Glow.alpha = 0f;
            StartCoroutine(RevealBoardAdventureReward084(
                visual,
                waitForAuthoritativeDice
                    ? BoardAdventureCommittedCheckRewardDelay084
                    : BoardAdventurePawnTravelDuration084 +
                      BoardAdventureCardFlipDuration084, dice132));
            return token.rectTransform;
        }

        /// <summary>
        /// Shared resolved-room reward surface used by story chapters, World Gate
        /// adventures, and the Tower. It gives treasure a chest, support rooms a
        /// boon token, hazards a warning token, and story rooms a clear event seal.
        /// Gameplay values and receipt timing remain owned by their original systems.
        /// </summary>
        void BuildBoardAdventureResolvedRewardStrip087(
            Transform parent,
            string roomKind,
            string rewardCopy,
            string revealKey,
            bool positive,
            bool waitForAuthoritativeDice)
        {
            if (parent == null) return;
            var visualKind = BoardAdventureVisualKind087(
                roomKind, rewardCopy, positive);
            var surface = RuntimeUi.AddPanel(
                parent,
                "Board Adventure Resolved Reward Surface 087 " + visualKind,
                positive
                    ? new Color(0.030f, 0.130f, 0.095f, 0.96f)
                    : new Color(0.175f, 0.065f, 0.035f, 0.96f));
            RuntimeUi.SetLayout(surface, preferredHeight: 78f);
            M1PremiumUi.StylePanel(
                surface,
                positive ? M1PremiumUi.Surface.Positive : M1PremiumUi.Surface.Warning);
            surface.raycastTarget = false;
            var rewardGroup = surface.gameObject.AddComponent<CanvasGroup>();
            BuildBoardAdventureRewardToken084(
                surface.transform,
                visualKind,
                revealKey,
                positive,
                waitForAuthoritativeDice,
                rewardGroup);
            var copy = RuntimeUi.AddText(
                surface.transform,
                "Board Adventure Resolved Reward Copy 087",
                "REWARD SAVED  •  " +
                (string.IsNullOrWhiteSpace(rewardCopy)
                    ? positive ? "STORY PROGRESS" : "THE QUEST CONTINUES"
                    : rewardCopy),
                19,
                TextAnchor.MiddleLeft,
                positive ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold);
            copy.rectTransform.anchorMin = Vector2.zero;
            copy.rectTransform.anchorMax = Vector2.one;
            copy.rectTransform.offsetMin = new Vector2(96f, 8f);
            copy.rectTransform.offsetMax = new Vector2(-12f, -8f);
            ConfigureResponsiveText062(copy, 13, 20);
            copy.raycastTarget = false;
        }

        static string BoardAdventureVisualKind087(
            string roomKind,
            string rewardCopy,
            bool positive)
        {
            if (!positive) return "DEBUFF";
            var kind = (roomKind ?? string.Empty).Trim().ToUpperInvariant();
            var reward = rewardCopy ?? string.Empty;
            if (reward.IndexOf("RECRUIT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                reward.IndexOf("APPLICANT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                reward.IndexOf("HERO UNLOCK", StringComparison.OrdinalIgnoreCase) >= 0)
                return "RECRUIT";
            if (reward.IndexOf("CHEST", StringComparison.OrdinalIgnoreCase) >= 0 ||
                reward.IndexOf("MATERIAL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                reward.IndexOf("EQUIPMENT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                reward.IndexOf("RELIC", StringComparison.OrdinalIgnoreCase) >= 0)
                return "TREASURE";

            switch (kind)
            {
                case "RESOURCE":
                case "TREASURE":
                case "CHEST":
                    return "TREASURE";
                case "CAMP":
                case "CAMPFIRE":
                    return "CAMPFIRE";
                case "BUFF":
                case "FORTRESS_PREPARATION":
                case "DEFENSE_PREP":
                case "PREPARATION":
                    return "BLESSING";
                case "HAZARD":
                    return "DEBUFF";
                case "CERTIFIED_BATTLE":
                case "BATTLE":
                case "MONSTER":
                case "AMBUSH":
                case "ELITE":
                case "BOSS":
                    return "MONSTER";
                case "CHECK":
                case "CHALLENGE":
                case "CHANCE":
                    return "FATE";
                case "CIVIC_EVENT":
                case "EVENT":
                case "FATE":
                    return "FATE";
                case "DIPLOMACY":
                case "STORY":
                    return "STORY";
                case "NONCOMBAT_RESOLUTION":
                case "OBJECTIVE":
                    return reward.IndexOf(" XP", StringComparison.OrdinalIgnoreCase) >= 0
                        ? "XP"
                        : "SKILL";
                case "RESULTS":
                case "EXIT":
                case "RETURN":
                    return "RETURN";
                case "BRIEFING":
                case "START":
                    return "START";
                case "ROUTE":
                case "TRAIL":
                    return "CLUE";
                case "SKILL":
                case "XP":
                case "RECRUIT":
                case "ASCENSION":
                case "BLESSING":
                case "DEBUFF":
                    return StringComparer.Ordinal.Equals(kind, "ASCENSION")
                        ? "RECRUIT"
                        : kind;
            }
            return "CLUE";
        }

        static string BoardAdventureVisualLabel087(string visualKind)
        {
            switch (visualKind)
            {
                case "TREASURE": return "TREASURE ROOM";
                case "CAMPFIRE": return "CAMP & RECOVER";
                case "BLESSING": return "GUILD BOON";
                case "DEBUFF": return "HAZARD";
                case "MONSTER": return "BATTLE ROOM";
                case "FATE": return "FATE EVENT";
                case "STORY": return "STORY CARD";
                case "SKILL": return "DISCOVERY";
                case "XP": return "GUILD EXPERIENCE";
                case "RECRUIT": return "RECRUIT DISCOVERY";
                case "RETURN": return "ROAD HOME";
                case "START": return "QUEST BEGINS";
                default: return "ROUTE REVEALED";
            }
        }

        static string BoardAdventureVisualGlyph087(string visualKind)
        {
            switch (visualKind)
            {
                case "TREASURE": return "CHEST";
                case "CAMPFIRE": return "REST";
                case "BLESSING": return "+";
                case "DEBUFF": return "!";
                case "MONSTER": return "!";
                case "FATE": return "2D6";
                case "STORY": return "STORY";
                case "SKILL": return "ART";
                case "XP": return "XP";
                case "RECRUIT": return "HERO";
                case "RETURN": return "HOME";
                case "START": return "GO";
                default: return "PATH";
            }
        }

        IEnumerator RevealBoardAdventureReward084(
            BoardRewardToken084 token,
            float revealDelay, CommittedQuestDice132 dice132 = null)
        {
            if (token?.Root == null || token.Glow == null ||
                token.Surface == null) yield break;
            if (dice132 != null)
            {
                while (dice132 != null && !dice132.IsSettled132)
                {
                    if (!dice132.isActiveAndEnabled || token.Root == null) yield break;
                    yield return null;
                }
                if (dice132 == null) yield break;
                revealDelay = BoardAdventureRewardAfterDicePause084;
            }
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, revealDelay));
            if (token.Root == null || token.Glow == null ||
                token.Surface == null) yield break;
            var lidHome = token.Lid == null
                ? Vector2.zero
                : token.Lid.anchoredPosition;
            token.Root.localScale = new Vector3(0.76f, 0.76f, 1f);
            token.Glow.alpha = 0f;
            var elapsed = 0f;
            while (elapsed < BoardAdventureRewardRevealDuration084 &&
                   token.Root != null && token.Glow != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(
                    elapsed / BoardAdventureRewardRevealDuration084);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                var overshoot = 1f + Mathf.Sin(t * Mathf.PI) * 0.12f;
                token.Root.localScale = Vector3.one *
                    Mathf.Lerp(0.76f, overshoot, eased);
                token.Surface.alpha = Mathf.SmoothStep(
                    0f, 1f, Mathf.InverseLerp(0f, 0.34f, t));
                token.Glow.alpha = Mathf.Sin(t * Mathf.PI) * 0.90f;
                if (token.Lid != null)
                {
                    var open = Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(0.22f, 0.78f, t));
                    token.Lid.localEulerAngles = new Vector3(
                        0f, 0f, Mathf.Lerp(0f, -13f, open));
                    token.Lid.anchoredPosition = lidHome +
                                                  Vector2.up * Mathf.Lerp(0f, 4f, open);
                }
                yield return null;
            }
            if (token.Root != null) token.Root.localScale = Vector3.one;
            if (token.Surface != null) token.Surface.alpha = 1f;
            if (token.Glow != null) token.Glow.alpha = 0.45f;
            if (token.Lid != null)
            {
                token.Lid.localEulerAngles = new Vector3(0f, 0f, -13f);
                token.Lid.anchoredPosition = lidHome + new Vector2(0f, 4f);
            }
        }

        static string SignedBoardAdventure084(int value) =>
            value >= 0 ? "+" + value : value.ToString();
    }
}
