using System;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.M1
{
    /// <summary>
    /// Campaign-wide limits for ordinary player-authored Union plans. Founding
    /// composition is intentionally separate: the opening still introduces six
    /// recruits as two recommended three-person Unions, while later recruitment
    /// can grow a plan to six members and the deployed force to ten Unions.
    /// </summary>
    public static class NormalUnionPlanRules
    {
        public const int MinimumUsedUnionCount = 2;
        public const int MaximumPlanCount = 10;
        public const int MaximumDeployedUnionCount = MaximumPlanCount;
        public const int MaximumMembersPerUnion = 6;
    }

    public enum OpeningStage
    {
        Charter,
        ApplicantBoard,
        Recruitment,
        Equipment,
        UnionBuilder,
        Complete
    }

    [Serializable]
    public sealed class OpeningFlowState
    {
        public const int RequiredOpeningRecruitCount = 6;
        public const int RecommendedOpeningUnionCount = 2;
        public const int RecommendedOpeningUnionMemberCount = 3;

        // Retained aliases keep old M1 callers and save-facing terminology stable.
        // "Maximum opening" now aliases the M1 command-surface capacity; it no
        // longer describes the recommended 2x3 quick start.
        public const int MinimumOpeningUnionCount = NormalUnionPlanRules.MinimumUsedUnionCount;
        public const int MaximumOpeningUnionCount = NormalUnionPlanRules.MaximumPlanCount;
        public const int MaximumOpeningUnionMemberCount = NormalUnionPlanRules.MaximumMembersPerUnion;

        public const int RequiredOpeningUnionCount = RecommendedOpeningUnionCount;
        public const int RequiredOpeningUnionMemberCount = RecommendedOpeningUnionMemberCount;

        public OpeningFlowState(
            OpeningStage stage,
            bool civicCharterAccepted,
            ApplicantBoardState applicantBoard,
            bool equipmentReviewCompleted,
            string lastCheckpointId)
            : this(
                stage,
                ApplicantBoardState.FrozenTutorialSeedId,
                civicCharterAccepted,
                applicantBoard,
                applicantBoard != null,
                0,
                0,
                false,
                equipmentReviewCompleted,
                false,
                false,
                lastCheckpointId)
        {
        }

        public OpeningFlowState(
            OpeningStage stage,
            string tutorialSeedId,
            bool civicCharterAccepted,
            ApplicantBoardState applicantBoard,
            bool applicantBoardCommitted,
            int signingCreditTotal,
            int signingCreditRemaining,
            bool recruitmentCompleted,
            bool equipmentReviewCompleted,
            bool unionBuilderCompleted,
            string lastCheckpointId)
            : this(
                stage,
                tutorialSeedId,
                civicCharterAccepted,
                applicantBoard,
                applicantBoardCommitted,
                signingCreditTotal,
                signingCreditRemaining,
                recruitmentCompleted,
                equipmentReviewCompleted,
                unionBuilderCompleted,
                false,
                lastCheckpointId)
        {
        }

        [JsonConstructor]
        public OpeningFlowState(
            OpeningStage stage,
            string tutorialSeedId,
            bool civicCharterAccepted,
            ApplicantBoardState applicantBoard,
            bool applicantBoardCommitted,
            int signingCreditTotal,
            int signingCreditRemaining,
            bool recruitmentCompleted,
            bool equipmentReviewCompleted,
            bool unionBuilderCompleted,
            bool manualEquipmentCommitObserved,
            string lastCheckpointId)
        {
            TutorialSeedId = string.IsNullOrWhiteSpace(tutorialSeedId)
                ? throw new ArgumentException("Tutorial seed ID is required.", nameof(tutorialSeedId))
                : tutorialSeedId;
            if (applicantBoardCommitted != (applicantBoard != null))
            {
                throw new ArgumentException("Committed-board flag must match the persisted board payload.");
            }
            if (signingCreditTotal < 0 || signingCreditRemaining < 0 || signingCreditRemaining > signingCreditTotal)
            {
                throw new ArgumentOutOfRangeException(nameof(signingCreditRemaining));
            }
            Stage = stage;
            CivicCharterAccepted = civicCharterAccepted;
            ApplicantBoard = applicantBoard;
            ApplicantBoardCommitted = applicantBoardCommitted;
            SigningCreditTotal = signingCreditTotal;
            SigningCreditRemaining = signingCreditRemaining;
            RecruitmentCompleted = recruitmentCompleted;
            EquipmentReviewCompleted = equipmentReviewCompleted;
            UnionBuilderCompleted = unionBuilderCompleted;
            ManualEquipmentCommitObserved = manualEquipmentCommitObserved;
            LastCheckpointId = lastCheckpointId ?? string.Empty;
        }

        public OpeningStage Stage { get; }
        public string TutorialSeedId { get; }
        public bool CivicCharterAccepted { get; }
        public ApplicantBoardState ApplicantBoard { get; }
        public bool ApplicantBoardCommitted { get; }
        public int SigningCreditTotal { get; }
        public int SigningCreditRemaining { get; }
        public bool RecruitmentCompleted { get; }
        public bool EquipmentReviewCompleted { get; }
        public bool UnionBuilderCompleted { get; }
        public bool ManualEquipmentCommitObserved { get; }
        public string LastCheckpointId { get; }

        public static OpeningFlowState NewProfileCommitted() =>
            new OpeningFlowState(
                OpeningStage.Charter,
                ApplicantBoardState.FrozenTutorialSeedId,
                civicCharterAccepted: false,
                applicantBoard: null,
                applicantBoardCommitted: false,
                signingCreditTotal: 0,
                signingCreditRemaining: 0,
                recruitmentCompleted: false,
                equipmentReviewCompleted: false,
                unionBuilderCompleted: false,
                lastCheckpointId: "autosave_profile");
    }
}
