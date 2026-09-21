#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign023;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class ExpeditionDeck089PresentationPlayModeTests
    {
        const string TestObjectPrefix = "Expedition Deck Presentation 089 Test";
        const string CardFacePrefix =
            "SecondDimension/Art/Board086/CardFaces/CARD_FACE_";

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (transform != null && transform.parent == null &&
                    transform.name.StartsWith(TestObjectPrefix,
                        StringComparison.Ordinal))
                    UnityEngine.Object.Destroy(transform.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TutorialShowsOneNumberedStepAtATimeAndFinalAckStaysSeen()
        {
            var steps = ExpeditionDeckService089.TutorialSteps.ToArray();
            Assert.That(steps, Has.Length.EqualTo(5));
            var state = ActiveState(false);
            state.ExpeditionDeckTutorialSteps = steps;
            var coordinator = new DeckCoordinator089(state);
            var presenter = CreatePresenter(" Tutorial");

            for (var index = 0; index < steps.Length; index++)
            {
                var body = CreateProbeBody(" Tutorial Step " + index);
                InvokeBuild(presenter, "BuildExpeditionDeckHelp089", body,
                    coordinator, state);

                var copy = VisibleText(body);
                Assert.That(copy, Does.Contain(
                    "EXPEDITION DECK TUTORIAL  •  " + (index + 1) + " OF 5"));
                Assert.That(copy, Does.Contain(steps[index]));
                for (var other = 0; other < steps.Length; other++)
                    if (other != index)
                        Assert.That(copy, Does.Not.Contain(steps[other]),
                            "The first-time tutorial must reveal one concise step, not the entire manual.");

                var helpButton = Buttons(body).Single(value => value.name ==
                    "Toggle Expedition Deck details 089");
                Assert.That(ButtonLabel(helpButton),
                    Is.EqualTo("HOW EXPEDITIONS WORK  •  DECK DETAILS"),
                    "Permanent help must remain reachable during every tutorial step.");

                var next = Buttons(body).Single(value => value.name ==
                    "Acknowledge Expedition Deck tutorial 089");
                Assert.That(ButtonLabel(next), Is.EqualTo(index < steps.Length - 1
                    ? "NEXT  •  " + (index + 2) + " OF 5"
                    : "GOT IT  •  SHUFFLE MY THREE CARDS"));
                next.onClick.Invoke();

                if (index < steps.Length - 1)
                {
                    Assert.That(coordinator.AcknowledgeCount, Is.Zero);
                    Assert.That(TutorialStep(presenter), Is.EqualTo(index + 1),
                        "NEXT must advance exactly one page.");
                }
                else
                {
                    Assert.That(coordinator.AcknowledgeCount, Is.EqualTo(1));
                    Assert.That(state.ExpeditionDeckTutorialSeen, Is.True);
                    Assert.That(TutorialStep(presenter), Is.Zero);
                }
                UnityEngine.Object.DestroyImmediate(body.gameObject);
            }

            // Re-open the presentation after the acknowledgement. This mirrors a
            // normal coordinator refresh and proves the acknowledged state, rather
            // than a local panel deletion, controls whether the tutorial returns.
            var reopened = CreateProbeBody(" Tutorial Reopened");
            InvokeBuild(presenter, "BuildExpeditionDeckHelp089", reopened,
                coordinator, state);
            Assert.That(VisibleText(reopened),
                Does.Not.Contain("EXPEDITION DECK TUTORIAL"));
            var permanentHelp = Buttons(reopened).Single(value => value.name ==
                "Toggle Expedition Deck details 089");
            permanentHelp.onClick.Invoke();
            UnityEngine.Object.DestroyImmediate(reopened.gameObject);

            var details = CreateProbeBody(" Permanent Details");
            InvokeBuild(presenter, "BuildExpeditionDeckHelp089", details,
                coordinator, state);
            var detailCopy = VisibleText(details);
            Assert.That(detailCopy, Does.Contain("HOW EXPEDITIONS WORK"));
            Assert.That(detailCopy, Does.Contain("DECK DETAILS"));
            Assert.That(detailCopy, Does.Contain("REMAINING  •  STORY × 4"));
            Assert.That(detailCopy, Does.Contain("DISCARD  •  HAZARD × 1"));
            Assert.That(detailCopy, Does.Contain(
                "SHUFFLE → DEAL THREE → CHOOSE → FLIP → ROLL → CLAIM"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActiveRouteDealsThreeBlindCardsAndRevealsOnlyTheSelectedFace091()
        {
            foreach (var key in new[]
                     {
                         "STORY", "CHEST", "BUFF", "HAZARD", "CHANCE",
                         "RECRUIT", "CAMP", "BATTLE", "OBJECTIVE"
                     })
            {
                var path = CardFacePrefix + key + "_089";
                Assert.That(Resources.Load<Sprite>(path), Is.Not.Null,
                    path + " must be imported as a shipping Sprite resource.");
            }

            var state = ActiveState(true);
            state.RouteCards = new[]
            {
                RouteCard("CARD_STORY_089", "STORY", "A Lantern in the Rain"),
                RouteCard("CARD_CHEST_089", "CHEST", "Wayglass Cache"),
                RouteCard("CARD_RECRUIT_089", "RECRUIT", "A Stranger's Trail")
            };
            var coordinator = new DeckCoordinator089(state);
            var presenter = CreatePresenter(" Route Row");
            var body = CreateProbeBody(" Route Row Body");
            InvokeBuild(presenter, "BuildExpeditionRouteRow089", body,
                coordinator, state);

            var row = body.GetComponentsInChildren<RectTransform>(true)
                .Single(value => value.name == "Expedition route row 089");
            var cardPanels = Enumerable.Range(0, row.childCount)
                .Select(index => row.GetChild(index) as RectTransform)
                .Where(value => value != null && value.name.StartsWith(
                    "Expedition route card ", StringComparison.Ordinal))
                .ToArray();
            Assert.That(cardPanels, Has.Length.EqualTo(3),
                "Every active draw must present one and only one three-card choice row.");

            var chooseButtons = cardPanels.SelectMany(panel =>
                    panel.GetComponentsInChildren<Button>(true))
                .Where(value => value.name.StartsWith(
                    "Choose Expedition route card ", StringComparison.Ordinal))
                .ToArray();
            Assert.That(chooseButtons, Has.Length.EqualTo(3));
            Assert.That(chooseButtons.All(value => !value.IsInteractable()), Is.True);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    "CHOOSE ROUTE  •  STORY", "CHOOSE ROUTE  •  CHEST",
                    "CHOOSE ROUTE  •  RECRUIT"
                },
                chooseButtons.Select(ButtonLabel));
            Assert.That(VisibleText(body), Does.Contain(
                "SHUFFLING THE EXPEDITION DECK"),
                "The row must visibly announce its shuffle before dealing three cards.");
            Assert.That(VisibleText(body), Does.Not.Contain("WAYGLASS CACHE"));
            Assert.That(VisibleText(body), Does.Not.Contain("REWARD  •"));
            var choice = row.GetComponent<ExpeditionCardChoice091>();
            choice.Tick091(2f);
            Assert.That(body.GetComponentsInChildren<Button>().Count(value =>
                value.name.StartsWith("Blind Quest Card Back ") && value.interactable),
                Is.EqualTo(3));
            Assert.That(coordinator.CommittedCardIds, Is.Empty);

            var faces = cardPanels.SelectMany(panel =>
                    panel.GetComponentsInChildren<Image>(true))
                .Where(value => value.name.StartsWith(
                    "Expedition card face ", StringComparison.Ordinal))
                .ToArray();
            Assert.That(faces, Has.Length.EqualTo(3));
            Assert.That(faces.All(value => value.sprite != null), Is.True);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    "CARD_FACE_STORY_089", "CHEST_OPENING_ATLAS_092",
                    "CARD_FACE_RECRUIT_089"
                },
                faces.Select(value => value.sprite.texture.name));
            var chestFace092 = faces.Single(value =>
                value.sprite.texture.name == "CHEST_OPENING_ATLAS_092");
            Assert.That(chestFace092.sprite, Is.SameAs(BoardChestReveal092.Frame092(0)),
                "The chosen chest starts sealed; the full three-pose atlas must never be shown as a card face.");
            Assert.That(chestFace092.preserveAspect, Is.True);
            Assert.That(cardPanels.SelectMany(panel =>
                    panel.GetComponentsInChildren<Text>(true))
                .Any(value => value.name.StartsWith(
                    "Expedition card fallback face ", StringComparison.Ordinal)),
                Is.False);
            Assert.That(cardPanels.All(panel => panel
                    .GetComponentsInChildren<Text>(true)
                    .Any(value => value.name.StartsWith(
                        "Expedition card possible results ",
                        StringComparison.Ordinal) &&
                        value.text.Contains("SUCCESS") &&
                        (value.text.Contains("SETBACK") ||
                         value.text.Contains("NO FAILURE ROLL")))),
                Is.True, "Each authored face must retain its check outcomes for the chosen reveal.");

            Assert.That(choice.Choose091(1), Is.True);
            Assert.That(choice.Choose091(0), Is.False);
            choice.Tick091(ExpeditionCardChoice091.RevealDuration091);
            Assert.That(VisibleText(body), Does.Contain("WAYGLASS CACHE"));
            Assert.That(VisibleText(body), Does.Not.Contain("A STRANGER'S TRAIL"));
            Assert.That(coordinator.CommittedCardIds, Is.Empty);

            chooseButtons.Single(value => value.name.Contains("CARD_CHEST_089"))
                .onClick.Invoke();
            CollectionAssert.AreEqual(new[] {"CARD_CHEST_089"},
                coordinator.CommittedCardIds);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnseenTutorialHidesRouteCardsUntilTheFinalAcknowledgement089()
        {
            var state = ActiveState(false);
            state.RouteCards = new[]
            {
                RouteCard("CARD_STORY_TUTORIAL_089", "STORY", "A Lantern in the Rain"),
                RouteCard("CARD_CHEST_TUTORIAL_089", "CHEST", "Wayglass Cache"),
                RouteCard("CARD_RECRUIT_TUTORIAL_089", "RECRUIT", "A Stranger's Trail")
            };
            var coordinator = new DeckCoordinator089(state);
            var presenter = CreatePresenter(" Tutorial Gate");
            var body = CreateProbeBody(" Tutorial Gate Body");

            InvokeBuild(presenter, "BuildActiveAdventureBoard084", body,
                coordinator, state);

            Assert.That(VisibleText(body), Does.Contain(
                "EXPEDITION DECK TUTORIAL  •  1 OF 5"));
            Assert.That(Buttons(body).Count(value => value.name.StartsWith(
                    "Choose Expedition route card ", StringComparison.Ordinal)),
                Is.Zero,
                "The first route draw must not animate, settle, or accept an off-screen " +
                "choice underneath the one-card-at-a-time tutorial.");
            Assert.That(Buttons(body).Single(value => value.name ==
                "Acknowledge Expedition Deck tutorial 089"), Is.Not.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SynchronousCoordinatorChangedCannotEraseTheLiveRouteFlip089()
        {
            var state = ActiveState(true);
            state.RouteCards = new[]
            {
                RouteCard("CARD_STORY_CHANGED_089", "STORY", "The True Road"),
                RouteCard("CARD_CHEST_CHANGED_089", "CHEST", "The Brass Cache"),
                RouteCard("CARD_BUFF_CHANGED_089", "BUFF", "A Guild Blessing")
            };
            var coordinator = new DeckCoordinator089(state);
            var presenter = CreatePresenter(" Changed Gate");
            presenter.Initialize(coordinator);
            presenter.enabled = true;

            var pageField = typeof(M1FlowPresenter).GetField(
                "_activePage", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(pageField, Is.Not.Null);
            var activePage = (RectTransform)pageField.GetValue(presenter);
            Assert.That(activePage, Is.Not.Null);
            var sentinel = new GameObject(
                TestObjectPrefix + " Changed Refresh Sentinel", typeof(RectTransform));
            sentinel.transform.SetParent(activePage, false);
            coordinator.ChangedSurvivalProbe = () =>
                sentinel != null && sentinel.transform.parent == activePage;

            var body = CreateProbeBody(" Changed Route Body");
            InvokeBuild(presenter, "BuildExpeditionRouteRow089", body,
                coordinator, state);
            var flipStages = body.GetComponentsInChildren<RectTransform>(true)
                .Where(value => value.name.StartsWith("Blind Quest Card Back "))
                .ToArray();
            Assert.That(flipStages, Has.Length.EqualTo(3));
            Assert.That(flipStages.All(value => value.gameObject.activeSelf), Is.True);

            var choice = body.GetComponentInChildren<ExpeditionCardChoice091>();
            choice.Tick091(2f);
            choice.Choose091(1);
            choice.Tick091(ExpeditionCardChoice091.RevealDuration091);
            Buttons(body).Single(value => value.name ==
                "Choose Expedition route card CARD_CHEST_CHANGED_089 089").onClick.Invoke();

            Assert.That(coordinator.ProbeSurvivedCoordinatorChanged, Is.True,
                "Changed must be suppressed during the saved command so only the " +
                "manual post-command build can create the animated authoritative state.");
            Assert.That(flipStages.All(value => value != null), Is.True,
                "A synchronous Changed rebuild must not destroy the live card-flip stage.");
            CollectionAssert.AreEqual(new[] {"CARD_CHEST_CHANGED_089"},
                coordinator.CommittedCardIds);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RevealedResultUsesTheChosenCardTitleFaceAndCategory089()
        {
            var state = ActiveState(true);
            state.PendingReceiptId = "EXPEDITION_RECEIPT_VISUAL_089";
            state.PendingCardTitle = "The Azure Guild Blessing";
            state.PendingCardCategory = "BUFF";
            state.PendingCardVisualResourcePath = CardFacePrefix + "BUFF_089";
            state.PendingOutcomeTitle = "BOON SECURED";
            state.PendingReward = "+12% Union cohesion for this quest";
            state.CurrentNode = new NodeView023
            {
                NodeId = "AUTHORED_EVENT_NODE_089",
                Kind = "EVENT",
                RoomKind = "EVENT",
                RoomTitle = "Old Authority Room",
                Title = "A Different Node Title",
                Description = "The Guild raises its lanterns and answers together."
            };
            var coordinator = new DeckCoordinator089(state);
            var presenter = CreatePresenter(" Selected Card Result");
            var body = CreateProbeBody(" Selected Card Result Body");

            InvokeBuildArguments(presenter, "BuildWorldGatePrimaryAction084",
                body, coordinator, state, state.CurrentNode);

            var copy = VisibleText(body);
            Assert.That(copy, Does.Contain(
                "ROOM FLIPPED  •  THE AZURE GUILD BLESSING"));
            Assert.That(copy, Does.Not.Contain("A DIFFERENT NODE TITLE"));
            Assert.That(body.GetComponentsInChildren<RectTransform>(true).Any(value =>
                    value.name ==
                    "Board Adventure Revealed Card Type Ribbon 087 BLESSING"),
                Is.True,
                "A selected BUFF card must reveal as a Guild boon even when the " +
                "underlying authored route node is a generic event.");
            var face = body.GetComponentsInChildren<Image>(true).Single(value =>
                value.name == "Revealed Expedition Card Face 089");
            Assert.That(face.sprite, Is.Not.Null);
            Assert.That(face.sprite.texture.name, Is.EqualTo("CARD_FACE_BUFF_089"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MerchantCardShowsExactXpCostPowerAndLeaveAlternative089()
        {
            var state = ActiveState(true);
            state.RouteCards = new[]
            {
                new ExpeditionRouteCardView089
                {
                    CardId = "CARD_MERCHANT_089",
                    Category = "MERCHANT",
                    VisualCategoryKey = "MERCHANT",
                    VisualResourcePath = CardFacePrefix + "CHEST_089",
                    Title = "Wayglass Merchant",
                    Description = "Choose another card to leave without spending XP.",
                    RiskLabel = "OPTIONAL PURCHASE",
                    Odds = "BALANCE 31 XP • COST 45 XP",
                    OutcomePreview =
                        "BUY • ITEM ENTERS INVENTORY  |  LEAVE • CHOOSE ANOTHER CARD",
                    RewardPreview =
                        "SPEND 45 XP • Rare Merchant Blade • PWR +9 / MYS +4 • sent to Inventory",
                    RouteLabel = "EVENT RESOLVES • ROUTE CARDS NEXT",
                    IsEncounterRound = true,
                    TreasuryXpCost = 45,
                    TreasuryXpBalance = 31,
                    CanChoose = false,
                    LockedReason = "NEED 45 XP • CURRENT BALANCE 31"
                },
                RouteCard("CARD_STORY_LEAVE_089", "STORY", "Leave the Stall"),
                RouteCard("CARD_CHEST_LEAVE_089", "CHEST", "Search Elsewhere")
            };
            var coordinator = new DeckCoordinator089(state);
            var presenter = CreatePresenter(" Merchant");
            var body = CreateProbeBody(" Merchant Body");
            InvokeBuild(presenter, "BuildExpeditionRouteRow089", body,
                coordinator, state);

            var copy = VisibleText(body);
            Assert.That(copy, Does.Not.Contain("BALANCE 31 XP • COST 45 XP"));
            var choice = body.GetComponentInChildren<ExpeditionCardChoice091>();
            choice.Tick091(2f);
            choice.Choose091(0);
            choice.Tick091(ExpeditionCardChoice091.RevealDuration091);
            Assert.That(coordinator.CommittedCardIds, Is.Empty,
                "Revealing a mystery merchant cannot spend undisclosed XP.");
            copy = VisibleText(body);
            Assert.That(copy, Does.Contain("BALANCE 31 XP • COST 45 XP"));
            Assert.That(copy, Does.Contain("PWR +9 / MYS +4"));
            Assert.That(copy, Does.Contain(
                "BUY • ITEM ENTERS INVENTORY  |  LEAVE • CHOOSE ANOTHER CARD"));
            Assert.That(copy, Does.Contain("NEED 45 XP • CURRENT BALANCE 31"));
            var buy = Buttons(body).Single(value =>
                value.name.Contains("CARD_MERCHANT_089"));
            Assert.That(ButtonLabel(buy), Is.EqualTo("BUY FOR 45 XP"));
            Assert.That(buy.interactable, Is.False,
                "An unaffordable offer must remain visible but cannot spend XP.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator OptionalBattleRevealWaitsForExplicitCertifiedCombatEntry089()
        {
            var state = ActiveState(true);
            state.PendingReceiptId = "EXPREC089_OPTIONAL_BATTLE_TEST";
            state.PendingCardTitle = "Battle: Wandering Riftguard";
            state.PendingCardCategory = "BATTLE";
            state.PendingCardVisualResourcePath = CardFacePrefix + "BATTLE_089";
            state.PendingOutcomeTitle = "OPTIONAL BATTLE REVEALED";
            state.PendingReward =
                "2 enemy Unions • battle loot + Art growth • route cards next";
            state.PendingDice = "CERTIFIED UNION BATTLE READY";
            state.PendingIsEncounterRound = true;
            state.PendingRequiresCertifiedBattle = true;
            state.CurrentNode = new NodeView023
            {
                NodeId = "OPTIONAL_BATTLE_ROOM_089",
                Kind = "START",
                RoomKind = "STORY",
                Title = "The Broken Causeway",
                Description = "A wandering force crosses the quest route."
            };
            var coordinator = new DeckCoordinator089(state);
            var presenter = CreatePresenter(" Optional Battle");
            var body = CreateProbeBody(" Optional Battle Body");
            InvokeBuildArguments(presenter, "BuildWorldGatePrimaryAction084",
                body, coordinator, state, state.CurrentNode);

            Assert.That(VisibleText(body), Does.Contain(
                "SAVED  •  ENTER UNION COMBAT WHEN READY"));
            Assert.That(VisibleText(body),
                Does.Contain("battle loot + Art growth").IgnoreCase);
            var enter = Buttons(body).Single(value => value.name ==
                "Enter optional Expedition card battle 089");
            Assert.That(ButtonLabel(enter), Is.EqualTo(
                "FACE THE THREAT  •  ENTER / RETURN TO BATTLE"));
            Assert.That(ScheduledReceiptCount089(presenter), Is.Zero,
                "The saved card receipt cannot auto-apply around certified combat.");
            Assert.That(coordinator.ReceiptApplyCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FailedSavedReceiptWaitsForExplicitRetryAndThenContinues()
        {
            var state = PendingResultState089("EXPEDITION_RECEIPT_RETRY_089");
            var coordinator = new DeckCoordinator089(state)
            {
                FailNextReceiptApply = true
            };
            var presenter = CreatePresenter(" Saved Result Retry");

            InvokeBuildArguments(presenter, "ApplyWorldGateReceiptNow084",
                coordinator, state.PendingReceiptId);
            Assert.That(coordinator.ReceiptApplyCount, Is.EqualTo(1));
            Assert.That(state.PendingReceiptId,
                Is.EqualTo("EXPEDITION_RECEIPT_RETRY_089"));

            var body = CreateProbeBody(" Saved Result Retry Body");
            InvokeBuildArguments(presenter, "BuildWorldGatePrimaryAction084",
                body, coordinator, state, state.CurrentNode);
            var retry = Buttons(body).Single(value =>
                value.name == "Retry saved World Gate result 084");
            Assert.That(ButtonLabel(retry), Is.EqualTo("RETRY SAVED RESULT"));
            Assert.That(coordinator.ReceiptApplyCount, Is.EqualTo(1),
                "A failed exact-once apply must wait for the player's retry input.");

            retry.onClick.Invoke();
            Assert.That(coordinator.ReceiptApplyCount, Is.EqualTo(2));
            Assert.That(state.PendingReceiptId, Is.Empty,
                "A successful retry must advance the already-saved result once.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator InterruptedReceiptScheduleIsReleasedForPresenterReEntry()
        {
            var state = PendingResultState089("EXPEDITION_RECEIPT_REENTRY_089");
            var coordinator = new DeckCoordinator089(state);
            var gameObject = new GameObject(
                TestObjectPrefix + " Receipt Schedule Re-entry");
            var presenter = gameObject.AddComponent<M1FlowPresenter>();

            InvokeBuildArguments(presenter, "ScheduleWorldGateReceiptApply084",
                coordinator, state.PendingReceiptId, false);
            Assert.That(ScheduledReceiptCount089(presenter), Is.EqualTo(1));

            presenter.enabled = false;
            Assert.That(ScheduledReceiptCount089(presenter), Is.Zero,
                "Disabling the presenter must release interrupted receipt keys.");

            presenter.enabled = true;
            InvokeBuildArguments(presenter, "ScheduleWorldGateReceiptApply084",
                coordinator, state.PendingReceiptId, false);
            Assert.That(ScheduledReceiptCount089(presenter), Is.EqualTo(1),
                "Re-entry must be able to schedule the same still-pending receipt.");
            presenter.enabled = false;
            yield return null;
        }

        [UnityTest]
        public IEnumerator LockedStoryBattleShowsIllustratedEncounterBeforeOneBattleAction()
        {
            var state = ActiveState(true);
            state.ActiveStatus = "Active";
            state.ActiveOperationKind = "CHAPTER";
            state.CurrentNode = new NodeView023
            {
                NodeId = "BOSS_GATE_089",
                Kind = "BATTLE",
                RoomKind = "BATTLE",
                RoomTitle = "The Bell Chamber",
                Title = "The Warden Below",
                Description = "The Warden seals the route home.",
                StoryFlavor = "The bell answers with a voice from beneath the world.",
                Objective = "Defeat the Warden and recover the Guild seal.",
                RewardPreview = "Guild seal • certified battle rewards",
                RequiresBattle = true
            };
            state.RouteCards = new[]
            {
                new ExpeditionRouteCardView089
                {
                    CardId = "LOCKED_BOSS_089_A",
                    Category = "BOSS",
                    VisualCategoryKey = "BATTLE",
                    VisualResourcePath = CardFacePrefix + "BATTLE_089",
                    Title = "Boss: The Warden Below",
                    Description = "Enemy preview: 8 hostile Unions. Resolve in certified Union combat.",
                    RewardPreview = "certified battle rewards",
                    RiskLabel = "CERTAIN",
                    Odds = "LOCKED STORY BATTLE",
                    SuccessBasisPoints = 10000,
                    RouteLabel = "FACE THE WARDEN",
                    LockedToBattle = true
                }
            };
            var coordinator = new DeckCoordinator089(state);
            var presenter = CreatePresenter(" Locked Battle");
            var body = CreateProbeBody(" Locked Battle Body");

            InvokeBuild(presenter, "BuildActiveAdventureBoard084", body,
                coordinator, state);

            var face = body.GetComponentsInChildren<Image>(true).Single(value =>
                value.name == "Locked Expedition Encounter Card Face 089");
            Assert.That(face.sprite, Is.Not.Null);
            Assert.That(face.sprite.texture.name, Is.EqualTo("CARD_FACE_BATTLE_089"));
            Assert.That(VisibleText(body), Does.Contain("BOSS ENCOUNTER"));
            Assert.That(VisibleText(body), Does.Contain(
                "LOCKED STORY BATTLE  •  FULL UNION FORECAST COMBAT"));

            var battleButton = Buttons(body).Single(value =>
                value.name == "Flip monster battle tile 084");
            Assert.That(Buttons(body).Count(value => value.name.StartsWith(
                "Choose Expedition route card ", StringComparison.Ordinal)),
                Is.Zero, "A locked authored battle must not masquerade as a selectable route card.");
            Assert.That(face.transform.parent.GetSiblingIndex(),
                Is.LessThan(battleButton.transform.GetSiblingIndex()),
                "The player must see the encounter card and enemy preview before the battle action.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ThreeBlindChoicesAndTheirLabelsFitAboveTheDockWithoutScrolling110()
        {
            var state = ActiveState(true);
            state.CurrentNode = new NodeView023
            {
                NodeId = "FIT_NODE_110", Kind = "RESOURCE", RoomKind = "STORY",
                Title = "The Hall Road", Description = "Follow the road to the next saved encounter."
            };
            state.RouteCards = new[]
            {
                RouteCard("FIT_STORY_110", "STORY", "Unseen Story"),
                RouteCard("FIT_CHEST_110", "CHEST", "Unseen Chest"),
                RouteCard("FIT_CHANCE_110", "CHANCE", "Unseen Chance")
            };
            var coordinator = new DeckCoordinator089(state);
            var presenter = CreatePresenter(" Viewport Fit 110");
            typeof(M1FlowPresenter).GetField("_reducedMotion",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(presenter, true);
            var canvasObject = new GameObject(TestObjectPrefix + " Viewport Fit Canvas 110",
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            var runtimeUi = typeof(M1FlowPresenter).Assembly.GetType(
                "SecondDimension.Presentation.RuntimeUi", true);
            var reference = (Vector2)runtimeUi.GetField("ReferenceResolution",
                BindingFlags.Public | BindingFlags.Static).GetValue(null);
            var screenRoot = new GameObject("Route Production Screen Root 110",
                typeof(RectTransform)).GetComponent<RectTransform>();
            screenRoot.SetParent(canvasRect, false);
            screenRoot.anchorMin = Vector2.zero;
            screenRoot.anchorMax = Vector2.one;
            screenRoot.offsetMin = new Vector2(96f, 42f);
            screenRoot.offsetMax = new Vector2(-96f, -42f);
            typeof(M1FlowPresenter).GetField("_screenRoot",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(presenter, screenRoot);
            typeof(M1FlowPresenter).GetField("_coordinator",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(presenter, coordinator);
            var body = (RectTransform)typeof(M1FlowPresenter).GetMethod("CreatePage",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(presenter,
                    new object[] { "BOARD ADVENTURE", "THREE SAVED CHOICES", null });
            var scroll = body.GetComponentInParent<ScrollRect>();
            var page = scroll.transform.parent as RectTransform;
            InvokeBuildArguments(presenter, "AddGuildMobileNavigation062", page);
            // Include the real mission summary and deck heading which consumed
            // the space omitted by the old standalone row geometry fixture.
            InvokeBuild(presenter, "BuildActiveAdventureBoard084", body, coordinator, state);
            foreach (var resolution in new[] { new Vector2(1280f, 800f),
                         new Vector2(1280f, 720f), new Vector2(1600f, 720f) })
            {
                // Match the shipping CanvasScaler without changing Game View.
                var scale = Mathf.Sqrt((resolution.x / reference.x) *
                                       (resolution.y / reference.y));
                canvasRect.sizeDelta = resolution / scale;
                canvasRect.localScale = Vector3.one * scale;
                for (var pass = 0; pass < 3; pass++)
                {
                    InvokeBuildArguments(presenter, "FinalizeActivePageLayout");
                    yield return null;
                    RebuildMerchantLayout104(page, body);
                }
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1f;
                Canvas.ForceUpdateCanvases();
                var backs = body.GetComponentsInChildren<Button>(false).Where(value =>
                    value.name.StartsWith("Blind Quest Card Back ", StringComparison.Ordinal)).ToArray();
                Assert.That(backs, Has.Length.EqualTo(3));
                foreach (var back in backs)
                {
                    Assert.That(back.IsInteractable(), Is.True);
                    AssertMerchantRectInside104(scroll.viewport, back.GetComponent<RectTransform>(),
                        resolution + " blind card above dock at top scroll");
                    var prompt = back.GetComponentInChildren<Text>();
                    Assert.That(prompt.text, Is.EqualTo("PICK THIS CARD"));
                    AssertMerchantRectInside104(scroll.viewport, prompt.rectTransform,
                        resolution + " complete pick label above dock");
                }
                Assert.That(coordinator.CommittedCardIds, Is.Empty);
                Assert.That(body.GetComponentsInChildren<RectTransform>(false).Any(value =>
                    value.name == "Adventure Compact Progress Strip 084"), Is.False);
            }
            var choice = body.GetComponentInChildren<ExpeditionCardChoice091>();
            Assert.That(choice.Choose091(0), Is.True);
            yield return null;
            RebuildMerchantLayout104(page, body);
            var action = Buttons(body).Single(value =>
                value.name == "Choose Expedition route card FIT_STORY_110 089");
            Assert.That(action.IsInteractable(), Is.True);
            AssertMerchantRectInside104(scroll.viewport, action.GetComponent<RectTransform>(),
                "revealed action remains above the dock without scrolling");
            Assert.That(coordinator.CommittedCardIds, Is.Empty,
                "Resizing and turning a card must never submit it.");
        }

        [UnityTest]
        public IEnumerator MerchantBuyAndReturnFitTheRevealedCardAt720p104()
        {
            var state = ActiveState(true);
            state.RouteCards = new[]
            {
                new ExpeditionRouteCardView089
                {
                    CardId = "CARD_MERCHANT_LAYOUT_104",
                    Category = "MERCHANT",
                    VisualCategoryKey = "CHEST",
                    VisualResourcePath = CardFacePrefix + "CHEST_089",
                    Title = "Wayglass Merchant",
                    Description = "A Wayglass merchant offers one permanent item. Choose another card to leave without spending XP.",
                    RiskLabel = "SAFE",
                    Odds = "BALANCE 6672 XP • COST 45 XP",
                    OutcomePreview = "BUY • ITEM ENTERS INVENTORY  |  LEAVE • CHOOSE ANOTHER CARD",
                    RewardPreview = "SPEND 45 XP • Rare Merchant Wayglass Blade • PWR +7 / MYS +2 • sent to Inventory",
                    RouteLabel = "EVENT RESOLVES • ROUTE CARDS NEXT",
                    IsEncounterRound = true,
                    TreasuryXpCost = 45,
                    TreasuryXpBalance = 6672,
                    CanChoose = true,
                    LockedReason = string.Empty
                },
                RouteCard("CARD_HIDDEN_STORY_104", "STORY", "Unseen Story Test Face"),
                RouteCard("CARD_HIDDEN_CHEST_104", "CHEST", "Unseen Chest Test Face")
            };
            var coordinator = new DeckCoordinator089(state);
            var presenter = CreatePresenter(" Merchant Layout 720p");
            var canvasObject = new GameObject(TestObjectPrefix + " Merchant Layout Canvas",
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            // Reproduce RuntimeUi's ScaleWithScreenSize / MatchWidthOrHeight(0.5)
            // at 1280x720 without changing the Editor's global Game View resolution.
            var runtimeUi = typeof(M1FlowPresenter).Assembly.GetType(
                "SecondDimension.Presentation.RuntimeUi", true);
            var reference = (Vector2)runtimeUi.GetField("ReferenceResolution",
                BindingFlags.Public | BindingFlags.Static).GetValue(null);
            var resolution = new Vector2(1280f, 720f);
            var scale = Mathf.Sqrt((resolution.x / reference.x) *
                                   (resolution.y / reference.y));
            canvasRect.sizeDelta = resolution / scale;
            canvasRect.localScale = Vector3.one * scale;
            var screenRoot = new GameObject("Merchant Production Screen Root 104",
                typeof(RectTransform)).GetComponent<RectTransform>();
            screenRoot.SetParent(canvasRect, false);
            screenRoot.anchorMin = Vector2.zero;
            screenRoot.anchorMax = Vector2.one;
            screenRoot.offsetMin = new Vector2(96f, 42f);
            screenRoot.offsetMax = new Vector2(-96f, -42f);
            typeof(M1FlowPresenter).GetField("_screenRoot",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(presenter, screenRoot);
            typeof(M1FlowPresenter).GetField("_coordinator",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(presenter, coordinator);
            var createPage = typeof(M1FlowPresenter).GetMethod("CreatePage",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(createPage, Is.Not.Null);
            var body = (RectTransform)createPage.Invoke(presenter, new object[]
                { "BOARD ADVENTURE", "MERCHANT CONTROL REACHABILITY", null });
            var scroll = body.GetComponentInParent<ScrollRect>();
            Assert.That(scroll, Is.Not.Null);
            Assert.That(scroll.content, Is.SameAs(body));
            Assert.That(scroll.viewport.GetComponent<Mask>(), Is.Not.Null);
            InvokeBuild(presenter, "BuildExpeditionRouteRow089", body, coordinator, state);
            var page = scroll.transform.parent as RectTransform;
            // Guild pages reserve a fixed navigation row below the ScrollRect.
            // Omitting it leaves enough extra height for an otherwise clipped action.
            InvokeBuildArguments(presenter, "AddGuildMobileNavigation062", page);
            InvokeBuildArguments(presenter, "FinalizeActivePageLayout");
            RebuildMerchantLayout104(page, body);
            yield return null;
            RebuildMerchantLayout104(page, body);
            InvokeBuildArguments(presenter, "FinalizeActivePageLayout");

            var choice = body.GetComponentInChildren<ExpeditionCardChoice091>();
            // Disable only automatic ticking; exercise the real deal/reveal state machine.
            choice.enabled = false;
            choice.Tick091(2f);
            Assert.That(VisibleText(body), Does.Not.Contain("WAYGLASS MERCHANT"));
            Assert.That(coordinator.CommittedCardIds, Is.Empty);
            Assert.That(choice.Choose091(0), Is.True);
            choice.Tick091(ExpeditionCardChoice091.RevealDuration091);
            // The face was inactive when the page was first built. Let its newly
            // active reading-column layout settle before measuring any child.
            yield return null;
            InvokeBuildArguments(presenter, "FinalizeActivePageLayout");
            yield return null;
            RebuildMerchantLayout104(page, body);
            var face = body.GetComponentsInChildren<RectTransform>(true).Single(value =>
                value.name == "Expedition route card surface CARD_MERCHANT_LAYOUT_104 089");
            var column = face.GetComponentsInChildren<RectTransform>(true).Single(value =>
                value.name == "Board Card Reading Column 091");
            var buy = Buttons(face).Single(value => value.name ==
                "Choose Expedition route card CARD_MERCHANT_LAYOUT_104 089");
            var leave = Buttons(face).Single(value => value.name ==
                "Return Revealed Quest Card 0 091");
            var navigation = page.GetComponentsInChildren<RectTransform>().Single(value =>
                value.name == "Guild Mobile Bottom Navigation 062");
            foreach (var measured in new[] { canvasRect, page, scroll.viewport, body,
                         face, column, buy.GetComponent<RectTransform>(),
                         leave.GetComponent<RectTransform>(), navigation })
                LogMerchantGeometry104(measured);
            foreach (var child in column.GetComponentsInChildren<RectTransform>())
                LogMerchantGeometry104(child);
            Assert.That(face.GetComponentsInChildren<ScrollRect>(true), Has.Length.EqualTo(1),
                "Long merchant copy scrolls inside the fitted card while its actions stay visible.");
            Assert.That(ButtonLabel(buy), Is.EqualTo("BUY FOR 45 XP"));
            Assert.That(ButtonLabel(leave), Is.EqualTo("NOT NOW • RETURN TO CARDS"));
            Assert.That(VisibleText(body), Does.Not.Contain("UNSEEN STORY TEST FACE"));
            Assert.That(VisibleText(body), Does.Not.Contain("UNSEEN CHEST TEST FACE"));
            Assert.That(coordinator.CommittedCardIds, Is.Empty);
            foreach (var button in new[] { buy, leave })
            {
                var rect = button.GetComponent<RectTransform>();
                AssertMerchantRectInside104(column, rect, button.name + " inside reading column");
                AssertMerchantRectInside104(face, rect, button.name + " inside card face");
            }
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();
            Debug.Log("MERCHANT_LAYOUT104_BOTTOM_SCROLL " + scroll.verticalNormalizedPosition);
            foreach (var measured in new[] { scroll.viewport, body, face, column,
                         buy.GetComponent<RectTransform>(), leave.GetComponent<RectTransform>() })
                LogMerchantGeometry104(measured);
            foreach (var button in new[] { buy, leave })
            {
                Assert.That(button.gameObject.activeInHierarchy && button.IsInteractable(), Is.True,
                    button.name + " must remain an active reachable action at bottom scroll");
                Assert.That(button.targetGraphic.raycastTarget, Is.True);
                AssertMerchantRectInside104(scroll.viewport, button.GetComponent<RectTransform>(),
                    button.name + " fully inside the 720p scroll viewport");
            }
            leave.onClick.Invoke();
            Assert.That(choice.Phase091, Is.EqualTo(
                ExpeditionCardChoice091.ChoicePhase091.AwaitingChoice));
            Assert.That(coordinator.CommittedCardIds, Is.Empty,
                "Returning from the merchant must not buy an item or submit any card.");
            Assert.That(VisibleText(body), Does.Not.Contain("UNSEEN STORY TEST FACE"));
            Assert.That(VisibleText(body), Does.Not.Contain("UNSEEN CHEST TEST FACE"));
            yield return null;
        }

        static void RebuildMerchantLayout104(RectTransform page, RectTransform body)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(page);
            LayoutRebuilder.ForceRebuildLayoutImmediate(body);
            LayoutRebuilder.ForceRebuildLayoutImmediate(page);
            LayoutRebuilder.ForceRebuildLayoutImmediate(body);
            Canvas.ForceUpdateCanvases();
        }

        static void AssertMerchantRectInside104(RectTransform outer,
            RectTransform inner, string reason)
        {
            Assert.That(outer.rect.width, Is.GreaterThan(1f), reason + " outer width");
            Assert.That(outer.rect.height, Is.GreaterThan(1f), reason + " outer height");
            Assert.That(inner.rect.width, Is.GreaterThan(1f), reason + " inner width");
            Assert.That(inner.rect.height, Is.GreaterThan(1f), reason + " inner height");
            var corners = new Vector3[4];
            inner.GetWorldCorners(corners);
            foreach (var world in corners)
            {
                var point = outer.InverseTransformPoint(world);
                Assert.That(point.x, Is.InRange(outer.rect.xMin - 0.5f,
                    outer.rect.xMax + 0.5f), reason + " horizontal bounds");
                Assert.That(point.y, Is.InRange(outer.rect.yMin - 0.5f,
                    outer.rect.yMax + 0.5f), reason + " vertical bounds");
            }
        }


        static void LogMerchantGeometry104(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var element = rect.GetComponent<LayoutElement>();
            var text = rect.GetComponent<Text>();
            Debug.Log("MERCHANT_LAYOUT104_GEOMETRY " + rect.name +
                " active=" + rect.gameObject.activeInHierarchy +
                " local=" + rect.rect + " worldMin=" + corners[0] +
                " worldMax=" + corners[2] +
                " minHeight=" + (element == null ? -1f : element.minHeight) +
                " preferredHeight=" + (element == null ? -1f : element.preferredHeight) +
                " calculatedPreferred=" + LayoutUtility.GetPreferredHeight(rect) +
                " font=" + (text == null ? 0 : text.fontSize));
        }

        [UnityTest]
        public IEnumerator NamedRecruitFacesUseExactSavedIdentityAndStayBlindUntilReveal114()
        {
            var cases = new[]
            {
                new { Id = "HERO_REC_183", Name = "Rogar Earthborn", Category = "RECRUIT", Exact = true },
                new { Id = "HERO_REC_276", Name = "Celia Sunclock", Category = "RECRUIT", Exact = true },
                new { Id = "", Name = "Rogar Earthborn", Category = "RECRUIT", Exact = false },
                new { Id = "UNKNOWN_EXACT_TARGET", Name = "Celia Sunclock", Category = "RECRUIT", Exact = false },
                new { Id = "HERO_REC_183", Name = "Rogar Earthborn", Category = "STORY", Exact = false }
            };
            var catalog = SecondDimension.Presentation.Creator028.HeroMaster300CreatorRegistry087.Load().Source;
            Sprite firstExact = null;
            for (var index = 0; index < cases.Length; index++)
            {
                var test = cases[index];
                var card = RouteCard("CARD_EXACT_RECRUIT_ART_114_" + index,
                    test.Category, "Recruit chance: " + test.Name);
                card.RecruitStableId = test.Id;
                card.RecruitName = test.Name;
                card.CanChoose = true;
                var state = ActiveState(true);
                state.RouteCards = new[] { card,
                    RouteCard("CARD_UNSEEN_ART_114_A_" + index, "STORY", "Unseen Story"),
                    RouteCard("CARD_UNSEEN_ART_114_B_" + index, "CHEST", "Unseen Chest") };
                var before = SnapshotPublicFields114(state);
                var beforeCards = state.RouteCards.Select(SnapshotPublicFields114).ToArray();
                var coordinator = new DeckCoordinator089(state);
                var presenter = CreatePresenter(" Exact Recruit Art " + index);
                var body = CreateProbeBody(" Exact Recruit Art Body " + index);
                InvokeBuild(presenter, "BuildExpeditionRouteRow089", body, coordinator, state);
                var face = body.GetComponentsInChildren<Image>(true).Single(value =>
                    value.name == "Expedition card face " + card.CardId + " 089");
                Assert.That(face.sprite, Is.Not.Null);
                Assert.That(face.gameObject.activeInHierarchy, Is.False,
                    "Correct hero art must remain hidden on the blind card.");
                Assert.That(face.preserveAspect, Is.True);
                var generic = Resources.Load<Sprite>(card.VisualResourcePath);
                if (test.Exact)
                {
                    Assert.That(catalog.TryGetAcceptedHero(test.Id, out var hero), Is.True);
                    Assert.That(hero.Name, Is.EqualTo(test.Name));
                    Assert.That(HeroRemasterAtlas093.TryResolve093(test.Id, false,
                        out var expected, out var key), Is.True);
                    Assert.That(key, Is.EqualTo(HeroRemasterAtlas093.Root093 + test.Id + "_PAIR_093#IDLE"));
                    Assert.That(face.sprite, Is.SameAs(expected), "Use this exact hero's existing framed idle sprite.");
                    Assert.That(face.sprite, Is.Not.SameAs(generic));
                    if (firstExact == null) firstExact = face.sprite;
                    else Assert.That(face.sprite, Is.Not.SameAs(firstExact), "The two named heroes need distinct art.");
                }
                else
                    Assert.That(face.sprite, Is.SameAs(generic),
                        "A display name, missing target, invalid target or unrelated card cannot invent an identity binding.");

                var choice = body.GetComponentInChildren<ExpeditionCardChoice091>();
                choice.enabled = false;
                choice.Tick091(2f);
                Assert.That(choice.Choose091(0), Is.True);
                choice.Tick091(ExpeditionCardChoice091.RevealDuration091);
                yield return null;
                Assert.That(face.gameObject.activeInHierarchy, Is.True);
                Assert.That(coordinator.CommittedCardIds, Is.Empty,
                    "Binding or revealing artwork cannot submit a choice or change its outcome.");
                Assert.That(SnapshotPublicFields114(state), Is.EqualTo(before));
                Assert.That(state.RouteCards.Select(SnapshotPublicFields114).ToArray(), Is.EqualTo(beforeCards));
                UnityEngine.Object.DestroyImmediate(body.gameObject);
                UnityEngine.Object.DestroyImmediate(presenter.gameObject);
            }
        }


        static object[] SnapshotPublicFields114(object value) => value.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Select(field => field.GetValue(value))
            .Select(fieldValue => fieldValue is IEnumerable sequence && !(fieldValue is string)
                ? sequence.Cast<object>().ToArray() : fieldValue).ToArray();

        static CampaignWorldGatePresentationState023 ActiveState(bool tutorialSeen) =>
            new CampaignWorldGatePresentationState023
            {
                IsAvailable = true,
                ActiveOperationId = "EXPEDITION_OPERATION_089_TEST",
                ActiveBoardTitle = "The Bell Beneath the Gate",
                ActiveOperationKind = "CHAPTER",
                ActiveStatus = "Active",
                BoardObjective = "Reach the bell chamber and return alive.",
                DeckDrawPileCount = 18,
                DeckDiscardCount = 1,
                DeckBanishedCount = 2,
                DeckMomentum = 1,
                DeckModifierLabels = new[] {"Scout Lens • +1 route checks"},
                DeckRemainingCompositionLabels = new[] {"STORY × 4", "CHEST × 2"},
                DeckDiscardCompositionLabels = new[] {"HAZARD × 1"},
                ExpeditionDeckTutorialSeen = tutorialSeen,
                ExpeditionDeckTutorialSteps = ExpeditionDeckService089.TutorialSteps
            };

        static CampaignWorldGatePresentationState023 PendingResultState089(
            string receiptId)
        {
            var state = ActiveState(true);
            state.PendingReceiptId = receiptId;
            state.PendingCardTitle = "Wayglass Cache";
            state.PendingCardCategory = "CHEST";
            state.PendingCardVisualResourcePath = CardFacePrefix + "CHEST_089";
            state.PendingOutcomeTitle = "CACHE SECURED";
            state.PendingReward = "Rare Wayglass gear sent to Inventory";
            state.CurrentNode = new NodeView023
            {
                NodeId = "RETRY_NODE_089",
                Kind = "RESOURCE",
                RoomKind = "CHEST",
                Title = "The Saved Cache",
                Description = "The Guild opens a sealed cache."
            };
            return state;
        }

        static ExpeditionRouteCardView089 RouteCard(
            string cardId, string category, string title) =>
            new ExpeditionRouteCardView089
            {
                CardId = cardId,
                Category = category,
                VisualCategoryKey = category,
                VisualResourcePath = CardFacePrefix + category + "_089",
                Title = title,
                Description = "Choose this visible route and resolve its reward.",
                RiskLabel = category == "STORY" ? "SAFE" : "FAVORED",
                Odds = category == "STORY" ? "NO ROLL • CERTAIN" : "BASE 75%",
                OutcomePreview = category == "STORY"
                    ? "SUCCESS • full listed reward  |  NO FAILURE ROLL"
                    : "SUCCESS • full listed reward  |  SETBACK • reduced reward",
                SuccessBasisPoints = category == "STORY" ? 10000 : 7500,
                RewardPreview = category == "RECRUIT"
                    ? "unlock a recruitment lead"
                    : "+10 Guild / Hall XP",
                RouteLabel = "PRESS FORWARD"
            };

        static M1FlowPresenter CreatePresenter(string suffix)
        {
            var gameObject = new GameObject(TestObjectPrefix + suffix);
            var presenter = gameObject.AddComponent<M1FlowPresenter>();
            presenter.enabled = false;
            return presenter;
        }

        static RectTransform CreateProbeBody(string suffix)
        {
            var gameObject = new GameObject(TestObjectPrefix + suffix,
                typeof(RectTransform), typeof(VerticalLayoutGroup));
            return gameObject.GetComponent<RectTransform>();
        }

        static void InvokeBuild(M1FlowPresenter presenter, string methodName,
            Transform body, object coordinator, object state)
        {
            var method = typeof(M1FlowPresenter).GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(presenter, new[] {body, coordinator, state});
        }

        static void InvokeBuildArguments(
            M1FlowPresenter presenter,
            string methodName,
            params object[] arguments)
        {
            var method = typeof(M1FlowPresenter).GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(presenter, arguments);
        }

        static int TutorialStep(M1FlowPresenter presenter)
        {
            var field = typeof(M1FlowPresenter).GetField(
                "_expeditionDeckTutorialStep089",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (int)field.GetValue(presenter);
        }

        static int ScheduledReceiptCount089(M1FlowPresenter presenter)
        {
            var field = typeof(M1FlowPresenter).GetField(
                "_scheduledWorldGateReceiptApplies084",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return ((IEnumerable<string>)field.GetValue(presenter)).Count();
        }

        static string VisibleText(RectTransform body) => string.Join("\n",
            body.GetComponentsInChildren<Text>().Select(value => value.text));

        static Button[] Buttons(RectTransform body) =>
            body.GetComponentsInChildren<Button>(true);

        static string ButtonLabel(Button button) =>
            button.GetComponentInChildren<Text>(true)?.text ?? string.Empty;

        sealed class DeckCoordinator089 :
            IM1PresentationCoordinator,
            ICampaignWorldGatePresentationCoordinator023,
            IExpeditionDeckPresentationCoordinator089
        {
            readonly CampaignWorldGatePresentationState023 _state;
            readonly List<string> _committedCardIds = new List<string>();
            readonly M1PresentationState _presentationState = new M1PresentationState
            {
                HasCampaign = false,
                HasSave = false,
                ResumeScreen = M1Screen.NewGuild,
                SelectedModeId = "Standard"
            };

            public DeckCoordinator089(CampaignWorldGatePresentationState023 state) =>
                _state = state;

            public event Action Changed;
            public M1PresentationState State => _presentationState;
            public CampaignWorldGatePresentationState023 CampaignWorldGate023 => _state;
            public IReadOnlyList<string> CommittedCardIds => _committedCardIds;
            public int AcknowledgeCount { get; private set; }
            public int ReceiptApplyCount { get; private set; }
            public int OptionalBattleEntryCount { get; private set; }
            public bool FailNextReceiptApply { get; set; }
            public Func<bool> ChangedSurvivalProbe { get; set; }
            public bool ProbeSurvivedCoordinatorChanged { get; private set; }

            public M1CommandResult CommitExpeditionRouteCard089(string cardId)
            {
                _committedCardIds.Add(cardId);
                Changed?.Invoke();
                ProbeSurvivedCoordinatorChanged = ChangedSurvivalProbe?.Invoke() == true;
                return M1CommandResult.Success("Route committed.");
            }

            public M1CommandResult AcknowledgeExpeditionDeckTutorial089()
            {
                AcknowledgeCount++;
                _state.ExpeditionDeckTutorialSeen = true;
                return M1CommandResult.Success("Tutorial saved.");
            }

            public M1CommandResult EnterExpeditionCardBattle089()
            {
                OptionalBattleEntryCount++;
                return M1CommandResult.Success(
                    "Entered the saved optional Union battle.");
            }

            public M1CommandResult BeginWorldGateOperation023(string definitionId) =>
                M1CommandResult.Success();
            public M1CommandResult CommitWorldGateChoice023(string choiceId) =>
                M1CommandResult.Success();
            public M1CommandResult CommitAutomaticWorldGateRoom023() =>
                M1CommandResult.Success();
            public M1CommandResult ApplyWorldGateReceipt023()
            {
                ReceiptApplyCount++;
                if (FailNextReceiptApply)
                {
                    FailNextReceiptApply = false;
                    return M1CommandResult.Failure(
                        "The saved result is waiting for another try.");
                }
                _state.PendingReceiptId = string.Empty;
                return M1CommandResult.Success();
            }
            public M1CommandResult EnterWorldGateBattle023() =>
                M1CommandResult.Success();
            public M1CommandResult FinalizeWorldGateOperation023() =>
                M1CommandResult.Success();
            public M1CommandResult TravelWorldGate023(string worldId) =>
                M1CommandResult.Success();
            public M1CommandResult RecoverLegacyWorldGateQuest023() =>
                M1CommandResult.Success();

            public M1CommandResult CreateGuild(M1NewGuildIntent intent) =>
                M1CommandResult.Success();
            public M1CommandResult SignRecruit(string recruitId) =>
                M1CommandResult.Success();
            public M1CommandResult EquipItem(
                string recruitId, string slotId, string itemId) =>
                M1CommandResult.Success();
            public M1CommandResult UnequipItem(string recruitId, string slotId) =>
                M1CommandResult.Success();
            public M1CommandResult SetEquipmentLock(
                string recruitId, string slotId, bool locked) =>
                M1CommandResult.Success();
            public M1CommandResult CompleteEquipmentReview() =>
                M1CommandResult.Success();
            public M1CommandResult AddUnion() => M1CommandResult.Success();
            public M1CommandResult RemoveUnion(int unionIndex) =>
                M1CommandResult.Success();
            public M1CommandResult AssignRecruitToUnion(
                string recruitId, int unionIndex, int slotIndex) =>
                M1CommandResult.Success();
            public M1CommandResult UnassignRecruitFromUnion(string recruitId) =>
                M1CommandResult.Success();
            public M1CommandResult SetUnionLeader(
                int unionIndex, string recruitId) => M1CommandResult.Success();
            public M1CommandResult SetFormation(
                int unionIndex, string formationId) => M1CommandResult.Success();
            public M1CommandResult SetDoctrine(
                int unionIndex, string doctrineId) => M1CommandResult.Success();
            public M1CommandResult SaveAndReloadProof() => M1CommandResult.Success();
        }
    }
}
#endif
