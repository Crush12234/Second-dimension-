using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Progression070;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation.GuildCity017D
{
    public sealed class GuildCityPlotView017D
    {
        public string PlotId { get; set; }
        public string DistrictId { get; set; }
        public string Size { get; set; }
        public bool Unlocked { get; set; }
        public bool RoadConnected { get; set; }
        public string BuildingId { get; set; }
        public string BuildingName { get; set; }
        public int BuildingLevel { get; set; }
        public int ConstructionProgress { get; set; }
        public IReadOnlyList<string> StaffRecruitIds { get; set; } = Array.Empty<string>();
    }

    public sealed class GuildCityBuildingView017D
    {
        public string BuildingId { get; set; }
        public string DisplayName { get; set; }
        public string DistrictId { get; set; }
        public string EffectIdentity { get; set; }
        public int MaxLevel { get; set; }
        public bool HasLevelOneCost { get; set; }
        public long LevelOneHallXpCost { get; set; }
        public bool CanAffordLevelOneWithResources { get; set; }
        public IReadOnlyList<GuildCityMaterialRequirementView017D> LevelOneMaterialRequirements { get; set; } =
            Array.Empty<GuildCityMaterialRequirementView017D>();
        public IReadOnlyList<string> AdjacencyBuildingIds { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> StaffRoles { get; set; } = Array.Empty<string>();
    }

    public sealed class GuildCityMaterialRequirementView017D
    {
        public string MaterialId { get; set; }
        public string DisplayName { get; set; }
        public int RequiredAmount { get; set; }
        public int AvailableAmount { get; set; }
    }

    public sealed class GuildCityAssignmentView017D
    {
        public string RecruitId { get; set; }
        public string RecruitName { get; set; }
        public string Kind { get; set; }
        public string FacilityId { get; set; }
        public int RecoveryProgress { get; set; }
        public int TrainingProgress { get; set; }
        public int DutyProgress { get; set; }
    }

    public sealed class GuildCityRelationshipView017D
    {
        public string MemoryId { get; set; }
        public string FirstRecruitId { get; set; }
        public string SecondRecruitId { get; set; }
        public string Summary { get; set; }
        public string SceneId { get; set; }
        public bool Viewed { get; set; }
        public int Strength { get; set; }
    }


    public sealed class GuildCityApplicantView017D
    {
        public int Slot { get; set; }
        public string RecruitId { get; set; }
        public string OwnedRecruitId { get; set; }
        public string DisplayName { get; set; }
        public string Kind { get; set; }
        public string RaceId { get; set; }
        public string WorldId { get; set; }
        public string ClassTendencyId { get; set; }
        public string AuthoredRole096 { get; set; }
        public string AuthoredWeaponStyle096 { get; set; }
        public string LeadershipBand { get; set; }
        public int SigningCostTreasuryXp { get; set; }
        public bool IsSigned { get; set; }
        public string ObservedSummary { get; set; }
        public string VisualSeed { get; set; }
        public string PortraitAuthorityId { get; set; }
        public string ClassSymbol { get; set; }
        public string TraitSummary { get; set; }
        public string EquipmentSummary { get; set; }
        public string PersonalHook { get; set; }
        public bool CanAfford { get; set; }
        public bool IsAscensionMerge { get; set; }
        public int CurrentAscensionLevel { get; set; }
        public int NextAscensionLevel { get; set; }
        public string DuplicateMergeKind { get; set; }
        public string DuplicateMergePreview { get; set; }
    }

    public sealed class GuildCityContractView017D
    {
        public string ContractId { get; set; }
        public string BoardId { get; set; }
        public string DisplayName { get; set; }
        public string Sponsor { get; set; }
        public string Family { get; set; }
        public string Hook { get; set; }
        public string PrimaryObjective { get; set; }
        public IReadOnlyList<string> OptionalObjectives { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> Hazards { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> RecommendedSkills { get; set; } = Array.Empty<string>();
        public long GuildXp { get; set; }
        public long HallXp { get; set; }
        public string CityHook { get; set; }
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsFailed { get; set; }
    }

    public sealed class GuildCityExpeditionView017D
    {
        public string ExpeditionId { get; set; }
        public string BoardId { get; set; }
        public string CurrentNodeId { get; set; }
        public string CurrentNodeKind { get; set; }
        public string CurrentEventId { get; set; }
        public string CurrentEventTitle { get; set; }
        public string CurrentEventProblem { get; set; }
        public IReadOnlyList<string> CurrentEventEligibleSkills { get; set; } = Array.Empty<string>();
        public string CurrentEventConsequence { get; set; }
        public string CurrentEventOutcomeText { get; set; }
        public bool CurrentEventUsesCommitted2d6 { get; set; } = true;
        public string CurrentCheckSkillId { get; set; }
        public string CurrentEncounterId { get; set; }
        public IReadOnlyList<string> LinkedNodeIds { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> VisitedNodeIds { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> RevealedNodeIds { get; set; } = Array.Empty<string>();
        public string Status { get; set; }
        public int Supplies { get; set; }
        public int Fatigue { get; set; }
        public int Threat { get; set; }
        public int Urgency { get; set; }
        public IReadOnlyList<string> ObjectiveFlags { get; set; } = Array.Empty<string>();
        public bool RequiresResolution { get; set; }
        public bool ResolutionComplete { get; set; }
        public string ResolutionHint { get; set; }
        public bool CanMove { get; set; }
        public bool CanCommitEncounter { get; set; }
        public bool CanFinalizeOperation { get; set; }
        public bool HasCommittedCheckAtCurrentNode { get; set; }
        public string LastCheckActorRecruitId { get; set; }
        public string LastCheckAssistantRecruitId { get; set; }
        public int LastCheckDieOne { get; set; }
        public int LastCheckDieTwo { get; set; }
        public int LastCheckModifier { get; set; }
        public int LastCheckTotal { get; set; }
        public string LastCheckOutcome { get; set; }
    }

    /// <summary>
    /// Player-facing projection of one authoritative first-hour quest card.
    /// It deliberately exposes the exact saved reward, price and combat power
    /// while keeping node IDs and deck bookkeeping out of normal play.
    /// </summary>
    public sealed class GuildQuestCardView090
    {
        public string CardId { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string RewardPreview { get; set; }
        public string RiskLabel { get; set; }
        // Internal route authority used by input automation and controller
        // selection. The presenter never renders this save key to the player.
        public string DestinationNodeId { get; set; }
        public string DestinationLabel { get; set; }
        public string RoutePreview { get; set; }
        public string RarityId { get; set; }
        public string ItemName { get; set; }
        public string ItemVisualId { get; set; }
        public string ResultRewardCopy092 { get; set; }
        public int PhysicalPower { get; set; }
        public int MysticPower { get; set; }
        public int TreasuryXpDelta { get; set; }
        public int TreasuryXpCost { get; set; }
        public int DieOne { get; set; }
        public int DieTwo { get; set; }
        public int Target { get; set; }
        public int FateCheckModifier { get; set; }
        public string EncounterId { get; set; }
        public int EnemyUnionCount { get; set; }
        public bool IsPermanentHeroBoon { get; set; }
        public string HeroName { get; set; }
        public bool CanChoose { get; set; }
        public string LockedReason { get; set; }
        public string VisualResourcePath { get; set; }
    }

    public static class GuildQuestCardPresentation090
    {
        public static string VisualResourcePath090(string category)
        {
            string face;
            switch ((category ?? string.Empty).ToUpperInvariant())
            {
                case "CHEST": face = "CHEST"; break;
                case "MERCHANT": face = "CAMP"; break;
                case "RECRUIT":
                case "FREE_RECRUIT": face = "RECRUIT"; break;
                case "BOON": face = "BUFF"; break;
                case "SCAR": face = "HAZARD"; break;
                case "FATE": face = "CHANCE"; break;
                case "BATTLE": face = "BATTLE"; break;
                case "XP": face = "BUFF"; break;
                default: face = "STORY"; break;
            }
            return "SecondDimension/Art/Board086/CardFaces/CARD_FACE_" +
                   face + "_089";
        }
    }

    public sealed class GuildCityPresentationState017D
    {
        public bool IsAvailable { get; set; }
        public string Error { get; set; }
        public int OperationOrdinal { get; set; }
        public string CampaignModeId { get; set; }
        public string TutorialDepthId { get; set; }
        public int TextScalePercent { get; set; }
        public bool HighContrast { get; set; }
        public bool ReducedMotion { get; set; }
        public int CombatSpeed { get; set; }
        public string ForecastDetail { get; set; }
        public int TotalRecruitCount { get; set; }
        public int EquippedRecruitCount { get; set; }
        public int NormalUnionCount { get; set; }
        public int InventoryItemCount { get; set; }
        public int SignedApplicantCount { get; set; }
        public int PlacedBuildingCount { get; set; }
        public int StaffedBuildingCount { get; set; }
        public int UpgradedBuildingCount { get; set; }
        public int RelationshipCount { get; set; }
        public int UnviewedRelationshipCount { get; set; }
        public int ExpeditionVisitedNodeCount { get; set; }
        public int CommittedCheckCount { get; set; }
        public int ClaimedBattleRewardCount { get; set; }
        public bool HasUnclaimedBattleReward { get; set; }
        public int CharterBuildCredits { get; set; }
        public int CivicTrust { get; set; }
        public long HallEnhancementXp { get; set; }
        public int AdjacencyBonusCount { get; set; }
        public int ApplicantBoardBonusSlots { get; set; }
        public int UnionCapacityBonus { get; set; }
        public int StartingSupplyBonus { get; set; }
        public int ScoutInformationBonus { get; set; }
        public int RecoveryProgressPerOperation { get; set; }
        public int TrainingProgressPerOperation { get; set; }
        public int RosterCapacity { get; set; }
        public long TreasuryXp { get; set; }
        public IReadOnlyList<string> ActiveCityEffects { get; set; } = Array.Empty<string>();
        public bool HasRecruitmentBoard { get; set; }
        public bool CanInviteEarnedContacts124 { get; set; }
        public IReadOnlyList<string> PendingExpeditionRecruitLeadNames089 { get; set; } =
            Array.Empty<string>();
        public bool FirstContractUnionBriefingConfirmed080 { get; set; }
        public bool FirstFacilityPayoffAcknowledged080 { get; set; }
        public bool RecoveredLootEquipped080 { get; set; }
        public string RecruitmentBoardId { get; set; }
        public int RecruitmentRefreshOrdinal { get; set; }
        public int RecruitmentDryStreak { get; set; }
        public IReadOnlyList<GuildCityApplicantView017D> Applicants { get; set; } = Array.Empty<GuildCityApplicantView017D>();
        public IReadOnlyList<string> MaterialSummaries { get; set; } = Array.Empty<string>();
        public IReadOnlyList<GuildCityPlotView017D> Plots { get; set; } = Array.Empty<GuildCityPlotView017D>();
        public IReadOnlyList<GuildCityBuildingView017D> Buildings { get; set; } = Array.Empty<GuildCityBuildingView017D>();
        public IReadOnlyList<GuildCityAssignmentView017D> Assignments { get; set; } = Array.Empty<GuildCityAssignmentView017D>();
        public IReadOnlyList<GuildCityRelationshipView017D> Relationships { get; set; } = Array.Empty<GuildCityRelationshipView017D>();
        public IReadOnlyList<GuildCityContractView017D> Contracts { get; set; } = Array.Empty<GuildCityContractView017D>();
        public GuildCityExpeditionView017D Expedition { get; set; }
        public IReadOnlyList<GuildQuestCardView090> QuestCards090 { get; set; } =
            Array.Empty<GuildQuestCardView090>();
        public int QuestCardRound090 { get; set; }
        public int QuestCardMinimumRounds090 { get; set; }
        public bool HasActiveContract { get; set; }
        public bool HasPendingEncounter { get; set; }
        public bool HasPendingBattleReturn { get; set; }
        public bool IsCertifiedEncounterBattle { get; set; }
        public string LastCheckpointId { get; set; }
    }

    public interface IGuildCityPresentationCoordinator017D
    {
        GuildCityPresentationState017D GuildCity017D { get; }
        M1CommandResult PlaceGuildCityBuilding017D(string plotId, string buildingId);
        M1CommandResult UpgradeGuildCityBuilding017D(string plotId);
        M1CommandResult AssignGuildCityStaff017D(string plotId, string recruitId);
        M1CommandResult RecallGuildCityStaff017D(string recruitId);
        M1CommandResult SetGuildCityAssignment017D(string recruitId, string assignmentKind);
        M1CommandResult ArchiveGuildCityMember017D(string recruitId, bool confirmed);
        M1CommandResult CommitGuildCityApplicantBoard017D();
        M1CommandResult RefreshGuildCityApplicantBoard017D();
        M1CommandResult SignGuildCityApplicant017D(string recruitId);
        M1CommandResult DeclineGuildCityApplicant017D(string recruitId);
        M1CommandResult AcceptGuildCityContract017D(string contractId);
        M1CommandResult StartGuildCityExpedition017D();
        M1CommandResult MoveGuildCityExpedition017D(string destinationNodeId);
        M1CommandResult ResolveGuildCityCheck017D(string eventId, string actorRecruitId,
            string assistantRecruitId, int modifier);
        M1CommandResult DiscoverGateworksMaintenancePassage066();
        M1CommandResult CommitGuildCityEncounter017D(string encounterId);
        M1CommandResult StartCommittedGuildCityBattle017D();
        M1CommandResult FinalizeGuildCityOperation017D();
        M1CommandResult AddGuildCityRelationshipMemory017D(string firstRecruitId, string secondRecruitId,
            string sourceId, string summary, int strength, string sceneId);
        M1CommandResult ViewGuildCityRelationshipScene017D(string sceneId);
    }

    /// <summary>
    /// Optional production command seam for the new first-hour three-card row.
    /// Legacy tests and alternate coordinators can keep the original Guild City
    /// interface and automatically receive the single-card fallback.
    /// </summary>
    public interface IBoardQuestDeckCoordinator090
    {
        M1CommandResult CommitBoardQuestCard090(string cardId);
    }

    /// <summary>
    /// Optional first-hour command surface. Keeping this separate preserves the
    /// general Guild City presenter contract while allowing the live Release 071
    /// world objective to commit the patrol rescue as one saved transaction.
    /// </summary>
    public interface IFirstHourPatrolRescueCoordinator071
    {
        M1CommandResult RescueFirstHourLanternPatrol071();
    }

    /// <summary>
    /// Optional command seam for the two durable first-hour Hall acknowledgements.
    /// Keeping it narrow lets the presenter exercise the same saved progression
    /// path in production and Play Mode without depending on a concrete runtime.
    /// </summary>
    public interface IGuildHallGuidedProgressionCoordinator080
    {
        M1CommandResult ConfirmFirstContractUnionBriefing080();
        M1CommandResult AcknowledgeFirstFacilityPayoff080();
    }

    public sealed class GuildMemberDevelopmentView067
    {
        public string RecruitId { get; set; }
        public string DisplayName { get; set; }
        public string RaceId { get; set; }
        public string VisualSeed { get; set; }
        public string PortraitAuthorityId { get; set; }
        public string ClassSymbol { get; set; }
        public bool IsApplicant { get; set; }
        public int PotentialScore { get; set; }
        public string PotentialBand { get; set; }
        public int LeadershipScore { get; set; }
        public int TacticalAptitude { get; set; }
        public string StartingClassId { get; set; }
        public string FixedWeaponFamilyId { get; set; }
        public string WeaponTreeId { get; set; }
        public string MysticTreeId { get; set; }
        public string PrimaryRoleTreeId { get; set; }
        public string SecondaryRoleTreeId { get; set; }
        public string StartingAdvantage { get; set; }
        public int StartingNodeCount { get; set; }
        public int Level { get; set; }
        public long TotalPersonalXp { get; set; }
        public long XpIntoCurrentLevel { get; set; }
        public long XpRequiredForNextLevel { get; set; }
        public int LearnedArtCount { get; set; }
        public string NextArtId { get; set; }
        public int NextArtMeaningfulUses { get; set; }
        public int NextArtMasteryPoints { get; set; }
        public int ClassTrainingXp { get; set; }
        public int ArtPracticeXp { get; set; }
        public int ArtPracticeMasteryPoints { get; set; }
        public int TrainingCostTreasuryXp { get; set; }
        public bool CanClassTrain { get; set; }
        public bool CanPracticeArt { get; set; }
        public string TrainingUnavailableReason { get; set; }
        public IReadOnlyList<GuildMemberTreeSlotView070> TreeSlots070 { get; set; } =
            Array.Empty<GuildMemberTreeSlotView070>();
    }

    public sealed class GuildMemberTreeSlotView070
    {
        public string TreeId { get; set; } = string.Empty;
        public string SlotLabel { get; set; } = string.Empty;
        public string TreeDisplayName { get; set; } = string.Empty;
        public bool Active { get; set; }
        public bool Earnable { get; set; }
        public bool CanUnlock { get; set; }
        public bool PermanentlyFixed { get; set; }
        public int UnlockCostTreasuryXp { get; set; }
        public string UnlockUnavailableReason { get; set; } = string.Empty;
        public int LearnedArtCount { get; set; }
        public int TotalArtCount { get; set; }
        public int MeaningfulUses { get; set; }
        public int MasteryPoints { get; set; }
        public string NextUnlockName { get; set; } = string.Empty;
        public string ProgressSummary { get; set; } = string.Empty;
    }

    public static class GuildMemberDevelopmentBridge067
    {
        public static IReadOnlyList<GuildMemberDevelopmentView067> Members067(
            this IGuildCityPresentationCoordinator017D coordinator)
        {
            var runtime = coordinator as global::SecondDimension.Presentation.M1RuntimeCoordinator;
            return runtime == null
                ? Array.Empty<GuildMemberDevelopmentView067>()
                : runtime.BuildGuildMemberDevelopment067();
        }

        public static GuildMemberDevelopmentView067 Applicant067(
            this IGuildCityPresentationCoordinator017D coordinator,
            string recruitId)
        {
            var runtime = coordinator as global::SecondDimension.Presentation.M1RuntimeCoordinator;
            return runtime?.BuildGuildApplicantDevelopment067(recruitId);
        }

        public static M1CommandResult TrainMember067(
            this IGuildCityPresentationCoordinator017D coordinator,
            string recruitId,
            string focus)
        {
            var runtime = coordinator as global::SecondDimension.Presentation.M1RuntimeCoordinator;
            return runtime == null
                ? M1CommandResult.Failure("Member development is unavailable.")
                : runtime.TrainGuildMember067(recruitId, focus);
        }

        public static M1CommandResult UnlockTree070(
            this IGuildCityPresentationCoordinator017D coordinator,
            string recruitId,
            string treeId)
        {
            var runtime = coordinator as global::SecondDimension.Presentation.M1RuntimeCoordinator;
            return runtime == null
                ? M1CommandResult.Failure("Member tree training is unavailable.")
                : runtime.UnlockGuildMemberTree070(recruitId, treeId);
        }
    }
}

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator
    {
        public IReadOnlyList<GuildCity017D.GuildMemberDevelopmentView067>
            BuildGuildMemberDevelopment067()
        {
            if (_campaign?.Guild == null || _guildCityRecruitment == null)
                return Array.Empty<GuildCity017D.GuildMemberDevelopmentView067>();
            var result = new List<GuildCity017D.GuildMemberDevelopmentView067>();
            foreach (var recruit in _campaign.Guild.Recruits
                         .OrderBy(value => value.DisplayName, StringComparer.Ordinal)
                         .ThenBy(value => value.RecruitId, StringComparer.Ordinal))
            {
                var path = _guildCityRecruitment.DescribeRecruit067(recruit);
                result.Add(BuildGuildMemberDevelopmentView067(recruit, path));
            }
            return result.AsReadOnly();
        }

        public GuildCity017D.GuildMemberDevelopmentView067 BuildGuildApplicantDevelopment067(
            string recruitId)
        {
            if (_campaign?.Guild?.GuildCity?.RecruitmentBoard == null ||
                _guildCityRecruitment == null || string.IsNullOrWhiteSpace(recruitId)) return null;
            var applicant = _campaign.Guild.GuildCity.RecruitmentBoard.FindApplicant(recruitId);
            if (applicant == null) return null;
            var path = _guildCityRecruitment.DescribeApplicant067(applicant);
            return new GuildCity017D.GuildMemberDevelopmentView067
            {
                RecruitId = applicant.RecruitId,
                DisplayName = applicant.DisplayName,
                RaceId = applicant.RaceId,
                VisualSeed = ReadVisualSeed(applicant.CanonicalApplicantJson, applicant.RecruitId),
                PortraitAuthorityId = PortraitAuthority(
                    applicant.AuthoredStableRecruitId,
                    applicant.SignatureId,
                    applicant.RecruitId),
                ClassSymbol = ClassSymbol(applicant.ClassTendencyId),
                IsApplicant = true,
                PotentialScore = path.PotentialScore,
                PotentialBand = path.PotentialBand,
                LeadershipScore = path.LeadershipScore,
                TacticalAptitude = path.TacticalAptitude,
                StartingClassId = path.StartingClassId,
                FixedWeaponFamilyId = path.FixedWeaponFamilyId,
                WeaponTreeId = path.WeaponTreeId,
                MysticTreeId = path.MysticTreeId,
                PrimaryRoleTreeId = path.PrimaryRoleTreeId,
                SecondaryRoleTreeId = path.SecondaryRoleTreeId,
                StartingAdvantage = path.StartingAdvantage,
                StartingNodeCount = path.StartingStableNodeIds.Count,
                Level = 1,
                ClassTrainingXp = path.ClassTrainingXp,
                ArtPracticeXp = path.ArtPracticeXp,
                ArtPracticeMasteryPoints = path.ArtPracticeMasteryPoints,
                TrainingCostTreasuryXp = GuildCityRecruitmentService017D.MemberTrainingCost159(_campaign),
                TrainingUnavailableReason = "Recruit this applicant before training their permanent build.",
                TreeSlots070 = BuildGuildMemberTreeSlots070(
                    applicant.SignatureId,
                    applicant.AuthoredStableRecruitId,
                    RecruitProgressionState.Default(),
                    path)
            };
        }

        public M1CommandResult TrainGuildMember067(string recruitId, string focus)
        {
            if (_guildCityRecruitment == null)
                return M1CommandResult.Failure("Recruitment and member-development authority is unavailable.");
            var artPractice = StringComparer.Ordinal.Equals(
                (focus ?? string.Empty).Trim().ToUpperInvariant(),
                GuildCityRecruitmentService017D.ArtPracticeFocus067);
            return ApplyGuildCityAndPersist(
                _guildCityRecruitment.TrainGuildMember067(_campaign, recruitId, focus),
                artPractice
                    ? "Art practice saved. The meaningful use, mastery points, and personal XP are permanent."
                    : "Class training saved. Personal XP and any level-up stat growth are permanent.");
        }

        public M1CommandResult UnlockGuildMemberTree070(string recruitId, string treeId)
        {
            if (_guildCityRecruitment == null || _recruitTreeProgression070 == null)
                return M1CommandResult.Failure("Member tree training authority is unavailable.");
            return ApplyGuildCityAndPersist(
                _guildCityRecruitment.UnlockGuildMemberTree070(
                    _campaign,
                    recruitId,
                    treeId,
                    _recruitTreeProgression070),
                "New Art tree unlocked. Its first Art is permanent and available for battle growth.");
        }

        private GuildCity017D.GuildMemberDevelopmentView067 BuildGuildMemberDevelopmentView067(
            RecruitState recruit,
            GuildMemberPathProfile067 path)
        {
            var progression = recruit.Progression ?? RecruitProgressionState.Default();
            var nextArt = progression.ArtMastery
                .OrderBy(value => value.MasteryPoints)
                .ThenBy(value => value.MeaningfulUses)
                .ThenBy(value => value.ArtId, StringComparer.Ordinal)
                .FirstOrDefault();
            var canTrain = CanTrainGuildMember067(recruit.RecruitId, out var unavailableReason);
            return new GuildCity017D.GuildMemberDevelopmentView067
            {
                RecruitId = recruit.RecruitId,
                DisplayName = recruit.DisplayName,
                RaceId = recruit.RaceId,
                VisualSeed = ReadVisualSeed(recruit.CanonicalApplicantJson, recruit.RecruitId),
                PortraitAuthorityId = PortraitAuthority(
                    recruit.AuthoredStableRecruitId,
                    recruit.SignatureId,
                    recruit.RecruitId),
                ClassSymbol = ClassSymbol(recruit.ClassTendencyId),
                PotentialScore = path.PotentialScore,
                PotentialBand = path.PotentialBand,
                LeadershipScore = path.LeadershipScore,
                TacticalAptitude = path.TacticalAptitude,
                StartingClassId = path.StartingClassId,
                FixedWeaponFamilyId = path.FixedWeaponFamilyId,
                WeaponTreeId = path.WeaponTreeId,
                MysticTreeId = path.MysticTreeId,
                PrimaryRoleTreeId = path.PrimaryRoleTreeId,
                SecondaryRoleTreeId = path.SecondaryRoleTreeId,
                StartingAdvantage = path.StartingAdvantage,
                StartingNodeCount = path.StartingStableNodeIds.Count,
                Level = progression.Level,
                TotalPersonalXp = progression.TotalPersonalXp,
                XpIntoCurrentLevel = progression.XpIntoCurrentLevel,
                XpRequiredForNextLevel = progression.XpRequiredForNextLevel,
                LearnedArtCount = progression.LearnedArtIds.Count,
                NextArtId = nextArt?.ArtId ?? string.Empty,
                NextArtMeaningfulUses = nextArt?.MeaningfulUses ?? 0,
                NextArtMasteryPoints = nextArt?.MasteryPoints ?? 0,
                ClassTrainingXp = path.ClassTrainingXp,
                ArtPracticeXp = path.ArtPracticeXp,
                ArtPracticeMasteryPoints = path.ArtPracticeMasteryPoints,
                TrainingCostTreasuryXp = GuildCityRecruitmentService017D.MemberTrainingCost159(_campaign),
                CanClassTrain = canTrain,
                CanPracticeArt = canTrain && nextArt != null,
                TrainingUnavailableReason = canTrain
                    ? nextArt == null
                        ? "This legacy member has no initialized Art to practice yet."
                        : string.Empty
                    : unavailableReason,
                TreeSlots070 = BuildGuildMemberTreeSlots070(
                    recruit.SignatureId,
                    recruit.AuthoredStableRecruitId,
                    progression,
                    path,
                    canTrain,
                    unavailableReason)
            };
        }

        public IReadOnlyList<GuildCity017D.GuildMemberTreeSlotView070>
            DescribeSignatureTreeSlotsForVerification070(string signatureOrStableRecruitId)
        {
            if (_deepProgressionCatalog070 == null || _recruitTreeProgression070 == null)
                return Array.Empty<GuildCity017D.GuildMemberTreeSlotView070>();
            return BuildDeepTreeSlots070(
                signatureOrStableRecruitId,
                RecruitProgressionState.Default());
        }

        private IReadOnlyList<GuildCity017D.GuildMemberTreeSlotView070>
            BuildGuildMemberTreeSlots070(
                string signatureId,
                string stableRecruitId,
                RecruitProgressionState progression,
                GuildMemberPathProfile067 fallbackPath,
                bool canUnlock = false,
                string unlockUnavailableReason = "Recruit this adventurer before unlocking a path.")
        {
            var authorityId = !string.IsNullOrWhiteSpace(signatureId)
                ? signatureId
                : stableRecruitId;
            if (_deepProgressionCatalog070 != null && _recruitTreeProgression070 != null &&
                !string.IsNullOrWhiteSpace(authorityId))
            {
                try
                {
                    return BuildDeepTreeSlots070(
                        authorityId,
                        progression,
                        canUnlock,
                        unlockUnavailableReason);
                }
                catch (KeyNotFoundException)
                {
                    // Procedural applicants use the same 30-tree catalog but are not
                    // members of the fixed 300 Signature Recruit plan index.
                }
            }
            return BuildProceduralTreeSlots070(
                progression,
                fallbackPath,
                canUnlock,
                unlockUnavailableReason);
        }

        private IReadOnlyList<GuildCity017D.GuildMemberTreeSlotView070> BuildDeepTreeSlots070(
            string signatureOrStableRecruitId,
            RecruitProgressionState progression,
            bool canUnlock = false,
            string unlockUnavailableReason = "Member training is unavailable.")
        {
            var snapshot = _recruitTreeProgression070.Describe(
                signatureOrStableRecruitId,
                progression ?? RecruitProgressionState.Default());
            var result = new List<GuildCity017D.GuildMemberTreeSlotView070>();
            foreach (var slot in snapshot.Slots)
            {
                var tree = _deepProgressionCatalog070.Tree(slot.TreeId);
                var arts = snapshot.Arts.Where(value =>
                    StringComparer.Ordinal.Equals(value.TreeId, slot.TreeId)).ToArray();
                var learnedIds = new HashSet<string>(
                    arts.Where(value => value.Learned).Select(value => value.ArtId),
                    StringComparer.Ordinal);
                var nextNodeId = tree.NodeIds.FirstOrDefault(value => !learnedIds.Contains(value));
                var nextName = string.IsNullOrWhiteSpace(nextNodeId)
                    ? "Tree mastered"
                    : _deepProgressionCatalog070.Node(nextNodeId).DisplayName;
                var learnedCount = learnedIds.Count;
                var mastery = arts.Sum(value => value.MasteryPoints);
                var uses = arts.Sum(value => value.MeaningfulUses);
                result.Add(new GuildCity017D.GuildMemberTreeSlotView070
                {
                    TreeId = tree.TreeId,
                    SlotLabel = PlayerFacingSlotLabel070(slot.SlotKind, slot.Unlocked),
                    TreeDisplayName = tree.DisplayName,
                    Active = slot.Unlocked,
                    Earnable = slot.Earnable,
                    CanUnlock = canUnlock && slot.Earnable && !slot.Unlocked,
                    PermanentlyFixed = slot.PermanentlyFixed,
                    UnlockCostTreasuryXp = GuildCityRecruitmentService017D.MemberTreeUnlockCostTreasuryXp070,
                    UnlockUnavailableReason = canUnlock || slot.Unlocked || !slot.Earnable
                        ? string.Empty
                        : unlockUnavailableReason,
                    LearnedArtCount = learnedCount,
                    TotalArtCount = tree.NodeIds.Count,
                    MeaningfulUses = uses,
                    MasteryPoints = mastery,
                    NextUnlockName = nextName,
                    ProgressSummary = slot.Unlocked
                        ? learnedCount + "/" + tree.NodeIds.Count + " Arts learned • " +
                          uses + " meaningful uses • " + mastery + " mastery"
                        : "Earn through Guild training • first Art: " + nextName
                });
            }
            return result.AsReadOnly();
        }

        private IReadOnlyList<GuildCity017D.GuildMemberTreeSlotView070> BuildProceduralTreeSlots070(
            RecruitProgressionState progression,
            GuildMemberPathProfile067 path,
            bool canUnlock = false,
            string unlockUnavailableReason = "Member training is unavailable.")
        {
            if (path == null) return Array.Empty<GuildCity017D.GuildMemberTreeSlotView070>();
            var definitions = new[]
            {
                new { Kind = RecruitTreeSlotKind070.Weapon, TreeId = path.WeaponTreeId,
                    Active = true, Earnable = false, Fixed = true },
                new { Kind = RecruitTreeSlotKind070.PrimaryRole, TreeId = path.PrimaryRoleTreeId,
                    Active = true, Earnable = false, Fixed = false },
                new { Kind = RecruitTreeSlotKind070.Mystic, TreeId = path.MysticTreeId,
                    Active = false, Earnable = true, Fixed = false },
                new { Kind = RecruitTreeSlotKind070.SecondaryRole, TreeId = path.SecondaryRoleTreeId,
                    Active = false, Earnable = true, Fixed = false }
            };
            var learned = new HashSet<string>(
                (progression ?? RecruitProgressionState.Default()).LearnedArtIds,
                StringComparer.Ordinal);
            foreach (var nodeId in path.StartingStableNodeIds ?? Array.Empty<string>()) learned.Add(nodeId);
            var masteryRows = (progression ?? RecruitProgressionState.Default()).ArtMastery;
            var result = new List<GuildCity017D.GuildMemberTreeSlotView070>();
            foreach (var definition in definitions)
            {
                DeepTreeDefinition070 tree = null;
                if (_deepProgressionCatalog070 != null && !string.IsNullOrWhiteSpace(definition.TreeId))
                {
                    try { tree = _deepProgressionCatalog070.Tree(definition.TreeId); }
                    catch (KeyNotFoundException) { }
                }
                var treeNodeIds = tree?.NodeIds ?? Array.Empty<string>();
                var learnedCount = treeNodeIds.Count(learned.Contains);
                var nextNodeId = treeNodeIds.FirstOrDefault(value => !learned.Contains(value));
                var nextName = string.IsNullOrWhiteSpace(nextNodeId)
                    ? treeNodeIds.Count == 0 ? "Revealed after recruitment" : "Tree mastered"
                    : _deepProgressionCatalog070.Node(nextNodeId).DisplayName;
                var mastery = masteryRows.Where(value => treeNodeIds.Contains(value.ArtId))
                    .Sum(value => value.MasteryPoints);
                var uses = masteryRows.Where(value => treeNodeIds.Contains(value.ArtId))
                    .Sum(value => value.MeaningfulUses);
                var displayName = tree?.DisplayName;
                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = FriendlyTreeName070(definition.TreeId);
                result.Add(new GuildCity017D.GuildMemberTreeSlotView070
                {
                    TreeId = definition.TreeId ?? string.Empty,
                    SlotLabel = PlayerFacingSlotLabel070(definition.Kind, definition.Active),
                    TreeDisplayName = displayName,
                    Active = definition.Active,
                    Earnable = definition.Earnable,
                    CanUnlock = false,
                    PermanentlyFixed = definition.Fixed,
                    UnlockCostTreasuryXp = GuildCityRecruitmentService017D.MemberTreeUnlockCostTreasuryXp070,
                    UnlockUnavailableReason = definition.Earnable && !definition.Active
                        ? string.IsNullOrWhiteSpace(unlockUnavailableReason)
                            ? "This generated recruit's extra path needs a future training plan."
                            : unlockUnavailableReason
                        : string.Empty,
                    LearnedArtCount = learnedCount,
                    TotalArtCount = treeNodeIds.Count,
                    MeaningfulUses = uses,
                    MasteryPoints = mastery,
                    NextUnlockName = nextName,
                    ProgressSummary = definition.Active
                        ? learnedCount + "/" + Math.Max(1, treeNodeIds.Count) +
                          " Arts learned • " + uses + " meaningful uses • " + mastery + " mastery"
                        : "Earn through Guild training • first Art: " + nextName
                });
            }
            return result.AsReadOnly();
        }

        private static string PlayerFacingSlotLabel070(RecruitTreeSlotKind070 kind, bool active)
        {
            switch (kind)
            {
                case RecruitTreeSlotKind070.Weapon: return "ACTIVE • WEAPON DISCIPLINE";
                case RecruitTreeSlotKind070.PrimaryRole: return "ACTIVE • NATIVE CALLING";
                case RecruitTreeSlotKind070.Mystic:
                    return active ? "ACTIVE • CROSS-TRAINING" : "LOCKED • CROSS-TRAINING";
                case RecruitTreeSlotKind070.SecondaryRole:
                    return active ? "ACTIVE • LEGACY DISCIPLINE" : "LOCKED • LEGACY DISCIPLINE";
                default: return "MEMBER PATH";
            }
        }

        private static string FriendlyTreeName070(string treeId)
        {
            if (string.IsNullOrWhiteSpace(treeId)) return "Path revealed after recruitment";
            var value = treeId;
            foreach (var prefix in new[]
                     {
                         "TREE_CA002_WPN_", "TREE_CA002_ROLE_", "TREE_CA002_MYS_",
                         "TREE_CA002_MYSTIC_", "TREE_"
                     })
                if (value.StartsWith(prefix, StringComparison.Ordinal))
                {
                    value = value.Substring(prefix.Length);
                    break;
                }
            return string.Join(" ", value.Split('_').Select(word =>
                string.IsNullOrWhiteSpace(word)
                    ? string.Empty
                    : char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant()));
        }

        private bool CanTrainGuildMember067(string recruitId, out string reason)
        {
            reason = string.Empty;
            if (_campaign?.Guild == null)
            {
                reason = "Create or resume a Guild first.";
                return false;
            }
            var developmentBlocked159 = IndependentProgression159.BlockReason(_campaign);
            if (developmentBlocked159 != null)
            {
                reason = developmentBlocked159;
                return false;
            }
            var assignment = _campaign.Guild.GuildCity.MemberAssignments.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.RecruitId, recruitId));
            if (assignment == null ||
                (assignment.Kind != GuildMemberAssignmentKind017D.Active &&
                 assignment.Kind != GuildMemberAssignmentKind017D.Reserve &&
                 assignment.Kind != GuildMemberAssignmentKind017D.Training))
            {
                reason = "Set this member to Active or Reserve before training.";
                return false;
            }
            var trainingCost159 = GuildCityRecruitmentService017D.MemberTrainingCost159(_campaign);
            if (_campaign.Guild.TreasuryXp < trainingCost159)
            {
                reason = "Need " + trainingCost159 + " Treasury XP for this training session.";
                return false;
            }
            return true;
        }
    }
}
