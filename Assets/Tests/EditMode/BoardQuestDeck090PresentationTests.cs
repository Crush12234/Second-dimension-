using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BoardQuestDeck090PresentationTests
    {
        [TestCase(480f, false)]
        [TestCase(640f, false)]
        [TestCase(480f, true)]
        [TestCase(640f, true)]
        public void NarrowSpeakerStripKeepsPortraitNameAndEntireQuoteReadable091(
            float width, bool chapterTwo)
        {
            var root = new GameObject("Speaker Layout Test 091", typeof(RectTransform));
            try
            {
                var rootRect = root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(width, 500f);
                var layout = InvokeRuntimeUi091<VerticalLayoutGroup>("AddVerticalLayout", root.transform,
                    new RectOffset(), 0f, TextAnchor.MiddleCenter);
                layout.childForceExpandHeight = false;
                var panel = InvokeRuntimeUi091<Image>("AddPanel", root.transform,
                    "Expedition Companion Story Beat 076", Color.white);
                InvokeRuntimeUi091<LayoutElement>("SetLayout", panel, -1f, 104f, -1f, -1f);
                const string words = "GUILDMATE  •  ODELIA • The Wayglass pulse is bending the old chain. Ground it now before that hazard follows us into battle.";
                var quote = InvokeRuntimeUi091<Text>("AddText", panel.transform,
                    "Expedition Companion Story Beat Text 076", words, 21,
                    TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
                quote.rectTransform.offsetMin = new Vector2(340f, 10f);
                var identity = InvokeRuntimeUi091<Image>("AddPanel", panel.transform,
                    chapterTwo ? "Chapter Two Story Identity Warden Ilyra 079" :
                        "Expedition Companion Identity Chip 078", Color.white);
                var name = InvokeRuntimeUi091<Text>("AddText", identity.transform,
                    chapterTwo ? "Chapter Two Story Identity Text Warden Ilyra 079" : "Label",
                    chapterTwo ? "WARDEN ILYRA\nGATEWATCH CAPTAIN" : "ODELIA", 24,
                    TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
                var portrait = InvokeRuntimeUi091<Image>("AddPanel", identity.transform,
                    chapterTwo ? "Chapter Two Story Portrait Warden Ilyra 079" :
                        "Portrait Frame Odelia", Color.white);
                portrait.sprite = Resources.Load<Sprite>(GuildQuestCardPresentation090.VisualResourcePath090("RECRUIT"));
                var originalArt = portrait.sprite;
                var originalName = name.text;

                var arrange = typeof(M1FlowPresenter).GetMethod("ArrangeBoardSpeakerStrip091",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(arrange, Is.Not.Null);
                arrange.Invoke(null, new object[] { panel.rectTransform });
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);

                Assert.That(quote.text, Is.EqualTo(words));
                Assert.That(name.text, Is.EqualTo(originalName));
                Assert.That(portrait.sprite, Is.SameAs(originalArt));
                Assert.That(portrait.rectTransform.rect.width,
                    Is.EqualTo(portrait.rectTransform.rect.height).Within(0.01f));
                Assert.That(portrait.rectTransform.rect.height, Is.GreaterThanOrEqualTo(58f));
                Assert.That(quote.rectTransform.rect.width,
                    Is.GreaterThanOrEqualTo(panel.rectTransform.rect.width - 25f),
                    "The old 340px indent must not survive in the narrow reading column.");
                var panelBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(rootRect, panel.rectTransform);
                var quoteBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(rootRect, quote.rectTransform);
                var identityBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(rootRect, identity.rectTransform);
                Assert.That(quoteBounds.max.y, Is.LessThan(identityBounds.min.y));
                Assert.That(quoteBounds.min.y, Is.GreaterThanOrEqualTo(panelBounds.min.y));
                Assert.That(identityBounds.max.y, Is.LessThanOrEqualTo(panelBounds.max.y));
                foreach (var text in new[] { quote, name })
                {
                    var settings = text.GetGenerationSettings(text.rectTransform.rect.size);
                    settings.resizeTextForBestFit = false;
                    settings.fontSize = 18;
                    var requiredHeight = text.cachedTextGeneratorForLayout.GetPreferredHeight(text.text, settings) /
                                         text.pixelsPerUnit;
                    Assert.That(requiredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 0.5f),
                        text.name + " must fit completely at a readable font size, not merely truncate overflow.");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static T InvokeRuntimeUi091<T>(string methodName, params object[] arguments)
        {
            var uiType = typeof(M1FlowPresenter).Assembly.GetType("SecondDimension.Presentation.RuntimeUi");
            Assert.That(uiType, Is.Not.Null);
            var method = uiType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            Assert.That(method, Is.Not.Null);
            return (T)method.Invoke(null, arguments);
        }

        [TestCase("CHEST", "CARD_FACE_CHEST_089")]
        [TestCase("MERCHANT", "CARD_FACE_CAMP_089")]
        [TestCase("RECRUIT", "CARD_FACE_RECRUIT_089")]
        [TestCase("BOON", "CARD_FACE_BUFF_089")]
        [TestCase("SCAR", "CARD_FACE_HAZARD_089")]
        [TestCase("FATE", "CARD_FACE_CHANCE_089")]
        [TestCase("BATTLE", "CARD_FACE_BATTLE_089")]
        [TestCase("XP", "CARD_FACE_BUFF_089")]
        public void EveryQuestCardCategoryHasAnAuthoredFace090(
            string category,
            string expectedAsset)
        {
            var path = GuildQuestCardPresentation090
                .VisualResourcePath090(category);

            Assert.That(path, Does.EndWith(expectedAsset));
            Assert.That(Resources.Load<Sprite>(path), Is.Not.Null,
                category + " must never appear as a blank programmer-art card.");
        }

        [Test]
        public void FullWidthDraftHidesThreeOffersUntilDeliberatePick091()
        {
            var presenterObject = new GameObject(
                "Quest Deck Presenter Test 090");
            var rootObject = new GameObject(
                "Quest Deck Root Test 090",
                typeof(RectTransform));
            try
            {
                var state = DeckState090();
                var coordinator = new DeckCoordinator090(state);
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                var method = typeof(M1FlowPresenter).GetMethod(
                    "BuildBoardQuestCardDraft090",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(method, Is.Not.Null);
                method.Invoke(presenter, new object[]
                {
                    rootObject.transform,
                    coordinator,
                    state
                });

                var visible = string.Join("\n",
                    rootObject.GetComponentsInChildren<Text>()
                        .Where(value => value != null)
                        .Select(value => value.text));
                Assert.That(visible, Does.Contain(
                    "CARD ROUND 4  •  10+ CARD ROUNDS THIS QUEST"));
                Assert.That(visible, Does.Contain(
                    "PICK 1 OF 3"));
                var currentRoomStory = rootObject
                    .GetComponentsInChildren<Text>(true)
                    .Single(value => value.gameObject.name.StartsWith(
                        "Expedition Current Position Summary 074",
                        StringComparison.Ordinal));
                Assert.That(currentRoomStory.text, Does.StartWith("NOW  •  "));
                Assert.That(currentRoomStory.text.Length, Is.GreaterThan(24),
                    "The three-card table must keep the current room's story visible while the next choice is dealt.");
                Assert.That(visible, Does.Not.Contain(
                    "COMBAT POWER  •  PWR +11  •  MYS +4"));
                Assert.That(visible, Does.Not.Contain(
                    "COST 45 XP (HAVE 30)"));
                Assert.That(visible, Does.Not.Contain(
                    "Need 45 Guild XP").IgnoreCase);
                Assert.That(visible, Does.Not.Contain(
                    "PHYSICAL 2D6  •  QUEST +0  •  TARGET 7"));
                Assert.That(visible, Does.Not.Contain("QUEST +0"));
                Assert.That(visible, Does.Not.Contain("NEXT: SKYGLASS CAUSEWAY"));
                Assert.That(visible, Does.Not.Contain("NEXT: LANTERN ARCHIVE"));
                Assert.That(visible, Does.Not.Contain("NEXT: GATEWATCH MARKET"));

                var cardButtons = rootObject
                    .GetComponentsInChildren<Button>(true)
                    .Where(value => value.gameObject.name.StartsWith(
                        "Choose Board Quest Card ",
                        StringComparison.Ordinal))
                    .ToArray();
                Assert.That(cardButtons, Has.Length.EqualTo(3));
                Assert.That(cardButtons.Count(value => value.interactable),
                    Is.Zero, "Hidden offers cannot accept an action.");
                var picks = rootObject.GetComponentsInChildren<Button>()
                    .Where(value => value.name.StartsWith("Blind Quest Card Back "))
                    .ToArray();
                Assert.That(picks, Has.Length.EqualTo(3));
                Assert.That(picks.All(value => value.interactable), Is.True,
                    "An unaffordable card must not identify itself before the blind pick.");
                Assert.That(coordinator.CommitCount, Is.Zero);
                rootObject.GetComponentInChildren<ExpeditionCardChoice091>()
                    .Choose091(0);
                Assert.That(coordinator.CommitCount, Is.Zero);
                visible = string.Join("\n", rootObject.GetComponentsInChildren<Text>()
                    .Select(value => value.text));
                Assert.That(visible, Does.Contain("COMBAT POWER  •  PWR +11  •  MYS +4"));
                Assert.That(visible, Does.Not.Contain("COST 45 XP (HAVE 30)"));

                var selectedFace = rootObject.GetComponentsInChildren<RectTransform>()
                    .Single(value => value.name == "Board Quest Card Surface QUESTCARD090_CHEST 090");
                var illustration = selectedFace.Find("Board Card Illustration Window 091") as RectTransform;
                var reading = selectedFace.Find("Board Card Reading Column 091") as RectTransform;
                Assert.That(illustration, Is.Not.Null);
                Assert.That(reading, Is.Not.Null);
                Assert.That(illustration.anchorMax.y - illustration.anchorMin.y,
                    Is.GreaterThanOrEqualTo(0.90f), "The artwork should occupy the card's height.");
                Assert.That(illustration.anchorMax.x, Is.LessThan(reading.anchorMin.x));
                Assert.That(illustration.GetComponent<RectMask2D>(), Is.Not.Null,
                    "An aspect-preserving cover crop needs a real image viewport.");
                Assert.That(reading.GetComponentsInChildren<Button>().Any(value =>
                    value.name == "Choose Board Quest Card QUESTCARD090_CHEST 090" && value.interactable), Is.True);
                Assert.That(selectedFace.GetComponent<Image>().color.g,
                    Is.LessThanOrEqualTo(selectedFace.GetComponent<Image>().color.b),
                    "The result should use a neutral surface, not a large green category fill.");

                Assert.That(cardButtons.Any(value =>
                    value.GetComponentInChildren<Text>(true)?.text ==
                    "OPEN CHEST"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        [Test]
        public void ThreeCardDealFocusesFirstBlindCardWithoutRevealingIt091()
        {
            var presenterObject = new GameObject(
                "Quest Deck Focus Presenter Test 090");
            var rootObject = new GameObject(
                "Quest Deck Focus Root Test 090",
                typeof(RectTransform));
            var eventSystemObject = EventSystem.current == null
                ? new GameObject(
                    "Quest Deck Focus Event System Test 090",
                    typeof(EventSystem),
                    typeof(StandaloneInputModule))
                : null;
            try
            {
                var eventSystem = UnityEngine.Object
                    .FindFirstObjectByType<EventSystem>();
                Assert.That(eventSystem, Is.Not.Null);
                if (!Application.isPlaying && !eventSystem.runInEditMode)
                    eventSystem.runInEditMode = true;
                if (EventSystem.current == null)
                {
                    eventSystem.enabled = false;
                    eventSystem.enabled = true;
                }

                var state = DeckState090();
                var coordinator = new DeckCoordinator090(state);
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                var method = typeof(M1FlowPresenter).GetMethod(
                    "BuildBoardQuestCardDraft090",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(method, Is.Not.Null);
                method.Invoke(presenter, new object[]
                {
                    rootObject.transform,
                    coordinator,
                    state
                });

                var expected = rootObject
                    .GetComponentsInChildren<Button>(true)
                    .First(value => value.gameObject.name ==
                        "Blind Quest Card Back 0 091");
                Assert.That(expected.interactable, Is.True);
                Assert.That(EventSystem.current, Is.Not.Null,
                    "The edit-mode harness must activate the runtime EventSystem lifecycle before testing controller focus.");
                Assert.That(EventSystem.current.currentSelectedGameObject,
                    Is.SameAs(expected.gameObject),
                    "The first face-down card must take controller focus after the deal without revealing an offer.");
            }
            finally
            {
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(null);
                UnityEngine.Object.DestroyImmediate(rootObject);
                UnityEngine.Object.DestroyImmediate(presenterObject);
                if (eventSystemObject != null)
                    UnityEngine.Object.DestroyImmediate(eventSystemObject);
            }
        }

        [Test]
        public void QuestDeckCommandIsOptionalAndProductionImplementsIt090()
        {
            Assert.That(
                typeof(IBoardQuestDeckCoordinator090).IsAssignableFrom(
                    typeof(IGuildCityPresentationCoordinator017D)),
                Is.False,
                "Legacy Guild City test fakes must not gain a new required method.");
            Assert.That(
                typeof(IBoardQuestDeckCoordinator090).IsAssignableFrom(
                    typeof(M1RuntimeCoordinator)),
                Is.True,
                "The production runtime must commit the real saved card command.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SuccessfulNonPlayingCommitClearsTransientCard090(
            bool reducedMotion)
        {
            var presenterObject = new GameObject(
                "Quest Deck Transient Result Test 090");
            try
            {
                var state = DeckState090();
                var coordinator = new DeckCoordinator090(state);
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                var reduced = typeof(M1FlowPresenter).GetField(
                    "_reducedMotion",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var commit = typeof(M1FlowPresenter).GetMethod(
                    "CommitBoardQuestCard090",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var transient = typeof(M1FlowPresenter).GetField(
                    "_lastBoardQuestCard090",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var acknowledgement = typeof(M1FlowPresenter).GetField(
                    "_boardQuestCardAwaitingAcknowledgement090",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(reduced, Is.Not.Null);
                Assert.That(commit, Is.Not.Null);
                Assert.That(transient, Is.Not.Null);
                Assert.That(acknowledgement, Is.Not.Null);
                reduced.SetValue(presenter, reducedMotion);
                commit.Invoke(presenter, new object[]
                {
                    coordinator,
                    coordinator,
                    state.QuestCards090[0]
                });

                Assert.That(coordinator.CommitCount, Is.EqualTo(1));
                Assert.That(transient.GetValue(presenter), Is.Null,
                    "A skipped reveal must not be resurrected by a later board hold.");
                Assert.That(acknowledgement.GetValue(presenter), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        [Test]
        public void ReducedMotionResolutionShowsStaticFateResultUntilContinue090()
        {
            var presenterObject = new GameObject(
                "Quest Deck Reduced Motion Result Test 090");
            var rootObject = new GameObject(
                "Quest Deck Reduced Motion Root 090",
                typeof(RectTransform));
            try
            {
                var state = DeckState090();
                var fate = state.QuestCards090.Single(value =>
                    StringComparer.Ordinal.Equals(value.Category, "FATE"));
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                var reduced = typeof(M1FlowPresenter).GetField(
                    "_reducedMotion",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var transient = typeof(M1FlowPresenter).GetField(
                    "_lastBoardQuestCard090",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var acknowledgement = typeof(M1FlowPresenter).GetField(
                    "_boardQuestCardAwaitingAcknowledgement090",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var policy = typeof(M1FlowPresenter).GetMethod(
                    "ShouldRequireBoardQuestCardAcknowledgement090",
                    BindingFlags.Static | BindingFlags.NonPublic);
                var build = typeof(M1FlowPresenter).GetMethod(
                    "BuildBoardQuestCardResolution090",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(reduced, Is.Not.Null);
                Assert.That(transient, Is.Not.Null);
                Assert.That(acknowledgement, Is.Not.Null);
                Assert.That(policy, Is.Not.Null);
                Assert.That(build, Is.Not.Null);
                Assert.That((bool)policy.Invoke(null, new object[]
                    { true, true }), Is.True,
                    "Actual Play Mode plus reduced motion must require acknowledgment.");
                Assert.That((bool)policy.Invoke(null, new object[]
                    { true, false }), Is.False,
                    "Normal Play Mode keeps its existing timed reveal lifecycle.");
                Assert.That((bool)policy.Invoke(null, new object[]
                    { false, true }), Is.False,
                    "Non-playing test or editor calls must not retain stale state.");

                reduced.SetValue(presenter, true);
                transient.SetValue(presenter, fate);
                acknowledgement.SetValue(presenter, true);
                build.Invoke(presenter, new object[]
                {
                    rootObject.transform,
                    state
                });

                var visible = string.Join("\n",
                    rootObject.GetComponentsInChildren<Text>(true)
                        .Where(value => value != null)
                        .Select(value => value.text));
                Assert.That(visible,
                    Does.Contain("DICE SETTLED  •  SAVED RESULT"));
                Assert.That(visible,
                    Does.Contain("DICE TOTAL 9  •  PASS"));
                Assert.That(visible,
                    Does.Contain("REVIEW THE RESULT, THEN CONTINUE"));
                var acknowledge = rootObject
                    .GetComponentsInChildren<Button>(true)
                    .Single(value => StringComparer.Ordinal.Equals(
                        value.gameObject.name,
                        "Acknowledge Board Quest Card Resolution 090"));
                Assert.That(acknowledge.GetComponentInChildren<Text>().text,
                    Is.EqualTo("CONTINUE"));
                Assert.That(transient.GetValue(presenter), Is.SameAs(fate),
                    "The result must remain visible before acknowledgment.");

                acknowledge.onClick.Invoke();

                Assert.That(transient.GetValue(presenter), Is.Null,
                    "Continue must release the static result exactly once.");
                Assert.That(acknowledgement.GetValue(presenter), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        [Test]
        public void LegacyShortDeckUsesHonestChooseOneOfThreeHeader090()
        {
            var presenterObject = new GameObject(
                "Quest Deck Legacy Header Test 090");
            var rootObject = new GameObject(
                "Quest Deck Legacy Header Root 090",
                typeof(RectTransform));
            try
            {
                var state = DeckState090();
                state.QuestCardMinimumRounds090 = 3;
                var coordinator = new DeckCoordinator090(state);
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                var build = typeof(M1FlowPresenter).GetMethod(
                    "BuildBoardQuestCardDraft090",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(build, Is.Not.Null);
                build.Invoke(presenter, new object[]
                {
                    rootObject.transform,
                    coordinator,
                    state
                });
                var visible = string.Join("\n",
                    rootObject.GetComponentsInChildren<Text>(true)
                        .Where(value => value != null)
                        .Select(value => value.text));

                Assert.That(visible,
                    Does.Contain("CHOOSE 1 OF 3 • CARD ADVENTURE"));
                Assert.That(visible,
                    Does.Not.Contain("10+ CARD ROUNDS THIS QUEST"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        [Test]
        public void SavedQuestBoonsAndScarsReachTheSubmittedDiceModifier090()
        {
            var presenterObject = new GameObject(
                "Quest Deck Modifier Presenter Test 090");
            try
            {
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                var baseline = CheckState090(Array.Empty<string>());
                var changed = CheckState090(new[]
                {
                    SecondDimension.Gameplay.GuildCity017D
                        .GuildCityExpeditionService017D
                        .QuestCardRunBoonPrefix090 + "ONE",
                    SecondDimension.Gameplay.GuildCity017D
                        .GuildCityExpeditionService017D
                        .QuestCardRunBoonPrefix090 + "TWO",
                    SecondDimension.Gameplay.GuildCity017D
                        .GuildCityExpeditionService017D
                        .QuestCardRunScarPrefix090 + "ONE"
                });

                Assert.That(CrewModifier090(presenter, changed),
                    Is.EqualTo(CrewModifier090(presenter, baseline) + 1),
                    "Two saved boons and one saved scar must produce a real +1 " +
                    "on the command submitted to the authoritative dice service.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        private static GuildCityPresentationState017D DeckState090() =>
            new GuildCityPresentationState017D
            {
                IsAvailable = true,
                HasActiveContract = true,
                TreasuryXp = 30,
                QuestCardRound090 = 4,
                QuestCardMinimumRounds090 = 10,
                QuestCards090 = new[]
                {
                    new GuildQuestCardView090
                    {
                        CardId = "QUESTCARD090_CHEST",
                        Category = "CHEST",
                        Title = "Rare Skyglass Chest",
                        Description = "Open a physical chest and keep its weapon.",
                        RewardPreview =
                            "Rare Skyglass Blade • PWR +11 • MYS +4 • +4 XP",
                        RiskLabel = "SAFE",
                        DestinationLabel = "Skyglass Causeway",
                        RoutePreview = "NEXT: SKYGLASS CAUSEWAY",
                        RarityId = "QUALITY_RARE",
                        ItemName = "Rare Skyglass Blade",
                        PhysicalPower = 11,
                        MysticPower = 4,
                        TreasuryXpDelta = 4,
                        CanChoose = true,
                        VisualResourcePath = GuildQuestCardPresentation090
                            .VisualResourcePath090("CHEST")
                    },
                    new GuildQuestCardView090
                    {
                        CardId = "QUESTCARD090_MERCHANT",
                        Category = "MERCHANT",
                        Title = "Wayglass Merchant",
                        Description = "Buy a permanent weapon with Guild XP.",
                        RewardPreview =
                            "Rare Starlantern Staff • −45 XP • PWR +3 • MYS +12",
                        RiskLabel = "SAFE",
                        DestinationLabel = "Lantern Archive",
                        RoutePreview = "NEXT: LANTERN ARCHIVE",
                        RarityId = "QUALITY_RARE",
                        ItemName = "Rare Starlantern Staff",
                        PhysicalPower = 3,
                        MysticPower = 12,
                        TreasuryXpCost = 45,
                        CanChoose = false,
                        LockedReason = "Need 45 Guild XP for this merchant offer.",
                        VisualResourcePath = GuildQuestCardPresentation090
                            .VisualResourcePath090("MERCHANT")
                    },
                    new GuildQuestCardView090
                    {
                        CardId = "QUESTCARD090_FATE",
                        Category = "FATE",
                        Title = "Test Your Fate",
                        Description = "Throw two physical dice.",
                        RewardPreview =
                            "Pass: +20 XP, −1 fatigue • Miss: +3 XP, +1 fatigue",
                        RiskLabel = "2D6 CHECK",
                        DestinationLabel = "Gatewatch Market",
                        RoutePreview = "NEXT: GATEWATCH MARKET",
                        DieOne = 5,
                        DieTwo = 4,
                        Target = 7,
                        TreasuryXpDelta = 20,
                        CanChoose = true,
                        VisualResourcePath = GuildQuestCardPresentation090
                            .VisualResourcePath090("FATE")
                    }
                },
                Expedition = new GuildCityExpeditionView017D
                {
                    ExpeditionId = "EXP_QUEST_DECK_PRESENTATION_090",
                    BoardId = "BOARD_FIRST_HOUR_071",
                    CurrentNodeId = "N04",
                    CurrentNodeKind = "FORK",
                    Status = "Active",
                    ResolutionComplete = true,
                    CanMove = true,
                    LinkedNodeIds = new[] { "N05" },
                    VisitedNodeIds = new[] { "N00", "N01", "N04" },
                    RevealedNodeIds = new[] { "N00", "N01", "N04", "N05" }
                }
            };

        private static GuildCityPresentationState017D CheckState090(
            string[] objectiveFlags) =>
            new GuildCityPresentationState017D
            {
                IsAvailable = true,
                CampaignModeId = "Standard",
                Assignments = new[]
                {
                    new GuildCityAssignmentView017D
                    {
                        RecruitId = "R1",
                        RecruitName = "Tala",
                        Kind = "Deployed"
                    }
                },
                Expedition = new GuildCityExpeditionView017D
                {
                    ExpeditionId = "EXP_QUEST_DECK_CHECK_090",
                    BoardId = "BOARD_FIRST_HOUR_071",
                    CurrentNodeId = "N04",
                    CurrentNodeKind = "EVENT",
                    CurrentEventEligibleSkills = Array.Empty<string>(),
                    Status = "Active",
                    Urgency = 8,
                    ObjectiveFlags = objectiveFlags ?? Array.Empty<string>()
                }
            };

        private static int CrewModifier090(
            M1FlowPresenter presenter,
            GuildCityPresentationState017D state)
        {
            var builder = typeof(M1FlowPresenter).GetMethod(
                "BoardQuestCrewFor081",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(builder, Is.Not.Null);
            var crew = builder.Invoke(presenter, new object[] { state });
            Assert.That(crew, Is.Not.Null);
            var modifier = crew.GetType().GetField(
                "FastModifier",
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic);
            Assert.That(modifier, Is.Not.Null);
            return (int)modifier.GetValue(crew);
        }

        private sealed class DeckCoordinator090 :
            IGuildCityPresentationCoordinator017D,
            IBoardQuestDeckCoordinator090
        {
            public DeckCoordinator090(GuildCityPresentationState017D state)
            {
                GuildCity017D = state;
            }

            public GuildCityPresentationState017D GuildCity017D { get; }
            public int CommitCount { get; private set; }
            public M1CommandResult CommitBoardQuestCard090(string cardId)
            {
                CommitCount++;
                return M1CommandResult.Success("Quest card saved.");
            }

            public M1CommandResult PlaceGuildCityBuilding017D(
                string plotId, string buildingId) => Ok090();
            public M1CommandResult UpgradeGuildCityBuilding017D(
                string plotId) => Ok090();
            public M1CommandResult AssignGuildCityStaff017D(
                string plotId, string recruitId) => Ok090();
            public M1CommandResult RecallGuildCityStaff017D(
                string recruitId) => Ok090();
            public M1CommandResult SetGuildCityAssignment017D(
                string recruitId, string assignmentKind) => Ok090();
            public M1CommandResult ArchiveGuildCityMember017D(
                string recruitId, bool confirmed) => Ok090();
            public M1CommandResult CommitGuildCityApplicantBoard017D() => Ok090();
            public M1CommandResult RefreshGuildCityApplicantBoard017D() => Ok090();
            public M1CommandResult SignGuildCityApplicant017D(
                string recruitId) => Ok090();
            public M1CommandResult DeclineGuildCityApplicant017D(
                string recruitId) => Ok090();
            public M1CommandResult AcceptGuildCityContract017D(
                string contractId) => Ok090();
            public M1CommandResult StartGuildCityExpedition017D() => Ok090();
            public M1CommandResult MoveGuildCityExpedition017D(
                string destinationNodeId) => Ok090();
            public M1CommandResult ResolveGuildCityCheck017D(
                string eventId,
                string actorRecruitId,
                string assistantRecruitId,
                int modifier) => Ok090();
            public M1CommandResult DiscoverGateworksMaintenancePassage066() =>
                Ok090();
            public M1CommandResult CommitGuildCityEncounter017D(
                string encounterId) => Ok090();
            public M1CommandResult StartCommittedGuildCityBattle017D() => Ok090();
            public M1CommandResult FinalizeGuildCityOperation017D() => Ok090();
            public M1CommandResult AddGuildCityRelationshipMemory017D(
                string firstRecruitId,
                string secondRecruitId,
                string sourceId,
                string summary,
                int strength,
                string sceneId) => Ok090();
            public M1CommandResult ViewGuildCityRelationshipScene017D(
                string sceneId) => Ok090();

            private static M1CommandResult Ok090() =>
                M1CommandResult.Success("ok");
        }
    }
}
