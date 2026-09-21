using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class WorldGateEvent110Tests
    {
        private GameObject _host;
        private GameObject _screenHost;
        private M1FlowPresenter _presenter;

        [SetUp]
        public void SetUp110()
        {
            _host = new GameObject("World Gate Event Test 110");
            // Shipping uses a separate RuntimeUi canvas/root. The presenter
            // may restore that live root while its own component is destroyed.
            _screenHost = new GameObject("World Gate Event Screen 110", typeof(RectTransform));
            _screenHost.GetComponent<RectTransform>().sizeDelta = new Vector2(2796f, 1290f);
            _presenter = _host.AddComponent<M1FlowPresenter>();
            // Do not run Start/registry loading or touch a player profile.
            _presenter.enabled = false;
            Set("_screenRoot", _screenHost.GetComponent<RectTransform>());
        }

        [TearDown]
        public void TearDown110()
        {
            try { UnityEngine.Object.DestroyImmediate(_host); }
            finally { UnityEngine.Object.DestroyImmediate(_screenHost); }
        }

        [Test]
        public void SavedEvent110UsesFullScreenExistingChestAndLargeCommittedDice()
        {
            var coordinator = new Coordinator110(State110());
            Set("_reducedMotion", true);
            var previousPage = new GameObject("Previous quest controls", typeof(RectTransform)).GetComponent<RectTransform>();
            previousPage.SetParent(_screenHost.transform, false);
            Set("_activePage", previousPage);
            Build(coordinator);
            var stage = Stage();
            Assert.That(previousPage.gameObject.activeSelf, Is.False);
            Assert.That(stage.parent, Is.SameAs(_screenHost.transform));
            Assert.That(stage.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(stage.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(stage.rect.height, Is.EqualTo(1290f).Within(1f));
            var scroll = stage.GetComponentsInChildren<ScrollRect>().Single();
            Assert.That(scroll.vertical, Is.True);
            Assert.That(scroll.horizontal, Is.False);
            Assert.That(scroll.viewport.GetComponent<RectMask2D>(), Is.Not.Null);
            var chest = stage.GetComponentInChildren<BoardChestReveal092>();
            Assert.That(chest, Is.Not.Null);
            Assert.That(chest.IsLootRevealed092, Is.True);
            Assert.That(chest.FrameIndex092, Is.EqualTo(2));
            Assert.That(chest.GetComponent<AspectRatioFitter>().aspectMode,
                Is.EqualTo(AspectRatioFitter.AspectMode.FitInParent));
            Assert.That(Texts(stage).Any(value => value.text == coordinator.CampaignWorldGate023.PendingChestRewardCopy092), Is.True);
            Assert.That(Texts(stage).Any(value => value.text.Contains("SAVED CHECK  •  TARGET 7")), Is.True);
            Assert.That(Texts(stage).Any(value => value.name == "Authoritative Dice Equation 084" && value.text.Contains("8")), Is.True);
            foreach (var pair in new[] { new { Name = "First Authoritative Die 084", Value = 2 },
                         new { Name = "Second Authoritative Die 084", Value = 5 } })
            {
                var die = stage.GetComponentsInChildren<RectTransform>().Single(value => value.name == pair.Name);
                Assert.That(die.rect.width, Is.EqualTo(136f));
                Assert.That(die.parent.GetComponent<LayoutElement>().preferredWidth, Is.EqualTo(144f));
                Assert.That(die.Cast<Transform>().Count(value => value.name.StartsWith("Die Pip ") && value.gameObject.activeSelf),
                    Is.EqualTo(pair.Value));
            }
            Assert.That(coordinator.ApplyCount, Is.Zero);
            Assert.That(coordinator.OtherCommandCount, Is.Zero);
            Assert.That(coordinator.CampaignWorldGate023.PendingReceiptId, Is.EqualTo("RECEIPT110"));
            var controls = stage.GetComponentsInChildren<Button>().Where(value => value.interactable).ToArray();
            Assert.That(controls.All(value => value.navigation.mode == Navigation.Mode.Explicit &&
                controls.Contains(value.navigation.selectOnRight as Button) &&
                controls.Contains(value.navigation.selectOnDown as Button)), Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DeferredBattle110UsesEncounterReadyCopyWithoutClaimingUnfinishedRewards(bool deferredBattle)
        {
            var state = State110();
            state.PendingChestHasReward092 = false;
            state.PendingCardCategory = deferredBattle ? "BATTLE" : "XP";
            state.PendingCardTitle = deferredBattle ? "Four Enemy Unions" : "Guild Experience";
            state.PendingCardVisualResourcePath = string.Empty;
            state.PendingHasCheck = false;
            state.PendingIsEncounterRound = true;
            state.PendingRequiresCertifiedBattle = deferredBattle;
            state.PendingReward = deferredBattle
                ? "4 enemy Unions + battle loot + Art growth • route cards next"
                : "+25 GUILD XP";
            var coordinator = new Coordinator110(state);
            Set("_reducedMotion", true);
            Build(coordinator);
            var stage = Stage();
            var copy = string.Join("\n", Texts(stage).Select(value => value.text));
            if (deferredBattle)
            {
                Assert.That(copy, Does.Contain("ENCOUNTER SAVED  •  BATTLE READY"));
                Assert.That(copy, Does.Contain("NO BATTLE REWARD CLAIMED"));
                Assert.That(copy, Does.Contain("AFTER VICTORY  •  " + state.PendingReward));
                Assert.That(copy, Does.Not.Contain("REWARD SAVED"));
                Assert.That(stage.GetComponentsInChildren<Button>(true).Any(value =>
                    value.name == "Enter optional Expedition card battle 089"), Is.True);
                Assert.That(Scheduled(), Is.Zero, "The battle must still be entered and completed deliberately.");
            }
            else
            {
                Assert.That(copy, Does.Contain("REWARD SAVED  •  +25 GUILD XP"));
                Assert.That(copy, Does.Not.Contain("NO BATTLE REWARD CLAIMED"));
                Assert.That(Scheduled(), Is.EqualTo(1), "Existing nonbattle receipt presentation retains its timer.");
            }
            Assert.That(state.PendingReceiptId, Is.EqualTo("RECEIPT110"));
            Assert.That(coordinator.ApplyCount, Is.Zero);
            Assert.That(coordinator.OtherCommandCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Skip110SettlesSameRewardAndRepeatedClicksDispatchNoAdditionalCommands()
        {
            var coordinator = new Coordinator110(State110());
            var timeScale = Time.timeScale;
            Build(coordinator);
            var previous = Stage();
            var skip = previous.GetComponentsInChildren<Button>().Single(value => value.name == "Committed Expedition Event Skip 110");
            skip.onClick.Invoke();
            var current = Stage();
            Assert.That(current, Is.Not.SameAs(previous));
            Assert.That(previous.gameObject.activeSelf, Is.False);
            Assert.That(current.GetComponentInChildren<BoardChestReveal092>().IsPlaying092, Is.False);
            Assert.That(current.GetComponentInChildren<BoardChestReveal092>().IsLootRevealed092, Is.True);
            Assert.That(Get<bool>("_reducedMotion"), Is.False, "Skip must restore the user's presentation setting.");
            Assert.That(Scheduled(), Is.EqualTo(1));
            Assert.That(coordinator.ApplyCount, Is.Zero, "Skip itself cannot apply a reward.");
            skip.onClick.Invoke();
            skip.onClick.Invoke();
            Assert.That(Stage(), Is.SameAs(current));
            Assert.That(Scheduled(), Is.EqualTo(1));
            Assert.That(coordinator.CampaignWorldGate023.PendingReceiptId, Is.EqualTo("RECEIPT110"));
            yield return new WaitForSecondsRealtime(0.65f);
            Assert.That(coordinator.ApplyCount, Is.EqualTo(1));
            Assert.That(coordinator.OtherCommandCount, Is.Zero);
            Assert.That(coordinator.CampaignWorldGate023.PendingReceiptId, Is.Empty);
            Assert.That(Scheduled(), Is.Zero);
            Assert.That(Time.timeScale, Is.EqualTo(timeScale));
        }

        [UnityTest]
        public IEnumerator DiceAndChest119FinishBrieflyWithoutApplyingBeforeTheirSavedReveal()
        {
            var coordinator = new Coordinator110(State110());
            Build(coordinator);
            var dice = Stage().GetComponentInChildren<CommittedQuestDice132>();
            var chest = Stage().GetComponentInChildren<BoardChestReveal092>();
            yield return new WaitForSecondsRealtime(3.5f);
            Assert.That(dice.IsSettled132, Is.False);
            Assert.That(chest.FrameIndex092, Is.Zero, "A checked chest stays closed while awaiting the player's roll.");
            Assert.That(coordinator.ApplyCount, Is.Zero, "The former fixed timer cannot apply an unrolled check.");
            Stage().GetComponentsInChildren<Button>().Single(value => value.name == CommittedQuestDice132.RollButton132).onClick.Invoke();
            var deadline = Time.realtimeSinceStartup + 12f;
            while (!dice.IsSettled132 && Time.realtimeSinceStartup < deadline)
            {
                Assert.That(coordinator.ApplyCount, Is.Zero);
                Assert.That(chest.FrameIndex092, Is.Zero);
                yield return null;
            }
            Assert.That(dice.IsSettled132, Is.True);
            while (coordinator.ApplyCount == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(chest.IsLootRevealed092, Is.True);
            Assert.That(coordinator.ApplyCount, Is.EqualTo(1));
            Assert.That(coordinator.CampaignWorldGate023.PendingDieOne, Is.EqualTo(2));
            Assert.That(coordinator.CampaignWorldGate023.PendingDieTwo, Is.EqualTo(5));
            Assert.That(coordinator.CampaignWorldGate023.PendingReceiptId, Is.Empty);
            Assert.That(coordinator.OtherCommandCount, Is.Zero);
            yield return new WaitForSecondsRealtime(0.45f);
            Assert.That(coordinator.ApplyCount, Is.EqualTo(1));
        }

        [Test]
        public void ExistingPendingReceipt110ShowsItsSavedEventWithoutChangingTutorialState()
        {
            var state = State110();
            state.ExpeditionDeckTutorialSeen = false;
            var coordinator = new Coordinator110(state);
            Set("_reducedMotion", true);
            var body = new GameObject("Legacy quest body", typeof(RectTransform)).GetComponent<RectTransform>();
            body.SetParent(_screenHost.transform, false);
            Set("_activePage", body);
            Invoke("BuildActiveAdventureBoard084", body, coordinator, state);
            Assert.That(Stage(), Is.Not.Null);
            Assert.That(body.gameObject.activeSelf, Is.False);
            Assert.That(state.ExpeditionDeckTutorialSeen, Is.False);
            Assert.That(state.PendingReceiptId, Is.EqualTo("RECEIPT110"));
            Assert.That(coordinator.ApplyCount, Is.Zero);
            Assert.That(coordinator.OtherCommandCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Closing110CancelsTimerAndSamePendingReceiptCanReenterExactlyOnce()
        {
            var coordinator = new Coordinator110(State110());
            Build(coordinator);
            Assert.That(Scheduled(), Is.EqualTo(1));
            var interruptedDice = Stage().GetComponentInChildren<CommittedQuestDice132>();
            interruptedDice.Roll132();
            yield return null;
            Assert.That(interruptedDice.IsRolling132, Is.True);
            Stage().gameObject.SetActive(false);
            Assert.That(Scheduled(), Is.Zero);
            Assert.That(coordinator.ApplyCount, Is.Zero);
            Set("_reducedMotion", true);
            Build(coordinator);
            Assert.That(Scheduled(), Is.EqualTo(1));
            Assert.That(coordinator.CampaignWorldGate023.PendingDieOne, Is.EqualTo(2));
            Assert.That(coordinator.CampaignWorldGate023.PendingDieTwo, Is.EqualTo(5));
            yield return new WaitForSecondsRealtime(0.65f);
            Assert.That(coordinator.ApplyCount, Is.EqualTo(1));
            Assert.That(Scheduled(), Is.Zero);
            yield return null;
            Assert.That(coordinator.ApplyCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DestroyingVisibleEvent110CancelsItsPendingApply()
        {
            var coordinator = new Coordinator110(State110());
            Set("_reducedMotion", true);
            Build(coordinator);
            Assert.That(Scheduled(), Is.EqualTo(1));
            UnityEngine.Object.DestroyImmediate(Stage().gameObject);
            Assert.That(Scheduled(), Is.Zero);
            Assert.That(coordinator.ApplyCount, Is.Zero);
            yield return new WaitForSecondsRealtime(0.65f);
            Assert.That(coordinator.ApplyCount, Is.Zero, "Destroying the event must cancel its delayed application.");
            Assert.That(coordinator.CampaignWorldGate023.PendingReceiptId, Is.EqualTo("RECEIPT110"));
            Assert.That(coordinator.OtherCommandCount, Is.Zero);
        }

        [Test]
        public void ReplacedReceipt110RejectsStaleSkipAndStaleScheduledApply()
        {
            var coordinator = new Coordinator110(State110());
            Build(coordinator);
            var stage = Stage();
            var scheduled = (IEnumerator)Invoke("ApplyWorldGateReceiptAfterReveal084", coordinator, "RECEIPT110", true, true, null);
            Assert.That(scheduled.MoveNext(), Is.True);
            coordinator.CampaignWorldGate023.PendingReceiptId = "REPLACEMENT110";
            Invoke("SkipWorldGateEventReveal110", coordinator, "RECEIPT110");
            Assert.That(Stage(), Is.SameAs(stage));
            Assert.That(scheduled.MoveNext(), Is.False);
            Assert.That(coordinator.ApplyCount, Is.Zero);
            Assert.That(coordinator.OtherCommandCount, Is.Zero);
        }

        [Test]
        public void Lifetime110CancelsOnceAndDetachedCompletedTimerCannotBeCanceledAgain()
        {
            var child = new GameObject("Lifetime 110");
            child.transform.SetParent(_screenHost.transform);
            var owner = child.AddComponent<WorldGateEventLifetime110>();
            var count = 0;
            owner.BindCancellation110(() => count++);
            child.SetActive(false);
            owner.Cancel110();
            Assert.That(count, Is.EqualTo(1));
            child.SetActive(true);
            owner.BindCancellation110(() => count++);
            owner.Detach110();
            UnityEngine.Object.DestroyImmediate(child);
            Assert.That(count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PlayerRoll132UsesRenderedButtonTumbleAndExactConcealedSavedFaces()
        {
            foreach (var size in new[] { new Vector2Int(1280, 800), new Vector2Int(1920, 1080) })
            {
                Canvas canvas = null; Camera camera = null; RenderTexture target = null; GameObject events = null;
                try
                {
                    var ui = typeof(M1FlowPresenter).Assembly.GetType("SecondDimension.Presentation.RuntimeUi");
                    canvas = (Canvas)ui.GetMethod("CreateCanvas", BindingFlags.Static | BindingFlags.Public)
                        .Invoke(null, new object[] { "Ordinary Dice Rendered Canvas 132" });
                    target = new RenderTexture(size.x, size.y, 24); target.Create();
                    camera = new GameObject("Ordinary Dice Camera 132", typeof(Camera)).GetComponent<Camera>();
                    camera.targetTexture = target; camera.transform.position = new Vector3(0, 0, -10);
                    canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                    if (EventSystem.current == null) events = new GameObject("Ordinary Dice Pointer 132", typeof(EventSystem));
                    _screenHost.transform.SetParent(canvas.transform, false);
                    var screen = (RectTransform)_screenHost.transform;
                    screen.anchorMin = Vector2.zero; screen.anchorMax = Vector2.one;
                    screen.offsetMin = screen.offsetMax = Vector2.zero;
                    var state = State110(); state.PendingReceiptId = "RENDER132_" + size.x;
                    state.PendingChestHasReward092 = false; state.PendingCardCategory = "XP";
                    state.PendingReward = "+73 GUILD XP";
                    Set("_worldGateReceiptRetryId084", state.PendingReceiptId);
                    var owner = new Coordinator110(state);
                    Build(owner);
                    yield return null; yield return null;
                    Assert.That(canvas.renderingDisplaySize, Is.EqualTo((Vector2)size));
                    var scale = canvas.GetComponent<CanvasScaler>();
                    Assert.That(canvas.scaleFactor, Is.EqualTo(Mathf.Sqrt(
                        size.x / scale.referenceResolution.x * size.y / scale.referenceResolution.y)).Within(.001f));
                    var stage = Stage();
                    var dice = stage.GetComponentInChildren<CommittedQuestDice132>();
                    var roll = stage.GetComponentsInChildren<Button>().Single(value => value.name == CommittedQuestDice132.RollButton132);
                    Assert.That(dice.IsRolling132 || dice.IsSettled132, Is.False);
                    var message = Texts(stage).Single(value => value.name.StartsWith("Message [", StringComparison.Ordinal));
                    var reward = Texts(stage).Single(value => value.name.StartsWith("Board Adventure Resolved Reward Copy 087 [", StringComparison.Ordinal));
                    var retryStatus = Texts(stage).Single(value => value.name.StartsWith("World Gate Automatic Result Apply 084 [", StringComparison.Ordinal));
                    var retry = stage.GetComponentsInChildren<Button>().Single(value => value.name == "Retry saved World Gate result 084");
                    Assert.That(message.text, Does.Contain("EVENT  •  A sealed container waits beside the path."));
                    Assert.That(message.text, Does.Not.Contain("SUCCESS"));
                    Assert.That(reward.text, Is.Empty);
                    Assert.That(retryStatus.text, Is.EqualTo("WAITING FOR YOUR ROLL"));
                    Assert.That(retry.IsInteractable(), Is.False, "Retry cannot bypass an unrolled saved result.");
                    Assert.That(string.Join("\n", Texts(stage).Select(value => value.text)), Does.Not.Contain("+73 GUILD XP"));
                    Assert.That(Texts(stage).Single(value => value.name == "Authoritative Dice Equation 084").text, Is.Empty);
                    var scroll = roll.GetComponentInParent<ScrollRect>();
                    if (scroll != null)
                    {
                        var targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, roll.transform);
                        var contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, scroll.content);
                        var excess = contentBounds.size.y - scroll.viewport.rect.height;
                        if (excess > 0) scroll.verticalNormalizedPosition -=
                            (scroll.viewport.rect.center.y - targetBounds.center.y) / excess;
                    }
                    yield return null; yield return null;
                    var corners = new Vector3[4]; ((RectTransform)roll.transform).GetWorldCorners(corners);
                    var point = RectTransformUtility.WorldToScreenPoint(camera, (corners[0] + corners[2]) * .5f);
                    var pointer = new PointerEventData(EventSystem.current) { position = point,
                        button = PointerEventData.InputButton.Left, eligibleForClick = true, pointerId = -1 };
                    var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
                    Assert.That(hits, Is.Not.Empty);
                    Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject), Is.EqualTo(roll.gameObject));
                    ExecuteEvents.Execute(roll.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                    ExecuteEvents.Execute(roll.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                    ExecuteEvents.Execute(roll.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                    Assert.That(dice.IsRolling132, Is.True);
                    Assert.That(roll.interactable, Is.False);
                    var first = stage.GetComponentsInChildren<RectTransform>().Single(value => value.name == "First Authoritative Die 084");
                    var start = first.localRotation;
                    yield return null; yield return null; yield return null;
                    Assert.That(Quaternion.Angle(start, first.localRotation), Is.GreaterThan(.1f), "Natural Update must visibly tumble the die.");
                    var sawStagger = false; var deadline = Time.realtimeSinceStartup + 10;
                    while (!dice.IsSettled132 && Time.realtimeSinceStartup < deadline)
                    {
                        sawStagger |= dice.LandedDice132 == 1;
                        Assert.That(owner.ApplyCount, Is.Zero);
                        Assert.That(Texts(stage).Single(value => value.name == "Authoritative Dice Equation 084").text, Is.Empty);
                        yield return null;
                    }
                    Assert.That(dice.IsSettled132, Is.True); Assert.That(sawStagger, Is.True);
                    foreach (var pair in new[] { new { Name = "First Authoritative Die 084", Value = 2 },
                        new { Name = "Second Authoritative Die 084", Value = 5 } })
                    {
                        var die = stage.GetComponentsInChildren<RectTransform>().Single(value => value.name == pair.Name);
                        Assert.That(die.Cast<Transform>().Count(value => value.name.StartsWith("Die Pip ") && value.gameObject.activeSelf), Is.EqualTo(pair.Value));
                    }
                    Assert.That(Texts(stage).Single(value => value.name == "Authoritative Dice Equation 084").text, Is.EqualTo("+1  =  8"));
                    Assert.That(string.Join("\n", Texts(stage).Select(value => value.text)), Does.Contain("+73 GUILD XP"));
                    Assert.That(message.text, Does.Contain("RESULT  •  SUCCESS"));
                    Assert.That(reward.text, Is.EqualTo("REWARD SAVED  •  +73 GUILD XP"));
                    Assert.That(retryStatus.text, Is.EqualTo("SAVED RESULT IS SAFE  •  READY TO RETRY"));
                    Assert.That(retry.IsInteractable(), Is.True);
                    dice.Roll132(); Assert.That(dice.IsRolling132, Is.False, "Repeated input cannot reroll a settled result.");
                    Assert.That(owner.ApplyCount + owner.OtherCommandCount, Is.Zero);
                    stage.gameObject.SetActive(false);
                }
                finally
                {
                    _screenHost.transform.SetParent(null, false);
                    if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas.gameObject);
                    if (camera != null) { camera.targetTexture = null; UnityEngine.Object.DestroyImmediate(camera.gameObject); }
                    if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                    if (events != null) UnityEngine.Object.DestroyImmediate(events);
                }
            }
        }

        private void Build(Coordinator110 coordinator) =>
            Invoke("BuildFullScreenWorldGateEvent110", coordinator, coordinator.CampaignWorldGate023);
        private RectTransform Stage() => Get<RectTransform>("_worldGateEventRoot110");
        private int Scheduled() => Get<HashSet<string>>("_scheduledWorldGateReceiptApplies084").Count;
        private static Text[] Texts(Transform parent) => parent.GetComponentsInChildren<Text>(true);
        private void Set(string name, object value) => typeof(M1FlowPresenter)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_presenter, value);
        private T Get<T>(string name) => (T)typeof(M1FlowPresenter)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_presenter);
        private object Invoke(string name, params object[] args) => typeof(M1FlowPresenter)
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_presenter, args);

        private static CampaignWorldGatePresentationState023 State110() => new CampaignWorldGatePresentationState023
        {
            IsAvailable = true, ActiveOperationId = "OPERATION110", ActiveOperationKind = "CHAPTER", ActiveStatus = "Active",
            ActiveBoardTitle = "Saved Supply Route", PendingReceiptId = "RECEIPT110", PendingCardCategory = "CHEST",
            PendingCardTitle = "Rare Chest", PendingCardVisualResourcePath = GuildQuestCardPresentation090.VisualResourcePath090("CHEST"),
            PendingOutcomeTitle = "SUCCESS", PendingReward = "RARE SPEAR • READY TO COLLECT", PendingChestHasReward092 = true,
            PendingChestItemVisualId092 = "SPEAR", PendingChestRewardCopy092 = "RARE SPEAR • READY TO COLLECT",
            PendingHasCheck = true, PendingDieOne = 2, PendingDieTwo = 5, PendingModifier = 1, PendingTotal = 8, PendingDifficulty = 7,
            ExpeditionDeckTutorialSeen = true,
            CurrentNode = new NodeView023 { NodeId = "NODE110", Kind = "RESOURCE", RoomKind = "CHEST", Title = "Supply Route",
                Description = "A sealed container waits beside the path." }
        };

        private sealed class Coordinator110 : ICampaignWorldGatePresentationCoordinator023
        {
            public Coordinator110(CampaignWorldGatePresentationState023 state) => CampaignWorldGate023 = state;
            public CampaignWorldGatePresentationState023 CampaignWorldGate023 { get; }
            public int ApplyCount { get; private set; }
            public int OtherCommandCount { get; private set; }
            public M1CommandResult ApplyWorldGateReceipt023()
            {
                ApplyCount++;
                CampaignWorldGate023.PendingReceiptId = string.Empty;
                return M1CommandResult.Success();
            }
            private M1CommandResult Other() { OtherCommandCount++; return M1CommandResult.Success(); }
            public M1CommandResult BeginWorldGateOperation023(string definitionId) => Other();
            public M1CommandResult CommitWorldGateChoice023(string choiceId) => Other();
            public M1CommandResult CommitAutomaticWorldGateRoom023() => Other();
            public M1CommandResult EnterWorldGateBattle023() => Other();
            public M1CommandResult FinalizeWorldGateOperation023() => Other();
            public M1CommandResult TravelWorldGate023(string worldId) => Other();
            public M1CommandResult RecoverLegacyWorldGateQuest023() => Other();
        }
    }
}
