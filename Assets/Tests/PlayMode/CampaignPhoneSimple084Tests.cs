using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign019;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign023;
using UnityEngine.EventSystems;
using SecondDimension.Determinism;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class CampaignPhoneSimple084Tests
    {
        [UnityTearDown]
        public IEnumerator TearDownCampaignPhoneSimple084()
        {
            // The shipping presenter owns a separate screen hierarchy. Destroy
            // its component while that screen is still live, then the canvas.
            foreach (var presenter in UnityEngine.Object.FindObjectsByType<M1FlowPresenter>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None)
                     .Where(value => value != null && value.name.StartsWith(
                         "Campaign Phone Simple 084 Presenter ", StringComparison.Ordinal))
                     .ToArray())
                UnityEngine.Object.DestroyImmediate(presenter.gameObject);
            foreach (var root in UnityEngine.Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None)
                     .Where(value => value != null && value.parent == null &&
                                     value.name.StartsWith(
                                         "Campaign Phone Simple 084",
                                         StringComparison.Ordinal))
                     .ToArray())
                UnityEngine.Object.Destroy(root.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NormalRoomShowsAuthoredSceneAndOneContinueWithoutQuestPath131()
        {
            foreach (var resolution in new[]
                     {
                         new Vector2(1280f, 720f),
                         new Vector2(1280f, 800f),
                         new Vector2(1920f, 1080f)
                     })
            {
                var harness = CreateHarness084(resolution, "Forward");
                var state = ActiveState084();
                var coordinator = new Coordinator084(state);
                InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);

                var cardRoot = CardRoot129(harness.Presenter);
                Assert.That(cardRoot.parent, Is.SameAs(harness.Screen));
                Assert.That(harness.Body.gameObject.activeSelf, Is.False,
                    "The old page must not remain visible behind the story card.");
                var buttons = CardActions129(cardRoot);
                Assert.That(buttons, Has.Length.EqualTo(1));
                Assert.That(buttons[0].name,
                    Is.EqualTo("Move forward campaign room 084"));
                Assert.That(VisibleText084(CardRoot129(harness.Presenter)),
                    Does.Contain("CONTINUE"));
                Assert.That(VisibleText084(CardRoot129(harness.Presenter)),
                    Does.Contain("THE WATCH CAPTAIN’S REQUEST"));
                Assert.That(VisibleText084(CardRoot129(harness.Presenter)),
                    Does.Contain("The watch captain asks the Guild to escort the crew."));
                Assert.That(cardRoot.GetComponentsInChildren<RectTransform>(true)
                        .Count(value => value.name == "Campaign Quest Scene 131"),
                    Is.EqualTo(1), "A linear authored step remains one authored scene with its existing command.");
                Assert.That(cardRoot.GetComponentsInChildren<RectTransform>(true)
                        .Any(value => value.name.StartsWith("Campaign Compact Room ",
                            StringComparison.Ordinal)), Is.False);
                Assert.That(VisibleText084(cardRoot), Does.Not.Contain("QUEST PATH"));
                Assert.That(cardRoot.GetComponentsInChildren<Button>(true)
                    .Count(value => value.name == "Campaign Quest Return To Guild 131"), Is.EqualTo(1));
                yield return null;
                yield return null;
                AssertInside129(harness.Screen, buttons[0].GetComponent<RectTransform>(),
                    resolution + " authored scene Continue click target");
                Assert.That(CardRoot129(harness.Presenter).GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name ==
                        "Campaign Adventure Face Down Track 084"), Is.False);

                buttons[0].onClick.Invoke();
                Assert.That(coordinator.CommitStepCount, Is.EqualTo(1));
                Assert.That(coordinator.LastStepOutcome, Is.EqualTo("SUCCESS"));
                Assert.That(coordinator.ApplyStepCount, Is.EqualTo(0));
                buttons[0].onClick.Invoke();
                Assert.That(coordinator.CommitStepCount, Is.EqualTo(1),
                    "The retained old card callback must not commit a second saved step.");

                DestroyHarness129(harness);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator PendingRoomFlipsWithoutASecondCollectAction084()
        {
            var harness = CreateHarness084(new Vector2(1280f, 800f), "Reveal");
            var state = PendingState084("STORY_ADVANCED");
            var coordinator = new Coordinator084(state);
            InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);

            Assert.That(CardRoot129(harness.Presenter).GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name == "Face Down Room Card Back 084"),
                Is.True);
            Assert.That(VisibleText084(CardRoot129(harness.Presenter)),
                Does.Contain("THE WATCH CAPTAIN’S REQUEST"));
            Assert.That(VisibleText084(CardRoot129(harness.Presenter)),
                Does.Contain("+20 GUILD XP"));
            Assert.That(CardRoot129(harness.Presenter).GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name ==
                        "Board Adventure Revealed Card Type Ribbon 087 FATE"),
                Is.True);
            Assert.That(CardRoot129(harness.Presenter).GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name ==
                        "Board Adventure Resolved Reward Surface 087 FATE"),
                Is.True);
            Assert.That(CardActions129(CardRoot129(harness.Presenter)), Is.Empty,
                "The saved room receipt applies itself after the reveal.");
            Assert.That(CardRoot129(harness.Presenter).GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name == "Authoritative Dice Roll 084"),
                Is.False, "Campaign 020 has no structured saved dice to present.");
            var flip = CardRoot129(harness.Presenter).GetComponentsInChildren<RectTransform>(true)
                .Single(value => value.name == "Board Adventure Card Flip Stage 086");
            Assert.That(flip.GetSiblingIndex(), Is.EqualTo(flip.parent.childCount - 1),
                "The opaque saved-card flip must stay above the added illustration and reading viewport.");

            DestroyHarness129(harness);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DefeatReceiptAppliesOnceButNeverFinalizesAutomatically084()
        {
            var harness = CreateHarness084(new Vector2(1280f, 800f), "Defeat");
            SetReducedMotion084(harness.Presenter);
            var state = PendingState084("Defeat");
            var coordinator = new Coordinator084(state);
            InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);

            yield return new WaitForSecondsRealtime(1.0f);
            Assert.That(coordinator.ApplyStepCount, Is.EqualTo(1));
            Assert.That(state.PendingStepReceiptId, Is.Empty);
            Assert.That(coordinator.FinalizeCount, Is.EqualTo(0));
            Assert.That(coordinator.ApplyChapterCount, Is.EqualTo(0));
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(coordinator.ApplyStepCount, Is.EqualTo(1));

            DestroyHarness129(harness);
        }

        [UnityTest]
        public IEnumerator BattleStopsAndChapterReturnStayDeliberateOneActions084()
        {
            var battleHarness = CreateHarness084(
                new Vector2(1280f, 800f), "Battle");
            var battleState = ActiveState084();
            battleState.Steps[battleState.CurrentStepIndex]
                .RequiresCertifiedBattle = true;
            battleState.Steps[battleState.CurrentStepIndex].ActionLabel =
                "ENTER UNION BATTLE";
            var battleCoordinator = new Coordinator084(battleState);
            InvokeBuild084(battleHarness.Presenter, battleHarness.Body,
                battleCoordinator, battleState);
            var battleButtons = CardActions129(CardRoot129(battleHarness.Presenter));
            Assert.That(battleButtons, Has.Length.EqualTo(1));
            Assert.That(battleButtons[0].name,
                Is.EqualTo("Enter campaign battle tile 084"));
            Assert.That(battleButtons.Any(value =>
                value.name == "Move forward campaign room 084"), Is.False);
            Assert.That(battleCoordinator.CommitStepCount, Is.Zero);
            battleButtons[0].onClick.Invoke();
            Assert.That(battleCoordinator.EnterBattleCount, Is.EqualTo(1));
            Assert.That(battleCoordinator.CommitStepCount, Is.Zero);
            Assert.That(battleCoordinator.ApplyStepCount, Is.Zero);
            DestroyHarness129(battleHarness);
            yield return null;

            var finalHarness = CreateHarness084(
                new Vector2(1280f, 800f), "ChapterReturn");
            SetReducedMotion084(finalHarness.Presenter);
            var finalState = ActiveState084();
            finalState.Status = "ReadyToFinalize";
            finalState.CurrentStepIndex = finalState.TotalSteps;
            finalState.ExistingBattleRewardReferenced = true;
            foreach (var step in finalState.Steps) step.Status = "COMPLETED";
            var finalCoordinator = new Coordinator084(finalState);
            finalCoordinator.Campaign019.PendingReceiptId =
                "CHAPTER_RETURN_RECEIPT_084";
            InvokeBuild084(finalHarness.Presenter, finalHarness.Body,
                finalCoordinator, finalState);
            var finalButtons = CardActions129(CardRoot129(finalHarness.Presenter));
            Assert.That(finalButtons, Has.Length.EqualTo(1));
            Assert.That(finalButtons[0].name,
                Is.EqualTo("Complete campaign chapter 084"));
            Assert.That(VisibleText084(CardRoot129(finalHarness.Presenter)),
                Does.Contain("RETURN TO GUILD\nCOMPLETE CHAPTER"));
            Assert.That(finalButtons.Any(value => value.name ==
                "Apply campaign adventure result 084"), Is.False);

            finalButtons[0].onClick.Invoke();
            Assert.That(finalCoordinator.FinalizeCount, Is.EqualTo(1));
            Assert.That(finalCoordinator.ApplyChapterCount, Is.Zero);

            var revealBody = CreateBody084(finalHarness.Screen,
                new Vector2(1280f, 800f), "ChapterReveal");
            InvokeBuild084(finalHarness.Presenter, revealBody,
                finalCoordinator, finalState);
            Assert.That(CardActions129(CardRoot129(finalHarness.Presenter)), Is.Empty);
            Assert.That(VisibleText084(CardRoot129(finalHarness.Presenter)),
                Does.Contain("RETURN-HOME CARD FLIPPED"));
            yield return new WaitForSecondsRealtime(1.0f);
            Assert.That(finalCoordinator.ApplyChapterCount, Is.EqualTo(1));
            Assert.That(finalCoordinator.Campaign019.PendingReceiptId, Is.Empty);
            Assert.That(finalState.ActiveOperationId, Is.Empty);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(finalCoordinator.ApplyChapterCount, Is.EqualTo(1));

            DestroyHarness129(finalHarness);
        }

        [UnityTest]
        public IEnumerator PersistedNonBattleChapterReceiptResumesExactlyOnce084()
        {
            var harness = CreateHarness084(
                new Vector2(1280f, 800f), "ReloadedChapterReceipt");
            SetReducedMotion084(harness.Presenter);
            var state = ActiveState084();
            state.Status = "ReadyToFinalize";
            state.CurrentStepIndex = state.TotalSteps;
            state.ExistingBattleRewardReferenced = false;
            foreach (var step in state.Steps) step.Status = "COMPLETED";
            var coordinator = new Coordinator084(state);
            coordinator.Campaign019.PendingReceiptId =
                "PERSISTED_NONBATTLE_CHAPTER_RECEIPT_084";

            InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);
            Assert.That(CardActions129(CardRoot129(harness.Presenter)), Is.Empty,
                "A persisted committed chapter receipt resumes without another click.");
            Assert.That(VisibleText084(CardRoot129(harness.Presenter)),
                Does.Contain("RETURN-HOME CARD FLIPPED"));
            Assert.That(coordinator.FinalizeCount, Is.Zero,
                "The persisted finalization receipt must not be recommitted.");

            yield return new WaitForSecondsRealtime(1.0f);
            Assert.That(coordinator.ApplyChapterCount, Is.EqualTo(1));
            Assert.That(coordinator.Campaign019.PendingReceiptId, Is.Empty);
            Assert.That(state.ActiveOperationId, Is.Empty);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(coordinator.ApplyChapterCount, Is.EqualTo(1));
            Assert.That(coordinator.FinalizeCount, Is.Zero);

            DestroyHarness129(harness);
        }

        [UnityTest]
        public IEnumerator LegacyAlternateCoordinatorWorldBoardFallbackDoesNotCommitAnotherStoryStep131()
        {
            var harness = CreateHarness084(new Vector2(1280f, 720f), "WorldBoard");
            var state = ActiveState084();
            state.Steps[state.CurrentStepIndex].IsWorldBoard = true;
            state.Steps[state.CurrentStepIndex].Title = "Commit Route and Board";
            var coordinator = new Coordinator084(state);
            InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);
            var root = CardRoot129(harness.Presenter);
            var actions = CardActions129(root);
            Assert.That(actions, Has.Length.EqualTo(1));
            Assert.That(actions[0].name, Is.EqualTo("Open campaign adventure board 084"));
            Assert.That(root.GetComponentsInChildren<RectTransform>(true)
                .Any(value => value.name == "Sealed Story Card 129"), Is.False);
            actions[0].onClick.Invoke();
            Assert.That(typeof(M1FlowPresenter).GetField("_guildCityTab017D",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(harness.Presenter),
                Is.EqualTo("WORLD GATE"));
            Assert.That(coordinator.CommitStepCount, Is.Zero);
            Assert.That(coordinator.ApplyStepCount, Is.Zero);
            Assert.That(coordinator.EnterBattleCount, Is.Zero);
            Assert.That(coordinator.FinalizeCount, Is.Zero);
            Assert.That(coordinator.ApplyChapterCount, Is.Zero);
            DestroyHarness129(harness);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClosingStepAndChapterCardsCancelsTimersAndReopeningResumesExactlyOnce129()
        {
            foreach (var chapter in new[] { false, true })
            {
                var harness = CreateHarness084(new Vector2(1280f, 800f),
                    chapter ? "ClosedChapter" : "ClosedStep");
                SetReducedMotion084(harness.Presenter);
                var coordinator = PendingCoordinator129(chapter);
                var state = coordinator.CampaignPlayable020;
                var receipt = PendingReceipt129(coordinator, chapter);
                InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);
                var first = CardRoot129(harness.Presenter);
                Assert.That(ScheduledReceipts129(harness.Presenter, chapter),
                    Does.Contain(receipt));
                first.gameObject.SetActive(false);
                Assert.That(ScheduledReceipts129(harness.Presenter, chapter), Is.Empty,
                    "Closing the card releases its presentation timer, not its durable receipt.");
                yield return new WaitForSecondsRealtime(1.0f);
                Assert.That(coordinator.ApplyStepCount + coordinator.ApplyChapterCount, Is.Zero);
                Assert.That(PendingReceipt129(coordinator, chapter), Is.EqualTo(receipt));

                InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);
                Assert.That(CardRoot129(harness.Presenter), Is.Not.SameAs(first));
                yield return new WaitForSecondsRealtime(1.0f);
                Assert.That(chapter ? coordinator.ApplyChapterCount : coordinator.ApplyStepCount,
                    Is.EqualTo(1));
                Assert.That(PendingReceipt129(coordinator, chapter), Is.Empty);
                Assert.That(ScheduledReceipts129(harness.Presenter, chapter), Is.Empty);
                Assert.That(coordinator.CommitStepCount, Is.Zero);
                Assert.That(coordinator.FinalizeCount, Is.Zero);
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(coordinator.ApplyStepCount + coordinator.ApplyChapterCount, Is.EqualTo(1));
                DestroyHarness129(harness);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator StaleStepAndChapterCardsCannotApplyAReplacementReceipt129()
        {
            foreach (var chapter in new[] { false, true })
            {
                var harness = CreateHarness084(new Vector2(1280f, 800f),
                    chapter ? "StaleChapter" : "StaleStep");
                SetReducedMotion084(harness.Presenter);
                var coordinator = PendingCoordinator129(chapter);
                var state = coordinator.CampaignPlayable020;
                InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);
                if (chapter) coordinator.Campaign019.PendingReceiptId = "REPLACEMENT_CHAPTER_129";
                else state.PendingStepReceiptId = "REPLACEMENT_STEP_129";
                var replacement = PendingReceipt129(coordinator, chapter);
                yield return new WaitForSecondsRealtime(1.0f);
                Assert.That(coordinator.ApplyStepCount + coordinator.ApplyChapterCount, Is.Zero);
                Assert.That(PendingReceipt129(coordinator, chapter), Is.EqualTo(replacement));
                Assert.That(ScheduledReceipts129(harness.Presenter, chapter), Is.Empty,
                    "A stale timer cannot leave its former receipt permanently scheduled.");
                InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);
                yield return new WaitForSecondsRealtime(1.0f);
                Assert.That(coordinator.ApplyStepCount + coordinator.ApplyChapterCount, Is.EqualTo(1));
                Assert.That(PendingReceipt129(coordinator, chapter), Is.Empty);
                Assert.That(coordinator.CommitStepCount, Is.Zero);
                Assert.That(coordinator.FinalizeCount, Is.Zero);
                DestroyHarness129(harness);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CompletedStoryCycleMissionsShowsReplayButtonBeforeDeliberateStart130()
        {
            var harness = CreateHarness084(new Vector2(1280f, 800f), "CompletedCycleMissions130");
            var coordinator = new AttachedCampaignCoordinator129(
                new Coordinator084(new CampaignPlayablePresentationState020 { IsAvailable = true }));
            // A completed UI projection only: no save, chapter reward or replay
            // authority is created. Real replay completion is covered separately.
            coordinator.Campaign019.CanStartNextCycle130 = true;
            coordinator.Campaign019.CurrentCycle130 = 1;
            coordinator.Campaign019.CycleCompleted130 = 82;
            coordinator.Campaign019.Chapters = Enumerable.Range(1, 82).Select(index =>
                new CampaignChapterView019 { ChapterId = "CH018_" + index.ToString("000"),
                    Completed = true, Status = "COMPLETED" }).ToArray();
            coordinator.GuildCity017D.Contracts = new[] {
                "CONTRACT_BELL_BENEATH_GATE", "CONTRACT_LINES_NOT_RETURNED", "CONTRACT_RELIEF_ROAD"
            }.Select(id => new GuildCityContractView017D { ContractId = id, DisplayName = id,
                IsCompleted = true }).ToArray();
            typeof(M1FlowPresenter).GetField("_screen", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(harness.Presenter, M1Screen.GuildOperations);
            // Attach on the supported story projection. The inert fixture has
            // no recruitment projection for constructing the full Contracts page.
            typeof(M1FlowPresenter).GetField("_guildCityTab017D", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(harness.Presenter, "CAMPAIGN");
            typeof(M1FlowPresenter).GetMethod("Attach", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(harness.Presenter, new object[] { coordinator });
            var previousStart = harness.Screen.GetComponentsInChildren<Button>(false)
                .Single(button => button.name == "Start Next Story Cycle 130");
            // Change only the starting tab, without building that unrelated page.
            // OpenMissions must choose and build Campaign again by itself.
            typeof(M1FlowPresenter).GetField("_guildCityTab017D", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(harness.Presenter, "CONTRACTS");
            typeof(M1FlowPresenter).GetMethod("OpenMissions084", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(harness.Presenter, null);
            yield return null;
            yield return null;
            Assert.That(typeof(M1FlowPresenter).GetField("_guildCityTab017D",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(harness.Presenter), Is.EqualTo("CAMPAIGN"));
            var start = harness.Screen.GetComponentsInChildren<Button>(false)
                .Single(button => button.name == "Start Next Story Cycle 130");
            Assert.That(start, Is.Not.SameAs(previousStart), "The actual Missions route rebuilt its own Campaign page.");
            Assert.That(start.gameObject.activeInHierarchy && start.IsInteractable(), Is.True);
            Assert.That(start.GetComponentInChildren<Text>().text, Is.EqualTo("START CYCLE 2"));
            Assert.That(harness.Screen.GetComponentsInChildren<Button>(false)
                .Any(button => button.name == "Guild City Tab CONTRACTS"), Is.True);
            Assert.That(coordinator.ReplayStarts130, Is.Zero, "Opening Missions is navigation, not a replay command.");
            start.onClick.Invoke();
            Assert.That(coordinator.ReplayStarts130, Is.EqualTo(1));
            Assert.That(coordinator.ReplayExpectedCycle130, Is.EqualTo(1));
            Assert.That(coordinator.ReplayGrowth130, Is.EqualTo(25));
            Assert.That(coordinator.Campaign019.Chapters.All(chapter => chapter.Completed), Is.True);
            DestroyHarness129(harness);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnablingPresenterAloneResumesVisibleStepAndChapterExactlyOnce129()
        {
            foreach (var chapter in new[] { false, true })
            {
                var harness = CreateHarness084(new Vector2(1280f, 800f),
                    chapter ? "DisabledChapter" : "DisabledStep");
                SetReducedMotion084(harness.Presenter);
                var coordinator = new AttachedCampaignCoordinator129(PendingCoordinator129(chapter));
                var state = coordinator.CampaignPlayable020;
                var receipt = PendingReceipt129(coordinator, chapter);
                // Attach the owner and use the actual GuildOperations/Campaign
                // router. No synthetic builder call is allowed after enabling.
                typeof(M1FlowPresenter).GetField("_screen",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(harness.Presenter, M1Screen.GuildOperations);
                typeof(M1FlowPresenter).GetField("_guildCityTab017D",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(harness.Presenter, "CAMPAIGN");
                var attach = typeof(M1FlowPresenter).GetMethod("Attach",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(attach, Is.Not.Null);
                attach.Invoke(harness.Presenter, new object[] { coordinator });
                var firstCard = CardRoot129(harness.Presenter);
                Assert.That(firstCard.gameObject.activeInHierarchy, Is.True);
                Assert.That(ScheduledReceipts129(harness.Presenter, chapter), Does.Contain(receipt));
                harness.Presenter.enabled = false;
                Assert.That(ScheduledReceipts129(harness.Presenter, chapter), Is.Empty);
                yield return new WaitForSecondsRealtime(1.0f);
                Assert.That(coordinator.ApplyStepCount + coordinator.ApplyChapterCount, Is.Zero);
                Assert.That(PendingReceipt129(coordinator, chapter), Is.EqualTo(receipt));
                harness.Presenter.enabled = true;
                Assert.That(CardRoot129(harness.Presenter), Is.Not.SameAs(firstCard),
                    "OnEnable must rebuild the still-visible saved story card by itself.");
                Assert.That(ScheduledReceipts129(harness.Presenter, chapter), Does.Contain(receipt));
                Assert.That(PendingReceipt129(coordinator, chapter), Is.EqualTo(receipt));
                Assert.That(coordinator.ApplyStepCount + coordinator.ApplyChapterCount, Is.Zero,
                    "Re-enabling resumes presentation; it must not apply the saved receipt immediately.");
                yield return new WaitForSecondsRealtime(1.0f);
                Assert.That(coordinator.ApplyStepCount + coordinator.ApplyChapterCount, Is.EqualTo(1));
                Assert.That(PendingReceipt129(coordinator, chapter), Is.Empty);
                Assert.That(coordinator.CommitStepCount, Is.Zero);
                Assert.That(coordinator.FinalizeCount, Is.Zero);
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(coordinator.ApplyStepCount + coordinator.ApplyChapterCount, Is.EqualTo(1));
                DestroyHarness129(harness);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator FailedStepAndChapterSaveWaitForExplicitRetryWithoutRecommitting129()
        {
            foreach (var chapter in new[] { false, true })
            {
                var harness = CreateHarness084(new Vector2(1280f, 800f),
                    chapter ? "RetryChapter" : "RetryStep");
                SetReducedMotion084(harness.Presenter);
                var coordinator = PendingCoordinator129(chapter);
                coordinator.FailNextApply = true;
                var state = coordinator.CampaignPlayable020;
                var receipt = PendingReceipt129(coordinator, chapter);
                InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);
                yield return new WaitForSecondsRealtime(1.0f);
                Assert.That(coordinator.ApplyStepCount + coordinator.ApplyChapterCount, Is.EqualTo(1));
                Assert.That(PendingReceipt129(coordinator, chapter), Is.EqualTo(receipt));
                // Rebuild only the same production partial in this isolated fixture.
                InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);
                var retry = CardRoot129(harness.Presenter).GetComponentsInChildren<Button>()
                    .Single(value => value.name == "Story Card Skip 129");
                Assert.That(retry.GetComponentInChildren<Text>().text, Is.EqualTo("RETRY SAVE"));
                Assert.That(retry.IsInteractable(), Is.True);
                Assert.That(ScheduledReceipts129(harness.Presenter, chapter), Is.Empty);
                yield return new WaitForSecondsRealtime(1.0f);
                Assert.That(coordinator.ApplyStepCount + coordinator.ApplyChapterCount, Is.EqualTo(1),
                    "A failed save must not spin in an automatic retry loop.");
                retry.onClick.Invoke();
                Assert.That(coordinator.ApplyStepCount + coordinator.ApplyChapterCount, Is.EqualTo(1),
                    "Retry uses the same delayed saved-receipt apply path.");
                InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);
                yield return new WaitForSecondsRealtime(1.0f);
                Assert.That(coordinator.ApplyStepCount + coordinator.ApplyChapterCount, Is.EqualTo(2));
                Assert.That(PendingReceipt129(coordinator, chapter), Is.Empty);
                Assert.That(coordinator.CommitStepCount, Is.Zero);
                Assert.That(coordinator.FinalizeCount, Is.Zero);
                DestroyHarness129(harness);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SkipStoryCardAnimationKeepsSavedOutcomeAndDoesNotApplyEarly129()
        {
            var harness = CreateHarness084(new Vector2(1280f, 800f), "Skip");
            var coordinator = PendingCoordinator129(false);
            var state = coordinator.CampaignPlayable020;
            var receipt = state.PendingStepReceiptId;
            var outcome = state.PendingOutcome;
            var reward = state.PendingReward;
            InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);
            var skip = CardRoot129(harness.Presenter).GetComponentsInChildren<Button>()
                .Single(value => value.name == "Story Card Skip 129");
            Assert.That(skip.IsInteractable(), Is.True);
            skip.onClick.Invoke();
            Assert.That(state.PendingStepReceiptId, Is.EqualTo(receipt));
            Assert.That(state.PendingOutcome, Is.EqualTo(outcome));
            Assert.That(state.PendingReward, Is.EqualTo(reward));
            Assert.That(coordinator.ApplyStepCount, Is.Zero);
            Assert.That(coordinator.CommitStepCount, Is.Zero);
            InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);
            Assert.That(CardRoot129(harness.Presenter).GetComponentsInChildren<Button>()
                .Single(value => value.name == "Story Card Skip 129").IsInteractable(), Is.False);
            Assert.That(coordinator.ApplyStepCount, Is.Zero);
            yield return new WaitForSecondsRealtime(1.0f);
            Assert.That(coordinator.ApplyStepCount, Is.EqualTo(1));
            Assert.That(coordinator.CommitStepCount, Is.Zero);
            DestroyHarness129(harness);
        }

        [UnityTest]
        public IEnumerator StoryScenesAndSavedFacesKeepArtReadableTitlesAndOriginalActions131()
        {
            foreach (var resolution in new[] { new Vector2(1280f, 720f), new Vector2(1280f, 800f) })
            foreach (var kind in new[] { "pending-step", "pending-return", "world-board", "battle", "return" })
            {
                var harness = CreateHarness084(resolution, "Illustrated-" + kind);
                SetReducedMotion084(harness.Presenter);
                var coordinator = kind == "pending-step" ? PendingCoordinator129(false) :
                    kind == "pending-return" || kind == "return" ? PendingCoordinator129(true) :
                    new Coordinator084(ActiveState084());
                var state = coordinator.CampaignPlayable020;
                string expectedAction = null;
                if (kind == "world-board")
                {
                    var current = state.Steps[state.CurrentStepIndex];
                    current.IsWorldBoard = true;
                    current.Kind = "WORLD_BOARD";
                    current.Title = "Commit Route and Board";
                    expectedAction = "Open campaign adventure board 084";
                }
                else if (kind == "battle")
                {
                    var current = state.Steps[state.CurrentStepIndex];
                    current.RequiresCertifiedBattle = true;
                    current.Kind = "BATTLE";
                    current.Title = "The Gate Watch";
                    current.ActionLabel = "ENTER UNION BATTLE";
                    expectedAction = "Enter campaign battle tile 084";
                }
                else if (kind == "return")
                {
                    state.ExistingBattleRewardReferenced = true;
                    expectedAction = "Complete campaign chapter 084";
                }
                var stepReceipt = state.PendingStepReceiptId;
                var chapterReceipt = coordinator.Campaign019.PendingReceiptId;
                if (kind == "pending-step" || kind == "pending-return")
                {
                    // Use the existing waiting-retry presentation so resource
                    // loading/layout has no race against an automatic hold.
                    // Dedicated lifecycle tests retain strict timer assertions.
                    typeof(M1FlowPresenter).GetField("_campaignCardRetryReceipt129",
                        BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(harness.Presenter,
                            kind == "pending-step" ? stepReceipt : chapterReceipt);
                }
                InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);
                Assert.That(coordinator.CommitStepCount + coordinator.ApplyStepCount +
                    coordinator.FinalizeCount + coordinator.ApplyChapterCount + coordinator.EnterBattleCount, Is.Zero);
                yield return null;
                yield return null;

                var root = CardRoot129(harness.Presenter);
                var illustration = root.GetComponentsInChildren<RectTransform>(true)
                    .Single(value => value.name == "Board Card Illustration Window 091");
                var art = illustration.GetComponentsInChildren<Image>(true)
                    .Single(value => value.name == "Campaign Quest Illustration 131");
                Assert.That(art.sprite, Is.Not.Null, kind + " uses an existing bound room illustration");
                Assert.That(art.gameObject.activeInHierarchy, Is.True);
                Assert.That(art.color.a, Is.GreaterThan(0.99f));
                Assert.That(illustration.GetComponent<RectMask2D>(), Is.Not.Null);
                Assert.That(illustration.rect.width, Is.GreaterThan(100f), kind + " art has a visible area");
                Assert.That(illustration.rect.height, Is.GreaterThan(100f));

                var reading = root.GetComponentsInChildren<RectTransform>(true)
                    .Single(value => value.name == "Board Card Reading Column 091");
                var scroll = reading.GetComponentInParent<ScrollRect>();
                // Opening-style131 scenes use a bounded reading column;
                // dense saved-event layouts may still provide a ScrollRect.
                if (scroll != null)
                {
                    Assert.That(scroll.content, Is.SameAs(reading));
                    Assert.That(scroll.vertical, Is.True);
                    Assert.That(scroll.horizontal, Is.False);
                }
                var readingViewport = scroll != null ? scroll.viewport : reading;
                Assert.That(readingViewport, Is.Not.Null);
                AssertInside129(harness.Screen, readingViewport, kind + " reading viewport");
                var title = reading.GetComponentsInChildren<Text>(true)
                    .Single(value => value.name == "Title" || value.name.StartsWith("Title [", StringComparison.Ordinal) ||
                        value.name.StartsWith("Campaign Quest Scene Title 131", StringComparison.Ordinal));
                Assert.That(title.text, Is.Not.Null.And.Not.Empty);
                Assert.That(title.fontSize, Is.GreaterThanOrEqualTo(24),
                    "The illustrated scene keeps an authored readable title, not the old full-screen 44px heading.");
                Assert.That(title.transform.IsChildOf(reading), Is.True);
                AssertInside129(readingViewport, title.rectTransform, kind + " initial title");
                var titleBounds = WorldBounds129(title.rectTransform);
                var artBounds = WorldBounds129(illustration);
                var ribbons = root.GetComponentsInChildren<RectTransform>(true).Where(value =>
                    value.name.StartsWith("Board Adventure Revealed Card Type Ribbon 087 ", StringComparison.Ordinal)).ToArray();
                Assert.That(ribbons.All(value => !value.gameObject.activeInHierarchy), Is.True,
                    "The authored title replaces the redundant generic category ribbon.");
                Assert.That(titleBounds.xMin, Is.GreaterThanOrEqualTo(artBounds.xMax - 0.5f),
                    kind + " title must occupy the reading column beside the illustration");


                var actions = CardActions129(root);
                Assert.That(actions.Length, Is.EqualTo(expectedAction == null ? 0 : 1));
                if (expectedAction != null)
                {
                    var action = actions.Single();
                    Assert.That(action.name, Is.EqualTo(expectedAction));
                    Assert.That(action.transform.IsChildOf(reading), Is.True);
                    Assert.That(action.IsInteractable(), Is.True);
                    if (scroll != null && !ContainsRect129(readingViewport, action.GetComponent<RectTransform>()))
                    {
                        // One ordinary ScrollRect movement must expose the
                        // original button; no transform/button forcing or click.
                        scroll.verticalNormalizedPosition = 0f;
                        yield return null;
                    }
                    AssertInside129(readingViewport, action.GetComponent<RectTransform>(),
                        kind + " existing action fits the bounded column or is reachable by one available scroll");
                    AssertInside129(harness.Screen, action.GetComponent<RectTransform>(),
                        kind + " action remains within the screen");
                }
                Assert.That(coordinator.CommitStepCount + coordinator.ApplyStepCount +
                    coordinator.FinalizeCount + coordinator.ApplyChapterCount + coordinator.EnterBattleCount, Is.Zero,
                    "Reading the new illustration/layout must not dispatch a command before deliberate input or the saved reveal hold.");
                Assert.That(state.PendingStepReceiptId, Is.EqualTo(stepReceipt));
                Assert.That(coordinator.Campaign019.PendingReceiptId, Is.EqualTo(chapterReceipt));
                DestroyHarness129(harness);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ChapterDeckUsesThreeExistingCardsAndPointerSelectionWithoutAnotherStart131()
        {
            foreach (var resolution in new[] { new Vector2(1280f, 720f), new Vector2(1920f, 1080f) })
            {
                var harness = CreateHarness084(resolution, "EmbeddedDeck131");
                SetReducedMotion084(harness.Presenter);
                AddPointerCamera131(harness, resolution);
                var state = ActiveState084();
                state.ActiveChapterId = "CH018_002";
                state.WorldId = "SKYHOME";
                state.Steps[state.CurrentStepIndex].Kind = "WORLD_BOARD";
                state.Steps[state.CurrentStepIndex].IsWorldBoard = true;
                var owner = new DeckCoordinator131(state);
                owner.CampaignWorldGate023.ExpeditionDeckTutorialSeen = resolution.x > 1280f;
                var snapshot = CanonicalJson.Serialize(owner.CampaignWorldGate023);
                var storySnapshot = CanonicalJson.Serialize(state);
                InvokeBuild084(harness.Presenter, harness.Body, owner, state);
                yield return null;
                yield return null;
                var root = QuestRoot131(harness.Presenter);
                var table = root.Find("Campaign Quest Card Table 131") as RectTransform;
                Assert.That(table, Is.Not.Null);
                Assert.That(harness.Body.gameObject.activeInHierarchy, Is.False);
                AssertInside129(harness.Screen, table, resolution + " chapter table");
                var row = table.Find("Expedition route row 089") as RectTransform;
                Assert.That(row, Is.Not.Null, "Use the existing 023 three-card row inside the actual chapter shell.");
                var cards = row.GetComponentsInChildren<Button>(false)
                    .Where(value => value.name.StartsWith("Blind Quest Card Back ", StringComparison.Ordinal))
                    .OrderBy(value => value.name, StringComparer.Ordinal).ToArray();
                Assert.That(cards, Has.Length.EqualTo(3));
                Assert.That(row.rect.height, Is.GreaterThan(table.rect.height * 0.70f));
                var bounds = cards.Select(value => WorldBounds129((RectTransform)value.transform)).ToArray();
                foreach (var card in cards)
                {
                    Assert.That(card.IsInteractable(), Is.True);
                    AssertInside129(table, (RectTransform)card.transform, resolution + " whole blind card");
                    AssertInside129((RectTransform)card.transform, card.GetComponentInChildren<Text>().rectTransform,
                        resolution + " PICK THIS CARD label");
                    Assert.That(card.GetComponent<Image>().sprite, Is.Not.Null);
                }
                Assert.That(bounds[0].Overlaps(bounds[1]) || bounds[1].Overlaps(bounds[2]) || bounds[0].Overlaps(bounds[2]), Is.False);
                Assert.That(VisibleText084(root), Does.Not.Contain("OPEN EXPEDITION CARDS"));
                var visible = string.Join("\n", root.GetComponentsInChildren<Text>(false).Select(value => value.text));
                Assert.That(visible, Does.Not.Contain("The Crew’s Lantern"));
                Assert.That(visible, Does.Not.Contain("A light guides the crew onward."));
                Assert.That(root.GetComponentsInChildren<Button>(false).Any(value =>
                    value.name == "Open campaign adventure board 084" || value.name == "Resume saved campaign deck 131" ||
                    value.name.StartsWith("Start playable chapter", StringComparison.Ordinal) ||
                    (value.GetComponentInChildren<Text>()?.text ?? string.Empty).Trim().StartsWith("START ", StringComparison.Ordinal)), Is.False);
                Assert.That(owner.DeckCommits, Is.Zero);
                Assert.That(CanonicalJson.Serialize(owner.CampaignWorldGate023), Is.EqualTo(snapshot));
                Assert.That(CanonicalJson.Serialize(state), Is.EqualTo(storySnapshot));

                // A real hit-tested blind choice is presentation only. The
                // existing revealed action then submits its exact saved CardId.
                Click131(cards[1]);
                yield return null;
                yield return null;
                Assert.That(owner.DeckCommits, Is.Zero);
                Assert.That(CanonicalJson.Serialize(owner.CampaignWorldGate023), Is.EqualTo(snapshot));
                var choice = row.GetComponent<ExpeditionCardChoice091>();
                Assert.That(choice.SelectedIndex091, Is.EqualTo(1));
                Assert.That(choice.Phase091, Is.EqualTo(ExpeditionCardChoice091.ChoicePhase091.AwaitingAction));
                var action = row.GetComponentsInChildren<Button>(false).Single(value =>
                    value.name == "Choose Expedition route card CARD_CHEST_131 089");
                Assert.That(action.IsInteractable(), Is.True);
                AssertInside129(table, (RectTransform)action.transform, resolution + " revealed legal action");
                Click131(action);
                Assert.That(owner.DeckCommits, Is.EqualTo(1));
                Assert.That(owner.SelectedCardId, Is.EqualTo("CARD_CHEST_131"));
                action.onClick.Invoke();
                Assert.That(owner.DeckCommits, Is.EqualTo(1), "The old submitted card cannot dispatch again.");
                Assert.That(owner.CommitStepCount + owner.ApplyStepCount + owner.FinalizeCount + owner.ApplyChapterCount, Is.Zero);
                Assert.That(CanonicalJson.Serialize(state), Is.EqualTo(storySnapshot));
                DestroyHarness129(harness);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ReopeningChapterDeckRebuildsOnlySavedRowWithoutCommitting131()
        {
            var harness = CreateHarness084(new Vector2(1280f, 720f), "DeckReload131");
            SetReducedMotion084(harness.Presenter);
            AddPointerCamera131(harness, new Vector2(1280f, 720f));
            var state = ActiveState084();
            state.WorldId = "SKYHOME";
            state.Steps[state.CurrentStepIndex].Kind = "WORLD_BOARD";
            state.Steps[state.CurrentStepIndex].IsWorldBoard = true;
            var owner = new DeckCoordinator131(state);
            var savedProjection = CanonicalJson.Serialize(owner.CampaignWorldGate023);
            InvokeBuild084(harness.Presenter, harness.Body, owner, state);
            yield return null;
            yield return null;
            var previous = QuestRoot131(harness.Presenter);
            var card = previous.GetComponentsInChildren<Button>(false).Single(value => value.name == "Blind Quest Card Back 0 091");
            Click131(card);
            yield return null;
            yield return null;
            Assert.That(owner.DeckCommits, Is.Zero);
            Assert.That(CanonicalJson.Serialize(owner.CampaignWorldGate023), Is.EqualTo(savedProjection));
            previous.gameObject.SetActive(false);
            // Fresh presentation owner rebuilt from the same fixed projection;
            // canonical equality covers its whole saved view. This tests UI
            // reattachment, not disk deserialization or fabricated save authority.
            var reloaded = new DeckCoordinator131(state);
            InvokeBuild084(harness.Presenter, harness.Body, reloaded, state);
            yield return null;
            yield return null;
            var reopened = QuestRoot131(harness.Presenter);
            Assert.That(reopened, Is.Not.SameAs(previous));
            Assert.That(reopened.GetComponentsInChildren<Button>(false).Count(value =>
                value.name.StartsWith("Blind Quest Card Back ", StringComparison.Ordinal) && value.IsInteractable()), Is.EqualTo(3));
            Assert.That(CanonicalJson.Serialize(reloaded.CampaignWorldGate023), Is.EqualTo(savedProjection));
            Assert.That(owner.DeckCommits + reloaded.DeckCommits, Is.Zero);
            Assert.That(owner.CommitStepCount + owner.ApplyStepCount + reloaded.CommitStepCount + reloaded.ApplyStepCount, Is.Zero);
            DestroyHarness129(harness);
            yield return null;
        }

        static RectTransform QuestRoot131(M1FlowPresenter presenter) => (RectTransform)
            typeof(M1FlowPresenter).GetField("_campaignQuestRoot131", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(presenter);

        static void AddPointerCamera131(Harness084 harness, Vector2 resolution)
        {
            var cameraObject = new GameObject("Campaign Phone Simple 084 Camera131", typeof(Camera));
            cameraObject.transform.SetParent(harness.Root.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = resolution.y / 2f;
            camera.aspect = resolution.x / resolution.y;
            harness.Root.GetComponent<Canvas>().worldCamera = camera;
            if (EventSystem.current == null)
                new GameObject("Campaign Phone Simple 084 Events131", typeof(EventSystem));
        }

        static void Click131(Button button)
        {
            Assert.That(button.gameObject.activeInHierarchy && button.IsInteractable(), Is.True);
            var canvas = button.GetComponentInParent<Canvas>();
            var rect = (RectTransform)button.transform;
            var point = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rect.TransformPoint(rect.rect.center));
            Assert.That(point.x, Is.InRange(0f, (float)Screen.width));
            Assert.That(point.y, Is.InRange(0f, (float)Screen.height));
            var pointer = new PointerEventData(EventSystem.current) { position = point, pressPosition = point,
                pointerId = -1, button = PointerEventData.InputButton.Left, eligibleForClick = true, clickCount = 1 };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            var hit = hits[0].gameObject;
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit), Is.EqualTo(button.gameObject),
                "The actual pointer hit is intercepted by " + hit.name);
            pointer.pointerCurrentRaycast = pointer.pointerPressRaycast = hits[0];
            pointer.pointerPress = ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
            Assert.That(pointer.pointerPress, Is.EqualTo(button.gameObject));
            ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerClickHandler);
        }

        static Rect WorldBounds129(RectTransform value)
        {
            var corners = new Vector3[4];
            value.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners.Min(point => point.x), corners.Min(point => point.y),
                corners.Max(point => point.x), corners.Max(point => point.y));
        }

        static bool ContainsRect129(RectTransform outer, RectTransform inner)
        {
            var a = WorldBounds129(outer);
            var b = WorldBounds129(inner);
            return b.xMin >= a.xMin - 0.5f && b.xMax <= a.xMax + 0.5f &&
                b.yMin >= a.yMin - 0.5f && b.yMax <= a.yMax + 0.5f;
        }

        static Coordinator084 PendingCoordinator129(bool chapter)
        {
            var state = chapter ? ActiveState084() : PendingState084("STORY_ADVANCED");
            var coordinator = new Coordinator084(state);
            if (chapter)
            {
                state.Status = "ReadyToFinalize";
                state.CurrentStepIndex = state.TotalSteps;
                state.ExistingBattleRewardReferenced = false;
                foreach (var step in state.Steps) step.Status = "COMPLETED";
                coordinator.Campaign019.PendingReceiptId = "SAVED_CHAPTER_129";
            }
            return coordinator;
        }

        static string PendingReceipt129(Coordinator084 coordinator, bool chapter) =>
            chapter ? coordinator.Campaign019.PendingReceiptId :
                coordinator.CampaignPlayable020.PendingStepReceiptId;

        static HashSet<string> ScheduledReceipts129(M1FlowPresenter presenter, bool chapter) =>
            (HashSet<string>)typeof(M1FlowPresenter).GetField(chapter
                    ? "_scheduledCampaignChapterReceipts084" : "_scheduledCampaignStepReceipts084",
                BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(presenter);

        static CampaignPlayablePresentationState020 ActiveState084()
        {
            var steps = Enumerable.Range(1, 8)
                .Select(index => new CampaignStepView020
                {
                    StepId = "STEP_" + index,
                    Kind = "CIVIC_EVENT",
                    TileType = "STORY",
                    ActionLabel = "RESOLVE STORY",
                    RewardPreview = "+20 GUILD XP",
                    Title = index == 4
                        ? "THE WATCH CAPTAIN’S REQUEST"
                        : "Room " + index,
                    Description = index == 4
                        ? "The watch captain asks the Guild to escort the crew."
                        : "The Guild advances through the story.",
                    Status = index < 4 ? "COMPLETED" :
                             index == 4 ? "CURRENT" : "FACE_DOWN"
                })
                .ToArray();
            return new CampaignPlayablePresentationState020
            {
                IsAvailable = true,
                ActiveOperationId = "CAMPAIGN_PHONE_SIMPLE_084",
                ActiveChapterId = "CHAPTER_PHONE_SIMPLE_084",
                OperationTitle = "The Lantern Road",
                WorldName = "Skyhome",
                Status = "Active",
                CurrentStepIndex = 3,
                TotalSteps = steps.Length,
                Steps = steps
            };
        }

        static CampaignPlayablePresentationState020 PendingState084(string outcome)
        {
            var state = ActiveState084();
            state.PendingStepReceiptId = "CAMPAIGN_STEP_RECEIPT_084";
            state.PendingOutcome = outcome;
            state.PendingReward = "+20 GUILD XP  •  +10 HALL XP";
            return state;
        }

        static Harness084 CreateHarness084(Vector2 resolution, string suffix)
        {
            var root = new GameObject(
                "Campaign Phone Simple 084 " + suffix,
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            root.GetComponent<RectTransform>().sizeDelta = resolution;
            var screen = new GameObject("Campaign Story Screen 129", typeof(RectTransform))
                .GetComponent<RectTransform>();
            screen.SetParent(root.transform, false);
            screen.anchorMin = Vector2.zero;
            screen.anchorMax = Vector2.one;
            screen.offsetMin = new Vector2(96f, 42f);
            screen.offsetMax = new Vector2(-96f, -42f);
            var presenterObject = new GameObject("Campaign Phone Simple 084 Presenter " + suffix);
            var presenter = presenterObject.AddComponent<M1FlowPresenter>();
            // This fixture invokes only the Campaign partial builder. Give Start
            // an existing canvas and an inert owner so it cannot load a profile
            // from the global registry during the rendered-frame waits.
            typeof(M1FlowPresenter).GetField("_canvas",
                BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(presenter, root.GetComponent<Canvas>());
            typeof(M1FlowPresenter).GetField("_coordinator",
                BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(presenter, new PageOnlyCoordinator129());
            // Keep unrelated page routing outside this partial-builder fixture;
            // the real Home -> Story -> Home regression covers that navigation.
            typeof(M1FlowPresenter).GetField("_screen",
                BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(presenter, (M1Screen)(-1));
            var body = CreateBody084(screen, resolution, "Main");
            typeof(M1FlowPresenter).GetField("_screenRoot",
                BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(presenter, screen);
            typeof(M1FlowPresenter).GetField("_activePage",
                BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(presenter, body);
            return new Harness084(root, presenter, body, screen);
        }

        static RectTransform CreateBody084(
            Transform parent,
            Vector2 resolution,
            string suffix)
        {
            var body = new GameObject(
                    "Campaign Phone Simple Body 084 " + suffix,
                    typeof(RectTransform), typeof(VerticalLayoutGroup),
                    typeof(ContentSizeFitter))
                .GetComponent<RectTransform>();
            body.SetParent(parent, false);
            body.anchorMin = new Vector2(0f, 1f);
            body.anchorMax = new Vector2(1f, 1f);
            body.pivot = new Vector2(0.5f, 1f);
            body.sizeDelta = new Vector2(0f, resolution.y);
            var layout = body.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(16, 16, 16, 16);
            body.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            return body;
        }

        static void InvokeBuild084(
            M1FlowPresenter presenter,
            Transform body,
            ICampaignPlayablePresentationCoordinator020 coordinator,
            CampaignPlayablePresentationState020 state)
        {
            var method = typeof(M1FlowPresenter).GetMethod(
                "BuildGuildCityCampaign020",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(presenter, new object[] {body, coordinator, state});
        }

        static void SetReducedMotion084(M1FlowPresenter presenter) =>
            typeof(M1FlowPresenter).GetField("_reducedMotion",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(presenter, true);

        static RectTransform CardRoot129(M1FlowPresenter presenter)
        {
            var field = typeof(M1FlowPresenter).GetField("_campaignCardRoot129",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            var root = field.GetValue(presenter) as RectTransform;
            Assert.That(root, Is.Not.Null);
            return root;
        }

        static Button[] CardActions129(RectTransform root) => root
            .GetComponentsInChildren<Button>(true).Where(value =>
                value.name != "Campaign Quest Return To Guild 131" && value.name != "Story Card Skip 129")
            .ToArray();

        static void AssertInside129(RectTransform outer, RectTransform inner, string label)
        {
            var outerCorners = new Vector3[4];
            var innerCorners = new Vector3[4];
            outer.GetWorldCorners(outerCorners);
            inner.GetWorldCorners(innerCorners);
            Assert.That(innerCorners.Min(value => value.x),
                Is.GreaterThanOrEqualTo(outerCorners.Min(value => value.x) - 0.5f), label);
            Assert.That(innerCorners.Max(value => value.x),
                Is.LessThanOrEqualTo(outerCorners.Max(value => value.x) + 0.5f), label);
            Assert.That(innerCorners.Min(value => value.y),
                Is.GreaterThanOrEqualTo(outerCorners.Min(value => value.y) - 0.5f), label);
            Assert.That(innerCorners.Max(value => value.y),
                Is.LessThanOrEqualTo(outerCorners.Max(value => value.y) + 0.5f), label);
        }

        static void DestroyHarness129(Harness084 harness)
        {
            try { UnityEngine.Object.DestroyImmediate(harness.Presenter.gameObject); }
            finally { UnityEngine.Object.Destroy(harness.Root); }
        }

        static string VisibleText084(Transform body) => string.Join("\n",
            body.GetComponentsInChildren<Text>(true).Select(value => value.text));

        readonly struct Harness084
        {
            public Harness084(GameObject root, M1FlowPresenter presenter,
                RectTransform body, RectTransform screen)
            {
                Root = root;
                Presenter = presenter;
                Body = body;
                Screen = screen;
            }

            public GameObject Root { get; }
            public M1FlowPresenter Presenter { get; }
            public RectTransform Body { get; }
            public RectTransform Screen { get; }
        }

        class PageOnlyCoordinator129 : IM1PresentationCoordinator
        {
            public event Action Changed { add { } remove { } }
            public M1PresentationState State { get; } = new M1PresentationState();
            static M1CommandResult Unexpected129() => throw new InvalidOperationException("Unexpected non-Campaign command in isolated card fixture.");
            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => Unexpected129();
            public M1CommandResult SignRecruit(string recruitId) => Unexpected129();
            public M1CommandResult EquipItem(string recruitId, string slotId, string itemId) => Unexpected129();
            public M1CommandResult UnequipItem(string recruitId, string slotId) => Unexpected129();
            public M1CommandResult SetEquipmentLock(string recruitId, string slotId, bool locked) => Unexpected129();
            public M1CommandResult CompleteEquipmentReview() => Unexpected129();
            public M1CommandResult AddUnion() => Unexpected129();
            public M1CommandResult RemoveUnion(int unionIndex) => Unexpected129();
            public M1CommandResult AssignRecruitToUnion(string recruitId, int unionIndex, int slotIndex) => Unexpected129();
            public M1CommandResult UnassignRecruitFromUnion(string recruitId) => Unexpected129();
            public M1CommandResult SetUnionLeader(int unionIndex, string recruitId) => Unexpected129();
            public M1CommandResult SetFormation(int unionIndex, string formationId) => Unexpected129();
            public M1CommandResult SetDoctrine(int unionIndex, string doctrineId) => Unexpected129();
            public M1CommandResult SaveAndReloadProof() => Unexpected129();
        }

        sealed class DeckCoordinator131 : Coordinator084,
            ICampaignWorldGatePresentationCoordinator023, IExpeditionDeckPresentationCoordinator089
        {
            public DeckCoordinator131(CampaignPlayablePresentationState020 story) : base(story)
            {
                CampaignWorldGate023 = new CampaignWorldGatePresentationState023 {
                    IsAvailable = true, CurrentWorldId = story.WorldId, CurrentWorldName = story.WorldName,
                    ActiveOperationId = "SAVED_CHAPTER_DECK_131", ActiveDefinitionId = story.ActiveChapterId,
                    ActiveBoardTitle = story.OperationTitle, ActiveOperationKind = "CHAPTER", ActiveStatus = "Active",
                    BoardObjective = "Escort the crew through the next saved room.", Supplies = 4,
                    CompletedNodes = 1, TotalNodes = 5, ExpeditionDeckTutorialSeen = true,
                    CurrentNode = new NodeView023 { NodeId = "SAVED_ROOM_131", Kind = "EVENT", RoomKind = "STORY",
                        Title = "The Crew’s Lantern", Description = "Keep the crew together.", StoryFlavor = "A light guides the crew onward." },
                    RouteCards = new[] { Card131("CARD_STORY_131", "STORY"), Card131("CARD_CHEST_131", "CHEST"), Card131("CARD_CAMP_131", "CAMP") }
                };
            }
            static ExpeditionRouteCardView089 Card131(string id, string category) => new ExpeditionRouteCardView089 {
                CardId = id, Category = category, VisualCategoryKey = category,
                VisualResourcePath = "SecondDimension/Art/Board086/CardFaces/CARD_FACE_" + category + "_089",
                Title = "Existing " + category + " card", Description = "The existing saved card projection.",
                CanChoose = true, IsEncounterRound = true, RiskLabel = "SAFE", Odds = "NO ROLL", SuccessBasisPoints = 10000,
                OutcomePreview = "Saved card result", RewardPreview = "Existing reward", RouteLabel = "CONTINUE"
            };
            public CampaignWorldGatePresentationState023 CampaignWorldGate023 { get; }
            public int DeckCommits { get; private set; }
            public string SelectedCardId { get; private set; }
            public M1CommandResult CommitExpeditionRouteCard089(string cardId)
            {
                Assert.That(CampaignWorldGate023.RouteCards.Count(value => value.CardId == cardId && value.CanChoose), Is.EqualTo(1));
                ++DeckCommits; SelectedCardId = cardId;
                return M1CommandResult.Success("Recorded explicit card choice in the presentation fixture.");
            }
            static M1CommandResult UnexpectedDeck131() => throw new InvalidOperationException("Unexpected start/travel/reward command while browsing the saved chapter deck.");
            public M1CommandResult BeginWorldGateOperation023(string id) => UnexpectedDeck131();
            public M1CommandResult CommitWorldGateChoice023(string id) => UnexpectedDeck131();
            public M1CommandResult CommitAutomaticWorldGateRoom023() => UnexpectedDeck131();
            public M1CommandResult ApplyWorldGateReceipt023() => UnexpectedDeck131();
            public M1CommandResult EnterWorldGateBattle023() => UnexpectedDeck131();
            public M1CommandResult FinalizeWorldGateOperation023() => UnexpectedDeck131();
            public M1CommandResult TravelWorldGate023(string id) => UnexpectedDeck131();
            public M1CommandResult RecoverLegacyWorldGateQuest023() => UnexpectedDeck131();
            public M1CommandResult EnterExpeditionCardBattle089() => UnexpectedDeck131();
            public M1CommandResult AcknowledgeExpeditionDeckTutorial089() => UnexpectedDeck131();
        }

        sealed class AttachedCampaignCoordinator129 : Coordinator084,
            IGuildCityPresentationCoordinator017D, ICampaignReplayPresentationCoordinator130
        {
            public AttachedCampaignCoordinator129(Coordinator084 source) : base(source.CampaignPlayable020)
            {
                Campaign019.PendingReceiptId = source.Campaign019.PendingReceiptId;
            }

            public int ReplayStarts130, ReplayExpectedCycle130, ReplayGrowth130;
            public M1CommandResult StartNextCampaignCycle130(int expectedCurrentCycle, int growthPercent = 25)
            {
                ReplayStarts130++;
                ReplayExpectedCycle130 = expectedCurrentCycle;
                ReplayGrowth130 = growthPercent;
                return M1CommandResult.Failure("Projection fixture records the deliberate request without creating a campaign cycle.");
            }

            public GuildCityPresentationState017D GuildCity017D { get; } =
                new GuildCityPresentationState017D { IsAvailable = true };
            static M1CommandResult UnexpectedGuild129() => throw new InvalidOperationException(
                "Unexpected Guild command while resuming an existing story receipt.");
            public M1CommandResult PlaceGuildCityBuilding017D(string plotId, string buildingId) => UnexpectedGuild129();
            public M1CommandResult UpgradeGuildCityBuilding017D(string plotId) => UnexpectedGuild129();
            public M1CommandResult AssignGuildCityStaff017D(string plotId, string recruitId) => UnexpectedGuild129();
            public M1CommandResult RecallGuildCityStaff017D(string recruitId) => UnexpectedGuild129();
            public M1CommandResult SetGuildCityAssignment017D(string recruitId, string assignmentKind) => UnexpectedGuild129();
            public M1CommandResult ArchiveGuildCityMember017D(string recruitId, bool confirmed) => UnexpectedGuild129();
            public M1CommandResult CommitGuildCityApplicantBoard017D() => UnexpectedGuild129();
            public M1CommandResult RefreshGuildCityApplicantBoard017D() => UnexpectedGuild129();
            public M1CommandResult SignGuildCityApplicant017D(string recruitId) => UnexpectedGuild129();
            public M1CommandResult DeclineGuildCityApplicant017D(string recruitId) => UnexpectedGuild129();
            public M1CommandResult AcceptGuildCityContract017D(string contractId) => UnexpectedGuild129();
            public M1CommandResult StartGuildCityExpedition017D() => UnexpectedGuild129();
            public M1CommandResult MoveGuildCityExpedition017D(string destinationNodeId) => UnexpectedGuild129();
            public M1CommandResult ResolveGuildCityCheck017D(string eventId, string actorRecruitId,
                string assistantRecruitId, int modifier) => UnexpectedGuild129();
            public M1CommandResult DiscoverGateworksMaintenancePassage066() => UnexpectedGuild129();
            public M1CommandResult CommitGuildCityEncounter017D(string encounterId) => UnexpectedGuild129();
            public M1CommandResult StartCommittedGuildCityBattle017D() => UnexpectedGuild129();
            public M1CommandResult FinalizeGuildCityOperation017D() => UnexpectedGuild129();
            public M1CommandResult AddGuildCityRelationshipMemory017D(string firstRecruitId, string secondRecruitId,
                string sourceId, string summary, int strength, string sceneId) => UnexpectedGuild129();
            public M1CommandResult ViewGuildCityRelationshipScene017D(string sceneId) => UnexpectedGuild129();
        }

        class Coordinator084 : PageOnlyCoordinator129,
            ICampaignPlayablePresentationCoordinator020,
            ICampaignPresentationCoordinator019
        {
            public Coordinator084(CampaignPlayablePresentationState020 state)
            {
                CampaignPlayable020 = state;
                Campaign019 = new CampaignPresentationState019 {IsAvailable = true};
            }

            public CampaignPlayablePresentationState020 CampaignPlayable020 { get; }
            public CampaignPresentationState019 Campaign019 { get; }
            public int CommitStepCount { get; private set; }
            public int EnterBattleCount { get; private set; }
            public bool FailNextApply { get; set; }
            public int ApplyStepCount { get; private set; }
            public int FinalizeCount { get; private set; }
            public int ApplyChapterCount { get; private set; }
            public string LastStepOutcome { get; private set; }

            public M1CommandResult StartPlayableChapter020(string chapterId) =>
                M1CommandResult.Success();

            public M1CommandResult CommitPlayableStep020(string outcome)
            {
                CommitStepCount++;
                LastStepOutcome = outcome;
                CampaignPlayable020.PendingStepReceiptId = "COMMITTED_CARD_129";
                return M1CommandResult.Success();
            }

            public M1CommandResult ApplyPlayableStep020()
            {
                ApplyStepCount++;
                if (FailNextApply)
                {
                    FailNextApply = false;
                    return M1CommandResult.Failure("Synthetic saved-receipt store failure 129.");
                }
                CampaignPlayable020.PendingStepReceiptId = string.Empty;
                return M1CommandResult.Success();
            }

            public M1CommandResult EnterPlayableBattle020()
            {
                EnterBattleCount++;
                return M1CommandResult.Success();
            }
            public M1CommandResult CommitPlayableBattleStepResult020() =>
                M1CommandResult.Success();

            public M1CommandResult FinalizePlayableChapter020()
            {
                FinalizeCount++;
                if (string.IsNullOrWhiteSpace(Campaign019.PendingReceiptId))
                    Campaign019.PendingReceiptId = "CHAPTER_RETURN_RECEIPT_084";
                return M1CommandResult.Success();
            }

            public M1CommandResult ApplyPlayableChapterResult020()
            {
                ApplyChapterCount++;
                if (FailNextApply)
                {
                    FailNextApply = false;
                    return M1CommandResult.Failure("Synthetic saved-receipt store failure 129.");
                }
                Campaign019.PendingReceiptId = string.Empty;
                CampaignPlayable020.ActiveOperationId = string.Empty;
                return M1CommandResult.Success();
            }

            public M1CommandResult RecoverLegacyPlayableQuest020() =>
                M1CommandResult.Success();
            public M1CommandResult RecoverInsertedBattleBoundary020() =>
                M1CommandResult.Success();
            public M1CommandResult RefreshCampaign019() => M1CommandResult.Success();
            public M1CommandResult StartChapter019(string chapterId) =>
                M1CommandResult.Success();
            public M1CommandResult EnterCampaignCertifiedBattle019() =>
                M1CommandResult.Success();
            public M1CommandResult CommitCampaignNonCombat019(string outcome) =>
                M1CommandResult.Success();
            public M1CommandResult ApplyCampaignReceipt019() =>
                M1CommandResult.Success();
        }
    }
}
