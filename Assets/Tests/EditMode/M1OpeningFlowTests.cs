using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M1OpeningFlowTests
    {
        private M1CommandService _commands;
        private ApplicantBoardState _board;

        [TestCase(1920, 1080)]
        [TestCase(1280, 800)]
        public void FoundingSixUseOneReadableThreeByTwoRoleGrid(
            int width,
            int height)
        {
            var cards = M1FlowPresenter.FoundingApplicantCardRectsForVerification076(6);

            Assert.That(cards, Has.Count.EqualTo(6));
            foreach (var card in cards)
            {
                Assert.That(card.width, Is.EqualTo(cards[0].width).Within(0.0001f));
                Assert.That(card.height, Is.EqualTo(cards[0].height).Within(0.0001f));
                Assert.That(card.width * width, Is.GreaterThanOrEqualTo(395f));
                Assert.That(card.height * height, Is.GreaterThanOrEqualTo(160f));
            }
            for (var left = 0; left < cards.Count; left++)
            for (var right = left + 1; right < cards.Count; right++)
                Assert.That(cards[left].Overlaps(cards[right]), Is.False);
        }

        [TestCase(false, 6, 1, true)]
        [TestCase(false, 10, 3, true)]
        [TestCase(false, 5, 2, false)]
        [TestCase(false, 6, 0, false)]
        [TestCase(true, 10, 3, false)]
        public void PartialFoundingPreparationRecoversOnlyWhenRosterCanBeOrganized(
            bool succeeded,
            int recruitCount,
            int unionCount,
            bool expected)
        {
            Assert.That(
                M1FlowPresenter.ShouldRecoverFoundingPreparationInUnionPlanner076(
                    succeeded,
                    recruitCount,
                    unionCount),
                Is.EqualTo(expected));
        }

        [SetUp]
        public void SetUp()
        {
            _commands = new M1CommandService();
            var content = RecruitmentContent.LoadFromDirectory(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            _board = ApplicantBoardStateAdapter.ToFrozenTutorialState(
                new TutorialApplicantFactory(content).CreateFrozenBoard());
        }

        [Test]
        public void TutorialBoardCommitPersistsLiteralSeedAndExactCredit()
        {
            var campaign = CreateCommittedCampaign();

            Assert.That(campaign.OpeningFlow.TutorialSeedId, Is.EqualTo("SDGOW_TUTORIAL_V1_001"));
            Assert.That(campaign.OpeningFlow.ApplicantBoard.BoardId, Is.EqualTo("BOARD_TUTORIAL_V1_001"));
            Assert.That(campaign.OpeningFlow.ApplicantBoard.Applicants.Count, Is.EqualTo(6));
            Assert.That(campaign.OpeningFlow.SigningCreditTotal, Is.EqualTo(439));
            Assert.That(campaign.OpeningFlow.SigningCreditRemaining, Is.EqualTo(439));
            Assert.That(campaign.Guild.TreasuryXp, Is.EqualTo(0));
        }

        [Test]
        public void ReopeningCommittedBoardCannotRerollOrConsumeState()
        {
            var campaign = CreateCommittedCampaign();
            var before = CanonicalJson.Sha256Hex(campaign);
            var reopened = RequireSuccess(_commands.CommitApplicantBoard(campaign, _board));

            Assert.That(CanonicalJson.Sha256Hex(reopened), Is.EqualTo(before));
            Assert.That(reopened.OpeningFlow.ApplicantBoard.CommittedApplicantsHash,
                Is.EqualTo(campaign.OpeningFlow.ApplicantBoard.CommittedApplicantsHash));
        }

        [Test]
        public void ForgedTutorialBoardIdOrRefreshOrdinalCannotCommit()
        {
            var forgedId = new ApplicantBoardState(
                "BOARD_TUTORIAL_V1_001_FORGED",
                _board.GenerationKey,
                _board.RefreshOrdinal,
                true,
                _board.Applicants,
                _board.CommittedApplicantsHash);
            AssertCommitRejected(forgedId, "M1_TUTORIAL_BOARD_IDENTITY_MISMATCH");

            var forgedRefresh = new ApplicantBoardState(
                _board.BoardId,
                _board.GenerationKey,
                ApplicantBoardState.FrozenTutorialRefreshOrdinal + 1,
                true,
                _board.Applicants,
                _board.CommittedApplicantsHash);
            AssertCommitRejected(forgedRefresh, "M1_TUTORIAL_BOARD_IDENTITY_MISMATCH");
        }

        [Test]
        public void ForgedTutorialDisplayNameOrCostCannotSelfRehashIntoFrozenBoard()
        {
            var displayApplicants = new List<ApplicantSnapshotState>(_board.Applicants);
            displayApplicants[0] = CopyApplicant(
                displayApplicants[0],
                displayName: displayApplicants[0].DisplayName + " FORGED");
            var forgedDisplay = RehashedTutorialBoard(displayApplicants);
            Assert.That(forgedDisplay.CommittedApplicantsHash,
                Is.Not.EqualTo(ApplicantBoardState.FrozenTutorialApplicantsHash));
            AssertCommitRejected(forgedDisplay, "M1_TUTORIAL_APPLICANT_PAYLOAD_MISMATCH");

            var costApplicants = new List<ApplicantSnapshotState>(_board.Applicants);
            costApplicants[0] = CopyApplicant(
                costApplicants[0],
                signingCostTreasuryXp: costApplicants[0].SigningCostTreasuryXp + 1);
            var forgedCost = RehashedTutorialBoard(costApplicants);
            Assert.That(forgedCost.CommittedApplicantsHash,
                Is.Not.EqualTo(ApplicantBoardState.FrozenTutorialApplicantsHash));
            AssertCommitRejected(forgedCost, "M1_TUTORIAL_APPLICANT_PAYLOAD_MISMATCH");
        }

        [Test]
        public void ApplicantBoardRejectsStaleHashAfterPayloadForgery()
        {
            var applicants = new List<ApplicantSnapshotState>(_board.Applicants);
            applicants[0] = CopyApplicant(
                applicants[0],
                signingCostTreasuryXp: applicants[0].SigningCostTreasuryXp + 1);

            Assert.Throws<ArgumentException>(() => new ApplicantBoardState(
                _board.BoardId,
                _board.GenerationKey,
                _board.RefreshOrdinal,
                true,
                applicants,
                _board.CommittedApplicantsHash));
        }

        [Test]
        public void SigningAllSixUsesCharterCreditOnceAndLeavesTreasuryNeutral()
        {
            var campaign = SignAll(CreateCommittedCampaign());

            Assert.That(campaign.Guild.Recruits.Count, Is.EqualTo(6));
            Assert.That(campaign.OpeningFlow.SigningCreditRemaining, Is.EqualTo(0));
            Assert.That(campaign.Guild.TreasuryXp, Is.EqualTo(0));
            Assert.That(campaign.OpeningFlow.RecruitmentCompleted, Is.True);

            var repeated = RequireSuccess(_commands.SignApplicant(campaign, _board.Applicants[0].RecruitId));
            Assert.That(CanonicalJson.Sha256Hex(repeated), Is.EqualTo(CanonicalJson.Sha256Hex(campaign)));
        }

        [Test]
        public void SignedRecruitPreservesFullApplicantAndThreePartSignatureIdentity()
        {
            var campaign = CreateCommittedCampaign();
            var maren = _board.Applicants[4];
            campaign = RequireSuccess(_commands.SignApplicant(campaign, maren.RecruitId));
            var recruit = campaign.Guild.Recruits[0];

            Assert.That(recruit.RecruitId, Is.EqualTo("SIGI_4559425B6CF6CBE6"));
            Assert.That(recruit.SignatureId, Is.EqualTo("SIG_W01_01"));
            Assert.That(recruit.TutorialAliasId, Is.EqualTo("SIG_MAREN_HOLT"));
            Assert.That(recruit.AuthoredStableRecruitId, Is.EqualTo("SIGREC_MAREN_HOLT"));
            Assert.That(recruit.CanonicalApplicantJson, Is.EqualTo(maren.CanonicalApplicantJson));
            Assert.That(recruit.CanonicalScoutingReportJson, Is.EqualTo(maren.CanonicalScoutingReportJson));
        }

        [Test]
        public void EverySignedRecruitHasSixManualSlotsAndStarterBodyArmor()
        {
            var campaign = SignAll(CreateCommittedCampaign());

            foreach (var recruit in campaign.Guild.Recruits)
            {
                Assert.That(recruit.Equipment.Slots.Count, Is.EqualTo(6), recruit.RecruitId);
                Assert.That(recruit.Equipment.Find(EquipmentSlotIds.BodyArmor), Is.Not.Null, recruit.RecruitId);
            }
            Assert.That(campaign.OpeningFlow.ManualEquipmentCommitObserved, Is.False);
        }

        [Test]
        public void StarterLoadoutsCanContinueWithoutChurnAndManualChangesRemainAvailable()
        {
            var starterCampaign = SignAll(CreateCommittedCampaign());
            Assert.That(starterCampaign.OpeningFlow.ManualEquipmentCommitObserved, Is.False);
            starterCampaign = RequireSuccess(_commands.CompleteEquipmentReview(starterCampaign));
            Assert.That(starterCampaign.OpeningFlow.EquipmentReviewCompleted, Is.True);

            var campaign = SignAll(CreateCommittedCampaign());
            var recruit = campaign.Guild.Recruits[0];
            var assignment = recruit.Equipment.Assignments[0];
            var otherHash = CanonicalJson.Sha256Hex(campaign.Guild.Recruits[1]);

            campaign = RequireSuccess(_commands.UnequipItem(campaign, recruit.RecruitId, assignment.SlotId));
            Assert.That(campaign.OpeningFlow.ManualEquipmentCommitObserved, Is.True);
            Assert.That(campaign.Guild.Inventory.Count, Is.EqualTo(1));
            campaign = RequireSuccess(_commands.EquipItem(
                campaign, recruit.RecruitId, assignment.SlotId, assignment.Item.InstanceId));

            Assert.That(campaign.Guild.Inventory.Count, Is.EqualTo(0));
            Assert.That(CanonicalJson.Sha256Hex(campaign.Guild.Recruits[1]), Is.EqualTo(otherHash));
            Assert.That(campaign.OpeningFlow.ManualEquipmentCommitObserved, Is.True);
            Assert.That(campaign.OpeningFlow.Stage, Is.EqualTo(OpeningStage.Equipment));
            Assert.That(campaign.OpeningFlow.EquipmentReviewCompleted, Is.False);

            var completed = BuildCompletedOpening();
            var completedRecruit = completed.Guild.Recruits[0];
            var completedAssignment = completedRecruit.Equipment.Find(EquipmentSlotIds.MainHand);
            completed = RequireSuccess(_commands.UnequipItem(
                completed, completedRecruit.RecruitId, completedAssignment.SlotId));
            completed = RequireSuccess(_commands.EquipItem(
                completed, completedRecruit.RecruitId, completedAssignment.SlotId,
                completedAssignment.Item.InstanceId));

            Assert.That(completed.OpeningFlow.Stage, Is.EqualTo(OpeningStage.Complete));
            Assert.That(completed.OpeningFlow.EquipmentReviewCompleted, Is.True);
            Assert.That(completed.OpeningFlow.UnionBuilderCompleted, Is.True);
            Assert.That(completed.OpeningFlow.ManualEquipmentCommitObserved, Is.True);
            Assert.That(_commands.CompleteOpening(completed).IsSuccess, Is.True);
        }

        [Test]
        public void LockedEquipmentCannotMoveUntilPlayerExplicitlyUnlocksIt()
        {
            var campaign = SignAll(CreateCommittedCampaign());
            var recruit = campaign.Guild.Recruits[0];
            var assignment = recruit.Equipment.Assignments[0];
            campaign = RequireSuccess(_commands.SetEquipmentLock(
                campaign, recruit.RecruitId, assignment.SlotId, playerLocked: true));

            var blocked = _commands.UnequipItem(campaign, recruit.RecruitId, assignment.SlotId);
            Assert.That(blocked.IsSuccess, Is.False);

            campaign = RequireSuccess(_commands.SetEquipmentLock(
                campaign, recruit.RecruitId, assignment.SlotId, playerLocked: false));
            campaign = RequireSuccess(_commands.UnequipItem(campaign, recruit.RecruitId, assignment.SlotId));
            Assert.That(campaign.Guild.Inventory.Count, Is.EqualTo(1));
        }

        [Test]
        public void FirstAddUnionQuickStartsExactlyTwoLegalThreeFounderPlans()
        {
            var campaign = SignAll(CreateCommittedCampaign());
            campaign = RequireSuccess(_commands.CompleteEquipmentReview(campaign));
            Assert.That(campaign.Guild.Unions, Is.Empty);

            campaign = RequireSuccess(_commands.AddOpeningUnion(campaign));

            Assert.That(campaign.Guild.Unions.Count, Is.EqualTo(2));
            Assert.That(campaign.Guild.Unions.Select(value => value.MemberRecruitIds.Count),
                Is.EqualTo(new[] { 3, 3 }));
            Assert.That(campaign.Guild.Unions.SelectMany(value => value.MemberRecruitIds),
                Is.EqualTo(campaign.Guild.Recruits.Take(6).Select(value => value.RecruitId)));
            Assert.That(campaign.Guild.Unions.All(value =>
                value.LeaderRecruitId == value.MemberRecruitIds[0]), Is.True);
            Assert.That(campaign.Guild.Unions.Select(value => value.FormationId).ToArray(),
                Is.EqualTo(new[] { "FORMATION_SHIELD_WALL", "FORMATION_WEDGE" }),
                "The two quick-start groups must teach distinct legal tactical roles.");
            Assert.That(campaign.Guild.Unions.All(value =>
                value.DoctrineId == "DOCTRINE_BALANCED"), Is.True);
            Assert.That(_commands.ValidateOpeningUnions(campaign.Guild).IsSuccess, Is.True);
            Assert.That(campaign.OpeningFlow.UnionBuilderCompleted, Is.True);

            var quickStartedHash = CanonicalJson.Sha256Hex(campaign.Guild.Unions);
            campaign = RequireSuccess(_commands.AddOpeningUnion(campaign));
            Assert.That(campaign.Guild.Unions.Count, Is.EqualTo(3),
                "Once drafts exist, Add Union must return to adding one explicit empty plan.");
            Assert.That(campaign.Guild.Unions[2].MemberRecruitIds, Is.Empty);
            Assert.That(campaign.Guild.Unions[2].FormationId,
                Is.EqualTo("FORMATION_SKIRMISH_LINE"),
                "The third starter plan must complete the Hold / Break / Move teaching set.");
            Assert.That(campaign.Guild.Unions[2].DoctrineId, Is.EqualTo("DOCTRINE_BALANCED"));
            Assert.That(CanonicalJson.Sha256Hex(campaign.Guild.Unions.Take(2).ToArray()),
                Is.EqualTo(quickStartedHash));

            while (campaign.Guild.Unions.Count < NormalUnionPlanRules.MaximumPlanCount)
                campaign = RequireSuccess(_commands.AddOpeningUnion(campaign));
            Assert.That(campaign.Guild.Unions.Select(value => value.UnionId).Distinct().Count(),
                Is.EqualTo(NormalUnionPlanRules.MaximumPlanCount));
            Assert.That(campaign.Guild.Unions.Last().UnionId, Is.EqualTo("UNION_OPENING_10"));
            Assert.That(_commands.AddOpeningUnion(campaign).Errors,
                Does.Contain("M1_MAXIMUM_NORMAL_UNION_PLANS_REACHED"));
        }

        [Test]
        public void SuggestedUnionsAtomicallyGroupTheOpeningRosterByCombatRole087()
        {
            var campaign = SignAll(CreateCommittedCampaign());
            campaign = RequireSuccess(_commands.CompleteEquipmentReview(campaign));

            var suggested = _commands.ApplySuggestedRoleUnions087(campaign);

            Assert.That(suggested.IsSuccess, Is.True,
                string.Join("\n", suggested.Errors));
            campaign = suggested.Value;
            var used = campaign.Guild.Unions
                .Where(value => value.MemberRecruitIds.Count > 0)
                .ToArray();
            Assert.That(used, Has.Length.EqualTo(4));
            Assert.That(used.SelectMany(value => value.MemberRecruitIds).Distinct().Count(),
                Is.EqualTo(6));
            foreach (var union in used)
            {
                var roles = union.MemberRecruitIds
                    .Select(id => campaign.Guild.Recruits.Single(
                        value => value.RecruitId == id))
                    .Select(value => M1CommandService.SuggestedUnionRole087(
                        value.ClassTendencyId))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                Assert.That(roles, Has.Length.EqualTo(1),
                    "A suggested Union must keep one combat-training focus.");
            }
            Assert.That(used.Single(value =>
                    M1CommandService.SuggestedUnionRole087(
                        campaign.Guild.Recruits.Single(recruit =>
                            recruit.RecruitId == value.MemberRecruitIds[0])
                            .ClassTendencyId) == "HEALER")
                .MemberRecruitIds, Has.Count.EqualTo(2));
            Assert.That(_commands.ValidateOpeningUnions(campaign.Guild).IsSuccess,
                Is.True);
        }

        [Test]
        public void CompletedLegacyTwoByThreePlansRemainLegalUnderCampaignRules()
        {
            var campaign = BuildCompletedOpening();

            Assert.That(OpeningFlowState.RequiredOpeningRecruitCount, Is.EqualTo(6));
            Assert.That(OpeningFlowState.RecommendedOpeningUnionCount, Is.EqualTo(2));
            Assert.That(OpeningFlowState.RecommendedOpeningUnionMemberCount, Is.EqualTo(3));
            Assert.That(NormalUnionPlanRules.MaximumMembersPerUnion, Is.EqualTo(6));
            Assert.That(NormalUnionPlanRules.MaximumPlanCount, Is.EqualTo(10));
            Assert.That(new M1PresentationState().MaximumUnionPlanCount,
                Is.EqualTo(NormalUnionPlanRules.MaximumPlanCount));
            Assert.That(campaign.Guild.Unions.Select(value => value.MemberRecruitIds.Count),
                Is.EqualTo(new[] { 3, 3 }));
            Assert.That(_commands.ValidateGuildUnionPlans(campaign.Guild).IsSuccess, Is.True,
                "Existing completed 2x3 saves must remain legal after campaign capacity expands.");
        }

        [Test]
        public void CampaignRulesAllowThreeFullSixMemberUnionsWithTwoReserves()
        {
            var guild = CreateCapacityGuild(unionCount: 3, membersPerUnion: 6, reserveCount: 2);

            var validation = _commands.ValidateGuildUnionPlans(guild);

            Assert.That(validation.IsSuccess, Is.True, string.Join("\n", validation.Errors));
            Assert.That(guild.Unions, Has.Count.EqualTo(3));
            Assert.That(guild.Unions.All(value => value.MemberRecruitIds.Count == 6), Is.True);
            Assert.That(guild.Recruits, Has.Count.EqualTo(20));
            Assert.That(guild.Unions.SelectMany(value => value.MemberRecruitIds).Distinct().Count(),
                Is.EqualTo(18));
        }

        [Test]
        public void AssigningUnassignedRecruitToLaterEmptySlotAppendsToUnion()
        {
            var campaign = SignAll(CreateCommittedCampaign());
            campaign = RequireSuccess(_commands.CompleteEquipmentReview(campaign));

            campaign = RequireSuccess(_commands.AssignRecruitToUnion(
                campaign,
                campaign.Guild.Recruits[0].RecruitId,
                unionIndex: 0,
                slotIndex: 0));
            campaign = RequireSuccess(_commands.AssignRecruitToUnion(
                campaign,
                campaign.Guild.Recruits[1].RecruitId,
                unionIndex: 0,
                slotIndex: 1));

            var unassignedId = campaign.Guild.Recruits[2].RecruitId;
            campaign = RequireSuccess(_commands.AssignRecruitToUnion(
                campaign,
                unassignedId,
                unionIndex: 0,
                slotIndex: 5));

            Assert.That(campaign.Guild.Unions[0].MemberRecruitIds.Count, Is.EqualTo(3));
            Assert.That(campaign.Guild.Unions[0].MemberRecruitIds[2], Is.EqualTo(unassignedId));
        }

        [Test]
        public void CampaignAssignmentAcceptsSixthSlotAndRejectsSeventh()
        {
            var baseCampaign = BuildCompletedOpening();
            var campaign = baseCampaign.With(
                CreateCapacityGuild(unionCount: 2, membersPerUnion: 1, reserveCount: 10),
                baseCampaign.OpeningFlow);

            for (var slotIndex = 1; slotIndex < NormalUnionPlanRules.MaximumMembersPerUnion; slotIndex++)
            {
                campaign = RequireSuccess(_commands.AssignRecruitToUnion(
                    campaign,
                    campaign.Guild.Recruits[slotIndex + 1].RecruitId,
                    unionIndex: 0,
                    slotIndex: slotIndex));
            }

            Assert.That(campaign.Guild.Unions[0].MemberRecruitIds,
                Has.Count.EqualTo(NormalUnionPlanRules.MaximumMembersPerUnion));
            Assert.That(_commands.ValidateGuildUnionPlans(campaign.Guild).IsSuccess, Is.True);
            var blocked = _commands.AssignRecruitToUnion(
                campaign,
                campaign.Guild.Recruits[NormalUnionPlanRules.MaximumMembersPerUnion + 1].RecruitId,
                unionIndex: 0,
                slotIndex: NormalUnionPlanRules.MaximumMembersPerUnion);
            Assert.That(blocked.Errors, Does.Contain("M1_UNION_SLOT_INVALID"));
        }

        [Test]
        public void MaximumTenBySixCampaignPlanRoundTripsWithoutSaveMigration()
        {
            var baseCampaign = BuildCompletedOpening();
            var maximumGuild = CreateCapacityGuild(
                NormalUnionPlanRules.MaximumPlanCount,
                NormalUnionPlanRules.MaximumMembersPerUnion,
                reserveCount: 0);
            var maximumCampaign = baseCampaign.With(maximumGuild, baseCampaign.OpeningFlow);
            var validation = _commands.ValidateGuildUnionPlans(maximumGuild);
            Assert.That(validation.IsSuccess, Is.True, string.Join("\n", validation.Errors));

            var directory = Path.Combine(
                Path.GetTempPath(), "SecondDimensionM1CapacityTests", Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "campaign.json");
            try
            {
                var before = CanonicalJson.Sha256Hex(maximumCampaign);
                var store = new AtomicSaveStore();
                store.Write(path, SaveEnvelopeV1.Create(
                    maximumCampaign, new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

                var loaded = store.ReadWithRecovery(path);

                Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
                Assert.That(loaded.Value.SaveFormatVersion, Is.EqualTo(SaveEnvelopeV1.CurrentFormatVersion));
                Assert.That(CanonicalJson.Sha256Hex(loaded.Value.CampaignState), Is.EqualTo(before));
                Assert.That(loaded.Value.CampaignState.Guild.Unions,
                    Has.Count.EqualTo(NormalUnionPlanRules.MaximumPlanCount));
                Assert.That(loaded.Value.CampaignState.Guild.Unions.All(value =>
                    value.MemberRecruitIds.Count == NormalUnionPlanRules.MaximumMembersPerUnion &&
                    value.MemberPositions.Count == NormalUnionPlanRules.MaximumMembersPerUnion), Is.True);
                Assert.That(_commands.ValidateGuildUnionPlans(
                    loaded.Value.CampaignState.Guild).IsSuccess, Is.True);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void CampaignRulesRejectEleventhPlanAndSeventhMember()
        {
            var tooManyPlans = CreateCapacityGuild(
                NormalUnionPlanRules.MaximumPlanCount + 1,
                membersPerUnion: 1,
                reserveCount: 0);
            var overfilledUnion = CreateCapacityGuild(
                unionCount: 2,
                membersPerUnion: NormalUnionPlanRules.MaximumMembersPerUnion + 1,
                reserveCount: 0);

            Assert.That(_commands.ValidateGuildUnionPlans(tooManyPlans).Errors,
                Does.Contain("M1_NORMAL_UNION_PLAN_COUNT_OUT_OF_RANGE"));
            Assert.That(_commands.ValidateGuildUnionPlans(overfilledUnion).Errors,
                Does.Contain("M1_NORMAL_UNION_SUPPORTS_ONE_TO_SIX_MEMBERS"));
        }

        [Test]
        public void PlayerCanCreateConventionalOrFlexibleOpeningUnionPlansAndReuseFormations()
        {
            var campaign = BuildCompletedOpening();
            var ids = new HashSet<string>(StringComparer.Ordinal);

            Assert.That(campaign.Guild.Unions.Count, Is.EqualTo(2));
            foreach (var union in campaign.Guild.Unions)
            {
                Assert.That(union.Kind, Is.EqualTo(UnionKind.Normal));
                Assert.That(union.MemberRecruitIds.Count, Is.EqualTo(3));
                Assert.That(union.LeaderRecruitId, Is.EqualTo(union.MemberRecruitIds[0]));
                Assert.That(union.SharedAp, Is.InRange(14, 30));
                Assert.That(union.CohesionBasisPoints, Is.InRange(6000, 10000));
                var preview = M1CommandService.ProjectOpeningUnionResources(campaign.Guild, union);
                Assert.That(preview.SharedAp, Is.EqualTo(union.SharedAp));
                Assert.That(preview.CohesionBasisPoints, Is.EqualTo(union.CohesionBasisPoints));
                var expectedFormationRule = union.FormationId == "FORMATION_SHIELD_WALL" ? 800 : 0;
                Assert.That(preview.FormationCohesionRuleBasisPoints, Is.EqualTo(expectedFormationRule));
                Assert.That(preview.AppliedFormationCohesionBasisPoints,
                    Is.InRange(0, preview.FormationCohesionRuleBasisPoints));
                foreach (var id in union.MemberRecruitIds) Assert.That(ids.Add(id), Is.True, id);
            }
            Assert.That(ids.Count, Is.EqualTo(6));
            Assert.That(campaign.OpeningFlow.UnionBuilderCompleted, Is.True);

            var flexible = SignAll(CreateCommittedCampaign());
            flexible = RequireSuccess(_commands.CompleteEquipmentReview(flexible));
            flexible = RequireSuccess(_commands.AddOpeningUnion(flexible));
            flexible = RequireSuccess(_commands.AddOpeningUnion(flexible));
            flexible = RequireSuccess(_commands.AddOpeningUnion(flexible));

            for (var index = 0; index < 3; index++)
            {
                flexible = RequireSuccess(_commands.AssignRecruitToUnion(
                    flexible, flexible.Guild.Recruits[index].RecruitId, 0, index));
            }
            for (var index = 3; index < 6; index++)
            {
                flexible = RequireSuccess(_commands.AssignRecruitToUnion(
                    flexible, flexible.Guild.Recruits[index].RecruitId, index - 2, 0));
            }

            var firstMember = flexible.Guild.Unions[0].MemberRecruitIds[0];
            var fourthMember = flexible.Guild.Unions[1].MemberRecruitIds[0];
            flexible = RequireSuccess(_commands.AssignRecruitToUnion(flexible, firstMember, 1, 0));
            Assert.That(flexible.Guild.Unions[0].MemberRecruitIds[0], Is.EqualTo(fourthMember));
            Assert.That(flexible.Guild.Unions[1].MemberRecruitIds[0], Is.EqualTo(firstMember));
            Assert.That(flexible.Guild.Unions.All(value => value.LeaderRecruitId == value.MemberRecruitIds[0]), Is.True);

            var returnedRecruit = flexible.Guild.Unions[3].MemberRecruitIds[0];
            flexible = RequireSuccess(_commands.UnassignRecruitFromUnion(flexible, returnedRecruit));
            Assert.That(flexible.Guild.Unions.SelectMany(value => value.MemberRecruitIds), Does.Not.Contain(returnedRecruit));
            flexible = RequireSuccess(_commands.AssignRecruitToUnion(flexible, returnedRecruit, 3, 0));
            Assert.That(flexible.Guild.Unions[3].LeaderRecruitId, Is.EqualTo(returnedRecruit));

            for (var unionIndex = 0; unionIndex < 4; unionIndex++)
            {
                flexible = RequireSuccess(_commands.SetFormation(
                    flexible, unionIndex, "FORMATION_SHIELD_WALL"));
            }

            var liveDraftPreview = M1CommandService.ProjectOpeningUnionResources(
                flexible.Guild,
                flexible.Guild.Unions[0]);
            Assert.That(flexible.Guild.Unions[0].SharedAp, Is.Zero,
                "Draft state should remain unfinalized while the UI reads the pure preview.");
            Assert.That(liveDraftPreview.SharedAp, Is.InRange(14, 30));
            Assert.That(liveDraftPreview.FormationCohesionRuleBasisPoints, Is.EqualTo(800));

            flexible = RequireSuccess(_commands.CompleteOpening(flexible));
            Assert.That(flexible.Guild.Unions.Count, Is.EqualTo(4));
            Assert.That(flexible.Guild.Unions.Select(value => value.MemberRecruitIds.Count),
                Is.EqualTo(new[] { 3, 1, 1, 1 }));
            Assert.That(flexible.Guild.Unions.All(value => value.FormationId == "FORMATION_SHIELD_WALL"), Is.True);
            Assert.That(flexible.Guild.Unions.All(value => value.LeaderRecruitId == value.MemberRecruitIds[0]), Is.True);
            Assert.That(flexible.Guild.Unions[0].SharedAp, Is.EqualTo(liveDraftPreview.SharedAp));
            Assert.That(flexible.Guild.Unions[0].CohesionBasisPoints, Is.EqualTo(liveDraftPreview.CohesionBasisPoints));
        }

        [Test]
        public void DuplicateMemberAcrossOpeningUnionsIsRejected()
        {
            var campaign = SignAll(CreateCommittedCampaign());
            var ids = campaign.Guild.Recruits;
            var first = new UnionState(
                "UNION_OPENING_01", "One", UnionKind.Normal, ids[0].RecruitId,
                new[] { ids[0].RecruitId, ids[1].RecruitId, ids[2].RecruitId },
                "FORMATION_SHIELD_WALL", "DOCTRINE_BALANCED", 14, 8000);
            var second = new UnionState(
                "UNION_OPENING_02", "Two", UnionKind.Normal, ids[0].RecruitId,
                new[] { ids[0].RecruitId, ids[4].RecruitId, ids[5].RecruitId },
                "FORMATION_WEDGE", "DOCTRINE_GUARDIAN", 14, 8000);
            var malformed = campaign.Guild.With(
                campaign.Guild.TreasuryXp, campaign.Guild.Recruits,
                new[] { first, second }, campaign.Guild.Inventory);

            Assert.That(_commands.ValidateOpeningUnions(malformed).IsSuccess, Is.False);
        }

        [Test]
        public void CompleteM1SaveRoundTripPreservesCanonicalHashAndOpeningState()
        {
            var campaign = BuildCompletedOpening();
            var directory = Path.Combine(Path.GetTempPath(), "SecondDimensionM1Tests", Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "campaign.json");
            try
            {
                var before = CanonicalJson.Sha256Hex(campaign);
                var store = new AtomicSaveStore();
                store.Write(path, SaveEnvelopeV1.Create(
                    campaign, new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
                var loaded = store.ReadWithRecovery(path);

                Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
                Assert.That(CanonicalJson.Sha256Hex(loaded.Value.CampaignState), Is.EqualTo(before));
                Assert.That(loaded.Value.CampaignState.OpeningFlow.ApplicantBoard.BoardId,
                    Is.EqualTo("BOARD_TUTORIAL_V1_001"));
                Assert.That(loaded.Value.CampaignState.Guild.Unions[0].MemberPositions.Count, Is.EqualTo(3));
                Assert.That(loaded.Value.CampaignState.Guild.Unions
                        .Select(value => value.FormationId)
                        .ToArray(),
                    Is.EqualTo(new[] { "FORMATION_SHIELD_WALL", "FORMATION_WEDGE" }),
                    "Save/reload must preserve the exact starter tactical identities.");
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            }
        }

        private CampaignState CreateCommittedCampaign()
        {
            var campaign = CreateBoardReadyCampaign();
            return RequireSuccess(_commands.CommitApplicantBoard(campaign, _board));
        }

        private CampaignState CreateBoardReadyCampaign()
        {
            var profile = new NewGuildProfileState(
                "Test Guildmaster", GameMode.Standard, TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(), ironConsequencesAcknowledged: false);
            var campaign = RequireSuccess(_commands.CreateNewGuild(new NewGuildCommand(
                "00000000-0000-0000-0000-000000000002", 20260813L, "1.0",
                "GUILD_TUTORIAL_M1", profile)));
            return RequireSuccess(_commands.AcceptCivicCharter(campaign));
        }

        private ApplicantBoardState RehashedTutorialBoard(IReadOnlyList<ApplicantSnapshotState> applicants) =>
            new ApplicantBoardState(
                _board.BoardId,
                _board.GenerationKey,
                _board.RefreshOrdinal,
                true,
                applicants,
                committedApplicantsHash: string.Empty);

        private void AssertCommitRejected(ApplicantBoardState board, string expectedError)
        {
            var result = _commands.CommitApplicantBoard(CreateBoardReadyCampaign(), board);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors, Does.Contain(expectedError));
        }

        private static ApplicantSnapshotState CopyApplicant(
            ApplicantSnapshotState source,
            string displayName = null,
            int? signingCostTreasuryXp = null) =>
            new ApplicantSnapshotState(
                source.Slot,
                source.RecruitId,
                displayName ?? source.DisplayName,
                source.Kind,
                source.SourceSeed,
                source.SignatureId,
                source.RaceId,
                source.WorldId,
                source.ClassTendencyId,
                source.LeadershipBand,
                source.CurrentHp,
                source.MaximumHp,
                source.CurrentMp,
                source.MaximumMp,
                source.VitalsInitialized,
                signingCostTreasuryXp ?? source.SigningCostTreasuryXp,
                source.PotentialBasisPoints,
                source.CanonicalApplicantJson,
                source.CanonicalScoutingReportJson,
                source.OpeningEquipment,
                source.TutorialAliasId,
                source.AuthoredStableRecruitId,
                source.OpeningLoadout,
                source.LeadershipScore,
                source.TacticalAptitude);

        private CampaignState SignAll(CampaignState campaign)
        {
            foreach (var applicant in _board.Applicants)
            {
                campaign = RequireSuccess(_commands.SignApplicant(campaign, applicant.RecruitId));
            }
            return campaign;
        }

        private CampaignState BuildCompletedOpening()
        {
            var campaign = SignAll(CreateCommittedCampaign());
            campaign = RequireSuccess(_commands.CompleteEquipmentReview(campaign));

            for (var index = 0; index < 6; index++)
            {
                campaign = RequireSuccess(_commands.AssignRecruitToUnion(
                    campaign, campaign.Guild.Recruits[index].RecruitId, index / 3, index % 3));
            }
            campaign = RequireSuccess(_commands.SetFormation(campaign, 0, "FORMATION_SHIELD_WALL"));
            campaign = RequireSuccess(_commands.SetFormation(campaign, 1, "FORMATION_WEDGE"));
            campaign = RequireSuccess(_commands.SetDoctrine(campaign, 0, "DOCTRINE_BALANCED"));
            campaign = RequireSuccess(_commands.SetDoctrine(campaign, 1, "DOCTRINE_GUARDIAN"));
            return RequireSuccess(_commands.CompleteOpening(campaign));
        }

        private static GuildState CreateCapacityGuild(
            int unionCount,
            int membersPerUnion,
            int reserveCount)
        {
            var recruitCount = checked(unionCount * membersPerUnion + reserveCount);
            var recruits = new List<RecruitState>(recruitCount);
            for (var recruitIndex = 0; recruitIndex < recruitCount; recruitIndex++)
                recruits.Add(new RecruitState(
                    "CAPACITY_RECRUIT_" + (recruitIndex + 1).ToString("D3"),
                    100,
                    100,
                    20,
                    20));

            var unions = new List<UnionState>(unionCount);
            for (var unionIndex = 0; unionIndex < unionCount; unionIndex++)
            {
                var members = recruits
                    .Skip(unionIndex * membersPerUnion)
                    .Take(membersPerUnion)
                    .Select(value => value.RecruitId)
                    .ToArray();
                unions.Add(new UnionState(
                    "UNION_CAPACITY_" + (unionIndex + 1).ToString("D2"),
                    "Capacity Union " + (unionIndex + 1),
                    UnionKind.Normal,
                    members[0],
                    members,
                    "FORMATION_SKIRMISH_LINE",
                    "DOCTRINE_BALANCED",
                    30,
                    7000));
            }

            return new GuildState(
                "GUILD_CAPACITY_TEST",
                0,
                recruits.AsReadOnly(),
                unions.AsReadOnly(),
                Array.Empty<EquipmentItemState>());
        }

        private static T RequireSuccess<T>(Result<T> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
