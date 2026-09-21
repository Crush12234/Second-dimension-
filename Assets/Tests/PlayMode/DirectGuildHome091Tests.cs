using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Determinism;
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
    public sealed class DirectGuildHome091Tests
    {
        private string _savePath;
        private GameObject _root;
        private M1FlowPresenter _presenter;
        private M1RuntimeCoordinator _coordinator;

        [SetUp]
        public void SetUp091()
        {
            _savePath = Path.Combine(Path.GetTempPath(), "sd_direct_guild_091_" + Guid.NewGuid().ToString("N") + ".json");
            _coordinator = new M1RuntimeCoordinator(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), _savePath);
            _root = new GameObject("Direct Guild Home Test 091");
            _presenter = _root.AddComponent<M1FlowPresenter>();
            // Exercise the shipping builders synchronously; no deferred layout
            // pass is needed to verify routing and save boundaries.
            _presenter.enabled = false;
            _presenter.Initialize(_coordinator);
        }

        [TearDown]
        public void TearDown091()
        {
            if (_presenter != null)
            {
                var canvas = (Canvas)typeof(M1FlowPresenter).GetField(
                    "_canvas", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_presenter);
                if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas.gameObject);
            }
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
            if (File.Exists(_savePath)) File.Delete(_savePath);
            if (File.Exists(_savePath + ".bak")) File.Delete(_savePath + ".bak");
        }

        [Test]
        public void FreshTitleOpensCharterDirectlyWithoutWorldMovementOrSaveMutation091()
        {
            Assert.That(_coordinator.State.HasCampaign, Is.False);
            var scrimRect = ScreenRoot091().GetComponentsInChildren<RectTransform>(false)
                .Single(value => value.name == "Studio Title Cinematic Scrim 091");
            Assert.That(scrimRect.anchorMin.x, Is.LessThan(0f));
            Assert.That(scrimRect.anchorMin.y, Is.LessThan(0f));
            Assert.That(scrimRect.anchorMax.x, Is.GreaterThan(1f));
            Assert.That(scrimRect.anchorMax.y, Is.GreaterThan(1f));
            var titleImages = ScreenRoot091().GetComponentsInChildren<Image>(false);
            var scrim = titleImages.Single(value => value.name == "Studio Title Cinematic Scrim 091");
            Assert.That(scrim.raycastTarget, Is.False);
            Assert.That(titleImages.Single(value => value.name == "Studio Title Lockup 076").color.a,
                Is.EqualTo(0f), "The title lockup must sit in the scene rather than an opaque box.");
            Assert.That(titleImages.Single(value => value.name == "Studio Title Story Card 076").color.a,
                Is.EqualTo(0f), "The story copy uses the shared cinematic scrim for readability.");
            Invoke091("PlayNowFromTitle062");
            AssertDirectCharter091();
            Assert.That(_coordinator.State.HasCampaign, Is.False,
                "Opening the Guild charter must not create a campaign before its Sign action.");
            Assert.That(File.Exists(_savePath), Is.False);
        }

        [Test]
        public void RestartOpensDirectCharterAndPreservesCurrentSaveUntilSigning091()
        {
            CreateGuild091();
            var before = File.ReadAllBytes(_savePath);
            Invoke091("EnterFreshFirstHourFromSavedTitle077");
            AssertDirectCharter091();
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(before),
                "Restart navigation must preserve the old Guild until the player signs its replacement charter.");
        }

        [Test]
        public void ReturningFromFocusedMenusShowsFourDirectGuildActionsWithoutWalking110()
        {
            CreateGuild091();
            var before = File.ReadAllBytes(_savePath);
            Invoke091("ReturnToWalkableHall069");
            var screenRoot = ScreenRoot091();
            var hall = screenRoot.GetComponentsInChildren<RectTransform>(false)
                .Single(value => value.name == "Living Guild Hub 074");
            var facilities = hall.GetComponentsInChildren<Button>(false)
                .Where(value => value.name.StartsWith("Living Guild Hub Facility ", StringComparison.Ordinal))
                .ToArray();
            Assert.That(facilities, Has.Length.EqualTo(4));
            Assert.That(facilities.Select(value => value.name), Is.EquivalentTo(new[]
            {
                "Living Guild Hub Facility " + WalkableGuildHall069.ContractDestinationId069 + " 074",
                "Living Guild Hub Facility " + M1FlowPresenter.LivingGuildHubTowerDestinationId081 + " 074",
                "Living Guild Hub Facility " + WalkableGuildHall069.PartyDestinationId069 + " 074",
                "Living Guild Hub Facility " + WalkableGuildHall069.ArmoryDestinationId069 + " 074"
            }));
            var secondary = hall.GetComponentsInChildren<Button>(false)
                .Where(value => value.name == "Living Guild Home Codes 110" ||
                                value.name == "Living Guild Home Title Options 110").ToArray();
            Assert.That(secondary.Select(value => value.name), Is.EquivalentTo(new[]
            {
                "Living Guild Home Codes 110", "Living Guild Home Title Options 110"
            }));
            Assert.That(secondary.All(value => value.IsInteractable()), Is.True);
            Assert.That(hall.GetComponentsInChildren<Button>(false)
                .Any(value => value.name.StartsWith("Guild Details ", StringComparison.Ordinal)), Is.False);
            Assert.That(facilities.All(value => value.interactable), Is.True);
            Assert.That(hall.Find("Living Guild Direct Navigation Dock 091"), Is.Not.Null);
            AssertNoWalkingHost091();
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(before));
        }

        [TestCase("Heroes Recruit 110", "APPLICANTS", "Recruitment Desk 074")]
        [TestCase("Heroes Development 110", "DEVELOPMENT", "Page MEMBER DEVELOPMENT")]
        public void HomeHeroesButtonsOpenExistingPlannerAndMemberPagesWithoutSaveMutation110(
            string linkName, string expectedTab, string expectedPage)
        {
            CreateGuild091();
            var before = File.ReadAllBytes(_savePath);
            Invoke091("ReturnToWalkableHall069");
            ClickButton110("Living Guild Hub Facility " + WalkableGuildHall069.PartyDestinationId069 + " 074");

            Assert.That(CurrentScreen110(), Is.EqualTo(M1Screen.UnionBuilder));
            var planner = ScreenRoot091().GetComponentsInChildren<RectTransform>(false)
                .Single(value => value.name == "Union Planner 074");
            var header = planner.GetComponentsInChildren<RectTransform>(false)
                .Single(value => value.name == "Union Planner Command Bar 074");
            Assert.That(header.GetComponentsInChildren<Button>(false)
                .Where(value => value.name == "Heroes Recruit 110" || value.name == "Heroes Development 110")
                .Select(value => value.name), Is.EquivalentTo(new[]
                {
                    "Heroes Recruit 110", "Heroes Development 110"
                }));
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(before));

            // Dispatch the live command-bar control. In particular, Recruit must
            // change the screen as well as the tab while leaving UnionBuilder.
            ClickButton110(linkName);
            Assert.That(CurrentScreen110(), Is.EqualTo(M1Screen.GuildOperations));
            Assert.That(CurrentGuildTab110(), Is.EqualTo(expectedTab));
            Assert.That(ScreenRoot091().GetComponentsInChildren<RectTransform>(false)
                .Any(value => value.name == expectedPage), Is.True);
            AssertNoWalkingHost091();
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(before),
                "Opening Heroes, Recruitment or Development must not edit the saved Guild.");
        }

        [TestCase("Living Guild Home Codes 110", M1Screen.GuildOperations, "CODES")]
        [TestCase("Living Guild Home Title Options 110", M1Screen.MainMenu, null)]
        public void HomeSecondaryButtonsOpenExistingCodesAndTitleWithoutSaveMutation110(
            string buttonName, M1Screen expectedScreen, string expectedTab)
        {
            CreateGuild091();
            var before = File.ReadAllBytes(_savePath);
            Invoke091("ReturnToWalkableHall069");
            ClickButton110(buttonName);

            Assert.That(CurrentScreen110(), Is.EqualTo(expectedScreen));
            if (expectedTab != null)
                Assert.That(CurrentGuildTab110(), Is.EqualTo(expectedTab));
            else
                Assert.That(ScreenRoot091().GetComponentsInChildren<Image>(false)
                    .Any(value => value.name == "Studio Title Lockup 076"), Is.True);
            AssertNoWalkingHost091();
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(before),
                "Secondary navigation cannot redeem a code or change the Guild save.");
        }

        [TestCase("CITY", "Guild City Workshop Return To Hall 078")]
        [TestCase("DEVELOPMENT", "Back")]
        [TestCase("ABYSS", "Back")]
        public void FocusedGuildPageBackButtonsReturnHomeWithoutOptionalDetailsOrSaveMutation110(
            string tab, string backButtonName)
        {
            CreateGuild091();
            var before = File.ReadAllBytes(_savePath);
            SetPresenterField110("_screen", M1Screen.GuildOperations);
            SetPresenterField110("_guildCityTab017D", tab);
            // Avoid the earlier compatibility return, so the shared Back button
            // really exercises the former DETAILS detour for Development/Tower.
            SetPresenterField110("_returnToWalkableHall069", false);
            Invoke091("BuildCurrentScreen");
            Assert.That(CurrentGuildTab110(), Is.EqualTo(tab));
            ClickButton110(backButtonName);

            Assert.That(CurrentScreen110(), Is.EqualTo(M1Screen.GuildOperations));
            Assert.That(CurrentGuildTab110(), Is.EqualTo("HALL"));
            var visible = ScreenRoot091().GetComponentsInChildren<RectTransform>(false);
            Assert.That(visible.Any(value => value.name == "Living Guild Hub 074"), Is.True);
            Assert.That(visible.Any(value => value.name.StartsWith("Guild Details ", StringComparison.Ordinal)),
                Is.False);
            AssertNoWalkingHost091();
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(before),
                "Leaving an optional Guild page must preserve facilities, staff and all saved progress.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AffectedVictoryHomeAndMissionButtonsOpenExistingClaimWithoutSaveMutation110(bool missions)
        {
            var source = LoadAffectedVictory110();
            var before = File.ReadAllBytes(_savePath);
            var sourceBefore = File.ReadAllBytes(source);
            var battleId = M2BattleViewAccess098.Read(_coordinator).BattleId;
            var operationId = _coordinator.CampaignProgression022.ActiveAbyssOperationId;
            Invoke091("ReturnToWalkableHall069");
            ClickAffectedHallAction110(missions);

            AssertAffectedVictorySurface110();
            var after = M2BattleViewAccess098.Read(_coordinator);
            Assert.That(after.BattleId, Is.EqualTo(battleId));
            Assert.That(after.Reward.Claimed, Is.False);
            Assert.That(_coordinator.CampaignProgression022.ActiveAbyssOperationId, Is.EqualTo(operationId));
            Assert.That(_coordinator.CampaignPlayable020.ActiveOperationId, Is.Empty);
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(before), "Navigation cannot claim or replace the saved outcome.");
            Assert.That(File.ReadAllBytes(source), Is.EqualTo(sourceBefore));
        }

        [UnityTest]
        public IEnumerator AffectedVictoryContinueButtonClaimsClosesResultsAndReloadsOwningTower110()
        {
            TestContext.Progress.WriteLine("Affected Continue110: load preserved terminal victory into isolated test save.");
            var source = LoadAffectedVictory110();
            var before = File.ReadAllBytes(_savePath);
            var sourceBefore = File.ReadAllBytes(source);
            var battle = M2BattleViewAccess098.Read(_coordinator);
            var operationId = _coordinator.CampaignProgression022.ActiveAbyssOperationId;
            var lifetimeXpBefore = Campaign110(_coordinator).Guild.Development.LifetimeTreasuryXpEarned;
            Invoke091("ReturnToWalkableHall069");
            ClickAffectedHallAction110(false);

            var controller = AssertAffectedVictorySurface110();
            var resultLayer = ScreenRoot091().GetComponentsInChildren<RectTransform>(false)
                .Single(value => value.name == "Staged Battle Result Layer 072");
            Assert.That(M2BattleViewAccess098.Read(_coordinator).BattleId, Is.EqualTo(battle.BattleId));
            Assert.That(M2BattleViewAccess098.Read(_coordinator).Reward.Claimed, Is.False);
            Assert.That(_coordinator.CampaignProgression022.ActiveAbyssOperationId, Is.EqualTo(operationId));
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(before),
                "Opening Victory must preserve the pending reward until the actual Continue click.");
            Assert.That(File.ReadAllBytes(source), Is.EqualTo(sourceBefore));
            TestContext.Progress.WriteLine("Affected Continue110: visible Victory preserves save; wait for Continue reveal.");

            var continueButton = resultLayer.GetComponentsInChildren<Button>(false)
                .Single(value => value.name == "Continue From Battle Results 072");
            var deadline = Time.realtimeSinceStartup + 10f;
            while (!continueButton.IsInteractable())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline),
                    "The actual staged results must reveal an enabled Continue button.");
                yield return null;
            }
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(EventSystem.current, Is.Not.Null);
            var canvas = continueButton.GetComponentInParent<Canvas>();
            var buttonRect = continueButton.GetComponent<RectTransform>();
            var pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                    buttonRect.TransformPoint(buttonRect.rect.center))
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, "Continue must be reachable by the UI raycaster.");
            var clickTarget = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Assert.That(clickTarget, Is.EqualTo(continueButton.gameObject),
                "A visible overlay must not intercept the actual Continue button.");
            TestContext.Progress.WriteLine("Affected Continue110: dispatch actual pointer click through the result controller.");
            ExecuteEvents.Execute(clickTarget, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;

            Assert.That(_presenter.IsFirstHourBattleExperienceActive072, Is.False,
                "A successful claim must close the battle result surface.");
            Assert.That(controller.IsActive, Is.False);
            Assert.That(resultLayer == null || !resultLayer.gameObject.activeInHierarchy, Is.True);
            AssertOwningTowerAction110(operationId);
            var claimed = Campaign110(_coordinator);
            Assert.That(claimed.Battle.BattleId, Is.EqualTo(battle.BattleId));
            Assert.That(claimed.Battle.Reward.Claimed, Is.True);
            Assert.That(claimed.Guild.Development.ClaimedBattleRewardIds.Count(
                id => id == battle.Reward.RewardId), Is.EqualTo(1));
            Assert.That(claimed.Guild.Development.LifetimeTreasuryXpEarned,
                Is.EqualTo(lifetimeXpBefore + battle.Reward.GuildTreasuryXpAward));
            var saved = new AtomicSaveStore().ReadWithRecovery(_savePath);
            Assert.That(saved.IsSuccess, Is.True, string.Join("; ", saved.Errors));
            Assert.That(saved.Value.CanonicalStateHash, Is.EqualTo(CanonicalJson.Sha256Hex(claimed)),
                "The closed results must correspond to the complete durable reward transaction.");
            var earnedBytes = File.ReadAllBytes(_savePath);
            var reloaded = new M1RuntimeCoordinator(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), _savePath);
            Assert.That(M2BattleViewAccess098.Read(reloaded).Reward.Claimed, Is.True);
            Assert.That(M2BattleViewAccess098.Read(reloaded).BattleId, Is.EqualTo(battle.BattleId));
            Assert.That(reloaded.CampaignProgression022.ActiveAbyssOperationId, Is.EqualTo(operationId));
            Assert.That(Campaign110(reloaded).Guild.Development.ClaimedBattleRewardIds.Count(
                id => id == battle.Reward.RewardId), Is.EqualTo(1));
            Assert.That(CanonicalJson.Sha256Hex(Campaign110(reloaded)), Is.EqualTo(saved.Value.CanonicalStateHash));
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(earnedBytes));
            Assert.That(File.ReadAllBytes(source), Is.EqualTo(sourceBefore));
            TestContext.Progress.WriteLine("Affected Continue110: claimed once, closed result, owning Tower shown, durable reload verified.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AffectedClaimedTowerHomeAndMissionButtonsResumeOwningFloor110(bool missions)
        {
            LoadAffectedVictory110();
            var claimed = _coordinator.ClaimBattleRewards();
            Assert.That(claimed.Succeeded, Is.True, claimed.Message);
            Assert.That(_coordinator.CampaignProgression022.ActiveAbyssOperationId, Is.Not.Empty);
            var before = File.ReadAllBytes(_savePath);
            var operationId = _coordinator.CampaignProgression022.ActiveAbyssOperationId;
            Invoke091("ReturnToWalkableHall069");
            ClickAffectedHallAction110(missions);

            AssertOwningTowerAction110(operationId);
            Assert.That(_coordinator.CampaignPlayable020.ActiveOperationId, Is.Empty);
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(before));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AffectedInProgressBattleHomeAndMissionButtonsResumeSameBattle110(bool missions)
        {
            LoadAffectedVictory110();
            var advanced = _coordinator.AdvanceTowerAutoAfterVictory108();
            Assert.That(advanced.Succeeded, Is.True, advanced.Message);
            var active = M2BattleViewAccess098.Read(_coordinator);
            Assert.That(active.IsResolved, Is.False);
            var before = File.ReadAllBytes(_savePath);
            Invoke091("ReturnToWalkableHall069");
            ClickAffectedHallAction110(missions);

            Assert.That(CurrentScreen110(), Is.EqualTo(M1Screen.Battle));
            Assert.That(M2BattleViewAccess098.Read(_coordinator).BattleId, Is.EqualTo(active.BattleId));
            Assert.That(_coordinator.CampaignPlayable020.ActiveOperationId, Is.Empty);
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(before));
        }

        private string LoadAffectedVictory110()
        {
            var source = Environment.GetEnvironmentVariable("SD_TOWER108_SOURCE");
            if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
                Assert.Ignore("Set SD_TOWER108_SOURCE to the preserved resolved affected-save copy.");
            File.Copy(source, _savePath, true);
            _coordinator = new M1RuntimeCoordinator(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), _savePath);
            _presenter.Initialize(_coordinator);
            var battle = M2BattleViewAccess098.Read(_coordinator);
            Assert.That(battle?.IsResolved, Is.True, "This fixture must contain the actual saved terminal victory.");
            Assert.That(battle.Outcome, Is.EqualTo("Victory"));
            Assert.That(battle.Reward.Claimed, Is.False);
            return source;
        }

        private void ClickAffectedHallAction110(bool missions)
        {
            var name = missions
                ? "Living Guild Hub Facility " + WalkableGuildHall069.ContractDestinationId069 + " 074"
                : "Living Guild Hub Primary CTA 074";
            var button = ScreenRoot091().GetComponentsInChildren<Button>(false)
                .Single(value => value.name == name);
            Assert.That(button.interactable, Is.True);
            button.onClick.Invoke();
        }

        private void ClickButton110(string name)
        {
            var button = ScreenRoot091().GetComponentsInChildren<Button>(false)
                .Single(value => value.name == name);
            Assert.That(button.gameObject.activeInHierarchy, Is.True);
            Assert.That(button.IsInteractable(), Is.True, name + " must be enabled.");
            button.onClick.Invoke();
        }

        private string CurrentGuildTab110() => (string)typeof(M1FlowPresenter)
            .GetField("_guildCityTab017D", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_presenter);

        private void SetPresenterField110(string name, object value) => typeof(M1FlowPresenter)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_presenter, value);

        private M1Screen CurrentScreen110() => (M1Screen)typeof(M1FlowPresenter)
            .GetField("_screen", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_presenter);

        private M2BattleExperienceController072 AssertAffectedVictorySurface110()
        {
            // Shipping results use the Battle screen and its active staged
            // controller; the legacy BattleResults enum is only a routing input.
            Assert.That(CurrentScreen110(), Is.EqualTo(M1Screen.Battle));
            Assert.That(_presenter.IsFirstHourBattleExperienceActive072, Is.True);
            var controller = _root.GetComponent<M2BattleExperienceController072>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsActive, Is.True);
            Assert.That(controller.OwnedResultsView078.OutcomeKickerText079, Does.Contain("VICTORY"));
            Assert.That(ScreenRoot091().GetComponentsInChildren<RectTransform>(false)
                .Any(value => value.name == "Staged Battle Result Layer 072"), Is.True);
            return controller;
        }

        private void AssertOwningTowerAction110(string operationId)
        {
            Assert.That(CurrentScreen110(), Is.EqualTo(M1Screen.GuildOperations));
            Assert.That(typeof(M1FlowPresenter).GetField("_guildCityTab017D",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_presenter), Is.EqualTo("ABYSS"));
            Assert.That(_coordinator.CampaignProgression022.ActiveAbyssOperationId, Is.EqualTo(operationId));
            var bankVictory = ScreenRoot091().GetComponentsInChildren<Button>(false)
                .Single(value => value.name == "Bank Tower battle victory 088");
            Assert.That(bankVictory.interactable, Is.True,
                "Claimed battle rewards leave the owning floor's explicit Bank Victory action available.");
        }

        private static CampaignState Campaign110(M1RuntimeCoordinator coordinator) =>
            (CampaignState)typeof(M1RuntimeCoordinator).GetField("_campaign",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(coordinator);

        private void CreateGuild091()
        {
            var result = _coordinator.CreateGuild(new M1NewGuildIntent
            {
                GuildmasterName = "Direct Hall Test",
                ModeId = "Standard",
                TutorialDepthId = "Fast Charter",
                TextScale = 1f
            });
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(File.Exists(_savePath), Is.True);
        }

        private void AssertDirectCharter091()
        {
            Assert.That(ScreenRoot091().GetComponentsInChildren<RectTransform>(false)
                .Any(value => value.name == "Guild Charter Prologue 074"), Is.True);
            Assert.That(ScreenRoot091().GetComponentsInChildren<Button>(false)
                .Any(value => value.GetComponentsInChildren<Text>(false)
                    .Any(label => label.text == "SIGN THE CHARTER")), Is.True);
            AssertNoWalkingHost091();
        }

        private void AssertNoWalkingHost091()
        {
            Assert.That(_root.GetComponent<WalkableSkyhomeArrival071>(), Is.Null,
                "Default entry must not require walking across the market and pressing E.");
            Assert.That(_root.GetComponent<WalkableGuildHall069>(), Is.Null,
                "Guild services must be immediately available through their direct menu actions.");
        }

        private RectTransform ScreenRoot091() => (RectTransform)typeof(M1FlowPresenter)
            .GetField("_screenRoot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_presenter);

        private void Invoke091(string name) => typeof(M1FlowPresenter)
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_presenter, null);
    }
}
