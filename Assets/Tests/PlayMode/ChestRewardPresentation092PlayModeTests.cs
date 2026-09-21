using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class ChestRewardPresentation092PlayModeTests
    {
        private GameObject _root;
        private M1FlowPresenter _presenter;

        [SetUp]
        public void SetUp092()
        {
            _root = new GameObject("Chest Presentation Test 092", typeof(RectTransform));
            _root.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
            _presenter = _root.AddComponent<M1FlowPresenter>();
            _presenter.enabled = false;
        }

        [TearDown]
        public void TearDown092() => UnityEngine.Object.DestroyImmediate(_root);

        [Test]
        public void AuthoredAtlasContainsThreeEqualSquarePosesWithTheSamePivot092()
        {
            var frames = Enumerable.Range(0, 3).Select(BoardChestReveal092.Frame092).ToArray();
            Assert.That(frames.All(value => value != null), Is.True, "The shipping chest atlas is required, not a poster fallback.");
            Assert.That(frames[0].texture.width, Is.EqualTo(2172));
            Assert.That(frames[0].texture.height, Is.EqualTo(724));
            for (var index = 0; index < frames.Length; index++)
            {
                Assert.That(frames[index].rect, Is.EqualTo(new Rect(index * 724, 0, 724, 724)));
                Assert.That(frames[index].pivot, Is.EqualTo(new Vector2(362, 362)));
            }
        }

        [UnityTest]
        public IEnumerator RealQuestResultBuilderShowsClosedHalfOpenThenExactEarnedLoot092()
        {
            var timeScale = Time.timeScale;
            var body = BuildQuestChest092("QUEST_CHEST_TIMELINE_092");
            var reveal = body.GetComponentInChildren<BoardChestReveal092>();
            var reward = Text092(body, "Board Quest Resolved Card Reward 090");
            var heading = Text092(body, "Board Quest Card Resolution Heading 090");
            Assert.That(reveal, Is.Not.Null);
            Assert.That(reveal.FrameIndex092, Is.Zero);
            Assert.That(reward.GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(heading.text, Does.Not.Contain("OPENED"));
            Assert.That(reveal.GetComponent<AspectRatioFitter>().aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.FitInParent));
            Assert.That(reveal.GetComponent<AspectRatioFitter>().aspectRatio, Is.EqualTo(1f));
            Assert.That(reveal.transform.localScale, Is.EqualTo(Vector3.one));

            yield return ObserveUntil100(() => reveal.FrameIndex092 >= 1, "Natural half-open pose");
            Assert.That(reveal.FrameIndex092, Is.EqualTo(1));
            Assert.That(reveal.IsLootRevealed092, Is.False);
            Assert.That(reward.GetComponent<CanvasGroup>().alpha, Is.Zero);
            yield return ObserveUntil100(() => !reveal.IsPlaying092, "Natural loot reveal completion");
            Assert.That(reveal.FrameIndex092, Is.EqualTo(2));
            Assert.That(reveal.IsLootRevealed092, Is.True);
            Assert.That(reveal.IsPlaying092, Is.False);
            Assert.That(reward.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(heading.text, Does.Contain("CHEST OPENED"));
            Assert.That(reward.text, Is.EqualTo("RARE SABER • EQUIPPED TO TEST HERO • PWR +8 • OLD WEAPON KEPT"));
            Assert.That(reward.text, Does.Not.Contain("ADDED TO INVENTORY"));
            var item = reveal.transform.Find("Chest Earned Item Art 092").GetComponent<Image>();
            Assert.That(item.sprite, Is.Not.Null);
            Assert.That(item.sprite.name, Is.EqualTo("EQUIPMENT_SWORD"));
            Assert.That(reveal.GetComponentsInChildren<Image>().All(value => !value.raycastTarget), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(timeScale));
        }

        [Test]
        public void ReducedMotionAndInterruptedResultReentryAlwaysExposeSavedReward092()
        {
            SetField092("_reducedMotion", true);
            var reduced = BuildQuestChest092("QUEST_REDUCED_092");
            var reveal = reduced.GetComponentInChildren<BoardChestReveal092>();
            Assert.That(reveal.FrameIndex092, Is.EqualTo(2));
            Assert.That(reveal.IsPlaying092, Is.False);
            Assert.That(Text092(reduced, "Board Quest Resolved Card Reward 090").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));

            SetField092("_reducedMotion", false);
            var interrupted = BuildQuestChest092("QUEST_INTERRUPTED_092");
            var opening = interrupted.GetComponentInChildren<BoardChestReveal092>();
            Assert.That(opening.IsPlaying092, Is.True);
            interrupted.gameObject.SetActive(false);
            interrupted.gameObject.SetActive(true);
            Assert.That(opening.IsPlaying092, Is.False);
            Assert.That(opening.IsLootRevealed092, Is.True);
            var rebuilt = BuildQuestChest092("QUEST_INTERRUPTED_092");
            Assert.That(rebuilt.GetComponentInChildren<BoardChestReveal092>().IsPlaying092, Is.False,
                "Rebuilding the same saved reveal must not hide earned loot or replay its opening.");
        }

        [UnityTest]
        public IEnumerator SuccessfulCampaignChestShowsPendingEntitlementWithoutClaimingIt092()
        {
            var state = CampaignState092("CHEST", true, "SUCCESS");
            var coordinator = new ChestCoordinator092(state);
            var body = Child092("C023 Chest Body");
            Invoke092("BuildWorldGatePrimaryAction084", body, coordinator, state, state.CurrentNode);
            var reveal = body.GetComponentInChildren<BoardChestReveal092>();
            Assert.That(reveal, Is.Not.Null);
            Assert.That(reveal.FrameIndex092, Is.Zero);
            Assert.That(coordinator.ApplyCount, Is.Zero);
            Assert.That(Text092(body, "World Gate Chest Exact Reward 092").GetComponent<CanvasGroup>().alpha, Is.Zero);
            yield return ObserveUntil100(() => reveal.FrameIndex092 >= 1, "Campaign half-open pose");
            Assert.That(reveal.FrameIndex092, Is.EqualTo(1));
            Assert.That(reveal.IsLootRevealed092, Is.False);
            Assert.That(coordinator.ApplyCount, Is.Zero);
            yield return ObserveUntil100(() => !reveal.IsPlaying092, "Campaign loot reveal completion");
            Assert.That(reveal.FrameIndex092, Is.EqualTo(2));
            Assert.That(reveal.IsLootRevealed092, Is.True);
            Assert.That(Text092(body, "World Gate Chest Exact Reward 092").text,
                Is.EqualTo("RARE SPEAR • READY TO COLLECT"));
            Assert.That(Text092(body, "Title").text, Does.Contain("READY TO COLLECT"));
            Assert.That(coordinator.ApplyCount, Is.Zero,
                "The presentation must not bypass the existing delayed exact-once receipt authority.");
            Assert.That(reveal.transform.Find("Chest Earned Item Art 092").GetComponent<Image>().sprite.name,
                Is.EqualTo("EQUIPMENT_SPEAR"));
        }

        [TestCase("CHEST", false, "SETBACK")]
        [TestCase("CHEST", true, "SETBACK")]
        [TestCase("TREASURE", true, "SUPPLIES RECOVERED")]
        public void FailedChecksAndLegacySupplyCachesDoNotPretendToEarnChestEquipment092(
            string category, bool rewardFlag, string outcome)
        {
            SetField092("_reducedMotion", true);
            var state = CampaignState092(category, rewardFlag, outcome);
            var coordinator = new ChestCoordinator092(state);
            var body = Child092("No Imaginary Chest Body");
            Invoke092("BuildWorldGatePrimaryAction084", body, coordinator, state, state.CurrentNode);
            Assert.That(body.GetComponentInChildren<BoardChestReveal092>(), Is.Null);
            Assert.That(coordinator.ApplyCount, Is.Zero);
        }

        [Test]
        public void CreationAndLaterLagCannotSkipHalfOpenPoseOrRepeatSettlement100()
        {
            var timeScale = Time.timeScale;
            var body = BuildQuestChest092("QUEST_LAGGED_CLOCK_100");
            var reveal = body.GetComponentInChildren<BoardChestReveal092>();
            var advance = typeof(BoardChestReveal092).GetMethod("AdvancePresentationClock100",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var hold = typeof(BoardChestReveal092).GetMethod("HoldCaptureClock092",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var elapsed = typeof(BoardChestReveal092).GetField("_elapsed", BindingFlags.Instance | BindingFlags.NonPublic);
            var settled = typeof(BoardChestReveal092).GetField("_settled", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(advance, Is.Not.Null);
            var callback = (Action)settled.GetValue(reveal);
            var settlementCount = 0;
            settled.SetValue(reveal, (Action)(() => { settlementCount++; callback?.Invoke(); }));

            // Same private clock called by Update, not a fake reward or pose setter.
            advance.Invoke(reveal, new object[] { 5f });
            Assert.That(reveal.FrameIndex092, Is.Zero);
            Assert.That((float)elapsed.GetValue(reveal), Is.Zero, "Pre-creation time is discarded.");
            foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, -1f, 0f })
                advance.Invoke(reveal, new object[] { invalid });
            Assert.That((float)elapsed.GetValue(reveal), Is.Zero);
            for (var frame = 0; frame < 13; frame++) advance.Invoke(reveal, new object[] { 5f });
            Assert.That(reveal.FrameIndex092, Is.EqualTo(1));
            Assert.That(reveal.IsLootRevealed092, Is.False);
            Assert.That(settlementCount, Is.Zero);
            var heldElapsed = (float)elapsed.GetValue(reveal);
            hold.Invoke(reveal, new object[] { true });
            advance.Invoke(reveal, new object[] { 5f });
            Assert.That((float)elapsed.GetValue(reveal), Is.EqualTo(heldElapsed));
            hold.Invoke(reveal, new object[] { false });
            advance.Invoke(reveal, new object[] { 5f });
            Assert.That((float)elapsed.GetValue(reveal), Is.EqualTo(heldElapsed), "Capture-release stall is discarded.");
            for (var frame = 0; frame < 60; frame++) advance.Invoke(reveal, new object[] { 5f });
            Assert.That(reveal.FrameIndex092, Is.EqualTo(2));
            Assert.That(reveal.IsLootRevealed092, Is.True);
            Assert.That(reveal.IsPlaying092, Is.False);
            Assert.That(settlementCount, Is.EqualTo(1));
            Assert.That(Text092(body, "Board Quest Resolved Card Reward 090").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            body.gameObject.SetActive(false);
            body.gameObject.SetActive(true);
            advance.Invoke(reveal, new object[] { 5f });
            Assert.That(settlementCount, Is.EqualTo(1));
            Assert.That(Time.timeScale, Is.EqualTo(timeScale));
        }

        private static IEnumerator ObserveUntil100(Func<bool> reached, string stage)
        {
            var deadline = Time.realtimeSinceStartup + 15f;
            while (!reached() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(reached(), Is.True, stage + " did not complete within the bounded observation.");
        }

        private RectTransform BuildQuestChest092(string cardId)
        {
            SetField092("_lastBoardQuestCard090", new GuildQuestCardView090
            {
                CardId = cardId, Category = "CHEST", Title = "Rare Chest", ItemName = "Rare Saber",
                ItemVisualId = "SWORD", RarityId = "QUALITY_RARE",
                RewardPreview = "Rare Saber • +4 XP", ResultRewardCopy092 =
                    "RARE SABER • EQUIPPED TO TEST HERO • PWR +8 • OLD WEAPON KEPT",
                VisualResourcePath = GuildQuestCardPresentation090.VisualResourcePath090("CHEST")
            });
            var body = Child092(cardId);
            Invoke092("BuildBoardQuestCardResolution090", body, new GuildCityPresentationState017D());
            return body;
        }

        private static CampaignWorldGatePresentationState023 CampaignState092(string category, bool rewardFlag, string outcome) =>
            new CampaignWorldGatePresentationState023
            {
                IsAvailable = true, ActiveOperationId = "CHEST_OPERATION_092", ActiveOperationKind = "CHAPTER", ActiveStatus = "Active",
                PendingReceiptId = "CHEST_RECEIPT_092", PendingCardCategory = category, PendingCardTitle = "Rare Chest",
                PendingCardVisualResourcePath = GuildQuestCardPresentation090.VisualResourcePath090("CHEST"),
                PendingOutcomeTitle = outcome, PendingReward = "RARE SPEAR • READY TO COLLECT",
                PendingChestHasReward092 = rewardFlag, PendingChestItemVisualId092 = "SPEAR",
                PendingChestRewardCopy092 = "RARE SPEAR • READY TO COLLECT", ExpeditionDeckTutorialSeen = true,
                CurrentNode = new NodeView023 { NodeId = "CHEST_NODE_092", Kind = "RESOURCE", RoomKind = category,
                    Title = "Supply Route", Description = "A sealed container waits beside the path." }
            };

        private RectTransform Child092(string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(_root.transform, false);
            rect.sizeDelta = new Vector2(1800, 900);
            return rect;
        }

        private static Text Text092(Transform parent, string prefix) => parent.GetComponentsInChildren<Text>(true)
            .First(value => value.name.StartsWith(prefix, StringComparison.Ordinal));

        private void SetField092(string name, object value) => typeof(M1FlowPresenter)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_presenter, value);

        private void Invoke092(string name, params object[] args) => typeof(M1FlowPresenter)
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_presenter, args);

        private sealed class ChestCoordinator092 : ICampaignWorldGatePresentationCoordinator023
        {
            public ChestCoordinator092(CampaignWorldGatePresentationState023 state) => CampaignWorldGate023 = state;
            public CampaignWorldGatePresentationState023 CampaignWorldGate023 { get; }
            public int ApplyCount { get; private set; }
            public M1CommandResult ApplyWorldGateReceipt023() { ApplyCount++; return M1CommandResult.Success(); }
            public M1CommandResult BeginWorldGateOperation023(string definitionId) => M1CommandResult.Success();
            public M1CommandResult CommitWorldGateChoice023(string choiceId) => M1CommandResult.Success();
            public M1CommandResult CommitAutomaticWorldGateRoom023() => M1CommandResult.Success();
            public M1CommandResult EnterWorldGateBattle023() => M1CommandResult.Success();
            public M1CommandResult FinalizeWorldGateOperation023() => M1CommandResult.Success();
            public M1CommandResult TravelWorldGate023(string worldId) => M1CommandResult.Success();
            public M1CommandResult RecoverLegacyWorldGateQuest023() => M1CommandResult.Success();
        }
    }
}
