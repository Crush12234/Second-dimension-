using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;

namespace SecondDimension.Tests.EditMode
{
    public sealed class GuildHallGuidedProgression080Tests
    {
        [Test]
        public void FirstContractGuideRequiresPersonThenSixSlotUnionConfirmation080()
        {
            var state = new GuildCityPresentationState017D
            {
                IsAvailable = true,
                OperationOrdinal = 0,
                TotalRecruitCount = 7
            };
            Assert.That(Stage080(state, firstComplete: false),
                Is.EqualTo(GuildHallGuidedStage080.MeetApplicant));

            state.HasRecruitmentBoard = true;
            state.TotalRecruitCount = 6;
            Assert.That(Stage080(state, firstComplete: false),
                Is.EqualTo(GuildHallGuidedStage080.RecruitAdventurer));

            state.TotalRecruitCount = 7;
            Assert.That(Stage080(state, firstComplete: false),
                Is.EqualTo(GuildHallGuidedStage080.ConfirmSixSlotUnion));
            Assert.That(M1FlowPresenter.GuidedHallObjectiveForVerification080(
                    GuildHallGuidedStage080.ConfirmSixSlotUnion),
                Does.Contain("six visible slots"));

            state.FirstContractUnionBriefingConfirmed080 = true;
            Assert.That(Stage080(state, firstComplete: false),
                Is.EqualTo(GuildHallGuidedStage080.Ready));
        }

        [Test]
        public void ReadyFoundingCompanyGoesStraightToFirstMission084()
        {
            var state = new GuildCityPresentationState017D
            {
                IsAvailable = true,
                OperationOrdinal = 0,
                TotalRecruitCount = 10,
                NormalUnionCount = 3,
                TreasuryXp = 0,
                HasRecruitmentBoard = false
            };

            Assert.That(Stage080(state, firstComplete: false),
                Is.EqualTo(GuildHallGuidedStage080.Ready),
                "A complete founding company must not be forced into paid recruitment before its first XP-earning mission.");
        }

        [Test]
        public void UnionConfirmationIsValidatedIdempotentAndSaveFacing080()
        {
            var campaign = CreateCampaign080();
            var blocked = GuildHallGuidedProgression080
                .ConfirmFirstContractUnionBriefing080(campaign);
            Assert.That(blocked.IsSuccess, Is.False);
            Assert.That(blocked.Errors,
                Contains.Item("GC080_MEET_AN_APPLICANT_BEFORE_UNION_BRIEFING"));

            campaign = WithRecruitmentBoard080(campaign);
            campaign = Require080(GuildHallGuidedProgression080
                .ConfirmFirstContractUnionBriefing080(campaign));
            Assert.That(GuildHallGuidedProgression080
                .HasFirstContractUnionBriefing080(campaign.Guild.GuildCity), Is.True);

            var replay = Require080(GuildHallGuidedProgression080
                .ConfirmFirstContractUnionBriefing080(campaign));
            Assert.That(replay, Is.SameAs(campaign));

            var reloaded = JsonConvert.DeserializeObject<CampaignState>(
                JsonConvert.SerializeObject(campaign));
            Assert.That(GuildHallGuidedProgression080
                .HasFirstContractUnionBriefing080(reloaded.Guild.GuildCity), Is.True);
        }

        [Test]
        public void HomecomingManagementNeverBlocksTheNextMission085()
        {
            var state = new GuildCityPresentationState017D
            {
                IsAvailable = true,
                OperationOrdinal = 1,
                ClaimedBattleRewardCount = 1,
                PlacedBuildingCount = 1,
                StaffedBuildingCount = 1,
                FirstFacilityPayoffAcknowledged080 = false,
                RecoveredLootEquipped080 = false
            };
            Assert.That(Stage080(state, firstComplete: true),
                Is.EqualTo(GuildHallGuidedStage080.Ready),
                "A staffed home base may still be inspected, but it must never gate Chapter 2.");
            Assert.That(M1FlowPresenter.GuidedHallActionForVerification080(
                    Stage080(state, firstComplete: true)),
                Is.Empty);

            state.FirstFacilityPayoffAcknowledged080 = true;
            Assert.That(Stage080(state, firstComplete: true),
                Is.EqualTo(GuildHallGuidedStage080.Ready),
                "Recovered gear remains an optional Inventory action.");

            state.RecoveredLootEquipped080 = true;
            Assert.That(Stage080(state, firstComplete: true),
                Is.EqualTo(GuildHallGuidedStage080.Ready));
        }

        [Test]
        public void FacilityPayoffRequiresAStaffedBuildingAndRecoveredLootRequiresEquip080()
        {
            var campaign = CreateCampaign080();
            var city = campaign.Guild.GuildCity.With(operationOrdinal: 1);
            campaign = campaign.With(
                campaign.Guild.WithGuildCity(city),
                campaign.OpeningFlow);

            var blocked = GuildHallGuidedProgression080
                .AcknowledgeFirstFacilityPayoff080(campaign);
            Assert.That(blocked.IsSuccess, Is.False);
            Assert.That(blocked.Errors,
                Contains.Item("GC080_WORKING_FIRST_FACILITY_REQUIRED"));

            var plots = city.CityPlots.ToArray();
            plots[0] = plots[0].With(
                buildingId: "GC017D_BUILD_CONTRACT_HOUSE",
                buildingLevel: 1,
                staffRecruitIds: new[] { "R7" });
            city = city.With(cityPlots: Array.AsReadOnly(plots));
            campaign = campaign.With(
                campaign.Guild.WithGuildCity(city),
                campaign.OpeningFlow);
            campaign = Require080(GuildHallGuidedProgression080
                .AcknowledgeFirstFacilityPayoff080(campaign));
            Assert.That(GuildHallGuidedProgression080
                .HasFirstFacilityPayoff080(campaign.Guild.GuildCity), Is.True);
            Assert.That(GuildHallGuidedProgression080
                .HasEquippedRecoveredLoot080(campaign.Guild), Is.False);

            var recruits = campaign.Guild.Recruits.ToArray();
            var loot = new EquipmentItemState(
                "LOOT_ITEM_070_FIRST_HOMECOMING",
                "EQ_NIGHTGLASS_TWIN_BLADES",
                "Nightglass Twin Blades",
                new[] { EquipmentSlotIds.MainHand },
                new[] { "DAGGER", "WEAPON" },
                "QUALITY_RARE",
                10000,
                false);
            recruits[0] = recruits[0].WithEquipment(new EquipmentLoadoutState(new[]
            {
                new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, loot)
            }));
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                Array.AsReadOnly(recruits),
                campaign.Guild.Unions,
                campaign.Guild.Inventory);
            campaign = campaign.With(guild, campaign.OpeningFlow);
            Assert.That(GuildHallGuidedProgression080
                .HasEquippedRecoveredLoot080(campaign.Guild), Is.True);
        }

        private static GuildHallGuidedStage080 Stage080(
            GuildCityPresentationState017D state,
            bool firstComplete) =>
            M1FlowPresenter.GuidedHallStageForVerification080(
                state,
                firstComplete,
                secondStoryComplete: false);

        private static CampaignState WithRecruitmentBoard080(CampaignState campaign)
        {
            var applicant = new ApplicantSnapshotState(
                1,
                "APPLICANT_080",
                "Lysa Vale",
                ApplicantKind.Procedural,
                "SEED_080",
                string.Empty,
                "HUMAN",
                "WORLD_GATE_01",
                "CLASS_TEND_RANGER",
                "STEADY",
                100,
                100,
                20,
                20,
                0,
                5000,
                "{}",
                "{}",
                Array.Empty<EquipmentItemState>());
            var board = new ApplicantBoardState(
                "BOARD_RECURRING_080",
                "GENERATION_080",
                0,
                true,
                new[] { applicant },
                string.Empty);
            var city = campaign.Guild.GuildCity.With(
                recruitmentBoard: board,
                replaceRecruitmentBoard: true,
                lastCheckpointId: "recruitment_board_committed");
            return campaign.With(
                campaign.Guild.WithGuildCity(city),
                campaign.OpeningFlow);
        }

        private static CampaignState CreateCampaign080()
        {
            var recruits = Enumerable.Range(1, 7)
                .Select(index => new RecruitState(
                    "R" + index,
                    100,
                    100,
                    20,
                    20))
                .ToArray();
            var unions = new[]
            {
                new UnionState(
                    "U1",
                    "Lantern Guard",
                    UnionKind.Normal,
                    "R1",
                    new[] { "R1", "R2", "R3" },
                    "FORMATION_SHIELD_WALL",
                    "DOCTRINE_BALANCED",
                    30,
                    9000),
                new UnionState(
                    "U2",
                    "Wayglass Watch",
                    UnionKind.Normal,
                    "R4",
                    new[] { "R4", "R5", "R6" },
                    "FORMATION_WEDGE",
                    "DOCTRINE_BALANCED",
                    30,
                    9000)
            };
            var guild = new GuildState(
                "GUILD_GUIDED_080",
                500,
                recruits,
                unions);
            var flow = new OpeningFlowState(
                OpeningStage.Complete,
                ApplicantBoardState.FrozenTutorialSeedId,
                true,
                null,
                false,
                0,
                0,
                true,
                true,
                true,
                true,
                "autosave_unions");
            return new CampaignState(
                "00000000-0000-0000-0000-000000000080",
                80,
                "1.0",
                ModeRuleSnapshot.StandardDefaults(),
                guild,
                new NewGuildProfileState(
                    "Tester",
                    GameMode.Standard,
                    TutorialDepth.FullTutorial,
                    AccessibilitySettingsState.Defaults(),
                    false),
                flow);
        }

        private static CampaignState Require080(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
