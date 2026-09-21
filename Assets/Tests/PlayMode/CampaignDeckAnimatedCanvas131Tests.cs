#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign023;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class CampaignDeckAnimatedCanvas131Tests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject _host;
        Canvas _canvas;
        Camera _camera;
        RenderTexture _target;
        GameObject _events;

        [UnityTearDown]
        public IEnumerator Cleanup131()
        {
            if (_host != null) UnityEngine.Object.DestroyImmediate(_host);
            if (_canvas != null) UnityEngine.Object.DestroyImmediate(_canvas.gameObject);
            if (_camera != null)
            {
                _camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(_camera.gameObject);
            }
            if (_target != null) { _target.Release(); UnityEngine.Object.DestroyImmediate(_target); }
            if (_events != null) UnityEngine.Object.DestroyImmediate(_events);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AutomaticScreenSpaceDealAndChestRevealKeepActualHitRectsAndCopySeparated131()
        {
            foreach (var size in new[] { new Vector2Int(1280, 800), new Vector2Int(1920, 1080) })
            {
                var runtimeUi = typeof(M1FlowPresenter).Assembly.GetType("SecondDimension.Presentation.RuntimeUi");
                _canvas = (Canvas)runtimeUi.GetMethod("CreateCanvas", BindingFlags.Static | BindingFlags.Public)
                    .Invoke(null, new object[] { "Campaign Animated Canvas 131" });
                var scaler = _canvas.GetComponent<CanvasScaler>();
                Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
                Assert.That(scaler.referenceResolution, Is.EqualTo((Vector2)runtimeUi.GetField("ReferenceResolution").GetValue(null)));
                Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f));
                // Same screen-space layout/scaler and real Update/LateUpdate,
                // with an offscreen pixel target so Editor GameView preferences
                // and the user's desktop resolution remain untouched.
                _target = new RenderTexture(size.x, size.y, 24) { name = "Campaign Target 131" };
                _target.Create();
                _camera = new GameObject("Campaign Camera 131", typeof(Camera)).GetComponent<Camera>();
                _camera.targetTexture = _target;
                _camera.transform.position = new Vector3(0, 0, -10);
                _canvas.renderMode = RenderMode.ScreenSpaceCamera;
                _canvas.worldCamera = _camera;
                _canvas.planeDistance = 1f;
                if (EventSystem.current == null)
                    _events = new GameObject("Campaign Events 131", typeof(EventSystem));
                // Establish only the enclosing production canvas. The whole row
                // is then constructed in one frame, with no test layout flush.
                yield return null;
                yield return null;
                Assert.That(_canvas.pixelRect.width, Is.EqualTo(size.x).Within(1f));
                Assert.That(_canvas.pixelRect.height, Is.EqualTo(size.y).Within(1f));
                Assert.That(_canvas.renderingDisplaySize.x, Is.EqualTo(size.x).Within(1f));
                Assert.That(_canvas.renderingDisplaySize.y, Is.EqualTo(size.y).Within(1f));
                var expectedScale = Mathf.Sqrt((size.x / scaler.referenceResolution.x) * (size.y / scaler.referenceResolution.y));
                Assert.That(_canvas.scaleFactor, Is.EqualTo(expectedScale).Within(0.001f));
                var screen = Rect131(_canvas.transform, "Campaign Screen 131");
                screen.offsetMin = new Vector2(96f, 42f);
                screen.offsetMax = new Vector2(-96f, -42f);
                var body = Rect131(screen, "Campaign Body 131");
                _host = new GameObject("Campaign Presenter 131");
                var presenter = _host.AddComponent<M1FlowPresenter>();
                Set131(presenter, "_canvas", _canvas);
                Set131(presenter, "_coordinator", new PageOnly131());
                Set131(presenter, "_screen", (M1Screen)(-1));
                Set131(presenter, "_screenRoot", screen);
                Set131(presenter, "_activePage", body);
                Set131(presenter, "_reducedMotion", false);
                var owner = new DeckOwner131();
                var saved = CanonicalJson.Serialize(owner.CampaignWorldGate023);
                typeof(M1FlowPresenter).GetMethod("BuildGuildCityWorldGate023", Private)
                    .Invoke(presenter, new object[] { body, owner, owner.CampaignWorldGate023 });
                var root = (RectTransform)typeof(M1FlowPresenter).GetField("_campaignQuestRoot131", Private).GetValue(presenter);
                var table = (RectTransform)root.Find("Campaign Quest Card Table 131");
                var row = (RectTransform)table.Find("Expedition route row 089");
                var choice = row.GetComponent<ExpeditionCardChoice091>();
                Assert.That(choice.Phase091, Is.EqualTo(ExpeditionCardChoice091.ChoicePhase091.Dealing));
                Assert.That((bool)typeof(ExpeditionCardChoice091).GetField("_automaticTick", Private).GetValue(choice), Is.True);
                Assert.That((bool)typeof(ExpeditionCardChoice091).GetField("_animate", Private).GetValue(choice), Is.True);
                var deadline = Time.realtimeSinceStartup + 10f;
                while (choice.Phase091 == ExpeditionCardChoice091.ChoicePhase091.Dealing && Time.realtimeSinceStartup < deadline)
                    yield return null;
                yield return null;
                yield return null;
                Assert.That(choice.Phase091, Is.EqualTo(ExpeditionCardChoice091.ChoicePhase091.AwaitingChoice));
                var cards = row.GetComponentsInChildren<Button>(false)
                    .Where(value => value.name.StartsWith("Blind Quest Card Back ", StringComparison.Ordinal))
                    .OrderBy(value => value.name, StringComparer.Ordinal).ToArray();
                Assert.That(cards, Has.Length.EqualTo(3));
                var hitRects = cards.Select(value => Pixels131((RectTransform)value.transform)).ToArray();
                for (var i = 0; i < cards.Length; i++)
                {
                    Assert.That(cards[i].IsInteractable(), Is.True);
                    Inside131(Pixels131(table), hitRects[i], size + " dealt card " + i);
                    Assert.That(hitRects[i].width, Is.GreaterThan(80f));
                    var hit = Hit131(cards[i]);
                    Assert.That(hit, Is.EqualTo(cards[i].gameObject), "Each separated card must receive its own actual raycast.");
                    for (var j = i + 1; j < cards.Length; j++)
                        Assert.That(hitRects[i].Overlaps(hitRects[j]), Is.False, size + " automatic motion collapsed dealt cards " + i + "/" + j);
                }
                var help = root.GetComponentsInChildren<Button>(false).Single(value => value.name == "Campaign deck help 131");
                Assert.That(hitRects.Any(value => value.Overlaps(Pixels131((RectTransform)help.transform))), Is.False);
                var heading = (RectTransform)table.Find("Expedition three card route heading 089");
                Assert.That(Pixels131(heading).Overlaps(Pixels131((RectTransform)help.transform)), Is.False,
                    "Help must also stay outside the actual round/status heading.");
                Assert.That(owner.Commits, Is.Zero);
                Assert.That(CanonicalJson.Serialize(owner.CampaignWorldGate023), Is.EqualTo(saved));

                Click131(cards[1]);
                Assert.That(choice.Phase091, Is.EqualTo(ExpeditionCardChoice091.ChoicePhase091.Revealing));
                deadline = Time.realtimeSinceStartup + 10f;
                while (choice.Phase091 == ExpeditionCardChoice091.ChoicePhase091.Revealing && Time.realtimeSinceStartup < deadline)
                    yield return null;
                yield return null;
                yield return null;
                Assert.That(choice.Phase091, Is.EqualTo(ExpeditionCardChoice091.ChoicePhase091.AwaitingAction));
                var face = row.GetComponentsInChildren<RectTransform>(false).Single(value =>
                    value.name == "Expedition route card surface CHEST_CANVAS_131 089");
                var facePixels = Pixels131(face);
                Inside131(Pixels131(table), facePixels, size + " selected chest face");
                Assert.That(facePixels.width, Is.GreaterThan(Pixels131(row).width * 0.85f),
                    "The selected face must use the available three-card row, not the tiny former draft slot.");
                var reading = (RectTransform)face.Find("Board Card Reading Column 091");
                var viewport = (RectTransform)reading.Find("Expedition Route Card Copy Viewport 110");
                var content = (RectTransform)viewport.Find("Expedition Route Card Copy 110");
                var texts = content.GetComponentsInChildren<Text>(false).Where(value => !string.IsNullOrEmpty(value.text)).ToArray();
                Assert.That(texts.Any(value => value.name.StartsWith("Expedition card title ", StringComparison.Ordinal)), Is.True);
                Assert.That(texts.Any(value => value.name.StartsWith("Expedition card description ", StringComparison.Ordinal)), Is.True);
                Assert.That(texts.Any(value => value.name.StartsWith("Expedition card odds ", StringComparison.Ordinal)), Is.True);
                Assert.That(texts.Any(value => value.name.StartsWith("Expedition card reward ", StringComparison.Ordinal)), Is.True);
                for (var i = 0; i < texts.Length; i++)
                {
                    Assert.That(texts[i].rectTransform.rect.height + 1f, Is.GreaterThanOrEqualTo(texts[i].preferredHeight),
                        size + " text must reserve its actual wrapped font height: " + texts[i].name);
                    Assert.That(texts[i].rectTransform.rect.height,
                        Is.LessThanOrEqualTo(Mathf.Max(36f, Mathf.Ceil(texts[i].preferredHeight) + 4f) + 1f),
                        size + " text must not retain an inflated height measured at an obsolete width: " + texts[i].name);
                    for (var j = i + 1; j < texts.Length; j++)
                        Assert.That(Pixels131(texts[i].rectTransform).Overlaps(Pixels131(texts[j].rectTransform)), Is.False,
                            size + " adjacent copy rectangles overlap: " + texts[i].name + "/" + texts[j].name);
                }
                var title = texts.Single(value => value.name.StartsWith("Expedition card title ", StringComparison.Ordinal));
                Inside131(Pixels131(viewport), Pixels131(title.rectTransform), size + " initial chest title");
                var action = face.GetComponentsInChildren<Button>(false).Single(value =>
                    value.name == "Choose Expedition route card CHEST_CANVAS_131 089");
                Assert.That(action.IsInteractable(), Is.True);
                Inside131(Pixels131(reading), Pixels131((RectTransform)action.transform), size + " actual chest action");
                Assert.That(Pixels131(viewport).Overlaps(Pixels131((RectTransform)action.transform)), Is.False);
                Assert.That(owner.Commits, Is.Zero);
                Assert.That(CanonicalJson.Serialize(owner.CampaignWorldGate023), Is.EqualTo(saved));
                Click131(action);
                Assert.That(owner.Commits, Is.EqualTo(1));
                Assert.That(owner.CardId, Is.EqualTo("CHEST_CANVAS_131"));
                action.onClick.Invoke();
                Assert.That(owner.Commits, Is.EqualTo(1));
                yield return Cleanup131();
            }
        }

        static void Set131(object owner, string name, object value) => owner.GetType().GetField(name, Private).SetValue(owner, value);
        static RectTransform Rect131(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        Rect Pixels131(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var pixels = corners.Select(value => RectTransformUtility.WorldToScreenPoint(_camera, value)).ToArray();
            return Rect.MinMaxRect(pixels.Min(value => value.x), pixels.Min(value => value.y), pixels.Max(value => value.x), pixels.Max(value => value.y));
        }
        static void Inside131(Rect outer, Rect inner, string label)
        {
            Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - 1f), label);
            Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + 1f), label);
            Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - 1f), label);
            Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + 1f), label);
        }
        GameObject Hit131(Button button)
        {
            var pointer = new PointerEventData(EventSystem.current) { position = Pixels131((RectTransform)button.transform).center };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            return ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
        }
        void Click131(Button button)
        {
            Assert.That(Hit131(button), Is.EqualTo(button.gameObject));
            var pointer = new PointerEventData(EventSystem.current) { position = Pixels131((RectTransform)button.transform).center,
                button = PointerEventData.InputButton.Left, pointerId = -1, eligibleForClick = true };
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        sealed class DeckOwner131 : ICampaignWorldGatePresentationCoordinator023, IExpeditionDeckPresentationCoordinator089
        {
            public CampaignWorldGatePresentationState023 CampaignWorldGate023 { get; } = new CampaignWorldGatePresentationState023 {
                IsAvailable = true, CurrentWorldId = "SKYHOME", CurrentWorldName = "Skyhome",
                ActiveOperationId = "CHAPTER_CANVAS_131", ActiveDefinitionId = "CH018_002", ActiveBoardTitle = "The Lantern Road",
                ActiveOperationKind = "CHAPTER", ActiveStatus = "Active", BoardObjective = "Escort the crew safely through the next saved room.",
                Supplies = 4, TotalNodes = 5, CompletedNodes = 1, ExpeditionDeckTutorialSeen = false,
                CurrentNode = new NodeView023 { NodeId = "SAVED_ROOM_131", Kind = "EVENT", RoomKind = "STORY", Title = "A saved room" },
                RouteCards = new[] { Card131("STORY_CANVAS_131", "STORY"), Card131("CHEST_CANVAS_131", "CHEST"), Card131("CAMP_CANVAS_131", "CAMP") }
            };
            static ExpeditionRouteCardView089 Card131(string id, string category) => new ExpeditionRouteCardView089 {
                CardId = id, Category = category, Title = category == "CHEST" ? "Wayglass Cache Beneath the Watchtower" : "The Lantern Road",
                VisualResourcePath = "SecondDimension/Art/Board086/CardFaces/CARD_FACE_" + category + "_089",
                Description = "Search the old watchtower stores for supplies left behind by the previous expedition crew.",
                Odds = "NO ROLL REQUIRED — THE SAVED CHEST OPENS AFTER YOUR CHOICE", RiskLabel = "SAFE",
                OutcomePreview = "Open the chest and reveal its saved reward once.",
                RewardPreview = "Guild and Hall experience plus the saved weapon reward, sent to Inventory for review.",
                RouteLabel = "CONTINUE", IsEncounterRound = true, CanChoose = true, SuccessBasisPoints = 10000
            };
            public int Commits { get; private set; }
            public string CardId { get; private set; }
            public M1CommandResult CommitExpeditionRouteCard089(string id)
            {
                Assert.That(CampaignWorldGate023.RouteCards.Single(value => value.CardId == id).CanChoose, Is.True);
                ++Commits; CardId = id; return M1CommandResult.Success();
            }
            static M1CommandResult Unexpected131() => throw new InvalidOperationException("Rendering or selecting a blind card dispatched another authority command.");
            public M1CommandResult BeginWorldGateOperation023(string id) => Unexpected131();
            public M1CommandResult CommitWorldGateChoice023(string id) => Unexpected131();
            public M1CommandResult CommitAutomaticWorldGateRoom023() => Unexpected131();
            public M1CommandResult ApplyWorldGateReceipt023() => Unexpected131();
            public M1CommandResult EnterWorldGateBattle023() => Unexpected131();
            public M1CommandResult FinalizeWorldGateOperation023() => Unexpected131();
            public M1CommandResult TravelWorldGate023(string id) => Unexpected131();
            public M1CommandResult RecoverLegacyWorldGateQuest023() => Unexpected131();
            public M1CommandResult EnterExpeditionCardBattle089() => Unexpected131();
            public M1CommandResult AcknowledgeExpeditionDeckTutorial089() => Unexpected131();
        }
        sealed class PageOnly131 : IM1PresentationCoordinator
        {
            public event Action Changed { add { } remove { } }
            public M1PresentationState State { get; } = new M1PresentationState();
            static M1CommandResult Unexpected131() => throw new InvalidOperationException("Unexpected command in isolated page fixture.");
            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => Unexpected131();
            public M1CommandResult SignRecruit(string id) => Unexpected131();
            public M1CommandResult EquipItem(string id, string slot, string item) => Unexpected131();
            public M1CommandResult UnequipItem(string id, string slot) => Unexpected131();
            public M1CommandResult SetEquipmentLock(string id, string slot, bool locked) => Unexpected131();
            public M1CommandResult CompleteEquipmentReview() => Unexpected131();
            public M1CommandResult AddUnion() => Unexpected131();
            public M1CommandResult RemoveUnion(int index) => Unexpected131();
            public M1CommandResult AssignRecruitToUnion(string id, int union, int slot) => Unexpected131();
            public M1CommandResult UnassignRecruitFromUnion(string id) => Unexpected131();
            public M1CommandResult SetUnionLeader(int union, string id) => Unexpected131();
            public M1CommandResult SetFormation(int union, string id) => Unexpected131();
            public M1CommandResult SetDoctrine(int union, string id) => Unexpected131();
            public M1CommandResult SaveAndReloadProof() => Unexpected131();
        }
    }
}
#endif
