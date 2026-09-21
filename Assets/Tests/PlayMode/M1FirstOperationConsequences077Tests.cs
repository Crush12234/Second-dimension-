using System;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;

namespace SecondDimension.Presentation.Tests
{
    public sealed class M1FirstOperationConsequences077Tests
    {
        private const string FirstContractId = "CONTRACT_BELL_BENEATH_GATE";
        private const string SecondContractId = "CONTRACT_LINES_NOT_RETURNED";

        [Test]
        public void FirstOperationConsequencesAdvanceFromOrdersToMemoryToFacilityAndStaff077()
        {
            var state = FirstOperationState077();

            Assert.That(
                M1FlowPresenter.FirstOperationConsequenceStageForVerification077(state),
                Is.EqualTo(FirstOperationConsequenceStage077.RecoveryOrTraining));
            Assert.That(
                M1FlowPresenter.FirstOperationConsequenceActionForVerification077(state),
                Is.EqualTo("SET RECOVERY & TRAINING"));

            state.Assignments[0].Kind = "Training";
            Assert.That(
                M1FlowPresenter.FirstOperationConsequenceStageForVerification077(state),
                Is.EqualTo(FirstOperationConsequenceStage077.RelationshipMemory));
            Assert.That(
                M1FlowPresenter.FirstOperationConsequenceActionForVerification077(state),
                Is.EqualTo("REMEMBER THE OPERATION"));

            state.Relationships[0].Viewed = true;
            Assert.That(
                M1FlowPresenter.FirstOperationConsequenceStageForVerification077(state),
                Is.EqualTo(FirstOperationConsequenceStage077.FacilityChoice));
            Assert.That(
                M1FlowPresenter.FirstOperationConsequenceActionForVerification077(state),
                Is.EqualTo("CHOOSE YOUR FIRST FACILITY"));

            state.PlacedBuildingCount = 1;
            state.Plots[0].BuildingId = "GC017D_BUILD_CONTRACT_HOUSE";
            state.Plots[0].BuildingName = "Contract House";
            state.Plots[0].BuildingLevel = 1;
            Assert.That(
                M1FlowPresenter.FirstOperationConsequenceStageForVerification077(state),
                Is.EqualTo(FirstOperationConsequenceStage077.StaffFacility));
            Assert.That(
                M1FlowPresenter.FirstOperationConsequenceActionForVerification077(state),
                Is.EqualTo("STAFF YOUR FIRST FACILITY"));

            state.StaffedBuildingCount = 1;
            state.Plots[0].StaffRecruitIds = new[] { "RECRUIT_MIRA" };
            Assert.That(
                M1FlowPresenter.FirstOperationConsequenceStageForVerification077(state),
                Is.EqualTo(FirstOperationConsequenceStage077.Complete));
            Assert.That(M1FlowPresenter.NeedsFirstOperationConsequences077(state), Is.False);
        }

        [Test]
        public void ExistingRelationshipMayBeKeptAndMissingMemoryGetsSafeHomecomingPayoff077()
        {
            var state = FirstOperationState077();
            state.Assignments[0].Kind = "Recovering";
            state.Relationships = Array.Empty<GuildCityRelationshipView017D>();

            Assert.That(
                M1FlowPresenter.FirstOperationConsequenceStageForVerification077(state),
                Is.EqualTo(FirstOperationConsequenceStage077.RelationshipMemory),
                "Two persisted Guild members must still receive a relationship payoff if an older route produced no memory.");

            state.Assignments = new[] { state.Assignments[0] };
            Assert.That(
                M1FlowPresenter.FirstOperationConsequenceStageForVerification077(state),
                Is.EqualTo(FirstOperationConsequenceStage077.FacilityChoice),
                "A one-member migrated roster must never be soft-locked by an impossible relationship pair.");
        }

        [Test]
        public void OneMemberMigratedSaveCannotSilentlySkipFirstFacilityStaffing078()
        {
            var state = FirstOperationState077();
            state.Assignments = new[] { state.Assignments[0] };
            state.Assignments[0].Kind = "Training";
            state.Relationships = Array.Empty<GuildCityRelationshipView017D>();
            state.PlacedBuildingCount = 1;
            state.Plots[0].BuildingId = "GC017D_BUILD_CONTRACT_HOUSE";
            state.Plots[0].BuildingName = "Contract House";
            state.Plots[0].BuildingLevel = 1;

            Assert.That(
                M1FlowPresenter.FirstOperationConsequenceStageForVerification077(state),
                Is.EqualTo(FirstOperationConsequenceStage077.StaffFacility));
            Assert.That(M1FlowPresenter.GuildCityWorkshopStaffCandidateIdsForVerification078(state),
                Is.EqualTo(new[] { "RECRUIT_KIRI" }),
                "The existing Training order may move only because no active or reserve member exists.");
        }

        [Test]
        public void LaterOperationsAndAlreadyStartedChapterTwoSavesAreNeverRegated077()
        {
            var state = FirstOperationState077();
            state.OperationOrdinal = 2;
            Assert.That(
                M1FlowPresenter.FirstOperationConsequenceStageForVerification077(state),
                Is.EqualTo(FirstOperationConsequenceStage077.NotRequired));

            state.OperationOrdinal = 1;
            state.Contracts[1].IsActive = true;
            Assert.That(
                M1FlowPresenter.FirstOperationConsequenceStageForVerification077(state),
                Is.EqualTo(FirstOperationConsequenceStage077.NotRequired),
                "An older save already inside Chapter 2 must not be moved backwards into onboarding.");

            state.Contracts[1].IsActive = false;
            state.Assignments[0].Kind = "Training";
            state.Relationships[0].Viewed = true;
            state.CharterBuildCredits = 0;
            state.Plots = Array.Empty<GuildCityPlotView017D>();
            state.Buildings = Array.Empty<GuildCityBuildingView017D>();
            Assert.That(
                M1FlowPresenter.FirstOperationConsequenceStageForVerification077(state),
                Is.EqualTo(FirstOperationConsequenceStage077.Complete),
                "A migrated save without a legal charter placement must continue instead of soft-locking Chapter 2.");
        }

        private static GuildCityPresentationState017D FirstOperationState077()
        {
            return new GuildCityPresentationState017D
            {
                IsAvailable = true,
                OperationOrdinal = 1,
                CharterBuildCredits = 1,
                Assignments = new[]
                {
                    new GuildCityAssignmentView017D
                    {
                        RecruitId = "RECRUIT_KIRI",
                        RecruitName = "Kiri",
                        Kind = "Active"
                    },
                    new GuildCityAssignmentView017D
                    {
                        RecruitId = "RECRUIT_MIRA",
                        RecruitName = "Mira",
                        Kind = "Reserve"
                    }
                },
                Relationships = new[]
                {
                    new GuildCityRelationshipView017D
                    {
                        MemoryId = "REL_MEMORY_LANTERN_ROAD",
                        FirstRecruitId = "RECRUIT_KIRI",
                        SecondRecruitId = "RECRUIT_MIRA",
                        Summary = "They brought the Wayglass home together.",
                        SceneId = "REL_SCENE_LANTERN_ROAD",
                        Strength = 2,
                        Viewed = false
                    }
                },
                Plots = new[]
                {
                    new GuildCityPlotView017D
                    {
                        PlotId = "GC017D_PLOT_01",
                        DistrictId = "DISTRICT_GUILD_CORE",
                        Unlocked = true,
                        RoadConnected = true,
                        BuildingId = string.Empty
                    }
                },
                Buildings = new[]
                {
                    new GuildCityBuildingView017D
                    {
                        BuildingId = "GC017D_BUILD_CONTRACT_HOUSE",
                        DisplayName = "Contract House",
                        DistrictId = "DISTRICT_GUILD_CORE",
                        EffectIdentity = "One additional contract choice"
                    }
                },
                Contracts = new[]
                {
                    new GuildCityContractView017D
                    {
                        ContractId = FirstContractId,
                        IsCompleted = true
                    },
                    new GuildCityContractView017D
                    {
                        ContractId = SecondContractId,
                        IsCompleted = false,
                        IsActive = false
                    }
                }
            };
        }
    }
}
