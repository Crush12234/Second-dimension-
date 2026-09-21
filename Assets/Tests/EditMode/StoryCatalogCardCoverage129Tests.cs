#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign023;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    // Catalog/presentation coverage only: no campaign creation, completion,
    // unlock, reward, save, or gameplay command is simulated by these tests.
    public sealed class StoryCatalogCardCoverage129Tests
    {
        CampaignRegistry020 _chapters;
        CampaignRegistry023 _boards;

        [OneTimeSetUp]
        public void LoadAuthoredCatalogs129()
        {
            _chapters = CampaignRegistry020.LoadFromResources();
            _boards = CampaignRegistry023.LoadFromResources();
        }

        [Test]
        public void All82AuthoredChapterEntriesReachTheSharedStoryCardRenderer129()
        {
            var ids = Enumerable.Range(1, 82).Select(value => "CH018_" + value.ToString("000")).ToArray();
            CollectionAssert.AreEquivalent(ids, _chapters.Base019.Base018.Chapters.Keys);
            CollectionAssert.AreEquivalent(ids, _chapters.Base019.Chapters.Keys);
            CollectionAssert.AreEquivalent(ids, _chapters.Blueprints.Keys);
            CollectionAssert.AreEquivalent(ids, _boards.Boards.Values
                .Where(value => value.operationKind == "CHAPTER").Select(value => value.definitionId));
            Assert.That(typeof(ICampaignPlayablePresentationCoordinator020)
                .IsAssignableFrom(typeof(M1RuntimeCoordinator)), Is.True,
                "The shipping coordinator must expose the normal playable Start route.");
            Assert.That(typeof(ICampaignDeckPresentationCoordinator131)
                .IsAssignableFrom(typeof(M1RuntimeCoordinator)), Is.True,
                "The shipping coordinator must expose the single existing chapter/deck start boundary.");

            var rules = new Campaign020RuleCatalogAdapter(_chapters);
            foreach (var id in ids)
            {
                Assert.That(rules.TryGetBlueprint(id, out var blueprint), Is.True, id);
                Assert.That(CampaignAdventureRules084.IsCompatible084(blueprint, out var error),
                    Is.True, id + ": " + error);
                Assert.That(blueprint.Steps.Count(CampaignAdventureRules084.IsWorldBoardStep084),
                    Is.EqualTo(1), id);
                AssertRendered129(_chapters.Blueprints[id], 0, false);
            }
        }

        [Test]
        public void EveryAuthoredStepKindAndFinalChapterReturnKeepsItsSharedCardAction129()
        {
            var representatives = _chapters.Blueprints.Values
                .OrderByDescending(value => value.chapterId, StringComparer.Ordinal)
                .SelectMany(blueprint => blueprint.steps.Select((step, index) => new { blueprint, step, index }))
                .GroupBy(value => value.step.kind, StringComparer.Ordinal)
                .Select(group => group.First()).ToArray();
            CollectionAssert.AreEquivalent(new[] { "BRIEFING", "WORLD_BOARD", "CIVIC_EVENT",
                "DIPLOMACY", "FORTRESS_PREPARATION", "NONCOMBAT_RESOLUTION", "CERTIFIED_BATTLE", "RESULTS" },
                representatives.Select(value => value.step.kind));
            foreach (var value in representatives)
                AssertRendered129(value.blueprint, value.index, false);
            var finalChapter = _chapters.Blueprints["CH018_082"];
            AssertRendered129(finalChapter, finalChapter.steps.Length, true);
        }

        static void AssertRendered129(ChapterOperationBlueprint020 blueprint, int currentIndex, bool finalReturn)
        {
            var state = new CampaignPlayablePresentationState020
            {
                IsAvailable = true,
                ActiveOperationId = "CATALOG_RENDER_ONLY_" + blueprint.chapterId,
                ActiveChapterId = blueprint.chapterId,
                OperationTitle = blueprint.title,
                WorldId = blueprint.worldId,
                Status = finalReturn ? "ReadyToFinalize" : "Active",
                CurrentStepIndex = currentIndex,
                TotalSteps = blueprint.steps.Length,
                Steps = blueprint.steps.Select((step, index) => new CampaignStepView020
                {
                    StepId = step.stepId, Kind = step.kind,
                    TileType = CampaignAdventureRules084.TileType084(step.kind),
                    ActionLabel = CampaignAdventureRules084.ActionLabel084(step.kind),
                    Title = step.title, Description = step.description,
                    Status = index == currentIndex ? "CURRENT" : index < currentIndex ? "COMPLETED" : "FACE_DOWN",
                    RequiresCertifiedBattle = step.requiresCertifiedBattle,
                    ConsumesOperation = step.consumesOperation,
                    IsWorldBoard = StringComparer.Ordinal.Equals(step.kind, "WORLD_BOARD")
                }).ToArray()
            };
            var owner = new RenderOnlyCoordinator129(state);
            var screen = new GameObject("Catalog Story Screen 129", typeof(RectTransform));
            screen.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 1080f);
            var host = new GameObject("Catalog Story Presenter 129");
            var presenter = host.AddComponent<M1FlowPresenter>();
            presenter.enabled = false; // Static EditMode projection: no delayed viewport coroutine.
            try
            {
                typeof(M1FlowPresenter).GetField("_screenRoot", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(presenter, screen.GetComponent<RectTransform>());
                var build = typeof(M1FlowPresenter).GetMethod("BuildGuildCityCampaign020",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(build, Is.Not.Null);
                build.Invoke(presenter, new object[] { screen.transform, owner, state });

                var table = screen.transform.Find("Campaign Quest Shell 131");
                Assert.That(table, Is.Not.Null, blueprint.chapterId + " / " + currentIndex);
                Assert.That(table.gameObject.activeInHierarchy, Is.True);
                Assert.That(table.GetComponentsInChildren<Text>().Any(value =>
                    value.text == blueprint.title), Is.True, "The current chapter identity must be retained.");
                Assert.That(screen.GetComponentsInChildren<Transform>(true).Any(value =>
                    value.name == "Campaign Compact Progress Strip 084"), Is.False);

                var current = state.Steps.FirstOrDefault(value => value.Status == "CURRENT");
                if (!finalReturn && current.IsWorldBoard)
                {
                    Assert.That(table.GetComponentsInChildren<Button>().Count(value =>
                        value.name.StartsWith("Blind Quest Card Back ", StringComparison.Ordinal) && value.IsInteractable()), Is.EqualTo(3));
                    Assert.That(table.GetComponentsInChildren<Button>(true).Any(value =>
                        value.name == "Open campaign adventure board 084" || value.name == "Resume saved campaign deck 131"), Is.False);
                    Assert.That(owner.CampaignWorldGate023.ActiveDefinitionId, Is.EqualTo(blueprint.chapterId));
                    Assert.That(owner.Commands, Is.Zero);
                    return;
                }
                var actionName = finalReturn ? "Complete campaign chapter 084" :
                    current.RequiresCertifiedBattle ? "Enter campaign battle tile 084" :
                    "Move forward campaign room 084";
                var action = table.GetComponentsInChildren<Button>().SingleOrDefault(value => value.name == actionName);
                Assert.That(action, Is.Not.Null, blueprint.chapterId + " / " + actionName);
                Assert.That(action.IsInteractable(), Is.True);
                Assert.That(table.GetComponentsInChildren<Transform>().Any(value => value.name == "Sealed Story Card 129"), Is.False);
                Assert.That(table.GetComponentsInChildren<Image>().Single(value =>
                    value.name == "Campaign Quest Illustration 131").sprite, Is.Not.Null);
                if (!finalReturn && !current.IsWorldBoard && !current.RequiresCertifiedBattle)
                {
                    Assert.That(action.GetComponentInChildren<Text>().text, Is.EqualTo("CONTINUE"));
                    Assert.That(table.GetComponentsInChildren<Text>().Any(value => value.text == current.Title), Is.True);
                    Assert.That(table.GetComponentsInChildren<Text>().Any(value => value.text == current.Description), Is.True);
                }
                Assert.That(owner.Commands, Is.Zero);
            }
            finally
            {
                // EditMode: destroy each small projection immediately so the
                // catalog loop does not retain 82 UI hierarchies until a frame.
                UnityEngine.Object.DestroyImmediate(screen);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        sealed class RenderOnlyCoordinator129 : ICampaignPlayablePresentationCoordinator020,
            ICampaignWorldGatePresentationCoordinator023, IExpeditionDeckPresentationCoordinator089
        {
            public RenderOnlyCoordinator129(CampaignPlayablePresentationState020 state)
            {
                CampaignPlayable020 = state;
                CampaignWorldGate023 = new CampaignWorldGatePresentationState023 {
                    IsAvailable = true, CurrentWorldId = state.WorldId, CurrentWorldName = state.WorldName,
                    ActiveOperationId = "CATALOG_BOARD_" + state.ActiveChapterId,
                    ActiveDefinitionId = state.ActiveChapterId, ActiveBoardTitle = state.OperationTitle,
                    ActiveOperationKind = "CHAPTER", ActiveStatus = "Active", ExpeditionDeckTutorialSeen = true,
                    TotalNodes = 3, CurrentNode = new NodeView023 { NodeId = "CATALOG_NODE", Kind = "EVENT", Title = "Authored board projection" },
                    RouteCards = new[] { "STORY", "CHEST", "CAMP" }.Select(category => new ExpeditionRouteCardView089 {
                        CardId = "CATALOG_CARD_" + category, Category = category, Title = category,
                        CanChoose = true, IsEncounterRound = true,
                        VisualResourcePath = "SecondDimension/Art/Board086/CardFaces/CARD_FACE_" + category + "_089"
                    }).ToArray()
                };
            }
            public CampaignPlayablePresentationState020 CampaignPlayable020 { get; }
            public CampaignWorldGatePresentationState023 CampaignWorldGate023 { get; }
            public int Commands { get; private set; }
            M1CommandResult RejectCommand129() { ++Commands; throw new InvalidOperationException("A catalog renderer dispatched gameplay."); }
            public M1CommandResult StartPlayableChapter020(string chapterId) => RejectCommand129();
            public M1CommandResult CommitPlayableStep020(string outcome) => RejectCommand129();
            public M1CommandResult ApplyPlayableStep020() => RejectCommand129();
            public M1CommandResult EnterPlayableBattle020() => RejectCommand129();
            public M1CommandResult CommitPlayableBattleStepResult020() => RejectCommand129();
            public M1CommandResult FinalizePlayableChapter020() => RejectCommand129();
            public M1CommandResult ApplyPlayableChapterResult020() => RejectCommand129();
            public M1CommandResult RecoverLegacyPlayableQuest020() => RejectCommand129();
            public M1CommandResult RecoverInsertedBattleBoundary020() => RejectCommand129();
            public M1CommandResult BeginWorldGateOperation023(string id) => RejectCommand129();
            public M1CommandResult CommitWorldGateChoice023(string id) => RejectCommand129();
            public M1CommandResult CommitAutomaticWorldGateRoom023() => RejectCommand129();
            public M1CommandResult ApplyWorldGateReceipt023() => RejectCommand129();
            public M1CommandResult EnterWorldGateBattle023() => RejectCommand129();
            public M1CommandResult FinalizeWorldGateOperation023() => RejectCommand129();
            public M1CommandResult TravelWorldGate023(string id) => RejectCommand129();
            public M1CommandResult RecoverLegacyWorldGateQuest023() => RejectCommand129();
            public M1CommandResult CommitExpeditionRouteCard089(string id) => RejectCommand129();
            public M1CommandResult EnterExpeditionCardBattle089() => RejectCommand129();
            public M1CommandResult AcknowledgeExpeditionDeckTutorial089() => RejectCommand129();
        }
    }
}
#endif
