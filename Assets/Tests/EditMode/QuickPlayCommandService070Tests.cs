using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class QuickPlayCommandService070Tests
    {
        private GuildCityContent017D _content;
        private GuildCityExpeditionService017D _expeditions;
        private QuickPlayCommandService070 _quickPlay;

        [SetUp]
        public void SetUp()
        {
            _content = GuildCityContent017D.LoadFromDirectory(Path.Combine(
                Application.streamingAssetsPath, "Authority", "CONTENT", "GUILD_CITY_017D"));
            _expeditions = new GuildCityExpeditionService017D();
            _quickPlay = new QuickPlayCommandService070();
        }

        [Test]
        public void FreshQuickPlayAcceptsAndStartsMarkedStoryContractUsingSavedUnions()
        {
            var campaign = CreateCampaign(7);
            var rosterBefore = CanonicalJson.Serialize(campaign.Guild.Recruits);
            var unionsBefore = CanonicalJson.Serialize(campaign.Guild.Unions);
            var inventoryBefore = CanonicalJson.Serialize(campaign.Guild.Inventory);

            var outcome = Require(_quickPlay.ContinueStory(campaign, _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));

            Assert.That(outcome.Destination, Is.EqualTo(QuickPlayDestination070.Expedition));
            Assert.That(outcome.AcceptedContract, Is.True);
            Assert.That(outcome.StartedExpedition, Is.True);
            Assert.That(outcome.ReusedSavedUnionPlan, Is.True);
            Assert.That(outcome.Campaign.Guild.GuildCity.ActiveContract.ContractId,
                Is.EqualTo(GuildCityExpeditionService017D.FirstStoryContractId066));
            Assert.That(outcome.Campaign.Guild.GuildCity.Expedition.Status,
                Is.EqualTo(ExpeditionStatus017D.Active));
            Assert.That(CanonicalJson.Serialize(outcome.Campaign.Guild.Recruits), Is.EqualTo(rosterBefore));
            Assert.That(CanonicalJson.Serialize(outcome.Campaign.Guild.Unions), Is.EqualTo(unionsBefore));
            Assert.That(CanonicalJson.Serialize(outcome.Campaign.Guild.Inventory), Is.EqualTo(inventoryBefore));
        }

        [Test]
        public void AcceptedStoryContractStartsWithoutReplacingSavedUnionPlan()
        {
            var campaign = CreateCampaign(7);
            campaign = RequireCampaign(_expeditions.AcceptContract(campaign, _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));
            var contractCommitId = campaign.Guild.GuildCity.ActiveContract.CommitId;
            var unionsBefore = CanonicalJson.Serialize(campaign.Guild.Unions);

            var outcome = Require(_quickPlay.ContinueStory(campaign, _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));

            Assert.That(outcome.AcceptedContract, Is.False);
            Assert.That(outcome.StartedExpedition, Is.True);
            Assert.That(outcome.Campaign.Guild.GuildCity.ActiveContract.CommitId,
                Is.EqualTo(contractCommitId));
            Assert.That(CanonicalJson.Serialize(outcome.Campaign.Guild.Unions), Is.EqualTo(unionsBefore));
        }

        [Test]
        public void ActiveExpeditionResumesWithoutReplacingOrAdvancingState()
        {
            var campaign = CreateCampaign(7);
            campaign = RequireCampaign(_expeditions.AcceptContract(campaign, _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));
            campaign = RequireCampaign(_expeditions.StartExpedition(campaign, _content));
            var before = CanonicalJson.Serialize(campaign);
            var expeditionId = campaign.Guild.GuildCity.Expedition.ExpeditionId;

            var outcome = Require(_quickPlay.ContinueStory(campaign, _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));

            Assert.That(outcome.Destination, Is.EqualTo(QuickPlayDestination070.Expedition));
            Assert.That(outcome.Title, Is.EqualTo("Resume Expedition"));
            Assert.That(outcome.AcceptedContract, Is.False);
            Assert.That(outcome.StartedExpedition, Is.False);
            Assert.That(outcome.Campaign.Guild.GuildCity.Expedition.ExpeditionId, Is.EqualTo(expeditionId));
            Assert.That(CanonicalJson.Serialize(outcome.Campaign), Is.EqualTo(before));
        }

        [Test]
        public void TerminalExpeditionRoutesBackToThePhysicalReturnInsteadOfADeadEndMessage()
        {
            var campaign = CreateCampaign(7);
            campaign = RequireCampaign(_expeditions.AcceptContract(campaign, _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));
            campaign = RequireCampaign(_expeditions.StartExpedition(campaign, _content));
            var city = campaign.Guild.GuildCity;
            var terminal = city.Expedition.With(status: ExpeditionStatus017D.Completed);
            city = city.With(
                expedition: terminal,
                replaceExpedition: true,
                lastCheckpointId: "test_physical_return_070");
            campaign = campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);

            var outcome = Require(_quickPlay.ContinueStory(campaign, _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));

            Assert.That(outcome.Destination, Is.EqualTo(QuickPlayDestination070.Expedition));
            Assert.That(outcome.Title, Is.EqualTo("Complete Your Return"));
            Assert.That(outcome.Guidance, Does.Contain("home marker"));
            Assert.That(outcome.StartedExpedition, Is.False);
        }

        [Test]
        public void InvalidSavedUnionPlanReturnsOnlyFocusedPartyPreparationRequirement()
        {
            var campaign = CreateCampaign(7);
            var invalidGuild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,
                new[] { campaign.Guild.Unions[0] },
                campaign.Guild.Inventory);
            campaign = campaign.With(invalidGuild, campaign.OpeningFlow);
            var before = CanonicalJson.Serialize(campaign);

            var outcome = Require(_quickPlay.ContinueStory(campaign, _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));

            Assert.That(outcome.Destination, Is.EqualTo(QuickPlayDestination070.PartyPreparation));
            Assert.That(outcome.RequirementId,
                Is.EqualTo(QuickPlayCommandService070.PartyPreparationRequirementId));
            Assert.That(outcome.Title, Is.EqualTo("Prepare Your Party"));
            Assert.That(outcome.Guidance, Does.Contain("at least two saved Unions"));
            Assert.That(outcome.Guidance, Does.Contain("one to six owned adventurers"));
            Assert.That(outcome.Guidance, Does.Contain("ten ready Unions"));
            Assert.That(outcome.Guidance, Does.Not.Contain("one to three"));
            Assert.That(outcome.AcceptedContract, Is.False);
            Assert.That(outcome.StartedExpedition, Is.False);
            Assert.That(outcome.Campaign.Guild.GuildCity.ActiveContract, Is.Null);
            Assert.That(CanonicalJson.Serialize(outcome.Campaign), Is.EqualTo(before));
        }

        [Test]
        public void SavedBattleResumesBeforeNewContractUnionValidation()
        {
            var campaign = CreateCampaign(7);
            var invalidGuild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,
                new[] { campaign.Guild.Unions[0] },
                campaign.Guild.Inventory);
            campaign = campaign.With(invalidGuild, campaign.OpeningFlow).WithBattle(
                new BattleState(
                    "BATTLE_QUICK_PLAY_RESUME_070",
                    "test",
                    2,
                    BattlePhase.ForecastSelection,
                    BattleOutcome.InProgress,
                    "Resume the saved fight",
                    Array.Empty<BattleUnionState>(),
                    Array.Empty<BattleUnionState>(),
                    Array.Empty<BattleForecastState>(),
                    Array.Empty<BattleForecastSelectionState>(),
                    Array.Empty<BattleEventState>(),
                    Array.Empty<BattleRoundRecordState>(),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    false));

            var outcome = Require(_quickPlay.ContinueStory(campaign, _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));

            Assert.That(outcome.Destination, Is.EqualTo(QuickPlayDestination070.Battle));
            Assert.That(outcome.Title, Is.EqualTo("Resume Battle"));
            Assert.That(outcome.RequirementId, Is.Empty);
            Assert.That(outcome.Campaign.Battle.BattleId,
                Is.EqualTo("BATTLE_QUICK_PLAY_RESUME_070"));
        }

        [Test]
        public void FirstStoryGatePointsToRecruitmentWithoutAutoSigningOrEquipping()
        {
            var campaign = CreateCampaign(6);
            var rosterBefore = CanonicalJson.Serialize(campaign.Guild.Recruits);
            var unionsBefore = CanonicalJson.Serialize(campaign.Guild.Unions);
            var inventoryBefore = CanonicalJson.Serialize(campaign.Guild.Inventory);

            var outcome = Require(_quickPlay.ContinueStory(campaign, _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));

            Assert.That(outcome.Destination, Is.EqualTo(QuickPlayDestination070.Recruitment));
            Assert.That(outcome.RequirementId,
                Is.EqualTo(QuickPlayCommandService070.RecruitmentRequirementId));
            Assert.That(outcome.Title, Is.EqualTo("Recruit One Adventurer"));
            Assert.That(outcome.Campaign.Guild.GuildCity.ActiveContract, Is.Null);
            Assert.That(CanonicalJson.Serialize(outcome.Campaign.Guild.Recruits), Is.EqualTo(rosterBefore));
            Assert.That(CanonicalJson.Serialize(outcome.Campaign.Guild.Unions), Is.EqualTo(unionsBefore));
            Assert.That(CanonicalJson.Serialize(outcome.Campaign.Guild.Inventory), Is.EqualTo(inventoryBefore));
        }

        [TestCase("EQ_PROC_TRAVEL_SPEAR", "Travel Spear")]
        [TestCase("TREE_BLADE_ARTS", "Blade Arts")]
        [TestCase("PASS_02_ART_DEFINITIONS", "Definitions")]
        [TestCase("BATTLE_ITEM_FIRST_GATE_SWORD", "First Gate Sword")]
        [TestCase("TREE_CA002_WPN_GREATSWORD", "Greatsword")]
        [TestCase("TREE_CA002_MYS_STORMCALLING", "Stormcalling")]
        [TestCase("CLASS_TEND_GUARDIAN", "Guardian")]
        [TestCase("WEAPON_FAMILY_SPEAR", "Spear")]
        public void DefaultLabelsHideRawAuthorityPrefixes(string rawId, string expected)
        {
            var labels = new PlayerFacingLabelService070();
            var visible = labels.DefaultLabel(rawId, rawId);

            Assert.That(visible, Is.EqualTo(expected));
            Assert.That(visible, Does.Not.Contain("EQ_"));
            Assert.That(visible, Does.Not.Contain("TREE_"));
            Assert.That(visible, Does.Not.Contain("PASS_"));
        }

        [Test]
        public void AuthoredPlayerLabelWinsAndEmptyInputGetsSafeFallback()
        {
            var labels = new PlayerFacingLabelService070();
            Assert.That(labels.DefaultLabel("Furnace Splitter", "EQ_FURNACE_SPLITTER_AXE"),
                Is.EqualTo("Furnace Splitter"));
            Assert.That(labels.DefaultLabel(string.Empty, string.Empty, "Unidentified Gear"),
                Is.EqualTo("Unidentified Gear"));
        }

        [TestCase("362fa9db3a4ab70e Main", "Adventurer")]
        [TestCase("SIGI_CE2768FD5A985F8B", "Adventurer")]
        [TestCase("PROC_36344E2400DC98B6", "Adventurer")]
        public void OpaqueRuntimeIdsNeverReachPlayerFacingLabels(string rawId, string expected)
        {
            var labels = new PlayerFacingLabelService070();
            var visible = labels.DefaultLabel(rawId, rawId, expected);

            Assert.That(visible, Is.EqualTo(expected));
            Assert.That(visible, Does.Not.Match("[0-9a-fA-F]{12,}"));
        }

        [Test]
        public void QuickPlayFailureLabelsNeverExposeStableErrorCodes()
        {
            var labels = new PlayerFacingLabelService070();

            Assert.That(labels.QuickPlayFailureMessage(new[] { "QUICK_PLAY_070_GUILD_REQUIRED" }),
                Is.EqualTo("Load or create a Guild before using Quick Play."));
            Assert.That(labels.QuickPlayFailureMessage(new[] { "GC017D_CONTRACT_NOT_FOUND" }),
                Does.Not.Contain("GC017D"));
            Assert.That(labels.QuickPlayFailureMessage(new[] { "SAVE_PIPELINE_FAILED" }),
                Does.Not.Contain("SAVE_PIPELINE_FAILED"));
            Assert.That(labels.QuickPlayFailureMessage(new[] { "SAVE_PIPELINE_FAILED: disk busy" }),
                Does.Not.Contain("SAVE_PIPELINE_FAILED"));
        }

        private static CampaignState CreateCampaign(int recruitCount)
        {
            if (recruitCount < 6) throw new ArgumentOutOfRangeException(nameof(recruitCount));
            var recruits = new List<RecruitState>();
            for (var index = 0; index < recruitCount; index++)
                recruits.Add(new RecruitState("R" + (index + 1), 100, 100, 20, 20));

            var unions = new[]
            {
                new UnionState("U1", "Gate Union", UnionKind.Normal, "R1",
                    new[] { "R1", "R2", "R3" }, "FORMATION_SKIRMISH_LINE",
                    "DOCTRINE_BALANCED", 30, 7000),
                new UnionState("U2", "Rescue Union", UnionKind.Normal, "R4",
                    new[] { "R4", "R5", "R6" }, "FORMATION_RESCUE_COLUMN",
                    "DOCTRINE_RESCUE_FIRST", 30, 7000)
            };
            var spareWeapon = new EquipmentItemState(
                "ITEM_SPARE_TRAVEL_SPEAR_070",
                "EQ_PROC_TRAVEL_SPEAR",
                "Travel Spear",
                new[] { EquipmentSlotIds.MainHand },
                new[] { "WEAPON", "SPEAR" },
                "QUALITY_STANDARD",
                10000,
                false);
            var guild = new GuildState("GUILD_QUICK_PLAY_070", 0,
                recruits.AsReadOnly(), unions, new[] { spareWeapon });
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
            return new CampaignState(
                "00000000-0000-0000-0000-000000000070",
                70070,
                "1.0",
                ModeRuleSnapshot.StandardDefaults(),
                guild,
                new NewGuildProfileState(
                    "Quick Play Tester",
                    GameMode.Standard,
                    TutorialDepth.FullTutorial,
                    AccessibilitySettingsState.Defaults(),
                    false),
                flow);
        }

        private static QuickPlayOutcome070 Require(Result<QuickPlayOutcome070> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }

        private static CampaignState RequireCampaign(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
