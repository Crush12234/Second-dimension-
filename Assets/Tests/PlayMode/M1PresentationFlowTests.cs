using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.FirstHour071;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.FirstHour071;
using SecondDimension.Presentation.GuildCity017D;
using SecondDimension.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class M1PresentationFlowTests
    {
        private static readonly string[] FirstHourFounderRecruitIds076 =
        {
            "PROC_36344E2400DC98B6",
            "PROC_F85A4CAA747BC8C6",
            "PROC_5B14E7816E55FFB5",
            "PROC_748DD03A23E1FEB0",
            "SIGREC_MAREN_HOLT",
            "SIGREC_ODELIA_FEN"
        };

        [UnityTearDown]
        public IEnumerator TearDownPresentationRoots()
        {
            foreach (var presenter in UnityEngine.Object.FindObjectsByType<M1FlowPresenter>(FindObjectsSortMode.None))
            {
                UnityEngine.Object.Destroy(presenter.gameObject);
            }
            yield return null;

            // A failed test can interrupt normal presenter cleanup. Remove only the
            // proof canvas name so unrelated runner UI is never affected.
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (StringComparer.Ordinal.Equals(canvas.name, "M1 Playable Proof Canvas"))
                {
                    UnityEngine.Object.Destroy(canvas.gameObject);
                }
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenuUsesGuidedGuildStoryLanguageAndKeepsIndividualArtLocked()
        {
            var presenter = CreatePresenter(new FakeCoordinator(M1Screen.ApplicantBoard));
            yield return null;

            AssertTextContains("SECOND DIMENSION");
            AssertTextContains("GUILD OF WORLDS");
            AssertTextContains("YOUR PEOPLE ARE WAITING.");
            AssertTextContains("Continue your saved adventure. Find the main card campaign in the Guild Hall.");
            AssertNamedTextEquals076(
                "Studio Title Hook 076",
                "YOUR PEOPLE ARE WAITING.\nTHE ROAD IS STILL OPEN.");
            AssertNamedTextEquals076(
                "Studio Title Promise 076",
                "Continue your saved adventure. Find the main card campaign in the Guild Hall.");
            Assert.That(FindButton("CONTINUE GAME"), Is.Not.Null);
            Assert.That(FindButton("CONTINUE GAME").name, Is.EqualTo("Title Primary Play Now 062"));
            AssertFocusedAction076("CONTINUE GAME");
            var startNewGuild077 = FindButton("START NEW GUILD");
            Assert.That(startNewGuild077, Is.Not.Null,
                "A saved campaign must always offer a deliberate way to start over.");
            Assert.That(startNewGuild077.name, Is.EqualTo("Title Start New Guild 077"));
            var continueStory077 = FindButton("CONTINUE GAME");
            var storyCard077 = FindRectByPrefix074("Studio Title Story Card 076");
            foreach (var resolution077 in new[]
                     {
                         new Vector2(1280f, 800f),
                         new Vector2(1920f, 1080f)
                     })
            {
                ConfigureExpeditionCanvas074(resolution077.x, resolution077.y);
                LayoutRebuilder.ForceRebuildLayoutImmediate(storyCard077);
                Canvas.ForceUpdateCanvases();
                var continueRect077 = continueStory077.GetComponent<RectTransform>();
                var startRect077 = startNewGuild077.GetComponent<RectTransform>();
                AssertRectInside074(storyCard077, continueRect077, "saved-title Continue action");
                AssertRectInside074(storyCard077, startRect077, "saved-title Start New action");
                AssertRectAbove074(
                    continueRect077,
                    startRect077,
                    "saved-title Continue and Start New actions");
                Assert.That(WorldRect074(startRect077).height, Is.GreaterThanOrEqualTo(48f));
                AssertTextFitsRect074(
                    startNewGuild077.GetComponentInChildren<Text>(),
                    "saved-title Start New action");
            }
            Assert.That(FindButton("CONTINUE"), Is.Null, "A saved campaign must name its next step instead of showing a vague CONTINUE action.");
            AssertNoTextContains("SELECT INDIVIDUAL ART");
            Assert.That(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), Is.Not.Null);
            Assert.That(Resources.Load<Font>("SecondDimension/Fonts/SecondDimensionUISans"), Is.Null);
            Assert.That(Resources.Load<Font>("SecondDimension/Fonts/SecondDimensionDisplay"), Is.Null);
            Assert.That(GameObject.Find("Studio Title Stage 076"), Is.Not.Null);
            Assert.That(GameObject.Find("Studio Title Lockup 076"), Is.Not.Null);
            Assert.That(GameObject.Find("Studio Title Story Card 076"), Is.Not.Null);
            Assert.That(UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None), Is.Empty,
                "The one-screen studio title must not hide its primary action in a scroll view.");
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator SavedTitleStartNewGuildRequiresSafeConfirmation077()
        {
            var coordinator = new FakeCoordinator(M1Screen.ApplicantBoard);
            var presenter = CreatePresenter(coordinator);
            yield return null;

            AssertFocusedAction076("CONTINUE GAME");
            var campaignHash077 = coordinator.State.CanonicalStateHash;
            FindButton("START NEW GUILD").onClick.Invoke();
            yield return null;

            Assert.That(GameObject.Find("Confirmation Blocker"), Is.Not.Null);
            AssertTextContains("START A NEW GUILD?");
            AssertTextContains("current save remains safe");
            AssertFocusedAction076("KEEP CURRENT GUILD");
            Assert.That(coordinator.State.HasCampaign, Is.True);
            Assert.That(coordinator.State.CanonicalStateHash, Is.EqualTo(campaignHash077));

            Click("KEEP CURRENT GUILD");
            yield return null;
            Assert.That(GameObject.Find("Confirmation Blocker"), Is.Null);
            Assert.That(coordinator.State.HasCampaign, Is.True);
            Assert.That(coordinator.State.CanonicalStateHash, Is.EqualTo(campaignHash077));
            AssertFocusedAction076("CONTINUE GAME");

            FindButton("START NEW GUILD").onClick.Invoke();
            yield return null;
            AssertFocusedAction076("KEEP CURRENT GUILD");
            var confirm077 = FindButtonByName("Confirm");
            Assert.That(confirm077, Is.Not.Null);
            confirm077.onClick.Invoke();
            yield return null;

            AssertTextContains("SIGN THE SKYHOME CHARTER");
            Assert.That(coordinator.State.HasCampaign, Is.True,
                "Entering the fresh charter route must not erase the current campaign.");
            Assert.That(coordinator.State.CanonicalStateHash, Is.EqualTo(campaignHash077));
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator SavedTitleNamesTheExactCurrentChapterAndNextStoryAction076()
        {
            var chapterTwo = new FakeGuildCityCoordinator(firstChapterComplete: true);
            var presenter = CreatePresenter(chapterTwo);
            yield return null;

            AssertNamedTextEquals076("Studio Title Chapter 076", "CHAPTER 2  •  THE DOOR INSIDE");
            AssertNamedTextEquals076(
                "Studio Title Hook 076",
                "THE PATROL CAME HOME.\nTHE WAYGLASS OPENED A ROAD WITHIN.");
            AssertNamedTextEquals076(
                "Studio Title Promise 076",
                "Return to the Hall table. Follow the survey crew's brass line beneath Skyhome.");
            AssertFocusedAction076("CONTINUE GAME");
            yield return Cleanup(presenter);

            var savedChapterTwoThreshold = new FakeGuildCityCoordinator(firstChapterComplete: true);
            savedChapterTwoThreshold.PrepareTitleContinuity076(
                1,
                withSavedExpedition: true,
                savedExpeditionNodeId: "N01");
            presenter = CreatePresenter(savedChapterTwoThreshold);
            yield return null;

            AssertNamedTextEquals076(
                "Studio Title Promise 076",
                "Resume the saved route. Find the survey crew and the unrecorded door beneath Skyhome.");
            AssertNamedTextEquals076(
                "Studio Title Identity 076",
                "CURRENT ORDER\nCONTINUE CHAPTER 2");
            AssertFocusedAction076("CONTINUE GAME");
            yield return Cleanup(presenter);

            var savedChapterTwoRoute = new FakeGuildCityCoordinator(firstChapterComplete: true);
            savedChapterTwoRoute.PrepareTitleContinuity076(1, withSavedExpedition: true);
            presenter = CreatePresenter(savedChapterTwoRoute);
            yield return null;

            AssertNamedTextEquals076(
                "Studio Title Promise 076",
                "Resume the saved route. Find the survey crew and the unrecorded door beneath Skyhome.");
            yield return Cleanup(presenter);

            var chapterThree = new FakeGuildCityCoordinator();
            chapterThree.PrepareTitleContinuity076(2);
            presenter = CreatePresenter(chapterThree);
            yield return null;

            AssertNamedTextEquals076(
                "Studio Title Chapter 076",
                "CHAPTER 3  •  KEEP THE RELIEF ROAD OPEN");
            AssertNamedTextEquals076(
                "Studio Title Hook 076",
                "THE DOOR INSIDE IS OPEN.\nSKYHOME'S RELIEF ROAD IS BREAKING.");
            AssertNamedTextEquals076(
                "Studio Title Promise 076",
                "Return to your Guild and keep the medicine convoy moving through Chapter 3.");
            yield return Cleanup(presenter);

            var openingArcComplete = new FakeGuildCityCoordinator();
            openingArcComplete.PrepareTitleContinuity076(3);
            presenter = CreatePresenter(openingArcComplete);
            yield return null;

            AssertNamedTextEquals076(
                "Studio Title Chapter 076",
                "OPENING CONTRACTS  •  3 OF 3 COMPLETE");
            AssertNamedTextEquals076(
                "Studio Title Hook 076",
                "THREE OPENING CONTRACTS COMPLETE.\nTHE MAIN CARD CAMPAIGN CONTINUES.");
            AssertNamedTextEquals076(
                "Studio Title Promise 076",
                "Continue your saved adventure. Choose Campaign in the Guild Hall for the main three-card story.");
            const System.Reflection.BindingFlags privateInstance115 =
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(M1FlowPresenter).GetField("_screen", privateInstance115)
                .SetValue(presenter, M1Screen.GuildOperations);
            typeof(M1FlowPresenter).GetField("_guildCityTab017D", privateInstance115)
                .SetValue(presenter, "HALL");
            typeof(M1FlowPresenter).GetMethod("BuildCurrentScreen", privateInstance115)
                .Invoke(presenter, null);
            yield return null;
            AssertNamedTextContains076("Living Guild Hub Chapter 074",
                "OPENING CONTRACTS  •  3 OF 3 COMPLETE");
            AssertNamedTextContains076("Living Guild Hub Current Objective 074",
                "Three opening contracts complete. Continue the story from Campaign.");
            Assert.That(openingArcComplete.GuildCity017D.Contracts.Count(value => value.IsCompleted), Is.EqualTo(3),
                "Title and Home copy must preserve the three completed opening contract records.");
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator CompletedChapterTwoReturnsToHallWithClearChapterThreeObjective078()
        {
            var coordinator = new FakeGuildCityCoordinator();
            coordinator.PrepareTitleContinuity076(2);
            var presenter = CreatePresenter(coordinator);
            yield return null;

            AssertNamedTextEquals076(
                "Studio Title Chapter 076",
                "CHAPTER 3  •  KEEP THE RELIEF ROAD OPEN");
            const System.Reflection.BindingFlags privateInstance078 =
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic;
            var screenField078 = typeof(M1FlowPresenter).GetField(
                "_screen", privateInstance078);
            var guildTabField078 = typeof(M1FlowPresenter).GetField(
                "_guildCityTab017D", privateInstance078);
            var buildCurrentScreen078 = typeof(M1FlowPresenter).GetMethod(
                "BuildCurrentScreen", privateInstance078);
            Assert.That(screenField078, Is.Not.Null);
            Assert.That(guildTabField078, Is.Not.Null);
            Assert.That(buildCurrentScreen078, Is.Not.Null);
            screenField078.SetValue(presenter, M1Screen.GuildOperations);
            guildTabField078.SetValue(presenter, "HALL");
            buildCurrentScreen078.Invoke(presenter, null);
            yield return null;

            Assert.That(GameObject.Find("Living Guild Hub 074"), Is.Not.Null,
                "The completed rescue must return to the playable Hall, not strand the player on an expedition result.");
            AssertNamedTextContains076(
                "Living Guild Hub Chapter 074",
                "CHAPTER 3");
            AssertNamedTextContains076(
                "Living Guild Hub Current Objective 074",
                "Begin Chapter 3");
            AssertNamedTextContains076(
                "Living Guild Hub Current Objective 074",
                "relief road");
            var chapterThreeAction078 = FindButtonByName("Living Guild Hub Primary CTA 074");
            Assert.That(chapterThreeAction078, Is.Not.Null);
            Assert.That(chapterThreeAction078.GetComponentInChildren<Text>().text,
                Is.EqualTo("BEGIN CHAPTER 3  →"));
            AssertFocusedAction076("BEGIN CHAPTER 3  →");
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator LivingGuildHomeUsesFourActionsAndSecondaryCodesRoute110()
        {
            var coordinator = new FakeGuildCityCoordinator();
            var presenter = CreatePresenter(coordinator);
            yield return null;

            const System.Reflection.BindingFlags privateInstance084 =
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic;
            var screenField084 = typeof(M1FlowPresenter).GetField(
                "_screen", privateInstance084);
            var guildTabField084 = typeof(M1FlowPresenter).GetField(
                "_guildCityTab017D", privateInstance084);
            var buildCurrentScreen084 = typeof(M1FlowPresenter).GetMethod(
                "BuildCurrentScreen", privateInstance084);
            Assert.That(screenField084, Is.Not.Null);
            Assert.That(guildTabField084, Is.Not.Null);
            Assert.That(buildCurrentScreen084, Is.Not.Null);
            screenField084.SetValue(presenter, M1Screen.GuildOperations);
            guildTabField084.SetValue(presenter, "HALL");
            buildCurrentScreen084.Invoke(presenter, null);
            yield return null;

            var contracts084 = new[]
            {
                ("CONTRACT", "CAMPAIGN\nTHREE-CARD QUESTS"),
                ("ENDLESS_TOWER_081", "TOWER\nOPTIONAL BATTLES"),
                ("PARTY", "HEROES\nOWNED HEROES & UNIONS"),
                ("ARMORY", "EQUIPMENT\nGEAR & ITEMS")
            };
            var actions084 = contracts084.Select(contract =>
            {
                var button = FindButtonByName(
                    "Living Guild Hub Facility " + contract.Item1 + " 074");
                Assert.That(button, Is.Not.Null);
                Assert.That(button.GetComponentInChildren<Text>().text,
                    Is.EqualTo(contract.Item2));
                return button;
            }).ToArray();
            Assert.That(CountNamedObjects("Living Guild Hub Facility "), Is.EqualTo(4));
            Assert.That(GameObject.Find("Living Guild Hub Facility CITY_BUILD_MODE_078 074"),
                Is.Null);
            Assert.That(GameObject.Find("Living Guild Hub Roster 074"), Is.Null);
            Assert.That(GameObject.Find("Living Guild Hub Homecoming 076"), Is.Null);
            AssertNamedTextContains076("Living Guild Hub Chapter 074", "NEXT STORY");
            Assert.That(FindButtonByName("Living Guild Hub Primary CTA 074"), Is.Not.Null);
            Assert.That(actions084[0].navigation.selectOnRight, Is.EqualTo(actions084[1]));
            Assert.That(actions084[0].navigation.selectOnDown,
                Is.EqualTo(FindButtonByName("Living Guild Hub Primary CTA 074")));
            Assert.That(actions084[3].navigation.selectOnUp,
                Is.EqualTo(FindButtonByName("Living Guild Hub Primary CTA 074")));

            FindButtonByName("Living Guild Home Codes 110").onClick.Invoke();
            yield return null;
            Assert.That(guildTabField084.GetValue(presenter), Is.EqualTo("CODES"));
            AssertTextContains("BONUS CODES");
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator SaveReloadReattachClearsPriorOperationToastFromChapterTwoTitle076()
        {
            var presenter = CreatePresenter(new FakeGuildCityCoordinator());
            yield return null;

            var localStatus = typeof(M1FlowPresenter).GetField(
                "_localStatus",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            var localStatusPositive = typeof(M1FlowPresenter).GetField(
                "_localStatusPositive",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            Assert.That(localStatus, Is.Not.Null);
            Assert.That(localStatusPositive, Is.Not.Null);
            localStatus.SetValue(
                presenter,
                "The previous operation finished and this deliberately long toast belongs only to the old coordinator instance.");
            localStatusPositive.SetValue(presenter, true);

            presenter.Initialize(new FakeGuildCityCoordinator(firstChapterComplete: true));
            yield return null;

            Assert.That(GameObject.Find("Pass Status"), Is.Null);
            Assert.That(GameObject.Find("Notice Status"), Is.Null);
            AssertNamedTextEquals076(
                "Studio Title Chapter 076",
                "CHAPTER 2  •  THE DOOR INSIDE");

            foreach (var resolution076 in new[]
                     {
                         new Vector2(1280f, 800f),
                         new Vector2(1920f, 1080f)
                     })
            {
                ConfigureExpeditionCanvas074(resolution076.x, resolution076.y);
                var story076 = FindRectByPrefix074("Studio Title Story Card 076");
                LayoutRebuilder.ForceRebuildLayoutImmediate(story076);
                Canvas.ForceUpdateCanvases();
                AssertRectInside074(
                    story076,
                    FindRectByPrefix074("Studio Title Chapter 076"),
                    "saved-title chapter heading");
                AssertRectInside074(
                    story076,
                    FindRectByPrefix074("Studio Title Hook 076"),
                    "saved-title story hook");
                AssertRectInside074(
                    story076,
                    FindRectByPrefix074("Studio Title Promise 076"),
                    "saved-title story promise");
                AssertRectInside074(
                    story076,
                    FindButtonByName("Title Primary Play Now 062").GetComponent<RectTransform>(),
                    "saved-title continue action");
            }

            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator CompletedBaseM2GuildKeepsPlayNowPrimaryAndBattleEntrySecondary()
        {
            var coordinator = new FakeCoordinator(M1Screen.GuildOperations);
            var presenter = CreatePresenter(coordinator);
            yield return null;

            Assert.That(FindButton("CONTINUE GAME"), Is.Not.Null);
            Assert.That(FindButton("CONTINUE GAME").name, Is.EqualTo("Title Primary Play Now 062"));
            Assert.That(FindButton("QUICK BATTLE"), Is.Null);
            Assert.That(FindButton("PLAY A BATTLE NOW"), Is.Not.Null);
            Assert.That(FindButton("PLAY A BATTLE NOW").name, Is.EqualTo("Base M2 Battle Entry Compatibility 063"));
            Assert.That(FindButton("CONTINUE"), Is.Null);
            Click("PLAY A BATTLE NOW");
            yield return null;

            Assert.That(coordinator.StartBattleCalls, Is.EqualTo(1));
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator ReadyGuildKeepsPracticeBattleSecondaryAndOneClick071()
        {
            var coordinator = new FakeGuildCityCoordinator();
            var presenter = CreatePresenter(coordinator);
            yield return null;

            Assert.That(FindButton("OPTIONAL PRACTICE"), Is.Not.Null);
            Assert.That(FindButton("OPTIONAL PRACTICE").name, Is.EqualTo("Title Optional Practice 071"));
            Click("OPTIONAL PRACTICE");
            yield return null;

            Assert.That(coordinator.StartBattleCalls, Is.EqualTo(1));
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator PartiallyInitializedGuildShowsOptionalPracticeSetupGuard071()
        {
            var coordinator = new FakeGuildCityCoordinator();
            coordinator.State.OpeningUnionsLegal = false;
            coordinator.State.TwoUnionsLegal = false;
            var presenter = CreatePresenter(coordinator);
            yield return null;

            Assert.That(FindButton("PARTY SETUP"), Is.Not.Null,
                "A migrated or partial Guild must still show an honest, visible path to combat.");
            Click("PARTY SETUP");
            yield return null;

            Assert.That(coordinator.StartBattleCalls, Is.EqualTo(0),
                "The visible entry must preserve the existing legal-party guard.");
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator FreshTitleOffersOneStoryStartAndNoPracticeShortcut071()
        {
            var coordinator = new FakeGuildCityCoordinator();
            coordinator.State.HasCampaign = false;
            coordinator.State.HasSave = false;
            var presenter = CreatePresenter(coordinator);
            yield return null;

            var startNewGuild076 = FindButton("START NEW GUILD");
            Assert.That(startNewGuild076, Is.Not.Null);
            AssertFocusedAction076("START NEW GUILD");
            AssertNamedTextEquals076(
                "Studio Title Identity 076",
                "GUILDMASTER'S ORDER\nBRING THEM HOME");
            AssertNamedTextEquals076(
                "Studio Title Pillars 076",
                "SIX FOUNDERS WAIT IN THE HALL\nTEN LANTERNS ARE MISSING BELOW");
            AssertNamedTextEquals076("Studio Game Title 076", "GUILD OF WORLDS");
            AssertNamedTextEquals076("Studio Game Subtitle 076", "SECOND DIMENSION");
            var quiet076 = FindRectByPrefix074("Studio Title Quiet Utilities 076");
            var identity076 = FindTextByNamePrefix076("Studio Title Identity 076");
            var pillars076 = FindTextByNamePrefix076("Studio Title Pillars 076");
            var lockup076 = FindRectByPrefix074("Studio Title Lockup 076");
            var storyCard076 = FindRectByPrefix074("Studio Title Story Card 076");
            var titleStage076 = FindRectByPrefix074("Studio Title Stage 076");
            var kiriFrame076 = FindRectByPrefix074("Studio Title Kiri Hero Frame 076");
            var kiriViewport076 = FindRectByPrefix074("Studio Title Kiri Hero Viewport 076");
            var kiriArtwork076 = GameObject.Find("Studio Title Kiri Hero Artwork 076")
                ?.GetComponent<Image>();
            var gameTitle076 = FindTextByNamePrefix076("Studio Game Title 076");
            var gameSubtitle076 = FindTextByNamePrefix076("Studio Game Subtitle 076");
            Assert.That(titleStage076, Is.Not.Null);
            Assert.That(storyCard076, Is.Not.Null);
            Assert.That(kiriFrame076, Is.Not.Null);
            Assert.That(kiriViewport076, Is.Not.Null);
            Assert.That(kiriViewport076.GetComponent<RectMask2D>(), Is.Not.Null,
                "Kiri's title portrait must crop inside a real masked viewport.");
            Assert.That(kiriArtwork076, Is.Not.Null);
            Assert.That(kiriArtwork076.sprite, Is.Not.Null,
                "The title must introduce Kiri with authored person-first key art.");
            var kiriFitter076 = kiriArtwork076.GetComponent<AspectRatioFitter>();
            Assert.That(kiriFitter076, Is.Not.Null);
            Assert.That(kiriFitter076.aspectMode,
                Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent));
            foreach (var resolution076 in new[]
                     {
                         new Vector2(1280f, 800f),
                         new Vector2(1920f, 1080f)
                     })
            {
                ConfigureExpeditionCanvas074(resolution076.x, resolution076.y);
                LayoutRebuilder.ForceRebuildLayoutImmediate(lockup076);
                LayoutRebuilder.ForceRebuildLayoutImmediate(quiet076);
                LayoutRebuilder.ForceRebuildLayoutImmediate(titleStage076);
                LayoutRebuilder.ForceRebuildLayoutImmediate(kiriFrame076);
                LayoutRebuilder.ForceRebuildLayoutImmediate(kiriViewport076);
                Canvas.ForceUpdateCanvases();
                AssertRectInside074(lockup076, gameTitle076.rectTransform, "game title");
                AssertRectInside074(lockup076, gameSubtitle076.rectTransform, "franchise subtitle");
                AssertRectAbove074(
                    gameTitle076.rectTransform,
                    gameSubtitle076.rectTransform,
                    "game title and franchise subtitle");
                AssertTextFitsRect074(gameTitle076, "game title");
                AssertTextFitsRect074(gameSubtitle076, "franchise subtitle");
                AssertRectAbove074(
                    identity076.rectTransform,
                    pillars076.rectTransform,
                    "Guildmaster order and first-hour stakes");
                AssertRectInside074(quiet076, identity076.rectTransform, "Guildmaster order");
                AssertRectInside074(quiet076, pillars076.rectTransform, "first-hour stakes");
                AssertTextFitsRect074(identity076, "Guildmaster order");
                AssertTextFitsRect074(pillars076, "first-hour stakes");
                AssertRectInside074(titleStage076, kiriFrame076, "Kiri title hero frame");
                AssertRectInside074(kiriFrame076, kiriViewport076, "Kiri title crop viewport");
                var kiriFrameRect076 = WorldRect074(kiriFrame076);
                var storyRect076 = WorldRect074(storyCard076);
                var lockupRect076 = WorldRect074(lockup076);
                var quietRect076 = WorldRect074(quiet076);
                Assert.That(kiriFrameRect076.xMin,
                    Is.GreaterThanOrEqualTo(storyRect076.xMax - 0.5f),
                    "Kiri's hero art must not cover the first-hour story card.");
                Assert.That(kiriFrameRect076.xMin,
                    Is.GreaterThanOrEqualTo(lockupRect076.xMax - 0.5f),
                    "Kiri's hero art must not cover the game-title lockup.");
                Assert.That(kiriFrameRect076.yMin,
                    Is.GreaterThanOrEqualTo(quietRect076.yMax - 0.5f),
                    "Kiri's hero art must not cover title utilities or focus targets.");
                Assert.That(kiriFrameRect076.width, Is.GreaterThanOrEqualTo(280f));
                Assert.That(kiriFrameRect076.height, Is.GreaterThanOrEqualTo(430f));
            }
            Assert.That(gameTitle076.resizeTextMinSize,
                Is.GreaterThan(gameSubtitle076.resizeTextMaxSize),
                "GUILD OF WORLDS must read as the game title, with SECOND DIMENSION as its smaller franchise line.");
            var startNewGuildLabel076 = startNewGuild076.GetComponentInChildren<Text>();
            Assert.That(startNewGuildLabel076, Is.Not.Null);
            Assert.That(
                ContrastRatio076(
                    startNewGuildLabel076.color,
                    startNewGuild076.colors.selectedColor),
                Is.GreaterThanOrEqualTo(4.5f),
                "The focused title action must remain readable instead of turning black-on-blue.");
            Assert.That(FindButton("NEW GUILD"), Is.Null);
            Assert.That(FindButton("PRACTICE BATTLE"), Is.Null);
            Assert.That(FindButton("QUICK BATTLE — NO SETUP"), Is.Null);
            Click("START NEW GUILD");
            yield return null;

            AssertTextContains("SIGN THE SKYHOME CHARTER");
            Assert.That(coordinator.EquipmentReviewCalls, Is.EqualTo(0));
            Assert.That(coordinator.SaveReloadCalls, Is.EqualTo(0));
            Assert.That(coordinator.StartBattleCalls, Is.EqualTo(0));
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator FreshRuntimeCharterPreparesSavedGuildAndEntersLivingHall074()
        {
            var savePath = Path.Combine(
                Path.GetTempPath(),
                "sd071_arrival_route_" + Guid.NewGuid().ToString("N") + ".json");
            M1FlowPresenter presenter = null;
            try
            {
                var coordinator = new M1RuntimeCoordinator(
                    Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"),
                    savePath);
                presenter = CreatePresenter(coordinator);
                yield return null;

                AssertFocusedAction076("START NEW GUILD");
                Click("START NEW GUILD");
                yield return null;
                var arrival076 = UnityEngine.Object.FindFirstObjectByType<WalkableSkyhomeArrival071>();
                Assert.That(arrival076, Is.Not.Null,
                    "Starting a new Guild must first give the player control in Skyhome Market.");
                Assert.That(arrival076.CurrentObjectiveDistanceForVerification076,
                    Is.GreaterThan(arrival076.CurrentObjectiveInteractionRadiusForVerification076));
                var largestArrivalStep076 = DriveWalkableArrivalToObjective076(arrival076);
                Assert.That(arrival076.IsWithinCurrentObjectiveInteractionRangeForVerification076, Is.True);
                Assert.That(largestArrivalStep076, Is.LessThan(0.55f),
                    "The integrated title-to-charter route must reach the Hall with frame-sized motor steps.");
                arrival076.ApplyInteractionForVerification071();
                yield return null;
                Assert.That(GameObject.Find("Guild Charter Prologue 074"), Is.Not.Null);
                AssertTextContains("YOU ARE THE NEW GUILDMASTER");
                AssertTextContains("FIRST ORDER");
                AssertNamedTextEquals076(
                    "Guildmaster Signature Label 074",
                    "YOUR NAME ON THE CHARTER");
                var kiriFrame076 = GameObject.Find("Kiri Aetherheart Prologue Portrait 076");
                Assert.That(kiriFrame076, Is.Not.Null,
                    "The prologue must identify Kiri with an authored portrait, not an anonymous tile.");
                var kiriArtwork076 = kiriFrame076.transform
                    .Find("Portrait Backing/Portrait Artwork")
                    ?.GetComponent<Image>();
                Assert.That(kiriArtwork076, Is.Not.Null);
                Assert.That(kiriArtwork076.sprite, Is.Not.Null);
                Assert.That(kiriArtwork076.sprite.name,
                    Is.EqualTo("KIRI_AETHERHEART_PROLOGUE_CROP_076"));
                Assert.That(kiriArtwork076.preserveAspect, Is.False,
                    "Kiri's prologue crop must fill its authored frame instead of shrinking to the source aspect.");
                Assert.That(
                    M1VisualAssets.TryResolvePortrait(
                        "CANON_KIRI_AETHERHEART",
                        "CANON_KIRI_AETHERHEART",
                        "HUMAN",
                        "CANON_KIRI_AETHERHEART",
                        out var kiriSource076,
                        out var kiriSourceKey076),
                    Is.True,
                    "Kiri's canonical source portrait did not resolve: " + kiriSourceKey076);
                Assert.That(kiriArtwork076.sprite.texture, Is.SameAs(kiriSource076.texture));
                Assert.That(kiriArtwork076.sprite.rect.x,
                    Is.EqualTo(kiriSource076.rect.x + kiriSource076.rect.width * 0.07f).Within(0.1f));
                Assert.That(kiriArtwork076.sprite.rect.y,
                    Is.EqualTo(kiriSource076.rect.y + kiriSource076.rect.height * 0.51f).Within(0.1f));
                Assert.That(kiriArtwork076.sprite.rect.width,
                    Is.EqualTo(kiriSource076.rect.width * 0.86f).Within(0.1f));
                Assert.That(kiriArtwork076.sprite.rect.height,
                    Is.EqualTo(kiriSource076.rect.height * 0.44f).Within(0.1f));
                AssertFocusedAction076("SIGN THE CHARTER");
                Click("SIGN THE CHARTER");
                yield return null;

                Assert.That(GameObject.Find("Guild Charter Prologue 074"), Is.Null);
                Assert.That(GameObject.Find("Founding Company Presentation 076"), Is.Not.Null);
                Assert.That(GameObject.Find(
                    M1FlowPresenter.FoundingPreparationRootName078), Is.Not.Null);
                AssertTextContains("THE SIX WHO ANSWERED");
                AssertNamedTextContains076(
                    "Founding Preparation Title 078",
                    "MEET THE SIX WHO ANSWERED");
                AssertNamedTextContains076(
                    "Founding Preparation Step 078",
                    "RESCUE PREPARATION  •  STEP 1 OF 5");
                AssertNoTextContains("PEOPLE YOUR CHARTER WILL SIGN");
                var presentedFounderIds = coordinator.State.Applicants
                    .Where(value => value != null)
                    .Take(6)
                    .Select(value => value.RecruitId)
                    .ToArray();
                Assert.That(presentedFounderIds, Has.Length.EqualTo(6));
                Assert.That(presentedFounderIds.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(6));
                for (var founderIndex076 = 0;
                     founderIndex076 < presentedFounderIds.Length;
                     founderIndex076++)
                {
                    var recruitId = presentedFounderIds[founderIndex076];
                    Assert.That(GameObject.Find("Founding Applicant " + recruitId + " 076"), Is.Not.Null,
                        "The visible founder card must use the canonical recruit identity.");
                    var applicant = coordinator.State.Applicants.First(value =>
                        value != null && StringComparer.Ordinal.Equals(value.RecruitId, recruitId));
                    var roleBadge = GameObject.Find(
                        "Founding Applicant Role Color " + recruitId + " 076")
                        ?.GetComponent<Image>();
                    var roleLabel = FindTextByNamePrefix076(
                        "Founding Applicant Role " + recruitId + " 076");
                    Assert.That(roleBadge, Is.Not.Null,
                        "Every founder must carry the same visible role color used by Union Command.");
                    Assert.That(roleLabel, Is.Not.Null);
                    var promiseRole076 =
                        M1FlowPresenter.FoundingPromiseRoleForVerification079(
                            founderIndex076,
                            applicant.ObservedClass);
                    Assert.That(roleLabel.text, Is.EqualTo(
                        promiseRole076 + " PROMISE"),
                        "Founder badges teach the six rescue promises without rewriting the recruit's trained class.");
                    var expectedRoleColor =
                        M1FlowPresenter.UnionPlannerRoleColorForVerification074(
                            promiseRole076);
                    Assert.That(Mathf.Abs(roleBadge.color.r - expectedRoleColor.r), Is.LessThan(0.001f));
                    Assert.That(Mathf.Abs(roleBadge.color.g - expectedRoleColor.g), Is.LessThan(0.001f));
                    Assert.That(Mathf.Abs(roleBadge.color.b - expectedRoleColor.b), Is.LessThan(0.001f));
                }
                Assert.That(coordinator.State.Recruits.Any(value => value != null &&
                    presentedFounderIds.Contains(value.RecruitId, StringComparer.Ordinal)), Is.False,
                    "The six visible founders must not be silently signed before the player chooses a person.");

                var chosenLead076 = coordinator.State.Applicants
                    .Where(value => value != null)
                    .Take(6)
                    .First(value =>
                        value.ObservedClass.IndexOf(
                            "Ranger",
                            StringComparison.OrdinalIgnoreCase) >= 0 ||
                        value.ObservedClass.IndexOf(
                            "Rogue",
                            StringComparison.OrdinalIgnoreCase) >= 0);
                ClickButtonByName076(
                    "Founding Applicant " + chosenLead076.RecruitId + " 076");
                yield return null;
                AssertFocusedButtonByName076("Founding Lead Confirm 078");
                AssertFoundingLeadPresentationAtResolution078(
                    presentedFounderIds,
                    1280f,
                    800f);
                AssertFoundingLeadPresentationAtResolution078(
                    presentedFounderIds,
                    1920f,
                    1080f);
                ClickButtonByName076("Founding Lead Confirm 078");
                yield return null;
                Assert.That(coordinator.State.Recruits, Has.Count.EqualTo(1),
                    "Choosing a person must sign only that field lead before auto-fill.");
                Assert.That(coordinator.State.Recruits[0].RecruitId,
                    Is.EqualTo(chosenLead076.RecruitId));
                AssertNamedTextContains076(
                    "Founding Preparation Step 078",
                    "2 OF 5");

                ClickButtonByName076(
                    "Founding Equipment Choice " + EquipmentSlotIds.MainHand + " 078");
                yield return null;
                AssertFocusedButtonByName076("Founding Equipment Confirm 078");
                ClickButtonByName076("Founding Equipment Confirm 078");
                yield return null;
                Assert.That(coordinator.State.Recruits, Has.Count.EqualTo(6));
                var equippedLead076 = coordinator.State.Recruits.First(value =>
                    value.RecruitId == chosenLead076.RecruitId);
                Assert.That(equippedLead076.HasManualEquipAction, Is.True,
                    "The mandatory equipment choice must commit gameplay authority.");
                Assert.That(equippedLead076.Slots.Any(value =>
                    value.SlotId == EquipmentSlotIds.MainHand && value.IsLocked), Is.True);
                Assert.That(coordinator.State.Unions, Has.Count.EqualTo(3));
                Assert.That(coordinator.State.Unions.SelectMany(value =>
                        value.MemberRecruitIds ?? Array.Empty<string>())
                    .Contains(chosenLead076.RecruitId, StringComparer.Ordinal), Is.False,
                    "The chosen lead must remain visibly unplaced until the Union decision.");
                AssertNamedTextContains076(
                    "Founding Preparation Step 078",
                    "3 OF 5");

                var chosenUnion076 = coordinator.State.Unions[0];
                ClickButtonByName076(
                    "Founding Union Destination " + chosenUnion076.Index + " 078");
                yield return null;
                AssertFocusedButtonByName076("Founding Placement Confirm 078");
                AssertFoundingDestinationPresentationAtResolution078(
                    coordinator.State.Unions.Take(3).ToArray(),
                    1280f,
                    800f);
                AssertFoundingDestinationPresentationAtResolution078(
                    coordinator.State.Unions.Take(3).ToArray(),
                    1920f,
                    1080f);
                ClickButtonByName076("Founding Placement Confirm 078");
                yield return null;
                Assert.That(coordinator.State.Unions.First(value =>
                        value.Index == chosenUnion076.Index).MemberRecruitIds[0],
                    Is.EqualTo(chosenLead076.RecruitId),
                    "Explicit placement must make the selected person the Union lead.");
                AssertNamedTextContains076(
                    "Founding Preparation Step 078",
                    "4 OF 5");

                var chosenFormation076 = coordinator.State.Formations.Take(3).Skip(1).First();
                ClickButtonByName076(
                    "Founding Formation Choice " + chosenFormation076.Id + " 078");
                yield return null;
                AssertFocusedButtonByName076("Founding Formation Confirm 078");
                AssertFoundingFormationPresentationAtResolution078(
                    coordinator.State.Formations.Take(3).ToArray(),
                    1280f,
                    800f);
                AssertFoundingFormationPresentationAtResolution078(
                    coordinator.State.Formations.Take(3).ToArray(),
                    1920f,
                    1080f);
                ClickButtonByName076("Founding Formation Confirm 078");
                yield return null;
                Assert.That(coordinator.State.Unions.First(value =>
                        value.Index == chosenUnion076.Index).FormationId,
                    Is.EqualTo(chosenFormation076.Id));
                AssertNamedTextContains076(
                    "Founding Preparation Step 078",
                    "5 OF 5");
                AssertFocusedButtonByName076(
                    M1FlowPresenter.FoundingPreparationConfirmName078);
                AssertFoundingConfirmationPresentationAtResolution078(
                    coordinator,
                    chosenLead076.RecruitId,
                    chosenUnion076.Index,
                    1280f,
                    800f);
                AssertFoundingConfirmationPresentationAtResolution078(
                    coordinator,
                    chosenLead076.RecruitId,
                    chosenUnion076.Index,
                    1920f,
                    1080f);
                ClickButtonByName076(
                    M1FlowPresenter.FoundingPreparationConfirmName078);
                yield return null;
                yield return null;
                Assert.That(GameObject.Find(
                    M1FlowPresenter.FoundingPreparationRootName078), Is.Null);
                AssertTextContains("YOUR FOUNDING COMPANY");
                var presentedUnions = coordinator.State.Unions
                    .Where(value => value != null &&
                                    (value.MemberRecruitIds?.Count ?? 0) > 0)
                    .Take(3)
                    .ToArray();
                Assert.That(presentedUnions, Has.Length.EqualTo(3));
                for (var unionPosition = 0;
                     unionPosition < presentedUnions.Length;
                     unionPosition++)
                {
                    var union = presentedUnions[unionPosition];
                    Assert.That(GameObject.Find(
                        "Founding Union Summary " + union.Index + " 076"), Is.Not.Null);
                    var leader = coordinator.State.Recruits.First(value =>
                        value != null && StringComparer.Ordinal.Equals(
                            value.RecruitId,
                            union.MemberRecruitIds.First()));
                    var roleBadge = GameObject.Find(
                        "Founding Union Leader Role Color " + union.Index + " 076")
                        ?.GetComponent<Image>();
                    Assert.That(roleBadge, Is.Not.Null,
                        "Each founding Union must preserve its chosen team-job color.");
                    var expectedTeamColor = M1FlowPresenter
                        .FoundingDestinationColorForVerification078(unionPosition);
                    Assert.That(Mathf.Abs(roleBadge.color.r - expectedTeamColor.r), Is.LessThan(0.001f));
                    Assert.That(Mathf.Abs(roleBadge.color.g - expectedTeamColor.g), Is.LessThan(0.001f));
                    Assert.That(Mathf.Abs(roleBadge.color.b - expectedTeamColor.b), Is.LessThan(0.001f));
                    AssertNamedTextEquals076(
                        "Founding Union Leader Role " + union.Index + " 076",
                        M1FlowPresenter.FoundingDestinationRibbonForVerification078(
                            unionPosition));
                    AssertNamedTextContains076(
                        "Founding Union Leader Class " + union.Index + " 076",
                        M1FlowPresenter.UnionPlannerRoleDesignationForVerification074(
                            leader.ObservedClass));
                }
                AssertTextContains("SKYHOME GATEWARDENS");
                AssertTextContains("LANTERN SPEAR");
                AssertTextContains("WAYFINDERS");
                AssertTextContains("HOLD  •  PROTECTS ALLIES UNDER PRESSURE");
                AssertTextContains("BREAK  •  FOCUSES FORCE ON ONE OPENING");
                AssertTextContains("MOVE  •  ANSWERS CHANGING THREATS");
                Assert.That(presentedFounderIds.All(recruitId => coordinator.State.Recruits.Any(value =>
                    value != null && StringComparer.Ordinal.Equals(value.RecruitId, recruitId))), Is.True,
                    "The signed founder identities must exactly include the six people the player met.");
                var assignedRecruitIds = new HashSet<string>(
                    presentedUnions.SelectMany(value => value.MemberRecruitIds),
                    StringComparer.Ordinal);
                var reserveRecruits = coordinator.State.Recruits
                    .Where(value => value != null &&
                                    !assignedRecruitIds.Contains(value.RecruitId))
                    .ToArray();
                Assert.That(GameObject.Find("Founding Reserve Strip 076"), Is.Not.Null,
                    "The founding briefing needs one compact reserve summary instead of another portrait dashboard.");
                AssertNamedTextContains076(
                    "Founding Reserve Strip Text 076",
                    reserveRecruits.Length + " READY AT HALL");
                foreach (var recruit in reserveRecruits)
                    AssertNamedTextContains076(
                        "Founding Reserve Strip Text 076",
                        recruit.DisplayName);
                Assert.That(GameObject.Find("Founding Union Lesson 076"), Is.Null,
                    "Battle instructions belong in the battle, not on the founding-company reveal.");
                Assert.That(GameObject.Find("Founding Hall Crew 076"), Is.Null,
                    "Hall guildmates should be introduced in the Hall instead of a second portrait dashboard.");
                AssertNamedTextContains076(
                    "Founding First Order Text 076",
                    "THE BELL BENEATH SKYHOME");
                AssertFocusedAction076("ENTER SKYHOME GUILD HALL");
                Click("ENTER SKYHOME GUILD HALL");
                yield return null;

                var cityAfterArrival = coordinator.GuildCity017D;
                Assert.That(GameObject.Find("Living Guild Hub 074"), Is.Not.Null,
                    "Living Guild Hall missing after charter. City available=" + cityAfterArrival.IsAvailable +
                    ", city error=" + cityAfterArrival.Error +
                    ", status=" + coordinator.State.StatusMessage +
                    ", resume=" + coordinator.State.ResumeScreen +
                    ", recruits=" + coordinator.State.Recruits.Count +
                    ", unions=" + coordinator.State.Unions.Count + ".");
                Assert.That(GameObject.Find("Living Guild Hub Persistent Status 074"), Is.Not.Null);
                Assert.That(GameObject.Find("Living Guild Hub Objective Card 074"), Is.Not.Null);
                Assert.That(CountNamedObjects("Living Guild Hub Facility "), Is.EqualTo(4));
                var homeActionContracts084 = new[]
                {
                    ("CONTRACT", "CAMPAIGN\nTHREE-CARD QUESTS"),
                    ("ENDLESS_TOWER_081", "TOWER\nOPTIONAL BATTLES"),
                    ("PARTY", "HEROES\nOWNED HEROES & UNIONS"),
                    ("ARMORY", "EQUIPMENT\nGEAR & ITEMS")
                };
                var homeActions084 = homeActionContracts084.Select(contract =>
                {
                    var action = FindButtonByName(
                        "Living Guild Hub Facility " + contract.Item1 + " 074");
                    Assert.That(action, Is.Not.Null, "Missing home action: " + contract.Item1);
                    Assert.That(action.GetComponentInChildren<Text>().text,
                        Is.EqualTo(contract.Item2));
                    return action;
                }).ToArray();
                Assert.That(GameObject.Find("Living Guild Hub Facility CITY_BUILD_MODE_078 074"),
                    Is.Null, "City remains outside the four main Home actions.");
                Assert.That(GameObject.Find("Living Guild Hub Roster 074"), Is.Null);
                Assert.That(GameObject.Find("Living Guild Hub Homecoming 076"), Is.Null);
                AssertNamedTextContains076("Living Guild Hub Chapter 074", "NEXT STORY");
                AssertNamedTextContains076("Living Guild Hub Rank Resources Day Reputation 074", "GUILD LEVEL");
                AssertNamedTextContains076("Living Guild Hub Rank Resources Day Reputation 074", "GUILD XP");
                AssertNamedTextContains076("Living Guild Hub Rank Resources Day Reputation 074", "XP TO SPEND");
                AssertNamedTextDoesNotContain076("Living Guild Hub Rank Resources Day Reputation 074", "DAY");
                AssertNamedTextDoesNotContain076("Living Guild Hub Rank Resources Day Reputation 074", "REPUTATION");
                Assert.That(homeActions084[0].navigation.selectOnRight,
                    Is.EqualTo(homeActions084[1]));
                Assert.That(homeActions084[0].navigation.selectOnDown,
                    Is.EqualTo(FindButtonByName("Living Guild Hub Primary CTA 074")));
                Assert.That(homeActions084[3].navigation.selectOnUp,
                    Is.EqualTo(FindButtonByName("Living Guild Hub Primary CTA 074")));
                ConfigureExpeditionCanvasFor1280By800074();
                var hallObjective = FindRectByPrefix074("Living Guild Hub Current Objective 074");
                var hallAction = FindButtonByName("Living Guild Hub Primary CTA 074")
                    ?.GetComponent<RectTransform>();
                Assert.That(hallObjective, Is.Not.Null);
                Assert.That(hallAction, Is.Not.Null);
                AssertRectAbove074(hallObjective, hallAction, "Hall objective and primary action");
                AssertTextFitsRect074(hallObjective.GetComponent<Text>(), "Hall Chapter 1 objective");
                Assert.That(hallAction.rect.height, Is.GreaterThanOrEqualTo(64f),
                    "The Living Hall's primary story action must remain a 64 px controller/mouse target at 1280x800.");
                Assert.That(hallAction.GetComponent<Button>()
                        .GetComponentInChildren<Text>().text,
                    Is.EqualTo("OPEN THE RESCUE CONTRACT  →"),
                    "The ready ten-person founding company must go directly to its first XP-earning mission.");
                Assert.That(UnityEngine.Object.FindFirstObjectByType<WalkableGuildHall069>(), Is.Null,
                    "Release 074 intentionally replaces the unreadable walking shell with one fixed Hall screen.");
                var ambienceRoot = presenter.transform.Find("Studio Ambience 076");
                Assert.That(ambienceRoot, Is.Not.Null);
                var ambienceSources = ambienceRoot.GetComponents<AudioSource>();
                Assert.That(ambienceSources, Has.Length.EqualTo(1));
                Assert.That(ambienceSources[0].loop, Is.True);
                Assert.That(ambienceSources[0].spatialBlend, Is.EqualTo(0f));
                Assert.That(Resources.Load<AudioClip>(
                    "SecondDimension/GuildCity017F/Audio/AMB_HALL_RUINED_017F"), Is.Not.Null);
                Assert.That(Resources.Load<AudioClip>(
                    "SecondDimension/GuildCity017F/Audio/AMB_CAMP_REST_017F"), Is.Not.Null);
                Assert.That(coordinator.State.HasCampaign, Is.True);
                Assert.That(coordinator.State.ResumeScreen, Is.EqualTo(M1Screen.GuildOperations));
                Assert.That(coordinator.State.GuildmasterName, Is.EqualTo("Skyhome Guildmaster"));
                Assert.That(coordinator.State.Recruits, Has.Count.EqualTo(10));
                var activeUnions = coordinator.State.Unions
                    .Where(value => value.MemberRecruitIds != null &&
                                    value.MemberRecruitIds.Count > 0)
                    .ToArray();
                Assert.That(activeUnions, Has.Length.EqualTo(3));
                var expectedOpeningFormationIds076 = new[]
                    {
                        chosenFormation076.Id
                    }
                    .Concat(coordinator.State.Formations
                        .Take(3)
                        .Select(value => value.Id)
                        .Where(value => !StringComparer.Ordinal.Equals(
                            value,
                            chosenFormation076.Id)))
                    .ToArray();
                Assert.That(
                    activeUnions.Select(value => value.FormationId).ToArray(),
                    Is.EqualTo(expectedOpeningFormationIds076),
                    "The chosen lead formation must survive auto-fill while all three starter formations teach distinct jobs.");
                Assert.That(activeUnions.First(value =>
                        value.Index == chosenUnion076.Index).MemberRecruitIds[0],
                    Is.EqualTo(chosenLead076.RecruitId),
                    "Final rescue auto-fill must preserve the player's person and placement decisions.");
                Assert.That(activeUnions.All(value => value.MemberRecruitIds.Count == 3), Is.True);
                Assert.That(activeUnions.SelectMany(value => value.MemberRecruitIds)
                    .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(9));
                Assert.That(coordinator.State.OpeningUnionsLegal, Is.True);
                Assert.That(coordinator.State.UsedUnionCount, Is.EqualTo(3));
                Assert.That(GameObject.Find("Sign Emergency Charter 071"), Is.Null);
                Assert.That(GameObject.Find("Enter Walkable Guild Hall 071"), Is.Null);
                Assert.That(GameObject.Find("Founding Company Presentation 076"), Is.Null);

                var persisted = new AtomicSaveStore().ReadWithRecovery(savePath);
                Assert.That(persisted.IsSuccess, Is.True, string.Join("\n", persisted.Errors));
                Assert.That(persisted.Value.CanonicalStateHash,
                    Is.EqualTo(coordinator.State.CanonicalStateHash));
                var restored = new M1RuntimeCoordinator(
                    Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"),
                    savePath);
                Assert.That(restored.State.CanonicalStateHash,
                    Is.EqualTo(coordinator.State.CanonicalStateHash));
                Assert.That(restored.State.Recruits, Has.Count.EqualTo(10));
                Assert.That(restored.State.Unions
                        .Where(value => value.MemberRecruitIds != null &&
                                        value.MemberRecruitIds.Count > 0)
                        .Select(value => value.FormationId)
                        .ToArray(),
                    Is.EqualTo(activeUnions.Select(value => value.FormationId).ToArray()),
                    "The distinct opening formation lesson must survive save and reload.");
                Assert.That(restored.State.Unions
                    .Where(value => value.MemberRecruitIds != null &&
                                    value.MemberRecruitIds.Count > 0)
                    .Select(value => string.Join("|", value.MemberRecruitIds))
                    .ToArray(),
                    Is.EqualTo(activeUnions
                        .Select(value => string.Join("|", value.MemberRecruitIds))
                        .ToArray()));
            }
            finally
            {
                if (presenter != null) UnityEngine.Object.Destroy(presenter.gameObject);
                foreach (var path in new[] { savePath, savePath + ".bak", savePath + ".tmp" })
                    if (File.Exists(path)) File.Delete(path);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator InterruptedFoundingSetupOpensColorCodedDragRecovery076()
        {
            var coordinator = new FakeCoordinator(
                M1Screen.FirstHourGuildReady,
                partialUnions: true);
            var presenter = CreatePresenter(coordinator);
            yield return null;

            Click("CONTINUE GAME");
            yield return null;
            Assert.That(GameObject.Find("Founding Company Presentation 076"), Is.Not.Null);
            AssertFocusedAction076("WELCOME THE SIX FOUNDERS");
            Click("WELCOME THE SIX FOUNDERS");
            yield return null;

            Assert.That(GameObject.Find("Founding Company Presentation 076"), Is.Null,
                "An interrupted setup must not strand the player on the same failed welcome screen.");
            Assert.That(GameObject.Find("Union Planner 074"), Is.Not.Null,
                "An interrupted setup must recover into the playable drag planner.");
            AssertTextContains("OLDER PARTIAL PARTY WAS FOUND");
            Assert.That(GameObject.Find("Union Planner Save And Return 074"), Is.Not.Null);
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator SaveFailureDetailsStayHiddenUntilPlayerRequestsThem071()
        {
            var coordinator = new FakeGuildCityCoordinator();
            coordinator.State.HasCampaign = false;
            coordinator.State.HasSave = true;
            coordinator.State.SaveRecoveryDiagnostic =
                "Primary save failed: Canonical state hash mismatch.";
            var presenter = CreatePresenter(coordinator);
            yield return null;

            Assert.That(FindButton("START NEW GUILD"), Is.Not.Null);
            Assert.That(FindButton("SAVE RECOVERY DETAILS"), Is.Not.Null);
            AssertNoTextContains("Canonical state hash mismatch");

            Click("SAVE RECOVERY DETAILS");
            yield return null;

            AssertTextContains("previous save was left untouched");
            AssertTextContains("Canonical state hash mismatch");
            yield return Cleanup(presenter);
        }

        [Test]
        public void GuidedFirstHallImprovementBuildsOnePreferredFacilityThroughCoordinator069()
        {
            var coordinator = new FakeGuildCityCoordinator(firstChapterComplete: true);
            coordinator.PrepareFirstHallImprovement069();

            Assert.That(M1FlowPresenter.NeedsGuidedFirstHallImprovement069(coordinator.GuildCity017D), Is.True);
            var result = M1FlowPresenter.ApplyGuidedFirstHallImprovement069(coordinator);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.Message, Does.Contain("Contract House"));
            Assert.That(result.Message, Does.Contain("in the Hall"));
            Assert.That(result.Message, Does.Contain("saved"));
            Assert.That(result.Message, Does.Not.Contain("GC017D_PLOT_"),
                "Player-facing Hall feedback must never expose an internal plot ID.");
            Assert.That(coordinator.PlaceBuildingCalls, Is.EqualTo(1));
            Assert.That(coordinator.LastPlacedPlotId, Is.EqualTo("GC017D_PLOT_01"));
            Assert.That(coordinator.LastPlacedBuildingId, Is.EqualTo("GC017D_BUILD_CONTRACT_HOUSE"));
            Assert.That(coordinator.GuildCity017D.PlacedBuildingCount, Is.EqualTo(1));
            Assert.That(M1FlowPresenter.NeedsGuidedFirstHallImprovement069(coordinator.GuildCity017D), Is.False);

            var repeat = M1FlowPresenter.ApplyGuidedFirstHallImprovement069(coordinator);
            Assert.That(repeat.Succeeded, Is.True);
            Assert.That(coordinator.PlaceBuildingCalls, Is.EqualTo(1),
                "Returning to Mira must not spend a second charter credit or place a second facility.");
        }

        [Test]
        public void RequiredSeventhRecruitInvitesOnlyAnEarnedContactWithoutPaidRefresh124()
        {
            var coordinator = new FakeGuildCityCoordinator();
            coordinator.PrepareStaleApplicantBoard069(produceRecruitableApplicant: true);

            var result = M1FlowPresenter.PrepareRequiredApplicantBoard069(coordinator);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(coordinator.CommitBoardCalls, Is.EqualTo(1));
            Assert.That(coordinator.RefreshBoardCalls, Is.EqualTo(0));
            Assert.That(M1FlowPresenter.HasRecruitableApplicant069(coordinator.GuildCity017D), Is.True);
        }

        [Test]
        public void RequiredSeventhRecruitWithoutEarnedContactsNeverRefreshesOrCharges124()
        {
            var coordinator = new FakeGuildCityCoordinator();
            coordinator.PrepareStaleApplicantBoard069(produceRecruitableApplicant: false);

            var result = M1FlowPresenter.PrepareRequiredApplicantBoard069(coordinator);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Does.Contain("No recruitable applicant"));
            Assert.That(coordinator.RefreshBoardCalls, Is.EqualTo(0));
        }

        [Test]
        public void SuccessfulRequiredSeventhRecruitReturnsToGuidedHall069()
        {
            var state = new GuildCityPresentationState017D { TotalRecruitCount = 7 };
            Assert.That(M1FlowPresenter.ShouldReturnToHallAfterRequiredRecruit069(
                6,
                M1CommandResult.Success("Saved permanently."),
                state), Is.True);
            Assert.That(M1FlowPresenter.ShouldReturnToHallAfterRequiredRecruit069(
                7,
                M1CommandResult.Success("Optional recruit saved."),
                new GuildCityPresentationState017D { TotalRecruitCount = 8 }), Is.False,
                "Optional later recruitment must retain the multi-applicant conversation behavior.");
            Assert.That(M1FlowPresenter.ShouldReturnToHallAfterRequiredRecruit069(
                6,
                M1CommandResult.Failure("Not saved."),
                state), Is.False);
        }

        [UnityTest]
        public IEnumerator MainMenuPlacesSingleStoryStartInsideTheFixedStudioTitle071()
        {
            var coordinator = new FakeCoordinator(M1Screen.ApplicantBoard);
            coordinator.State.HasCampaign = false;
            coordinator.State.HasSave = false;
            var presenter = CreatePresenter(coordinator);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            var proofCanvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .FirstOrDefault(value => StringComparer.Ordinal.Equals(value.name, "M1 Playable Proof Canvas"));
            Assert.That(proofCanvas, Is.Not.Null, "M1 proof canvas was not created.");

            var newGuild = FindButton("START NEW GUILD");
            var storyCard = GameObject.Find("Studio Title Story Card 076")
                ?.GetComponent<RectTransform>();
            Assert.That(proofCanvas.GetComponentInChildren<ScrollRect>(), Is.Null,
                "The fixed studio title should never require scrolling.");
            Assert.That(newGuild, Is.Not.Null, "The single START NEW GUILD action was not created.");
            Assert.That(storyCard, Is.Not.Null, "The fixed studio story card was not created.");
            Assert.That(FindButton("NEW GUILD"), Is.Null, "The title must not duplicate its story-start action.");
            var newGuildRect = newGuild.GetComponent<RectTransform>();
            AssertRectInside074(storyCard, newGuildRect, "studio title primary action");
            Assert.That(newGuildRect.rect.height, Is.GreaterThanOrEqualTo(80f));
            var storySurface = storyCard.GetComponent<Image>();
            Assert.That(storySurface, Is.Not.Null);
            Assert.That(storySurface.sprite, Is.Not.Null,
                "The studio story card must use the premium cut-corner surface.");
            Assert.That(storySurface.type, Is.EqualTo(Image.Type.Sliced));
            yield return Cleanup(presenter);
        }

        [Test]
        public void PersistedScoutingReportRestoresExactObservedFieldsAndRejectsWrongIdentity()
        {
            const string json = "{\"recruitId\":\"RECRUIT_1\",\"displayName\":\"Applicant 1\",\"raceId\":\"HUMAN\",\"homeCommunityId\":\"SKYHOME\",\"startingClassId\":\"CLASS_GUARDIAN\",\"backgroundId\":\"BACKGROUND_WARDEN\",\"visibleTraitIds\":[\"TRAIT_STEADY\"],\"leadershipEstimate\":{\"range\":[41,55],\"confidence\":80},\"disciplineEstimates\":{},\"growthAssessment\":\"WITHHELD\",\"qualityProfile\":\"SCOUTED\",\"exactPotentialDisplayed\":false,\"hiddenTraitRevealed\":false,\"hiddenDataWithheld\":true,\"scoutingAccuracy\":72}";

            var restored = M1RuntimeCoordinator.TryRestorePersistedScoutingReport(
                "RECRUIT_1", json, out var report, out var error);
            Assert.That(restored, Is.True, error);
            Assert.That(report.ScoutingAccuracy, Is.EqualTo(72));
            Assert.That(report.BackgroundId, Is.EqualTo("BACKGROUND_WARDEN"));
            Assert.That(report.VisibleTraitIds, Is.EqualTo(new[] { "TRAIT_STEADY" }));
            Assert.That(report.LeadershipEstimate.Range, Is.EqualTo(new[] { 41, 55 }));

            Assert.That(M1RuntimeCoordinator.TryRestorePersistedScoutingReport(
                "RECRUIT_2", json, out _, out var mismatch), Is.False);
            Assert.That(mismatch, Does.Contain("identity mismatch"));
        }

        [Test]
        public void PortraitLookupPrefersStableRecruitAndSignatureBeforeSeedAndRace()
        {
            var keys = M1VisualAssets.PortraitResourceKeys(
                "CAMPAIGN_RECRUIT_17",
                "ABC123",
                "RACE_HUMAN",
                "SIGREC_MAREN_HOLT");

            Assert.That(keys, Is.EqualTo(new[]
            {
                "SecondDimension/Art/Portraits/Recruits/SIGREC_MAREN_HOLT",
                "SecondDimension/Art/Portraits/Recruits/CAMPAIGN_RECRUIT_17",
                "SecondDimension/Art/Portraits/PORTRAIT_SIGNATURE_01",
                "SecondDimension/Art/Portraits/Seeds/ABC123",
                "SecondDimension/Art/Portraits/Races/HUMAN"
            }));
            Assert.That(M1VisualAssets.StableIndex("ABC123", 7), Is.EqualTo(M1VisualAssets.StableIndex("ABC123", 7)));
            Assert.That(M1VisualAssets.Initials("Maren Holt"), Is.EqualTo("MH"));

            var garaA = M1VisualAssets.BuildPortraitDescriptor(
                "PROC_36344E2400DC98B6", "234B7103DA5CD0DEC716E0C3", "DOG_TRIBE", "PROC_36344E2400DC98B6");
            var garaB = M1VisualAssets.BuildPortraitDescriptor(
                "PROC_36344E2400DC98B6", "234B7103DA5CD0DEC716E0C3", "DOG_TRIBE", "PROC_36344E2400DC98B6");
            var daeven = M1VisualAssets.BuildPortraitDescriptor(
                "PROC_F85A4CAA747BC8C6", "E9459BD93D70BC112D1E101E", "DEMON_HERITAGE", "PROC_F85A4CAA747BC8C6");
            var maren = M1VisualAssets.BuildPortraitDescriptor(
                "SIGI_4559425B6CF6CBE6", "B38D49E29D687F7ACC6CD872", "HUMAN", "SIGREC_MAREN_HOLT");
            var odelia = M1VisualAssets.BuildPortraitDescriptor(
                "SIGI_CE2768FD5A985F8B", "8D1AC991FF9F4DD0D78A0F78", "HUMAN", "SIGREC_ODELIA_FEN");

            Assert.That(garaA.DescriptorHash, Is.EqualTo(garaB.DescriptorHash), "Portrait descriptors must be stable across reopen.");
            Assert.That(garaA.LayerIds.Count, Is.EqualTo(22));
            Assert.That(daeven.LayerIds.Count, Is.EqualTo(22));
            Assert.That(garaA.LayerIds[2], Is.Not.EqualTo(daeven.LayerIds[2]), "Adjacent applicants must not reuse the same face descriptor.");
            Assert.That(garaA.LayerIds[7], Is.Not.EqualTo(daeven.LayerIds[7]), "Adjacent applicants must not reuse the same hair descriptor.");
            Assert.That(garaA.LayerIds[11], Is.Not.EqualTo(daeven.LayerIds[11]), "Adjacent applicants must not reuse the same outfit descriptor.");
            Assert.That(maren.IsBespokeSignature, Is.True);
            Assert.That(odelia.IsBespokeSignature, Is.True);
            Assert.That(maren.SourceKind, Is.EqualTo("BESPOKE_SIGNATURE"));
            Assert.That(odelia.SourceKind, Is.EqualTo("BESPOKE_SIGNATURE"));

            foreach (var visualId in new[]
                     {
                         "SWORD", "DAGGER", "SPEAR", "AXE", "BOW", "STAFF",
                         "SHIELD", "ARMOR", "ACCESSORY", "REMEDY_KIT"
                     })
            {
                Assert.That(
                    M1VisualAssets.TryResolveEquipment(visualId, out var equipmentSprite, out var equipmentKey),
                    Is.True,
                    "The packaged equipment atlas cell did not resolve: " + equipmentKey);
                Assert.That(equipmentSprite, Is.Not.Null);
                Assert.That(equipmentSprite.rect.width, Is.GreaterThan(0f));
                Assert.That(equipmentSprite.rect.height, Is.GreaterThan(0f));
                Assert.That(
                    Mathf.Abs(equipmentSprite.rect.width - equipmentSprite.rect.height),
                    Is.LessThanOrEqualTo(1f),
                    "Unity may resize the NPOT source atlas, but every runtime cell must remain square.");
            }
            Assert.That(
                M1VisualAssets.EquipmentVisualId(
                    "CA002_WPN_DAGGER_NIGHTGLASS_T2",
                    "SLOT_OFF_HAND",
                    new[] { "DAGGER", "WEAPON", "WEAPON_FAMILY_DAGGER" }),
                Is.EqualTo("DAGGER"),
                "A dagger's authoritative family must win over the off-hand shield fallback.");
            Assert.That(M1VisualAssets.EquipmentRarityTierId(string.Empty), Is.EqualTo("BASIC"));
            Assert.That(M1VisualAssets.EquipmentRarityTierId("QUALITY_REINFORCED"), Is.EqualTo("COMMON"));
            Assert.That(M1VisualAssets.EquipmentRarityTierId("QUALITY_MASTERWORK"), Is.EqualTo("RARE"));
            Assert.That(M1VisualAssets.EquipmentRarityTierId("QUALITY_EVOLVED"), Is.EqualTo("LEGENDARY"));
            Assert.That(M1VisualAssets.EquipmentRarityTierId("QUALITY_EDITED_RELIC"), Is.EqualTo("GODLY"));
            Assert.That(M1VisualAssets.ClassColorDesignation("CLASS_GUARDIAN"), Is.EqualTo("GUARDIAN"));
            Assert.That(M1VisualAssets.ClassColorDesignation("Warrior"), Is.EqualTo("WARRIOR"));
            Assert.That(M1VisualAssets.ClassColorDesignation("Tend Ranger Medic"), Is.EqualTo("RANGER"));
            Assert.That(M1VisualAssets.ClassColorDesignation("Class Tend Generalist"), Is.EqualTo("RANGER"));
            Assert.That(M1VisualAssets.ClassColorDesignation("Rogue"), Is.EqualTo("ROGUE"));
            Assert.That(M1VisualAssets.ClassColorDesignation("Guard Mystic"), Is.EqualTo("GUARDIAN"));
            Assert.That(M1VisualAssets.ClassColorDesignation("Mage"), Is.EqualTo("MAGE"));
            Assert.That(M1VisualAssets.ClassColorDesignation("Priest"), Is.EqualTo("PRIEST"));
            Assert.That(
                M1VisualAssets.OpeningBaseStatIndex("{\"statTendencies\":{\"STR\":{\"baseIndex\":91}}}", "STR"),
                Is.EqualTo(91));
            Assert.That(M1VisualAssets.OpeningBaseStatIndex("not json", "STR"), Is.Zero);
            Assert.That(new[] { "Guardian", "Warrior", "Ranger", "Rogue", "Mage", "Priest" }
                .Select(M1VisualAssets.ClassColorDisplayName)
                .Distinct(StringComparer.Ordinal)
                .Count(), Is.EqualTo(6), "Every base class needs a distinct named color designation.");
        }

        [Test]
        public void BackdropRolesUseRelease071ProductionEnvironmentsAndLocationFirstResourceIds()
        {
            Assert.That(
                M1VisualAssets.BackdropResourceKey(M1VisualAssets.BackdropRole.SkyhomeTitle),
                Is.EqualTo("SecondDimension/Art/FirstHour071/Environments/SKYHOME_MARKET_GAMEPLAY_PLATE_071"));
            Assert.That(
                M1VisualAssets.BackdropResourceKey(M1VisualAssets.BackdropRole.SkyhomeHall),
                Is.EqualTo("SecondDimension/Art/FirstHour071/Environments/GUILD_HALL_GAMEPLAY_PLATE_071"));
            Assert.That(
                M1VisualAssets.BackdropResourceKey(M1VisualAssets.BackdropRole.ApplicantBoard),
                Is.EqualTo("SecondDimension/Art/Backgrounds/ANCHOR_03_APPLICANTS"));
            Assert.That(
                M1VisualAssets.BackdropResourceKey(M1VisualAssets.BackdropRole.GuildHallStage01),
                Is.EqualTo("SecondDimension/Art/FirstHour071/Environments/GUILD_HALL_GAMEPLAY_PLATE_071"));
            Assert.That(
                M1VisualAssets.BackdropResourceKey(M1VisualAssets.BackdropRole.RecruitDossier),
                Is.EqualTo("SecondDimension/Art/Backgrounds/BG_RECRUIT_DOSSIER_ALCOVE"));
            Assert.That(
                M1VisualAssets.BackdropResourceKey(M1VisualAssets.BackdropRole.QuartermasterArmory),
                Is.EqualTo("SecondDimension/Art/Backgrounds/BG_QUARTERMASTER_ARMORY"));
            Assert.That(
                M1VisualAssets.BackdropResourceKey(M1VisualAssets.BackdropRole.UnionStrategy),
                Is.EqualTo("SecondDimension/Art/Backgrounds/BG_UNION_STRATEGY_CHAMBER"));

            foreach (M1VisualAssets.BackdropRole role in Enum.GetValues(typeof(M1VisualAssets.BackdropRole)))
            {
                Assert.That(
                    M1VisualAssets.TryResolveBackdrop(role, out var sprite, out var resourceKey),
                    Is.True,
                    "The packaged backdrop did not resolve from Resources: " + resourceKey);
                Assert.That(sprite, Is.Not.Null);
            }

            foreach (var portraitId in new[]
                     {
                         "PROC_36344E2400DC98B6",
                         "PROC_F85A4CAA747BC8C6",
                         "PROC_5B14E7816E55FFB5",
                         "PROC_748DD03A23E1FEB0",
                         "SIGREC_MAREN_HOLT",
                         "SIGREC_ODELIA_FEN"
                     })
            {
                Assert.That(
                    M1VisualAssets.TryResolvePortrait(
                        portraitId,
                        portraitId,
                        "HUMAN",
                        portraitId,
                        out var sprite,
                        out var resourceKey),
                    Is.True,
                    "The packaged portrait did not resolve from Resources: " + resourceKey);
                Assert.That(sprite, Is.Not.Null);
                Assert.That(
                    M1VisualAssets.TryResolveBattleStandee(
                        portraitId,
                        portraitId,
                        "HUMAN",
                        portraitId,
                        out var standee,
                        out var standeeResourceKey),
                    Is.True,
                    "The packaged full-body battle standee did not resolve from Resources: " + standeeResourceKey);
                Assert.That(standee, Is.Not.Null);
            }
            Assert.That(
                M1VisualAssets.TryResolvePortrait(
                    "CANDIDATE_RECURRING_068",
                    "RECURRING_VISUAL_SEED_068",
                    "ORC",
                    "SIG_W01_02",
                    out var recurringPortrait069,
                    out var recurringPortraitKey069),
                Is.True,
                "Recurring applicants must show real character art instead of an initials tile or class emblem.");
            Assert.That(recurringPortrait069, Is.Not.Null);
            Assert.That(recurringPortraitKey069,
                Does.Contain("Portraits/Races/ORC/"),
                "Recurring applicants should use the expanded deterministic race portrait pool before the one-face compatibility fallback.");
            var recurringOrcKeys086 = new[]
                {
                    "RECURRING_VISUAL_SEED_068",
                    "RECURRING_VISUAL_SEED_086_A",
                    "RECURRING_VISUAL_SEED_086_B",
                    "RECURRING_VISUAL_SEED_086_C"
                }
                .Select(seed =>
                {
                    Assert.That(M1VisualAssets.TryResolvePortrait(
                        "CANDIDATE_" + seed,
                        seed,
                        "ORC",
                        string.Empty,
                        out var portrait,
                        out var key), Is.True);
                    Assert.That(portrait, Is.Not.Null);
                    return key;
                })
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            Assert.That(recurringOrcKeys086.Length, Is.GreaterThanOrEqualTo(2),
                "Stable visual seeds should visibly diversify applicants of the same race.");
            Assert.That(M1VisualAssets.TryResolveBattleBackdrop(out var battleBackdrop, out var battleBackdropKey),
                Is.True, battleBackdropKey);
            Assert.That(battleBackdrop, Is.Not.Null);
            Assert.That(battleBackdropKey,
                Is.EqualTo("SecondDimension/Art/FirstHour071/Environments/GATEHOUSE_BOSS_ARENA_071"));
            Assert.That(M1VisualAssets.TryResolveEnemyBattleStandee(out var enemyStandee, out var enemyStandeeKey),
                Is.True, enemyStandeeKey);
            Assert.That(enemyStandee, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator M1RootKeepsReferenceSafeMarginsInsideDeviceSafeArea()
        {
            var presenter = CreatePresenter(new FakeCoordinator(M1Screen.ApplicantBoard));
            yield return null;

            var screenRoot = UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None)
                .FirstOrDefault(value => StringComparer.Ordinal.Equals(value.name, "M1 Screen Root"));
            Assert.That(screenRoot, Is.Not.Null);
            Assert.That(screenRoot.offsetMin.x, Is.GreaterThanOrEqualTo(96f));
            Assert.That(screenRoot.offsetMin.y, Is.GreaterThanOrEqualTo(42f));
            Assert.That(screenRoot.offsetMax.x, Is.LessThanOrEqualTo(-96f));
            Assert.That(screenRoot.offsetMax.y, Is.LessThanOrEqualTo(-42f));
            Assert.That(screenRoot.parent.GetComponent<SafeAreaFitter>(), Is.Not.Null);

            var backdrop = UnityEngine.Object.FindObjectsByType<Image>(FindObjectsSortMode.None)
                .FirstOrDefault(value => StringComparer.Ordinal.Equals(value.name, "M1 Backdrop Artwork"));
            Assert.That(backdrop, Is.Not.Null);
            Assert.That(backdrop.sprite, Is.Not.Null, "The active screen must show its packaged backdrop, not the color fallback.");
            var aspectFitter = backdrop.GetComponent<AspectRatioFitter>();
            Assert.That(aspectFitter, Is.Not.Null);
            Assert.That(aspectFitter.aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent));
            Assert.That(
                aspectFitter.aspectRatio,
                Is.EqualTo(backdrop.sprite.rect.width / backdrop.sprite.rect.height).Within(0.001f),
                "The backdrop must fill without aspect distortion.");

            var scrim = UnityEngine.Object.FindObjectsByType<Image>(FindObjectsSortMode.None)
                .FirstOrDefault(value => StringComparer.Ordinal.Equals(value.name, "M1 Backdrop Scrim"));
            var stage = GameObject.Find("Studio Title Stage 076")
                ?.GetComponent<Image>();
            Assert.That(scrim, Is.Not.Null);
            Assert.That(stage, Is.Not.Null);
            Assert.That(stage.color.a, Is.EqualTo(0f).Within(0.001f));
            var visibleBackdropContribution = 1f - scrim.color.a;
            Assert.That(
                visibleBackdropContribution,
                Is.GreaterThanOrEqualTo(0.28f),
                "The studio title must leave a meaningful amount of authored backdrop visible.");
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator NewGuildPresentsFrozenModesAndAccessibilityChoices()
        {
            var coordinator = new FakeCoordinator(M1Screen.ApplicantBoard);
            coordinator.State.HasCampaign = false;
            coordinator.State.HasSave = false;
            var originalHash = coordinator.State.CanonicalStateHash;
            var presenter = CreatePresenter(coordinator);
            yield return null;
            Click("START NEW GUILD");
            yield return null;

            AssertTextContains("RELAXED");
            AssertTextContains("STANDARD");
            AssertTextContains("IRON GUILD");
            AssertTextContains("OP START");
            AssertTextContains("HIGH CONTRAST");
            AssertTextContains("REDUCED MOTION");
            Assert.That(CountNamedObjects("Premium Input Brass Edge"), Is.GreaterThanOrEqualTo(1));
            Click("HIGH CONTRAST: OFF");
            yield return null;
            Click("REDUCED MOTION: OFF");
            yield return null;
            Assert.That(coordinator.State.CanonicalStateHash, Is.EqualTo(originalHash), "Accessibility presentation choices must not mutate campaign state.");
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator ApplicantBoardShowsThreeReadableFounderCardsWithoutPrivatePotential()
        {
            var presenter = CreatePresenter(new FakeCoordinator(M1Screen.ApplicantBoard));
            yield return null;
            Click("CONTINUE GAME");
            yield return null;

            AssertTextContains("MEET YOUR FOUNDING PARTY");
            AssertTextContains("YOUR SIX FOUNDERS");
            AssertNoTextContains("DEVELOPMENT POTENTIAL");
            AssertNoTextContains("HIDDEN TRAIT");
            Assert.That(CountNamedObjects("Portrait Frame "), Is.GreaterThanOrEqualTo(3));
            Assert.That(CountNamedObjects("Premium Applicant Commitment Seal"), Is.GreaterThanOrEqualTo(3));
            Assert.That(CountNamedObjects("Premium Class Crest "), Is.GreaterThanOrEqualTo(3));
            AssertTextContains("GUARDIAN");
            AssertTextContains("WARRIOR");
            AssertTextContains("RANGER");
            Assert.That(CountNamedObjects("Class Color Legend "), Is.EqualTo(0),
                "The opening board should not require a six-entry class chart.");
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator RecruitDossierUsesPortraitIntegrationWithoutPlaceholderCopy()
        {
            var presenter = CreatePresenter(new FakeCoordinator(M1Screen.ApplicantBoard));
            yield return null;
            Click("CONTINUE GAME");
            yield return null;
            Click("MEET THIS APPLICANT");
            yield return null;

            Assert.That(CountNamedObjects("Portrait Frame "), Is.GreaterThanOrEqualTo(1));
            AssertTextContains("MEET APPLICANT 1");
            AssertTextContains("WHAT WE KNOW SO FAR");
            AssertNoTextContains("DOSSIER");
            AssertNoTextContains("INTERVIEW RECORD");
            AssertNoTextContains("PLACEHOLDER PORTRAIT");
            Assert.That(CountNamedObjects("Premium Ink Corner"), Is.GreaterThanOrEqualTo(2));
            Assert.That(CountNamedObjects("Premium Class Identity GUARDIAN"), Is.EqualTo(1));
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator EquipmentContinueSubmitsExplicitReviewCommand()
        {
            var coordinator = new FakeCoordinator(M1Screen.Equipment);
            var presenter = CreatePresenter(coordinator);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;
            Click("CONTINUE TO PARTY SETUP");
            yield return null;

            Assert.That(coordinator.EquipmentReviewCalls, Is.EqualTo(1));
            Assert.That(GameObject.Find("Union Planner 074"), Is.Not.Null);
            AssertNamedTextEquals076("Union Planner Title 074", "HEROES\n6 / 6 PLACED");
            Assert.That(CountNamedObjects("Union Planner Member Slot "), Is.EqualTo(6),
                "Equipment review must enter the simple six-slot Union Planner.");
            AssertTextContains("10 UNIONS × 6 = 60");
            AssertTextContains("FORMATION  •  1 OF 1 UNLOCKED  •  PAGE 1/1");
            AssertTextContains("BATTLE INTENT  •  1 OF 1 UNLOCKED  •  PAGE 1/1");
            Assert.That(UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None), Is.Empty,
                "The Release 074 Union planner is a fixed command table, not a scrolling setup dashboard.");
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator EquipmentAcceptsStarterLoadoutWithoutChurnAndShowsCharacterAndItemIdentity()
        {
            var presenter = CreatePresenter(new FakeCoordinator(M1Screen.Equipment));
            yield return null;
            Click("CONTINUE GAME");
            yield return null;

            Assert.That(CountNamedObjects("Portrait Frame "), Is.GreaterThanOrEqualTo(6));
            var equip = FindButton("EQUIP");
            Assert.That(equip, Is.Not.Null);
            Assert.That(equip.interactable, Is.False, "Already-equipped gear must not be submitted as an inventory equip command.");
            Assert.That(FindButton("CONTINUE TO PARTY SETUP").interactable, Is.True,
                "A legal starter loadout must not require pointless unequip/re-equip churn.");
            Assert.That(CountNamedObjects("Large Recruit Showcase "), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountNamedObjects("Premium Class Crest "), Is.GreaterThanOrEqualTo(6));
            Assert.That(CountNamedObjects("Premium Class Crest GUARDIAN"), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountNamedObjects("Premium Class Crest WARRIOR"), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountNamedObjects("Premium Class Crest RANGER"), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountNamedObjects("Premium Class Crest ROGUE"), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountNamedObjects("Premium Class Crest MAGE"), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountNamedObjects("Premium Class Crest PRIEST"), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountNamedObjects("Premium Equipment Art "), Is.GreaterThanOrEqualTo(2));
            Assert.That(CountNamedObjects("Premium Equipped Item Picture Badge"), Is.GreaterThanOrEqualTo(1));
            AssertTextContains("RARE");
            AssertNoTextContains("PERSONAL XP");
            AssertNoTextContains("LIVE STATS");
            AssertNoTextContains("TOP USE-BASED ARTS");
            Assert.That(CountNamedObjects("Selected Inventory Item Preview 068"), Is.GreaterThanOrEqualTo(1));
            Assert.That(CountNamedObjects("Inventory Workspace 068"), Is.EqualTo(1));
            var inventoryWorkspace068 = UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None)
                .Single(value => StringComparer.Ordinal.Equals(value.name, "Inventory Workspace 068"));
            Assert.That(inventoryWorkspace068.GetComponent<LayoutElement>().preferredHeight,
                Is.GreaterThanOrEqualTo(1588f),
                "Three compatible items need 1300 pixels plus two complete 132-pixel rows and their 12-pixel spacing.");
            AssertTextContains("IN BATTLE");
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator FoundingUnionPlannerDownChainReachesReadableSaveAt1280x800076()
        {
            var coordinator = new FakeGuildCityCoordinator();
            coordinator.State.ResumeScreen = M1Screen.UnionBuilder;
            var presenter = CreatePresenter(coordinator);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;
            ConfigureExpeditionCanvasFor1280By800074();

            AssertTextContains("CHOOSE ONE BATTLE FORMATION");
            var formation = FindButtonByName("Union Planner Formation FORMATION_SHIELD_WALL 074");
            var save = FindButtonByName("Union Planner Save And Return 074");
            Assert.That(formation, Is.Not.Null);
            Assert.That(save, Is.Not.Null);
            Assert.That(save.interactable, Is.True);
            Assert.That(save.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(64f),
                "The founding planner Save action must remain at least 64 physical pixels tall at 1280x800.");
            Assert.That(formation.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
            Assert.That(formation.navigation.selectOnDown, Is.SameAs(save),
                "Down from the founding formation must reach Save instead of falling into a dead controller region.");

            var selected = EventSystem.current?.currentSelectedGameObject;
            var visited = new List<string>();
            for (var step = 0; step < 8 && selected != save.gameObject; step++)
            {
                Assert.That(selected, Is.Not.Null,
                    "Controller focus disappeared before reaching the founding planner Save action.");
                visited.Add(selected.name);
                var move = new AxisEventData(EventSystem.current)
                {
                    moveDir = MoveDirection.Down,
                    moveVector = Vector2.down
                };
                ExecuteEvents.Execute(selected, move, ExecuteEvents.moveHandler);
                yield return null;
                var next = EventSystem.current.currentSelectedGameObject;
                Assert.That(next, Is.Not.SameAs(selected),
                    "Down was trapped on " + selected.name + ". Visited: " + string.Join(" -> ", visited));
                selected = next;
            }
            Assert.That(selected, Is.SameAs(save.gameObject),
                "Repeated Down from the initial founding control must reach Save. Visited: " +
                string.Join(" -> ", visited));
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator UnionPlannerUsesSixReadableMemberSlotsAndHidesAssignedRecruits074()
        {
            var coordinator = new FakeGuildCityCoordinator(
                firstChapterComplete: true,
                partialUnions: true);
            coordinator.PrepareChapterOnePlannerChoices076();
            var presenter = CreatePresenter(coordinator);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;

            Assert.That(GameObject.Find("Union Planner 074"), Is.Not.Null);
            AssertNamedTextEquals076("Union Planner Title 074", "HEROES\n1 / 6 PLACED");
            var leaderCard = FindButtonByName("Union Planner Member Slot 0 074");
            Assert.That(leaderCard, Is.Not.Null);
            var leaderIdentity = leaderCard.GetComponentsInChildren<Text>(true)
                .Single(value => value.name.StartsWith(
                    "Union Planner Active Member Label 074", StringComparison.Ordinal));
            Assert.That(leaderIdentity.text.Replace('\u00A0', ' '),
                Is.EqualTo("RECRUIT 1\nGUARDIAN  •  LEADER"),
                "An occupied card must identify the member and role in two readable lines; a non-breaking name space may preserve that layout.");
            Assert.That(leaderIdentity.text,
                Does.Not.Contain("EMPTY").And.Not.Contain("CHOOSE FROM RESERVE"),
                "An occupied active card must never read like a placeholder-only slot.");
            AssertNoTextContains("SET AS LEADER");
            AssertTextContains("FORMATION  •  6 OF 8 UNLOCKED  •  PAGE 1/2");
            AssertTextContains("BATTLE INTENT  •  6 OF 9 UNLOCKED  •  PAGE 1/2");
            AssertTextContains("CHAPTER I REWARD ACTIVE");
            Assert.That(FindButtonByName("Create Starter Union 074"), Is.Not.Null);
            Assert.That(FindButtonByName("Union Planner Formation FORMATION_SHIELD_WALL 074"), Is.Not.Null);
            var formationPrevious076 = FindButtonByName("Union Planner Formation Previous Page 076");
            var formationNext076 = FindButtonByName("Union Planner Formation Next Page 076");
            var doctrinePrevious076 = FindButtonByName("Union Planner Doctrine Previous Page 076");
            var doctrineNext076 = FindButtonByName("Union Planner Doctrine Next Page 076");
            Assert.That(formationPrevious076, Is.Not.Null);
            Assert.That(formationNext076, Is.Not.Null);
            Assert.That(doctrinePrevious076, Is.Not.Null);
            Assert.That(doctrineNext076, Is.Not.Null);
            Assert.That(formationPrevious076.interactable, Is.False);
            Assert.That(formationNext076.interactable, Is.True);
            Assert.That(doctrinePrevious076.interactable, Is.False);
            Assert.That(doctrineNext076.interactable, Is.True);
            Assert.That(CountNamedObjects("Union Planner Reserve Member RECRUIT_1 074"), Is.EqualTo(0));
            Assert.That(CountNamedObjects("Union Planner Reserve Member RECRUIT_2 074"), Is.EqualTo(1));
            FindButtonByName("Union Planner Reserve Member RECRUIT_2 074").onClick.Invoke();
            yield return null;
            Assert.That(coordinator.AssignmentCalls, Is.EqualTo(1));
            Assert.That(coordinator.LastAssignedRecruitId, Is.EqualTo("RECRUIT_2"));
            Assert.That(coordinator.LastAssignedUnionIndex, Is.EqualTo(0));
            Assert.That(coordinator.LastAssignedSlotIndex, Is.EqualTo(1));
            AssertNoTextContains("DIRECT ORDER");
            AssertNoTextContains("CHOOSE ART");
            Assert.That(CountNamedObjects("Union Planner Member Slot "), Is.EqualTo(6));
            Assert.That(CountNamedObjects("Premium Combined Union Stats"), Is.EqualTo(0),
                "The first-hour planner must not show a stat graph.");
            Assert.That(CountNamedObjects("Union Stat Metric "), Is.EqualTo(0));
            Assert.That(CountVisiblePlannerChoiceButtons076("Union Planner Formation "), Is.EqualTo(3));
            Assert.That(CountVisiblePlannerChoiceButtons076("Union Planner Doctrine "), Is.EqualTo(3));
            AssertTextContains("XP TO SPEND  480");
            AssertTextContains("10 UNIONS × 6 = 60");
            AssertTextContains("HOUSING III GOAL");
            AssertTextContains("CAP 75");
            Assert.That(UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None), Is.Empty);
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator CompletedGuildRoutesFromLivingHallThroughExpeditionBoardToBattleEntry074()
        {
            var coordinator = new FakeGuildCityCoordinator();
            var presenter = CreatePresenter(coordinator);
            yield return null;
            Assert.That(FindButton("OPTIONAL PRACTICE"), Is.Not.Null,
                "A ready Guild may expose optional practice without competing with Continue Story.");
            Assert.That(FindButton("PLAY A BATTLE NOW"), Is.Null);
            Click("CONTINUE GAME");
            yield return null;
            yield return null;
            var contractDetails076 = FindRectByPrefix074("Featured Contract Details 062");
            var stakes076 = FindRectByPrefix074("Featured Contract Risk And Reward 076");

            AssertTextContains("THE BELL BENEATH SKYHOME");
            AssertNamedTextEquals076(
                "Featured Contract Reward 062",
                "REWARD  •  42 XP TO SPEND  •  44 HALL XP");
            AssertNamedTextEquals076(
                "Featured Contract Risk 076",
                "RISK  •  3 UNION BATTLES  •  TILE EVENTS SPEND SUPPLIES");
            AssertNamedTextEquals076(
                "Featured Contract Hook 062",
                "Kael holds the Hall breach. Follow Lantern Road: Zorin's missing patrol carries Skyhome's Wayglass toward the old Gatehouse.");
            var hook076 = FindTextByNamePrefix076("Featured Contract Hook 062");
            var risk076 = FindTextByNamePrefix076("Featured Contract Risk 076");
            var reward076 = FindTextByNamePrefix076("Featured Contract Reward 062");
            foreach (var resolution076 in new[]
                     {
                         new Vector2(1280f, 800f),
                         new Vector2(1920f, 1080f)
                     })
            {
                ConfigureExpeditionCanvas074(resolution076.x, resolution076.y);
                LayoutRebuilder.ForceRebuildLayoutImmediate(contractDetails076);
                Canvas.ForceUpdateCanvases();
                AssertRectInside074(contractDetails076, stakes076, "contract risk/reward ribbon");
                AssertRectInside074(stakes076, risk076.rectTransform, "contract risk forecast");
                AssertRectInside074(stakes076, reward076.rectTransform, "contract reward forecast");
                AssertTextFitsRect074(hook076, "contract story hook");
                AssertTextFitsRect074(risk076, "contract risk forecast");
                AssertTextFitsRect074(reward076, "contract reward forecast");
                Assert.That(stakes076.rect.height, Is.GreaterThanOrEqualTo(95f),
                    "The risk/reward ribbon must reserve enough height to render both forecasts.");
                Assert.That(risk076.preferredHeight,
                    Is.LessThanOrEqualTo(risk076.rectTransform.rect.height + 1f),
                    "The contract risk forecast must fit without relying on Truncate.");
                Assert.That(reward076.preferredHeight,
                    Is.LessThanOrEqualTo(reward076.rectTransform.rect.height + 1f),
                    "The contract reward forecast must fit without relying on Truncate.");
            }
            Assert.That(risk076.canvasRenderer.GetAlpha(), Is.GreaterThan(0.95f));
            Assert.That(reward076.canvasRenderer.GetAlpha(), Is.GreaterThan(0.95f));
            Assert.That(risk076.color.a, Is.GreaterThan(0.95f));
            Assert.That(reward076.color.a, Is.GreaterThan(0.95f));
            Assert.That(Vector4.Distance(risk076.color, reward076.color), Is.GreaterThan(0.1f),
                "Risk and reward need distinct warning/positive color coding at a glance.");
            var worldRibbonFill076 = new Color(0.020f, 0.035f, 0.045f, 1f);
            Assert.That(ContrastRatio076(risk076.color, worldRibbonFill076),
                Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(ContrastRatio076(reward076.color, worldRibbonFill076),
                Is.GreaterThanOrEqualTo(4.5f));
            AssertNoTextContains("TREASURY XP");
            Assert.That(FindButton("ACCEPT CONTRACT"), Is.Not.Null,
                "Continue Story must open the exact next objective instead of stopping at the Hall.");
            Assert.That(FindButton("ACCEPT CONTRACT").name, Is.EqualTo("Featured Contract Accept 062"));
            Click("ACCEPT CONTRACT");
            yield return null;
            Assert.That(coordinator.AcceptContractCalls, Is.EqualTo(1));
            Assert.That(coordinator.LastAcceptedContractId, Is.EqualTo(FakeGuildCityCoordinator.FirstContractId));
            Assert.That(coordinator.GuildCity017D.HasActiveContract, Is.True);
            Assert.That(FindButton("BEGIN EXPEDITION"), Is.Not.Null);
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.EqualTo(FindButton("BEGIN EXPEDITION").gameObject),
                "Accepting the featured contract must focus its single clear departure action for controller players.");

            Click("←  BACK");
            yield return null;
            Assert.That(GameObject.Find("Living Guild Hub 074"), Is.Not.Null);
            Assert.That(GameObject.Find("Living Guild Hub Populated Background 074"), Is.Not.Null);
            Assert.That(GameObject.Find("Living Guild Hub Objective Card 074"), Is.Not.Null);
            Assert.That(GameObject.Find("Living Guild Hub Roster 074"), Is.Null);
            Assert.That(GameObject.Find("Living Guild Hub Homecoming 076"), Is.Null);
            Assert.That(CountNamedObjects("Living Guild Hub Facility "), Is.EqualTo(4));
            Assert.That(GameObject.Find(
                    "Living Guild Hub Facility ENDLESS_TOWER_081 074"),
                Is.Not.Null,
                "The additive Endless Tower destination must remain available from the Hall.");
            AssertTextContains("GUILD XP  80 / 400");
            AssertTextContains("XP TO SPEND  480");
            AssertNoTextContains("REPUTATION");
            Assert.That(UnityEngine.Object.FindFirstObjectByType<WalkableGuildHall069>(), Is.Null);

            FindButtonByName("Living Guild Home Codes 110").onClick.Invoke();
            yield return null;
            var codesTabField084 = typeof(M1FlowPresenter).GetField(
                "_guildCityTab017D",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            Assert.That(codesTabField084, Is.Not.Null);
            Assert.That(codesTabField084.GetValue(presenter), Is.EqualTo("CODES"));
            AssertTextContains("BONUS CODES");
            Click("←  BACK");
            yield return null;
            Assert.That(GameObject.Find("Living Guild Hub 074"), Is.Not.Null,
                "The stable CODES action must return cleanly to the simplified home.");

            FindButtonByName("Living Guild Hub Facility CONTRACT 074").onClick.Invoke();
            yield return null;
            AssertTextContains("THE BELL BENEATH SKYHOME");
            Assert.That(FindButton("REVIEW PARTY"), Is.Not.Null);
            Click("REVIEW PARTY");
            yield return null;
            Assert.That(FindButton("BEGIN EXPEDITION"), Is.Not.Null);
            Assert.That(FindButton("BEGIN EXPEDITION").name,
                Is.EqualTo("First Contract Begin Expedition 062"));
            Click("BEGIN EXPEDITION");
            yield return null;
            Assert.That(coordinator.StartExpeditionCalls, Is.EqualTo(1));
            Assert.That(coordinator.GuildCity017D.Expedition, Is.Not.Null);
            Assert.That(coordinator.GuildCity017D.HasPendingEncounter, Is.False);
            Assert.That(coordinator.GuildCity017D.Expedition.CanCommitEncounter, Is.True);
            Assert.That(GameObject.Find("Board Quest 081"), Is.Not.Null);
            AssertBoardQuestFiveSpaceLayout081();
            Assert.That(GameObject.Find("Expedition Board Overlay 074"), Is.Null,
                "The player-facing expedition must be a readable operation plan, not a node graph.");
            Assert.That(UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None), Is.Empty);
            AssertTextContains("ENEMY CONTACT");
            Assert.That(FindButton("FIGHT NOW"), Is.Not.Null);
            AssertFocusedAction076("FIGHT NOW");
            Click("FIGHT NOW");
            yield return null;
            Assert.That(coordinator.CommitEncounterCalls, Is.EqualTo(1),
                "The one Board Quest fight action must commit the exact active encounter once.");
            Assert.That(coordinator.StartCommittedBattleCalls, Is.EqualTo(1),
                "The same clear fight action must launch the committed Union encounter once.");
            Assert.That(UnityEngine.Object.FindFirstObjectByType<OuterGateworksExploration066>(), Is.Null,
                "The rejected walkable shell must not be restored behind the battle.");
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator ExpeditionBoardProvidesReadableRoutesChecksAndSavedReturn074()
        {
            {
                var routeCoordinator074 = new FakeGuildCityCoordinator();
                routeCoordinator074.PrepareInteractiveExpedition();
                var routePresenter074 = CreatePresenter(routeCoordinator074);
                yield return null;
                typeof(M1FlowPresenter).GetField(
                        "_reducedMotion",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(routePresenter074, true);
                Click("CONTINUE GAME");
                yield return null;
                Canvas.ForceUpdateCanvases();

                Assert.That(GameObject.Find("Board Quest 081"), Is.Not.Null);
                AssertBoardQuestFiveSpaceLayout081();
                Assert.That(GameObject.Find("Board Quest Scene 081"), Is.Not.Null);
                Assert.That(GameObject.Find("Expedition Board Overlay 074"), Is.Null);
                Assert.That(GameObject.Find("Expedition Context Action Area 074"), Is.Not.Null);
                Assert.That(UnityEngine.Object.FindFirstObjectByType<OuterGateworksExploration066>(), Is.Null,
                    "Release 074 uses a fixed operation plan instead of the rejected walking shell.");
                Assert.That(UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None), Is.Empty);
                AssertTextContains("Tap once. Your pawn moves one space");
                Assert.That(CountNamedObjects("Select Expedition Destination "), Is.EqualTo(0),
                    "The phone-simple quest must choose its authored branch without exposing a route graph.");
                var routeView081 = ExpeditionBoardProjection074.Build(routeCoordinator074.GuildCity017D);
                var automaticDestination081 = BoardQuestRules081.AutomaticDestination081(
                    routeCoordinator074.GuildCity017D,
                    routeView081);
                Assert.That(automaticDestination081, Is.Not.Null);
                Assert.That(automaticDestination081.NodeId,
                    Is.EqualTo("N02").Or.EqualTo("N04"));
                var moveForward081 = FindButtonByName("Expedition Primary Context Action 074");
                Assert.That(moveForward081, Is.Not.Null);
                Assert.That(moveForward081.GetComponentInChildren<Text>().text,
                    Is.EqualTo("MOVE FORWARD\nFLIP NEXT ROOM"));
                Assert.That(EventSystem.current.currentSelectedGameObject,
                    Is.SameAs(moveForward081.gameObject));

                moveForward081.onClick.Invoke();
                yield return null;
                Assert.That(routeCoordinator074.MoveExpeditionCalls, Is.EqualTo(1));
                Assert.That(routeCoordinator074.LastMoveDestinationNodeId,
                    Is.EqualTo(automaticDestination081.NodeId));
                Assert.That(routeCoordinator074.GuildCity017D.Expedition.CurrentNodeId,
                    Is.EqualTo(automaticDestination081.NodeId));
                var expectedEvent081 = routeCoordinator074.GuildCity017D.Expedition.CurrentEventId;
                var automaticRoll081 = FindButtonByName("Expedition Primary Context Action 074");
                Assert.That(automaticRoll081, Is.Not.Null);
                Assert.That(automaticRoll081.GetComponentInChildren<Text>().text,
                    Does.StartWith("ROLLING 2D6").And.Contain("WATCH THE DICE"));
                Assert.That(automaticRoll081.interactable, Is.False);
                yield return new WaitForSecondsRealtime(
                    M1FlowPresenter.BoardAdventureCardFlipDuration084 + 0.40f);
                Assert.That(routeCoordinator074.ResolveCheckCalls, Is.EqualTo(1));
                Assert.That(routeCoordinator074.LastCheckModifier, Is.EqualTo(2));
                Assert.That(routeCoordinator074.LastCheckEventId, Is.EqualTo(expectedEvent081));
                Assert.That(routeCoordinator074.GuildCity017D.Expedition.LastCheckTotal, Is.EqualTo(9));
                Assert.That(routeCoordinator074.GuildCity017D.Expedition.LastCheckOutcome, Is.EqualTo("FULL_SUCCESS"));
                Assert.That(routeCoordinator074.GuildCity017D.Expedition.CanMove, Is.True);
                foreach (var resolution081 in new[]
                         {
                             new Vector2(1280f, 800f),
                             new Vector2(1920f, 1080f)
                         })
                {
                    ConfigureExpeditionCanvas074(resolution081.x, resolution081.y);
                    AssertBoardQuestSceneGeometry081(
                        resolution081.x,
                        resolution081.y,
                        false);
                    AssertBoardQuestCommittedDiceGeometry081(
                        resolution081.x,
                        resolution081.y);
                }
                Assert.That(FindVisibleButtonStartingWith081("ROLL 2D6\n"), Is.Null,
                    "A committed room roll must remove every reroll control from the player UI.");
                Assert.That(FindButtonByName("Expedition Primary Context Action 074")
                        ?.GetComponentInChildren<Text>().text,
                    Is.EqualTo("MOVE FORWARD\nFLIP NEXT ROOM"),
                    "The saved result must advance to one forward action instead of offering a reroll.");
                Assert.That(GameObject.Find("Board Quest 081"), Is.Not.Null,
                    "A resolved check must stay on the same saved route board.");
                yield return Cleanup(routePresenter074);

                routeCoordinator074.PrepareOptionalElite();
                var bypassPresenter074 = CreatePresenter(routeCoordinator074);
                yield return null;
                Click("CONTINUE GAME");
                yield return null;
                Assert.That(GameObject.Find("Board Quest 081"), Is.Not.Null);
                AssertTextContains("A monster was under this card");
                Assert.That(FindButton("FIGHT THE MONSTER\nFULL UNION BATTLE"), Is.Not.Null);
                Assert.That(FindButtonByName("Board Quest Skip Optional Elite 081"), Is.Not.Null);
                Assert.That(CountNamedObjects("Select Expedition Destination "), Is.EqualTo(0));
                Assert.That(routeCoordinator074.CommitEncounterCalls, Is.EqualTo(0),
                    "Opening the elite decision must not silently commit combat.");
                FindButtonByName("Board Quest Skip Optional Elite 081").onClick.Invoke();
                yield return null;
                Assert.That(routeCoordinator074.CommitEncounterCalls, Is.EqualTo(0));
                Assert.That(routeCoordinator074.LastMoveDestinationNodeId, Is.EqualTo("N10"),
                    "The bypass must use the authoritative linked route.");
                yield return Cleanup(bypassPresenter074);

                routeCoordinator074.PrepareOptionalElite();
                var elitePresenter074 = CreatePresenter(routeCoordinator074);
                yield return null;
                Click("CONTINUE GAME");
                yield return null;
                Assert.That(FindButton("FIGHT THE MONSTER\nFULL UNION BATTLE"), Is.Not.Null);
                Assert.That(FindButtonByName("Board Quest Skip Optional Elite 081"), Is.Not.Null);
                Click("FIGHT THE MONSTER\nFULL UNION BATTLE");
                yield return null;
                Assert.That(routeCoordinator074.CommitEncounterCalls, Is.EqualTo(1));
                Assert.That(routeCoordinator074.StartCommittedBattleCalls, Is.EqualTo(1));
                yield return Cleanup(elitePresenter074);
                routeCoordinator074.GuildCity017D.HasPendingEncounter = false;

                routeCoordinator074.PrepareClearedNorthGate();
                var returnPresenter074 = CreatePresenter(routeCoordinator074);
                yield return null;
                Click("CONTINUE GAME");
                yield return null;
                Assert.That(GameObject.Find("Board Quest 081"), Is.Not.Null);
                Assert.That(routeCoordinator074.GuildCity017D.Expedition.CanFinalizeOperation, Is.False);
                var returnMove081 = FindButtonByName("Expedition Primary Context Action 074");
                Assert.That(returnMove081, Is.Not.Null);
                Assert.That(returnMove081.GetComponentInChildren<Text>().text,
                    Is.EqualTo("MOVE FORWARD\nFLIP NEXT ROOM"));
                returnMove081.onClick.Invoke();
                yield return null;
                Assert.That(routeCoordinator074.LastMoveDestinationNodeId, Is.EqualTo("N14"));
                Assert.That(routeCoordinator074.GuildCity017D.Expedition.CanFinalizeOperation, Is.True);
                yield return new WaitForSecondsRealtime(
                    M1FlowPresenter.BoardAdventurePawnTravelDuration084 +
                    M1FlowPresenter.BoardAdventureCardFlipDuration084 + 0.40f);
                AssertTextContains("EVERYONE IS COMING HOME");
                Click("RETURN HOME");
                yield return null;
                Assert.That(routeCoordinator074.FinalizeOperationCalls, Is.EqualTo(1));
                Assert.That(routeCoordinator074.GuildCity017D.Expedition, Is.Null);
                Assert.That(routeCoordinator074.GuildCity017D.HasActiveContract, Is.False);
                Assert.That(GameObject.Find(M1FlowPresenter.FirstOperationConsequencesRootName077),
                    Is.Not.Null,
                    "Returning from the first operation must open its one-tap homecoming directly.");
                Assert.That(
                    M1FlowPresenter.FirstOperationConsequenceStageForVerification077(
                        routeCoordinator074.GuildCity017D),
                    Is.EqualTo(FirstOperationConsequenceStage077.RecoveryOrTraining));
                AssertNamedTextEquals076(
                    "Phone Simple Homecoming Heading 085",
                    "MISSION COMPLETE  •  THE PATROL IS HOME");
                AssertNamedTextContains076(
                    "Phone Simple Homecoming Summary 085",
                    "HOME BASE READY");
                Assert.That(GameObject.Find("First Operation Choose Recovery RECRUIT_1 077"),
                    Is.Null);
                Assert.That(GameObject.Find("First Operation Choose Training RECRUIT_1 077"),
                    Is.Null);
                Assert.That(FindButtonByName("Phone Simple Homecoming Continue 085"),
                    Is.Not.Null);
                Assert.That(GameObject.Find("Living Guild Hub 074"), Is.Null,
                    "The one-tap homecoming must not be hidden behind an extra Hall click.");
                yield return Cleanup(returnPresenter074);
            }
        }

        [UnityTest]
        public IEnumerator ChapterTwoFinalReturnNamesSurveyorsEvidenceAndSkyhome080()
        {
            var coordinator080 = new FakeGuildCityCoordinator(firstChapterComplete: true);
            coordinator080.PrepareChapterTwoFinalizable080();
            var presenter080 = CreatePresenter(coordinator080);
            yield return null;

            Click("CONTINUE GAME");
            yield return null;

            Assert.That(GameObject.Find("Board Quest 081"), Is.Not.Null);
            foreach (var resolution080 in new[]
                     {
                         new Vector2(1280f, 800f),
                         new Vector2(1920f, 1080f)
                     })
            {
                ConfigureExpeditionCanvas074(resolution080.x, resolution080.y);
                AssertNextOrderGeometry074("THE SURVEYORS ARE COMING HOME");
                AssertNamedTextEquals076(
                    "Expedition Current Position Summary 074",
                    "Return Sella, Orra, and their recovered route evidence to Skyhome's Wayglass table.");
                AssertNamedTextEquals076(
                    "Expedition Context Notice Title 074",
                    "THE SURVEYORS ARE COMING HOME");
                AssertNamedTextEquals076(
                    "Expedition Context Notice Copy 074",
                    "Sella, Orra, and the survey crew are safe. Return their evidence to Skyhome's Wayglass table.");
                AssertNamedTextDoesNotContain076(
                    "Expedition Context Notice Copy 074",
                    "patrol and Wayglass");
                AssertNamedTextContains076(
                    "Expedition Current Position Summary 074",
                    "recovered route evidence");
                AssertNoTextContains("ORRA TRAIL 6/6");
                AssertChapterTwoStoryIdentity079(
                    "ORRA VALE",
                    "RESCUED",
                    ExpeditionBoardProjection074.ChapterTwoOrraPortraitResource079);
                AssertFocusedAction076("RETURN TO SKYHOME");
            }

            yield return Cleanup(presenter080);
        }

        [UnityTest]
        public IEnumerator ChapterTwoSurveyorRescueKeepsOrraVisibleWhileKiriSpeaks080()
        {
            var coordinator080 = new FakeGuildCityCoordinator(firstChapterComplete: true);
            coordinator080.PrepareChapterTwoRescue080();
            var presenter080 = CreatePresenter(coordinator080);
            yield return null;

            Click("CONTINUE GAME");
            yield return null;

            Assert.That(GameObject.Find("Board Quest 081"), Is.Not.Null);
            foreach (var resolution080 in new[]
                     {
                         new Vector2(1280f, 800f),
                         new Vector2(1920f, 1080f)
                     })
            {
                ConfigureExpeditionCanvas074(resolution080.x, resolution080.y);
                AssertNamedTextContains076(
                    "Expedition Companion Story Beat Text 076",
                    "KIRI");
                AssertChapterTwoStoryIdentity079(
                    "ORRA VALE",
                    "RESCUE OBJECTIVE",
                    ExpeditionBoardProjection074.ChapterTwoOrraPortraitResource079);
                AssertNamedTextDoesNotContain076(
                    "Expedition Current Position Summary 074",
                    "SECURED");
            }

            yield return Cleanup(presenter080);
        }

        [UnityTest]
        public IEnumerator Guided071FieldOwnsOneObjectiveAndNamedPartyAtBothStudioResolutions076()
        {
            foreach (var resolution076 in new[]
                     {
                         new Vector2(1280f, 800f),
                         new Vector2(1920f, 1080f)
                     })
            {
                var coordinator076 = new FakeGuildCityCoordinator();
                coordinator076.PrepareGuidedFirstHourField076();
                var host076 = new GameObject(
                    "Guided First Hour Field " + resolution076.x + "x" + resolution076.y + " 076");
                var field076 = host076.AddComponent<OuterGateworksExploration066>();
                var missionBriefCalls076 = 0;
                var battleCalls076 = 0;
                field076.Begin066(
                    coordinator076,
                    () => missionBriefCalls076++,
                    () => battleCalls076++);
                yield return null;

                ConfigureGuidedFieldCanvas076(resolution076.x, resolution076.y);
                Assert.That(field076.IsActiveForVerification076, Is.True);
                Assert.That(field076.ControlledAvatarForVerification076, Is.Not.Null);
                Assert.That(field076.CurrentNodeForVerification076, Is.EqualTo("N00"));
                Assert.That(field076.CurrentObjectiveForVerification076, Is.Not.Empty);
                Assert.That(field076.AuthoritativeActionCountForVerification076, Is.EqualTo(1));
                Assert.That(field076.AuthoredBackdropForVerification076,
                    Does.EndWith("GUILD_HALL_GAMEPLAY_PLATE_071"));
                Assert.That(field076.UsesFullFrameAuthoredVista069, Is.True);
                Assert.That(field076.NamedCompanionIdsForVerification076,
                    Is.EqualTo(new[] { "RECRUIT_1", "RECRUIT_4" }));
                Assert.That(field076.GuildmasterStandeeResourceKeyForVerification076,
                    Is.EqualTo(M1VisualAssets.GuildmasterStandeeResourceKey076));
                Assert.That(field076.GuildmasterPoseResourceRootForVerification076,
                    Is.EqualTo(M1VisualAssets.GuildmasterPoseResourceRoot076));
                Assert.That(field076.UsesPoseDrivenWorldSprite072, Is.True,
                    "The field Guildmaster must retain the same authored pose identity as the Market and Hall.");
                Assert.That(field076.HasControlledIdentityBadgeForVerification076, Is.True,
                    "The controlled leader needs one polished YOU cue in exploration.");
                Assert.That(field076.HasKaelIdentityBadgeForVerification076, Is.True,
                    "The first destination must visibly identify Kael before the player moves.");
                Assert.That(field076.HasEncounterCastForVerification076, Is.True,
                    "The Hall-breach threat must be staged beside, not on top of, its destination.");
                Assert.That(field076.StoryPartyTextForVerification076.fontSize,
                    Is.GreaterThanOrEqualTo(20));
                Assert.That(field076.StoryContextTextForVerification076.resizeTextMinSize,
                    Is.GreaterThanOrEqualTo(22));
                Assert.That(field076.ControlsTextForVerification076.fontSize,
                    Is.GreaterThanOrEqualTo(22));
                if (!Application.isMobilePlatform)
                {
                    field076.RefreshPersistentHudForVerification076();
                    Assert.That(field076.DesktopControlsVisibleForVerification076, Is.True,
                        "Desktop movement, roll, and ACT bindings must remain visible throughout traversal.");
                }
                Assert.That(field076.HasOptionalCampOrSecretForVerification076, Is.False);
                Assert.That(
                    GameObject.Find("Controlled Guildmaster Contact Shadow 076"),
                    Is.Not.Null,
                    "The party leader must be visually grounded on the authored plate.");
                var guildmasterStandee076 = GameObject.Find(
                        "Gateworks Character Art Guildmaster Vanguard 066 068")
                    ?.GetComponent<SpriteRenderer>()?.sprite;
                Assert.That(guildmasterStandee076, Is.Not.Null);
                var guildmasterIdlePose076 = Resources.Load<Sprite>(
                    M1VisualAssets.GuildmasterPoseResourceRoot076 + "/POSE_IDLE");
                Assert.That(guildmasterIdlePose076, Is.Not.Null);
                Assert.That(guildmasterStandee076.texture,
                    Is.SameAs(guildmasterIdlePose076.texture),
                    "The field renderer must show the dedicated Guildmaster idle pose after animation initializes.");
                var kaelStandee076 = GameObject.Find(
                        "Gateworks Character Art Hall Breach Kael Support 072 Body 068")
                    ?.GetComponent<SpriteRenderer>()?.sprite;
                Assert.That(kaelStandee076, Is.Not.Null);
                Assert.That(kaelStandee076.texture.name,
                    Is.EqualTo("STANDEE_SIGREC_ANSEL_WINTERGLASS"),
                    "Kael cannot reuse deployed companion Tazren's standee.");
                var kaelRoot076 = GameObject.Find("Hall Breach Kael Support 072");
                Assert.That(kaelRoot076, Is.Not.Null);
                Assert.That(Vector3.Distance(
                        kaelRoot076.transform.position,
                        field076.CurrentObjectivePositionForVerification076),
                    Is.InRange(1.20f, 1.60f),
                    "Kael must stand beside the destination while remaining visually separate from its gold pillar.");
                Assert.That(Vector3.Distance(
                        kaelRoot076.transform.position,
                        field076.ControlledAvatarForVerification076.position),
                    Is.GreaterThan(8f),
                    "Kael's destination identity must not collide with the departing party cluster.");
                Assert.That(GameObject.Find("Visible Party Follower 1 066"), Is.Null);
                Assert.That(GameObject.Find("Visible Party Follower 2 066"), Is.Null);
                Assert.That(UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                    .Count(value => value != null &&
                                    StringComparer.Ordinal.Equals(
                                        value.name,
                                        "Outer Gateworks Exploration HUD 066")), Is.EqualTo(1));
                Assert.That(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                    .Count(value => value != null &&
                                    StringComparer.Ordinal.Equals(
                                        value.name,
                                        "Gateworks Following Isometric Camera 066")), Is.EqualTo(1));

                var firstSpawn076 = field076.ControlledAvatarForVerification076.position;
                Assert.That(field076.CurrentObjectiveDistanceForVerification076,
                    Is.GreaterThan(field076.CurrentObjectiveInteractionRadiusForVerification076));
                var firstLargestStep076 = DriveGuidedFieldToObjective076(field076);
                Assert.That(field076.IsWithinCurrentObjectiveInteractionRangeForVerification076,
                    Is.True,
                    "The Hall-breach room motor must reach its gold action from spawn without a teleport.");
                Assert.That(Vector3.Distance(
                    firstSpawn076,
                    field076.ControlledAvatarForVerification076.position), Is.GreaterThan(3f));
                Assert.That(firstLargestStep076, Is.LessThan(0.45f),
                    "The guided field certification path must remain frame-sized.");
                Assert.That(field076.NearestInteractionIdForVerification066, Is.EqualTo("QUEST"));
                var positionBeforeNodeTransition076 =
                    field076.ControlledAvatarForVerification076.position;
                field076.InteractForVerification076();
                Assert.That(coordinator076.MoveExpeditionCalls, Is.EqualTo(1));
                Assert.That(coordinator076.CommitEncounterCalls, Is.Zero,
                    "Walking to a new 071 beat must not commit its encounter off-screen.");
                Assert.That(battleCalls076, Is.Zero);
                Assert.That(field076.CurrentNodeForVerification076, Is.EqualTo("N01"));
                Assert.That(field076.AuthoritativeActionCountForVerification076, Is.EqualTo(1));
                var nodeTransitionDisplacement076 =
                    field076.ControlledAvatarForVerification076.position -
                    positionBeforeNodeTransition076;
                nodeTransitionDisplacement076.y = 0f;
                Assert.That(
                    nodeTransitionDisplacement076.magnitude,
                    Is.LessThan(0.05f),
                    "Advancing to another beat in the same room must preserve forward field position instead of returning to the left edge.");
                Assert.That(
                    field076.PreservedPositionOnLastBeatTransitionForVerification076,
                    Is.True);

                var encounterLargestStep076 = DriveGuidedFieldToObjective076(field076);
                Assert.That(field076.IsWithinCurrentObjectiveInteractionRangeForVerification076,
                    Is.True,
                    "The encounter action must also be physically reachable after the room rebuild.");
                Assert.That(encounterLargestStep076, Is.LessThan(0.45f));
                field076.InteractForVerification076();
                Assert.That(coordinator076.CommitEncounterCalls, Is.EqualTo(1));
                Assert.That(battleCalls076, Is.EqualTo(1));

                field076.OpenMissionBriefForVerification076();
                Assert.That(missionBriefCalls076, Is.EqualTo(1));
                field076.Shutdown066();
                UnityEngine.Object.Destroy(host076);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Guided071LanternRoadRoomMotorReachesObjectiveWithoutTeleport076()
        {
            var coordinator076 = new FakeGuildCityCoordinator();
            coordinator076.PrepareGuidedLanternRoadField076();
            var host076 = new GameObject("Guided Lantern Road Motor Certification 076");
            var field076 = host076.AddComponent<OuterGateworksExploration066>();
            field076.Begin066(coordinator076, () => { }, () => { });
            yield return null;

            Assert.That(field076.ActiveExpeditionRoomForVerification072,
                Is.EqualTo("LanternRoad"));
            Assert.That(field076.AuthoredBackdropForVerification076,
                Does.EndWith("LANTERN_ROAD_GAMEPLAY_PLATE_071"));
            Assert.That(field076.CurrentNodeForVerification076, Is.EqualTo("N04"));
            Assert.That(field076.CurrentObjectiveDirectionForVerification076.sqrMagnitude,
                Is.EqualTo(1f).Within(0.001f));
            Assert.That(field076.CurrentObjectiveDistanceForVerification076,
                Is.GreaterThan(field076.CurrentObjectiveInteractionRadiusForVerification076));
            var spawn076 = field076.ControlledAvatarForVerification076.position;
            var largestStep076 = DriveGuidedFieldToObjective076(field076);

            Assert.That(field076.IsWithinCurrentObjectiveInteractionRangeForVerification076,
                Is.True,
                "The real Lantern Road room motor must traverse the authored lane into ACT range.");
            Assert.That(field076.NearestInteractionIdForVerification066, Is.EqualTo("QUEST"));
            Assert.That(Vector3.Distance(
                spawn076,
                field076.ControlledAvatarForVerification076.position), Is.GreaterThan(3f));
            Assert.That(largestStep076, Is.LessThan(0.45f),
                "Lantern Road traversal must be continuous CharacterController motion, not a hidden teleport.");

            var savedPosition076 = field076.ControlledAvatarForVerification076.position;
            var savedCheckpoint076 = field076.CaptureFieldCheckpoint076();
            Assert.That(savedCheckpoint076, Is.Not.Null);
            field076.Shutdown066();
            UnityEngine.Object.Destroy(host076);
            yield return null;

            var resumedHost076 = new GameObject("Guided Lantern Road Resume Certification 076");
            var resumedField076 = resumedHost076.AddComponent<OuterGateworksExploration066>();
            resumedField076.Begin066(
                coordinator076,
                () => { },
                () => { },
                savedCheckpoint076);
            yield return null;

            Assert.That(resumedField076.RestoredFieldCheckpointForVerification076, Is.True);
            var resumeDisplacement076 =
                resumedField076.ControlledAvatarForVerification076.position -
                savedPosition076;
            resumeDisplacement076.y = 0f;
            Assert.That(
                resumeDisplacement076.magnitude,
                Is.LessThan(0.01f),
                "Mission brief and battle handoffs must reopen the same expedition room at its exact saved walkable-plane position.");
            resumedField076.Shutdown066();
            UnityEngine.Object.Destroy(resumedHost076);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Guided071N04CheckAdvancesThroughN05BeforeN06076()
        {
            var coordinator076 = new FakeGuildCityCoordinator();
            coordinator076.PrepareGuidedLanternRoadField076();
            var host076 = new GameObject("Guided Lantern Road N04 Check Certification 076");
            var field076 = host076.AddComponent<OuterGateworksExploration066>();
            field076.Begin066(coordinator076, () => { }, () => { });
            yield return null;

            DriveGuidedFieldToObjective076(field076);
            Assert.That(field076.IsWithinCurrentObjectiveInteractionRangeForVerification076, Is.True);
            field076.InteractForVerification076();
            Assert.That(field076.InteractionLockedForVerification076, Is.True,
                "The committed 2d6 presentation must own interaction until its result finishes.");

            var deadline076 = Time.realtimeSinceStartup + 10f;
            while (field076.InteractionLockedForVerification076 &&
                   Time.realtimeSinceStartup < deadline076)
                yield return null;

            Assert.That(field076.InteractionLockedForVerification076, Is.False,
                "The field interaction must become available again after the 2d6 result.");
            Assert.That(coordinator076.ResolveCheckCalls, Is.EqualTo(1));
            Assert.That(coordinator076.GuildCity017D.Expedition.ResolutionComplete, Is.True);
            Assert.That(field076.CurrentNodeForVerification076, Is.EqualTo("N04"));
            Assert.That(field076.IsWithinCurrentObjectiveInteractionRangeForVerification076, Is.True,
                "The same-room refresh must preserve the player's position at the next gold action.");

            field076.InteractForVerification076();
            Assert.That(coordinator076.GuildCity017D.Expedition.CurrentNodeId, Is.EqualTo("N05"),
                "The first interaction after the dice presentation must advance to the mandatory patrol cache.");
            DriveGuidedFieldToObjective076(field076);
            Assert.That(field076.IsWithinCurrentObjectiveInteractionRangeForVerification076, Is.True);
            field076.InteractForVerification076();
            Assert.That(coordinator076.GuildCity017D.Expedition.CurrentNodeId, Is.EqualTo("N06"),
                "The patrol cache must then advance to the Lantern Road ambush.");

            field076.Shutdown066();
            UnityEngine.Object.Destroy(host076);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExpeditionNextOrderRegionsNeverOverlapAtCertificationFrames078()
        {
            var departure = new FakeGuildCityCoordinator();
            departure.PrepareGuidedFirstHourField076();
            var kael = departure.State.Recruits.Single(value => value.RecruitId == "RECRUIT_1");
            kael.DisplayName = "Kael Ward";
            kael.PortraitAuthorityId = "SIGREC071_KAEL";
            var departurePresenter = CreatePresenter(departure);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;
            foreach (var resolution in new[]
                     {
                         new Vector2(1920f, 1080f),
                         new Vector2(1280f, 800f)
                     })
            {
                ConfigureExpeditionCanvas074(resolution.x, resolution.y);
                AssertBoardQuestRouteChoiceGeometry081(
                    resolution.x,
                    resolution.y,
                    expectSingleRequiredRoute: true);
                AssertBoardQuestSceneGeometry081(
                    resolution.x, resolution.y, expectIdentityChip: true);
            }
            yield return Cleanup(departurePresenter);

            var route = new FakeGuildCityCoordinator();
            route.PrepareInteractiveExpedition();
            var routePresenter = CreatePresenter(route);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;
            var routeProjection081 = ExpeditionBoardProjection074.Build(route.GuildCity017D);
            var expectedRoute081 = BoardQuestRules081.AutomaticDestination081(
                route.GuildCity017D,
                routeProjection081);
            Assert.That(expectedRoute081, Is.Not.Null);
            var routeChoice = FindButtonByName("Expedition Primary Context Action 074");
            Assert.That(routeChoice, Is.Not.Null);
            Assert.That(routeChoice.GetComponentInChildren<Text>().text,
                Is.EqualTo("MOVE FORWARD\nFLIP NEXT ROOM"));
            foreach (var resolution in new[]
                     {
                         new Vector2(1920f, 1080f),
                         new Vector2(1280f, 800f)
                     })
            {
                ConfigureExpeditionCanvas074(resolution.x, resolution.y);
                AssertBoardQuestRouteChoiceGeometry081(
                    resolution.x,
                    resolution.y,
                    expectSingleRequiredRoute: false);
                AssertBoardQuestSceneGeometry081(
                    resolution.x, resolution.y, expectIdentityChip: false);
            }
            routeChoice.onClick.Invoke();
            yield return null;
            Assert.That(route.GuildCity017D.Expedition.CurrentNodeId,
                Is.EqualTo(expectedRoute081.NodeId));
            yield return Cleanup(routePresenter);

            var wayglass = new FakeGuildCityCoordinator();
            wayglass.PrepareWayglassResolvedRoute078();
            var wayglassPresenter = CreatePresenter(wayglass);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;
            foreach (var resolution in new[]
                     {
                         new Vector2(1920f, 1080f),
                         new Vector2(1280f, 800f)
                     })
            {
                ConfigureExpeditionCanvas074(resolution.x, resolution.y);
                AssertBoardQuestRouteChoiceGeometry081(
                    resolution.x,
                    resolution.y,
                    expectSingleRequiredRoute: false);
                AssertBoardQuestSceneGeometry081(
                    resolution.x, resolution.y, expectIdentityChip: true);
            }
            yield return Cleanup(wayglassPresenter);

            var check = new FakeGuildCityCoordinator();
            check.PrepareInteractiveExpedition();
            Assert.That(check.MoveGuildCityExpedition017D("N02").Succeeded, Is.True);
            check.PrepareExpeditionDecisionIdentityFixture078();
            var checkPresenter = CreatePresenter(check);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;
            ConfigureExpeditionCanvas074(1920f, 1080f);
            AssertBoardQuestDiceDecisionGeometry081(
                1920f, 1080f, "JAZZI", expectTeamUp: true);
            ConfigureExpeditionCanvasFor1280By800074();
            AssertBoardQuestDiceDecisionGeometry081(
                1280f, 800f, "JAZZI", expectTeamUp: true);
            yield return Cleanup(checkPresenter);

            var secondaryObjective = new FakeGuildCityCoordinator();
            secondaryObjective.PrepareWayglassSecondaryObjective078();
            var secondaryPresenter = CreatePresenter(secondaryObjective);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;
            foreach (var resolution in new[]
                     {
                         new Vector2(1920f, 1080f),
                         new Vector2(1280f, 800f)
                     })
            {
                ConfigureExpeditionCanvas074(resolution.x, resolution.y);
                AssertBoardQuestDiceDecisionGeometry081(
                    resolution.x,
                    resolution.y,
                    "ORREN",
                    expectTeamUp: false);
                AssertBoardQuestSceneGeometry081(
                    resolution.x, resolution.y, expectIdentityChip: true);
            }
            Assert.That(FindVisibleButtonStartingWith081("TEAM UP\n"), Is.Null,
                "The phone-simple board must not expose the retired approach-choice graph.");
            Assert.That(FindVisibleButtonStartingWith081("MOVE FAST\n"), Is.Null,
                "The phone-simple board must resolve one clear physical roll instead of a second approach button.");
            var zeroPressureRoll079 = FindButtonByName("Expedition Primary Context Action 074");
            Assert.That(zeroPressureRoll079, Is.Not.Null);
            Assert.That(zeroPressureRoll079.interactable, Is.False,
                "The automatic physical roll must be locked while the dice are resolving.");
            Assert.That(zeroPressureRoll079.GetComponentInChildren<Text>().text,
                Does.StartWith("ROLLING 2D6").And.Contain("WATCH THE DICE"));
            yield return Cleanup(secondaryPresenter);

            var encounter = new FakeGuildCityCoordinator();
            encounter.PrepareGeometryEncounter074();
            var encounterPresenter = CreatePresenter(encounter);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;
            ConfigureExpeditionCanvasFor1280By800074();
            AssertNextOrderGeometry074("ENEMY CONTACT");
            yield return Cleanup(encounterPresenter);

            var rescue = new FakeGuildCityCoordinator();
            rescue.PrepareGeometryRescue074();
            var rescuePresenter = CreatePresenter(rescue);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;
            ConfigureExpeditionCanvasFor1280By800074();
            AssertNextOrderGeometry074(ExpeditionBoardProjection074.PatrolFoundDecisionTitle076);
            yield return Cleanup(rescuePresenter);

            var routeHome = new FakeGuildCityCoordinator();
            routeHome.PrepareFinalizable("ObjectiveComplete");
            var routeHomePresenter = CreatePresenter(routeHome);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;
            ConfigureExpeditionCanvasFor1280By800074();
            AssertNextOrderGeometry074("EVERYONE IS COMING HOME");
            yield return Cleanup(routeHomePresenter);
        }

        [UnityTest]
        public IEnumerator FixedFiveBeatStoryBoardsNeverExposeRetiredNodeGraph076()
        {
            var firstChapter = new FakeGuildCityCoordinator();
            firstChapter.PrepareInteractiveExpedition();
            var presenter = CreatePresenter(firstChapter);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;
            foreach (var resolution in new[]
                     {
                         new Vector2(1280f, 800f),
                         new Vector2(1920f, 1080f)
                     })
            {
                ConfigureExpeditionCanvas074(resolution.x, resolution.y);
                AssertCleanFixedFiveBeatBoard076(
                    "MISSION  •  THE BELL BENEATH SKYHOME",
                    "GUILD_HALL_GAMEPLAY_PLATE_071",
                    resolution.x,
                    resolution.y);
            }
            yield return Cleanup(presenter);

            var chapterTwo = new FakeGuildCityCoordinator(firstChapterComplete: true);
            Assert.That(chapterTwo.AcceptGuildCityContract017D(
                FakeGuildCityCoordinator.SecondContractId).Succeeded, Is.True);
            Assert.That(chapterTwo.StartGuildCityExpedition017D().Succeeded, Is.True);
            Assert.That(chapterTwo.MoveGuildCityExpedition017D(
                M1FlowPresenter.ChapterTwoNorthRouteNodeId076).Succeeded, Is.True);
            presenter = CreatePresenter(chapterTwo);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;
            foreach (var resolution in new[]
                     {
                         new Vector2(1280f, 800f),
                         new Vector2(1920f, 1080f)
                     })
            {
                ConfigureExpeditionCanvas074(resolution.x, resolution.y);
                AssertCleanFixedFiveBeatBoard076(
                    "MISSION  •  THE DOOR INSIDE",
                    "WAYGLASS_THRESHOLD_086",
                    resolution.x,
                    resolution.y,
                    "OPERATION 2  •  THE LINES NOT RETURNED  •  ROUTE A  •  THE FRESH MARKS");
            }
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator FixedBoardLanternPatrolRescueShowsBothCeremonyPagesAndReturnsToGateEater076()
        {
            var savePath = Path.Combine(
                Path.GetTempPath(),
                "sd076_live_patrol_ceremony_" + Guid.NewGuid().ToString("N") + ".json");
            M1FlowPresenter presenter = null;
            try
            {
                var staged = CreateActiveFirstStoryAtNode076("N13");
                var coordinator = new M1RuntimeCoordinator(
                    Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"),
                    savePath);
                var campaignField = typeof(M1RuntimeCoordinator).GetField(
                    "_campaign",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                Assert.That(campaignField, Is.Not.Null);
                campaignField.SetValue(coordinator, staged);

                presenter = CreatePresenter(coordinator);
                yield return null;
                Assert.That(coordinator.State.Recruits, Has.Count.EqualTo(10));
                var expectedPreRescueRoster076 = FirstHourFounderRecruitIds076
                    .Concat(FirstHourRosterService071.CharterStableRecruitIds)
                    .ToArray();
                Assert.That(
                    coordinator.State.Recruits.Select(value => value.PortraitAuthorityId),
                    Is.EquivalentTo(expectedPreRescueRoster076),
                    "The fixed-board rescue must begin with the six founders and four authored charter companions.");
                AssertFocusedAction076("CONTINUE GAME");
                Click("CONTINUE GAME");
                yield return null;
                Assert.That(UnityEngine.Object.FindFirstObjectByType<OuterGateworksExploration066>(),
                    Is.Null,
                    "Production play must keep the retired walkable corridor closed.");
                Assert.That(GameObject.Find("Board Quest 081"), Is.Not.Null,
                    "A saved first-hour operation must resume on its exact authored operation beat.");
                Assert.That(coordinator.GuildCity017D.Expedition.CurrentNodeId,
                    Is.EqualTo("N13"));
                Assert.That(
                    ExpeditionBoardProjection074.NeedsFirstHourPatrolRescue074(
                        coordinator.GuildCity017D),
                    Is.True);
                AssertTextContains(ExpeditionBoardProjection074.PatrolFoundDecisionTitle076);
                Assert.That(FindButton("RALLY THE LANTERN PATROL"), Is.Not.Null);
                Click("RALLY THE LANTERN PATROL");
                yield return null;
                yield return null;

                Assert.That(coordinator.State.Recruits, Has.Count.EqualTo(20));
                var expectedPostRescueRoster076 = expectedPreRescueRoster076
                    .Concat(FirstHourRosterService071.PatrolStableRecruitIds)
                    .ToArray();
                Assert.That(
                    coordinator.State.Recruits
                        .Select(value => value.PortraitAuthorityId)
                        .Where(value => !string.IsNullOrWhiteSpace(value)),
                    Is.EquivalentTo(expectedPostRescueRoster076),
                    "The runtime roster projection must preserve the exact founder, charter-companion, and rescued-patrol identities.");
                Assert.That(coordinator.GuildCity017D.Expedition.ObjectiveFlags,
                    Does.Contain(
                        GuildCityExpeditionService017D.FirstHourLanternPatrolRescuedFlag071));
                Assert.That(coordinator.GuildCity017D.Expedition.CanCommitEncounter, Is.True);
                Assert.That(
                    ExpeditionBoardProjection074.NeedsFirstHourPatrolRescue074(
                        coordinator.GuildCity017D),
                    Is.False,
                    "The canonical rescue receipt must advance the board to the Gate-Eater.");
                Assert.That(GameObject.Find("Lantern Patrol Rescue Ceremony 076"), Is.Not.Null);
                AssertNamedTextEquals076(
                    "Lantern Patrol Rescue Heading Text 076",
                    ExpeditionBoardProjection074.RescueCeremonyHeading076(0));
                Assert.That(CountNamedObjects("Rescued Lantern Patrol Portrait "), Is.EqualTo(5));
                foreach (var recruitId in FirstHourRosterService071.PatrolStableRecruitIds.Take(5))
                    Assert.That(GameObject.Find(
                        "Rescued Lantern Patrol Portrait " + recruitId + " 076"), Is.Not.Null);
                foreach (var recruitId in FirstHourRosterService071.PatrolStableRecruitIds.Skip(5))
                    Assert.That(GameObject.Find(
                        "Rescued Lantern Patrol Portrait " + recruitId + " 076"), Is.Null);
                AssertFocusedAction076("MEET THE REST OF THE PATROL");

                Click("MEET THE REST OF THE PATROL");
                yield return null;
                AssertNamedTextEquals076(
                    "Lantern Patrol Rescue Heading Text 076",
                    ExpeditionBoardProjection074.RescueCeremonyHeading076(1));
                Assert.That(CountNamedObjects("Rescued Lantern Patrol Portrait "), Is.EqualTo(5));
                foreach (var recruitId in FirstHourRosterService071.PatrolStableRecruitIds.Take(5))
                    Assert.That(GameObject.Find(
                        "Rescued Lantern Patrol Portrait " + recruitId + " 076"), Is.Null);
                foreach (var recruitId in FirstHourRosterService071.PatrolStableRecruitIds.Skip(5))
                    Assert.That(GameObject.Find(
                        "Rescued Lantern Patrol Portrait " + recruitId + " 076"), Is.Not.Null);
                AssertNamedTextEquals076(
                    "Lantern Patrol Rescue Story Text 076",
                    ExpeditionBoardProjection074.RescueCeremonyRosterTruth076);
                AssertFocusedAction076("REVIEW 20-MEMBER UNION PLAN");

                Click("REVIEW 20-MEMBER UNION PLAN");
                yield return null;
                Assert.That(GameObject.Find("Lantern Patrol Rescue Ceremony 076"), Is.Null);
                Assert.That(GameObject.Find("Union Planner 074"), Is.Not.Null);
                AssertFocusedAction076("SAVE & FACE THE GATE-EATER");
                Click("SAVE & FACE THE GATE-EATER");
                yield return null;
                yield return null;

                Assert.That(UnityEngine.Object.FindFirstObjectByType<OuterGateworksExploration066>(),
                    Is.Null);
                Assert.That(GameObject.Find("Board Quest 081"), Is.Not.Null,
                    "Saving the twenty-member plan must return to the fixed Gate-Eater operation beat.");
                Assert.That(coordinator.GuildCity017D.Expedition.CurrentNodeId,
                    Is.EqualTo("N13"));
                Assert.That(FindButton("FIGHT NOW"), Is.Not.Null);
                AssertFocusedAction076("FIGHT NOW");
                Assert.That(GameObject.Find("Lantern Patrol Rescue Ceremony 076"), Is.Null);
                Assert.That(FindButton("RALLY THE LANTERN PATROL"), Is.Null);

                var persisted = new AtomicSaveStore().ReadWithRecovery(savePath);
                Assert.That(persisted.IsSuccess, Is.True, string.Join("\n", persisted.Errors));
                Assert.That(persisted.Value.CampaignState.Guild.Recruits, Has.Count.EqualTo(20));
                Assert.That(
                    persisted.Value.CampaignState.Guild.Recruits.Select(value =>
                        string.IsNullOrWhiteSpace(value.AuthoredStableRecruitId)
                            ? value.RecruitId
                            : value.AuthoredStableRecruitId),
                    Is.EquivalentTo(expectedPostRescueRoster076),
                    "Save/reload must retain the exact twenty named people without migration duplicates.");
                Assert.That(
                    persisted.Value.CampaignState.Guild.GuildCity.Expedition.ObjectiveFlags,
                    Does.Contain(
                        GuildCityExpeditionService017D.FirstHourLanternPatrolRescuedFlag071));
            }
            finally
            {
                if (presenter != null) UnityEngine.Object.Destroy(presenter.gameObject);
                DeleteSaveFamily076(savePath);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstChapterCompletionReturnsToLivingHallWithSavedGrowthAndChapterTwo074()
        {
            var coordinator = new FakeGuildCityCoordinator(firstChapterComplete: true);
            coordinator.State.Recruits.Single(value =>
                StringComparer.Ordinal.Equals(value.RecruitId, "RECRUIT_1")).ObservedClass = "Priest";
            coordinator.PrepareFirstHallImprovement069();
            var presenter = CreatePresenter(coordinator);
            yield return null;
            AssertNamedTextEquals076("Studio Title Chapter 076", "CHAPTER 2  •  THE DOOR INSIDE");
            AssertNamedTextEquals076(
                "Studio Title Hook 076",
                "THE PATROL CAME HOME.\nTHE WAYGLASS OPENED A ROAD WITHIN.");
            AssertNamedTextEquals076(
                "Studio Title Promise 076",
                "Return to the Hall table. Follow the survey crew's brass line beneath Skyhome.");
            Assert.That(FindButton("OPTIONAL PRACTICE"), Is.Null,
                "The Chapter 2 continuation title must present one unambiguous story action.");
            AssertNamedTextEquals076(
                "Studio Title Chapter Two Proof 076",
                "20 MEMBERS HOME\nWAYGLASS SECURED");
            Click("CONTINUE GAME");
            yield return null;
            yield return null;

            Assert.That(GameObject.Find(M1FlowPresenter.FirstOperationConsequencesRootName077),
                Is.Not.Null,
                "Continue Story must restore the one-tap homecoming directly.");
            AssertNamedTextEquals076(
                "Phone Simple Homecoming Heading 085",
                "MISSION COMPLETE  •  THE PATROL IS HOME");
            AssertNamedTextContains076(
                "Phone Simple Homecoming Summary 085",
                "HOME BASE READY");
            Assert.That(GameObject.Find("First Operation Choose Recovery RECRUIT_1 077"),
                Is.Null);
            Assert.That(GameObject.Find("First Operation Choose Training RECRUIT_1 077"),
                Is.Null);
            var homecoming085 = FindButtonByName("Phone Simple Homecoming Continue 085");
            Assert.That(homecoming085, Is.Not.Null);
            Assert.That(homecoming085.GetComponentInChildren<Text>().text,
                Is.EqualTo("CONTINUE TO GUILD HALL  →"));
            homecoming085.onClick.Invoke();
            yield return null;

            Assert.That(coordinator.SetAssignmentCalls077, Is.EqualTo(1));
            Assert.That(coordinator.GuildCity017D.Assignments.First(value =>
                    StringComparer.Ordinal.Equals(value.RecruitId, "RECRUIT_1")).Kind,
                Is.EqualTo("Recovering"));
            Assert.That(coordinator.ViewRelationshipCalls077, Is.EqualTo(1));
            Assert.That(coordinator.GuildCity017D.Relationships.Single().Viewed, Is.True);
            Assert.That(coordinator.PlaceBuildingCalls, Is.EqualTo(1));
            Assert.That(coordinator.GuildCity017D.PlacedBuildingCount, Is.EqualTo(1));
            Assert.That(coordinator.AssignStaffCalls078, Is.EqualTo(1));
            Assert.That(coordinator.GuildCity017D.StaffedBuildingCount, Is.EqualTo(1));
            Assert.That(coordinator.GuildCity017D.FirstFacilityPayoffAcknowledged080, Is.True);
            Assert.That(M1FlowPresenter.GuidedHallStageForVerification080(
                    coordinator.GuildCity017D, true, false),
                Is.EqualTo(GuildHallGuidedStage080.Ready));
            Assert.That(GameObject.Find("Living Guild Hub 074"), Is.Not.Null,
                "The one-tap homecoming must return to the fixed Living Hall.");
            Assert.That(GameObject.Find(M1FlowPresenter.FirstOperationConsequencesRootName077),
                Is.Null);
            Assert.That(GameObject.Find("Living Guild Hub Objective Card 074"), Is.Not.Null);
            Assert.That(GameObject.Find("Living Guild Hub Roster 074"), Is.Null);
            Assert.That(GameObject.Find("Living Guild Hub Homecoming 076"), Is.Null);
            Assert.That(CountNamedObjects("Living Guild Hub Facility "), Is.EqualTo(4));
            Assert.That(GameObject.Find(
                    "Living Guild Hub Facility ENDLESS_TOWER_081 074"),
                Is.Not.Null,
                "The additive Endless Tower destination must remain available from the Hall.");
            AssertTextContains("CHAPTER 2");
            AssertTextContains("THE DOOR INSIDE");
            AssertNamedTextContains076(
                "Living Guild Hub Rank Resources Day Reputation 074",
                "XP TO SPEND");
            Assert.That(coordinator.State.Recruits.Take(3).All(value => value.TotalPersonalXp == 335), Is.True,
                "The staged post-battle Growth result must remain committed to the persistent member records.");
            Assert.That(coordinator.State.Recruits.Take(3).All(value =>
                    value.ArtMastery.Any(art => StringComparer.Ordinal.Equals(art.DisplayName, "Saber Cut"))),
                Is.True);
            Assert.That(coordinator.GuildCity017D.ClaimedBattleRewardCount, Is.EqualTo(1));
            Assert.That(CountNamedObjects("Member Growth "), Is.EqualTo(0),
                "Growth belongs to the staged battle payoff; the Hall must remain a clean playable hub.");
            AssertNamedTextContains076("Living Guild Hub Chapter 074", "NEXT STORY");
            AssertNamedTextContains076("Living Guild Hub Current Objective 074", "CAMPAIGN");
            AssertNamedTextContains076("Living Guild Hub Current Objective 074", "The Door Inside");
            Assert.That(FindButtonByName("Living Guild Hub Primary CTA 074")
                    .GetComponentInChildren<Text>().text,
                Does.StartWith("BEGIN CHAPTER 2"));
            AssertNoTextContains("GC017D_PLOT_");

            FindButtonByName(
                "Living Guild Hub Facility " +
                WalkableGuildHall069.ArmoryDestinationId069 + " 074").onClick.Invoke();
            yield return null;
            var recoveredLootArmory080 = UnityEngine.Object
                .FindFirstObjectByType<CompactInventoryPresenter069>();
            Assert.That(recoveredLootArmory080, Is.Not.Null);
            Assert.That(GameObject.Find(CompactInventoryPresenter069.RootObjectName), Is.Not.Null,
                "The optional Inventory action must open the compact Armory.");
            recoveredLootArmory080.Refresh("RECRUIT_1");
            Assert.That(recoveredLootArmory080.TrySelectSlot(EquipmentSlotIds.MainHand), Is.True);
            Assert.That(recoveredLootArmory080.TrySelectItem(
                "LOOT_ITEM_070_FIRST_HOMECOMING"), Is.True);
            Assert.That(recoveredLootArmory080.EquipButtonForTests.interactable, Is.True);
            recoveredLootArmory080.EquipButtonForTests.onClick.Invoke();
            yield return null;
            Assert.That(coordinator.EquipItemCalls, Is.EqualTo(1));
            Assert.That(coordinator.State.Recruits.First(value =>
                    StringComparer.Ordinal.Equals(value.RecruitId, "RECRUIT_1"))
                    .Slots.First(value => StringComparer.Ordinal.Equals(
                        value.SlotId, EquipmentSlotIds.MainHand)).EquippedItemId,
                Is.EqualTo("LOOT_ITEM_070_FIRST_HOMECOMING"));
            Assert.That(coordinator.GuildCity017D.RecoveredLootEquipped080, Is.True,
                "Equipping earned loot must still persist when used voluntarily.");
            Assert.That(M1FlowPresenter.GuidedHallStageForVerification080(
                    coordinator.GuildCity017D, true, false),
                Is.EqualTo(GuildHallGuidedStage080.Ready));
            recoveredLootArmory080.BackButtonForTests.onClick.Invoke();
            yield return null;
            Assert.That(GameObject.Find("Living Guild Hub 074"), Is.Not.Null);
            AssertNamedTextContains076("Living Guild Hub Current Objective 074", "CAMPAIGN");
            AssertNamedTextContains076("Living Guild Hub Current Objective 074", "The Door Inside");

            var chapterTwoHallAction = FindButtonByName("Living Guild Hub Primary CTA 074");
            Assert.That(chapterTwoHallAction, Is.Not.Null);
            Assert.That(chapterTwoHallAction.GetComponentInChildren<Text>().text,
                Does.StartWith("BEGIN CHAPTER 2"));
            chapterTwoHallAction.onClick.Invoke();
            yield return null;
            Assert.That(coordinator.AcceptContractCalls, Is.EqualTo(0),
                "The Hall's Chapter 2 button must open the Chapter 2 contract, never restart Chapter 1 through Quick Play.");
            AssertTextContains("THE DOOR INSIDE");
            var chapterTwoAction = FindButton("BEGIN CHAPTER 2");
            Assert.That(chapterTwoAction, Is.Not.Null);
            Assert.That(chapterTwoAction.name, Is.EqualTo("Featured Contract Accept 062"),
                "The contract board must expose one real Chapter 2 action instead of a duplicate no-op objective button.");

            Click("BEGIN CHAPTER 2");
            yield return null;
            Assert.That(coordinator.LastAcceptedContractId,
                Is.EqualTo(FakeGuildCityCoordinator.SecondContractId));
            Assert.That(GameObject.Find(M1FlowPresenter.ChapterTwoOpeningRootName076), Is.Not.Null,
                "BEGIN CHAPTER 2 must enter a real authored scene instead of silently returning to management UI.");
            ConfigureExpeditionCanvas074(1280f, 800f);
            AssertChapterTwoOpeningPresentation076(1280f, 800f);
            ConfigureExpeditionCanvas074(1920f, 1080f);
            AssertChapterTwoOpeningPresentation076(1920f, 1080f);
            ConfigureExpeditionCanvasFor1280By800074();
            AssertTextContains("THE WAYGLASS POINTS BENEATH YOUR GUILD HALL");
            AssertNamedTextEquals076(
                "Chapter Two Opening Subtitle 076",
                "OPERATION 2  •  THE LINES NOT RETURNED");
            AssertTextContains("SITUATION");
            AssertTextContains("YOUR OBJECTIVE");
            AssertTextContains("FIRST ORDER");
            AssertTextContains("OPERATION 2 STARTS AT THE THRESHOLD");
            var thresholdTest076 = FindTextByNamePrefix076(
                "Chapter Two Route Test " + M1FlowPresenter.ChapterTwoThresholdAction078 + " 076");
            var thresholdRisk076 = FindTextByNamePrefix076(
                "Chapter Two Route Risk " + M1FlowPresenter.ChapterTwoThresholdAction078 + " 076");
            Assert.That(thresholdTest076.text,
                Is.EqualTo("FIRST TEST  •  DIPLOMACY / MEDICINE"));
            Assert.That(thresholdRisk076.text, Is.EqualTo("RISK  •  FALSE MARKS"));
            AssertNoTextContains("RESOLVE");
            AssertNoTextContains("DECEPTION");
            AssertNoTextContains("FATIGUE");
            AssertNoTextContains("fastest");
            AssertNoTextContains("slower");
            foreach (var compactTextPrefix076 in new[]
                     {
                         "Chapter Two Opening Subtitle 076",
                         "Chapter Two Opening Operation Status 076",
                         "Chapter Two Scene Heading 076",
                         "Chapter Two Kiri Dialogue 076",
                         "Chapter Two Stakes 076",
                         "Chapter Two First Command Copy 076",
                         "Chapter Two Route Heading " + M1FlowPresenter.ChapterTwoThresholdAction078 + " 076",
                         "Chapter Two Route Story " + M1FlowPresenter.ChapterTwoThresholdAction078 + " 076",
                         "Chapter Two Route Test " + M1FlowPresenter.ChapterTwoThresholdAction078 + " 076",
                         "Chapter Two Route Risk " + M1FlowPresenter.ChapterTwoThresholdAction078 + " 076"
                     })
            {
                AssertAuthoredReadableText076(compactTextPrefix076);
                AssertTextFitsRect074(
                    FindTextByNamePrefix076(compactTextPrefix076),
                    compactTextPrefix076);
            }
            AssertGoldFocusContrast076(
                FindButton(M1FlowPresenter.ChapterTwoThresholdAction078 + "  →"),
                "Chapter 2 Wayglass threshold");
            Assert.That(FindButton(M1FlowPresenter.ChapterTwoThresholdAction078 + "  →"), Is.Not.Null);
            AssertFocusedAction076(M1FlowPresenter.ChapterTwoThresholdAction078 + "  →");
            Assert.That(FindButton("BEGIN EXPEDITION"), Is.Null,
                "The first Chapter 2 interaction must be a story decision, not a generic expedition command.");
            Assert.That(FindButtonByName("Enter Walkable Outer Gateworks 066"), Is.Null,
                "The second contract must not offer the first contract's Gateworks world.");

            var marenSpeaker079 = coordinator.State.Recruits[4];
            marenSpeaker079.DisplayName = "Maren Holt";
            marenSpeaker079.PortraitAuthorityId = "SIGREC_MAREN_HOLT";
            var talaSpeaker079 = coordinator.State.Recruits[5];
            talaSpeaker079.DisplayName = "Tala Stormroad";
            talaSpeaker079.PortraitAuthorityId = "SIGREC_TALA_STORMROAD";

            Click("←  GUILD HALL");
            yield return null;
            AssertNamedTextContains076(
                "Living Guild Hub Current Objective 074",
                "Return to the Wayglass threshold and follow the survey crew's brass line");
            var chooseWayglassRoute076 = FindButtonByName("Living Guild Hub Primary CTA 074");
            Assert.That(chooseWayglassRoute076, Is.Not.Null);
            Assert.That(chooseWayglassRoute076.GetComponentInChildren<Text>().text,
                Is.EqualTo("BEGIN WAYGLASS DESCENT  →"));
            AssertFocusedAction076("BEGIN WAYGLASS DESCENT  →");
            Click("BEGIN WAYGLASS DESCENT  →");
            yield return null;
            Assert.That(GameObject.Find(M1FlowPresenter.ChapterTwoOpeningRootName076), Is.Not.Null);
            AssertFocusedAction076(M1FlowPresenter.ChapterTwoThresholdAction078 + "  →");

            Assert.That(FindButton("REVIEW 2 UNION PLANS"), Is.Not.Null);
            Click("REVIEW 2 UNION PLANS");
            yield return null;
            AssertNamedTextEquals076(
                "Party Readiness Summary 063",
                "2 Union plans are ready for The Door Inside.");
            Assert.That(FindButton("RETURN TO WAYGLASS"), Is.Not.Null);
            Click("RETURN TO WAYGLASS");
            yield return null;
            Assert.That(GameObject.Find(M1FlowPresenter.ChapterTwoOpeningRootName076), Is.Not.Null);
            AssertFocusedAction076(M1FlowPresenter.ChapterTwoThresholdAction078 + "  →");

            Click(M1FlowPresenter.ChapterTwoThresholdAction078 + "  →");
            yield return null;
            Assert.That(coordinator.StartExpeditionCalls, Is.EqualTo(1));
            Assert.That(coordinator.GuildCity017D.Expedition.CurrentNodeId, Is.EqualTo("N00"));
            Assert.That(GameObject.Find("Board Quest 081"), Is.Not.Null);
            AssertTextContains("SELLA");
            var marenChip079 = FindButtonByName("Expedition Companion Identity Chip 078");
            Assert.That(marenChip079, Is.Null,
                "The threshold's one identity card belongs to Sella; Maren remains truthfully named in the quote.");
            AssertNamedTextContains076(
                "Expedition Companion Story Beat Text 076",
                "MAREN");
            AssertChapterTwoStoryIdentity079(
                "SELLA VEY",
                "WAYGLASS WITNESS",
                ExpeditionBoardProjection074.ChapterTwoSellaPortraitResource079);
            AssertNamedTextEquals076("Expedition Field Command Heading 074", "YOUR MOVE");
            Assert.That(FindButtonByName("Expedition Check Approach 0 074"), Is.Null);
            Assert.That(FindButtonByName("Expedition Check Approach 1 074"), Is.Null);

            var automaticRoll076 = FindButtonByName(
                "Expedition Primary Context Action 074");
            if (automaticRoll076 != null &&
                automaticRoll076.GetComponentInChildren<Text>().text.StartsWith(
                    "ROLLING 2D6", StringComparison.Ordinal))
            {
                var automaticRollLabel076 = automaticRoll076.GetComponentInChildren<Text>();
                Assert.That(automaticRollLabel076.text,
                    Does.Contain("WATCH THE DICE"));
                Assert.That(automaticRoll076.interactable, Is.False,
                    "The phone-simple mission must roll once automatically, never offer an approach graph or reroll.");
                AssertTextFitsRect074(automaticRollLabel076,
                    "automatic Sella threshold roll");

                yield return new WaitForSecondsRealtime(
                    M1FlowPresenter.BoardAdventureCardFlipDuration084 + 0.40f);
                Assert.That(coordinator.ResolveCheckCalls, Is.EqualTo(1));
                Assert.That(coordinator.LastCheckEventId,
                    Is.EqualTo("EVENT_FOUND_APPRENTICE"));
                Assert.That(coordinator.GuildCity017D.Expedition.LastCheckAssistantRecruitId,
                    Is.Not.Empty,
                    "The automatic resolver must still commit the best useful friendly partner.");
                Assert.That(coordinator.GuildCity017D.Expedition.LastCheckTotal,
                    Is.EqualTo(7 + coordinator.LastCheckModifier));
                Assert.That(GameObject.Find("Authoritative Dice Roll 084"), Is.Not.Null,
                    "The saved dice must stay visible after the automatic room check resolves.");
                yield return Cleanup(presenter);
                yield break;
            }

            // Migrated legacy fallback retained for old saves that still surface
            // the pre-phone-simple manual check presentation.
            AssertTextContains("Roll 7+ to succeed");

            var apprenticeButton076 = FindButtonByName(
                "Expedition Primary Context Action 074");
            Assert.That(apprenticeButton076, Is.Not.Null);
            var apprenticeLabel076 = apprenticeButton076.GetComponentInChildren<Text>();
            Assert.That(apprenticeLabel076.text,
                Does.StartWith("TEAM UP\n")
                    .And.Contain("ROLL 2D6")
                    .And.Contain("COST 1 PRESSURE"));
            AssertTextFitsRect074(apprenticeLabel076, "Sella threshold action");

            foreach (var resolution080 in new[]
                     {
                         new Vector2(1280f, 800f),
                         new Vector2(1920f, 1080f)
                     })
            {
                ConfigureExpeditionCanvas074(resolution080.x, resolution080.y);
                AssertChapterTwoStoryIdentity079(
                    "SELLA VEY",
                    "WAYGLASS WITNESS",
                    ExpeditionBoardProjection074.ChapterTwoSellaPortraitResource079);
                AssertTextFitsRect074(apprenticeLabel076,
                    "Sella threshold action at " + resolution080.x + "x" + resolution080.y);
            }

            var textScaleField080 = typeof(M1FlowPresenter).GetField(
                "_textScale",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            Assert.That(textScaleField080, Is.Not.Null);
            textScaleField080.SetValue(presenter, 1.45f);
            presenter.SendMessage("BuildCurrentScreen", SendMessageOptions.DontRequireReceiver);
            yield return null;
            ConfigureExpeditionCanvas074(1280f, 800f);
            AssertChapterTwoStoryIdentity079(
                "SELLA VEY",
                "WAYGLASS WITNESS",
                ExpeditionBoardProjection074.ChapterTwoSellaPortraitResource079);
            var largeTextAction080 = FindButtonByName("Expedition Primary Context Action 074")
                .GetComponentInChildren<Text>();
            Assert.That(largeTextAction080.text,
                Does.StartWith("TEAM UP\n")
                    .And.Contain("ROLL 2D6")
                    .And.Contain("COST 1 PRESSURE"));
            AssertTextFitsRect074(largeTextAction080, "Sella threshold action at 145% text scale");

            textScaleField080.SetValue(presenter, 1f);
            presenter.SendMessage("BuildCurrentScreen", SendMessageOptions.DontRequireReceiver);
            yield return null;
            ConfigureExpeditionCanvasFor1280By800074();
            apprenticeButton076 = FindButtonByName("Expedition Primary Context Action 074");
            Assert.That(apprenticeButton076, Is.Not.Null);
            apprenticeLabel076 = apprenticeButton076.GetComponentInChildren<Text>();
            var apprenticeCommandCopy076 = apprenticeLabel076.text;
            apprenticeButton076.onClick.Invoke();
            yield return null;
            Assert.That(coordinator.ResolveCheckCalls, Is.EqualTo(1));
            Assert.That(coordinator.LastCheckEventId, Is.EqualTo("EVENT_FOUND_APPRENTICE"));
            Assert.That(coordinator.GuildCity017D.Expedition.LastCheckAssistantRecruitId,
                Is.Not.Empty,
                "TEAM UP must pass the displayed partner into the authoritative 2d6 command.");
            Assert.That(apprenticeCommandCopy076,
                Does.Contain("ROLL 2D6 " +
                    (coordinator.LastCheckModifier >= 0 ? "+" : string.Empty) +
                    coordinator.LastCheckModifier),
                "The visible dice modifier must equal the modifier committed by the coordinator.");
            Assert.That(coordinator.GuildCity017D.Expedition.LastCheckTotal,
                Is.EqualTo(7 + coordinator.LastCheckModifier));

            Assert.That(FindButtonByName("Select Expedition Destination N01 074"), Is.Not.Null);
            FindButtonByName("Select Expedition Destination N01 074").onClick.Invoke();
            yield return null;
            Assert.That(coordinator.GuildCity017D.Expedition.CurrentNodeId, Is.EqualTo("N01"));

            Assert.That(FindButtonByName("Select Expedition Destination N02 074"), Is.Not.Null);
            var sellaRouteCopy079 = FindButtonByName(
                    "Select Expedition Destination N02 074")
                .GetComponentInChildren<Text>().text;
            Assert.That(sellaRouteCopy079,
                Does.StartWith("MOVE PAWN LEFT\nFLIP THE ROOM\n")
                    .And.Contain("RISK"));
            Assert.That(sellaRouteCopy079,
                Does.Not.Contain("SELLA")
                    .And.Not.Contain("N02")
                    .And.Not.Contain("FOLLOW THE FRESH MARKS")
                    .And.Not.Contain("PATROL RUNNER")
                    .And.Not.Contain("STORY ROUTE"));
            FindButtonByName("Select Expedition Destination N02 074").onClick.Invoke();
            yield return null;
            Assert.That(coordinator.MoveExpeditionCalls, Is.EqualTo(2));
            Assert.That(coordinator.LastMoveDestinationNodeId,
                Is.EqualTo(M1FlowPresenter.ChapterTwoNorthRouteNodeId076));
            Assert.That(coordinator.GuildCity017D.Expedition.BoardId,
                Is.EqualTo("BOARD_LINES_NOT_RETURNED"));
            Assert.That(coordinator.GuildCity017D.Expedition.CurrentNodeId, Is.EqualTo("N02"));
            Assert.That(FindButtonByName("Enter Walkable Outer Gateworks 066"), Is.Null);
            Assert.That(GameObject.Find("Board Quest 081"), Is.Not.Null);
            var talaChip079 = FindButtonByName("Expedition Companion Identity Chip 078");
            Assert.That(talaChip079, Is.Not.Null);
            Assert.That(talaChip079.GetComponentInChildren<Text>().text,
                Does.StartWith("TALA"));
            Assert.That(GameObject.Find("Chapter Two Story Identity SELLA VEY 079"), Is.Null,
                "Tala's spoken false-mark beat must show Tala's roster portrait, not Sella's identity card.");
            AssertBoardQuestFiveSpaceLayout081();
            Assert.That(GameObject.Find("Expedition Board Overlay 074"), Is.Null);

            var zorin079 = coordinator.State.Recruits[0];
            zorin079.DisplayName = "Zorin Bramblecross";
            zorin079.PortraitAuthorityId = "SIGREC071_ZORIN";
            coordinator.GuildCity017D.Expedition.CurrentNodeId = "N03";
            coordinator.GuildCity017D.Expedition.CurrentNodeKind = "SCOUTING";
            coordinator.GuildCity017D.Expedition.CurrentEventId = string.Empty;
            coordinator.GuildCity017D.Expedition.LinkedNodeIds = Array.Empty<string>();
            coordinator.GuildCity017D.Expedition.RequiresResolution = false;
            coordinator.GuildCity017D.Expedition.ResolutionComplete = true;
            coordinator.GuildCity017D.Expedition.CanMove = false;
            presenter.SendMessage("BuildCurrentScreen", SendMessageOptions.DontRequireReceiver);
            yield return null;
            var zorinChip079 = FindButtonByName("Expedition Companion Identity Chip 078");
            Assert.That(zorinChip079, Is.Not.Null);
            Assert.That(zorinChip079.GetComponentInChildren<Text>().text,
                Does.StartWith("ZORIN"));
            Assert.That(GameObject.Find("Chapter Two Story Identity SELLA VEY 079"), Is.Null,
                "Zorin's spoken beat must show Zorin's name and roster portrait, not Sella's identity card.");

            var orren079 = coordinator.State.Recruits[1];
            orren079.DisplayName = "Orren Glass";
            orren079.PortraitAuthorityId = "SIGREC071_ORREN";
            coordinator.GuildCity017D.Expedition.CurrentNodeId = "N10";
            presenter.SendMessage("BuildCurrentScreen", SendMessageOptions.DontRequireReceiver);
            yield return null;
            var orrenChip079 = FindButtonByName("Expedition Companion Identity Chip 078");
            Assert.That(orrenChip079, Is.Not.Null);
            Assert.That(orrenChip079.GetComponentInChildren<Text>().text,
                Does.StartWith("ORREN"));
            Assert.That(GameObject.Find("Chapter Two Story Identity SELLA VEY 079"), Is.Null,
                "Orren's spoken beat must show Orren's name and roster portrait, not Sella's identity card.");

            var jazzi079 = coordinator.State.Recruits[2];
            jazzi079.DisplayName = "Jazzi Wirewick";
            jazzi079.PortraitAuthorityId = "PROC_748DD03A23E1FEB0";
            coordinator.GuildCity017D.Expedition.CurrentNodeId = "N07";
            presenter.SendMessage("BuildCurrentScreen", SendMessageOptions.DontRequireReceiver);
            yield return null;
            var jazziChip079 = FindButtonByName("Expedition Companion Identity Chip 078");
            Assert.That(jazziChip079, Is.Not.Null);
            Assert.That(jazziChip079.GetComponentInChildren<Text>().text,
                Does.StartWith("JAZZI"));
            Assert.That(GameObject.Find("Chapter Two Story Identity SELLA VEY 079"), Is.Null,
                "Jazzi's spoken camp beat must show Jazzi's roster portrait, not Sella's identity card.");

            var tazren079 = coordinator.State.Recruits[3];
            tazren079.DisplayName = "Tazren Warmask";
            tazren079.PortraitAuthorityId = "PROC_5B14E7816E55FFB5";
            coordinator.GuildCity017D.Expedition.CurrentNodeId = "N11";
            presenter.SendMessage("BuildCurrentScreen", SendMessageOptions.DontRequireReceiver);
            yield return null;
            var tazrenChip079 = FindButtonByName("Expedition Companion Identity Chip 078");
            Assert.That(tazrenChip079, Is.Not.Null);
            Assert.That(tazrenChip079.GetComponentInChildren<Text>().text,
                Does.StartWith("TAZREN"));
            Assert.That(GameObject.Find("Chapter Two Story Identity SELLA VEY 079"), Is.Null,
                "Tazren's spoken instrument-recovery beat must show Tazren's roster portrait, not Sella's identity card.");
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator ChapterTwoAutomaticRouteClicksThroughAndPersistsAcrossPresenterReopen076()
        {
            var coordinator = new FakeGuildCityCoordinator(firstChapterComplete: true);
            coordinator.State.Recruits.Single(value =>
                StringComparer.Ordinal.Equals(value.RecruitId, "RECRUIT_1")).ObservedClass = "Priest";
            coordinator.AcceptGuildCityContract017D(FakeGuildCityCoordinator.SecondContractId);
            var presenter = CreatePresenter(coordinator);
            yield return null;
            typeof(M1FlowPresenter).GetField(
                    "_reducedMotion",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(presenter, true);

            AssertNamedTextEquals076(
                "Studio Title Identity 076",
                "CURRENT ORDER\nCONTINUE CHAPTER 2");
            Click("CONTINUE GAME");
            yield return null;
            Assert.That(GameObject.Find(M1FlowPresenter.ChapterTwoOpeningRootName076), Is.Not.Null);
            Assert.That(FindButton(M1FlowPresenter.ChapterTwoThresholdAction078 + "  →"), Is.Not.Null);

            Click(M1FlowPresenter.ChapterTwoThresholdAction078 + "  →");
            yield return null;
            Assert.That(coordinator.StartExpeditionCalls, Is.EqualTo(1),
                "Descending with Kiri must start Operation 2 exactly once.");
            Assert.That(coordinator.GuildCity017D.Expedition.CurrentNodeId, Is.EqualTo("N00"));
            Assert.That(GameObject.Find("Board Quest 081"), Is.Not.Null);
            AssertTextContains("SELLA");
            AssertChapterTwoStoryIdentity079(
                "SELLA VEY",
                "WAYGLASS WITNESS",
                ExpeditionBoardProjection074.ChapterTwoSellaPortraitResource079);
            var automaticThresholdRoll076 = FindButtonByName(
                "Expedition Primary Context Action 074");
            Assert.That(automaticThresholdRoll076, Is.Not.Null);
            var automaticThresholdLabel076 =
                automaticThresholdRoll076.GetComponentInChildren<Text>();
            Assert.That(automaticThresholdLabel076.text,
                Does.StartWith("ROLLING 2D6").And.Contain("WATCH THE DICE"));
            Assert.That(automaticThresholdRoll076.interactable, Is.False,
                "The threshold room must resolve its physical dice once without an approach menu.");
            AssertTextFitsRect074(automaticThresholdLabel076, "Sella threshold automatic roll");
            yield return new WaitForSecondsRealtime(
                M1FlowPresenter.BoardAdventureCardFlipDuration084 + 0.40f);
            Assert.That(coordinator.ResolveCheckCalls, Is.EqualTo(1));
            Assert.That(coordinator.LastCheckEventId, Is.EqualTo("EVENT_FOUND_APPRENTICE"));
            Assert.That(coordinator.GuildCity017D.Expedition.LastCheckAssistantRecruitId,
                Is.Not.Empty);
            Assert.That(coordinator.GuildCity017D.Expedition.LastCheckTotal,
                Is.EqualTo(7 + coordinator.LastCheckModifier));
            Assert.That(GameObject.Find("Authoritative Dice Roll 084"), Is.Not.Null);

            var forwardToJunction076 = FindButtonByName(
                "Expedition Primary Context Action 074");
            Assert.That(forwardToJunction076.GetComponentInChildren<Text>().text,
                Is.EqualTo("MOVE FORWARD\nFLIP NEXT ROOM"));
            forwardToJunction076.onClick.Invoke();
            yield return null;
            Assert.That(coordinator.GuildCity017D.Expedition.CurrentNodeId, Is.EqualTo("N01"));

            var branchView076 = ExpeditionBoardProjection074.Build(coordinator.GuildCity017D);
            var automaticBranch076 = BoardQuestRules081.AutomaticDestination081(
                coordinator.GuildCity017D,
                branchView076);
            Assert.That(automaticBranch076, Is.Not.Null);
            Assert.That(automaticBranch076.NodeId,
                Is.EqualTo(M1FlowPresenter.ChapterTwoNorthRouteNodeId076)
                    .Or.EqualTo(M1FlowPresenter.ChapterTwoUnderhallRouteNodeId076));
            var forwardIntoRoute076 = FindButtonByName(
                "Expedition Primary Context Action 074");
            Assert.That(forwardIntoRoute076.GetComponentInChildren<Text>().text,
                Is.EqualTo("MOVE FORWARD\nFLIP NEXT ROOM"));
            forwardIntoRoute076.onClick.Invoke();
            yield return null;
            Assert.That(coordinator.MoveExpeditionCalls, Is.EqualTo(2));
            Assert.That(coordinator.LastMoveDestinationNodeId,
                Is.EqualTo(automaticBranch076.NodeId));
            Assert.That(coordinator.GuildCity017D.Expedition.BoardId,
                Is.EqualTo("BOARD_LINES_NOT_RETURNED"));
            Assert.That(coordinator.GuildCity017D.Expedition.CurrentNodeId,
                Is.EqualTo(automaticBranch076.NodeId));
            Assert.That(coordinator.GuildCity017D.Expedition.CurrentNodeKind,
                Is.EqualTo("EVENT").Or.EqualTo("SKILL_CHECK"));
            var savedRouteEvent076 = coordinator.GuildCity017D.Expedition.CurrentEventId;
            Assert.That(savedRouteEvent076, Is.Not.Empty);
            Assert.That(coordinator.GuildCity017D.Expedition.VisitedNodeIds,
                Does.Contain(automaticBranch076.NodeId));
            Assert.That(GameObject.Find("Board Quest 081"), Is.Not.Null);
            var branchRoll076 = FindButtonByName(
                "Expedition Primary Context Action 074");
            Assert.That(branchRoll076, Is.Not.Null);
            Assert.That(branchRoll076.GetComponentInChildren<Text>().text,
                Does.StartWith("ROLLING 2D6").And.Contain("WATCH THE DICE"));
            Assert.That(branchRoll076.interactable, Is.False);

            yield return Cleanup(presenter);
            presenter = CreatePresenter(coordinator);
            yield return null;
            AssertNamedTextEquals076(
                "Studio Title Promise 076",
                "Resume the saved route. Find the survey crew and the unrecorded door beneath Skyhome.");
            AssertNamedTextEquals076(
                "Studio Title Identity 076",
                "CURRENT ORDER\nCONTINUE CHAPTER 2");
            Click("CONTINUE GAME");
            yield return null;
            Assert.That(GameObject.Find(M1FlowPresenter.ChapterTwoOpeningRootName076), Is.Null,
                "A persisted shuffled route must not reopen the uncommitted Wayglass decision.");
            Assert.That(GameObject.Find("Board Quest 081"), Is.Not.Null);
            Assert.That(coordinator.GuildCity017D.Expedition.CurrentNodeId,
                Is.EqualTo(automaticBranch076.NodeId));
            branchRoll076 = FindButtonByName("Expedition Primary Context Action 074");
            Assert.That(branchRoll076, Is.Not.Null);
            Assert.That(branchRoll076.GetComponentInChildren<Text>().text,
                Does.StartWith("ROLLING 2D6").And.Contain("WATCH THE DICE"));
            Assert.That(coordinator.ResolveCheckCalls, Is.EqualTo(1),
                "Reopening must preserve the pending room instead of duplicating its dice command immediately.");
            Assert.That(coordinator.GuildCity017D.Expedition.CurrentEventId,
                Is.EqualTo(savedRouteEvent076));
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator ChapterTwoInvalidPlanBlocksRoutesAndFocusesTruthfulFixAction076()
        {
            var coordinator = new FakeGuildCityCoordinator(firstChapterComplete: true);
            coordinator.AcceptGuildCityContract017D(FakeGuildCityCoordinator.SecondContractId);
            coordinator.StartGuildCityExpedition017D();
            coordinator.ResolveGuildCityCheck017D(
                "EVENT_FOUND_APPRENTICE",
                "RECRUIT_1",
                "RECRUIT_4",
                GuildCityExpeditionService017D.CarefulApproachModifier076);
            coordinator.MoveGuildCityExpedition017D("N01");
            Assert.That(
                M1FlowPresenter.ChapterTwoOpeningStageForVerification076(
                    coordinator.GuildCity017D),
                Is.EqualTo(ChapterTwoOpeningStage076.RouteDecision));
            coordinator.State.OpeningUnionsLegal = false;
            coordinator.State.TwoUnionsLegal = false;
            coordinator.GuildCity017D.NormalUnionCount = 1;
            var presenter = CreatePresenter(coordinator);
            yield return null;

            AssertNamedTextEquals076(
                "Studio Title Identity 076",
                "CURRENT ORDER\nCONTINUE CHAPTER 2");
            AssertNamedTextEquals076(
                "Studio Title Promise 076",
                "Repair your active Union plans before beginning the Wayglass descent beneath Skyhome.");
            Click("CONTINUE GAME");
            yield return null;

            Assert.That(GameObject.Find("Union Planner 074"), Is.Not.Null,
                "Continue Story must route a broken Chapter 2 save directly to Union repair.");
            Assert.That(GameObject.Find(M1FlowPresenter.ChapterTwoOpeningRootName076), Is.Null);
            var invalidSave076 = FindButtonByName("Union Planner Save And Return 074");
            Assert.That(invalidSave076, Is.Not.Null);
            Assert.That(invalidSave076.interactable, Is.False);
            Assert.That(FindButtonByName("Select Expedition Destination N02 074"), Is.Null,
                "An invalid saved plan must remain in repair instead of exposing a route command.");

            coordinator.State.OpeningUnionsLegal = true;
            coordinator.State.TwoUnionsLegal = true;
            coordinator.SetFormation(0, "FORMATION_SHIELD_WALL");
            yield return null;
            var repairedSave076 = FindButton("SAVE & RETURN TO WAYGLASS");
            Assert.That(repairedSave076, Is.Not.Null);
            Assert.That(repairedSave076.interactable, Is.True);
            Click("SAVE & RETURN TO WAYGLASS");
            yield return null;
            Assert.That(coordinator.SaveReloadCalls, Is.EqualTo(1));
            Assert.That(GameObject.Find(M1FlowPresenter.ChapterTwoOpeningRootName076), Is.Null);
            Assert.That(GameObject.Find("Board Quest 081"), Is.Not.Null,
                "Saving repaired Chapter 2 Unions must return to the saved N01 route choice.");
            Assert.That(CountNamedObjects("Select Expedition Destination "), Is.EqualTo(0));
            var repairedView076 = ExpeditionBoardProjection074.Build(coordinator.GuildCity017D);
            var repairedDestination076 = BoardQuestRules081.AutomaticDestination081(
                coordinator.GuildCity017D,
                repairedView076);
            Assert.That(repairedDestination076, Is.Not.Null);
            Assert.That(repairedDestination076.NodeId,
                Is.EqualTo("N02").Or.EqualTo("N04"));
            var repairedMove076 = FindButtonByName("Expedition Primary Context Action 074");
            Assert.That(repairedMove076, Is.Not.Null);
            Assert.That(repairedMove076.GetComponentInChildren<Text>().text,
                Is.EqualTo("MOVE FORWARD\nFLIP NEXT ROOM"));
            Assert.That(repairedMove076.GetComponentInChildren<Text>().text,
                Does.Not.Contain("SELLA").And.Not.Contain("ORRA")
                    .And.Not.Contain("N02").And.Not.Contain("N04"));
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(repairedMove076.gameObject));
            repairedMove076.onClick.Invoke();
            yield return null;
            Assert.That(coordinator.LastMoveDestinationNodeId,
                Is.EqualTo(repairedDestination076.NodeId));
            Assert.That(coordinator.GuildCity017D.Expedition.CurrentNodeId,
                Is.EqualTo(repairedDestination076.NodeId));
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator RealRuntimeChapterTwoResumeHonorsUnionRepairPromise078()
        {
            var savePath = Path.Combine(
                Path.GetTempPath(),
                "sd078_runtime_chapter_two_repair_" + Guid.NewGuid().ToString("N") + ".json");
            var previousMigrationSetting =
                M1RuntimeCoordinator.SuppressAutomaticFirstHourRosterMigration072;
            M1FlowPresenter presenter = null;
            try
            {
                M1RuntimeCoordinator.SuppressAutomaticFirstHourRosterMigration072 = true;
                var staged = CreateInvalidActiveChapterTwoAtJunction078();
                new AtomicSaveStore().Write(
                    savePath,
                    SaveEnvelopeV1.Create(staged, DateTime.UtcNow));
                var coordinator = new M1RuntimeCoordinator(
                    Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"),
                    savePath);
                Assert.That(coordinator.State.HasCampaign, Is.True);
                Assert.That(coordinator.State.OpeningUnionsLegal, Is.False);
                Assert.That(coordinator.GuildCity017D.Expedition, Is.Not.Null);
                Assert.That(coordinator.GuildCity017D.Expedition.CurrentNodeId, Is.EqualTo("N01"));

                presenter = CreatePresenter(coordinator);
                yield return null;
                AssertNamedTextEquals076(
                    "Studio Title Promise 076",
                    "Repair your active Union plans before beginning the Wayglass descent beneath Skyhome.");

                Click("CONTINUE GAME");
                yield return null;

                var activeScreen078 = typeof(M1FlowPresenter).GetField(
                    "_screen",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                Assert.That(activeScreen078, Is.Not.Null);
                Assert.That(activeScreen078.GetValue(presenter),
                    Is.EqualTo(M1Screen.UnionBuilder),
                    "The real title branch must select Union repair before rendering it.");
                Assert.That(GameObject.Find("Union Planner 074"), Is.Not.Null,
                    "The shipping M1RuntimeCoordinator branch must honor the title's repair promise.");
                Assert.That(GameObject.Find("Board Quest 081"), Is.Null,
                    "An illegal saved plan must not expose Chapter 2 route commands before repair.");
                Assert.That(coordinator.GuildCity017D.Expedition.CurrentNodeId, Is.EqualTo("N01"),
                    "Opening Union repair must preserve the exact saved Chapter 2 junction.");
            }
            finally
            {
                M1RuntimeCoordinator.SuppressAutomaticFirstHourRosterMigration072 =
                    previousMigrationSetting;
                if (presenter != null) UnityEngine.Object.Destroy(presenter.gameObject);
                DeleteSaveFamily076(savePath);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ShippingRecruitmentDeskUsesEarnedEligibilityAndNeverOffersRandomGroups124()
        {
            // Fake authority deliberately controls the eligibility projection;
            // actual desk layout, raycast interception and commands are exercised.
            foreach (var mode in new[] { "no_leads", "queued", "eligible" })
            {
                var coordinator = new FakeGuildCityCoordinator();
                coordinator.PrepareRecruitmentBoard();
                if (mode == "queued")
                {
                    var template = coordinator.GuildCity017D.Applicants[0];
                    coordinator.GuildCity017D.Applicants = Enumerable.Range(1, 10).Select(index =>
                    {
                        var copy = new GuildCityApplicantView017D
                        {
                            RaceId = template.RaceId, WorldId = template.WorldId,
                            ClassTendencyId = template.ClassTendencyId,
                            AuthoredRole096 = template.AuthoredRole096,
                            AuthoredWeaponStyle096 = template.AuthoredWeaponStyle096,
                            LeadershipBand = template.LeadershipBand,
                            SigningCostTreasuryXp = template.SigningCostTreasuryXp,
                            IsSigned = false, CanAfford = template.CanAfford,
                            VisualSeed = template.VisualSeed, PortraitAuthorityId = template.PortraitAuthorityId,
                            ClassSymbol = template.ClassSymbol, Kind = template.Kind,
                            ObservedSummary = template.ObservedSummary, TraitSummary = template.TraitSummary,
                            EquipmentSummary = template.EquipmentSummary, PersonalHook = template.PersonalHook
                        };
                        copy.Slot = index; copy.RecruitId = "PENDING_LEGACY_124_" + index;
                        copy.DisplayName = "Saved Interview " + index;
                        return copy;
                    }).ToArray();
                }
                coordinator.GuildCity017D.PendingExpeditionRecruitLeadNames089 = mode == "no_leads"
                    ? Array.Empty<string>() : new[] { "Earned Contact" };
                coordinator.GuildCity017D.CanInviteEarnedContacts124 = mode == "eligible";
                var presenter = CreatePresenter(coordinator);
                yield return null;
                Click("CONTINUE GAME");
                yield return null;
                var button = GameObject.Find(mode == "no_leads" ? "Refresh Applicant Group 074" : "Invite Expedition Recruit Lead 089")
                    ?.GetComponent<Button>();
                Assert.That(button, Is.Not.Null, mode);
                var caption = button.GetComponentInChildren<Text>().text;
                Assert.That(caption, Does.Not.Contain("NEW GROUP"));
                Assert.That(caption, Does.Not.Contain(" XP"));
                Assert.That(coordinator.CommitBoardCalls, Is.Zero);
                Assert.That(coordinator.RefreshBoardCalls, Is.Zero);
                if (mode == "queued")
                {
                    Assert.That(caption, Does.Contain("CONTACTS QUEUED"));
                    Assert.That(button.interactable, Is.False);
                    Assert.That(coordinator.GuildCity017D.Applicants.Count, Is.EqualTo(10));
                }
                else
                {
                    Assert.That(caption, Does.Contain(mode == "eligible" ? "INVITE" : "EARN CONTACTS"));
                    PointerClickRecruitment124(button);
                    yield return null;
                    Assert.That(coordinator.CommitBoardCalls, Is.EqualTo(mode == "eligible" ? 1 : 0));
                    Assert.That(coordinator.RefreshBoardCalls, Is.Zero);
                    if (mode == "eligible")
                        Assert.That(coordinator.GuildCity017D.Applicants.Any(value => value.RecruitId == "EARNED_CONTACT_124"), Is.True);
                }
                yield return Cleanup(presenter);
            }
        }

        private static void PointerClickRecruitment124(Button button)
        {
            Assert.That(button.interactable && button.gameObject.activeInHierarchy, Is.True);
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            Assert.That(point.x, Is.InRange(0f, (float)Screen.width));
            Assert.That(point.y, Is.InRange(0f, (float)Screen.height));
            var pointer = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            var hit = hits[0].gameObject;
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit), Is.EqualTo(button.gameObject), "The visible control must receive the pointer.");
            pointer.pointerPress = ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
            Assert.That(pointer.pointerPress, Is.EqualTo(button.gameObject));
            ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerClickHandler);
        }

        [UnityTest]
        public IEnumerator RecruitmentDeskIsAFocusedPermanentRecruitmentLoop074()
        {
            var coordinator = new FakeGuildCityCoordinator();
            coordinator.PrepareRecruitmentBoard();
            var presenter = CreatePresenter(coordinator);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;
            Assert.That(FindButton("ACCEPT CONTRACT"), Is.Null,
                "The first rescue contract must not bypass the required permanent recruit.");

            Assert.That(GameObject.Find("Recruitment Desk 074"), Is.Not.Null);
            Assert.That(GameObject.Find("Selected Applicant Portrait 074"), Is.Not.Null);
            Assert.That(GameObject.Find("Applicant Decision Card 074"), Is.Not.Null);
            Assert.That(GameObject.Find("Applicant Shortlist 074"), Is.Not.Null);
            AssertTextContains("RECRUITMENT DESK");
            AssertTextContains("Review saved interviews. Missions earn named contacts and XP.");
            AssertTextContains("ROLE");
            AssertTextContains("GROWTH");
            AssertTextContains("TRAINING");
            Assert.That(coordinator.GuildCity017D.Applicants[0].AuthoredWeaponStyle096, Is.Null.Or.Empty,
                "This legacy fixture has no authored weapon-style metadata.");
            AssertNoTextContains("WEAPON STYLE");
            AssertTextContains("TRAITS");
            AssertTextContains("STARTING GEAR");
            AssertTextContains("WHY THEY CAME");
            AssertTextContains("PERMANENT RECRUIT");
            AssertTextContains("TODAY'S APPLICANTS");
            Assert.That(FindButton("RECRUIT MIRA VALE"), Is.Not.Null);
            Assert.That(FindButton("RECRUIT MIRA VALE").interactable, Is.True);
            Assert.That(CountNamedObjects("Applicant Shortlist CANDIDATE_"), Is.EqualTo(3));
            Assert.That(UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None), Is.Empty,
                "Recruitment is one fixed portrait/decision/shortlist screen.");
            AssertNoTextContains("SIGNATURE");
            AssertNoTextContains("PROCEDURAL");
            AssertNoTextContains("DRY STREAK");
            AssertNoTextContains("RARITY");
            AssertNoTextContains("GACHA");
            AssertNoTextContains("AUTHORITY");
            AssertTextContains("GATE SPEAR");
            AssertTextContains("REPAIRED SHIELD");
            AssertNoTextContains("EQ_PROC");
            AssertNoTextContains("…");

            Click("RECRUIT MIRA VALE");
            yield return null;
            Assert.That(coordinator.SignApplicantCalls, Is.EqualTo(1));
            Assert.That(coordinator.GuildCity017D.TotalRecruitCount, Is.EqualTo(7));
            Assert.That(coordinator.State.AllSixSigned, Is.True,
                "Adding a recurring member must not invalidate the completed founding roster.");
            Assert.That(coordinator.State.Recruits.Any(value =>
                StringComparer.Ordinal.Equals(value.RecruitId, "CANDIDATE_1")), Is.True);
            Assert.That(FindButton("PLACE IN A UNION"), Is.Not.Null);
            AssertTextContains("PERMANENT MEMBER");

            Click("PLACE IN A UNION");
            yield return null;
            Assert.That(GameObject.Find("Union Planner 074"), Is.Not.Null);
            Assert.That(FindButtonByName("Create Starter Union 074"), Is.Not.Null);
            FindButtonByName("Create Starter Union 074").onClick.Invoke();
            yield return null;
            var thirdUnion = FindButtonByName("Union Planner Tab 2 074");
            Assert.That(thirdUnion, Is.Not.Null);
            thirdUnion.onClick.Invoke();
            yield return null;
            var addRecruit = FindButtonByName("Union Planner Reserve Member CANDIDATE_1 074");
            Assert.That(addRecruit, Is.Not.Null);
            Assert.That(addRecruit.interactable, Is.True);
            PointerClickRecruitment124(addRecruit);
            yield return null;
            Assert.That(coordinator.AssignmentCalls, Is.Zero,
                "Selecting a reserve must wait for the player's exact destination slot.");
            var destination = FindButtonByName("Union Planner Member Slot 0 074");
            Assert.That(destination, Is.Not.Null);
            PointerClickRecruitment124(destination);
            yield return null;
            Assert.That(coordinator.ReserveAssignmentCalls109, Is.EqualTo(1));
            Assert.That(coordinator.AssignmentCalls, Is.EqualTo(1));
            Assert.That(coordinator.LastAssignedSlotIndex, Is.Zero);
            Assert.That(coordinator.LastAssignedRecruitId, Is.EqualTo("CANDIDATE_1"));
            Assert.That(coordinator.LastAssignedUnionIndex, Is.EqualTo(2));
            Assert.That(coordinator.State.Unions[2].MemberRecruitIds,
                Does.Contain("CANDIDATE_1"));
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator RecruitmentDeskShortlistSelectionAndDeclinePreserveGuildMembers074()
        {
            var coordinator = new FakeGuildCityCoordinator();
            coordinator.PrepareRecruitmentBoard();
            var presenter = CreatePresenter(coordinator);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;

            Assert.That(GameObject.Find("Recruitment Desk 074"), Is.Not.Null);
            var tamsin = FindButtonByName("Applicant Shortlist CANDIDATE_2 074");
            Assert.That(tamsin, Is.Not.Null);
            tamsin.onClick.Invoke();
            yield return null;
            AssertTextContains("TAMSIN REED");
            AssertTextContains("PATIENT • CURIOUS");
            Assert.That(FindButton("DECLINE FOR THIS BOARD"), Is.Null,
                "The focused Release 074 desk keeps one permanent recruit CTA and a reversible return to Hall.");

            var decline = coordinator.DeclineGuildCityApplicant017D("CANDIDATE_2");

            Assert.That(decline.Succeeded, Is.True);
            Assert.That(coordinator.DeclineApplicantCalls, Is.EqualTo(1));
            Assert.That(coordinator.GuildCity017D.Applicants.Count, Is.EqualTo(2));
            Assert.That(coordinator.GuildCity017D.TotalRecruitCount, Is.EqualTo(6),
                "Declining an interview must never remove a permanent Guild member.");
            yield return Cleanup(presenter);
        }

        [UnityTest]
        public IEnumerator UnionPlannerCanSavePartyReadinessAndReturnToGuild()
        {
            var coordinator = new FakeGuildCityCoordinator();
            var presenter = CreatePresenter(coordinator);
            yield return null;
            Click("CONTINUE GAME");
            yield return null;
            var unions = FindButtonByName("Guild Mobile Nav Unions 084");
            Assert.That(unions, Is.Not.Null);
            unions.onClick.Invoke();
            yield return null;
            Assert.That(GameObject.Find("Union Planner 074"), Is.Not.Null);
            var save = FindButtonByName("Union Planner Save And Return 074");
            Assert.That(save, Is.Not.Null);
            Assert.That(save.interactable, Is.True);
            Assert.That(save.GetComponentInChildren<Text>().text,
                Is.EqualTo("SAVE & RETURN TO GUILD"));
            save.onClick.Invoke();
            yield return null;

            Assert.That(coordinator.SaveReloadCalls, Is.EqualTo(1));
            Assert.That(GameObject.Find("Union Planner 074"), Is.Null);
            Assert.That(GameObject.Find("Living Guild Hub 074"), Is.Not.Null,
                "Saving a legal Union plan must return directly to the playable Guild Hall.");
            AssertNoTextContains("HASH");
            AssertNoTextContains("PROOF");
            yield return Cleanup(presenter);
        }

        private static CampaignState CreateActiveFirstStoryAtNode076(string nodeId)
        {
            var contentRoot = Path.Combine(
                Application.streamingAssetsPath,
                "Authority",
                "CONTENT");
            var content = GuildCityContent017D.LoadFromDirectory(Path.Combine(
                contentRoot,
                "GUILD_CITY_017D"));
            var expeditions = new GuildCityExpeditionService017D();
            var roster = FirstHourRosterService071.LoadFromContentRoot(contentRoot);
            var preRescue = RequireCampaign076(roster.EnsureCharterRoster(CreateCampaign076()));
            Assert.That(preRescue.Guild.Recruits, Has.Count.EqualTo(
                GuildCityExpeditionService017D.FirstHourThreeBattleRosterCount071));
            var campaign = RequireCampaign076(expeditions.AcceptContract(
                preRescue,
                content,
                GuildCityExpeditionService017D.FirstStoryContractId066));
            campaign = RequireCampaign076(expeditions.StartExpedition(campaign, content));
            var stagedPatrolRoute076 = new[]
            {
                "N00", "N01", "N02", "N03", "N04", "N05", "N06", "N07",
                "N08", "N10", "N11", nodeId
            };
            var expedition = campaign.Guild.GuildCity.Expedition.With(
                currentNodeId: nodeId,
                status: ExpeditionStatus017D.Active,
                visitedNodeIds: stagedPatrolRoute076,
                revealedNodeIds: stagedPatrolRoute076.Concat(new[] { "N14" }).ToArray(),
                objectiveFlags: new[]
                {
                    GuildCityExpeditionService017D.EncounterClearedFlag("N01"),
                    GuildCityExpeditionService017D.EncounterClearedFlag("N06")
                },
                lastCheckpointId: "first_hour_076_live_ceremony_stage");
            var city = campaign.Guild.GuildCity.With(
                expedition: expedition,
                replaceExpedition: true,
                pendingEncounter: null,
                replacePendingEncounter: true,
                pendingBattleReturn: null,
                replacePendingBattleReturn: true,
                lastCheckpointId: "first_hour_076_live_ceremony_stage");
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        private static CampaignState CreateInvalidActiveChapterTwoAtJunction078()
        {
            var contentRoot = Path.Combine(
                Application.streamingAssetsPath,
                "Authority",
                "CONTENT");
            var content = GuildCityContent017D.LoadFromDirectory(Path.Combine(
                contentRoot,
                "GUILD_CITY_017D"));
            var expeditions = new GuildCityExpeditionService017D();
            var campaign = RequireCampaign076(expeditions.AcceptContract(
                CreateCampaign076(),
                content,
                GuildCityExpeditionService017D.SecondStoryContractId076));
            campaign = RequireCampaign076(expeditions.StartExpedition(campaign, content));
            campaign = RequireCampaign076(expeditions.ResolveCommittedCheck(
                campaign,
                content,
                "EVENT_FOUND_APPRENTICE",
                FirstHourFounderRecruitIds076[0],
                FirstHourFounderRecruitIds076[1],
                GuildCityExpeditionService017D.CarefulApproachModifier076));
            campaign = RequireCampaign076(expeditions.CommitMove(campaign, content, "N01"));

            var chapterTwoCity = campaign.Guild.GuildCity.With(
                operationOrdinal: 1,
                lastCheckpointId: "chapter_two_invalid_union_resume_078");
            var chapterOneDevelopment = campaign.Guild.Development.RecordBattleReward(
                "CHAPTER_ONE_REWARD_078",
                100,
                100);
            var invalidGuild = campaign.Guild.With(
                    campaign.Guild.TreasuryXp,
                    campaign.Guild.Recruits,
                    Array.Empty<UnionState>(),
                    campaign.Guild.Inventory,
                    chapterOneDevelopment)
                .WithGuildCity(chapterTwoCity);
            return campaign.With(invalidGuild, campaign.OpeningFlow);
        }

        private static CampaignState CreateCampaign076()
        {
            var recruits = FirstHourFounderRecruitIds076
                .Select(recruitId => new RecruitState(recruitId, 100, 100, 20, 20))
                .ToArray();
            var unions = new[]
            {
                new UnionState(
                    "U1",
                    "First Union",
                    UnionKind.Normal,
                    FirstHourFounderRecruitIds076[0],
                    FirstHourFounderRecruitIds076.Take(3).ToArray(),
                    "FORMATION_SKIRMISH_LINE",
                    "DOCTRINE_BALANCED",
                    30,
                    7000),
                new UnionState(
                    "U2",
                    "Second Union",
                    UnionKind.Normal,
                    FirstHourFounderRecruitIds076[3],
                    FirstHourFounderRecruitIds076.Skip(3).Take(3).ToArray(),
                    "FORMATION_SKIRMISH_LINE",
                    "DOCTRINE_BALANCED",
                    30,
                    7000)
            };
            var guild = new GuildState("GUILD_FIRST_HOUR_076", 0, recruits, unions);
            var flow = new OpeningFlowState(
                OpeningStage.Complete,
                "SDGOW_TUTORIAL_V1_001",
                true,
                null,
                false,
                439,
                0,
                true,
                true,
                true,
                true,
                "complete");
            var profile = new NewGuildProfileState(
                "Tester",
                SecondDimension.Core.GameMode.Standard,
                TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(),
                false);
            return new CampaignState(
                "00000000-0000-0000-0000-000000076076",
                76076,
                "1.0",
                ModeRuleSnapshot.StandardDefaults(),
                guild,
                profile,
                flow);
        }

        private static CampaignState RequireCampaign076(
            SecondDimension.Core.Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }

        private static void DeleteSaveFamily076(string savePath)
        {
            foreach (var path in new[] { savePath, savePath + ".bak", savePath + ".tmp" })
                if (File.Exists(path)) File.Delete(path);
        }

        private static M1FlowPresenter CreatePresenter(IM1PresentationCoordinator coordinator)
        {
            var host = new GameObject("M1 Presentation Test Host");
            var presenter = host.AddComponent<M1FlowPresenter>();
            presenter.Initialize(coordinator);
            return presenter;
        }

        private static IEnumerator Cleanup(M1FlowPresenter presenter)
        {
            UnityEngine.Object.Destroy(presenter.gameObject);
            yield return null;
        }

        private static void Click(string label)
        {
            var button = FindButton(label);
            Assert.That(button, Is.Not.Null, "Button not found: " + label);
            Assert.That(button.interactable, Is.True, "Button was not interactable: " + label);
            button.onClick.Invoke();
        }

        private static Button FindButton(string label)
        {
            var canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .FirstOrDefault(value => StringComparer.Ordinal.Equals(value.name, "M1 Playable Proof Canvas"));
            return canvas == null ? null : canvas.GetComponentsInChildren<Button>()
                .FirstOrDefault(value =>
                {
                    return value.GetComponentsInChildren<Text>()
                        .Any(text => text != null && StringComparer.Ordinal.Equals(text.text, label));
                });
        }

        private static Button FindButtonByName(string name) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(value => StringComparer.Ordinal.Equals(value.name, name));

        private static void ClickButtonByName076(string name)
        {
            var button = FindButtonByName(name);
            Assert.That(button, Is.Not.Null, "Button object not found: " + name);
            Assert.That(button.gameObject.activeInHierarchy, Is.True,
                "Button object was not visible: " + name);
            Assert.That(button.interactable, Is.True,
                "Button object was not interactable: " + name);
            button.onClick.Invoke();
        }

        private static void AssertFocusedAction076(string label)
        {
            var button = FindButton(label);
            Assert.That(button, Is.Not.Null, "Focused action was not found: " + label);
            Assert.That(EventSystem.current, Is.Not.Null,
                "No EventSystem exists while checking controller focus for " + label + ".");
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(button.gameObject),
                "Controller focus must start on the visible primary action: " + label + ".");
        }

        private static void AssertFocusedButtonByName076(string name)
        {
            var button = FindButtonByName(name);
            Assert.That(button, Is.Not.Null, "Focused button was not found: " + name);
            Assert.That(EventSystem.current, Is.Not.Null,
                "No EventSystem exists while checking controller focus for " + name + ".");
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(button.gameObject),
                "Controller focus must start on " + name + ".");
        }

        private static int CountNamedObjects(string prefix) =>
            UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None)
                .Count(value => value.name.StartsWith(prefix, StringComparison.Ordinal));

        private static void ConfigureExpeditionCanvasFor1280By800074() =>
            ConfigureExpeditionCanvas074(1280f, 800f);

        private static void AssertFoundingLeadPresentationAtResolution078(
            IReadOnlyList<string> recruitIds,
            float width,
            float height)
        {
            ConfigureExpeditionCanvas074(width, height);
            foreach (var recruitId in recruitIds ?? Array.Empty<string>())
            {
                var card = FindButtonByName("Founding Applicant " + recruitId + " 076");
                var cardLabel = FindResponsiveButtonLabel078(card);
                var ribbon = GameObject.Find(
                    "Founding Applicant Role Color " + recruitId + " 076")
                    ?.GetComponent<RectTransform>();
                var ribbonLabel = FindTextByNamePrefix076(
                    "Founding Applicant Role " + recruitId + " 076");
                Assert.That(card, Is.Not.Null);
                Assert.That(cardLabel, Is.Not.Null);
                Assert.That(cardLabel.text.Trim(), Is.Not.Empty,
                    "A founder card may never capture without its name and readable job promise.");
                Assert.That(ribbon, Is.Not.Null);
                Assert.That(ribbonLabel, Is.Not.Null);
                Assert.That(ribbonLabel.text.Trim(), Is.Not.Empty,
                    "A founder role-color ribbon may never capture blank.");
                AssertTextFitsRect074(cardLabel,
                    card.name + " at " + width + "x" + height);
                AssertTextFitsRect074(ribbonLabel,
                    ribbonLabel.name + " at " + width + "x" + height);
                AssertRectInside074(card.GetComponent<RectTransform>(), ribbon,
                    "Founder role ribbon at " + width + "x" + height);
                AssertRectInside074(ribbon, ribbonLabel.rectTransform,
                    "Founder role label at " + width + "x" + height);
            }
        }

        private static void AssertFoundingDestinationPresentationAtResolution078(
            IReadOnlyList<M1UnionView> unions,
            float width,
            float height)
        {
            ConfigureExpeditionCanvas074(width, height);
            var visible = (unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null)
                .Take(3)
                .ToArray();
            Assert.That(visible, Has.Length.EqualTo(3));
            for (var index = 0; index < visible.Length; index++)
            {
                var union = visible[index];
                var card = FindButtonByName(
                    "Founding Union Destination " + union.Index + " 078");
                var cardLabel = FindResponsiveButtonLabel078(card);
                var ribbon = GameObject.Find(
                    "Founding Union Destination Role Color " + union.Index + " 078")
                    ?.GetComponent<RectTransform>();
                var ribbonLabel = FindTextByNamePrefix076(
                    "Founding Union Destination Role " + union.Index + " 078");
                Assert.That(card, Is.Not.Null);
                Assert.That(cardLabel, Is.Not.Null);
                Assert.That(cardLabel.text.Trim(), Is.Not.Empty);
                Assert.That(ribbon, Is.Not.Null);
                Assert.That(ribbonLabel, Is.Not.Null);
                Assert.That(ribbonLabel.text,
                    Is.EqualTo(M1FlowPresenter.FoundingDestinationRibbonForVerification078(index)),
                    "The Union color ribbon must state its simple battlefield job.");
                AssertTextFitsRect074(cardLabel,
                    card.name + " at " + width + "x" + height);
                AssertTextFitsRect074(ribbonLabel,
                    ribbonLabel.name + " at " + width + "x" + height);
                AssertRectInside074(card.GetComponent<RectTransform>(), ribbon,
                    "Union job ribbon at " + width + "x" + height);
                AssertRectInside074(ribbon, ribbonLabel.rectTransform,
                    "Union job label at " + width + "x" + height);
            }
        }

        private static void AssertFoundingFormationPresentationAtResolution078(
            IReadOnlyList<M1ChoiceView> formations,
            float width,
            float height)
        {
            ConfigureExpeditionCanvas074(width, height);
            var instruction = FindTextByNamePrefix076(
                "Founding Preparation Instruction Copy 078");
            Assert.That(instruction, Is.Not.Null);
            Assert.That(instruction.text,
                Does.Contain("whole team").IgnoreCase,
                "Formation setup must explain that this is one decision for the whole team.");
            AssertNoTextContains("M2 ROLE");
            AssertNoTextContains("NO RAW STAT CHANGE");
            var visible = (formations ?? Array.Empty<M1ChoiceView>())
                .Where(value => value != null)
                .Take(3)
                .ToArray();
            Assert.That(visible, Has.Length.EqualTo(3),
                "Founding must keep exactly three readable starter formations.");
            var diagrams = new HashSet<string>(StringComparer.Ordinal);
            foreach (var formation in visible)
            {
                var card = FindButtonByName(
                    "Founding Formation Choice " + formation.Id + " 078");
                var title = FindResponsiveButtonLabel078(card);
                var diagram = GameObject.Find(
                    "Founding Formation Diagram " + formation.Id + " 078")
                    ?.GetComponent<RectTransform>();
                var positions = FindTextByNamePrefix076(
                    "Founding Formation Positions " + formation.Id + " 078");
                var front = FindTextByNamePrefix076(
                    "Founding Formation Front Cue " + formation.Id + " 078");
                var rear = FindTextByNamePrefix076(
                    "Founding Formation Rear Cue " + formation.Id + " 078");
                var promise = FindTextByNamePrefix076(
                    "Founding Formation Promise " + formation.Id + " 078");
                Assert.That(card, Is.Not.Null);
                Assert.That(title, Is.Not.Null);
                Assert.That(diagram, Is.Not.Null);
                Assert.That(positions, Is.Not.Null);
                Assert.That(front, Is.Not.Null);
                Assert.That(rear, Is.Not.Null);
                Assert.That(promise, Is.Not.Null);
                Assert.That(positions.text.Count(value => value == '●'), Is.EqualTo(6),
                    formation.DisplayName + " must show all six possible member positions.");
                diagrams.Add(positions.text);
                Assert.That(front.text, Does.Contain("ENEMY"));
                Assert.That(rear.text, Does.Contain("BACK"));
                Assert.That(promise.text,
                    Is.EqualTo(M1FlowPresenter.FriendlyFormationPromiseForVerification078(
                        formation)));
                Assert.That(promise.text, Does.Not.Contain("M2"));
                Assert.That(promise.text, Does.Not.Contain("RAW STAT"));
                AssertTextFitsRect074(title,
                    formation.DisplayName + " title at " + width + "x" + height);
                AssertTextFitsRect074(positions,
                    formation.DisplayName + " diagram at " + width + "x" + height);
                AssertTextFitsRect074(front,
                    formation.DisplayName + " front cue at " + width + "x" + height);
                AssertTextFitsRect074(rear,
                    formation.DisplayName + " rear cue at " + width + "x" + height);
                AssertTextFitsRect074(promise,
                    formation.DisplayName + " promise at " + width + "x" + height);
                AssertRectInside074(card.GetComponent<RectTransform>(), diagram,
                    formation.DisplayName + " diagram at " + width + "x" + height);
            }
            Assert.That(diagrams.Count, Is.EqualTo(3),
                "The three starter formations need three visibly distinct position diagrams.");
        }

        private static void AssertFoundingConfirmationPresentationAtResolution078(
            M1RuntimeCoordinator coordinator,
            string leadRecruitId,
            int leadUnionIndex,
            float width,
            float height)
        {
            ConfigureExpeditionCanvas074(width, height);
            AssertNoTextContains("LEGAL FIELD UNIONS");
            AssertNoTextContains("LEGAL SLOTS");
            var savedChoices = FindTextByNamePrefix076(
                "Founding Rescue Team Saved Choices 078");
            var reserveText = FindTextByNamePrefix076(
                "Founding Confirmation Reserve Text 078");
            Assert.That(savedChoices, Is.Not.Null);
            Assert.That(reserveText, Is.Not.Null);
            AssertTextFitsRect074(savedChoices,
                "saved founding choices at " + width + "x" + height);
            AssertTextFitsRect074(reserveText,
                "named founding reserve at " + width + "x" + height);

            var founderIds = coordinator.State.Applicants
                .Where(value => value != null)
                .Take(OpeningFlowState.RequiredOpeningRecruitCount)
                .Select(value => value.RecruitId)
                .ToArray();
            var founders = coordinator.State.Recruits
                .Where(value => value != null &&
                    founderIds.Contains(value.RecruitId, StringComparer.Ordinal))
                .ToArray();
            var unions = coordinator.State.Unions
                .Where(value => value != null)
                .Take(3)
                .ToArray();
            Assert.That(founders, Has.Length.EqualTo(6));
            Assert.That(unions, Has.Length.EqualTo(3));
            var leadPosition = Array.FindIndex(unions,
                value => value.Index == leadUnionIndex);
            Assert.That(leadPosition, Is.GreaterThanOrEqualTo(0));
            var groups = M1FlowPresenter.BuildFoundingRoleGroupsForVerification078(
                founders,
                leadRecruitId,
                leadPosition);
            var names = founders.ToDictionary(
                value => value.RecruitId,
                value => value.DisplayName,
                StringComparer.Ordinal);
            var charter = new FirstHourDirector071().PlayableRoster
                .Where(value => value.Wave == FirstHourRosterWave071.SkyhomeCharter)
                .Take(4)
                .ToArray();
            var reserve = charter.First(value =>
                value.DisplayName.StartsWith("Bessa ", StringComparison.OrdinalIgnoreCase));
            var fieldCharter = charter
                .Where(value => !ReferenceEquals(value, reserve))
                .Take(3)
                .ToArray();

            for (var index = 0; index < unions.Length; index++)
            {
                var union = unions[index];
                var card = GameObject.Find(
                    "Founding Confirmation Union " + union.Index + " 078")
                    ?.GetComponent<RectTransform>();
                var ribbonLabel = FindTextByNamePrefix076(
                    "Founding Confirmation Union Role " + union.Index + " 078");
                var copy = FindTextByNamePrefix076(
                    "Founding Confirmation Union Text " + union.Index + " 078");
                Assert.That(card, Is.Not.Null);
                Assert.That(ribbonLabel, Is.Not.Null);
                Assert.That(copy, Is.Not.Null);
                Assert.That(ribbonLabel.text,
                    Is.EqualTo(M1FlowPresenter.FoundingDestinationRibbonForVerification078(index)));
                foreach (var founderId in groups[index])
                    Assert.That(copy.text, Does.Contain(names[founderId].ToUpperInvariant()),
                        "Every founder must be named under the exact Union the save will use.");
                Assert.That(copy.text,
                    Does.Contain(fieldCharter[index].DisplayName.ToUpperInvariant()),
                    "Each Union must name its incoming charter ally before confirmation.");
                AssertTextFitsRect074(ribbonLabel,
                    ribbonLabel.name + " at " + width + "x" + height);
                AssertTextFitsRect074(copy,
                    copy.name + " at " + width + "x" + height);
                AssertRectInside074(
                    GameObject.Find("Founding Rescue Team Summary 078")
                        .GetComponent<RectTransform>(),
                    card,
                    "Founding Union preview at " + width + "x" + height);
            }

            Assert.That(reserveText.text,
                Does.Contain(reserve.DisplayName.ToUpperInvariant()),
                "The person staying at Hall must be named, not summarized as an anonymous reserve.");
        }

        private static Text FindResponsiveButtonLabel078(Button button)
        {
            return button?.GetComponentsInChildren<Text>(includeInactive: true)
                .FirstOrDefault(value => value != null &&
                    value.gameObject.name.StartsWith(
                        "Label",
                        StringComparison.Ordinal));
        }

        private static void ConfigureGuidedFieldCanvas076(
            float screenWidth,
            float screenHeight)
        {
            var canvas076 = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Single(value => value != null && StringComparer.Ordinal.Equals(
                    value.name,
                    "Outer Gateworks Exploration HUD 066"));
            var scaler076 = canvas076.GetComponent<CanvasScaler>();
            if (scaler076 != null) scaler076.enabled = false;
            canvas076.renderMode = RenderMode.WorldSpace;
            var canvasRect076 = canvas076.GetComponent<RectTransform>();
            canvasRect076.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                screenWidth);
            canvasRect076.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                screenHeight);
            canvasRect076.localScale = Vector3.one;
            Canvas.ForceUpdateCanvases();

            Assert.That(canvasRect076.rect.width, Is.EqualTo(screenWidth).Within(0.5f));
            Assert.That(canvasRect076.rect.height, Is.EqualTo(screenHeight).Within(0.5f));
            var objective076 = GameObject.Find("Gateworks Plain Objective 066")
                ?.GetComponent<Text>();
            var party076 = GameObject.Find("Gateworks Story So Far Text 073")
                ?.GetComponent<Text>();
            var deployed076 = GameObject.Find("Gateworks Deployed Party Strip 076")
                ?.GetComponent<Text>();
            var controls076 = GameObject.Find("Gateworks Controls 066")
                ?.GetComponent<Text>();
            Assert.That(objective076, Is.Not.Null);
            Assert.That(party076, Is.Not.Null);
            Assert.That(deployed076, Is.Not.Null);
            Assert.That(controls076, Is.Not.Null);
            Assert.That(party076.resizeTextMinSize, Is.GreaterThanOrEqualTo(22));
            Assert.That(deployed076.resizeTextMinSize, Is.GreaterThanOrEqualTo(18));
            Assert.That(controls076.fontSize, Is.GreaterThanOrEqualTo(22));
            AssertTextFitsRect074(objective076, "guided field objective");
            AssertTextFitsRect074(deployed076, "guided field named party strip");
            AssertTextFitsRect074(party076, "guided field story context");
            AssertTextFitsRect074(controls076, "guided field persistent controls");
        }

        private static float DriveGuidedFieldToObjective076(
            OuterGateworksExploration066 field076)
        {
            var largestStep076 = 0f;
            for (var step076 = 0;
                 step076 < 200 &&
                 !field076.IsWithinCurrentObjectiveInteractionRangeForVerification076;
                 step076++)
            {
                var previous076 = field076.ControlledAvatarForVerification076.position;
                field076.ApplyMovementInput066(
                    field076.CurrentObjectiveDirectionForVerification076,
                    false,
                    0.05f);
                var displacement076 =
                    field076.ControlledAvatarForVerification076.position - previous076;
                displacement076.y = 0f;
                largestStep076 = Mathf.Max(largestStep076, displacement076.magnitude);
            }
            return largestStep076;
        }

        private static float DriveWalkableArrivalToObjective076(
            WalkableSkyhomeArrival071 arrival076)
        {
            var largestStep076 = 0f;
            for (var step076 = 0;
                 step076 < 200 &&
                 !arrival076.IsWithinCurrentObjectiveInteractionRangeForVerification076;
                 step076++)
            {
                var previous076 = arrival076.ControlledAvatar071.position;
                arrival076.ApplyMovementForVerification071(
                    arrival076.CurrentObjectiveDirectionForVerification076,
                    true,
                    false,
                    0.05f);
                var displacement076 = arrival076.ControlledAvatar071.position - previous076;
                displacement076.y = 0f;
                largestStep076 = Mathf.Max(largestStep076, displacement076.magnitude);
            }
            return largestStep076;
        }

        private static void ConfigureExpeditionCanvas074(
            float screenWidth,
            float screenHeight)
        {
            var canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .FirstOrDefault(value => StringComparer.Ordinal.Equals(
                    value.name,
                    "M1 Playable Proof Canvas"));
            Assert.That(canvas, Is.Not.Null);
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null) scaler.enabled = false;
            canvas.renderMode = RenderMode.WorldSpace;

            var targetCanvasSize = M1FlowPresenter.ExpeditionCanvasSizeForVerification074(
                screenWidth,
                screenHeight);
            var canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetCanvasSize.x);
            canvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetCanvasSize.y);
            canvasRect.localScale = Vector3.one;
            Canvas.ForceUpdateCanvases();

            Assert.That(canvasRect.rect.width, Is.EqualTo(targetCanvasSize.x).Within(0.5f));
            Assert.That(canvasRect.rect.height, Is.EqualTo(targetCanvasSize.y).Within(0.5f));
            Assert.That(targetCanvasSize.x / targetCanvasSize.y,
                Is.EqualTo(screenWidth / screenHeight).Within(0.001f));
        }

        private static void AssertBoardQuestFiveSpaceLayout081()
        {
            var track = FindRectByPrefix074("Board Quest Face Down Rooms 081");
            Assert.That(track, Is.Not.Null,
                "The Board Quest must keep its single readable face-down room strip.");
            var tiles = UnityEngine.Object.FindObjectsByType<RectTransform>(
                    FindObjectsSortMode.None)
                .Where(value => value != null && value.gameObject.activeInHierarchy &&
                                value.name.StartsWith("Board Quest Space ",
                                    StringComparison.Ordinal) &&
                                !value.name.StartsWith("Board Quest Space Label ",
                                    StringComparison.Ordinal))
                .ToArray();
            var labels = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Where(value => value != null && value.gameObject.activeInHierarchy &&
                                value.name.StartsWith("Board Quest Space Label ",
                                    StringComparison.Ordinal))
                .ToArray();
            Assert.That(tiles, Has.Length.EqualTo(5));
            Assert.That(labels, Has.Length.EqualTo(5));
            Assert.That(CountNamedObjects("Expedition Route Phase Card "), Is.EqualTo(0));
            Assert.That(GameObject.Find("Expedition Board Overlay 074"), Is.Null);
            foreach (var tile in tiles)
                AssertRectInside074(track, tile, tile.name);
            foreach (var label in labels)
            {
                AssertRectInside074(track, label.rectTransform, label.name);
                Assert.That(label.text,
                    Does.Contain("COMPLETE").Or.Contain("YOU ARE HERE")
                        .Or.Contain("START HERE").Or.Contain("FACE DOWN")
                        .Or.Contain("CLEARED").Or.Contain("YOUR PAWN")
                        .Or.Contain("PLACE YOUR PAWN"));
                AssertTextFitsRect074(label, label.name);
            }
        }

        private static void AssertCleanFixedFiveBeatBoard076(
            string expectedChapterHeader,
            string expectedBackdropSprite,
            float screenWidth,
            float screenHeight,
            string expectedOperationHierarchy = null)
        {
            var root = FindRectByPrefix074("Board Quest 081");
            var canvas = FindRectByPrefix074("M1 Playable Proof Canvas");
            var story = FindRectByPrefix074("Board Quest Header 081");
            var chapterHeaders = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Where(value => value != null && value.gameObject.activeInHierarchy &&
                                value.name.StartsWith(
                                    "Board Quest Chapter 081",
                                    StringComparison.Ordinal))
                .ToArray();
            var objective = FindTextByNamePrefix076("Board Quest Goal 081");
            var operationHierarchy = FindTextByNamePrefix076(
                "Board Quest Operation Route 081");
            var backdrop = GameObject.Find("Expedition Illustrated Story Backdrop 076")
                ?.GetComponent<Image>();
            var back = FindButtonByName("Expedition Return To Hall 074");

            Assert.That(root, Is.Not.Null);
            Assert.That(canvas, Is.Not.Null);
            Assert.That(story, Is.Not.Null);
            Assert.That(chapterHeaders, Has.Length.EqualTo(1),
                "A fixed story board must expose exactly one chapter heading at " +
                screenWidth + "x" + screenHeight + ".");
            Assert.That(chapterHeaders[0].text, Is.EqualTo(expectedChapterHeader));
            Assert.That(objective, Is.Not.Null);
            Assert.That(operationHierarchy, Is.Null,
                "The phone-simple quest must not restore the old operation/route ledger" +
                (string.IsNullOrWhiteSpace(expectedOperationHierarchy)
                    ? "."
                    : ": " + expectedOperationHierarchy));
            Assert.That(backdrop, Is.Not.Null);
            Assert.That(backdrop.sprite, Is.Not.Null);
            Assert.That(backdrop.sprite.name, Does.StartWith(expectedBackdropSprite));
            Assert.That(back, Is.Not.Null);
            Assert.That(CountNamedObjects("Board Quest Space "), Is.EqualTo(10),
                "Five face-down room tiles and their five labels must be present.");
            Assert.That(CountNamedObjects("Board Quest Space Label "), Is.EqualTo(5));
            Assert.That(CountNamedObjects("Expedition Route Phase Card "), Is.EqualTo(0));

            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            LayoutRebuilder.ForceRebuildLayoutImmediate(story);
            Canvas.ForceUpdateCanvases();
            AssertRectInside074(canvas, root, "fixed story board viewport");
            AssertRectInside074(root, story, "fixed story chapter card");
            AssertRectInside074(story, chapterHeaders[0].rectTransform, "chapter heading");
            AssertRectInside074(story, objective.rectTransform, "chapter objective");
            AssertTextFitsRect074(chapterHeaders[0], "fixed story chapter heading");
            AssertTextFitsRect074(objective, "fixed story objective");

            var activeText = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Where(value => value != null && value.gameObject.activeInHierarchy)
                .ToArray();
            Assert.That(activeText.Count(value =>
                    (value.text ?? string.Empty).TrimStart().StartsWith(
                        "MISSION  •  ",
                        StringComparison.Ordinal)),
                Is.EqualTo(1),
                "Only the authored five-beat mission heading may identify the current quest.");
            foreach (var forbiddenText in new[]
                     {
                         "EXPEDITION BOARD",
                         "COMMITTED EXPEDITION BOARD",
                         "ROUTES, EVENTS, CAMP",
                         "LINES THAT DID NOT RETURN"
                     })
            {
                Assert.That(activeText.Any(value =>
                        (value.text ?? string.Empty).IndexOf(
                            forbiddenText,
                            StringComparison.OrdinalIgnoreCase) >= 0),
                    Is.False,
                    "Retired graph copy leaked into the fixed board: " + forbiddenText);
            }
            for (var node = 0; node <= 14; node++)
            {
                var token = "N" + node.ToString("00");
                Assert.That(activeText.Any(value =>
                        (value.text ?? string.Empty).IndexOf(
                            token,
                            StringComparison.Ordinal) >= 0),
                    Is.False,
                    "Retired node id " + token + " leaked into the fixed story UI.");
            }

            foreach (var forbiddenObjectPrefix in new[]
                     {
                         "Illustrated Expedition Route Board 074",
                         "Expedition Board Overlay 074",
                         "Expedition Map Heading 074",
                         "Expedition Map Node ",
                         "Route Connection ",
                         "First Expedition Illustrated Route Map 063",
                         "Playable Expedition Board Overlay 065",
                         "Expedition Board Node "
                     })
                Assert.That(CountNamedObjects(forbiddenObjectPrefix), Is.EqualTo(0),
                    "Retired graph object remains active: " + forbiddenObjectPrefix);

            Assert.That(UnityEngine.Object.FindObjectsByType<Image>(FindObjectsSortMode.None).Any(value =>
                    value != null && value.gameObject.activeInHierarchy &&
                    value.sprite != null &&
                    value.sprite.name.IndexOf(
                        "BOARD_SURVEY_017E",
                        StringComparison.OrdinalIgnoreCase) >= 0),
                Is.False,
                "The archival N00-N14 graph sprite must never render beneath fixed five-beat play.");

            var backLabel = back.GetComponentInChildren<Text>();
            Assert.That(backLabel, Is.Not.Null);
            Assert.That(backLabel.text, Is.EqualTo("←  GUILD HALL"));
            Assert.That(backLabel.enabled && backLabel.gameObject.activeInHierarchy, Is.True);
            Assert.That(backLabel.transform.GetSiblingIndex(),
                Is.EqualTo(back.transform.childCount - 1),
                "The Hall label must render above premium rails and frames.");
            Assert.That(backLabel.resizeTextMinSize, Is.GreaterThanOrEqualTo(24));
            AssertRectInside074(
                back.GetComponent<RectTransform>(),
                backLabel.rectTransform,
                "Guild Hall back label");
            AssertTextFitsRect074(backLabel, "Guild Hall back label");
            AssertButtonStateContrast076(back, "Guild Hall back label");
        }

        private static void AssertChapterTwoOpeningPresentation076(
            float screenWidth,
            float screenHeight)
        {
            var root = FindRectByPrefix074(M1FlowPresenter.ChapterTwoOpeningRootName076);
            var canvas = FindRectByPrefix074("M1 Playable Proof Canvas");
            var viewport = FindRectByPrefix074("Chapter Two Kiri Art Viewport 076");
            var artwork = FindRectByPrefix074("Chapter Two Kiri Wayglass Key Art 076");
            var header = FindRectByPrefix074("Chapter Two Opening Header 076");
            var brief = FindRectByPrefix074("Chapter Two Story Brief 076");
            var threshold = FindRectByPrefix074(
                "Chapter Two Route " + M1FlowPresenter.ChapterTwoThresholdAction078 + " 076");
            var hall = FindButtonByName("Chapter Two Return To Hall 076");
            var party = FindButtonByName("Chapter Two Review Unions 076");
            var save = FindRectByPrefix074("Chapter Two Save Consequence 076");

            Assert.That(root, Is.Not.Null);
            Assert.That(canvas, Is.Not.Null);
            Assert.That(viewport, Is.Not.Null);
            Assert.That(artwork, Is.Not.Null);
            Assert.That(header, Is.Not.Null);
            Assert.That(brief, Is.Not.Null);
            Assert.That(threshold, Is.Not.Null);
            Assert.That(hall, Is.Not.Null);
            Assert.That(party, Is.Not.Null);
            Assert.That(save, Is.Not.Null);

            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
            LayoutRebuilder.ForceRebuildLayoutImmediate(threshold);
            Canvas.ForceUpdateCanvases();
            AssertRectInside074(canvas, root, "Chapter 2 opening viewport");

            var fitter = artwork.GetComponent<AspectRatioFitter>();
            Assert.That(fitter, Is.Not.Null);
            Assert.That(fitter.aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent));
            Assert.That(artwork.pivot.y,
                Is.EqualTo(M1FlowPresenter.ChapterTwoKiriVerticalFocus076).Within(0.001f));
            var artImage = artwork.GetComponent<Image>();
            Assert.That(artImage, Is.Not.Null);
            Assert.That(artImage.sprite, Is.Not.Null);
            var artRect = WorldRect074(artwork);
            var viewportRect = WorldRect074(viewport);
            var topCropFraction = Mathf.Max(0f, artRect.yMax - viewportRect.yMax) /
                                  Mathf.Max(1f, artRect.height);
            Assert.That(topCropFraction, Is.LessThanOrEqualTo(0.055f),
                "Kiri's face/head band is cropped by " + topCropFraction.ToString("P1") +
                " at " + screenWidth + "x" + screenHeight + ".");

            var topLevelRegions = new[]
            {
                header,
                brief,
                threshold,
                hall.GetComponent<RectTransform>(),
                party.GetComponent<RectTransform>(),
                save
            };
            foreach (var region in topLevelRegions)
                AssertRectInside074(root, region, "Chapter 2 top-level region " + region.name);
            for (var left = 0; left < topLevelRegions.Length; left++)
            for (var right = left + 1; right < topLevelRegions.Length; right++)
                Assert.That(WorldRect074(topLevelRegions[left]).Overlaps(
                        WorldRect074(topLevelRegions[right])),
                    Is.False,
                    "Chapter 2 regions overlap at " + screenWidth + "x" + screenHeight +
                    ": " + topLevelRegions[left].name + " / " + topLevelRegions[right].name);

            AssertChapterTwoRouteCard076(
                threshold,
                M1FlowPresenter.ChapterTwoThresholdAction078,
                screenWidth,
                screenHeight);

            foreach (var prefix in new[]
                     {
                         "Chapter Two Opening Title 076",
                         "Chapter Two Opening Subtitle 076",
                         "Chapter Two Opening Operation Status 076",
                         "Chapter Two Scene Heading 076",
                         "Chapter Two Kiri Dialogue 076",
                         "Chapter Two Stakes 076",
                         "Chapter Two First Command Copy 076",
                         "Chapter Two Save Consequence Copy 076"
                     })
                AssertReadableChapterTwoText076(
                    FindTextByNamePrefix076(prefix),
                    prefix,
                    screenWidth,
                    screenHeight);

            var hallLabel = hall.GetComponentInChildren<Text>();
            Assert.That(hallLabel, Is.Not.Null);
            Assert.That(hallLabel.text, Is.EqualTo("←  GUILD HALL"));
            Assert.That(hallLabel.enabled && hallLabel.gameObject.activeInHierarchy, Is.True);
            Assert.That(hallLabel.transform.GetSiblingIndex(),
                Is.EqualTo(hall.transform.childCount - 1),
                "The Chapter 2 Hall label must remain above decorative rails.");
            AssertRectInside074(
                hall.GetComponent<RectTransform>(),
                hallLabel.rectTransform,
                "Chapter 2 Hall label");
            AssertReadableChapterTwoText076(
                hallLabel,
                "Chapter 2 Hall label",
                screenWidth,
                screenHeight);
            AssertButtonStateContrast076(hall, "Chapter 2 Hall label");

            AssertReadableChapterTwoText076(
                party.GetComponentInChildren<Text>(),
                "Chapter 2 Union-plan label",
                screenWidth,
                screenHeight);
            AssertGoldFocusContrast076(
                FindButton(M1FlowPresenter.ChapterTwoThresholdAction078 + "  →"),
                "Chapter 2 Wayglass threshold");
        }

        private static void AssertChapterTwoRouteCard076(
            RectTransform card,
            string actionLabel,
            float screenWidth,
            float screenHeight)
        {
            var heading = FindRectByPrefix074(
                "Chapter Two Route Heading " + actionLabel + " 076");
            var story = FindRectByPrefix074(
                "Chapter Two Route Story " + actionLabel + " 076");
            var test = FindRectByPrefix074(
                "Chapter Two Route Test " + actionLabel + " 076");
            var risk = FindRectByPrefix074(
                "Chapter Two Route Risk " + actionLabel + " 076");
            var action = FindButtonByName("Chapter Two Route Action " + actionLabel + " 076")
                ?.GetComponent<RectTransform>();
            Assert.That(heading, Is.Not.Null);
            Assert.That(story, Is.Not.Null);
            Assert.That(test, Is.Not.Null);
            Assert.That(risk, Is.Not.Null);
            Assert.That(action, Is.Not.Null);

            foreach (var region in new[] { heading, story, test, risk, action })
                AssertRectInside074(card, region, actionLabel + " / " + region.name);
            AssertRectAbove074(heading, story, actionLabel + " heading/story");
            AssertRectAbove074(story, test, actionLabel + " story/test");
            AssertRectAbove074(story, risk, actionLabel + " story/risk");
            AssertRectAbove074(test, action, actionLabel + " test/action");
            AssertRectAbove074(risk, action, actionLabel + " risk/action");
            Assert.That(WorldRect074(test).xMax,
                Is.LessThanOrEqualTo(WorldRect074(risk).xMin + 0.5f),
                actionLabel + " test/risk columns overlap.");

            var maximumGap = WorldRect074(card).height * 0.031f;
            AssertVerticalGapAtMost076(heading, story, maximumGap, actionLabel + " heading/story");
            AssertVerticalGapAtMost076(story, test, maximumGap, actionLabel + " story/test");
            AssertVerticalGapAtMost076(test, action, maximumGap, actionLabel + " test/action");
            foreach (var text in new[]
                     {
                         heading.GetComponent<Text>(),
                         story.GetComponent<Text>(),
                         test.GetComponent<Text>(),
                         risk.GetComponent<Text>(),
                         action.GetComponentInChildren<Text>()
                     })
                AssertReadableChapterTwoText076(
                    text,
                    actionLabel + " / " + text.name,
                    screenWidth,
                    screenHeight);
        }

        private static void AssertVerticalGapAtMost076(
            RectTransform upper,
            RectTransform lower,
            float maximumGap,
            string label)
        {
            var gap = WorldRect074(upper).yMin - WorldRect074(lower).yMax;
            Assert.That(gap, Is.InRange(-0.5f, maximumGap + 0.5f),
                label + " leaves " + gap + " unused layout units.");
        }

        private static void AssertReadableChapterTwoText076(
            Text text,
            string label,
            float screenWidth,
            float screenHeight)
        {
            Assert.That(text, Is.Not.Null, label);
            Assert.That(text.resizeTextForBestFit, Is.True, label);
            Assert.That(text.resizeTextMinSize, Is.GreaterThanOrEqualTo(28),
                label + " fell below the authored readability floor.");
            var canvasSize = M1FlowPresenter.ExpeditionCanvasSizeForVerification074(
                screenWidth,
                screenHeight);
            var productionScale = screenWidth / Mathf.Max(1f, canvasSize.x);
            Assert.That(text.resizeTextMinSize * productionScale,
                Is.GreaterThanOrEqualTo(14.5f),
                label + " can render below 14.5 physical pixels at " +
                screenWidth + "x" + screenHeight + ".");
            AssertTextFitsRect074(text, label);
        }

        private static void AssertButtonStateContrast076(Button button, string label)
        {
            Assert.That(button, Is.Not.Null, label);
            var text = button.GetComponentInChildren<Text>();
            Assert.That(text, Is.Not.Null, label);
            foreach (var background in new[]
                     {
                         button.colors.normalColor,
                         button.colors.highlightedColor,
                         button.colors.selectedColor,
                         button.colors.pressedColor
                     })
                Assert.That(ContrastRatio076(text.color, background),
                    Is.GreaterThanOrEqualTo(4.5f),
                    label + " falls below 4.5:1 in one of its interactive states.");
        }

        private static void AssertGoldFocusContrast076(Button button, string label)
        {
            AssertButtonStateContrast076(button, label);
            var text = button.GetComponentInChildren<Text>();
            Assert.That(RelativeLuminance076(text.color), Is.LessThan(0.05f),
                label + " must use dark ink on the warm selected surface.");
            Assert.That(RelativeLuminance076(button.colors.selectedColor),
                Is.GreaterThan(0.35f),
                label + " must keep controller focus on a warm gold surface.");
        }

        private static void AssertFieldDecisionGeometry078(
            float screenWidth,
            float screenHeight,
            string expectedFit,
            string expectedCompanionVoice)
        {
            var context = FindRectByPrefix074("Expedition Context Action Area 074");
            var moment = FindRectByPrefix074("Board Quest Scene 081");
            var decisionHeading = FindRectByPrefix074("Expedition Field Command Heading 074");
            var leadHeading = FindRectByPrefix074("Expedition Check Lead Heading 074");
            var leadRow = FindRectByPrefix074("Expedition Check Lead Choices 074");
            var approachHeading = FindRectByPrefix074("Expedition Check Approach Heading 074");
            var approaches = FindRectByPrefix074("Expedition Check Approach Choices 074");
            var primary = FindButtonByName("Expedition Primary Context Action 074");
            var companionVoice = FindTextByNamePrefix076(
                "Expedition Companion Story Beat Text 076");

            Assert.That(context, Is.Not.Null);
            Assert.That(moment, Is.Not.Null);
            Assert.That(decisionHeading, Is.Not.Null);
            Assert.That(leadHeading, Is.Not.Null);
            Assert.That(leadRow, Is.Not.Null);
            Assert.That(approachHeading, Is.Not.Null);
            Assert.That(approaches, Is.Not.Null);
            Assert.That(primary, Is.Not.Null);
            Assert.That(companionVoice, Is.Not.Null);
            LayoutRebuilder.ForceRebuildLayoutImmediate(context);
            LayoutRebuilder.ForceRebuildLayoutImmediate(moment);
            Canvas.ForceUpdateCanvases();

            var frame = screenWidth + "x" + screenHeight;
            AssertRectAbove074(decisionHeading, leadHeading,
                "decision heading and field-lead heading at " + frame);
            AssertRectAbove074(leadHeading, leadRow,
                "field-lead heading and cards at " + frame);
            AssertRectAbove074(leadRow, approachHeading,
                "field-lead cards and approach heading at " + frame);
            AssertRectAbove074(approachHeading, approaches,
                "approach heading and partner cards at " + frame);
            AssertRectAbove074(approaches, primary.GetComponent<RectTransform>(),
                "partner cards and primary action at " + frame);
            foreach (var rect in new[]
                     {
                         decisionHeading, leadHeading, leadRow, approachHeading, approaches,
                         primary.GetComponent<RectTransform>()
                     })
                AssertRectInside074(context, rect, rect.name + " at " + frame);

            var leadHeadingText = leadHeading.GetComponent<Text>();
            var approachHeadingText = approachHeading.GetComponent<Text>();
            Assert.That(expectedFit, Is.Not.Null.And.Not.Empty,
                "A field-decision fixture must name its authored skill fit.");
            Assert.That(expectedCompanionVoice, Is.Not.Null.And.Not.Empty,
                "A field-decision fixture must name its authored companion voice.");
            Assert.That(leadHeadingText.text,
                Is.EqualTo(M1FlowPresenter.ExpeditionFieldLeadLabel078 + "  •  FIT: " + expectedFit));
            Assert.That(approachHeadingText.text, Is.EqualTo("CHOOSE THE ORDER"));
            AssertTextFitsRect074(leadHeadingText, "field-lead heading at " + frame);
            AssertTextFitsRect074(approachHeadingText, "approach heading at " + frame);

            var leadButtons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(value => value != null && value.gameObject.activeInHierarchy &&
                                value.name.StartsWith(
                                    "Expedition Check Lead ",
                                    StringComparison.Ordinal))
                .ToArray();
            Assert.That(leadButtons, Has.Length.EqualTo(3));
            Assert.That(leadButtons.Count(value => value.GetComponentInChildren<Text>().text.Contains(
                    M1FlowPresenter.ExpeditionTopFitLabel078)),
                Is.EqualTo(1));
            foreach (var leadButton in leadButtons)
            {
                var label = leadButton.GetComponentInChildren<Text>();
                Assert.That(label.text, Does.Not.Contain("RECOMMENDED"));
                AssertTextFitsRect074(label, leadButton.name + " at " + frame);
            }

            var careful = FindButtonByName("Expedition Check Approach 0 074");
            var swift = FindButtonByName("Expedition Check Approach 1 074");
            var carefulText = careful.GetComponentInChildren<Text>().text;
            var partnerPrefix = M1FlowPresenter.ExpeditionFieldPartnerLabel078 + "  •  ";
            var partnerLines = carefulText
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(value => value.StartsWith(partnerPrefix, StringComparison.Ordinal))
                .ToArray();
            Assert.That(partnerLines, Has.Length.EqualTo(1),
                "The careful order must expose exactly one explicit FIELD PARTNER assignment line.");
            var assignedPartner = partnerLines[0].Substring(partnerPrefix.Length).Trim();
            Assert.That(assignedPartner, Is.Not.Empty,
                "The FIELD PARTNER assignment must name a person.");
            Assert.That(
                StringComparer.OrdinalIgnoreCase.Equals(assignedPartner, expectedCompanionVoice),
                Is.False,
                "The assigned field partner must be independent of the authored companion story voice.");
            AssertTextFitsRect074(careful.GetComponentInChildren<Text>(),
                "field-partner card at " + frame);
            AssertTextFitsRect074(swift.GetComponentInChildren<Text>(),
                "field-lead-only card at " + frame);
            AssertTextFitsRect074(primary.GetComponentInChildren<Text>(),
                "primary field order at " + frame);
            Assert.That(companionVoice.text,
                Does.StartWith(M1FlowPresenter.ExpeditionCompanionVoiceLabel078 + "  •  " +
                               expectedCompanionVoice),
                expectedCompanionVoice +
                " is speaking story context; that person is not implicitly the selected lead or partner.");
            AssertTextFitsRect074(companionVoice, "companion voice at " + frame);
            AssertFocusedButtonByName076("Expedition Primary Context Action 074");
        }

        private static Button FindVisibleButtonStartingWith081(string prefix)
        {
            return UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(value => value != null && value.gameObject.activeInHierarchy &&
                                         (value.GetComponentInChildren<Text>()?.text ?? string.Empty)
                                         .StartsWith(prefix, StringComparison.Ordinal));
        }

        private static void AssertFocusedFaceDownRoute081(params string[] nodeIds)
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            Assert.That(selected, Is.Not.Null,
                "A face-down Board Quest route must receive controller focus.");
            var expectedNames = (nodeIds ?? Array.Empty<string>())
                .Select(value => "Select Expedition Destination " + value + " 074")
                .ToArray();
            Assert.That(expectedNames, Does.Contain(selected.name),
                "Focus must land on one of the shuffled face-down route moves.");
            Assert.That(selected.GetComponent<Button>()?.interactable, Is.True);
        }

        private static void AssertBoardQuestDiceDecisionGeometry081(
            float screenWidth,
            float screenHeight,
            string expectedCompanionVoice,
            bool expectTeamUp)
        {
            var context = FindRectByPrefix074("Expedition Context Action Area 074");
            var panel = FindRectByPrefix074("Board Quest Action Panel 081");
            var heading = FindRectByPrefix074("Expedition Field Command Heading 074");
            var prompt = FindRectByPrefix074("Board Quest Plain Prompt 081");
            var lead = FindRectByPrefix074("Expedition Check Lead Heading 074");
            var primary = FindButtonByName("Expedition Primary Context Action 074");
            var team = FindVisibleButtonStartingWith081("TEAM UP\n");
            var fast = FindVisibleButtonStartingWith081("MOVE FAST\n");
            var companionVoice = FindTextByNamePrefix076(
                "Expedition Companion Story Beat Text 076");
            var frame = screenWidth + "x" + screenHeight;

            Assert.That(context, Is.Not.Null);
            Assert.That(panel, Is.Not.Null);
            Assert.That(heading, Is.Not.Null);
            Assert.That(prompt, Is.Not.Null);
            Assert.That(lead, Is.Not.Null);
            Assert.That(primary, Is.Not.Null);
            Assert.That(team, Is.Null,
                "The phone-simple room must not expose a Team Up approach menu.");
            Assert.That(fast, Is.Null,
                "The phone-simple room must not expose a Move Fast approach menu.");
            Assert.That(companionVoice, Is.Not.Null);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            Canvas.ForceUpdateCanvases();

            AssertRectInside074(context, panel, "dice action panel at " + frame);
            foreach (var rect in new[]
                     {
                         heading, prompt, lead, primary.GetComponent<RectTransform>()
                     })
                AssertRectInside074(panel, rect, rect.name + " at " + frame);
            AssertRectAbove074(heading, prompt, "move heading / dice rules at " + frame);
            AssertRectAbove074(prompt, lead, "dice rules / field lead at " + frame);
            AssertRectAbove074(lead, primary.GetComponent<RectTransform>(),
                "field lead / automatic dice at " + frame);
            Assert.That(primary.interactable, Is.False,
                "The physical dice must auto-resolve once without a reroll button.");
            Assert.That(primary.GetComponentInChildren<Text>().text,
                Does.StartWith("ROLLING 2D6").And.Contain("WATCH THE DICE"));
            Assert.That(lead.GetComponent<Text>().text,
                expectTeamUp
                    ? Does.Contain(" + ").And.Not.Contain(" GOES FAST")
                    : Does.Contain(" GOES FAST").And.Not.Contain(" + "));
            Assert.That(companionVoice.text,
                Does.StartWith("GUILDMATE  •  " + expectedCompanionVoice),
                "The story speaker must remain independent of the automatic field lead.");
            AssertTextFitsRect074(prompt.GetComponentInChildren<Text>(),
                "dice rules at " + frame);
            AssertTextFitsRect074(lead.GetComponent<Text>(), "field lead at " + frame);
            AssertTextFitsRect074(primary.GetComponentInChildren<Text>(),
                "automatic dice action at " + frame);
        }

        private static string ExpectedFieldFit078(IReadOnlyList<string> eligibleSkills)
        {
            var expected = string.Join(
                    " / ",
                    (eligibleSkills ?? Array.Empty<string>())
                    .Where(value => !string.IsNullOrWhiteSpace(value)))
                .ToUpperInvariant();
            Assert.That(expected, Is.Not.Empty,
                "A field-decision fixture must carry authored eligible skills, not a blank generic fit.");
            Assert.That(expected,
                Does.Not.Contain("UNKNOWN").And.Not.Contain("GENERIC"),
                "The field-lead heading must expose the event's authored skill fit.");
            return expected;
        }

        private static void AssertNextOrderGeometry074(string noticeTitle)
        {
            var context = FindRectByPrefix074("Expedition Context Action Area 074");
            var moment = FindRectByPrefix074("Board Quest Scene 081");
            Assert.That(context, Is.Not.Null);
            Assert.That(moment, Is.Not.Null);
            LayoutRebuilder.ForceRebuildLayoutImmediate(context);
            LayoutRebuilder.ForceRebuildLayoutImmediate(moment);
            Canvas.ForceUpdateCanvases();

            var decisionHeading = FindRectByPrefix074("Expedition Field Command Heading 074");
            var sceneTitle = FindRectByPrefix074("Board Quest Scene Title 081");
            var sceneSummary = FindRectByPrefix074("Expedition Current Position Summary 074");
            var revealedReward = FindRectByPrefix074("Board Quest Revealed Reward 081");
            var companion = FindRectByPrefix074("Expedition Companion Story Beat 076");
            var notice = FindRectByPrefix074("Expedition Context Notice " + noticeTitle + " 074");
            var noticeHeading = FindRectByPrefix074("Expedition Context Notice Title 074");
            var noticeCopy = FindRectByPrefix074("Expedition Context Notice Copy 074");
            var action = FindButtonByName("Expedition Primary Context Action 074")
                ?.GetComponent<RectTransform>();

            Assert.That(decisionHeading, Is.Not.Null);
            Assert.That(sceneTitle, Is.Not.Null);
            Assert.That(sceneSummary, Is.Not.Null);
            Assert.That(revealedReward, Is.Not.Null);
            Assert.That(companion, Is.Not.Null);
            Assert.That(notice, Is.Not.Null);
            Assert.That(noticeHeading, Is.Not.Null);
            Assert.That(noticeCopy, Is.Not.Null);
            Assert.That(action, Is.Not.Null);
            Assert.That(notice.rect.height, Is.GreaterThanOrEqualTo(260f));
            Assert.That(noticeCopy.rect.height, Is.GreaterThanOrEqualTo(190f));

            var momentRect = WorldRect074(moment);
            var contextRect = WorldRect074(context);
            Assert.That(momentRect.xMax, Is.LessThanOrEqualTo(contextRect.xMin + 0.5f),
                "The story moment and next decision overlap at 1280x800: moment=" +
                momentRect + ", context=" + contextRect);
            AssertRectAbove074(sceneTitle, sceneSummary, "room title and current story");
            AssertRectAbove074(sceneSummary, revealedReward, "current story and room reward");
            AssertRectAbove074(revealedReward, companion, "room reward and guildmate story");
            AssertRectAbove074(decisionHeading, notice, "decision heading and " + noticeTitle);
            AssertRectAbove074(noticeHeading, noticeCopy, noticeTitle + " heading/body");
            AssertRectAbove074(notice, action, noticeTitle + " and primary action");
            AssertRectInside074(moment, sceneTitle, "room title");
            AssertRectInside074(moment, sceneSummary, "current story");
            AssertRectInside074(moment, revealedReward, "revealed reward");
            AssertRectInside074(moment, companion, "guildmate story");
            AssertRectInside074(context, decisionHeading, "decision heading");
            AssertRectInside074(context, notice, noticeTitle);
            AssertRectInside074(context, action, "primary action");
            AssertTextFitsRect074(sceneTitle.GetComponent<Text>(), "room title");
            AssertTextFitsRect074(sceneSummary.GetComponent<Text>(), "current story");
            AssertTextFitsRect074(noticeCopy.GetComponent<Text>(), noticeTitle + " body");
        }

        private static void AssertBoardQuestRouteChoiceGeometry081(
            float screenWidth,
            float screenHeight,
            bool expectSingleRequiredRoute)
        {
            var root = FindRectByPrefix074("Board Quest 081");
            var canvasRoot = FindRectByPrefix074("M1 Playable Proof Canvas");
            var screenRoot = FindRectByPrefix074("M1 Screen Root");
            var header = FindRectByPrefix074("Board Quest Header 081");
            var track = FindRectByPrefix074("Board Quest Face Down Rooms 081");
            var scene = FindRectByPrefix074("Board Quest Scene 081");
            var context = FindRectByPrefix074("Expedition Context Action Area 074");
            var actionPanel = FindRectByPrefix074("Board Quest Action Panel 081");
            var decisionHeading = FindRectByPrefix074("Expedition Field Command Heading 074");
            var prompt = FindRectByPrefix074("Board Quest Plain Prompt 081");
            var actionButton = FindButtonByName("Expedition Primary Context Action 074");
            var action = actionButton?.GetComponent<RectTransform>();
            var frame = screenWidth + "x" + screenHeight;

            foreach (var rect in new[]
                     {
                         root, canvasRoot, screenRoot, header, track, scene, context,
                         actionPanel, decisionHeading, prompt, action
                     })
                Assert.That(rect, Is.Not.Null, "Missing Board Quest region at " + frame + ".");

            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            LayoutRebuilder.ForceRebuildLayoutImmediate(actionPanel);
            Canvas.ForceUpdateCanvases();
            AssertRectInside074(screenRoot, root, "zero-offset Board Quest viewport");
            AssertRectInside074(canvasRoot, root, "Board Quest inside the " + frame + " canvas");
            AssertRectInside074(root, header, "Board Quest header at " + frame);
            AssertRectInside074(root, track, "face-down room strip at " + frame);
            AssertRectInside074(root, scene, "revealed story room at " + frame);
            AssertRectInside074(root, context, "one-action context at " + frame);
            AssertRectInside074(context, actionPanel, "Board Quest action panel at " + frame);
            AssertRectInside074(actionPanel, decisionHeading, "field command heading at " + frame);
            AssertRectInside074(actionPanel, prompt, "face-down move prompt at " + frame);
            AssertRectInside074(actionPanel, action, "one-tap room action at " + frame);
            AssertRectAbove074(header, track, "story header / room strip at " + frame);
            AssertRectAbove074(track, scene, "room strip / revealed room at " + frame);
            AssertRectAbove074(track, context, "room strip / move controls at " + frame);
            Assert.That(WorldRect074(scene).xMax,
                Is.LessThanOrEqualTo(WorldRect074(context).xMin + 0.5f),
                "Revealed room and direct move controls overlap at " + frame + ".");

            AssertBoardQuestFiveSpaceLayout081();
            var routeButtons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(value => value != null && value.gameObject.activeInHierarchy &&
                                value.name.StartsWith(
                                    "Select Expedition Destination ",
                                    StringComparison.Ordinal))
                .OrderBy(value => value.name, StringComparer.Ordinal)
                .ToArray();
            Assert.That(routeButtons,
                Has.Length.Zero,
                "Phone-simple play must never expose authored node choices or a route graph at " + frame + ".");
            var actionLabel = actionButton.GetComponentInChildren<Text>();
            Assert.That(actionButton.interactable, Is.True,
                (expectSingleRequiredRoute ? "Required" : "Shuffled") +
                " routes must both remain one legal tap.");
            Assert.That(actionLabel, Is.Not.Null);
            Assert.That(actionLabel.text, Is.EqualTo("MOVE FORWARD\nFLIP NEXT ROOM"));
            Assert.That(actionLabel.text,
                Does.Not.Contain("LEFT").And.Not.Contain("RIGHT")
                    .And.Not.Match(@"(^|\s)N\d{2}($|\s)"));
            AssertTextFitsRect074(actionLabel, "one-tap room action at " + frame);
            Assert.That(EventSystem.current?.currentSelectedGameObject,
                Is.SameAs(actionButton.gameObject),
                "Controller focus must land on the only forward action at " + frame + ".");
        }

        private static void AssertBoardQuestSceneGeometry081(
            float screenWidth,
            float screenHeight,
            bool expectIdentityChip)
        {
            var scene = FindRectByPrefix074("Board Quest Scene 081");
            var title = FindRectByPrefix074("Board Quest Scene Title 081");
            var summary = FindRectByPrefix074("Expedition Current Position Summary 074");
            var reveal = FindRectByPrefix074("Board Quest Revealed Reward 081");
            var companion = FindRectByPrefix074("Expedition Companion Story Beat 076");
            var companionText = FindRectByPrefix074("Expedition Companion Story Beat Text 076");
            var immediate = FindRectByPrefix074("Board Quest Immediate Promise 081");
            var diceResult = FindRectByPrefix074("Board Quest Dice Result 081");
            var finalBeat = diceResult ?? immediate;
            var identity = FindRectByPrefix074("Expedition Companion Identity Chip 078");
            var frame = screenWidth + "x" + screenHeight;

            foreach (var rect in new[]
                     {
                         scene, title, summary, reveal, companion, companionText, finalBeat
                     })
                Assert.That(rect, Is.Not.Null, "Missing revealed-room story region at " + frame + ".");
            LayoutRebuilder.ForceRebuildLayoutImmediate(scene);
            Canvas.ForceUpdateCanvases();
            AssertRectAbove074(title, summary, "room title / current story at " + frame);
            AssertRectAbove074(summary, reveal, "current story / revealed reward at " + frame);
            AssertRectAbove074(reveal, companion, "revealed reward / companion voice at " + frame);
            AssertRectAbove074(companion, finalBeat,
                "companion voice / immediate resolution at " + frame);
            foreach (var rect in new[]
                     {
                         title, summary, reveal, companion, companionText, finalBeat
                     })
                AssertRectInside074(scene, rect, rect.name + " at " + frame);
            AssertRectInside074(companion, companionText, "companion voice at " + frame);
            AssertTextFitsRect074(title.GetComponent<Text>(), "room title at " + frame);
            AssertTextFitsRect074(summary.GetComponent<Text>(), "current story at " + frame);
            AssertTextFitsRect074(companionText.GetComponent<Text>(),
                "companion voice at " + frame);
            if (immediate != null)
                AssertTextFitsRect074(immediate.GetComponent<Text>(),
                    "immediate room promise at " + frame);
            Assert.That(identity != null, Is.EqualTo(expectIdentityChip));
            if (identity == null) return;
            AssertRectInside074(companion, identity, "companion portrait chip at " + frame);
            Assert.That(WorldRect074(identity).xMax,
                Is.LessThanOrEqualTo(WorldRect074(companionText).xMin + 0.5f),
                "Companion portrait chip overlaps its story voice at " + frame + ".");
            var identityButton = identity.GetComponent<Button>();
            Assert.That(identityButton, Is.Not.Null);
            Assert.That(identityButton.interactable, Is.False,
                "The identity chip must not compete with the one Board Quest action.");
            Assert.That(identity.GetComponentsInChildren<Image>(true).Any(value =>
                    value != null && value.gameObject.name.StartsWith(
                        "Portrait Frame ",
                        StringComparison.Ordinal)),
                Is.True,
                "The identity chip must reuse the authored recruit portrait pipeline.");
        }

        private static void AssertBoardQuestCommittedDiceGeometry081(
            float screenWidth,
            float screenHeight)
        {
            var scene = FindRectByPrefix074("Board Quest Scene 081");
            var companion = FindRectByPrefix074("Expedition Companion Story Beat 076");
            var result = FindRectByPrefix074("Board Quest Dice Result 081");
            var numbers = FindRectByPrefix074("Board Quest Dice Numbers 081");
            var physicalDice = FindRectByPrefix074("Authoritative Dice Roll 084");
            var outcome = FindRectByPrefix074("Board Quest Dice Outcome 081");
            var reward = FindRectByPrefix074("Board Quest Dice Reward 081");
            var frame = screenWidth + "x" + screenHeight;

            foreach (var rect in new[]
                     { scene, companion, result, numbers, physicalDice, outcome, reward })
                Assert.That(rect, Is.Not.Null,
                    "Missing committed dice-result region at " + frame + ".");
            LayoutRebuilder.ForceRebuildLayoutImmediate(scene);
            LayoutRebuilder.ForceRebuildLayoutImmediate(result);
            Canvas.ForceUpdateCanvases();
            AssertRectAbove074(companion, result,
                "companion story / committed dice result at " + frame);
            AssertRectInside074(scene, result, "committed dice result at " + frame);
            AssertRectInside074(result, numbers, "committed dice numbers at " + frame);
            AssertRectInside074(result, physicalDice, "physical committed dice at " + frame);
            AssertRectInside074(result, outcome, "committed dice outcome at " + frame);
            AssertRectInside074(result, reward, "committed dice reward at " + frame);
            AssertRectAbove074(numbers, physicalDice, "dice heading / physical dice at " + frame);
            AssertRectAbove074(physicalDice, outcome, "physical dice / outcome at " + frame);
            AssertRectAbove074(outcome, reward, "dice outcome / reward at " + frame);
            AssertTextFitsRect074(numbers.GetComponent<Text>(),
                "committed dice numbers at " + frame);
            AssertTextFitsRect074(outcome.GetComponent<Text>(),
                "committed dice outcome at " + frame);
            AssertTextFitsRect074(reward.GetComponent<Text>(),
                "committed dice reward at " + frame);
        }

        private static void AssertRouteChoiceGeometry076(
            float screenWidth,
            float screenHeight,
            bool expectSingleRequiredRoute,
            bool expectSelectedSummary)
        {
            var root = FindRectByPrefix074("Full Screen Expedition Board 074");
            var canvasRoot = FindRectByPrefix074("M1 Playable Proof Canvas");
            var screenRoot = FindRectByPrefix074("M1 Screen Root");
            var context = FindRectByPrefix074("Expedition Context Action Area 074");
            var moment = FindRectByPrefix074("Expedition Journey Overview 074");
            var consequence = FindRectByPrefix074("Expedition Progress Consequence 076");
            var decisionHeading = FindRectByPrefix074("Expedition Field Command Heading 074");
            var routeHeading = FindRectByPrefix074("Expedition Route Choice Heading 074");
            var routeRow = FindRectByPrefix074("Expedition Route Choice Buttons 074");
            var summary = FindRectByPrefix074("Selected Expedition Destination Summary 074");
            var actionButton = FindButtonByName("Expedition Primary Context Action 074");
            var action = actionButton?.GetComponent<RectTransform>();
            var frame = screenWidth + "x" + screenHeight;

            Assert.That(root, Is.Not.Null);
            Assert.That(canvasRoot, Is.Not.Null);
            Assert.That(screenRoot, Is.Not.Null);
            Assert.That(context, Is.Not.Null);
            Assert.That(moment, Is.Not.Null);
            Assert.That(consequence, Is.Not.Null);
            Assert.That(decisionHeading, Is.Not.Null);
            Assert.That(routeHeading, Is.Not.Null);
            Assert.That(routeRow, Is.Not.Null);
            Assert.That(summary != null, Is.EqualTo(expectSelectedSummary),
                "Selected-route detail did not match the state at " + frame + ".");
            Assert.That(action, Is.Not.Null);
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            LayoutRebuilder.ForceRebuildLayoutImmediate(context);
            Canvas.ForceUpdateCanvases();

            AssertRectInside074(screenRoot, root, "zero-offset expedition viewport");
            AssertRectInside074(canvasRoot, root, "expedition viewport inside the " + frame + " canvas");
            Assert.That(root.anchoredPosition.sqrMagnitude, Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(root.localScale, Vector3.one), Is.LessThan(0.001f));
            AssertRectAbove074(decisionHeading, routeHeading,
                "decision heading / route heading at " + frame);
            AssertRectAbove074(routeHeading, routeRow,
                "route heading / destination cards at " + frame);
            if (summary != null)
            {
                AssertRectAbove074(routeRow, summary,
                    "destination cards / arrival forecast at " + frame);
                AssertRectAbove074(summary, action,
                    "arrival forecast / travel action at " + frame);
                AssertRectInside074(context, summary, "selected route forecast at " + frame);
            }
            else
            {
                AssertRectAbove074(routeRow, action,
                    "destination cards / select-a-route prompt at " + frame);
            }
            AssertRectInside074(context, decisionHeading, "decision heading at " + frame);
            AssertRectInside074(context, routeHeading, "route heading at " + frame);
            AssertRectInside074(context, routeRow, "route cards at " + frame);
            AssertRectInside074(context, action, "travel action");
            AssertRectInside074(moment, consequence, "expedition progress consequence");
            Assert.That(WorldRect074(moment).xMax,
                Is.LessThanOrEqualTo(WorldRect074(context).xMin + 0.5f),
                "Current beat and route decision overlap at " + frame + ".");

            var decisionHeadingText = decisionHeading.GetComponent<Text>();
            var routeHeadingText = routeHeading.GetComponent<Text>();
            Assert.That(decisionHeadingText.text,
                Is.EqualTo(expectSingleRequiredRoute ? "YOUR NEXT ORDER" : "YOUR NEXT DECISION"));
            Assert.That(routeHeadingText.text,
                Is.EqualTo(expectSingleRequiredRoute ? "STORY ROUTE  •  REQUIRED" : "CHOOSE A ROUTE"));
            AssertReadableChapterTwoText076(
                decisionHeadingText,
                "route decision heading at " + frame,
                screenWidth,
                screenHeight);
            AssertReadableChapterTwoText076(
                routeHeadingText,
                "route choice heading at " + frame,
                screenWidth,
                screenHeight);

            var routeButtons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(value => value != null && value.gameObject.activeInHierarchy &&
                                value.name.StartsWith(
                                    "Select Expedition Destination ",
                                    StringComparison.Ordinal))
                .ToArray();
            Assert.That(routeButtons, Has.Length.EqualTo(expectSingleRequiredRoute ? 1 : 2));
            foreach (var routeButton in routeButtons)
            {
                var label = routeButton.GetComponentInChildren<Text>();
                Assert.That(label.text,
                    Does.Not.Contain("SUPPLIES").And.Not.Contain("FATIGUE").And.Not.Contain("URGENCY"),
                    "Route cards must identify choices; the selected detail owns resource forecast at " + frame + ".");
                AssertReadableChapterTwoText076(
                    label,
                    routeButton.name + " at " + frame,
                    screenWidth,
                    screenHeight);
                Assert.That(label.verticalOverflow, Is.EqualTo(VerticalWrapMode.Truncate));
            }
            if (expectSingleRequiredRoute)
                Assert.That(routeButtons[0].GetComponentInChildren<Text>().text,
                    Does.Contain("STORY ROUTE  •  REQUIRED"));

            if (summary != null)
            {
                var summaryText = summary.GetComponent<Text>();
                Assert.That(summaryText, Is.Not.Null);
                Assert.That(summaryText.verticalOverflow, Is.EqualTo(VerticalWrapMode.Truncate));
                AssertReadableChapterTwoText076(
                    summaryText,
                    "selected route forecast at " + frame,
                    screenWidth,
                    screenHeight);
                Assert.That(summaryText.text, Does.StartWith("ARRIVAL FORECAST"));
                Assert.That(summaryText.text, Does.Contain("SUPPLIES −1"));
                Assert.That(summaryText.text, Does.Contain("FATIGUE +1"));
                Assert.That(summaryText.text, Does.Contain("PRESSURE −1"));
                Assert.That(summaryText.text, Does.Contain("\nNEXT  •"));
                Assert.That(summaryText.text, Does.Contain("AUTOSAVE ON ARRIVAL"));
            }
            else
            {
                Assert.That(actionButton.interactable, Is.False);
                Assert.That(actionButton.GetComponentInChildren<Text>().text, Is.EqualTo("SELECT A ROUTE"));
            }

            var actionLabel = actionButton.GetComponentInChildren<Text>();
            Assert.That(actionLabel, Is.Not.Null);
            Assert.That(ContrastRatio076(actionLabel.color, actionButton.colors.selectedColor),
                Is.GreaterThanOrEqualTo(4.5f),
                 "The focused travel action must retain WCAG AA-level text contrast instead of turning black-on-blue.");
        }

        private static void AssertCurrentMomentGeometry078(
            float screenWidth,
            float screenHeight,
            bool expectIdentityChip)
        {
            var moment = FindRectByPrefix074("Expedition Journey Overview 074");
            var context = FindRectByPrefix074("Expedition Context Action Area 074");
            var eyebrow = FindRectByPrefix074("Expedition Current Position Eyebrow 074");
            var name = FindRectByPrefix074("Expedition Current Position Name 074");
            var summary = FindRectByPrefix074("Expedition Current Position Summary 074");
            var consequence = FindRectByPrefix074("Expedition Progress Consequence 076");
            var companion = FindRectByPrefix074("Expedition Companion Story Beat 076");
            var companionText = FindRectByPrefix074("Expedition Companion Story Beat Text 076");
            var fieldCondition = FindRectByPrefix074("Expedition Field Condition 076");
            var identity = FindRectByPrefix074("Expedition Companion Identity Chip 078");
            var frame = screenWidth + "x" + screenHeight;

            foreach (var rect in new[]
                     {
                         moment, context, eyebrow, name, summary, consequence, companion,
                         companionText, fieldCondition
                     })
                Assert.That(rect, Is.Not.Null, "Missing current-beat region at " + frame + ".");
            Assert.That(identity != null, Is.EqualTo(expectIdentityChip),
                "Companion identity-chip availability was wrong at " + frame + ".");

            LayoutRebuilder.ForceRebuildLayoutImmediate(moment);
            Canvas.ForceUpdateCanvases();
            Assert.That(WorldRect074(moment).xMax,
                Is.LessThanOrEqualTo(WorldRect074(context).xMin + 0.5f),
                "Current beat and decision panel overlap at " + frame + ".");
            AssertRectAbove074(eyebrow, name, "current beat / location at " + frame);
            AssertRectAbove074(name, summary, "location / story summary at " + frame);
            AssertRectAbove074(summary, consequence, "story summary / result at " + frame);
            AssertRectAbove074(consequence, companion, "result / companion at " + frame);
            AssertRectAbove074(companion, fieldCondition, "companion / resources at " + frame);
            foreach (var rect in new[]
                     {
                         eyebrow, name, summary, consequence, companion, companionText, fieldCondition
                     })
                AssertRectInside074(moment, rect, rect.name + " at " + frame);
            AssertRectInside074(companion, companionText, "companion voice copy at " + frame);

            foreach (var text in new[]
                     {
                         eyebrow.GetComponent<Text>(), summary.GetComponent<Text>(),
                         consequence.GetComponent<Text>(), companionText.GetComponent<Text>(),
                         fieldCondition.GetComponent<Text>()
                     })
                AssertReadableChapterTwoText076(
                    text,
                    text.name + " at " + frame,
                    screenWidth,
                    screenHeight);
            AssertTextFitsRect074(name.GetComponent<Text>(), "current location at " + frame);

            if (identity == null) return;
            AssertRectInside074(companion, identity, "companion identity chip at " + frame);
            Assert.That(WorldRect074(identity).xMax,
                Is.LessThanOrEqualTo(WorldRect074(companionText).xMin + 0.5f),
                "Companion portrait chip overlaps its quote at " + frame + ".");
            var identityButton = identity.GetComponent<Button>();
            Assert.That(identityButton, Is.Not.Null);
            Assert.That(identityButton.interactable, Is.False,
                "The identity chip must not compete with the one expedition action.");
            Assert.That(identity.GetComponentsInChildren<Image>(true).Any(value =>
                    value != null && value.gameObject.name.StartsWith(
                        "Portrait Frame ",
                        StringComparison.Ordinal)),
                Is.True,
                "The identity chip must reuse the authored recruit portrait pipeline.");
        }

        private static void AssertChapterTwoStoryIdentity079(
            string expectedName,
            string expectedRoleFragment,
            string expectedPortraitResource)
        {
            var identityObject = GameObject.Find(
                "Chapter Two Story Identity " + expectedName + " 079");

            Assert.That(identityObject, Is.Not.Null,
                expectedName + " must remain visible as the Chapter 2 story anchor.");
            var portrait = identityObject.GetComponentsInChildren<Image>(true)
                .SingleOrDefault(value => value != null && value.gameObject.name.StartsWith(
                    "Chapter Two Story Portrait " + expectedName + " 079",
                    StringComparison.Ordinal));
            var identityText = identityObject.GetComponentsInChildren<Text>(true)
                .SingleOrDefault(value => value != null && value.gameObject.name.StartsWith(
                    "Chapter Two Story Identity Text " + expectedName + " 079",
                    StringComparison.Ordinal));
            Assert.That(portrait, Is.Not.Null);
            Assert.That(identityText, Is.Not.Null,
                "Responsive text appends a sizing marker; identity lookup must use its stable prefix.");
            Assert.That(identityObject.GetComponent<Button>(), Is.Null,
                "The witness card must not compete with the one field action.");
            Assert.That(identityText.text, Does.Contain(expectedName));
            foreach (var expectedRoleWord in expectedRoleFragment.Split(' '))
                Assert.That(identityText.text, Does.Contain(expectedRoleWord));
            Assert.That(identityText.name, Does.Contain("[Authored Compact 076]"));
            var expectedIdentityLineCount = StringComparer.Ordinal.Equals(
                expectedRoleFragment,
                "RESCUED") ? 4 : 5;
            Assert.That(identityText.text.Split('\n'),
                Has.Length.EqualTo(expectedIdentityLineCount),
                expectedName + " must render every role and story-state word on its own readable line.");
            Assert.That(identityText.resizeTextMinSize, Is.GreaterThanOrEqualTo(18));
            AssertTextFitsRect074(identityText, expectedName + " story identity");

            var expectedSprite = SecondDimension.Presentation.GuildCity017E
                .GuildCityOpeningExperienceRegistry017E.Sprite(expectedPortraitResource);
            Assert.That(expectedSprite, Is.Not.Null);
            Assert.That(portrait.sprite, Is.SameAs(expectedSprite));
            var companion = FindRectByPrefix074("Expedition Companion Story Beat 076");
            var companionText = FindRectByPrefix074("Expedition Companion Story Beat Text 076");
            var identity = identityObject.GetComponent<RectTransform>();
            AssertRectInside074(companion, identity, expectedName + " story identity");
            Assert.That(WorldRect074(identity).xMax,
                Is.LessThanOrEqualTo(WorldRect074(companionText).xMin + 0.5f),
                expectedName + " portrait identity overlaps the companion voice.");
        }

        private static RectTransform FindRectByPrefix074(string prefix) =>
            UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None)
                .FirstOrDefault(value => value.name.StartsWith(prefix, StringComparison.Ordinal));

        private static void AssertRectAbove074(
            RectTransform upper,
            RectTransform lower,
            string label)
        {
            var upperRect = WorldRect074(upper);
            var lowerRect = WorldRect074(lower);
            Assert.That(upperRect.yMin, Is.GreaterThanOrEqualTo(lowerRect.yMax - 0.5f),
                label + " overlap at 1280x800: upper=" + upperRect + ", lower=" + lowerRect);
        }

        private static void AssertRectInside074(
            RectTransform parent,
            RectTransform child,
            string label)
        {
            var parentRect = WorldRect074(parent);
            var childRect = WorldRect074(child);
            Assert.That(childRect.xMin, Is.GreaterThanOrEqualTo(parentRect.xMin - 0.5f), label);
            Assert.That(childRect.xMax, Is.LessThanOrEqualTo(parentRect.xMax + 0.5f), label);
            Assert.That(childRect.yMin, Is.GreaterThanOrEqualTo(parentRect.yMin - 0.5f), label);
            Assert.That(childRect.yMax, Is.LessThanOrEqualTo(parentRect.yMax + 0.5f), label);
        }

        private static Rect WorldRect074(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static float ContrastRatio076(Color foreground, Color background)
        {
            var foregroundLuminance = RelativeLuminance076(foreground);
            var backgroundLuminance = RelativeLuminance076(background);
            var lighter = Mathf.Max(foregroundLuminance, backgroundLuminance);
            var darker = Mathf.Min(foregroundLuminance, backgroundLuminance);
            return (lighter + 0.05f) / (darker + 0.05f);
        }

        private static float RelativeLuminance076(Color color)
        {
            return 0.2126f * LinearChannel076(color.r) +
                   0.7152f * LinearChannel076(color.g) +
                   0.0722f * LinearChannel076(color.b);
        }

        private static float LinearChannel076(float value)
        {
            return value <= 0.03928f
                ? value / 12.92f
                : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
        }

        private static void AssertTextFitsRect074(Text text, string label)
        {
            Assert.That(text, Is.Not.Null, label);
            var settings = text.GetGenerationSettings(text.rectTransform.rect.size);
            var preferredHeight = text.cachedTextGeneratorForLayout
                .GetPreferredHeight(text.text, settings) / text.pixelsPerUnit;
            Assert.That(preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1f),
                label + " text needs " + preferredHeight + " but owns " +
                text.rectTransform.rect.height + " layout units at 1280x800.");
        }

        private static void AssertTextContains(string expected)
        {
            var found = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Any(value => value.text != null && value.text.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.That(found, Is.True, "Visible text did not contain: " + expected);
        }

        private static int CountVisiblePlannerChoiceButtons076(string prefix)
        {
            return UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Count(value => value != null &&
                                value.gameObject.activeInHierarchy &&
                                value.name.StartsWith(prefix, StringComparison.Ordinal) &&
                                value.name.IndexOf(" Page 076", StringComparison.Ordinal) < 0);
        }

        private static Text FindTextByNamePrefix076(string prefix)
        {
            return UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .FirstOrDefault(value => value != null &&
                                         value.gameObject.activeInHierarchy &&
                                         value.name.StartsWith(prefix, StringComparison.Ordinal));
        }

        private static void AssertAuthoredReadableText076(string objectNamePrefix)
        {
            var text = FindTextByNamePrefix076(objectNamePrefix);
            Assert.That(text, Is.Not.Null,
                "Authored Chapter 2 text was not found: " + objectNamePrefix);
            Assert.That(text.name, Does.Contain("[Authored Compact 076]"),
                objectNamePrefix + " must opt out of the page-wide 32 px floor.");
            Assert.That(text.resizeTextForBestFit, Is.True);
            Assert.That(text.resizeTextMinSize, Is.GreaterThanOrEqualTo(28),
                objectNamePrefix + " must retain the Chapter 2 readability floor.");
            Assert.That(text.resizeTextMaxSize, Is.GreaterThanOrEqualTo(30),
                objectNamePrefix + " must retain a studio-facing headline/body ceiling.");
        }

        private static void AssertNamedTextContains076(string objectName, string expected)
        {
            var text = FindTextByNamePrefix076(objectName);
            Assert.That(text, Is.Not.Null, "Visible text object was not found: " + objectName);
            Assert.That(text.text, Does.Contain(expected),
                objectName + " did not contain the expected player-facing copy.");
        }

        private static void AssertNamedTextDoesNotContain076(string objectName, string forbidden)
        {
            var text = FindTextByNamePrefix076(objectName);
            Assert.That(text, Is.Not.Null, "Visible text object was not found: " + objectName);
            Assert.That(text.text.IndexOf(forbidden, StringComparison.OrdinalIgnoreCase), Is.LessThan(0),
                objectName + " leaked an earlier chapter objective: " + forbidden);
        }

        private static void AssertNamedTextEquals076(string objectName, string expected)
        {
            var text = FindTextByNamePrefix076(objectName);
            Assert.That(text, Is.Not.Null, "Visible text object was not found: " + objectName);
            Assert.That(text.text, Is.EqualTo(expected),
                objectName + " did not match the current story state.");
        }

        private static void AssertNoTextContains(string forbidden)
        {
            var found = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Any(value => value.text != null && value.text.IndexOf(forbidden, StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.That(found, Is.False, "Visible text contained forbidden phrase: " + forbidden);
        }

        private class FakeCoordinator : IM2PresentationCoordinator
        {
            public FakeCoordinator(M1Screen resume, bool partialUnions = false)
            {
                var recruits = Enumerable.Range(1, 6).Select(index => new M1RecruitLoadoutView
                {
                    RecruitId = "RECRUIT_" + index,
                    RaceId = index % 2 == 0 ? "GOBLIN" : "HUMAN",
                    VisualSeed = "VISUAL_SEED_" + index,
                    PortraitAuthorityId = "RECRUIT_" + index,
                    DisplayName = "Recruit " + index,
                    ObservedClass = ClassName(index),
                    ClassSymbol = ClassMark(index),
                    Level = 3,
                    TotalPersonalXp = 335,
                    XpIntoCurrentLevel = 35,
                    XpRequiredForNextLevel = 300,
                    HasManualEquipAction = false,
                    IsLegal = true,
                    MaximumHp = 126,
                    MaximumMp = 24,
                    MaximumHpBonus = 12,
                    MaximumMpBonus = 4,
                    StrengthIndex = 44,
                    MagicIndex = 38,
                    DefenseIndex = 42,
                    AgilityIndex = 39,
                    WillIndex = 40,
                    StrengthBonus = 2,
                    DefenseBonus = 2,
                    AgilityBonus = 1,
                    MagicBonus = 1,
                    WillBonus = 1,
                    LearnedArtIds = new[] { "ART_BASIC_SABER_CUT" },
                    ArtMastery = new[]
                    {
                        new M1ArtMasteryView
                        {
                            ArtId = "ART_BASIC_SABER_CUT",
                            DisplayName = "Saber Cut",
                            Discipline = "Martial",
                            MeaningfulUses = 4,
                            MasteryPoints = 18
                        }
                    },
                    Slots = new[]
                    {
                        new M1EquipmentSlotView
                        {
                            SlotId = "SLOT_BODY_ARMOR",
                            DisplayName = "Body Armor",
                            EquippedItemId = "ITEM_" + index,
                            EquippedItemName = "Starter Armor",
                            VisualGlyph = "♜",
                            EquippedVisualId = "ARMOR",
                            EquippedQualityId = "QUALITY_MASTERWORK",
                            EquippedRarityTierId = "RARE",
                            EquippedRarityDisplayName = "Rare",
                            IsLegal = true,
                            Choices = new[]
                            {
                                new M1EquipmentChoiceView
                                {
                                    ItemId = "ITEM_" + index,
                                    DisplayName = "Starter Armor",
                                    VisualGlyph = "♜",
                                    EquipmentVisualId = "ARMOR",
                                    QualityId = "QUALITY_MASTERWORK",
                                    RarityTierId = "RARE",
                                    RarityDisplayName = "Rare",
                                    DirectChange = "Body protection",
                                    ForecastBehavior = "May strengthen Guard-oriented forecasts.",
                                    IsEquipped = true
                                },
                                new M1EquipmentChoiceView
                                {
                                    ItemId = "ITEM_RESERVE_" + index,
                                    DisplayName = "Reserve Brigandine",
                                    VisualGlyph = "♜",
                                    EquipmentVisualId = "ARMOR",
                                    QualityId = "QUALITY_STANDARD",
                                    RarityTierId = "COMMON",
                                    RarityDisplayName = "Common",
                                    DirectChange = "Alternative body protection",
                                    ForecastBehavior = "May trade speed for Guard-oriented forecasts."
                                },
                                new M1EquipmentChoiceView
                                {
                                    ItemId = "ITEM_SCOUT_" + index,
                                    DisplayName = "Scout Coat",
                                    VisualGlyph = "♜",
                                    EquipmentVisualId = "ARMOR",
                                    QualityId = "QUALITY_STANDARD",
                                    RarityTierId = "COMMON",
                                    RarityDisplayName = "Common",
                                    DirectChange = "Lighter body protection",
                                    ForecastBehavior = "May trade Guard strength for faster forecasts."
                                }
                            }
                        }
                    }
                }).ToArray();
                State = new M1PresentationState
                {
                    HasCampaign = true,
                    HasSave = true,
                    ResumeScreen = resume,
                    SelectedModeId = "Standard",
                    GuildLevel = 2,
                    LifetimeGuildXp = 480,
                    GuildXpIntoCurrentLevel = 80,
                    GuildXpRequiredForNextLevel = 400,
                    TreasuryXp = 480,
                    HallStageIndex = 0,
                    HallStageId = "HALL_STAGE_00_RUINED_ANNEX",
                    HallStageName = "Ruined Annex",
                    HallEnhancementXp = 480,
                    Facilities = Enumerable.Range(1, 19).Select(index => new M1FacilityProgressionView
                    {
                        FacilityId = "FACILITY_" + index.ToString("00"),
                        DisplayName = "Foundation Facility " + index,
                        Level = 0,
                        TotalFacilityXp = 0
                    }).ToArray(),
                    Modes = new[]
                    {
                        Mode("Relaxed", "RELAXED"), Mode("Standard", "STANDARD"),
                        Mode("Iron", "IRON GUILD"), Mode("OverpoweredStart", "OP START")
                    },
                    Applicants = Enumerable.Range(1, 6).Select(index => new M1ApplicantView
                    {
                        RecruitId = "RECRUIT_" + index,
                        RaceId = index % 2 == 0 ? "GOBLIN" : "HUMAN",
                        VisualSeed = "VISUAL_SEED_" + index,
                        PortraitAuthorityId = "RECRUIT_" + index,
                        DisplayName = "Applicant " + index,
                        RaceAndWorld = "Human • Gate",
                        ObservedClass = ClassName(index),
                        ClassSymbol = ClassMark(index),
                        LeadershipBand = "Observed band",
                        ScoutObservations = "Committed observations",
                        PersonalityClues = "Steady",
                        GearSummary = "Starter gear",
                        SigningCost = "charter covered",
                        IsSigned = true
                    }).ToArray(),
                    Recruits = recruits,
                    Unions = new[]
                    {
                        Union(0, partialUnions
                            ? new[] { "RECRUIT_1" }
                            : new[] { "RECRUIT_1", "RECRUIT_2", "RECRUIT_3" }),
                        Union(1, partialUnions
                            ? Array.Empty<string>()
                            : new[] { "RECRUIT_4", "RECRUIT_5", "RECRUIT_6" })
                    },
                    Formations = new[] { Choice("FORMATION_SHIELD_WALL", "Shield Wall") },
                    Doctrines = new[] { Choice("DOCTRINE_BALANCED", "Read the Field") },
                    AllSixSigned = true,
                    OpeningEquipmentLegal = true,
                    OpeningUnionsLegal = !partialUnions,
                    TwoUnionsLegal = !partialUnions,
                    UsedUnionCount = partialUnions ? 1 : 2,
                    MaximumUnionPlanCount = 6,
                    SaveReloadVerified = true,
                    CanonicalStateHash = "TEST_HASH"
                };
            }

            public event Action Changed;
            public M1PresentationState State { get; }
            public int EquipmentReviewCalls { get; private set; }
            public int SaveReloadCalls { get; private set; }
            public int AssignmentCalls { get; private set; }
            public int EquipItemCalls { get; private set; }
            public int StartBattleCalls { get; private set; }
            public string LastAssignedRecruitId { get; private set; }
            public int LastAssignedUnionIndex { get; private set; }
            public int LastAssignedSlotIndex { get; private set; }

            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => Success();
            public M1CommandResult SignRecruit(string recruitId) => Success();
            public M1CommandResult EquipItem(string recruitId, string slotId, string itemId)
            {
                var recruit = (State.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                    .FirstOrDefault(value => value != null &&
                        StringComparer.Ordinal.Equals(value.RecruitId, recruitId));
                var slot = (recruit?.Slots ?? Array.Empty<M1EquipmentSlotView>())
                    .FirstOrDefault(value => value != null &&
                        StringComparer.Ordinal.Equals(value.SlotId, slotId));
                var choice = (slot?.Choices ?? Array.Empty<M1EquipmentChoiceView>())
                    .FirstOrDefault(value => value != null && value.IsLegal &&
                        StringComparer.Ordinal.Equals(value.ItemId, itemId));
                if (slot == null || choice == null)
                    return M1CommandResult.Failure("The test equipment choice was unavailable.");

                EquipItemCalls++;
                slot.EquippedItemId = choice.ItemId;
                slot.EquippedItemName = choice.DisplayName;
                slot.VisualGlyph = choice.VisualGlyph;
                slot.EquippedVisualId = choice.EquipmentVisualId;
                slot.EquippedQualityId = choice.QualityId;
                slot.EquippedRarityTierId = choice.RarityTierId;
                slot.EquippedRarityDisplayName = choice.RarityDisplayName;
                foreach (var projectedChoice in slot.Choices.Where(value => value != null))
                    projectedChoice.IsEquipped = StringComparer.Ordinal.Equals(
                        projectedChoice.ItemId, choice.ItemId);
                recruit.HasManualEquipAction = true;
                if (this is FakeGuildCityCoordinator guildCoordinator080 &&
                    choice.ItemId.StartsWith("LOOT_ITEM_070_", StringComparison.Ordinal))
                    guildCoordinator080.GuildCity017D.RecoveredLootEquipped080 = true;
                return Success();
            }
            public M1CommandResult UnequipItem(string recruitId, string slotId) => Success();
            public M1CommandResult SetEquipmentLock(string recruitId, string slotId, bool locked) => Success();
            public M1CommandResult CompleteEquipmentReview()
            {
                EquipmentReviewCalls++;
                return Success();
            }
            public M1CommandResult AddUnion()
            {
                var unions = (State.Unions ?? Array.Empty<M1UnionView>()).ToList();
                if (unions.Count >= State.MaximumUnionPlanCount)
                    return M1CommandResult.Failure("No more Union plans are available.");
                unions.Add(Union(unions.Count, Array.Empty<string>()));
                State.Unions = unions.ToArray();
                return Success();
            }
            public M1CommandResult RemoveUnion(int unionIndex) => Success();
            public M1CommandResult AssignRecruitToUnion(string recruitId, int unionIndex, int slotIndex)
            {
                AssignmentCalls++;
                LastAssignedRecruitId = recruitId;
                LastAssignedUnionIndex = unionIndex;
                LastAssignedSlotIndex = slotIndex;
                var union = State.Unions.First(value => value.Index == unionIndex);
                union.MemberRecruitIds = union.MemberRecruitIds.Concat(new[] { recruitId }).ToArray();
                union.LeaderRecruitId = union.MemberRecruitIds[0];
                union.IsLegal = union.MemberRecruitIds.Count > 0;
                union.LegalitySummary = union.IsLegal ? "Ready" : "Empty plan — optional";
                return Success();
            }
            public M1CommandResult UnassignRecruitFromUnion(string recruitId) => Success();
            public M1CommandResult SetUnionLeader(int unionIndex, string recruitId) => Success();
            public M1CommandResult SetFormation(int unionIndex, string formationId) => Success();
            public M1CommandResult SetDoctrine(int unionIndex, string doctrineId) => Success();
            public M1CommandResult SaveAndReloadProof()
            {
                SaveReloadCalls++;
                return Success();
            }
            public M1CommandResult StartTutorialBattle()
            {
                StartBattleCalls++;
                return Success();
            }
            public M1CommandResult SelectForecast(string unionId, string forecastId) => Success();
            public M1CommandResult ConfirmBattleRound() => Success();
            public M1CommandResult ReplayTutorialBattle() => Success();
            public M1CommandResult RetryTutorialBattle() => Success();
            public M1CommandResult ClaimBattleRewards() => Success();

            protected M1CommandResult Success()
            {
                Changed?.Invoke();
                return M1CommandResult.Success("Test command accepted.");
            }

            private static string ClassName(int index)
            {
                switch (index)
                {
                    case 1: return "Guardian";
                    case 2: return "Ranger";
                    case 3: return "Warrior";
                    case 4: return "Priest";
                    case 5: return "Mage";
                    default: return "Rogue";
                }
            }

            private static string ClassMark(int index)
            {
                switch (index)
                {
                    case 1: return "◈";
                    case 2: return "➶";
                    case 3: return "⚔";
                    case 4: return "✚";
                    case 5: return "✦";
                    default: return "◇";
                }
            }

            private static M1ModeView Mode(string id, string name) => new M1ModeView
            {
                Id = id,
                DisplayName = name,
                Summary = "Mode summary"
            };

            private static M1ChoiceView Choice(string id, string name) => new M1ChoiceView
            {
                Id = id,
                DisplayName = name,
                Summary = id.StartsWith("FORMATION_", StringComparison.Ordinal)
                    ? "OPENING: UP TO +8% COHESION • M2 ROLE: GUARD / COHESION"
                    : "FORECAST BIAS: BALANCED OFFENSE / GUARD / HEAL / TACTICAL • NO STAT BONUS"
            };

            private static M1UnionView Union(int index, IReadOnlyList<string> members) => new M1UnionView
            {
                Index = index,
                UnionId = "UNION_OPENING_0" + (index + 1),
                DisplayName = "Opening Union " + (index + 1),
                MemberRecruitIds = members,
                LeaderRecruitId = members.Count == 0 ? null : members[0],
                FormationId = "FORMATION_SHIELD_WALL",
                DoctrineId = "DOCTRINE_BALANCED",
                SharedAp = members.Count == 0 ? 0 : 16,
                CombinedCurrentHp = members.Count * 100,
                CombinedMaximumHp = members.Count * 120,
                CombinedMp = members.Count * 12,
                CombinedMaximumMp = members.Count * 16,
                CombinedAttack = members.Count * 42,
                CombinedMagicAttack = members.Count * 38,
                CombinedDefense = members.Count * 40,
                CombinedAgility = members.Count * 36,
                CombinedWill = members.Count * 39,
                BaseCohesionBasisPoints = members.Count == 0 ? 0 : 7200,
                FormationCohesionRuleBasisPoints = members.Count == 0 ? 0 : 800,
                CohesionBasisPoints = members.Count == 0 ? 0 : 8000,
                IsLegal = members.Count > 0,
                LegalitySummary = members.Count > 0 ? "Ready" : "Empty plan — optional",
                ExpectedTendencies = "Shield Wall • Read the Field"
            };
        }

        private sealed class FakeGuildCityCoordinator : FakeCoordinator,
            IGuildCityPresentationCoordinator017D,
            IGuildHallGuidedProgressionCoordinator080,
            IUnionReserveAssignmentCoordinator109
        {
            public const string FirstContractId = "CONTRACT_BELL_BENEATH_GATE";
            public const string SecondContractId = "CONTRACT_LINES_NOT_RETURNED";
            private readonly GuildCityContractView017D _firstContract;
            private readonly GuildCityContractView017D _secondContract;

            public FakeGuildCityCoordinator(
                bool firstChapterComplete = false,
                bool partialUnions = false)
                : base(M1Screen.GuildOperations, partialUnions)
            {
                _firstContract = new GuildCityContractView017D
                {
                    ContractId = FirstContractId,
                    DisplayName = "The Bell Beneath Skyhome",
                    Sponsor = "Kiri Aetherheart, Skyhome Guild Hall",
                    Family = "RESCUE",
                    Hook = "Kael holds the Hall breach. Follow Lantern Road: Zorin's missing patrol carries Skyhome's Wayglass toward the old Gatehouse.",
                    PrimaryObjective = "Reach the old Gatehouse, rescue the patrol, and recover the Wayglass.",
                    OptionalObjectives = new[] { "Repair the broken waymarker" },
                    Hazards = new[] { "Collapsed Lantern Road", "Gatehouse threat" },
                    RecommendedSkills = new[] { "Medicine", "Engineering" },
                    GuildXp = 42,
                    HallXp = 44,
                    CityHook = "Rescued workers accelerate the first settlement project."
                };
                _secondContract = new GuildCityContractView017D
                {
                    ContractId = SecondContractId,
                    DisplayName = "The Door Inside",
                    Sponsor = "Skyhome Wayglass Table",
                    Family = "INVESTIGATION_RESCUE",
                    Hook = "The recovered Wayglass projects a road beneath the Guild Hall.",
                    PrimaryObjective = "Find the missing survey crew and locate the door inside Skyhome.",
                    OptionalObjectives = new[] { "Recover the survey instruments" },
                    Hazards = new[] { "False route marks", "Gate fog" },
                    RecommendedSkills = new[] { "Perception", "Survival" },
                    GuildXp = 36,
                    HallXp = 34,
                    CityHook = "Recovered route data strengthens the Scout Lodge."
                };
                _firstContract.IsCompleted = firstChapterComplete;
                GuildCity017D = new GuildCityPresentationState017D
                {
                    IsAvailable = true,
                    OperationOrdinal = 1,
                    CampaignModeId = "Standard",
                    TutorialDepthId = "FastCharter",
                    TextScalePercent = 100,
                    CombatSpeed = 1,
                    ForecastDetail = "Full",
                    TotalRecruitCount = 7,
                    EquippedRecruitCount = 6,
                    NormalUnionCount = 2,
                    InventoryItemCount = 6,
                    ClaimedBattleRewardCount = firstChapterComplete ? 1 : 0,
                    CharterBuildCredits = 1,
                    CivicTrust = 12,
                    HallEnhancementXp = 480,
                    ActiveCityEffects = Array.Empty<string>(),
                    HasRecruitmentBoard = true,
                    FirstContractUnionBriefingConfirmed080 = true,
                    Assignments = new[]
                    {
                        new GuildCityAssignmentView017D
                        {
                            RecruitId = "RECRUIT_1",
                            RecruitName = "Recruit 1",
                            Kind = "Active"
                        },
                        new GuildCityAssignmentView017D
                        {
                            RecruitId = "RECRUIT_2",
                            RecruitName = "Recruit 2",
                            Kind = "Reserve"
                        }
                    },
                    Applicants = Array.Empty<GuildCityApplicantView017D>(),
                    MaterialSummaries = firstChapterComplete
                        ? new[] { "Salvaged Timber ×19", "Gate Iron ×13" }
                        : Array.Empty<string>(),
                    Plots = firstChapterComplete
                        ? new[]
                        {
                            new GuildCityPlotView017D
                            {
                                PlotId = "GC017D_PLOT_07",
                                DistrictId = "CIVIC",
                                Unlocked = false,
                                ConstructionProgress = 50
                            }
                        }
                        : Array.Empty<GuildCityPlotView017D>(),
                    Buildings = Array.Empty<GuildCityBuildingView017D>(),
                    Relationships = firstChapterComplete
                        ? new[]
                        {
                            new GuildCityRelationshipView017D
                            {
                                MemoryId = "REL_MEMORY_FIRST_VICTORY",
                                FirstRecruitId = "RECRUIT_1",
                                SecondRecruitId = "RECRUIT_2",
                                Summary = "They completed the rescue together.",
                                SceneId = "REL_SCENE_FIRST_VICTORY",
                                Viewed = false,
                                Strength = 2
                            }
                        }
                        : Array.Empty<GuildCityRelationshipView017D>(),
                    RelationshipCount = firstChapterComplete ? 1 : 0,
                    UnviewedRelationshipCount = firstChapterComplete ? 1 : 0,
                    Contracts = new[] { _firstContract, _secondContract },
                    LastCheckpointId = firstChapterComplete ? "operation_completed" : string.Empty
                };
            }

            // This fixture supports the empty-slot branch used by the recruitment
            // flow. Occupied swaps remain covered by the dedicated 109 authority tests.
            public M1CommandResult AssignReserveRecruitToUnion109(
                string recruitId, int unionIndex, int slotIndex, string expectedOccupantId)
            {
                var unions = State.Unions ?? Array.Empty<M1UnionView>();
                var recruits = State.Recruits ?? Array.Empty<M1RecruitLoadoutView>();
                var target = unions.FirstOrDefault(value => value != null && value.Index == unionIndex);
                if (target == null || slotIndex < 0 ||
                    slotIndex >= NormalUnionPlanRules.MaximumMembersPerUnion)
                    return M1CommandResult.Failure("M1_UNION_SLOT_INVALID");
                if (string.IsNullOrWhiteSpace(recruitId) || !recruits.Any(value =>
                        value != null && StringComparer.Ordinal.Equals(value.RecruitId, recruitId)))
                    return M1CommandResult.Failure("M1_RECRUIT_NOT_FOUND");
                if (unions.Any(value => value != null &&
                        (value.MemberRecruitIds ?? Array.Empty<string>()).Contains(recruitId)))
                    return M1CommandResult.Failure("M1_SELECTED_HERO_NO_LONGER_IN_RESERVE");
                var members = target.MemberRecruitIds ?? Array.Empty<string>();
                if (slotIndex > members.Count)
                    return M1CommandResult.Failure("M1_FILL_UNION_SLOTS_IN_ORDER");
                if (slotIndex < members.Count || !string.IsNullOrEmpty(expectedOccupantId))
                    return M1CommandResult.Failure("Fixture supports an unchanged empty destination only.");
                ReserveAssignmentCalls109++;
                return AssignRecruitToUnion(recruitId, unionIndex, slotIndex);
            }

            public int ReserveAssignmentCalls109 { get; private set; }
            public GuildCityPresentationState017D GuildCity017D { get; }
            public int AcceptContractCalls { get; private set; }
            public int StartExpeditionCalls { get; private set; }
            public int StartCommittedBattleCalls { get; private set; }
            public int MoveExpeditionCalls { get; private set; }
            public int ResolveCheckCalls { get; private set; }
            public int DiscoverSecretCalls { get; private set; }
            public int CommitEncounterCalls { get; private set; }
            public int FinalizeOperationCalls { get; private set; }
            public int SignApplicantCalls { get; private set; }
            public int DeclineApplicantCalls { get; private set; }
            public int PlaceBuildingCalls { get; private set; }
            public int AssignStaffCalls078 { get; private set; }
            public int CommitBoardCalls { get; private set; }
            public int RefreshBoardCalls { get; private set; }
            public int SetAssignmentCalls077 { get; private set; }
            public int ViewRelationshipCalls077 { get; private set; }
            public int LastCheckModifier { get; private set; }
            public string LastAcceptedContractId { get; private set; }
            public string LastMoveDestinationNodeId { get; private set; }
            public string LastCheckEventId { get; private set; }
            public string LastPlacedPlotId { get; private set; }
            public string LastPlacedBuildingId { get; private set; }
            private bool _refreshProducesRecruitableApplicant069 = true;

            public void PrepareFirstHallImprovement069()
            {
                var firstRecruit080 = State.Recruits.First(value =>
                    StringComparer.Ordinal.Equals(value.RecruitId, "RECRUIT_1"));
                firstRecruit080.Slots = firstRecruit080.Slots.Concat(new[]
                {
                    new M1EquipmentSlotView
                    {
                        SlotId = EquipmentSlotIds.MainHand,
                        DisplayName = "Main Hand",
                        EquippedItemId = "ITEM_STARTER_SABER_080",
                        EquippedItemName = "Starter Saber",
                        VisualGlyph = "⚔",
                        EquippedVisualId = "SWORD",
                        EquippedQualityId = "QUALITY_STARTER",
                        EquippedRarityTierId = "COMMON",
                        EquippedRarityDisplayName = "Common",
                        IsLegal = true,
                        Choices = new[]
                        {
                            new M1EquipmentChoiceView
                            {
                                ItemId = "ITEM_STARTER_SABER_080",
                                DisplayName = "Starter Saber",
                                VisualGlyph = "⚔",
                                EquipmentVisualId = "SWORD",
                                QualityId = "QUALITY_STARTER",
                                RarityTierId = "COMMON",
                                RarityDisplayName = "Common",
                                DirectChange = "Reliable patrol weapon",
                                ForecastBehavior = "Supports balanced Union commands.",
                                IsEquipped = true,
                                IsLegal = true
                            },
                            new M1EquipmentChoiceView
                            {
                                ItemId = "LOOT_ITEM_070_FIRST_HOMECOMING",
                                DisplayName = "Nightglass Patrol Blade",
                                VisualGlyph = "✦",
                                EquipmentVisualId = "DAGGER",
                                QualityId = "QUALITY_RARE",
                                RarityTierId = "RARE",
                                RarityDisplayName = "Rare",
                                DirectChange = "Recovered Wayglass edge",
                                ForecastBehavior = "Opens faster strike forecasts.",
                                IsEquipped = false,
                                IsLegal = true
                            }
                        }
                    }
                }).ToArray();
                GuildCity017D.PlacedBuildingCount = 0;
                GuildCity017D.Plots = new[]
                {
                    new GuildCityPlotView017D
                    {
                        PlotId = "GC017D_PLOT_05",
                        DistrictId = "DISTRICT_CRAFT",
                        Unlocked = true,
                        RoadConnected = true,
                        BuildingId = string.Empty
                    },
                    new GuildCityPlotView017D
                    {
                        PlotId = "GC017D_PLOT_01",
                        DistrictId = "DISTRICT_GUILD_CORE",
                        Unlocked = true,
                        RoadConnected = true,
                        BuildingId = string.Empty
                    }
                };
                GuildCity017D.Buildings = new[]
                {
                    new GuildCityBuildingView017D
                    {
                        BuildingId = "GC017D_BUILD_UNION_COMMAND",
                        DisplayName = "Union Command",
                        DistrictId = "DISTRICT_GUILD_CORE",
                        MaxLevel = 3
                    },
                    new GuildCityBuildingView017D
                    {
                        BuildingId = "GC017D_BUILD_CONTRACT_HOUSE",
                        DisplayName = "Contract House",
                        DistrictId = "DISTRICT_GUILD_CORE",
                        MaxLevel = 3
                    },
                    new GuildCityBuildingView017D
                    {
                        BuildingId = "GC017D_BUILD_FORGE",
                        DisplayName = "Forge",
                        DistrictId = "DISTRICT_CRAFT",
                        MaxLevel = 4
                    }
                };
            }

            public M1CommandResult ConfirmFirstContractUnionBriefing080()
            {
                GuildCity017D.FirstContractUnionBriefingConfirmed080 = true;
                return Success();
            }

            public M1CommandResult AcknowledgeFirstFacilityPayoff080()
            {
                GuildCity017D.FirstFacilityPayoffAcknowledged080 = true;
                return Success();
            }

            public void PrepareTitleContinuity076(
                int completedChapterCount,
                bool withSavedExpedition = false,
                string savedExpeditionNodeId = "N02")
            {
                _firstContract.IsCompleted = completedChapterCount >= 1;
                _secondContract.IsCompleted = completedChapterCount >= 2;
                _secondContract.IsActive = completedChapterCount == 1 && withSavedExpedition;
                GuildCity017D.HasActiveContract = _secondContract.IsActive;
                var thirdContract076 = new GuildCityContractView017D
                {
                    ContractId = "CONTRACT_RELIEF_ROAD",
                    DisplayName = "Keep the Relief Road Open"
                };
                thirdContract076.IsCompleted = completedChapterCount >= 3;
                GuildCity017D.Contracts = new[]
                {
                    _firstContract,
                    _secondContract,
                    thirdContract076
                };
                GuildCity017D.Expedition = withSavedExpedition
                    ? new GuildCityExpeditionView017D
                    {
                        ExpeditionId = "EXPEDITION_TITLE_CONTINUITY_076",
                        BoardId = "BOARD_LINES_NOT_RETURNED",
                        CurrentNodeId = savedExpeditionNodeId,
                        Status = "Active"
                    }
                    : null;
            }

            public void PrepareChapterOnePlannerChoices076()
            {
                State.ResumeScreen = M1Screen.UnionBuilder;
                State.Formations = Enumerable.Range(0, 8)
                    .Select(index => new M1ChoiceView
                    {
                        Id = index == 0
                            ? "FORMATION_SHIELD_WALL"
                            : "FORMATION_STORY_UNLOCK_" + index,
                        DisplayName = index == 0
                            ? "Shield Wall"
                            : "Formation " + (index + 1),
                        Summary = "A readable whole-Union position choice."
                    })
                    .ToArray();
                State.Doctrines = Enumerable.Range(0, 9)
                    .Select(index => new M1ChoiceView
                    {
                        Id = index == 0
                            ? "DOCTRINE_BALANCED"
                            : "DOCTRINE_STORY_UNLOCK_" + index,
                        DisplayName = index == 0
                            ? "Read the Field"
                            : "Battle Intent " + (index + 1),
                        Summary = "A readable whole-Union forecast preference."
                    })
                    .ToArray();
            }

            public void PrepareStaleApplicantBoard069(bool produceRecruitableApplicant)
            {
                _refreshProducesRecruitableApplicant069 = produceRecruitableApplicant;
                GuildCity017D.CanInviteEarnedContacts124 = produceRecruitableApplicant;
                GuildCity017D.PendingExpeditionRecruitLeadNames089 = produceRecruitableApplicant
                    ? new[] { "Earned Contact" } : Array.Empty<string>();
                GuildCity017D.TotalRecruitCount = 6;
                GuildCity017D.HasRecruitmentBoard = true;
                GuildCity017D.Applicants = new[]
                {
                    Applicant("SIGNED_STALE_069", 1, "Old Record", "HUMAN", "CLASS_GUARDIAN", "◈",
                        "Already signed", "Old travel gear", "This record should not block the required interview.")
                };
                GuildCity017D.Applicants[0].IsSigned = true;
            }

            public void PrepareInteractiveExpedition()
            {
                _firstContract.IsActive = true;
                GuildCity017D.HasActiveContract = true;
                GuildCity017D.HasPendingEncounter = false;
                GuildCity017D.Expedition = new GuildCityExpeditionView017D
                {
                    ExpeditionId = "EXPEDITION_V65_INTERACTIVE_TEST",
                    BoardId = "BOARD_BELL_BENEATH_GATE",
                    CurrentNodeId = "N01",
                    CurrentNodeKind = "FORK",
                    LinkedNodeIds = new[] { "N02", "N04" },
                    VisitedNodeIds = new[] { "N00", "N01" },
                    RevealedNodeIds = new[] { "N00", "N01", "N02", "N04" },
                    Status = "Active",
                    Supplies = 12,
                    Fatigue = 0,
                    Threat = 1,
                    Urgency = 14,
                    ResolutionComplete = true,
                    CanMove = true
                };
            }

            public void PrepareExpeditionDecisionIdentityFixture078()
            {
                GuildCity017D.Assignments = new[]
                {
                    new GuildCityAssignmentView017D
                    {
                        RecruitId = "RECRUIT_4",
                        RecruitName = "Wirewick",
                        Kind = "Deployed"
                    },
                    new GuildCityAssignmentView017D
                    {
                        RecruitId = "RECRUIT_1",
                        RecruitName = "Odelia Fen",
                        Kind = "Deployed"
                    },
                    new GuildCityAssignmentView017D
                    {
                        RecruitId = "RECRUIT_3",
                        RecruitName = "Daeven Fellstar",
                        Kind = "Deployed"
                    }
                };
                var wirewick = State.Recruits.Single(value =>
                    StringComparer.Ordinal.Equals(value.RecruitId, "RECRUIT_4"));
                var odelia = State.Recruits.Single(value =>
                    StringComparer.Ordinal.Equals(value.RecruitId, "RECRUIT_1"));
                var daeven = State.Recruits.Single(value =>
                    StringComparer.Ordinal.Equals(value.RecruitId, "RECRUIT_3"));
                wirewick.DisplayName = "Wirewick";
                wirewick.ObservedClass = "Priest";
                odelia.DisplayName = "Odelia Fen";
                odelia.ObservedClass = "Ranger";
                daeven.DisplayName = "Daeven Fellstar";
                daeven.ObservedClass = "Warrior";
                GuildCity017D.Expedition.CurrentEventEligibleSkills = new[] { "Medicine" };
            }

            public void PrepareWayglassResolvedRoute078()
            {
                PrepareGuidedFirstHourField076();
                GuildCity017D.Expedition.CurrentNodeId = "N08";
                GuildCity017D.Expedition.CurrentNodeKind = "EVENT";
                GuildCity017D.Expedition.CurrentEventId = "EVENT_UNSTABLE_BELL_CHAIN";
                GuildCity017D.Expedition.CurrentEventTitle = "Wayglass Resonance";
                GuildCity017D.Expedition.CurrentEventConsequence =
                    "Odelia's Wayglass reading and the Gatehouse chain hazard";
                GuildCity017D.Expedition.LinkedNodeIds = new[] { "N09", "N10" };
                GuildCity017D.Expedition.VisitedNodeIds = new[]
                    { "N00", "N01", "N02", "N03", "N04", "N05", "N06", "N07", "N08" };
                GuildCity017D.Expedition.RevealedNodeIds = GuildCity017D.Expedition.VisitedNodeIds
                    .Concat(new[] { "N09", "N10" })
                    .ToArray();
                GuildCity017D.Expedition.Supplies = 4;
                GuildCity017D.Expedition.Fatigue = 5;
                GuildCity017D.Expedition.Threat = 0;
                GuildCity017D.Expedition.Urgency = 2;
                GuildCity017D.Expedition.RequiresResolution = false;
                GuildCity017D.Expedition.ResolutionComplete = true;
                GuildCity017D.Expedition.CanMove = true;
                GuildCity017D.Expedition.CanCommitEncounter = false;
                GuildCity017D.Expedition.HasCommittedCheckAtCurrentNode = true;
                GuildCity017D.Expedition.LastCheckOutcome = "EXCEPTIONAL";
                GuildCity017D.Expedition.LastCheckActorRecruitId = "RECRUIT_1";
                GuildCity017D.Expedition.LastCheckAssistantRecruitId = "RECRUIT_3";
                GuildCity017D.Expedition.LastCheckTotal = 14;
                State.Recruits.Single(value => value.RecruitId == "RECRUIT_1").DisplayName =
                    "Odelia Fen";
                State.Recruits.Single(value => value.RecruitId == "RECRUIT_1").PortraitAuthorityId =
                    "SIGREC071_ODELIA";
            }

            public void PrepareWayglassSecondaryObjective078()
            {
                PrepareGuidedFirstHourField076();
                GuildCity017D.Expedition.CurrentNodeId = "N11";
                GuildCity017D.Expedition.CurrentNodeKind = "SECONDARY_OBJECTIVE";
                GuildCity017D.Expedition.CurrentEventId = "EVENT_GATEGLASS_PULSE";
                GuildCity017D.Expedition.CurrentEventTitle = "Orren's Blue Service Line";
                GuildCity017D.Expedition.CurrentEventProblem =
                    "Orren sees the Wayglass pulse reveal a broken Gatehouse service line for only a few seconds.";
                GuildCity017D.Expedition.CurrentEventEligibleSkills = new[] { "Lore", "Perception" };
                GuildCity017D.Expedition.LinkedNodeIds = new[] { "N13" };
                GuildCity017D.Expedition.VisitedNodeIds = new[]
                    { "N00", "N01", "N02", "N03", "N04", "N05", "N06", "N07", "N08", "N10", "N11" };
                GuildCity017D.Expedition.RevealedNodeIds = GuildCity017D.Expedition.VisitedNodeIds;
                GuildCity017D.Expedition.Supplies = 2;
                GuildCity017D.Expedition.Fatigue = 7;
                GuildCity017D.Expedition.Threat = 0;
                GuildCity017D.Expedition.Urgency = 0;
                GuildCity017D.Expedition.RequiresResolution = true;
                GuildCity017D.Expedition.ResolutionComplete = false;
                GuildCity017D.Expedition.CanMove = false;
                GuildCity017D.Expedition.CanCommitEncounter = false;
                GuildCity017D.Assignments = new[]
                {
                    new GuildCityAssignmentView017D
                    {
                        RecruitId = "RECRUIT_4", RecruitName = "Tala Stormroad", Kind = "Deployed"
                    },
                    new GuildCityAssignmentView017D
                    {
                        RecruitId = "RECRUIT_1", RecruitName = "Tazren Warmask", Kind = "Deployed"
                    },
                    new GuildCityAssignmentView017D
                    {
                        RecruitId = "RECRUIT_3", RecruitName = "Vaelis Noct", Kind = "Deployed"
                    }
                };
                var tala = State.Recruits.Single(value => value.RecruitId == "RECRUIT_4");
                tala.DisplayName = "Tala Stormroad";
                tala.ObservedClass = "Ranger";
                var tazren = State.Recruits.Single(value => value.RecruitId == "RECRUIT_1");
                tazren.DisplayName = "Tazren Warmask";
                tazren.ObservedClass = "Ranger";
                var vaelis = State.Recruits.Single(value => value.RecruitId == "RECRUIT_3");
                vaelis.DisplayName = "Vaelis Noct";
                vaelis.ObservedClass = "Rogue";
                var orren = State.Recruits.Single(value => value.RecruitId == "RECRUIT_2");
                orren.DisplayName = "Orren Vale";
                orren.PortraitAuthorityId = "SIGREC071_ORREN";
            }

            public void PrepareGuidedFirstHourField076()
            {
                _firstContract.IsActive = true;
                GuildCity017D.HasActiveContract = true;
                GuildCity017D.HasPendingEncounter = false;
                State.GuildmasterName = "Test Guildmaster";
                // Field checks and the named party are driven by deployed
                // assignments. The general fake starts with Active/Reserve Hall
                // work, so make this field-specific fixture authoritative.
                GuildCity017D.Assignments = new[]
                {
                    new GuildCityAssignmentView017D
                    {
                        RecruitId = "RECRUIT_1",
                        RecruitName = "Recruit 1",
                        Kind = "Deployed"
                    },
                    new GuildCityAssignmentView017D
                    {
                        RecruitId = "RECRUIT_4",
                        RecruitName = "Recruit 4",
                        Kind = "Deployed"
                    }
                };
                GuildCity017D.Expedition = new GuildCityExpeditionView017D
                {
                    ExpeditionId = "EXPEDITION_GUIDED_FIELD_076",
                    BoardId = GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071,
                    CurrentNodeId = "N00",
                    CurrentNodeKind = "ENTRY",
                    CurrentEncounterId = string.Empty,
                    LinkedNodeIds = new[] { "N01" },
                    VisitedNodeIds = new[] { "N00" },
                    RevealedNodeIds = new[] { "N00", "N01" },
                    ObjectiveFlags = Array.Empty<string>(),
                    Status = "Active",
                    Supplies = 12,
                    Fatigue = 0,
                    Threat = 0,
                    Urgency = 15,
                    ResolutionComplete = true,
                    CanMove = true,
                    CanCommitEncounter = false,
                    CanFinalizeOperation = false
                };
            }

            public void PrepareGuidedLanternRoadField076()
            {
                PrepareGuidedFirstHourField076();
                GuildCity017D.Expedition.CurrentNodeId = "N04";
                GuildCity017D.Expedition.CurrentNodeKind = "SKILL_CHECK";
                GuildCity017D.Expedition.CurrentEventId = "EVENT_COLLAPSED_HANDRAIL";
                GuildCity017D.Expedition.CurrentEncounterId = string.Empty;
                GuildCity017D.Expedition.LinkedNodeIds = new[] { "N05" };
                GuildCity017D.Expedition.VisitedNodeIds = new[]
                    { "N00", "N01", "N02", "N03", "N04" };
                GuildCity017D.Expedition.RevealedNodeIds =
                    new[] { "N00", "N01", "N02", "N03", "N04", "N05" };
                GuildCity017D.Expedition.ObjectiveFlags = new[]
                {
                    GuildCityExpeditionService017D.EncounterClearedFlag("N01")
                };
                GuildCity017D.Expedition.ResolutionComplete = false;
                GuildCity017D.Expedition.CanMove = false;
                GuildCity017D.Expedition.CanCommitEncounter = false;
            }

            public void PrepareGeometryEncounter074()
            {
                PrepareInteractiveExpedition();
                GuildCity017D.Expedition.CurrentNodeId = "N06";
                GuildCity017D.Expedition.CurrentNodeKind = "ENCOUNTER";
                GuildCity017D.Expedition.CurrentEncounterId = "ENCOUNTER071_LANTERN_ROAD_AMBUSH";
                GuildCity017D.Expedition.LinkedNodeIds = Array.Empty<string>();
                GuildCity017D.Expedition.VisitedNodeIds = new[]
                    { "N00", "N01", "N02", "N03", "N04", "N05", "N06" };
                GuildCity017D.Expedition.RevealedNodeIds =
                    GuildCity017D.Expedition.VisitedNodeIds;
                GuildCity017D.Expedition.Supplies = 9;
                GuildCity017D.Expedition.Fatigue = 3;
                GuildCity017D.Expedition.Threat = 0;
                GuildCity017D.Expedition.Urgency = 11;
                GuildCity017D.Expedition.ResolutionComplete = true;
                GuildCity017D.Expedition.CanMove = false;
                GuildCity017D.Expedition.CanCommitEncounter = true;
            }

            public void PrepareGeometryRescue074()
            {
                PrepareInteractiveExpedition();
                GuildCity017D.Expedition.CurrentNodeId = "N13";
                GuildCity017D.Expedition.CurrentNodeKind = "MAIN_OBJECTIVE";
                GuildCity017D.Expedition.CurrentEncounterId = "ENCOUNTER071_GATE_EATER";
                GuildCity017D.Expedition.LinkedNodeIds = Array.Empty<string>();
                GuildCity017D.Expedition.VisitedNodeIds = new[]
                    { "N00", "N01", "N02", "N03", "N04", "N05", "N06", "N07",
                      "N08", "N10", "N11", "N13" };
                GuildCity017D.Expedition.RevealedNodeIds = GuildCity017D.Expedition.VisitedNodeIds;
                GuildCity017D.Expedition.ObjectiveFlags = Array.Empty<string>();
                GuildCity017D.Expedition.Supplies = 7;
                GuildCity017D.Expedition.Fatigue = 5;
                GuildCity017D.Expedition.Threat = 0;
                GuildCity017D.Expedition.Urgency = 9;
                GuildCity017D.Expedition.ResolutionComplete = true;
                GuildCity017D.Expedition.CanMove = false;
                GuildCity017D.Expedition.CanCommitEncounter = true;
            }

            public void PrepareRecruitmentBoard()
            {
                GuildCity017D.HasRecruitmentBoard = true;
                GuildCity017D.TotalRecruitCount = 6;
                GuildCity017D.RosterCapacity = 18;
                GuildCity017D.TreasuryXp = 480;
                GuildCity017D.Applicants = new[]
                {
                    Applicant("CANDIDATE_1", 1, "Mira Vale", "HUMAN", "CLASS_GUARDIAN", "◈",
                        "Protective • Candid", "EQ_PROC_GATE_SPEAR, EQ_PROC_REPAIRED_SHIELD",
                        "She wants to find the watch patrol that disappeared below the old gate."),
                    Applicant("CANDIDATE_2", 2, "Tamsin Reed", "DOG_TRIBE", "CLASS_RANGER", "➶",
                        "Patient • Curious", "Hunting Bow, Trail Coat",
                        "They promised to carry medicine to an isolated family."),
                    Applicant("CANDIDATE_3", 3, "Orin Ash", "GOBLIN", "CLASS_MEDIC", "✚",
                        "Warm • Resourceful", "Field Knife, Remedy Kit",
                        "He is searching for the healer who taught him."),
                };
            }

            public void PrepareOptionalElite()
            {
                GuildCity017D.HasPendingEncounter = false;
                GuildCity017D.Expedition.CurrentNodeId = "N09";
                GuildCity017D.Expedition.CurrentNodeKind = "OPTIONAL_ELITE";
                GuildCity017D.Expedition.CurrentEventId = string.Empty;
                GuildCity017D.Expedition.CurrentEncounterId = "ENCOUNTER_GATE_GNAWER_ELITE";
                GuildCity017D.Expedition.LinkedNodeIds = new[] { "N10" };
                GuildCity017D.Expedition.ResolutionComplete = true;
                GuildCity017D.Expedition.CanMove = true;
                GuildCity017D.Expedition.CanCommitEncounter = true;
                GuildCity017D.Expedition.CanFinalizeOperation = false;
                GuildCity017D.Expedition.Status = "Active";
            }

            public void PrepareSecretNode()
            {
                GuildCity017D.HasPendingEncounter = false;
                GuildCity017D.Expedition.CurrentNodeId = "N05";
                GuildCity017D.Expedition.CurrentNodeKind = "RESOURCE";
                GuildCity017D.Expedition.CurrentEventId = string.Empty;
                GuildCity017D.Expedition.CurrentEncounterId = string.Empty;
                GuildCity017D.Expedition.LinkedNodeIds = new[] { "N06" };
                GuildCity017D.Expedition.ObjectiveFlags = Array.Empty<string>();
                GuildCity017D.Expedition.ResolutionComplete = true;
                GuildCity017D.Expedition.CanMove = true;
                GuildCity017D.Expedition.CanCommitEncounter = false;
                GuildCity017D.Expedition.CanFinalizeOperation = false;
                GuildCity017D.Expedition.Status = "Active";
                GuildCity017D.Expedition.Supplies = 12;
            }

            public void PrepareClearedNorthGate()
            {
                GuildCity017D.HasPendingEncounter = false;
                GuildCity017D.Expedition.CurrentNodeId = "N13";
                GuildCity017D.Expedition.CurrentNodeKind = "MAIN_OBJECTIVE";
                GuildCity017D.Expedition.CurrentEncounterId = "ENCOUNTER_GATE_GNAWER_MAIN";
                GuildCity017D.Expedition.LinkedNodeIds = new[] { "N14" };
                GuildCity017D.Expedition.ObjectiveFlags = new[]
                {
                    "ENCOUNTER_CLEARED_N13",
                    "PRIMARY_OBJECTIVE_RESCUE_COMPLETE",
                    "WAYGLASS_RECOVERED"
                };
                GuildCity017D.Expedition.ResolutionComplete = true;
                GuildCity017D.Expedition.CanMove = true;
                GuildCity017D.Expedition.CanCommitEncounter = false;
                GuildCity017D.Expedition.CanFinalizeOperation = false;
                GuildCity017D.Expedition.Status = "Active";
            }

            public void PrepareFinalizable(string status, string currentNodeId = "N14")
            {
                _firstContract.IsActive = true;
                _firstContract.IsCompleted = false;
                _firstContract.IsFailed = false;
                GuildCity017D.HasActiveContract = true;
                GuildCity017D.HasPendingEncounter = false;
                GuildCity017D.Expedition = new GuildCityExpeditionView017D
                {
                    ExpeditionId = "EXPEDITION_V66_RETURN_TEST",
                    BoardId = "BOARD_BELL_BENEATH_GATE",
                    CurrentNodeId = currentNodeId,
                    CurrentNodeKind = StringComparer.Ordinal.Equals(currentNodeId, "N14") ? "EXTRACTION" : "ENCOUNTER",
                    LinkedNodeIds = Array.Empty<string>(),
                    VisitedNodeIds = new[] { "N00", "N01", "N04", "N05", "N06", "N08", "N09", "N10", "N12", "N13", "N14" },
                    Status = status,
                    Supplies = 4,
                    Fatigue = 8,
                    ResolutionComplete = true,
                    CanFinalizeOperation = true
                };
            }

            public void PrepareChapterTwoRescue080()
            {
                AcceptGuildCityContract017D(SecondContractId);
                StartGuildCityExpedition017D();
                GuildCity017D.Expedition.ExpeditionId = "EXPEDITION_CH2_RESCUE_080";
                GuildCity017D.Expedition.CurrentNodeId = "N13";
                GuildCity017D.Expedition.CurrentNodeKind = "MAIN_OBJECTIVE";
                GuildCity017D.Expedition.CurrentEventId = string.Empty;
                GuildCity017D.Expedition.CurrentEncounterId = "ENCOUNTER_SURVEYOR_RESCUE";
                GuildCity017D.Expedition.LinkedNodeIds = Array.Empty<string>();
                GuildCity017D.Expedition.VisitedNodeIds = new[]
                {
                    "N00", "N01", "N02", "N03", "N06", "N07", "N10", "N11", "N13"
                };
                GuildCity017D.Expedition.RevealedNodeIds =
                    GuildCity017D.Expedition.VisitedNodeIds;
                GuildCity017D.Expedition.ObjectiveFlags = new[]
                {
                    GuildCityExpeditionService017D.ChapterTwoWayglassOpenedFlag076,
                    "EVENT_RESOLVED_EVENT_FOUND_APPRENTICE",
                    GuildCityExpeditionService017D.EventSuccessFlag076(
                        "EVENT_FOUND_APPRENTICE"),
                    "EVENT_RESOLVED_EVENT_WRONG_ROUTE_MARKS",
                    GuildCityExpeditionService017D.EventSuccessFlag076(
                        "EVENT_WRONG_ROUTE_MARKS"),
                    "EVENT_RESOLVED_EVENT_CAMP_ARGUMENT",
                    GuildCityExpeditionService017D.EventSuccessFlag076(
                        "EVENT_CAMP_ARGUMENT"),
                    "EVENT_RESOLVED_EVENT_BROKEN_ASTROLABE",
                    GuildCityExpeditionService017D.EventSuccessFlag076(
                        "EVENT_BROKEN_ASTROLABE"),
                    "EVENT_RESOLVED_EVENT_ABANDONED_SURVEY_PACK",
                    GuildCityExpeditionService017D.EventSuccessFlag076(
                        "EVENT_ABANDONED_SURVEY_PACK")
                };
                GuildCity017D.Expedition.RequiresResolution = false;
                GuildCity017D.Expedition.ResolutionComplete = true;
                GuildCity017D.Expedition.CanMove = false;
                GuildCity017D.Expedition.CanCommitEncounter = true;
                GuildCity017D.Expedition.CanFinalizeOperation = false;
                GuildCity017D.Expedition.Status = "Active";
            }

            public void PrepareChapterTwoFinalizable080()
            {
                PrepareFinalizable("Completed");
                _firstContract.IsActive = false;
                _firstContract.IsCompleted = true;
                _secondContract.IsActive = true;
                _secondContract.IsCompleted = false;
                _secondContract.IsFailed = false;
                GuildCity017D.HasActiveContract = true;
                GuildCity017D.Expedition.ExpeditionId = "EXPEDITION_CH2_RETURN_080";
                GuildCity017D.Expedition.BoardId = GuildCityExpeditionService017D.SecondStoryBoardId076;
                GuildCity017D.Expedition.ObjectiveFlags = new[]
                {
                    GuildCityExpeditionService017D.EncounterClearedFlag("N13"),
                    "PRIMARY_OBJECTIVE_RESCUE_COMPLETE"
                };
            }

            private static GuildCityApplicantView017D Applicant(
                string recruitId,
                int slot,
                string name,
                string race,
                string role,
                string symbol,
                string traits,
                string gear,
                string hook) => new GuildCityApplicantView017D
                {
                    Slot = slot,
                    RecruitId = recruitId,
                    DisplayName = name,
                    Kind = slot == 1 ? "Signature" : "Procedural",
                    RaceId = race,
                    WorldId = "WORLD_GATE_01",
                    ClassTendencyId = role,
                    LeadershipBand = "STEADY",
                    SigningCostTreasuryXp = slot == 1 ? 0 : 60 + slot * 5,
                    IsSigned = false,
                    ObservedSummary = "In a crisis, they protect the people beside them.",
                    VisualSeed = "VISUAL_" + recruitId,
                    PortraitAuthorityId = recruitId,
                    ClassSymbol = symbol,
                    TraitSummary = traits,
                    EquipmentSummary = gear,
                    PersonalHook = hook,
                    CanAfford = true
                };

            public M1CommandResult AcceptGuildCityContract017D(string contractId)
            {
                AcceptContractCalls++;
                LastAcceptedContractId = contractId;
                if (!StringComparer.Ordinal.Equals(contractId, FirstContractId) &&
                    !StringComparer.Ordinal.Equals(contractId, SecondContractId))
                    return M1CommandResult.Failure("Unexpected contract.");
                if (StringComparer.Ordinal.Equals(contractId, FirstContractId) &&
                    GuildCity017D.TotalRecruitCount <= 6)
                    return M1CommandResult.Failure("Recruit one new adventurer before accepting the rescue.");
                _firstContract.IsActive = StringComparer.Ordinal.Equals(contractId, FirstContractId);
                _secondContract.IsActive = StringComparer.Ordinal.Equals(contractId, SecondContractId);
                GuildCity017D.HasActiveContract = true;
                return M1CommandResult.Success("Story contract committed.");
            }

            public M1CommandResult StartGuildCityExpedition017D()
            {
                StartExpeditionCalls++;
                var secondChapter = _secondContract.IsActive;
                GuildCity017D.Expedition = new GuildCityExpeditionView017D
                {
                    ExpeditionId = "EXPEDITION_V62_TEST",
                    BoardId = secondChapter ? "BOARD_LINES_NOT_RETURNED" : "BOARD_BELL_BENEATH_GATE",
                    CurrentNodeId = secondChapter ? "N00" : "N06",
                    CurrentNodeKind = secondChapter ? "EVENT" : "ENCOUNTER",
                    CurrentEventId = secondChapter ? "EVENT_FOUND_APPRENTICE" : string.Empty,
                    CurrentEventEligibleSkills = secondChapter
                        ? new[] { "Medicine", "Diplomacy" }
                        : Array.Empty<string>(),
                    CurrentEncounterId = secondChapter ? string.Empty : "ENCOUNTER_GATE_GNAWER_STANDARD",
                    LinkedNodeIds = secondChapter ? new[] { "N01" } : new[] { "N07" },
                    VisitedNodeIds = secondChapter
                        ? new[] { "N00" }
                        : new[] { "N00", "N01", "N02", "N03", "N04", "N05", "N06" },
                    RevealedNodeIds = secondChapter
                        ? new[] { "N00", "N01" }
                        : new[] { "N00", "N01", "N02", "N03", "N04", "N05", "N06", "N07" },
                    Status = "Active",
                    Supplies = secondChapter ? 11 : 10,
                    Fatigue = secondChapter ? 0 : 2,
                    Threat = 2,
                    Urgency = secondChapter ? 12 : 11,
                    ObjectiveFlags = secondChapter
                        ? new[] { GuildCityExpeditionService017D.ChapterTwoWayglassOpenedFlag076 }
                        : new[] { "PRIMARY OBJECTIVE ACTIVE" },
                    CurrentEventUsesCommitted2d6 = true,
                    RequiresResolution = secondChapter,
                    ResolutionComplete = false,
                    CanMove = false,
                    CanCommitEncounter = !secondChapter
                };
                GuildCity017D.HasPendingEncounter = false;
                return M1CommandResult.Success("Opening expedition committed.");
            }

            public M1CommandResult StartCommittedGuildCityBattle017D()
            {
                StartCommittedBattleCalls++;
                return M1CommandResult.Success("Battle launch committed.");
            }

            public M1CommandResult PlaceGuildCityBuilding017D(string plotId, string buildingId)
            {
                PlaceBuildingCalls++;
                LastPlacedPlotId = plotId;
                LastPlacedBuildingId = buildingId;
                var plot = GuildCity017D.Plots.FirstOrDefault(value =>
                    StringComparer.Ordinal.Equals(value.PlotId, plotId));
                if (plot == null || !string.IsNullOrWhiteSpace(plot.BuildingId))
                    return M1CommandResult.Failure("Plot is unavailable.");
                var building = GuildCity017D.Buildings.FirstOrDefault(value =>
                    StringComparer.Ordinal.Equals(value.BuildingId, buildingId));
                if (building == null || !StringComparer.Ordinal.Equals(building.DistrictId, plot.DistrictId))
                    return M1CommandResult.Failure("Building is illegal for this district.");
                plot.BuildingId = buildingId;
                plot.BuildingName = building.DisplayName;
                plot.BuildingLevel = 1;
                GuildCity017D.PlacedBuildingCount++;
                GuildCity017D.CharterBuildCredits = Math.Max(0, GuildCity017D.CharterBuildCredits - 1);
                return M1CommandResult.Success("Building placed and saved.");
            }
            public M1CommandResult UpgradeGuildCityBuilding017D(string plotId) => NoOp();
            public M1CommandResult AssignGuildCityStaff017D(string plotId, string recruitId)
            {
                var plot = (GuildCity017D.Plots ?? Array.Empty<GuildCityPlotView017D>())
                    .FirstOrDefault(value => value != null &&
                        StringComparer.Ordinal.Equals(value.PlotId, plotId));
                var assignment = (GuildCity017D.Assignments ??
                                  Array.Empty<GuildCityAssignmentView017D>())
                    .FirstOrDefault(value => value != null &&
                        StringComparer.Ordinal.Equals(value.RecruitId, recruitId));
                if (plot == null || assignment == null ||
                    string.IsNullOrWhiteSpace(plot.BuildingId))
                    return M1CommandResult.Failure(
                        "A built facility and available member are required.");
                AssignStaffCalls078++;
                plot.StaffRecruitIds = new[] { recruitId };
                assignment.Kind = "Staff";
                assignment.FacilityId = plot.BuildingId;
                GuildCity017D.StaffedBuildingCount = 1;
                return M1CommandResult.Success("Facility staff assignment saved.");
            }
            public M1CommandResult RecallGuildCityStaff017D(string recruitId) => NoOp();
            public M1CommandResult SetGuildCityAssignment017D(string recruitId, string assignmentKind)
            {
                var assignment = (GuildCity017D.Assignments ??
                                  Array.Empty<GuildCityAssignmentView017D>())
                    .FirstOrDefault(value => value != null &&
                                             StringComparer.Ordinal.Equals(
                                                 value.RecruitId, recruitId));
                if (assignment == null)
                    return M1CommandResult.Failure("Assignment member was not found.");
                if (!StringComparer.OrdinalIgnoreCase.Equals(assignmentKind, "Recovering") &&
                    !StringComparer.OrdinalIgnoreCase.Equals(assignmentKind, "Training") &&
                    !StringComparer.OrdinalIgnoreCase.Equals(assignmentKind, "Reserve"))
                    return M1CommandResult.Failure("Assignment kind was not recognized.");
                SetAssignmentCalls077++;
                assignment.Kind = assignmentKind;
                GuildCity017D.LastCheckpointId = "assignment_changed";
                return M1CommandResult.Success("Parallel Guild assignment saved.");
            }
            public M1CommandResult ArchiveGuildCityMember017D(string recruitId, bool confirmed) => NoOp();
            public M1CommandResult CommitGuildCityApplicantBoard017D()
            {
                CommitBoardCalls++;
                GuildCity017D.HasRecruitmentBoard = true;
                if (GuildCity017D.CanInviteEarnedContacts124)
                {
                    GuildCity017D.Applicants = (GuildCity017D.Applicants ?? Array.Empty<GuildCityApplicantView017D>())
                        .Concat(new[] { Applicant("EARNED_CONTACT_124", 10, "Earned Contact", "HUMAN",
                            "CLASS_GUARDIAN", "◈", "Ready", "Travel gear", "An earned invitation.") }).ToArray();
                    GuildCity017D.CanInviteEarnedContacts124 = false;
                    GuildCity017D.PendingExpeditionRecruitLeadNames089 = Array.Empty<string>();
                }
                if (GuildCity017D.Applicants == null || GuildCity017D.Applicants.Count == 0)
                {
                    GuildCity017D.Applicants = new[]
                    {
                        Applicant("COMMITTED_CANDIDATE_069", 1, "New Applicant", "HUMAN",
                            "CLASS_GUARDIAN", "◈", "Ready", "Travel gear", "Ready for a permanent Guild.")
                    };
                }
                return M1CommandResult.Success("Applicant board committed and saved.");
            }
            public M1CommandResult RefreshGuildCityApplicantBoard017D()
            {
                RefreshBoardCalls++;
                GuildCity017D.HasRecruitmentBoard = true;
                GuildCity017D.Applicants = _refreshProducesRecruitableApplicant069
                    ? new[]
                    {
                        Applicant("REFRESHED_CANDIDATE_069", 1, "Fresh Applicant", "ORC",
                            "CLASS_WARRIOR", "◆", "Ready", "Field gear", "Ready for a permanent Guild.")
                    }
                    : Array.Empty<GuildCityApplicantView017D>();
                return M1CommandResult.Success("Applicant board refreshed and saved.");
            }
            public M1CommandResult SignGuildCityApplicant017D(string recruitId)
            {
                SignApplicantCalls++;
                var applicant = GuildCity017D.Applicants.First(value =>
                    StringComparer.Ordinal.Equals(value.RecruitId, recruitId));
                applicant.IsSigned = true;
                GuildCity017D.TotalRecruitCount++;
                State.Recruits = State.Recruits.Concat(new[]
                {
                    new M1RecruitLoadoutView
                    {
                        RecruitId = applicant.RecruitId,
                        RaceId = applicant.RaceId,
                        VisualSeed = applicant.VisualSeed,
                        PortraitAuthorityId = applicant.PortraitAuthorityId,
                        DisplayName = applicant.DisplayName,
                        ObservedClass = "Guardian",
                        ClassSymbol = applicant.ClassSymbol,
                        Level = 1,
                        IsLegal = true,
                        LearnedArtIds = new[] { "ART_GUARD_STANCE" },
                        Slots = new[]
                        {
                            new M1EquipmentSlotView
                            {
                                SlotId = "SLOT_BODY_ARMOR",
                                DisplayName = "Body Armor",
                                EquippedItemId = "ITEM_" + applicant.RecruitId,
                                EquippedItemName = "Traveler's Coat",
                                VisualGlyph = "♜",
                                EquippedVisualId = "ARMOR",
                                EquippedQualityId = "QUALITY_STANDARD",
                                EquippedRarityTierId = "COMMON",
                                EquippedRarityDisplayName = "Common",
                                IsLegal = true,
                                Choices = new[]
                                {
                                    new M1EquipmentChoiceView
                                    {
                                        ItemId = "ITEM_" + applicant.RecruitId,
                                        DisplayName = "Traveler's Coat",
                                        VisualGlyph = "♜",
                                        EquipmentVisualId = "ARMOR",
                                        QualityId = "QUALITY_STANDARD",
                                        RarityTierId = "COMMON",
                                        RarityDisplayName = "Common",
                                        DirectChange = "Keeps body armor ready.",
                                        ForecastBehavior = "Supports Guard-oriented forecasts.",
                                        IsEquipped = true,
                                        IsLegal = true
                                    }
                                }
                            }
                        }
                    }
                }).ToArray();
                return M1CommandResult.Success("New adventurer recruited permanently.");
            }
            public M1CommandResult DeclineGuildCityApplicant017D(string recruitId)
            {
                DeclineApplicantCalls++;
                GuildCity017D.Applicants = GuildCity017D.Applicants
                    .Where(value => !StringComparer.Ordinal.Equals(value.RecruitId, recruitId))
                    .ToArray();
                return M1CommandResult.Success("Applicant declined for this board.");
            }
            public M1CommandResult MoveGuildCityExpedition017D(string destinationNodeId)
            {
                MoveExpeditionCalls++;
                LastMoveDestinationNodeId = destinationNodeId;
                var expedition = GuildCity017D.Expedition;
                expedition.CurrentNodeId = destinationNodeId;
                expedition.VisitedNodeIds = (expedition.VisitedNodeIds ?? Array.Empty<string>())
                    .Concat(new[] { destinationNodeId }).Distinct().ToArray();
                expedition.Supplies--;
                expedition.Fatigue++;
                expedition.Urgency--;
                expedition.CanCommitEncounter = false;
                expedition.CanFinalizeOperation = false;
                if (StringComparer.Ordinal.Equals(
                        expedition.BoardId,
                        GuildCityExpeditionService017D.SecondStoryBoardId076) &&
                    StringComparer.Ordinal.Equals(destinationNodeId, "N01"))
                {
                    expedition.CurrentNodeKind = "FORK";
                    expedition.CurrentEventId = string.Empty;
                    expedition.CurrentEncounterId = string.Empty;
                    expedition.LinkedNodeIds = new[] { "N02", "N04" };
                    expedition.RequiresResolution = false;
                    expedition.ResolutionComplete = true;
                    expedition.CanMove = true;
                }
                else if (StringComparer.Ordinal.Equals(
                        expedition.BoardId,
                        GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071) &&
                    StringComparer.Ordinal.Equals(destinationNodeId, "N01"))
                {
                    expedition.CurrentNodeKind = "ENCOUNTER";
                    expedition.CurrentEventId = string.Empty;
                    expedition.CurrentEncounterId = "ENCOUNTER071_HALL_BREACH";
                    expedition.LinkedNodeIds = Array.Empty<string>();
                    expedition.RequiresResolution = false;
                    expedition.ResolutionComplete = true;
                    expedition.CanMove = false;
                    expedition.CanCommitEncounter = true;
                }
                else if (StringComparer.Ordinal.Equals(destinationNodeId, "N02"))
                {
                    expedition.CurrentNodeKind = "EVENT";
                    expedition.CurrentEventId = _secondContract.IsActive
                        ? "EVENT_WRONG_ROUTE_MARKS"
                        : "EVENT_INJURED_COURIER";
                    expedition.CurrentEncounterId = string.Empty;
                    expedition.LinkedNodeIds = new[] { "N03" };
                    expedition.RequiresResolution = true;
                    expedition.ResolutionComplete = false;
                    expedition.CanMove = false;
                }
                else if (StringComparer.Ordinal.Equals(destinationNodeId, "N04"))
                {
                    expedition.CurrentNodeKind = "SKILL_CHECK";
                    expedition.CurrentEventId = _secondContract.IsActive
                        ? "EVENT_BROKEN_SURVEY_BRIDGE"
                        : "EVENT_COLLAPSED_HANDRAIL";
                    expedition.LinkedNodeIds = new[] { "N05" };
                    expedition.RequiresResolution = true;
                    expedition.ResolutionComplete = false;
                    expedition.CanMove = false;
                }
                else if (StringComparer.Ordinal.Equals(destinationNodeId, "N05"))
                {
                    expedition.CurrentNodeKind = "RESOURCE";
                    expedition.CurrentEventId = string.Empty;
                    expedition.CurrentEncounterId = string.Empty;
                    expedition.LinkedNodeIds = new[] { "N06" };
                    expedition.RequiresResolution = false;
                    expedition.ResolutionComplete = true;
                    expedition.CanMove = true;
                }
                else if (StringComparer.Ordinal.Equals(destinationNodeId, "N10"))
                {
                    expedition.CurrentNodeKind = "SKILL_CHECK";
                    expedition.CurrentEventId = "EVENT_TRAPPED_FOREMAN";
                    expedition.CurrentEncounterId = string.Empty;
                    expedition.LinkedNodeIds = new[] { "N11", "N12" };
                    expedition.RequiresResolution = true;
                    expedition.ResolutionComplete = false;
                    expedition.CanMove = false;
                }
                else if (StringComparer.Ordinal.Equals(destinationNodeId, "N14"))
                {
                    expedition.CurrentNodeKind = "EXTRACTION";
                    expedition.CurrentEventId = string.Empty;
                    expedition.CurrentEncounterId = string.Empty;
                    expedition.LinkedNodeIds = Array.Empty<string>();
                    expedition.RequiresResolution = false;
                    expedition.ResolutionComplete = true;
                    expedition.CanMove = false;
                    expedition.CanFinalizeOperation = true;
                    expedition.Status = "Completed";
                }
                return M1CommandResult.Success("Board movement committed and saved.");
            }
            public M1CommandResult ResolveGuildCityCheck017D(string eventId, string actorRecruitId,
                string assistantRecruitId, int modifier)
            {
                ResolveCheckCalls++;
                LastCheckModifier = modifier;
                LastCheckEventId = eventId;
                var expedition = GuildCity017D.Expedition;
                expedition.ResolutionComplete = true;
                expedition.CanMove = true;
                expedition.HasCommittedCheckAtCurrentNode = true;
                expedition.LastCheckActorRecruitId = actorRecruitId;
                expedition.LastCheckAssistantRecruitId = assistantRecruitId;
                expedition.LastCheckDieOne = 3;
                expedition.LastCheckDieTwo = 4;
                expedition.LastCheckModifier = modifier;
                expedition.LastCheckTotal = 7 + modifier;
                expedition.LastCheckOutcome = modifier == 0 ? "SUCCESS_WITH_COST" : "FULL_SUCCESS";
                return M1CommandResult.Success("The committed 2d6 result and relationship memory were saved.");
            }
            public M1CommandResult DiscoverGateworksMaintenancePassage066()
            {
                var expedition = GuildCity017D.Expedition;
                if (expedition == null || !StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N05"))
                    return M1CommandResult.Failure("The cache is not reachable.");
                if ((expedition.ObjectiveFlags ?? Array.Empty<string>()).Contains(
                        GuildCityExpeditionService017D.GateworksMaintenancePassageFlag066))
                    return M1CommandResult.Success("Secret already found.");
                DiscoverSecretCalls++;
                expedition.Supplies++;
                expedition.ObjectiveFlags = (expedition.ObjectiveFlags ?? Array.Empty<string>())
                    .Concat(new[] { GuildCityExpeditionService017D.GateworksMaintenancePassageFlag066 })
                    .ToArray();
                return M1CommandResult.Success("Secret found.");
            }
            public M1CommandResult CommitGuildCityEncounter017D(string encounterId)
            {
                CommitEncounterCalls++;
                var expedition = GuildCity017D.Expedition;
                if (expedition == null || !expedition.CanCommitEncounter ||
                    !StringComparer.Ordinal.Equals(expedition.CurrentEncounterId, encounterId))
                    return M1CommandResult.Failure("Encounter is not available here.");
                expedition.CanCommitEncounter = false;
                GuildCity017D.HasPendingEncounter = true;
                return M1CommandResult.Success("Encounter committed.");
            }
            public M1CommandResult FinalizeGuildCityOperation017D()
            {
                FinalizeOperationCalls++;
                if (GuildCity017D.Expedition == null || !GuildCity017D.Expedition.CanFinalizeOperation)
                    return M1CommandResult.Failure("The operation cannot return yet.");
                var terminalStatus = GuildCity017D.Expedition.Status;
                GuildCity017D.Expedition = null;
                GuildCity017D.HasActiveContract = false;
                GuildCity017D.HasPendingEncounter = false;
                _firstContract.IsActive = false;
                _firstContract.IsCompleted = StringComparer.Ordinal.Equals(terminalStatus, "Completed");
                _firstContract.IsFailed = StringComparer.Ordinal.Equals(terminalStatus, "Failed");
                GuildCity017D.OperationOrdinal = Math.Max(1, GuildCity017D.OperationOrdinal);
                GuildCity017D.ClaimedBattleRewardCount = Math.Max(1, GuildCity017D.ClaimedBattleRewardCount);
                return M1CommandResult.Success("Operation finalized.");
            }
            public M1CommandResult AddGuildCityRelationshipMemory017D(string firstRecruitId,
                string secondRecruitId, string sourceId, string summary, int strength, string sceneId)
            {
                var existing = (GuildCity017D.Relationships ??
                                Array.Empty<GuildCityRelationshipView017D>())
                    .FirstOrDefault(value => value != null &&
                                             StringComparer.Ordinal.Equals(value.SceneId, sceneId));
                if (existing != null) return M1CommandResult.Success("Memory already saved.");
                GuildCity017D.Relationships = (GuildCity017D.Relationships ??
                                               Array.Empty<GuildCityRelationshipView017D>())
                    .Concat(new[]
                    {
                        new GuildCityRelationshipView017D
                        {
                            MemoryId = "REL_MEMORY_TEST_077_" + sourceId,
                            FirstRecruitId = firstRecruitId,
                            SecondRecruitId = secondRecruitId,
                            Summary = summary,
                            Strength = strength,
                            SceneId = sceneId,
                            Viewed = false
                        }
                    })
                    .ToArray();
                GuildCity017D.RelationshipCount = GuildCity017D.Relationships.Count;
                GuildCity017D.UnviewedRelationshipCount++;
                return M1CommandResult.Success("Relationship memory saved.");
            }
            public M1CommandResult ViewGuildCityRelationshipScene017D(string sceneId)
            {
                var memory = (GuildCity017D.Relationships ??
                              Array.Empty<GuildCityRelationshipView017D>())
                    .FirstOrDefault(value => value != null &&
                                             StringComparer.Ordinal.Equals(value.SceneId, sceneId));
                if (memory == null)
                    return M1CommandResult.Failure("Relationship scene was not found.");
                memory.Viewed = true;
                GuildCity017D.UnviewedRelationshipCount = Math.Max(
                    0, GuildCity017D.UnviewedRelationshipCount - 1);
                GuildCity017D.LastCheckpointId = "relationship_scene_viewed_free";
                ViewRelationshipCalls077++;
                return M1CommandResult.Success("Relationship memory kept in the Hall.");
            }

            private static M1CommandResult NoOp() => M1CommandResult.Success("Test command accepted.");
        }
    }
}
