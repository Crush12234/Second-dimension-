using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    /// <summary>
    /// Focused non-combat proof for the phone-simple front layer. The fixture
    /// builds the shipping private presenters directly, without entering battle
    /// or traversing any legacy route.
    /// </summary>
    public sealed class PhoneSimpleUi084PlayModeTests
    {
        private static readonly Vector2[] SupportedResolutions084 =
        {
            new Vector2(1920f, 1080f),
            new Vector2(1280f, 800f)
        };

        private static readonly string[] HomeActionLabels084 =
        {
            "CAMPAIGN\nTHREE-CARD QUESTS",
            "TOWER\nOPTIONAL BATTLES",
            "HEROES\nOWNED HEROES & UNIONS",
            "EQUIPMENT\nGEAR & ITEMS"
        };

        private static readonly string[] NavigationLabels084 =
        {
            "CAMPAIGN",
            "RECRUIT",
            "UNIONS",
            "INVENTORY",
            "INFINITE TOWER",
            "ENTER CODE"
        };

        [UnityTearDown]
        public IEnumerator TearDownPhoneSimpleUi084()
        {
            foreach (var root in UnityEngine.Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (root.parent == null &&
                    root.name.StartsWith("Phone Simple UI PlayMode ",
                        StringComparison.Ordinal))
                    UnityEngine.Object.Destroy(root.gameObject);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator GuildHallExposesFourDirectActionsWithoutRosterAtBothResolutions110()
        {
            foreach (var resolution in SupportedResolutions084)
            {
                var harness = CreateHarness084(resolution, "GuildHall");
                InvokePrivate084(
                    harness.Presenter,
                    "BuildLivingGuildHub074",
                    null,
                    BasicGuildCityState084());
                Rebuild084(harness.ScreenRoot);

                var hall = FindRect084(harness.ScreenRoot, "Living Guild Hub 074");
                Assert.That(hall, Is.Not.Null, resolution + " Guild Hall root");
                var actions = hall.GetComponentsInChildren<Button>(true)
                    .Where(value => value.name.StartsWith(
                        "Living Guild Hub Facility ",
                        StringComparison.Ordinal))
                    .ToArray();
                Assert.That(actions, Has.Length.EqualTo(4),
                    resolution + " must expose exactly four main Hall actions.");
                Assert.That(actions.Select(ButtonLabel084).ToArray(),
                    Is.EquivalentTo(HomeActionLabels084));
                foreach (var action in actions)
                {
                    var rect = action.GetComponent<RectTransform>();
                    AssertInside084(hall, rect, resolution + " " + action.name);
                    Assert.That(WorldSize084(rect).x, Is.GreaterThanOrEqualTo(132f),
                        resolution + " " + action.name + " width");
                    Assert.That(WorldSize084(rect).y, Is.GreaterThanOrEqualTo(132f),
                        resolution + " " + action.name + " height");
                }

                Assert.That(FindRect084(hall, "Living Guild Hub Roster 074"), Is.Null,
                    "The phone-simple Hall must not rebuild the old roster strip.");
                Assert.That(FindRect084(hall, "Living Guild Hub Homecoming 076"), Is.Null,
                    "The primary Hall must stay free of the old homecoming rail.");

                var primary = FindButton084(hall, "Living Guild Hub Primary CTA 074");
                Assert.That(primary, Is.Not.Null.And.Property("interactable").True);
                Assert.That(EventSystem.current, Is.Not.Null);
                Assert.That(EventSystem.current.currentSelectedGameObject,
                    Is.SameAs(primary.gameObject),
                    "Controller focus must enter on the Hall's one next-story action.");

                DestroyHarness084(harness);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator BottomNavigationPreservesSixServiceRoutesAtBothResolutions110()
        {
            foreach (var resolution in SupportedResolutions084)
            {
                var harness = CreateHarness084(resolution, "BottomNavigation");
                var page = AddStretchRect084(
                    harness.ScreenRoot,
                    "Phone Simple UI PlayMode Navigation Page 084");
                var pageLayout = page.gameObject.AddComponent<VerticalLayoutGroup>();
                pageLayout.childControlWidth = true;
                pageLayout.childControlHeight = true;
                pageLayout.childForceExpandWidth = true;
                pageLayout.childForceExpandHeight = false;
                pageLayout.spacing = 0f;

                var spacer = new GameObject(
                    "Phone Simple UI PlayMode Navigation Spacer 084",
                    typeof(RectTransform),
                    typeof(LayoutElement));
                spacer.transform.SetParent(page, false);
                spacer.GetComponent<LayoutElement>().flexibleHeight = 1f;

                InvokePrivate084(
                    harness.Presenter,
                    "AddGuildMobileNavigation062",
                    page);
                Rebuild084(page);

                var navigation = FindRect084(page, "Guild Mobile Bottom Navigation 062");
                Assert.That(navigation, Is.Not.Null, resolution + " bottom navigation");
                var actions = navigation.GetComponentsInChildren<Button>(true);
                Assert.That(actions, Has.Length.EqualTo(6));
                Assert.That(actions.Select(ButtonLabel084).ToArray(),
                    Is.EquivalentTo(NavigationLabels084));
                // These existing page shortcuts remain available after Home
                // consolidates its main entry points into four destinations.
                AssertInside084(page, navigation, resolution + " bottom navigation");
                foreach (var action in actions)
                {
                    var rect = action.GetComponent<RectTransform>();
                    AssertInside084(navigation, rect, resolution + " " + action.name);
                    Assert.That(WorldSize084(rect).y, Is.GreaterThanOrEqualTo(64f),
                        resolution + " " + action.name + " touch height");
                }

                DestroyHarness084(harness);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator BranchingQuestShowsOneForwardFlipActionWithControllerFocus084()
        {
            foreach (var resolution in SupportedResolutions084)
            {
                var harness = CreateHarness084(resolution, "BranchingQuest");
                InvokePrivate084(
                    harness.Presenter,
                    "BuildBoardQuestExperience081",
                    harness.ScreenRoot,
                    null,
                    BranchingQuestState084(false));
                Rebuild084(harness.ScreenRoot);

                var quest = FindRect084(harness.ScreenRoot, "Board Quest 081");
                Assert.That(quest, Is.Not.Null, resolution + " Board Quest root");
                var labels = quest.GetComponentsInChildren<Button>(true)
                    .Select(ButtonLabel084)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToArray();
                Assert.That(labels.Count(value => StringComparer.Ordinal.Equals(
                        value,
                        "MOVE FORWARD\nFLIP NEXT ROOM")),
                    Is.EqualTo(1),
                    resolution + " must expose one obvious room-flip action.");
                Assert.That(labels.Any(value => ContainsControlWord084(value, "LEFT")),
                    Is.False);
                Assert.That(labels.Any(value => ContainsControlWord084(value, "RIGHT")),
                    Is.False);
                Assert.That(labels.Any(value => value.IndexOf(
                        "TEAM UP", StringComparison.OrdinalIgnoreCase) >= 0),
                    Is.False);
                Assert.That(labels.Any(value => value.IndexOf(
                        "MOVE FAST", StringComparison.OrdinalIgnoreCase) >= 0),
                    Is.False);

                var primary = FindButton084(
                    quest,
                    "Expedition Primary Context Action 074");
                Assert.That(primary, Is.Not.Null);
                Assert.That(primary.IsInteractable(), Is.True);
                Assert.That(ButtonLabel084(primary),
                    Is.EqualTo("MOVE FORWARD\nFLIP NEXT ROOM"));
                AssertInside084(
                    quest,
                    primary.GetComponent<RectTransform>(),
                    resolution + " primary quest action");
                Assert.That(EventSystem.current, Is.Not.Null);
                Assert.That(EventSystem.current.currentSelectedGameObject,
                    Is.SameAs(primary.gameObject),
                    "Controller focus must land on the only interactable quest action.");

                DestroyHarness084(harness);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator RoomCardBackAndAuthoritativePipDiceStayInsideTheScene084()
        {
            Assert.That(
                M1FlowPresenter.BoardAdventureCardFlipMinimumHorizontalScale084,
                Is.GreaterThanOrEqualTo(0.68f),
                "The card midpoint must stay broad enough for the Guild back to remain readable.");
            Assert.That(
                M1FlowPresenter.BoardAdventureCardFlipMinimumHorizontalScale084,
                Is.LessThan(1f),
                "The card reveal still needs a visible perspective squash before the face swap.");
            foreach (var resolution in SupportedResolutions084)
            {
                var cardHarness = CreateHarness084(resolution, "RoomCard");
                InvokePrivate084(
                    cardHarness.Presenter,
                    "BuildBoardQuestExperience081",
                    cardHarness.ScreenRoot,
                    null,
                    BranchingQuestState084(false));
                Rebuild084(cardHarness.ScreenRoot);

                var cardScene = FindRect084(
                    cardHarness.ScreenRoot,
                    "Board Quest Scene 081");
                var cardBack = FindRect084(
                    cardHarness.ScreenRoot,
                    "Face Down Room Card Back 084");
                var portraitViewport = FindRect084(
                    cardHarness.ScreenRoot,
                    "Room Card Portrait Viewport 091");
                var cardStage = FindRect084(
                    cardHarness.ScreenRoot,
                    "Board Adventure Card Flip Stage 086");
                var cardFrame = FindRect084(
                    cardHarness.ScreenRoot,
                    "Face Down Room Card Frame 085");
                var cardCrest = FindRect084(
                    cardHarness.ScreenRoot,
                    "Face Down Room Card Crest 085");
                var libraryFrame = FindRect084(
                    cardHarness.ScreenRoot,
                    "Face Down Room Card Library Frame 086");
                var revealedFrame = FindRect084(
                    cardHarness.ScreenRoot,
                    "Board Adventure Revealed Card Library Frame 086");
                var pawn = FindRect084(
                    cardHarness.ScreenRoot,
                    "Board Adventure Guild Pawn 086");
                var pawnBase = FindRect084(
                    cardHarness.ScreenRoot,
                    "Board Adventure Pawn Base 086");
                var nextCard = FindRect084(
                    cardHarness.ScreenRoot,
                    "Board Adventure Next Face Down Physical Card 086");
                Assert.That(cardScene, Is.Not.Null);
                Assert.That(cardStage, Is.Not.Null,
                    "The reveal needs an authored stage, not a full-pane text flash.");
                var stageImage = cardStage.GetComponent<Image>();
                Assert.That(stageImage, Is.Not.Null);
                Assert.That(stageImage.color.a, Is.EqualTo(1f).Within(0.001f),
                    "The destination room must not show through the reveal stage before the flip midpoint.");
                Assert.That(stageImage.sprite, Is.Null,
                    "The fully opaque base must not gain transparent image holes exposing the destination.");
                Assert.That(stageImage.color.b, Is.GreaterThan(stageImage.color.r * 2f),
                    "The privacy stage should be a deep-blue Guild table, not an empty black slab.");
                var privacyPattern = FindRect084(cardHarness.ScreenRoot,
                    "Board Card Privacy Table Pattern 091");
                Assert.That(privacyPattern, Is.Not.Null);
                Assert.That(privacyPattern.GetComponent<Image>().sprite,
                    Is.SameAs(libraryFrame.GetComponent<Image>().sprite),
                    "Only the fixed magic-back pattern may decorate the privacy stage; no destination artwork.");
                Assert.That(privacyPattern.GetComponent<Image>().color.a, Is.LessThanOrEqualTo(0.20f));
                Assert.That(cardStage.GetComponent<RectMask2D>(), Is.Not.Null);
                Assert.That(cardStage.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(cardStage.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(cardStage.GetSiblingIndex(),
                    Is.EqualTo(cardStage.parent.childCount - 1),
                    "The opaque reveal stage must remain above all destination-room artwork until midpoint.");
                Assert.That(cardBack, Is.Not.Null,
                    "The first presentation frame must contain a physical card back.");
                Assert.That(cardBack.gameObject.activeInHierarchy, Is.True,
                    "The card back must be visible before the midpoint swap.");
                AssertInside084(cardScene, cardBack, resolution + " room-card back");
                Assert.That(cardFrame, Is.Not.Null,
                    "The face-down card needs a distinct framed back, not a text overlay.");
                Assert.That(cardCrest, Is.Not.Null,
                    "The legacy crest object remains available to the presentation contract.");
                Assert.That(cardCrest.gameObject.activeSelf, Is.False,
                    "The library back already contains the Guild crest; a second diamond must not obscure its art.");
                Assert.That(libraryFrame, Is.Not.Null,
                    "The card back must reuse the authored Anime Dojo command-card frame.");
                Assert.That(libraryFrame.GetComponent<Image>().sprite, Is.Not.Null);
                Assert.That(libraryFrame.GetComponent<Image>().preserveAspect, Is.True);
                Assert.That(revealedFrame, Is.Not.Null,
                    "The revealed room must retain a physical command-card face.");
                Assert.That(revealedFrame.GetComponent<Image>().sprite, Is.Not.Null);
                Assert.That(pawn, Is.Not.Null,
                    "The active board space needs a visible constructed Guild pawn.");
                Assert.That(pawnBase, Is.Not.Null,
                    "The Guild pawn needs a physical base instead of a text bullet.");
                Assert.That(nextCard, Is.Not.Null,
                    "The only move action needs a visible sealed next-room card.");
                Assert.That(nextCard.GetComponent<Image>().sprite, Is.Not.Null);
                AssertInside084(cardBack, cardFrame, resolution + " room-card frame");
                AssertInside084(cardFrame, cardCrest, resolution + " room-card crest");
                Assert.That(portraitViewport, Is.Not.Null);
                AssertInside084(cardScene, portraitViewport, resolution + " portrait viewport");
                AssertInside084(portraitViewport, cardBack, resolution + " fitted portrait back");
                var portraitSize = WorldSize084(portraitViewport);
                var backSize = WorldSize084(cardBack);
                const float authoredBackAspect = 2f / 3f;
                var expectedHeight = Mathf.Min(portraitSize.y,
                    portraitSize.x / authoredBackAspect);
                Assert.That(backSize.y, Is.EqualTo(expectedHeight).Within(1f),
                    resolution + " the portrait back must use the available stage height without cropping");
                Assert.That(backSize.x, Is.EqualTo(expectedHeight * authoredBackAspect).Within(1f));
                Assert.That(backSize.x / backSize.y,
                    Is.EqualTo(authoredBackAspect).Within(0.001f),
                    resolution + " the 1024×1536 magic artwork must not stretch to the old landscape width");
                Assert.That(backSize.x, Is.GreaterThanOrEqualTo(resolution.x * 0.22f),
                    resolution + " the portrait remains large enough to read its authored detail");
                Assert.That(backSize.y, Is.GreaterThanOrEqualTo(resolution.y * 0.60f),
                    resolution + " the intact physical card must dominate the reveal height");

                DestroyHarness084(cardHarness);
                yield return null;

                var diceHarness = CreateHarness084(
                    resolution,
                    "CommittedDice",
                    reducedMotion: true);
                InvokePrivate084(
                    diceHarness.Presenter,
                    "BuildBoardQuestExperience081",
                    diceHarness.ScreenRoot,
                    null,
                    BranchingQuestState084(true));
                Rebuild084(diceHarness.ScreenRoot);

                var diceScene = FindRect084(
                    diceHarness.ScreenRoot,
                    "Board Quest Scene 081");
                var diceRow = FindRect084(
                    diceHarness.ScreenRoot,
                    "Authoritative Dice Roll 084");
                var first = FindRect084(
                    diceHarness.ScreenRoot,
                    "First Authoritative Die 084");
                var second = FindRect084(
                    diceHarness.ScreenRoot,
                    "Second Authoritative Die 084");
                Assert.That(diceScene, Is.Not.Null);
                Assert.That(diceRow, Is.Not.Null);
                Assert.That(first, Is.Not.Null);
                Assert.That(second, Is.Not.Null);
                AssertInside084(diceScene, diceRow, resolution + " dice row");
                AssertInside084(diceRow, first, resolution + " first die");
                AssertInside084(diceRow, second, resolution + " second die");
                AssertSquare084(first, resolution + " first die");
                AssertSquare084(second, resolution + " second die");
                AssertPipFace084(first, 4, resolution + " first saved die");
                AssertPipFace084(second, 5, resolution + " second saved die");

                DestroyHarness084(diceHarness);
                yield return null;

                var treasureHarness = CreateHarness084(
                    resolution,
                    "TreasureReward",
                    reducedMotion: true);
                var treasureState = BranchingQuestState084(false);
                treasureState.Expedition.ExpeditionId = "PHONE_SIMPLE_TREASURE_086";
                treasureState.Expedition.CurrentNodeId = "N05";
                treasureState.Expedition.CurrentNodeKind = "RESOURCE";
                treasureState.Expedition.LinkedNodeIds = new[] { "N06" };
                treasureState.Expedition.VisitedNodeIds =
                    new[] { "N00", "N01", "N04", "N05" };
                treasureState.Expedition.RevealedNodeIds =
                    new[] { "N00", "N01", "N04", "N05", "N06" };
                InvokePrivate084(
                    treasureHarness.Presenter,
                    "BuildBoardQuestExperience081",
                    treasureHarness.ScreenRoot,
                    null,
                    treasureState);
                Rebuild084(treasureHarness.ScreenRoot);
                var rewardStrip = FindRect084(
                    treasureHarness.ScreenRoot,
                    "Board Quest Revealed Reward 081");
                var rewardToken = FindRect084(
                    treasureHarness.ScreenRoot,
                    "Board Adventure Reward Token 086 TREASURE");
                var chestBody = FindRect084(
                    treasureHarness.ScreenRoot,
                    "Board Reward Chest Body 086");
                var chestLid = FindRect084(
                    treasureHarness.ScreenRoot,
                    "Board Reward Chest Lid 086");
                Assert.That(rewardStrip, Is.Not.Null);
                Assert.That(rewardToken, Is.Not.Null,
                    "A treasure reward must read as a physical loot reveal.");
                Assert.That(chestBody, Is.Not.Null);
                Assert.That(chestLid, Is.Not.Null);
                AssertInside084(rewardStrip, rewardToken,
                    resolution + " physical treasure token");
                Assert.That(chestLid.localEulerAngles.z,
                    Is.Not.EqualTo(0f).Within(0.5f),
                    "Reduced motion must settle the chest in its open state.");

                DestroyHarness084(treasureHarness);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator PawnCardAndRewardAnimateInReadablePhysicalOrder084()
        {
            foreach (var resolution in SupportedResolutions084)
            {
                var harness = CreateHarness084(resolution, "PhysicalSequence");
                InvokePrivate084(
                    harness.Presenter,
                    "BuildBoardQuestExperience081",
                    harness.ScreenRoot,
                    null,
                    BranchingQuestState084(false));
                Rebuild084(harness.ScreenRoot);

                var pawn = FindRect084(
                    harness.ScreenRoot,
                    "Board Adventure Guild Pawn 086");
                var pawnGroup = pawn.GetComponent<CanvasGroup>();
                var stage = FindRect084(
                    harness.ScreenRoot,
                    "Board Adventure Card Flip Stage 086");
                var card = FindRect084(harness.ScreenRoot, "Board Quest Scene 081");
                var reward = harness.ScreenRoot
                    .GetComponentsInChildren<RectTransform>(true)
                    .Single(value => value.name.StartsWith(
                        "Board Adventure Reward Token 086 ",
                        StringComparison.Ordinal));
                Assert.That(pawnGroup, Is.Not.Null);
                Assert.That(pawnGroup.alpha, Is.LessThan(1f),
                    resolution + " pawn starts in travel");
                Assert.That(stage.gameObject.activeSelf, Is.True,
                    resolution + " sealed card waits for pawn landing");

                yield return new WaitForSecondsRealtime(
                    M1FlowPresenter.BoardAdventurePawnTravelDuration084 +
                    M1FlowPresenter.BoardAdventureCardFlipDuration084 + 0.12f);
                Assert.That(pawnGroup.alpha, Is.EqualTo(1f).Within(0.02f),
                    resolution + " pawn settles before the resolved room is interactive");
                Assert.That(stage.gameObject.activeSelf, Is.False,
                    resolution + " face-down stage clears after the midpoint swap");
                Assert.That(Vector3.Distance(card.localScale, Vector3.one),
                    Is.LessThan(0.01f));
                Assert.That(card.localEulerAngles.sqrMagnitude, Is.LessThan(0.01f));

                yield return new WaitForSecondsRealtime(
                    M1FlowPresenter.BoardAdventureRewardRevealDuration084 + 0.08f);
                Assert.That(reward, Is.Not.Null,
                    resolution + " reward token remains visible after reveal");
                Assert.That(Vector3.Distance(reward.localScale, Vector3.one),
                    Is.LessThan(0.01f));

                DestroyHarness084(harness);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CommittedCheckDiceSettleBeforeRewardSurfaceBegins084()
        {
            foreach (var resolution in SupportedResolutions084)
            {
                var harness = CreateHarness084(resolution, "DiceBeforeReward");
                InvokePrivate084(
                    harness.Presenter,
                    "BuildBoardQuestExperience081",
                    harness.ScreenRoot,
                    null,
                    BranchingQuestState084(true));
                Rebuild084(harness.ScreenRoot);

                var stage = FindRect084(
                    harness.ScreenRoot,
                    "Board Adventure Card Flip Stage 086");
                var reward = FindRect084(
                    harness.ScreenRoot,
                    "Board Quest Revealed Reward 081");
                var rewardGroup = reward.GetComponent<CanvasGroup>();
                var diceMotion = harness.ScreenRoot
                    .GetComponentsInChildren<Text>(true)
                    .Single(value => value.name.StartsWith(
                        "Authoritative Dice Motion Status 086",
                        StringComparison.Ordinal));
                Assert.That(stage.gameObject.activeSelf, Is.True);
                Assert.That(rewardGroup, Is.Not.Null);
                Assert.That(rewardGroup.alpha, Is.EqualTo(0f).Within(0.001f),
                    resolution + " committed reward starts fully hidden");
                Assert.That(diceMotion.text, Does.Contain("ROLLING"));

                yield return new WaitForSecondsRealtime(
                    M1FlowPresenter.BoardAdventureCommittedCheckRewardDelay084 -
                    0.18f);
                Assert.That(diceMotion.text, Does.Contain("ROLLING"),
                    resolution + " physical dice are still resolving");
                Assert.That(rewardGroup.alpha, Is.EqualTo(0f).Within(0.001f),
                    resolution + " reward cannot pre-empt the settling dice");

                yield return new WaitForSecondsRealtime(0.30f);
                Assert.That(diceMotion.text, Does.Contain("DICE SETTLED"),
                    resolution + " saved dice settle before reward presentation");
                Assert.That(rewardGroup.alpha, Is.GreaterThan(0f),
                    resolution + " reward begins only after dice settlement");

                DestroyHarness084(harness);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ShippingRoomBuilderBindsConfiguredRosterAndChapterSpeaker091()
        {
            foreach (var resolution in SupportedResolutions084)
            foreach (var chapterTwo in new[] { false, true })
            {
                var harness = CreateHarness084(resolution, "LiveSpeaker", reducedMotion: true);
                try
                {
                    SetPrivateField084(harness.Presenter, "_coordinator", new SpeakerCoordinator091());
                    var state = BranchingQuestState084(true);
                    state.Expedition.BoardId = chapterTwo
                        ? ExpeditionBoardProjection074.ChapterTwoBoardId074
                        : ExpeditionBoardProjection074.FirstBoardId074;
                    state.Expedition.CurrentNodeId = chapterTwo ? "N13" : "N08";
                    state.Expedition.VisitedNodeIds = new[] { "N00", state.Expedition.CurrentNodeId };
                    state.Expedition.RevealedNodeIds = state.Expedition.VisitedNodeIds;
                    var view = ExpeditionBoardProjection074.Build(state);
                    InvokePrivate084(harness.Presenter, "BuildBoardQuestScene081",
                        harness.ScreenRoot, state, view);
                    Rebuild084(harness.ScreenRoot);
                    yield return null;
                    Rebuild084(harness.ScreenRoot);

                    var panel = FindRect084(harness.ScreenRoot, "Expedition Companion Story Beat 076");
                    var quote = panel.GetComponentsInChildren<Text>(true).Single(value =>
                        value.name.StartsWith("Expedition Companion Story Beat Text 076", StringComparison.Ordinal));
                    Assert.That(quote.name, Does.Contain("[Authored Compact 076]"),
                        "Exercise the shipping text configurator that adds the name suffix.");
                    Assert.That(panel.GetComponent<LayoutElement>().minHeight, Is.GreaterThanOrEqualTo(190f),
                        "The actual room builder must bind speaker normalization, not only an isolated panel fixture.");
                    Assert.That(quote.rectTransform.offsetMin.x, Is.EqualTo(12f).Within(0.01f));
                    var identity = panel.Cast<Transform>().Single(value =>
                        value.name == "Expedition Companion Identity Chip 078" ||
                        value.name.StartsWith("Chapter Two Story Identity ", StringComparison.Ordinal)) as RectTransform;
                    var label = identity.GetComponentsInChildren<Text>(true).First(value => value.transform.parent == identity);
                    Assert.That(label.text, Does.StartWith(chapterTwo ? "ORRA VALE" : "ODELIA"));
                    Assert.That(label.text.Split('\n').Length, Is.LessThanOrEqualTo(2));
                    var artwork = identity.GetComponentsInChildren<Image>(true).FirstOrDefault(value =>
                        value.name == "Portrait Artwork" ||
                        value.name.StartsWith("Chapter Two Story Portrait ", StringComparison.Ordinal));
                    Assert.That(artwork, Is.Not.Null);
                    Assert.That(artwork.sprite, Is.Not.Null, "The shipping speaker retains real character artwork.");
                    AssertInside084(panel, identity, resolution + " speaker identity");
                    AssertInside084(panel, quote.rectTransform, resolution + " complete quote");
                    var quoteBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, quote.rectTransform);
                    var identityBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, identity);
                    Assert.That(quoteBounds.max.y, Is.LessThan(identityBounds.min.y));
                    foreach (var text in new[] { quote, label })
                    {
                        var settings = text.GetGenerationSettings(text.rectTransform.rect.size);
                        settings.resizeTextForBestFit = false;
                        settings.fontSize = 18;
                        var needed = text.cachedTextGeneratorForLayout.GetPreferredHeight(text.text, settings) / text.pixelsPerUnit;
                        Assert.That(needed, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 0.5f),
                            resolution + " " + text.name + " must fit at a readable size without clipped words.");
                    }
                    if (!chapterTwo)
                        Assert.That(quote.text, Does.Contain("before that hazard follows us into battle."));
                }
                finally { DestroyHarness084(harness); }
                yield return null;
            }
        }

        private sealed class SpeakerCoordinator091 : IM1PresentationCoordinator
        {
            public event Action Changed { add { } remove { } }
            public M1PresentationState State { get; } = new M1PresentationState
            {
                Recruits = new[] { new M1RecruitLoadoutView
                {
                    RecruitId = "SIGREC_ODELIA_FEN", PortraitAuthorityId = "SIGREC_ODELIA_FEN",
                    DisplayName = "Odelia Fen", RaceId = "HUMAN", ObservedClass = "Mage"
                } }
            };
            private static M1CommandResult ReadOnly() => throw new InvalidOperationException("Speaker presentation must not issue a gameplay command.");
            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => ReadOnly();
            public M1CommandResult SignRecruit(string recruitId) => ReadOnly();
            public M1CommandResult EquipItem(string recruitId, string slotId, string itemId) => ReadOnly();
            public M1CommandResult UnequipItem(string recruitId, string slotId) => ReadOnly();
            public M1CommandResult SetEquipmentLock(string recruitId, string slotId, bool locked) => ReadOnly();
            public M1CommandResult CompleteEquipmentReview() => ReadOnly();
            public M1CommandResult AddUnion() => ReadOnly();
            public M1CommandResult RemoveUnion(int unionIndex) => ReadOnly();
            public M1CommandResult AssignRecruitToUnion(string recruitId, int unionIndex, int slotIndex) => ReadOnly();
            public M1CommandResult UnassignRecruitFromUnion(string recruitId) => ReadOnly();
            public M1CommandResult SetUnionLeader(int unionIndex, string recruitId) => ReadOnly();
            public M1CommandResult SetFormation(int unionIndex, string formationId) => ReadOnly();
            public M1CommandResult SetDoctrine(int unionIndex, string doctrineId) => ReadOnly();
            public M1CommandResult SaveAndReloadProof() => ReadOnly();
        }

        private static GuildCityPresentationState017D BasicGuildCityState084() =>
            new GuildCityPresentationState017D
            {
                IsAvailable = true,
                TreasuryXp = 42,
                Contracts = Array.Empty<GuildCityContractView017D>()
            };

        private static GuildCityPresentationState017D BranchingQuestState084(
            bool committedDice)
        {
            var currentNode = committedDice ? "N02" : "N01";
            return new GuildCityPresentationState017D
            {
                IsAvailable = true,
                HasActiveContract = true,
                TreasuryXp = 42,
                Contracts = new[]
                {
                    new GuildCityContractView017D
                    {
                        ContractId = "PHONE_SIMPLE_QUEST_084",
                        BoardId = ExpeditionBoardProjection074.FirstBoardId074,
                        DisplayName = "The Bell Beneath Skyhome",
                        PrimaryObjective = "Find the missing patrol and bring everyone home.",
                        IsActive = true
                    }
                },
                Expedition = new GuildCityExpeditionView017D
                {
                    ExpeditionId = committedDice
                        ? "PHONE_SIMPLE_DICE_084"
                        : "PHONE_SIMPLE_BRANCH_084",
                    BoardId = ExpeditionBoardProjection074.FirstBoardId074,
                    CurrentNodeId = currentNode,
                    CurrentNodeKind = committedDice ? "EVENT" : "FORK",
                    CurrentEventId = committedDice
                        ? "EVENT_COLLAPSED_HANDRAIL"
                        : string.Empty,
                    CurrentEventTitle = committedDice
                        ? "The Broken Crossing"
                        : string.Empty,
                    CurrentEventProblem = committedDice
                        ? "The best field team found a safe line across."
                        : string.Empty,
                    CurrentEventOutcomeText = committedDice
                        ? "The Guild crosses together and keeps moving."
                        : string.Empty,
                    Status = "Active",
                    Supplies = 8,
                    Urgency = 3,
                    ResolutionComplete = true,
                    CanMove = true,
                    LinkedNodeIds = committedDice
                        ? new[] { "N03", "N04" }
                        : new[] { "N02", "N04" },
                    VisitedNodeIds = committedDice
                        ? new[] { "N00", "N01", "N02" }
                        : new[] { "N00", "N01" },
                    RevealedNodeIds = committedDice
                        ? new[] { "N00", "N01", "N02", "N03", "N04" }
                        : new[] { "N00", "N01", "N02", "N04" },
                    ObjectiveFlags = Array.Empty<string>(),
                    HasCommittedCheckAtCurrentNode = committedDice,
                    LastCheckActorRecruitId = committedDice ? "LEAD_084" : string.Empty,
                    LastCheckAssistantRecruitId = committedDice ? "ALLY_084" : string.Empty,
                    LastCheckDieOne = committedDice ? 4 : 0,
                    LastCheckDieTwo = committedDice ? 5 : 0,
                    LastCheckModifier = committedDice ? 2 : 0,
                    LastCheckTotal = committedDice ? 11 : 0,
                    LastCheckOutcome = committedDice ? "FULL_SUCCESS" : string.Empty
                }
            };
        }

        private static Harness084 CreateHarness084(
            Vector2 resolution,
            string suffix,
            bool reducedMotion = false)
        {
            EnsureEventSystem084();
            var presenterObject = new GameObject(
                "Phone Simple UI PlayMode Presenter 084 " + suffix + " " + resolution.x);
            var presenter = presenterObject.AddComponent<M1FlowPresenter>();
            var canvasObject = new GameObject(
                "Phone Simple UI PlayMode Canvas 084 " + suffix + " " + resolution.x,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = resolution;
            var screenRoot = AddStretchRect084(
                canvasRect,
                "Phone Simple UI PlayMode Screen Root 084");

            SetPrivateField084(presenter, "_screenRoot", screenRoot);
            SetPrivateField084(presenter, "_reducedMotion", reducedMotion);
            return new Harness084(presenter, canvasObject, screenRoot);
        }

        private static void EnsureEventSystem084()
        {
            if (EventSystem.current != null) return;
            new GameObject(
                "Phone Simple UI PlayMode Event System 084",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
        }

        private static RectTransform AddStretchRect084(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform))
                .GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void InvokePrivate084(
            M1FlowPresenter presenter,
            string methodName,
            params object[] arguments)
        {
            var method = typeof(M1FlowPresenter).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(presenter, arguments);
        }

        private static void SetPrivateField084(
            M1FlowPresenter presenter,
            string fieldName,
            object value)
        {
            var field = typeof(M1FlowPresenter).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(presenter, value);
        }

        private static void Rebuild084(RectTransform root)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true)
                         .OrderByDescending(value => Depth084(value)))
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            Canvas.ForceUpdateCanvases();
        }

        private static int Depth084(Transform value)
        {
            var depth = 0;
            for (var current = value; current != null; current = current.parent)
                depth++;
            return depth;
        }

        private static RectTransform FindRect084(Transform root, string name) =>
            root.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(value => StringComparer.Ordinal.Equals(value.name, name));

        private static Button FindButton084(Transform root, string name) =>
            root.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(value => StringComparer.Ordinal.Equals(value.name, name));

        private static string ButtonLabel084(Button button) =>
            button == null
                ? string.Empty
                : button.GetComponentsInChildren<Text>(true)
                    .FirstOrDefault(value => StringComparer.Ordinal.Equals(
                        value.name,
                        "Label"))?.text ??
                  button.GetComponentInChildren<Text>(true)?.text ??
                  string.Empty;

        private static bool ContainsControlWord084(string copy, string word)
        {
            var tokens = (copy ?? string.Empty).Split(
                new[] { ' ', '\t', '\r', '\n', '•', '→', '←', '/', '-', '—' },
                StringSplitOptions.RemoveEmptyEntries);
            return tokens.Any(value => StringComparer.OrdinalIgnoreCase.Equals(value, word));
        }

        private static void AssertPipFace084(
            RectTransform die,
            int expectedActivePips,
            string label)
        {
            var pips = die.GetComponentsInChildren<RectTransform>(true)
                .Where(value => value.name.StartsWith("Die Pip ", StringComparison.Ordinal))
                .ToArray();
            Assert.That(pips, Has.Length.EqualTo(9), label + " physical pip roots");
            Assert.That(pips.Count(value => value.gameObject.activeSelf),
                Is.EqualTo(expectedActivePips),
                label + " settled saved face");
        }

        private static void AssertSquare084(RectTransform rect, string label)
        {
            var size = WorldSize084(rect);
            Assert.That(Mathf.Abs(size.x - size.y), Is.LessThanOrEqualTo(1f), label);
            Assert.That(size.x, Is.GreaterThanOrEqualTo(64f), label + " size");
        }

        private static Vector2 WorldSize084(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return new Vector2(
                Mathf.Abs(corners[2].x - corners[0].x),
                Mathf.Abs(corners[2].y - corners[0].y));
        }

        private static void AssertInside084(
            RectTransform outer,
            RectTransform inner,
            string label)
        {
            var outerCorners = new Vector3[4];
            var innerCorners = new Vector3[4];
            outer.GetWorldCorners(outerCorners);
            inner.GetWorldCorners(innerCorners);
            Assert.That(innerCorners.Min(value => value.x),
                Is.GreaterThanOrEqualTo(outerCorners.Min(value => value.x) - 0.5f),
                label + " left");
            Assert.That(innerCorners.Max(value => value.x),
                Is.LessThanOrEqualTo(outerCorners.Max(value => value.x) + 0.5f),
                label + " right");
            Assert.That(innerCorners.Min(value => value.y),
                Is.GreaterThanOrEqualTo(outerCorners.Min(value => value.y) - 0.5f),
                label + " bottom");
            Assert.That(innerCorners.Max(value => value.y),
                Is.LessThanOrEqualTo(outerCorners.Max(value => value.y) + 0.5f),
                label + " top");
        }

        private static void DestroyHarness084(Harness084 harness)
        {
            if (harness.Presenter != null)
                UnityEngine.Object.Destroy(harness.Presenter.gameObject);
            if (harness.CanvasObject != null)
                UnityEngine.Object.Destroy(harness.CanvasObject);
        }

        private readonly struct Harness084
        {
            public Harness084(
                M1FlowPresenter presenter,
                GameObject canvasObject,
                RectTransform screenRoot)
            {
                Presenter = presenter;
                CanvasObject = canvasObject;
                ScreenRoot = screenRoot;
            }

            public M1FlowPresenter Presenter { get; }
            public GameObject CanvasObject { get; }
            public RectTransform ScreenRoot { get; }
        }
    }
}
