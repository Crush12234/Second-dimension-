using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Creator028;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    /// <summary>
    /// Focused presentation proof for the phone-simple Enter Code page. These
    /// tests invoke only the code-page presenter and its coordinator seam.
    /// </summary>
    public sealed class CreatorCodePhoneSimple084PlayModeTests
    {
        private static readonly Vector2[] SupportedResolutions084 =
        {
            new Vector2(1920f, 1080f),
            new Vector2(1280f, 800f)
        };

        [UnityTearDown]
        public IEnumerator TearDownCreatorCodePhoneSimple084()
        {
            foreach (var root in UnityEngine.Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (root.parent == null &&
                    root.name.StartsWith(
                        "Creator Code Phone PlayMode ",
                        StringComparison.Ordinal))
                    UnityEngine.Object.Destroy(root.gameObject);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator DefaultPageHasOneClearCodeFlowAndNoManagementLedger084()
        {
            foreach (var resolution in SupportedResolutions084)
            {
                var coordinator = new CreatorCoordinator084(RoomState084(true, true));
                var harness = CreateHarness084(resolution, "Default", coordinator);
                BuildCodes084(harness, coordinator);
                Rebuild084(harness.Page, harness.Body);

                var visible = VisibleText084(harness.Body);
                Assert.That(visible, Does.Contain("REDEEM A BONUS CODE"));
                Assert.That(visible, Does.Contain(
                    "Nothing is claimed until you choose any required target and press Claim Reward."));
                Assert.That(visible, Does.Contain("REWARDS CLAIMED 7"));
                Assert.That(visible, Does.Not.Contain("YOUR CLAIMS"));
                Assert.That(visible, Does.Not.Contain("BONUS TOKENS"));
                Assert.That(visible, Does.Not.Contain("STORIES"));
                Assert.That(visible, Does.Not.Contain("ROOM KEYS"));
                Assert.That(visible, Does.Not.Contain("SECRET ROOMS"));
                Assert.That(visible, Does.Not.Contain("999"),
                    "The total catalog count is not a player task or progress goal.");

                var inputs = harness.Body.GetComponentsInChildren<InputField>(true);
                Assert.That(inputs, Has.Length.EqualTo(1));
                Assert.That(inputs[0].name, Is.EqualTo("Creator Code 028"));
                var check = ButtonsWithLabel084(harness.Body, "CHECK CODE");
                Assert.That(check, Has.Length.EqualTo(1));
                var toggle = ButtonsWithLabel084(
                    harness.Body,
                    M1FlowPresenter.CreatorBonusRoomsToggleLabel084);
                Assert.That(toggle, Has.Length.EqualTo(1));
                Assert.That(FindRect084(
                    harness.Body,
                    "Creator Optional Bonus Room Card 084"), Is.Null,
                    "Bonus-room management must be closed on first entry.");

                var inputRow = FindRect084(harness.Body, "Creator Code Input 028");
                AssertInside084(inputRow, inputs[0].GetComponent<RectTransform>(),
                    resolution + " code input");
                AssertInside084(inputRow, check[0].GetComponent<RectTransform>(),
                    resolution + " Check Code");
                AssertTouchAndController084(inputs[0], resolution + " code input");
                AssertTouchAndController084(check[0], resolution + " Check Code");
                AssertTouchAndController084(toggle[0], resolution + " optional-room toggle");

                SetPrivateField084(harness.Presenter, "_creatorCodeInput028", "PHONE-CODE-084");
                SetPrivateField084(harness.Presenter, "_coordinator", null);
                check[0].onClick.Invoke();
                Assert.That(coordinator.PreviewCalls, Is.EqualTo(1));
                Assert.That(coordinator.LastPreviewInput, Is.EqualTo("PHONE-CODE-084"));

                DestroyHarness084(harness);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ClaimedWeaponShowsImmediateManualEquipHandoff087()
        {
            foreach (var resolution in SupportedResolutions084)
            {
                var coordinator = new CreatorCoordinator084(RoomState084(false, true));
                var harness = CreateHarness084(resolution, "EquipmentHandoff", coordinator);
                SetPrivateField084(
                    harness.Presenter,
                    "_creatorClaimedEquipmentId087",
                    "CRITEM10000_HANDOFF_087");
                SetPrivateField084(
                    harness.Presenter,
                    "_creatorClaimedEquipmentName087",
                    "Swords — Balanced Prototype");
                BuildCodes084(harness, coordinator);
                Rebuild084(harness.Page, harness.Body);

                var visible = VisibleText084(harness.Body);
                Assert.That(visible, Does.Contain("WEAPON ADDED TO YOUR ARMORY"));
                Assert.That(visible, Does.Contain("Swords — Balanced Prototype"));
                Assert.That(visible, Does.Contain(
                    "preview every combat-stat change before equipping"));
                var choose = ButtonsWithLabel084(
                    harness.Body,
                    "CHOOSE WHO EQUIPS IT");
                Assert.That(choose, Has.Length.EqualTo(1));
                AssertTouchAndController084(
                    choose[0],
                    resolution + " Creator weapon Armory handoff");

                DestroyHarness084(harness);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator OptionalRoomsStayClosedThenShowOnlyPlayerFacingRoomCopy084()
        {
            foreach (var resolution in SupportedResolutions084)
            {
                var active = new CreatorCoordinator084(RoomState084(true, true));
                var activeHarness = CreateHarness084(resolution, "ActiveRoom", active);
                SetPrivateField084(activeHarness.Presenter, "_creatorBonusRoomsOpen084", true);
                BuildCodes084(activeHarness, active);
                Rebuild084(activeHarness.Page, activeHarness.Body);

                var activeCopy = VisibleText084(activeHarness.Body);
                Assert.That(activeCopy, Does.Contain("Moonlit Cache"));
                Assert.That(activeCopy, Does.Contain(
                    "A quiet side chamber holds a gift for the Guild."));
                Assert.That(activeCopy, Does.Contain("READY"));
                Assert.That(activeCopy, Does.Contain("REWARD  •  STORY BONUS"));
                Assert.That(activeCopy, Does.Not.Contain("VISIT_TECH_028"));
                Assert.That(activeCopy, Does.Not.Contain("ROOM_TECH_028"));
                Assert.That(activeCopy, Does.Not.Contain("REWARD_TECH_028"));
                Assert.That(activeCopy, Does.Not.Contain("KEY_TECH_028"));
                Assert.That(activeCopy, Does.Not.Contain("ENTRY_TECH_028"));
                Assert.That(activeCopy, Does.Not.Contain("AUTHORED_SECRET_TECH"));
                var finish = ButtonsWithLabel084(activeHarness.Body, "FINISH BONUS ROOM");
                Assert.That(finish, Has.Length.EqualTo(1));
                var roomCard = FindRect084(
                    activeHarness.Body,
                    "Creator Optional Bonus Room Card 084");
                AssertInside084(roomCard, finish[0].GetComponent<RectTransform>(),
                    resolution + " finish bonus room");
                AssertTouchAndController084(finish[0], resolution + " finish bonus room");

                DestroyHarness084(activeHarness);
                yield return null;

                var locked = new CreatorCoordinator084(RoomState084(false, false));
                var lockedHarness = CreateHarness084(resolution, "LockedRoom", locked);
                SetPrivateField084(lockedHarness.Presenter, "_creatorBonusRoomsOpen084", true);
                BuildCodes084(lockedHarness, locked);
                Rebuild084(lockedHarness.Page, lockedHarness.Body);
                var lockedCopy = VisibleText084(lockedHarness.Body);
                Assert.That(lockedCopy, Does.Contain("LOCKED"));
                Assert.That(lockedCopy, Does.Contain("REWARD  •  STORY BONUS"));
                Assert.That(lockedCopy, Does.Not.Contain("KEY_TECH_028"));
                Assert.That(ButtonsWithLabel084(
                    lockedHarness.Body,
                    "ENTER BONUS ROOM"), Is.Empty);
                Assert.That(ButtonsWithLabel084(
                    lockedHarness.Body,
                    "FINISH BONUS ROOM"), Is.Empty);

                DestroyHarness084(lockedHarness);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator RewardPreviewShowsTargetOnlyWhenRequiredAndOneClaimAction084()
        {
            foreach (var resolution in SupportedResolutions084)
            {
                var noTarget = new CreatorCoordinator084(RoomState084(false, true));
                var noTargetHarness = CreateHarness084(resolution, "NoTarget", noTarget);
                SetPrivateField084(noTargetHarness.Presenter, "_creatorCodeInput028", "CACHE-084");
                SetPrivateField084(
                    noTargetHarness.Presenter,
                    "_creatorRewardPreview028",
                    Preview084(false, false));
                BuildCodes084(noTargetHarness, noTarget);
                Rebuild084(noTargetHarness.Page, noTargetHarness.Body);

                var noTargetCopy = VisibleText084(noTargetHarness.Body);
                Assert.That(noTargetCopy, Does.Contain("REWARD PREVIEW"));
                Assert.That(noTargetCopy, Does.Contain("REWARD CACHE  •  Guild Supply Cache"));
                Assert.That(noTargetCopy, Does.Not.Contain("CHOOSE THE TARGET"));
                var noTargetClaim = ButtonsWithLabel084(
                    noTargetHarness.Body,
                    M1FlowPresenter.CreatorClaimActionLabel084);
                Assert.That(noTargetClaim, Has.Length.EqualTo(1));
                AssertTouchAndController084(
                    noTargetClaim[0],
                    resolution + " untargeted Claim Reward");

                DestroyHarness084(noTargetHarness);
                yield return null;

                var target = new CreatorCoordinator084(RoomState084(false, true));
                var waitingHarness = CreateHarness084(resolution, "TargetWaiting", target);
                SetPrivateField084(waitingHarness.Presenter, "_creatorCodeInput028", "POWER-084");
                SetPrivateField084(
                    waitingHarness.Presenter,
                    "_creatorRewardPreview028",
                    Preview084(true, true));
                BuildCodes084(waitingHarness, target);
                Rebuild084(waitingHarness.Page, waitingHarness.Body);
                Assert.That(VisibleText084(waitingHarness.Body), Does.Contain("CHOOSE THE TARGET"));
                Assert.That(ButtonsWithLabel084(
                    waitingHarness.Body,
                    M1FlowPresenter.CreatorClaimActionLabel084), Is.Empty,
                    "A required target must be selected before Claim Reward appears.");
                DestroyHarness084(waitingHarness);
                yield return null;

                var selectedHarness = CreateHarness084(resolution, "TargetSelected", target);
                SetPrivateField084(selectedHarness.Presenter, "_creatorCodeInput028", "POWER-084");
                SetPrivateField084(
                    selectedHarness.Presenter,
                    "_creatorRewardPreview028",
                    Preview084(true, true));
                SetPrivateField084(selectedHarness.Presenter, "_selectedCreatorTarget028", "TARGET_A");
                BuildCodes084(selectedHarness, target);
                Rebuild084(selectedHarness.Page, selectedHarness.Body);
                var claims = ButtonsWithLabel084(
                    selectedHarness.Body,
                    M1FlowPresenter.CreatorClaimActionLabel084);
                Assert.That(claims, Has.Length.EqualTo(1));
                Assert.That(VisibleText084(selectedHarness.Body),
                    Does.Not.Contain("I UNDERSTAND — CLAIM CREATOR"));
                AssertTouchAndController084(claims[0], resolution + " targeted Claim Reward");

                SetPrivateField084(selectedHarness.Presenter, "_coordinator", null);
                claims[0].onClick.Invoke();
                Assert.That(target.RedeemCalls, Is.EqualTo(1));
                Assert.That(target.LastRedeemInput, Is.EqualTo("POWER-084"));
                Assert.That(target.LastRedeemTarget, Is.EqualTo("TARGET_A"));
                Assert.That(target.LastCreatorPowerConfirmation, Is.True,
                    "The simplified Claim Reward button must preserve required confirmation authority.");

                DestroyHarness084(selectedHarness);
                yield return null;
            }
        }

        private static CreatorRewardPreview028 Preview084(
            bool requiresTarget,
            bool creatorPower) =>
            new CreatorRewardPreview028
            {
                IsValid = true,
                Label = requiresTarget ? "Guildmaster Growth" : "Guild Supply Cache",
                Category = requiresTarget ? "XP_VOUCHER" : "MULTI_GRANT",
                RewardSummary = requiresTarget
                    ? "500 Personal XP for one chosen adventurer."
                    : "Three useful rewards saved together.",
                TargetMode = requiresTarget ? "PLAYER_SELECT_CHARACTER" : string.Empty,
                TargetPrompt = requiresTarget
                    ? "Choose the adventurer who receives this Personal XP."
                    : string.Empty,
                TargetOptions = requiresTarget
                    ? new[]
                    {
                        new CreatorRewardTargetOption028
                        {
                            TargetId = "TARGET_A",
                            DisplayName = "Maren Holt",
                            Detail = "Level 4 • Vanguard"
                        },
                        new CreatorRewardTargetOption028
                        {
                            TargetId = "TARGET_B",
                            DisplayName = "Odelia Fen",
                            Detail = "Level 4 • Ranger"
                        }
                    }
                    : Array.Empty<CreatorRewardTargetOption028>(),
                RequiresCreatorPowerConfirmation = creatorPower,
                CreatorPowerWarning = creatorPower
                    ? "This reward marks the campaign as using Creator Power."
                    : string.Empty
            };

        private static CreatorAccessPresentationState028 RoomState084(
            bool active,
            bool ready) =>
            new CreatorAccessPresentationState028
            {
                IsAvailable = true,
                TotalCodes = 999,
                RedeemedCodes = 7,
                CreatorTokens = 91,
                UnlockedContent = 82,
                RoomKeys = 73,
                CompletedRooms = 64,
                ActiveRoomVisitId = active ? "VISIT_TECH_028" : string.Empty,
                LastCheckpointId = "CHECKPOINT_TECH_028",
                CurrentRoom = new CreatorRoomView028
                {
                    RoomId = "ROOM_TECH_028",
                    DisplayName = "Moonlit Cache",
                    RoomType = "AUTHORED_SECRET_TECH",
                    Description = "A quiet side chamber holds a gift for the Guild.",
                    RewardKind = "CONTENT",
                    RewardId = "REWARD_TECH_028",
                    EntryNodeId = "ENTRY_TECH_028",
                    RequiredRoomKeyId = "KEY_TECH_028",
                    KeyUnlocked = ready
                }
            };

        private static Harness084 CreateHarness084(
            Vector2 resolution,
            string suffix,
            CreatorCoordinator084 coordinator)
        {
            EnsureEventSystem084();
            var presenterObject = new GameObject(
                "Creator Code Phone PlayMode Presenter 084 " + suffix + " " + resolution.x);
            var presenter = presenterObject.AddComponent<M1FlowPresenter>();
            var canvasObject = new GameObject(
                "Creator Code Phone PlayMode Canvas 084 " + suffix + " " + resolution.x,
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
                "Creator Code Phone PlayMode Screen Root 084");
            SetPrivateField084(presenter, "_screenRoot", screenRoot);
            SetPrivateField084(presenter, "_coordinator", coordinator);

            var createPage = typeof(M1FlowPresenter).GetMethod(
                "CreatePage",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(createPage, Is.Not.Null);
            var body = (RectTransform)createPage.Invoke(
                presenter,
                new object[] { "ENTER CODE", "CLAIM BONUS REWARDS", null });
            var scroll = body.GetComponentInParent<ScrollRect>();
            Assert.That(scroll, Is.Not.Null);
            return new Harness084(
                presenter,
                canvasObject,
                screenRoot,
                scroll.transform.parent as RectTransform,
                body);
        }

        private static void BuildCodes084(
            Harness084 harness,
            CreatorCoordinator084 coordinator)
        {
            var method = typeof(M1FlowPresenter).GetMethod(
                "BuildCreatorCodesRooms028",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(
                harness.Presenter,
                new object[] { harness.Body, coordinator, coordinator.CreatorAccess028 });
        }

        private static void EnsureEventSystem084()
        {
            if (EventSystem.current != null) return;
            new GameObject(
                "Creator Code Phone PlayMode Event System 084",
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

        private static void Rebuild084(RectTransform page, RectTransform body)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var rect in page.GetComponentsInChildren<RectTransform>(true)
                         .OrderByDescending(Depth084))
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(body);
            LayoutRebuilder.ForceRebuildLayoutImmediate(page);
            Canvas.ForceUpdateCanvases();
        }

        private static int Depth084(Transform value)
        {
            var depth = 0;
            for (var current = value; current != null; current = current.parent)
                depth++;
            return depth;
        }

        private static string VisibleText084(Transform root) =>
            string.Join("\n", root.GetComponentsInChildren<Text>(true)
                .Select(value => value.text));

        private static Button[] ButtonsWithLabel084(Transform root, string label) =>
            root.GetComponentsInChildren<Button>(true)
                .Where(value => StringComparer.Ordinal.Equals(
                    value.GetComponentInChildren<Text>(true)?.text,
                    label))
                .ToArray();

        private static RectTransform FindRect084(Transform root, string name) =>
            root.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(value => StringComparer.Ordinal.Equals(value.name, name));

        private static void AssertTouchAndController084(Selectable selectable, string label)
        {
            Assert.That(selectable, Is.Not.Null, label);
            var rect = selectable.GetComponent<RectTransform>();
            Assert.That(WorldSize084(rect).y, Is.GreaterThanOrEqualTo(132f),
                label + " touch height");
            Assert.That(selectable.navigation.mode, Is.Not.EqualTo(Navigation.Mode.None),
                label + " controller navigation");
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
            Assert.That(outer, Is.Not.Null, label + " outer");
            Assert.That(inner, Is.Not.Null, label + " inner");
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
                RectTransform screenRoot,
                RectTransform page,
                RectTransform body)
            {
                Presenter = presenter;
                CanvasObject = canvasObject;
                ScreenRoot = screenRoot;
                Page = page;
                Body = body;
            }

            public M1FlowPresenter Presenter { get; }
            public GameObject CanvasObject { get; }
            public RectTransform ScreenRoot { get; }
            public RectTransform Page { get; }
            public RectTransform Body { get; }
        }

        private sealed class CreatorCoordinator084 :
            IM1PresentationCoordinator,
            ICreatorAccessPresentationCoordinator028
        {
            public CreatorCoordinator084(CreatorAccessPresentationState028 state)
            {
                CreatorAccess028 = state;
                State = new M1PresentationState
                {
                    HasCampaign = true,
                    HasSave = true,
                    ResumeScreen = M1Screen.GuildOperations,
                    GuildLevel = 2,
                    GuildXpIntoCurrentLevel = 58,
                    GuildXpRequiredForNextLevel = 400,
                    TreasuryXp = 42
                };
            }

            public event Action Changed;
            public M1PresentationState State { get; }
            public CreatorAccessPresentationState028 CreatorAccess028 { get; }
            public CreatorRewardPreview028 Preview { get; set; } = Preview084(false, false);
            public int PreviewCalls { get; private set; }
            public string LastPreviewInput { get; private set; }
            public int RedeemCalls { get; private set; }
            public string LastRedeemInput { get; private set; }
            public string LastRedeemTarget { get; private set; }
            public bool LastCreatorPowerConfirmation { get; private set; }

            public CreatorRewardPreview028 PreviewCreatorCode028(string input)
            {
                PreviewCalls++;
                LastPreviewInput = input;
                return Preview;
            }

            public M1CommandResult RedeemCreatorCode028(string input) =>
                RedeemCreatorCode028(input, string.Empty, false);

            public M1CommandResult RedeemCreatorCode028(
                string input,
                string targetId,
                bool creatorPowerConfirmed)
            {
                RedeemCalls++;
                LastRedeemInput = input;
                LastRedeemTarget = targetId;
                LastCreatorPowerConfirmation = creatorPowerConfirmed;
                return M1CommandResult.Success("Reward saved once for test.");
            }

            public M1CommandResult EnterCreatorRoom028() =>
                M1CommandResult.Success("Entered bonus room for test.");

            public M1CommandResult ResolveCreatorRoom028() =>
                M1CommandResult.Success("Resolved bonus room for test.");

            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => Pass084();
            public M1CommandResult SignRecruit(string recruitId) => Pass084();
            public M1CommandResult EquipItem(
                string recruitId, string slotId, string itemId) => Pass084();
            public M1CommandResult UnequipItem(string recruitId, string slotId) => Pass084();
            public M1CommandResult SetEquipmentLock(
                string recruitId, string slotId, bool locked) => Pass084();
            public M1CommandResult CompleteEquipmentReview() => Pass084();
            public M1CommandResult AddUnion() => Pass084();
            public M1CommandResult RemoveUnion(int unionIndex) => Pass084();
            public M1CommandResult AssignRecruitToUnion(
                string recruitId, int unionIndex, int slotIndex) => Pass084();
            public M1CommandResult UnassignRecruitFromUnion(string recruitId) => Pass084();
            public M1CommandResult SetUnionLeader(
                int unionIndex, string recruitId) => Pass084();
            public M1CommandResult SetFormation(
                int unionIndex, string formationId) => Pass084();
            public M1CommandResult SetDoctrine(
                int unionIndex, string doctrineId) => Pass084();
            public M1CommandResult SaveAndReloadProof() => Pass084();

            private static M1CommandResult Pass084() =>
                M1CommandResult.Success("Saved for test.");

#pragma warning disable 67
            private void PreserveChanged084() => Changed?.Invoke();
#pragma warning restore 67
        }
    }
}
