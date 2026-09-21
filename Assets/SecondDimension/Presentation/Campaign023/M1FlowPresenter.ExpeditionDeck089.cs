using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        bool _showExpeditionDeckDetails089;
        int _expeditionDeckTutorialStep089;

        void BuildExpeditionDeckHelp089(
            Transform body,
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            Campaign023.CampaignWorldGatePresentationState023 state)
        {
            var deckCoordinator = coordinator as
                Campaign023.IExpeditionDeckPresentationCoordinator089;
            if (state == null || deckCoordinator == null ||
                string.IsNullOrWhiteSpace(state.ActiveOperationId)) return;

            if (!state.ExpeditionDeckTutorialSeen)
            {
                var steps = state.ExpeditionDeckTutorialSteps ?? Array.Empty<string>();
                var stepIndex = steps.Count == 0
                    ? 0
                    : Mathf.Clamp(_expeditionDeckTutorialStep089, 0, steps.Count - 1);
                var copy = steps.Count == 0
                    ? "Choose one of three routes. The selected card resolves before the next row is drawn."
                    : steps[stepIndex];
                var tutorial = AddMessagePanel(body,
                    "EXPEDITION DECK TUTORIAL  •  " +
                    (stepIndex + 1) + " OF " + Math.Max(1, steps.Count),
                    copy, RuntimeUi.Accent);
                RuntimeUi.AddButton(tutorial,
                    "Acknowledge Expedition Deck tutorial 089",
                    steps.Count > 0 && stepIndex < steps.Count - 1
                        ? "NEXT  •  " + (stepIndex + 2) + " OF " + steps.Count
                        : "GOT IT  •  SHUFFLE MY THREE CARDS",
                    () =>
                    {
                        if (steps.Count > 0 && stepIndex < steps.Count - 1)
                        {
                            _expeditionDeckTutorialStep089 = stepIndex + 1;
                            BuildCurrentScreen();
                            return;
                        }
                        _expeditionDeckTutorialStep089 = 0;
                        RunGuildCityCommand017D(
                            deckCoordinator.AcknowledgeExpeditionDeckTutorial089);
                    },
                    104f, RuntimeUi.Positive);
                UseContentDrivenBoardPanelHeight084(tutorial);
            }

            var toggle = RuntimeUi.AddButton(body,
                "Toggle Expedition Deck details 089",
                _showExpeditionDeckDetails089
                    ? "CLOSE HELP & DECK DETAILS"
                    : "HOW EXPEDITIONS WORK  •  DECK DETAILS",
                () =>
                {
                    _showExpeditionDeckDetails089 = !_showExpeditionDeckDetails089;
                    BuildCurrentScreen();
                }, 86f, RuntimeUi.ButtonNormal);
            ConfigureResponsiveText062(toggle.GetComponentInChildren<Text>(),
                14, 22);
            if (!_showExpeditionDeckDetails089) return;

            var modifiers = state.DeckModifierLabels ?? Array.Empty<string>();
            var remaining = state.DeckRemainingCompositionLabels ??
                            Array.Empty<string>();
            var discarded = state.DeckDiscardCompositionLabels ??
                            Array.Empty<string>();
            var detailCopy =
                "DRAW PILE  " + state.DeckDrawPileCount +
                "  •  DISCARD  " + state.DeckDiscardCount +
                "  •  PASSED ROOMS  " + state.DeckBanishedCount +
                "\nREMAINING  •  " + (remaining.Count == 0
                    ? "No cards remain" : string.Join("  •  ", remaining)) +
                "\nDISCARD  •  " + (discarded.Count == 0
                    ? "Empty" : string.Join("  •  ", discarded)) +
                 "\nROUTE MOMENTUM  " +
                (state.DeckMomentum > 0 ? "+" : string.Empty) +
                 state.DeckMomentum +
                 "\nDECK GROWTH  •  TIER " +
                 Math.Max(1, state.DeckProgressionTier) + "  •  " +
                 Math.Max(6, state.DeckCardsPerNode) +
                 " CARDS PER SAFE ROOM" +
                 "\nUNLOCKED  •  " + string.Join("  •  ",
                     state.DeckUnlockedCategoryIds ?? Array.Empty<string>()) +
                 "\nDECK ITEMS  •  " + string.Join("  •  ", modifiers) +
                "\nOBJECTIVE  •  " + CompactWorldGateCopy084(
                    state.BoardObjective, "Reach the objective and return.", 170) +
                "\nCARD JOURNEY  •  MINIMUM " +
                Math.Max(SecondDimension.Gameplay.Campaign023.ExpeditionDeckService089
                    .MinimumQuestChoiceRounds089, state.DeckMinimumChoiceRounds) +
                " THREE-CARD CHOICES, PLUS LOCKED STORY BATTLES" +
                "\nCARD FLOW  •  SHUFFLE → DEAL THREE → CHOOSE → FLIP → " +
                "ROLL → CLAIM → ROUTE / NEXT ENCOUNTER";
            if ((state.ExpeditionRecruitLeadIds?.Count ?? 0) > 0)
                detailCopy += "\nRECRUIT LEADS EARNED  •  " +
                    state.ExpeditionRecruitLeadIds.Count +
                    " (sign them later at the Recruitment Desk)";
            var details = AddMessagePanel(body, "DECK DETAILS", detailCopy,
                RuntimeUi.ButtonNormal);
            UseContentDrivenBoardPanelHeight084(details);

            var tutorialSteps = state.ExpeditionDeckTutorialSteps ??
                                Array.Empty<string>();
            var replayCopy = tutorialSteps.Count == 0
                ? "Choose one route card, resolve it, then draw the next three."
                : string.Join("\n", tutorialSteps.Select((value, index) =>
                    (index + 1) + ".  " + value));
            var help = AddMessagePanel(body, "HOW EXPEDITIONS WORK", replayCopy,
                RuntimeUi.Accent);
            UseContentDrivenBoardPanelHeight084(help);

            var glossary = AddMessagePanel(body, "CARD GLOSSARY",
                "STORY ◇ advance the chapter  •  CHEST ▣ XP and loot  •  " +
                "BUFF ✦ route momentum  •  CAMP ⌂ recovery and preparation\n" +
                "HAZARD ⚠ higher risk and salvage  •  CHANCE ◆ physical 2D6 check  •  " +
                 "XP ↑ direct Guild and Hall growth  •  " +
                 "MERCHANT ◈ optional XP purchase with exact PWR/MYS preview  •  " +
                 "PERMANENT ✥ hero-bound Expedition boon or scar  •  " +
                 "RECRUIT ♟ unlock a hero lead  •  BATTLE ⚔ / AMBUSH ⟁ / ELITE ✧ / " +
                "BOSS ♛ full Union combat  •  " +
                "OBJECTIVE ◎ chapter progress",
                RuntimeUi.ButtonNormal);
            UseContentDrivenBoardPanelHeight084(glossary);
        }

        void BuildExpeditionRouteRow089(
            Transform body,
            Campaign023.IExpeditionDeckPresentationCoordinator089 coordinator,
            Campaign023.CampaignWorldGatePresentationState023 state)
        {
            var cards = (state.RouteCards ??
                         Array.Empty<Campaign023.ExpeditionRouteCardView089>())
                .Take(3).ToArray();
            if (cards.Length != 3)
            {
                AddMessagePanel(body, "SHUFFLING ROUTES",
                    "The saved Expedition Deck is rebuilding this room's three-card row. " +
                    "No route or reward was changed.", RuntimeUi.Warning);
                return;
            }

            var encounterRound = cards.All(value => value.IsEncounterRound);
            var choiceRound = Math.Max(1, state.DeckChoiceRound);
            var minimumRounds = Math.Max(
                SecondDimension.Gameplay.Campaign023.ExpeditionDeckService089
                    .MinimumQuestChoiceRounds089,
                state.DeckMinimumChoiceRounds);

            var headingPanel = RuntimeUi.AddPanel(body,
                "Expedition three card route heading 089",
                new Color(0.02f, 0.035f, 0.06f, 0.58f));
            RuntimeUi.SetLayout(headingPanel, preferredHeight: 1f);
            RuntimeUi.AddVerticalLayout(headingPanel.transform,
                new RectOffset(14, 14, 8, 8), 4f, TextAnchor.UpperCenter);
            var heading = RuntimeUi.AddText(headingPanel.transform,
                "Expedition route row heading text 089",
                (encounterRound
                    ? "ENCOUNTER ROUND " + choiceRound
                    : "ROUTE ROUND " + choiceRound) +
                "  •  " + minimumRounds + "+ CHOICES THIS QUEST",
                23, TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
            RuntimeUi.SetLayout(heading, preferredHeight: 38f);
            var dealStatus = RuntimeUi.AddText(headingPanel.transform,
                "Expedition shuffle and deal status 089",
                _reducedMotion
                    ? "THREE FACE-DOWN CARDS READY"
                    : "SHUFFLING THE EXPEDITION DECK…",
                22, TextAnchor.MiddleCenter, RuntimeUi.Warning,
                FontStyle.Bold);
            RuntimeUi.SetLayout(dealStatus, preferredHeight: 30f);
            var instruction = RuntimeUi.AddText(headingPanel.transform,
                "Expedition route row instruction 089",
                "Pick one mystery card. Turn it over to discover your encounter. The other two return unseen to the deck.",
                22, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Normal);
            RuntimeUi.SetLayout(instruction, preferredHeight: 34f);
            UseContentDrivenBoardPanelHeight084(headingPanel.transform);

            var row = AddRow(body, "Expedition route row 089", 12f, 830f);
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(100, 100, 0, 0);
            var blindChoice = row.gameObject.AddComponent<Campaign023.ExpeditionCardChoice091>();
            blindChoice.Configure091(!_reducedMotion && Application.isPlaying, dealStatus);
            var minimumCardHeight156E = 1f;
            for (var cardIndex = 0; cardIndex < cards.Length; cardIndex++)
            {
                var card = cards[cardIndex];
                var captured = card;
                var wrapper = RuntimeUi.AddPanel(row,
                    "Expedition route card " + card.CardId + " 089", Color.clear);
                RuntimeUi.SetLayout(wrapper, preferredHeight: 810f,
                    flexibleWidth: 1f);
                var panel = RuntimeUi.AddPanel(wrapper.transform,
                    "Expedition route card surface " + card.CardId + " 089",
                    Color.white);
                Stretch(panel.rectTransform);
                M1PremiumUi.StylePanel(panel, SurfaceForCard089(card.Category));
                RuntimeUi.AddVerticalLayout(panel.transform,
                    new RectOffset(10, 10, 10, 12), 6f, TextAnchor.UpperCenter);
                var cardMotion = wrapper.gameObject.AddComponent<
                    Campaign023.ExpeditionRouteCardMotion089>();
                cardMotion.Configure089(cardIndex, card.CardId, !_reducedMotion);

                var category = RuntimeUi.AddText(panel.transform,
                    "Expedition card category " + card.CardId + " 089",
                    card.Category + "  •  " + card.RiskLabel,
                    25, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
                RuntimeUi.SetLayout(category, preferredHeight: 30f);
                ConfigureResponsiveText062(category, 18, 24);

                // The saved target ID selects existing exact-identity art. Card
                // titles/names never supply an identity or change a reward.
                Sprite sprite = null;
                if (!StringComparer.Ordinal.Equals(card.Category, "RECRUIT") ||
                    !HeroRemasterAtlas093.TryResolve093(card.RecruitStableId,
                        false, out sprite, out _))
                    sprite = string.IsNullOrWhiteSpace(card.VisualResourcePath)
                        ? null : Resources.Load<Sprite>(card.VisualResourcePath);
                if (sprite != null)
                {
                    var face = GuildCity017E.GuildCityOpeningExperienceRegistry017E.AddImage(
                        panel.transform,
                        "Expedition card face " + card.CardId + " 089",
                        card.VisualResourcePath, 190f);
                    face.sprite = sprite;
                }
                else
                {
                    var fallback = RuntimeUi.AddText(panel.transform,
                        "Expedition card fallback face " + card.CardId + " 089",
                        CardGlyph089(card.Category), 52, TextAnchor.MiddleCenter,
                        ColorForCard089(card.Category), FontStyle.Bold);
                    RuntimeUi.SetLayout(fallback, preferredHeight: 190f);
                }

                var title = RuntimeUi.AddText(panel.transform,
                    "Expedition card title " + card.CardId + " 089",
                    card.Title.ToUpperInvariant(), 29, TextAnchor.MiddleCenter,
                    Color.white, FontStyle.Bold);
                RuntimeUi.SetLayout(title, preferredHeight: 56f);
                ConfigureResponsiveText062(title, 20, 28);

                var description = RuntimeUi.AddText(panel.transform,
                    "Expedition card description " + card.CardId + " 089",
                    CompactWorldGateCopy084(card.Description,
                        "Advance the expedition.", 100),
                    22, TextAnchor.UpperCenter, RuntimeUi.Text, FontStyle.Normal);
                RuntimeUi.SetLayout(description, preferredHeight: 66f);
                ConfigureResponsiveText062(description, 18, 23);
                if (!card.CanChoose && !string.IsNullOrWhiteSpace(card.LockedReason))
                    description.gameObject.SetActive(false);

                var odds = RuntimeUi.AddText(panel.transform,
                    "Expedition card odds " + card.CardId + " 089",
                    card.Odds, 23, TextAnchor.MiddleCenter,
                    card.SuccessBasisPoints >= 5000
                        ? RuntimeUi.Positive : RuntimeUi.Warning,
                    FontStyle.Bold);
                RuntimeUi.SetLayout(odds, preferredHeight: 50f);
                ConfigureResponsiveText062(odds, 18, 24);

                var outcomes = RuntimeUi.AddText(panel.transform,
                    "Expedition card possible results " + card.CardId + " 089",
                    string.IsNullOrWhiteSpace(card.OutcomePreview)
                        ? "SUCCESS  •  FULL REWARD  |  SETBACK  •  REDUCED REWARD"
                        : card.OutcomePreview.ToUpperInvariant(),
                    20, TextAnchor.MiddleCenter, RuntimeUi.Text,
                    FontStyle.Bold);
                RuntimeUi.SetLayout(outcomes, preferredHeight: 48f);
                ConfigureResponsiveText062(outcomes, 17, 21);

                var reward = RuntimeUi.AddText(panel.transform,
                    "Expedition card reward " + card.CardId + " 089",
                    "REWARD  •  " + CompactWorldGateCopy084(
                        card.RewardPreview, "Story progress", 90),
                    22, TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
                RuntimeUi.SetLayout(reward, preferredHeight: 58f);
                ConfigureResponsiveText062(reward, 18, 23);

                var route = RuntimeUi.AddText(panel.transform,
                    "Expedition card route " + card.CardId + " 089",
                    (card.IsEncounterRound ? "ENCOUNTER  •  " : "ROUTE  •  ") +
                    card.RouteLabel, 18,
                    TextAnchor.MiddleCenter, RuntimeUi.MutedText,
                    FontStyle.Italic);
                RuntimeUi.SetLayout(route, preferredHeight: 40f);
                route.gameObject.SetActive(false);

                 var button = RuntimeUi.AddButton(panel.transform,
                    "Choose Expedition route card " + card.CardId + " 089",
                    card.TreasuryXpCost > 0
                        ? "BUY FOR " + card.TreasuryXpCost + " XP"
                        : card.RequiresCertifiedBattle
                            ? "TAKE CARD  •  FACE THE THREAT"
                            : card.Category == "PERMANENT"
                                ? "ACCEPT " +
                                  (card.PermanentHeroEffectKind ?? "FATE")
                                : (card.IsEncounterRound
                                      ? "TAKE CARD  •  "
                                      : "CHOOSE ROUTE  •  ") +
                                  card.Category,
                    () => RunWorldGateAnimatedCommand084(() =>
                            coordinator.CommitExpeditionRouteCard089(captured.CardId),
                        "That route card could not be resolved. Draw again."),
                    88f, RuntimeUi.Accent);
                 ConfigureResponsiveText062(button.GetComponentInChildren<Text>(),
                     20, 27);
                if (!card.CanChoose &&
                    !string.IsNullOrWhiteSpace(card.LockedReason))
                {
                    var locked = RuntimeUi.AddText(panel.transform,
                        "Expedition card locked reason " + card.CardId + " 089",
                        card.LockedReason, 16, TextAnchor.MiddleCenter,
                        RuntimeUi.Warning, FontStyle.Bold);
                    RuntimeUi.SetLayout(locked, preferredHeight: 40f);
                }
                button.interactable = false;
                var relay = button.gameObject.AddComponent<
                    Campaign023.ExpeditionRouteCardButtonRelay089>();
                relay.Bind089(cardMotion);
                blindChoice.Register091(wrapper.rectTransform, panel.rectTransform,
                    button, captured.CanChoose ||
                        (captured.TreasuryXpCost <= 0 && string.IsNullOrWhiteSpace(captured.LockedReason)),
                    () => RunWorldGateAnimatedCommand084(() =>
                            coordinator.CommitExpeditionRouteCard089(captured.CardId),
                        "That route card could not be resolved. Draw again."),
                    Resources.Load<Sprite>(BoardAdventureCardFrameResource084),
                    captured.TreasuryXpCost > 0 || captured.Category == "RECRUIT");
                ArrangeIllustratedBoardCard091(panel.rectTransform,
                    "Expedition card face ", true);
                if (StringComparer.Ordinal.Equals(card.Category, "CHEST"))
                    UseClosedChestCardArt092(panel.rectTransform, "Expedition card face ");
                minimumCardHeight156E = Mathf.Max(minimumCardHeight156E,
                    FitExpeditionRouteReadingColumn110(panel.rectTransform));
            }
            var scroll = body.GetComponentInParent<ScrollRect>();
            if (scroll != null && scroll.content == body)
                row.gameObject.AddComponent<Campaign023.ExpeditionRouteViewport110>()
                    .Configure110(row, scroll, minimumCardHeight156E);
            foreach (var prompt in row.GetComponentsInChildren<Text>(true)
                .Where(value => value.name.StartsWith("Blind Quest Card Pick Prompt ", StringComparison.Ordinal)))
                ConfigureAuthoredCompactText076(prompt, 22, 30);
        }

        // Keep the original actions and a usable disclosure viewport. The outer
        // page scroll may grow when its available height cannot hold both.
        static float FitExpeditionRouteReadingColumn110(RectTransform face)
        {
            var reading = face.Find("Board Card Reading Column 091") as RectTransform;
            if (reading == null) return 1f;
            var layout = reading.GetComponent<VerticalLayoutGroup>();
            if (layout != null) layout.enabled = false;
            var children = reading.Cast<Transform>().ToArray();
            var buttons = children.Where(value => value.GetComponent<Button>() != null).ToArray();
            // Faces are inactive until turned; LayoutUtility ignores their
            // disabled hierarchy, so use each existing control's authored limits.
            var actionHeight = buttons.Sum(value => Mathf.Max(
                value.GetComponent<LayoutElement>().minHeight,
                value.GetComponent<LayoutElement>().preferredHeight)) +
                Math.Max(0, buttons.Length - 1) * 8f;
            var actions = RuntimeUi.AddStretchRect(reading, "Expedition Route Card Actions 110");
            actions.anchorMax = new Vector2(1f, 0f);
            actions.pivot = new Vector2(0.5f, 0f);
            actions.anchoredPosition = Vector2.zero;
            actions.sizeDelta = new Vector2(0f, actionHeight);
            RuntimeUi.AddVerticalLayout(actions, new RectOffset(0, 0, 0, 0), 8f, TextAnchor.LowerCenter);
            foreach (var button in buttons)
            {
                button.SetParent(actions, false);
                ConfigureAuthoredCompactText076(button.GetComponentInChildren<Text>(true), 22, 30);
            }

            var viewport = RuntimeUi.AddPanel(reading, "Expedition Route Card Copy Viewport 110", Color.clear);
            Stretch(viewport.rectTransform);
            viewport.rectTransform.offsetMin = new Vector2(0f, actionHeight + 12f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = RuntimeUi.AddStretchRect(viewport.transform, "Expedition Route Card Copy 110");
            content.anchorMin = new Vector2(0f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            RuntimeUi.AddVerticalLayout(content, new RectOffset(4, 4, 8, 8), 10f, TextAnchor.UpperLeft);
            foreach (var child in children)
            {
                if (child.GetComponent<Button>() != null) continue;
                child.SetParent(content, false);
                var text = child.GetComponent<Text>();
                if (text == null) continue;
                // Let Text report its actual wrapped height at the column width.
                // The old character-count estimate clipped longer disclosures.
                var element = text.GetComponent<LayoutElement>();
                if (element != null)
                {
                    element.minHeight = -1f;
                    element.preferredHeight = -1f;
                    element.flexibleHeight = 0f;
                }
                text.resizeTextForBestFit = false;
                text.verticalOverflow = VerticalWrapMode.Overflow;
            }
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36f;
            AddPremiumScrollbar(viewport.transform, scroll);
            // This existing ScrollRect uses its own RectTransform as viewport,
            // so reserve the thumb's width explicitly instead of relying on expansion.
            content.offsetMax = new Vector2(-34f, content.offsetMax.y);
            const float minimumCopyHeight156E = 240f;
            var readingFraction156E = Mathf.Max(.01f, reading.anchorMax.y - reading.anchorMin.y);
            return (actionHeight + 12f + minimumCopyHeight156E - reading.sizeDelta.y) / readingFraction156E;
        }

        static M1PremiumUi.Surface SurfaceForCard089(string category)
        {
            switch (category)
            {
                case "CHEST": return M1PremiumUi.Surface.Vellum;
                case "MERCHANT": return M1PremiumUi.Surface.Vellum;
                case "PERMANENT": return M1PremiumUi.Surface.WorldRibbon;
                case "BUFF":
                case "CAMP": return M1PremiumUi.Surface.Positive;
                case "XP": return M1PremiumUi.Surface.Positive;
                case "HAZARD":
                case "BATTLE": return M1PremiumUi.Surface.Warning;
                case "AMBUSH": return M1PremiumUi.Surface.Iron;
                case "ELITE": return M1PremiumUi.Surface.Vellum;
                case "BOSS": return M1PremiumUi.Surface.WorldRibbon;
                case "RECRUIT": return M1PremiumUi.Surface.WorldPaper;
                case "ASCENSION": return M1PremiumUi.Surface.WorldRibbon;
                case "OBJECTIVE": return M1PremiumUi.Surface.WorldRibbon;
                default: return M1PremiumUi.Surface.EtchedGlass;
            }
        }

        static Color ColorForCard089(string category)
        {
            switch (category)
            {
                case "CHEST": return new Color(0.95f, 0.72f, 0.2f);
                case "MERCHANT": return new Color(0.35f, 0.86f, 1f);
                case "PERMANENT": return new Color(0.86f, 0.57f, 1f);
                case "BUFF": return new Color(0.35f, 0.95f, 0.72f);
                case "XP": return new Color(0.98f, 0.79f, 0.28f);
                case "HAZARD": return new Color(1f, 0.38f, 0.28f);
                case "RECRUIT": return new Color(0.63f, 0.72f, 1f);
                case "ASCENSION": return new Color(0.86f, 0.57f, 1f);
                case "BATTLE": return new Color(1f, 0.2f, 0.18f);
                case "AMBUSH": return new Color(1f, 0.48f, 0.18f);
                case "ELITE": return new Color(1f, 0.76f, 0.26f);
                case "BOSS": return new Color(1f, 0.34f, 0.48f);
                default: return RuntimeUi.Accent;
            }
        }

        static string CardGlyph089(string category)
        {
            switch (category)
            {
                case "CHEST": return "▣";
                case "MERCHANT": return "◈";
                case "PERMANENT": return "✥";
                case "BUFF": return "✦";
                case "HAZARD": return "⚠";
                case "CHANCE": return "◆";
                case "XP": return "↑";
                case "RECRUIT": return "♟";
                case "ASCENSION": return "★";
                case "CAMP": return "⌂";
                case "BATTLE": return "⚔";
                case "AMBUSH": return "⟁";
                case "ELITE": return "✧";
                case "BOSS": return "♛";
                case "OBJECTIVE": return "◎";
                default: return "◇";
            }
        }
    }
}
