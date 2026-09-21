using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign020;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class CommittedGlassBattle132PlayModeTests
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject _host, _events;
        M1FlowPresenter _presenter;
        Canvas _canvas;
        Owner132 _owner;

        [SetUp]
        public void Setup132()
        {
            if (EventSystem.current == null)
                _events = new GameObject("Glass battle132 events", typeof(EventSystem), typeof(StandaloneInputModule));
            _host = new GameObject("Glass battle132 presentation fixture");
            _presenter = _host.AddComponent<M1FlowPresenter>();
            _presenter.enabled = false;
            typeof(M1FlowPresenter).GetMethod("EnsureCanvas", Hidden).Invoke(_presenter, null);
            _canvas = (Canvas)typeof(M1FlowPresenter).GetField("_canvas", Hidden).GetValue(_presenter);
            _owner = new Owner132();
            typeof(M1FlowPresenter).GetField("_coordinator", Hidden).SetValue(_presenter, _owner);
        }

        [TearDown]
        public void Cleanup132()
        {
            if (_host != null) UnityEngine.Object.DestroyImmediate(_host);
            if (_canvas != null) UnityEngine.Object.DestroyImmediate(_canvas.gameObject);
            if (_events != null) UnityEngine.Object.DestroyImmediate(_events);
        }

        void Build132() => Assert.That((bool)typeof(M1FlowPresenter)
            .GetMethod("TryBuildCampaignInterruption132", Hidden)
            .Invoke(_presenter, new object[] { _owner, _owner.CampaignPlayable020 }), Is.True);
        Button Continue132() => _canvas.GetComponentsInChildren<Button>(false)
            .Single(value => value.name == "Skip Committed Story Glass 132");

        [UnityTest]
        public IEnumerator EnterPointerCreatesBattleBeforeTransparentShatterAndHoldsOrdersWithoutResolving132()
        {
            // Synthetic coordinator exercises real presenter/controller/Canvas
            // routing. This is not a native-art or combat-authority acceptance run.
            Build132(); yield return null; yield return null;
            var button = Continue132();
            Assert.That(button.GetComponentInChildren<Text>().text, Is.EqualTo("ENTER BATTLE"));
            Assert.That(_owner.Enters, Is.Zero);
            Assert.That(_canvas.GetComponentsInChildren<CommittedGlassShatter132>(false), Is.Empty,
                "The pre-battle reading card does not show shatter against a black screen.");
            var illustration = _canvas.GetComponentsInChildren<Image>(false)
                .Single(value => value.name == "Campaign Quest Illustration 131");
            Assert.That(illustration.sprite, Is.Not.Null);
            PointerClick132(button);
            button.onClick.Invoke();
            var controller = _host.GetComponent<M2BattleExperienceController072>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsActive, Is.True);
            Assert.That(_owner.Enters, Is.EqualTo(1));
            Assert.That(_owner.Completions, Is.EqualTo(1), "A retained entry callback cannot replay the committed transition.");
            Assert.That(_owner.Battle.IsResolved, Is.False);
            Assert.That(controller.OwnedDiorama078, Is.Not.Null);
            Assert.That(controller.StoryBattleIntroHeld132, Is.True);
            Assert.That(controller.IsResolving, Is.True, "Presentation hold prevents QA/Auto from treating entry as ready.");
            var effect = _canvas.GetComponentsInChildren<CommittedGlassShatter132>(false).Single();
            Assert.That(effect.GetComponent<Image>().color.a, Is.Zero,
                "The actual arena and enemy actors remain visible behind the glass.");
            var facets = effect.GetComponentsInChildren<BattleGlassFacet132>(false);
            Assert.That(facets.Length, Is.EqualTo(24));
            Assert.That(effect.GetComponentsInChildren<GlassShardGraphic132>(false), Is.Empty,
                "The battle never displays the legacy center triangles.");
            Assert.That(facets.All(value => value.color.a <= .075f && value.color.b >= value.color.r &&
                value.color.g >= value.color.r && !value.raycastTarget), Is.True,
                "Pane fills are translucent silver/cyan, never an opaque red wash.");
            Assert.That(facets.Min(value => value.rectTransform.pivot.x), Is.LessThan(.22f));
            Assert.That(facets.Max(value => value.rectTransform.pivot.x), Is.GreaterThan(.78f));
            Assert.That(facets.Min(value => value.rectTransform.pivot.y), Is.LessThan(.22f));
            Assert.That(facets.Max(value => value.rectTransform.pivot.y), Is.GreaterThan(.78f));
            var mesh = new Mesh();
            try
            {
                using (var vertices = new VertexHelper())
                {
                    typeof(BattleGlassFacet132).GetMethod("OnPopulateMesh", Hidden, null, new[] { typeof(VertexHelper) }, null)
                        .Invoke(facets[0], new object[] { vertices });
                    vertices.FillMesh(mesh);
                    Assert.That(mesh.colors.Any(value => value.a > .4f && value.b >= value.r), Is.True,
                        "Fine bright cracked edges remain visible over the low-opacity pane fill.");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
            Assert.That(effect.GetComponentsInChildren<Button>(false), Is.Empty,
                "Shatter settlement is presentation-only, without another gameplay Continue.");
            Assert.That(effect.GetComponentsInChildren<Text>(false).Single().text, Does.StartWith("STORY BATTLE"));
            controller.SetAutoOrders091(true);
            typeof(M2BattleExperienceController072).GetMethod("SelectForecast", Hidden)
                .Invoke(controller, new object[] { "ALLY132", "FORECAST132" });
            typeof(M2BattleExperienceController072).GetMethod("ConfirmRound", Hidden).Invoke(controller, null);
            effect.Skip132();
            Assert.That(controller.AutoOrdersEnabled091, Is.False);
            Assert.That(controller.StoryBattleIntroHeld132, Is.True, "An early retained skip cannot bypass the intro.");
            Assert.That(_owner.Commands, Is.Zero);
            yield return null; yield return null;
            var settleDeadline = Time.realtimeSinceStartup + 8f;
            while (controller.StoryBattleIntroHeld132 && Time.realtimeSinceStartup < settleDeadline) yield return null;
            yield return null;
            Assert.That(controller.StoryBattleIntroHeld132, Is.False);
            Assert.That(controller.IsResolving, Is.False);
            Assert.That(_owner.Commands, Is.Zero, "Animation completion never resolves or rewards combat.");
            Assert.That(_owner.Battle.IsResolved, Is.False);
            Assert.That(_owner.Enters, Is.EqualTo(1));
            var auto = _canvas.GetComponentsInChildren<Button>(false).Single(value => value.name == "Battle Auto Orders 091");
            Assert.That(auto.interactable, Is.True);
            PointerClick132(auto);
            Assert.That(controller.AutoOrdersEnabled091, Is.True);
            controller.SetAutoOrders091(false);
            // Hide/detach cancels only presentation, retaining the same battle.
            Assert.That(controller.BeginStoryBattleIntro132("COMMITTED_REQUEST132", "Saved battle"), Is.True);
            controller.SetVisible(false);
            Assert.That(controller.StoryBattleIntroHeld132, Is.False);
            Assert.That(controller.EnterCurrentBattle(), Is.True);
            controller.ReducedMotion = true;
            Assert.That(controller.BeginStoryBattleIntro132("COMMITTED_REQUEST132", "Saved battle"), Is.True);
            Assert.That(controller.StoryBattleIntroHeld132, Is.False, "Reduced motion is immediately ready.");
            Assert.That(_owner.Commands, Is.Zero);
            Assert.That(_owner.Enters, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator NoncombatUsesExistingIllustratedReadingAndCanceledContinueCannotApply132()
        {
            _owner.View.Kind = "STORY";
            Build132(); yield return null; yield return null;
            Assert.That(_canvas.GetComponentsInChildren<CommittedGlassShatter132>(false), Is.Empty);
            Assert.That(_canvas.GetComponentsInChildren<Image>(false)
                .Single(value => value.name == "Campaign Quest Illustration 131").sprite, Is.Not.Null);
            var button = Continue132();
            Assert.That(button.GetComponentInChildren<Text>().text, Is.EqualTo("CONTINUE STORY"));
            Assert.That(_canvas.GetComponentsInChildren<Text>(false)
                .Single(value => value.name == "Campaign Quest Progress 131" || value.name.StartsWith("Campaign Quest Progress 131 [", StringComparison.Ordinal)).text, Does.Contain("STORY MOMENT"));
            Assert.That(_owner.Completions, Is.Zero);
            var root = (RectTransform)typeof(M1FlowPresenter).GetField("_campaignQuestRoot131", Hidden).GetValue(_presenter);
            root.gameObject.SetActive(false);
            Assert.That(button.interactable, Is.False);
            button.onClick.Invoke();
            Assert.That(_owner.Completions, Is.Zero);
            Assert.That(_owner.Enters, Is.Zero);
            Assert.That(_owner.Commands, Is.Zero);
        }

        void PointerClick132(Button button)
        {
            Assert.That(button.interactable, Is.True);
            var rect = (RectTransform)button.transform;
            var point = RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, rect.TransformPoint(rect.rect.center));
            Assert.That(point.x, Is.InRange(0f, (float)Screen.width));
            Assert.That(point.y, Is.InRange(0f, (float)Screen.height));
            var pointer = new PointerEventData(EventSystem.current) { position = point, pressPosition = point,
                pointerId = -1, button = PointerEventData.InputButton.Left, eligibleForClick = true, clickCount = 1 };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            var hit = hits[0].gameObject;
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit), Is.EqualTo(button.gameObject),
                "The visible control must receive the actual pointer raycast.");
            pointer.pointerCurrentRaycast = pointer.pointerPressRaycast = hits[0];
            pointer.pointerPress = ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerClickHandler);
        }

        sealed class Owner132 : IM2PresentationCoordinator, IM2BattleViewReader098,
            ICampaignPlayablePresentationCoordinator020, ICampaignCardFlowCoordinator132
        {
            public event Action Changed { add { } remove { } }
            public int Enters, Commands, Completions;
            public M2BattleView Battle;
            public CampaignInterruptionView132 View = new CampaignInterruptionView132 {
                Identity = "COMMITTED_REQUEST132", Kind = "BOSS", OperationId = "OP132", StepIndex = 2,
                Title = "The authored encounter", Description = "The enemy waits on the road ahead." };
            public CampaignInterruptionView132 CampaignInterruption132 => View;
            public CampaignPlayablePresentationState020 CampaignPlayable020 { get; } = new CampaignPlayablePresentationState020 {
                IsAvailable = true, OperationTitle = "Synthetic story fixture", WorldName = "Skyhome" };
            public M1PresentationState State => new M1PresentationState { HasCampaign = true, Battle = Battle };
            public M2BattleView ReadBattleView098() => Battle;
            public M1CommandResult CompleteCampaignInterruption132(string identity)
            {
                if (View == null || identity != View.Identity) return M1CommandResult.Failure("Stale fixture receipt");
                Completions++;
                if (View.Kind != "BOSS") throw new InvalidOperationException("Canceled story applied.");
                View = null;
                return EnterPlayableBattle020();
            }
            public M1CommandResult EnterPlayableBattle020()
            {
                Enters++;
                Battle = new M2BattleView { BattleId = "BATTLE_CAMPAIGN132_SYNTHETIC", Outcome = "InProgress", Round = 1, CanConfirmRound = true,
                    PlayerUnions = new[] { Union("ALLY132", "Guild Union") },
                    EnemyUnions = new[] { Union("ENEMY132", "Story enemy") } };
                return M1CommandResult.Success();
            }
            static M2BattleUnionView Union(string id, string name) => new M2BattleUnionView {
                UnionId = id, DisplayName = name, CanAct = true, Members = new[] { new M2BattleMemberView {
                    MemberId = id + "_MEMBER", DisplayName = name, CurrentHp = 100, MaximumHp = 100 } } };
            M1CommandResult Unexpected() { Commands++; return M1CommandResult.Failure("Unexpected command in presentation fixture"); }
            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => Unexpected();
            public M1CommandResult SignRecruit(string id) => Unexpected();
            public M1CommandResult EquipItem(string id, string slot, string item) => Unexpected();
            public M1CommandResult UnequipItem(string id, string slot) => Unexpected();
            public M1CommandResult SetEquipmentLock(string id, string slot, bool locked) => Unexpected();
            public M1CommandResult CompleteEquipmentReview() => Unexpected();
            public M1CommandResult AddUnion() => Unexpected();
            public M1CommandResult RemoveUnion(int index) => Unexpected();
            public M1CommandResult AssignRecruitToUnion(string id, int union, int slot) => Unexpected();
            public M1CommandResult UnassignRecruitFromUnion(string id) => Unexpected();
            public M1CommandResult SetUnionLeader(int index, string id) => Unexpected();
            public M1CommandResult SetFormation(int index, string id) => Unexpected();
            public M1CommandResult SetDoctrine(int index, string id) => Unexpected();
            public M1CommandResult SaveAndReloadProof() => Unexpected();
            public M1CommandResult StartTutorialBattle() => Unexpected();
            public M1CommandResult SelectForecast(string unionId, string forecastId) => Unexpected();
            public M1CommandResult ConfirmBattleRound() => Unexpected();
            public M1CommandResult ReplayTutorialBattle() => Unexpected();
            public M1CommandResult RetryTutorialBattle() => Unexpected();
            public M1CommandResult ClaimBattleRewards() => Unexpected();
            public M1CommandResult StartPlayableChapter020(string chapterId) => Unexpected();
            public M1CommandResult CommitPlayableStep020(string outcome) => Unexpected();
            public M1CommandResult ApplyPlayableStep020() => Unexpected();
            public M1CommandResult CommitPlayableBattleStepResult020() => Unexpected();
            public M1CommandResult FinalizePlayableChapter020() => Unexpected();
            public M1CommandResult ApplyPlayableChapterResult020() => Unexpected();
            public M1CommandResult RecoverLegacyPlayableQuest020() => Unexpected();
            public M1CommandResult RecoverInsertedBattleBoundary020() => Unexpected();
            public M1CommandResult ResumeCampaignInterruption132(string operationId, int stepIndex) => Unexpected();
        }
    }
}
