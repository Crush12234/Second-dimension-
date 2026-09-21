using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Progression070;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    public enum HeroMasterDuplicateMergeKind089
    {
        None,
        Ascension,
        TreeUnlocked,
        ArtLeveled,
        VeteranTraining
    }

    /// <summary>
    /// Deterministic preview produced by the same authority that commits a
    /// duplicate Hero Master. Presentation never guesses the result.
    /// </summary>
    public sealed class HeroMasterDuplicateMergeForecast089
    {
        internal HeroMasterDuplicateMergeForecast089(
            HeroMasterDuplicateMergeKind089 kind,
            string ownedRecruitId,
            int ownedRecruitIndex,
            int previousAscensionLevel,
            int ascensionLevel,
            int maximumHpGain,
            int maximumMpGain,
            int coreStatGain,
            string treeId,
            string artId,
            int previousArtLevel,
            int artLevel,
            long personalXpGain,
            string summary,
            RecruitProgressionState projectedProgression)
        {
            Kind = kind;
            OwnedRecruitId = ownedRecruitId ?? string.Empty;
            OwnedRecruitIndex = ownedRecruitIndex;
            PreviousAscensionLevel = previousAscensionLevel;
            AscensionLevel = ascensionLevel;
            MaximumHpGain = maximumHpGain;
            MaximumMpGain = maximumMpGain;
            CoreStatGain = coreStatGain;
            TreeId = treeId ?? string.Empty;
            ArtId = artId ?? string.Empty;
            PreviousArtLevel = previousArtLevel;
            ArtLevel = artLevel;
            PersonalXpGain = personalXpGain;
            Summary = summary ?? string.Empty;
            ProjectedProgression = projectedProgression;
        }

        public HeroMasterDuplicateMergeKind089 Kind { get; }
        public bool IsDuplicateOffer => Kind != HeroMasterDuplicateMergeKind089.None;
        public string OwnedRecruitId { get; }
        internal int OwnedRecruitIndex { get; }
        public int PreviousAscensionLevel { get; }
        public int AscensionLevel { get; }
        public int MaximumHpGain { get; }
        public int MaximumMpGain { get; }
        public int CoreStatGain { get; }
        public string TreeId { get; }
        public string ArtId { get; }
        public int PreviousArtLevel { get; }
        public int ArtLevel { get; }
        public long PersonalXpGain { get; }
        public string Summary { get; }
        internal RecruitProgressionState ProjectedProgression { get; }

        internal static HeroMasterDuplicateMergeForecast089 None() =>
            new HeroMasterDuplicateMergeForecast089(
                HeroMasterDuplicateMergeKind089.None,
                string.Empty,
                -1,
                0,
                0,
                0,
                0,
                0,
                string.Empty,
                string.Empty,
                0,
                0,
                0,
                string.Empty,
                null);
    }

    /// <summary>
    /// Pure preflight from the same service that owns applicant signing. Quest
    /// cards and other presentation surfaces can therefore expose every known
    /// lock without copying roster, protection, equipment, or Treasury rules.
    /// </summary>
    public sealed class GuildCityApplicantSigningForecast090
    {
        internal GuildCityApplicantSigningForecast090(
            bool canSign,
            int effectiveCostTreasuryXp,
            bool isDuplicateOffer,
            string failureCode,
            string lockedReason)
        {
            CanSign = canSign;
            EffectiveCostTreasuryXp = Math.Max(0, effectiveCostTreasuryXp);
            IsDuplicateOffer = isDuplicateOffer;
            FailureCode = failureCode ?? string.Empty;
            LockedReason = lockedReason ?? string.Empty;
        }

        public bool CanSign { get; }
        public int EffectiveCostTreasuryXp { get; }
        public bool IsDuplicateOffer { get; }
        public string FailureCode { get; }
        public string LockedReason { get; }
    }

    /// <summary>
    /// Player-facing projection of the permanent build that Auto-Generation 010
    /// already assigns to an applicant. Potential is an earned roster-planning
    /// difference, never a pull rarity: every recruit remains permanent and viable.
    /// </summary>
    public sealed class GuildMemberPathProfile067
    {
        public string RecruitId { get; internal set; }
        public int PotentialScore { get; internal set; }
        public string PotentialBand { get; internal set; }
        public int ClassTrainingXp { get; internal set; }
        public int ArtPracticeXp { get; internal set; }
        public int ArtPracticeMasteryPoints { get; internal set; }
        public int LeadershipScore { get; internal set; }
        public int TacticalAptitude { get; internal set; }
        public string StartingClassId { get; internal set; }
        public string FixedWeaponFamilyId { get; internal set; }
        public string WeaponTreeId { get; internal set; }
        public string MysticTreeId { get; internal set; }
        public string PrimaryRoleTreeId { get; internal set; }
        public string SecondaryRoleTreeId { get; internal set; }
        public IReadOnlyList<string> StartingStableNodeIds { get; internal set; } = Array.Empty<string>();
        public string StartingAdvantage { get; internal set; }
    }

    /// <summary>
    /// Commits recurring Applicant Boards into the permanent Guild/city save and signs
    /// applicants without reopening the one-time M1 tutorial flow. Boards are never
    /// regenerated by navigation. Signing is exact-once, uses the one-time founding
    /// charter invitation before normal Treasury XP costs, preserves manual equipment
    /// ownership, and initializes Auto-Generation 010 once.
    /// </summary>
    public sealed partial class GuildCityRecruitmentService017D
    {
        public const int MemberTrainingCostTreasuryXp067 = 50;
        public const int MemberTreeUnlockCostTreasuryXp070 = MemberTrainingCostTreasuryXp067;
        public const string ClassTrainingFocus067 = "CLASS";
        public const string ArtPracticeFocus067 = "ART";
        public const int MaximumApplicantOffers089 = 10;

        private static readonly IReadOnlyList<string> OpeningWorlds =
            Array.AsReadOnly(new[] { "WORLD_GATE_01" });
        private static readonly IReadOnlyList<string> OpeningRaces =
            Array.AsReadOnly(new[] { "HUMAN", "ORC", "GOBLIN", "DOG_TRIBE", "DARK_ELF", "DEMON_HERITAGE" });

        private readonly RecruitAutoGenerationSigningService010 _autoGeneration;
        private readonly RecruitAutoGenerator010 _generator;
        private readonly HeroMaster300Catalog087 _heroMasterCatalog089;
        private readonly RecruitTreeProgressionService070 _recruitTreeProgression089;
        private readonly HeroMaster300DeepProgressionAdapter089
            _heroMasterDeepProgression089;
        private readonly GuildCityEffectService017D _effects = new GuildCityEffectService017D();

        public GuildCityRecruitmentService017D(
            RecruitAutoGenerator010 generator,
            HeroMaster300Catalog087 heroMasterCatalog089 = null,
            RecruitTreeProgressionService070 recruitTreeProgression089 = null)
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _autoGeneration = new RecruitAutoGenerationSigningService010(
                new M1CommandService(), _generator);
            _heroMasterCatalog089 = heroMasterCatalog089;
            _recruitTreeProgression089 = recruitTreeProgression089;
            if (_heroMasterCatalog089 != null &&
                _recruitTreeProgression089 != null)
                _heroMasterDeepProgression089 =
                    new HeroMaster300DeepProgressionAdapter089(
                        _heroMasterCatalog089,
                        _generator,
                        _recruitTreeProgression089);
        }

        public GuildMemberPathProfile067 DescribeApplicant067(ApplicantSnapshotState applicant)
        {
            if (applicant == null) throw new ArgumentNullException(nameof(applicant));
            var recruit = new RecruitState(
                applicant.RecruitId,
                applicant.CurrentHp,
                applicant.MaximumHp,
                applicant.CurrentMp,
                applicant.MaximumMp,
                applicant.DisplayName,
                applicant.Kind == ApplicantKind.Signature
                    ? RecruitOriginKind.Signature
                    : RecruitOriginKind.Procedural,
                applicant.SignatureId,
                applicant.RaceId,
                applicant.WorldId,
                applicant.ClassTendencyId,
                applicant.LeadershipBand,
                applicant.PotentialBasisPoints,
                RecruitAuthorityKind.Normal,
                applicant.CanonicalApplicantJson,
                applicant.CanonicalScoutingReportJson,
                applicant.OpeningLoadout,
                applicant.VitalsInitialized,
                applicant.TutorialAliasId,
                applicant.AuthoredStableRecruitId,
                applicant.LeadershipScore,
                applicant.TacticalAptitude);
            return BuildPathProfile067(recruit);
        }

        public GuildMemberPathProfile067 DescribeRecruit067(RecruitState recruit)
        {
            if (recruit == null) throw new ArgumentNullException(nameof(recruit));
            return BuildPathProfile067(recruit);
        }

        public static int MemberTrainingCost159(CampaignState campaign) => checked((int)
            TownProgression159.Discount(campaign, 3, MemberTrainingCostTreasuryXp067));

        public Result<CampaignState> TrainGuildMember067(
            CampaignState campaign,
            string recruitId,
            string focus)
        {
            if (campaign == null) return Result<CampaignState>.Failure("GC017D_CAMPAIGN_REQUIRED");
            if (string.IsNullOrWhiteSpace(recruitId))
                return Result<CampaignState>.Failure("GC017D_RECRUIT_REQUIRED");
            var normalizedFocus = (focus ?? string.Empty).Trim().ToUpperInvariant();
            if (!StringComparer.Ordinal.Equals(normalizedFocus, ClassTrainingFocus067) &&
                !StringComparer.Ordinal.Equals(normalizedFocus, ArtPracticeFocus067))
                return Result<CampaignState>.Failure("GC017D_TRAINING_FOCUS_UNKNOWN");
            var developmentBlocked159 = IndependentProgression159.BlockReason(campaign);
            if (developmentBlocked159 != null)
                return Result<CampaignState>.Failure(developmentBlocked159);
            var trainingCost159 = MemberTrainingCost159(campaign);
            if (campaign.Guild.TreasuryXp < trainingCost159)
                return Result<CampaignState>.Failure("GC017D_INSUFFICIENT_TREASURY_FOR_MEMBER_TRAINING");

            var recruitIndex = FindRecruitIndex(campaign.Guild.Recruits, recruitId);
            if (recruitIndex < 0) return Result<CampaignState>.Failure("GC017D_RECRUIT_NOT_OWNED");
            var city = campaign.Guild.GuildCity;
            var assignmentIndex = FindAssignmentIndex067(city.MemberAssignments, recruitId);
            if (assignmentIndex < 0)
                return Result<CampaignState>.Failure("GC017D_MEMBER_ASSIGNMENT_REQUIRED");
            var assignment = city.MemberAssignments[assignmentIndex];
            if (!CanTrainAssignment067(assignment.Kind))
                return Result<CampaignState>.Failure("GC017D_MEMBER_UNAVAILABLE_FOR_TRAINING");

            var recruits = new List<RecruitState>(campaign.Guild.Recruits);
            var recruit = recruits[recruitIndex];
            var progression = recruit.Progression ?? RecruitProgressionState.Default();
            var classTrainingXp = TrainingPersonalXpForPotential067(recruit.PotentialBasisPoints);
            if (StringComparer.Ordinal.Equals(normalizedFocus, ClassTrainingFocus067))
            {
                progression = progression.GainPersonalXp(classTrainingXp, recruit.ClassTendencyId);
            }
            else
            {
                if (progression.ArtMastery.Count == 0)
                    return Result<CampaignState>.Failure("GC017D_MEMBER_HAS_NO_ART_TO_PRACTICE");
                var mastery = new List<RecruitArtMasteryState>(progression.ArtMastery);
                var masteryIndex = LowestMasteryIndex067(mastery);
                var practiced = mastery[masteryIndex];
                mastery[masteryIndex] = new RecruitArtMasteryState(
                    practiced.ArtId,
                    practiced.Discipline,
                    checked(practiced.MeaningfulUses + 1),
                    checked(practiced.MasteryPoints + ArtPracticeMasteryPointsForPotential067(
                        recruit.PotentialBasisPoints)));
                progression = progression.WithArts(progression.LearnedArtIds, mastery.AsReadOnly())
                    .GainPersonalXp(ArtPracticePersonalXpForPotential067(recruit.PotentialBasisPoints),
                        recruit.ClassTendencyId);
            }
            recruits[recruitIndex] = recruit.WithProgression(progression);

            var assignments = new List<GuildMemberAssignmentState017D>(city.MemberAssignments);
            assignments[assignmentIndex] = assignment.With(
                trainingProgress: checked(assignment.TrainingProgress + 100));
            var updatedCity = city.With(
                memberAssignments: assignments.AsReadOnly(),
                lastCheckpointId: StringComparer.Ordinal.Equals(normalizedFocus, ClassTrainingFocus067)
                    ? "guild_member_class_training_067"
                    : "guild_member_art_practice_067");
            var guild = campaign.Guild.With(
                    campaign.Guild.TreasuryXp - trainingCost159,
                    recruits.AsReadOnly(),
                    campaign.Guild.Unions,
                    campaign.Guild.Inventory)
                .WithGuildCity(updatedCity);
            return Result<CampaignState>.Success(campaign.With(guild, campaign.OpeningFlow));
        }

        public Result<CampaignState> UnlockGuildMemberTree070(
            CampaignState campaign,
            string recruitId,
            string treeId,
            RecruitTreeProgressionService070 progressionService)
        {
            if (campaign == null) return Result<CampaignState>.Failure("GC017D_CAMPAIGN_REQUIRED");
            if (string.IsNullOrWhiteSpace(recruitId))
                return Result<CampaignState>.Failure("GC017D_RECRUIT_REQUIRED");
            if (string.IsNullOrWhiteSpace(treeId))
                return Result<CampaignState>.Failure("GC017D_MEMBER_TREE_REQUIRED");
            if (progressionService == null)
                return Result<CampaignState>.Failure("GC017D_MEMBER_TREE_AUTHORITY_UNAVAILABLE");
            var developmentBlocked159 = IndependentProgression159.BlockReason(campaign);
            if (developmentBlocked159 != null)
                return Result<CampaignState>.Failure(developmentBlocked159);

            var recruitIndex = FindRecruitIndex(campaign.Guild.Recruits, recruitId);
            if (recruitIndex < 0) return Result<CampaignState>.Failure("GC017D_RECRUIT_NOT_OWNED");
            var city = campaign.Guild.GuildCity;
            var assignmentIndex = FindAssignmentIndex067(city.MemberAssignments, recruitId);
            if (assignmentIndex < 0)
                return Result<CampaignState>.Failure("GC017D_MEMBER_ASSIGNMENT_REQUIRED");
            var assignment = city.MemberAssignments[assignmentIndex];
            if (!CanTrainAssignment067(assignment.Kind))
                return Result<CampaignState>.Failure("GC017D_MEMBER_UNAVAILABLE_FOR_TRAINING");

            var recruit = campaign.Guild.Recruits[recruitIndex];
            var authorityId = !string.IsNullOrWhiteSpace(recruit.SignatureId)
                ? recruit.SignatureId
                : recruit.AuthoredStableRecruitId;
            if (string.IsNullOrWhiteSpace(authorityId))
                return Result<CampaignState>.Failure("GC017D_MEMBER_TREE_PLAN_REQUIRED");

            try
            {
                var progression = recruit.Progression ?? RecruitProgressionState.Default();
                var snapshot = progressionService.Describe(authorityId, progression);
                var slot = snapshot.Slots.FirstOrDefault(value =>
                    StringComparer.Ordinal.Equals(value.TreeId, treeId));
                if (slot == null)
                    return Result<CampaignState>.Failure("GC017D_MEMBER_TREE_NOT_ASSIGNED");
                if (!slot.Earnable)
                    return Result<CampaignState>.Failure("GC017D_MEMBER_TREE_NOT_EARNABLE");
                if (slot.Unlocked)
                    return Result<CampaignState>.Failure("GC017D_MEMBER_TREE_ALREADY_UNLOCKED");
                if (campaign.Guild.TreasuryXp < MemberTreeUnlockCostTreasuryXp070)
                    return Result<CampaignState>.Failure("GC017D_INSUFFICIENT_TREASURY_FOR_MEMBER_TREE");

                var recruits = new List<RecruitState>(campaign.Guild.Recruits);
                recruits[recruitIndex] = progressionService.UnlockEarnableTree(recruit, treeId);
                var assignments = new List<GuildMemberAssignmentState017D>(city.MemberAssignments);
                assignments[assignmentIndex] = assignment.With(
                    trainingProgress: checked(assignment.TrainingProgress + 100));
                var updatedCity = city.With(
                    memberAssignments: assignments.AsReadOnly(),
                    lastCheckpointId: "guild_member_tree_unlocked_070");
                var guild = campaign.Guild.With(
                        campaign.Guild.TreasuryXp - MemberTreeUnlockCostTreasuryXp070,
                        recruits.AsReadOnly(),
                        campaign.Guild.Unions,
                        campaign.Guild.Inventory)
                    .WithGuildCity(updatedCity);
                return Result<CampaignState>.Success(campaign.With(guild, campaign.OpeningFlow));
            }
            catch (KeyNotFoundException)
            {
                return Result<CampaignState>.Failure("GC017D_MEMBER_TREE_PLAN_REQUIRED");
            }
            catch (InvalidOperationException exception)
            {
                return Result<CampaignState>.Failure(
                    "GC017D_MEMBER_TREE_UNLOCK_REJECTED: " + exception.Message);
            }
        }

        public static string PotentialBandForScore067(int potentialScore)
        {
            if (potentialScore >= 735) return "EXCEPTIONAL";
            if (potentialScore >= 690) return "HIGH POTENTIAL";
            if (potentialScore >= 630) return "PROMISING";
            return "STEADY";
        }

        public static int TrainingPersonalXpForPotential067(int potentialScore)
        {
            if (potentialScore >= 735) return 120;
            if (potentialScore >= 690) return 100;
            if (potentialScore >= 630) return 85;
            return 70;
        }

        public static int ArtPracticePersonalXpForPotential067(int potentialScore) =>
            Math.Max(25, TrainingPersonalXpForPotential067(potentialScore) / 2);

        public static int ArtPracticeMasteryPointsForPotential067(int potentialScore)
        {
            if (potentialScore >= 735) return 14;
            if (potentialScore >= 690) return 12;
            if (potentialScore >= 630) return 10;
            return 8;
        }

        private GuildMemberPathProfile067 BuildPathProfile067(RecruitState recruit)
        {
            // SSS acquisition stores its own authority envelope, not an
            // OpeningRecruitRecord. Resolve the existing exact-identity catalog
            // before asking the ordinary generator to read startingClassId.
            // Missing/malformed ordinary applicant data still fails validation.
            SssTenV4Roster090.TryGetRecruit(recruit, out var sssHero);
            GeneratedRecruitProfile010 generated = null;
            if (sssHero == null && !string.IsNullOrWhiteSpace(recruit.CanonicalApplicantJson))
                generated = _generator.Generate(recruit);
            var sssStartingArts = sssHero == null ? null :
                SssTenV4Roster090.MaterializeGrant(sssHero.HeroId)
                    .Recruit.Progression.LearnedArtIds;
            var potential = recruit.PotentialBasisPoints;
            return new GuildMemberPathProfile067
            {
                RecruitId = recruit.RecruitId,
                PotentialScore = potential,
                PotentialBand = PotentialBandForScore067(potential),
                ClassTrainingXp = TrainingPersonalXpForPotential067(potential),
                ArtPracticeXp = ArtPracticePersonalXpForPotential067(potential),
                ArtPracticeMasteryPoints = ArtPracticeMasteryPointsForPotential067(potential),
                LeadershipScore = recruit.LeadershipScore,
                TacticalAptitude = recruit.TacticalAptitude,
                StartingClassId = sssHero?.ClassId ?? generated?.StartingClassId ?? recruit.ClassTendencyId,
                FixedWeaponFamilyId = sssHero?.WeaponFamilyId ?? generated?.FixedWeaponFamilyId ?? string.Empty,
                // SSS Arts use their own registry; assigning generated tree IDs
                // here would advertise training paths they do not own.
                WeaponTreeId = generated?.WeaponTreeId ?? string.Empty,
                MysticTreeId = generated?.MysticTreeId ?? string.Empty,
                PrimaryRoleTreeId = generated?.PrimaryRoleTreeId ?? string.Empty,
                SecondaryRoleTreeId = generated?.SecondaryRoleTreeId ?? string.Empty,
                StartingStableNodeIds = sssStartingArts ?? generated?.StartingStableNodeIds ?? Array.Empty<string>(),
                StartingAdvantage = sssHero != null
                    ? sssHero.Role + " — " + sssHero.PrimaryDiscipline + " / " +
                      sssHero.SecondaryDiscipline + " signature Arts"
                    : potential >= 735
                    ? "Extra weapon technique and secondary-role foundation"
                    : potential >= 690
                        ? "Extra starting weapon technique"
                        : "Focused foundation with the full long-term path available"
            };
        }

        private static bool CanTrainAssignment067(GuildMemberAssignmentKind017D kind) =>
            kind == GuildMemberAssignmentKind017D.Active ||
            kind == GuildMemberAssignmentKind017D.Reserve ||
            kind == GuildMemberAssignmentKind017D.Training;

        private static int FindAssignmentIndex067(
            IReadOnlyList<GuildMemberAssignmentState017D> assignments,
            string recruitId)
        {
            for (var index = 0; index < assignments.Count; index++)
                if (StringComparer.Ordinal.Equals(assignments[index].RecruitId, recruitId)) return index;
            return -1;
        }

        private static int LowestMasteryIndex067(IReadOnlyList<RecruitArtMasteryState> mastery)
        {
            var selected = 0;
            for (var index = 1; index < mastery.Count; index++)
            {
                var candidate = mastery[index];
                var current = mastery[selected];
                if (candidate.MasteryPoints < current.MasteryPoints ||
                    candidate.MasteryPoints == current.MasteryPoints &&
                    (candidate.MeaningfulUses < current.MeaningfulUses ||
                     candidate.MeaningfulUses == current.MeaningfulUses &&
                     StringComparer.Ordinal.Compare(candidate.ArtId, current.ArtId) < 0))
                    selected = index;
            }
            return selected;
        }

        public Result<CampaignState> CommitBoard(
            CampaignState campaign,
            RecruitmentContent recruitment,
            GuildCityContent017D cityContent)
        {
            if (campaign == null || recruitment == null || cityContent == null)
                return Result<CampaignState>.Failure("GC017D_RECRUITMENT_INPUT_REQUIRED");
            return CommitEarnedBoard124(campaign, refresh: false);
        }

        public IReadOnlyList<HeroMaster300Hero087> PendingExpeditionRecruitLeads089(
            CampaignState campaign)
        {
            var board = campaign?.Guild?.GuildCity?.RecruitmentBoard;
            return PendingExpeditionRecruitLeads089(campaign, board);
        }

        public bool CanInviteEarnedContacts124(CampaignState campaign)
        {
            var board = campaign?.Guild?.GuildCity?.RecruitmentBoard;
            return PendingExpeditionRecruitLeads089(campaign, board).Count > 0 &&
                (board == null || board.Applicants.Count < MaximumApplicantOffers089 ||
                 board.Applicants.Any(value => ApplicantAlreadySigned089(campaign, value)));
        }

        public Result<CampaignState> RefreshBoard(
            CampaignState campaign,
            RecruitmentContent recruitment,
            GuildCityContent017D cityContent)
        {
            if (campaign == null || recruitment == null || cityContent == null)
                return Result<CampaignState>.Failure("GC017D_RECRUITMENT_INPUT_REQUIRED");
            return CommitEarnedBoard124(campaign, refresh: true);
        }

        public static long NextBoardRefreshCostTreasuryXp067(
            int currentRefreshOrdinal)
        {
            var nextRefresh = checked(Math.Max(0, currentRefreshOrdinal) + 1);
            return checked(75L + nextRefresh * 25L);
        }

        public HeroMasterDuplicateMergeForecast089 DescribeHeroMasterDuplicate089(
            CampaignState campaign,
            ApplicantSnapshotState applicant)
        {
            if (campaign?.Guild == null || applicant == null ||
                _heroMasterCatalog089 == null ||
                string.IsNullOrWhiteSpace(applicant.AuthoredStableRecruitId) ||
                !_heroMasterCatalog089.TryGetAcceptedHero(
                    applicant.AuthoredStableRecruitId, out var hero) ||
                hero == null || !hero.IsNormalApplicantEligible ||
                !HasExpeditionRecruitLead089(campaign, hero.StableId))
                return HeroMasterDuplicateMergeForecast089.None();

            var ownedIndex = FindOwnedHeroMasterRecruitIndex089(
                campaign.Guild.Recruits, hero);
            if (ownedIndex < 0) return HeroMasterDuplicateMergeForecast089.None();
            return BuildHeroMasterDuplicateForecast089(
                campaign.Guild.Recruits[ownedIndex],
                ownedIndex,
                hero.StableId);
        }

        public bool IsApplicantAlreadyOwned089(
            CampaignState campaign,
            ApplicantSnapshotState applicant) =>
            !string.IsNullOrWhiteSpace(OwnedRecruitIdForApplicant089(
                campaign, applicant));

        public string OwnedRecruitIdForApplicant089(
            CampaignState campaign,
            ApplicantSnapshotState applicant)
        {
            if (campaign?.Guild == null || applicant == null) return string.Empty;
            var exactIndex = FindRecruitIndex(
                campaign.Guild.Recruits, applicant.RecruitId);
            if (exactIndex >= 0)
                return campaign.Guild.Recruits[exactIndex].RecruitId;
            if (_heroMasterCatalog089 != null &&
                !string.IsNullOrWhiteSpace(applicant.AuthoredStableRecruitId) &&
                _heroMasterCatalog089.TryGetAcceptedHero(
                    applicant.AuthoredStableRecruitId, out var hero) &&
                hero != null)
            {
                var heroIndex = FindOwnedHeroMasterRecruitIndex089(
                    campaign.Guild.Recruits, hero);
                if (heroIndex >= 0)
                    return campaign.Guild.Recruits[heroIndex].RecruitId;
            }
            // Normal procedural and non-tutorial Signature applicants do not
            // carry an AuthoredStableRecruitId. Never compare an empty authority
            // value: otherwise the first ordinary roster member (whose authority
            // fields are also empty) makes every fresh applicant look owned.
            if (string.IsNullOrWhiteSpace(applicant.AuthoredStableRecruitId))
                return string.Empty;
            return campaign.Guild.Recruits.FirstOrDefault(value => value != null &&
                (StringComparer.Ordinal.Equals(
                     value.SignatureId, applicant.AuthoredStableRecruitId) ||
                 StringComparer.Ordinal.Equals(
                     value.AuthoredStableRecruitId,
                     applicant.AuthoredStableRecruitId)))?.RecruitId ?? string.Empty;
        }

        public GuildCityApplicantSigningForecast090
            DescribeApplicantSigning090(
                CampaignState campaign,
                ApplicantSnapshotState applicant)
        {
            if (campaign?.Guild == null)
                return SigningForecastBlocked090(
                    "GC017D_CAMPAIGN_REQUIRED",
                    "Campaign data is unavailable.");
            if (applicant == null)
                return SigningForecastBlocked090(
                    "GC017D_APPLICANT_ID_REQUIRED",
                    "Choose a recruit first.");
            var city = campaign.Guild.GuildCity;
            var board = city?.RecruitmentBoard;
            if (board == null)
                return SigningForecastBlocked090(
                    "GC017D_RECRUITMENT_BOARD_REQUIRED",
                    "Open the current recruitment board first.");
            applicant = board.FindApplicant(applicant.RecruitId);
            if (applicant == null)
                return SigningForecastBlocked090(
                    "GC017D_APPLICANT_NOT_FOUND",
                    "That recruit is no longer on the current board.");

            var duplicate = DescribeHeroMasterDuplicate089(campaign, applicant);
            var cost = AvailableChestRecruitCredit092(campaign, applicant.AuthoredStableRecruitId) != null
                ? 0 : EffectiveSigningCostTreasuryXp(campaign, applicant.SigningCostTreasuryXp);
            if (duplicate.IsDuplicateOffer)
                return campaign.Guild.TreasuryXp >= cost
                    ? SigningForecastAllowed090(cost, true)
                    : SigningForecastBlocked090(
                        "GC017D_INSUFFICIENT_TREASURY_TO_SIGN",
                        "Need " + cost + " Guild XP to ascend " +
                        applicant.DisplayName + ".",
                        cost,
                        true);

            if (FindRecruitIndex(
                    campaign.Guild.Recruits, applicant.RecruitId) >= 0 ||
                RosterContainsAuthority089(
                    campaign.Guild.Recruits,
                    applicant.AuthoredStableRecruitId))
                return SigningForecastAllowed090(0, false);

            if (!ProtectedActorPolicy.CanEnterNormalApplicantOrRoster(
                    applicant.RecruitId,
                    applicant.SignatureId,
                    RecruitAuthorityKind.Normal))
                return SigningForecastBlocked090(
                    "GC017D_PROTECTED_ACTOR_CANNOT_BE_SIGNED",
                    "This protected story character cannot be recruited here.",
                    cost,
                    false);

            var capacity = _effects.RosterCapacity(campaign.Guild.Development);
            if (campaign.Guild.Recruits.Count >= capacity)
                return SigningForecastBlocked090(
                    "GC017D_ROSTER_CAPACITY_REACHED",
                    "Roster full (" + capacity + "/" + capacity +
                    "). Increase Guild housing before recruiting " +
                    applicant.DisplayName + ".",
                    cost,
                    false);
            if (campaign.Guild.TreasuryXp < cost)
                return SigningForecastBlocked090(
                    "GC017D_INSUFFICIENT_TREASURY_TO_SIGN",
                    "Need " + cost + " Guild XP to recruit " +
                    applicant.DisplayName + ".",
                    cost,
                    false);

            for (var index = 0; index < applicant.OpeningEquipment.Count;
                 index++)
            {
                var item = applicant.OpeningEquipment[index];
                if (FindInventoryIndex(
                        campaign.Guild.Inventory, item.InstanceId) >= 0 ||
                    IsItemEquipped(campaign.Guild.Recruits, item.InstanceId))
                    return SigningForecastBlocked090(
                        "GC017D_DUPLICATE_EQUIPMENT_INSTANCE",
                        "Resolve the duplicate starting equipment for " +
                        applicant.DisplayName + " before recruiting.",
                        cost,
                        false);
            }

            return SigningForecastAllowed090(cost, false);
        }

        private static GuildCityApplicantSigningForecast090
            SigningForecastAllowed090(int cost, bool duplicate) =>
                new GuildCityApplicantSigningForecast090(
                    true, cost, duplicate, string.Empty, string.Empty);

        private static GuildCityApplicantSigningForecast090
            SigningForecastBlocked090(
                string failureCode,
                string lockedReason,
                int cost = 0,
                bool duplicate = false) =>
                new GuildCityApplicantSigningForecast090(
                    false, cost, duplicate, failureCode, lockedReason);

        private Result<CampaignState> SignApplicantCore092(CampaignState campaign, string recruitId)
        {
            if (campaign == null) return Result<CampaignState>.Failure("GC017D_CAMPAIGN_REQUIRED");
            if (string.IsNullOrWhiteSpace(recruitId))
                return Result<CampaignState>.Failure("GC017D_APPLICANT_ID_REQUIRED");
            var city = campaign.Guild.GuildCity;
            var board = city.RecruitmentBoard;
            if (board == null) return Result<CampaignState>.Failure("GC017D_RECRUITMENT_BOARD_REQUIRED");
            var applicant = board.FindApplicant(recruitId);
            if (applicant == null) return Result<CampaignState>.Failure("GC017D_APPLICANT_NOT_FOUND");
            var signing = DescribeApplicantSigning090(campaign, applicant);
            if (!signing.CanSign)
                return Result<CampaignState>.Failure(signing.FailureCode);
            var duplicate = DescribeHeroMasterDuplicate089(campaign, applicant);
            if (duplicate.IsDuplicateOffer)
            {
                var duplicateCost = signing.EffectiveCostTreasuryXp;

                var mergedRecruits = new List<RecruitState>(
                    campaign.Guild.Recruits);
                var owned = mergedRecruits[duplicate.OwnedRecruitIndex];
                mergedRecruits[duplicate.OwnedRecruitIndex] =
                    owned.WithProgression(duplicate.ProjectedProgression);
                var mergedCity = city.With(
                    lastCheckpointId: "hero_master_duplicate_merged_089");
                mergedCity = ConsumeExpeditionRecruitLead089(
                    mergedCity, applicant.AuthoredStableRecruitId);
                var mergedGuild = campaign.Guild.With(
                        campaign.Guild.TreasuryXp - duplicateCost,
                        mergedRecruits.AsReadOnly(),
                        campaign.Guild.Unions,
                        campaign.Guild.Inventory)
                    .WithGuildCity(mergedCity);
                return Result<CampaignState>.Success(
                    campaign.With(mergedGuild, campaign.OpeningFlow));
            }
            if (FindRecruitIndex(campaign.Guild.Recruits, recruitId) >= 0)
                return Result<CampaignState>.Success(
                    ConsumeExpeditionRecruitLead089(
                        campaign, applicant.AuthoredStableRecruitId));
            if (RosterContainsAuthority089(
                    campaign.Guild.Recruits, applicant.AuthoredStableRecruitId))
                return Result<CampaignState>.Success(
                    ConsumeExpeditionRecruitLead089(
                        campaign, applicant.AuthoredStableRecruitId));
            var signingCost = signing.EffectiveCostTreasuryXp;

            var recruit = MaterializeApplicant094(applicant);

            var recruits = new List<RecruitState>(campaign.Guild.Recruits) { recruit };
            recruits.Sort((left, right) => StringComparer.Ordinal.Compare(left.RecruitId, right.RecruitId));
            var inventory = new List<EquipmentItemState>(campaign.Guild.Inventory);
            inventory.AddRange(ApplicantInventory094(applicant));
            inventory.Sort((left, right) => StringComparer.Ordinal.Compare(left.InstanceId, right.InstanceId));

            var assignments = new List<GuildMemberAssignmentState017D>(city.MemberAssignments)
            {
                new GuildMemberAssignmentState017D(recruitId,
                    GuildMemberAssignmentKind017D.Reserve, string.Empty, 0, 0, 0)
            };
            assignments.Sort((left, right) => StringComparer.Ordinal.Compare(left.RecruitId, right.RecruitId));
            var updatedCity = city.With(
                memberAssignments: assignments.AsReadOnly(),
                lastCheckpointId: "recurring_applicant_signed");
            updatedCity = ConsumeExpeditionRecruitLead089(
                updatedCity, applicant.AuthoredStableRecruitId);
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp - signingCost,
                recruits.AsReadOnly(), campaign.Guild.Unions, inventory.AsReadOnly())
                .WithGuildCity(updatedCity);
            return Result<CampaignState>.Success(campaign.With(guild, campaign.OpeningFlow));
        }

        public static int EffectiveSigningCostTreasuryXp(CampaignState campaign, int applicantCost)
        {
            if (campaign == null) return Math.Max(0, applicantCost);
            var discounted159 = checked((int)TownProgression159.Discount(campaign, 4, Math.Max(0, applicantCost)));
            var city = campaign.Guild.GuildCity;
            var board = city.RecruitmentBoard;
            if (city.OperationOrdinal != 0 || city.RecruitmentRefreshOrdinal != 0 ||
                campaign.Guild.Recruits.Count > 6 || board == null)
                return discounted159;

            var boardAlreadySigned = board.Applicants.Any(applicant =>
                campaign.Guild.Recruits.Any(recruit =>
                    StringComparer.Ordinal.Equals(recruit.RecruitId, applicant.RecruitId)));
            return boardAlreadySigned ? discounted159 : 0;
        }

        public Result<CampaignState> DeclineApplicant(CampaignState campaign, string recruitId)
        {
            if (campaign == null) return Result<CampaignState>.Failure("GC017D_CAMPAIGN_REQUIRED");
            if (string.IsNullOrWhiteSpace(recruitId))
                return Result<CampaignState>.Failure("GC017D_APPLICANT_ID_REQUIRED");
            var city = campaign.Guild.GuildCity;
            var board = city.RecruitmentBoard;
            if (board == null) return Result<CampaignState>.Failure("GC017D_RECRUITMENT_BOARD_REQUIRED");
            if (FindRecruitIndex(campaign.Guild.Recruits, recruitId) >= 0)
                return Result<CampaignState>.Failure("GC017D_SIGNED_MEMBER_CANNOT_BE_DECLINED");
            if (board.FindApplicant(recruitId) == null)
                return Result<CampaignState>.Failure("GC017D_APPLICANT_NOT_FOUND");

            var remaining = board.Applicants
                .Where(value => !StringComparer.Ordinal.Equals(value.RecruitId, recruitId))
                .ToArray();
            if (remaining.Length == 0)
                return Result<CampaignState>.Failure("GC017D_LAST_APPLICANT_REQUIRES_BOARD_REFRESH");

            var reducedBoard = new ApplicantBoardState(
                board.BoardId,
                board.GenerationKey,
                board.RefreshOrdinal,
                committed: true,
                applicants: remaining,
                committedApplicantsHash: string.Empty);
            var updatedCity = city.With(
                recruitmentBoard: reducedBoard,
                replaceRecruitmentBoard: true,
                lastCheckpointId: "recurring_applicant_declined");
            return Result<CampaignState>.Success(
                campaign.With(campaign.Guild.WithGuildCity(updatedCity), campaign.OpeningFlow));
        }

        // Recurring recruitment exposes only the exact contacts already earned by
        // the existing card/chest lead authority. Historical offers remain valid.
        private Result<CampaignState> CommitEarnedBoard124(
            CampaignState campaign, bool refresh)
        {
            var city = campaign.Guild.GuildCity;
            var board = MergeExpeditionRecruitLeads089(campaign, city.RecruitmentBoard);
            if (ReferenceEquals(board, city.RecruitmentBoard))
                return Result<CampaignState>.Success(campaign);

            var ordinal = city.RecruitmentRefreshOrdinal;
            var cost = refresh ? NextBoardRefreshCostTreasuryXp067(ordinal) : 0L;
            if (campaign.Guild.TreasuryXp < cost)
                return Result<CampaignState>.Failure("GC017D_INSUFFICIENT_TREASURY_FOR_BOARD_REFRESH");
            if (refresh)
            {
                ordinal = checked(ordinal + 1);
                board = new ApplicantBoardState(board.BoardId, board.GenerationKey,
                    ordinal, true, board.Applicants, string.Empty);
            }
            var updatedCity = city.With(
                recruitmentBoard: board,
                replaceRecruitmentBoard: true,
                recruitmentRefreshOrdinal: ordinal,
                lastCheckpointId: refresh ? "recruitment_board_refreshed" :
                    "expedition_recruit_lead_invited_089");
            var guild = campaign.Guild.With(campaign.Guild.TreasuryXp - cost,
                campaign.Guild.Recruits, campaign.Guild.Unions, campaign.Guild.Inventory)
                .WithGuildCity(updatedCity);
            return Result<CampaignState>.Success(campaign.With(guild, campaign.OpeningFlow));
        }

        private ApplicantBoardState MergeExpeditionRecruitLeads089(
            CampaignState campaign,
            ApplicantBoardState board)
        {
            if (campaign == null || _heroMasterCatalog089 == null)
                return board;
            var pending = PendingExpeditionRecruitLeads089(campaign, board);
            if (pending.Count == 0) return board;

            var applicants = new List<ApplicantSnapshotState>(
                board?.Applicants ?? Array.Empty<ApplicantSnapshotState>());
            var changed = false;
            foreach (var hero in pending)
            {
                if (HeroMaster300ApplicantLead089.ApplicantsContain(
                        applicants, hero))
                    continue;

                int slot;
                if (applicants.Count < MaximumApplicantOffers089)
                {
                    slot = FirstAvailableSlot089(applicants);
                }
                else
                {
                    var replacement = applicants
                        // Ownership alone is insufficient: an owned hero with an
                        // unconsumed earned lead is a legitimate duplicate offer.
                        .Where(value => ApplicantAlreadySigned089(campaign, value))
                        .OrderByDescending(value => value.Slot)
                        .FirstOrDefault();
                    if (replacement == null) break;
                    slot = replacement.Slot;
                    applicants.Remove(replacement);
                }

                applicants.Add(HeroMaster300ApplicantLead089.ToApplicant(
                    hero,
                    slot,
                    ExpeditionLeadWorldId089(campaign)));
                changed = true;
            }

            return changed
                ? new ApplicantBoardState(
                    board?.BoardId ?? "GC017D_EARNED_BOARD_124_" +
                        campaign.Guild.GuildCity.OperationOrdinal.ToString("D4") + "_" +
                        campaign.Guild.GuildCity.RecruitmentRefreshOrdinal.ToString("D4"),
                    board?.GenerationKey ?? "EARNED_LEADS_124|" + campaign.CampaignGuid,
                    board?.RefreshOrdinal ?? campaign.Guild.GuildCity.RecruitmentRefreshOrdinal,
                    true,
                    applicants.AsReadOnly(),
                    string.Empty)
                : board;
        }

        private bool ApplicantAlreadySigned089(
            CampaignState campaign,
            ApplicantSnapshotState applicant)
        {
            if (campaign?.Guild == null || applicant == null) return false;
            if (HasExpeditionRecruitLead089(
                    campaign, applicant.AuthoredStableRecruitId))
                return false;
            return IsApplicantAlreadyOwned089(campaign, applicant);
        }

        private IReadOnlyList<HeroMaster300Hero087> PendingExpeditionRecruitLeads089(
            CampaignState campaign,
            ApplicantBoardState board)
        {
            var result = new List<HeroMaster300Hero087>();
            if (campaign?.Guild == null || _heroMasterCatalog089 == null)
                return result.AsReadOnly();
            var runtime = campaign.Guild.GuildCity?.Strategic017H?.Campaign019
                ?.Playable020?.WorldGate023;
            if (runtime?.ExpeditionRecruitLeadIds089 == null)
                return result.AsReadOnly();

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var leadId in runtime.ExpeditionRecruitLeadIds089
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(leadId) ||
                    !_heroMasterCatalog089.TryGetAcceptedHero(leadId, out var hero) ||
                    hero == null || !seen.Add(hero.StableId) ||
                    !hero.IsNormalApplicantEligible ||
                    HeroMaster300ApplicantLead089.BoardContains(board, hero))
                    continue;
                result.Add(hero);
            }
            result.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.StableId, right.StableId));
            return result.AsReadOnly();
        }

        private HeroMasterDuplicateMergeForecast089 BuildHeroMasterDuplicateForecast089(
            RecruitState owned,
            int ownedIndex,
            string authorityId,
            bool allowGeneratedTreeUnlocks094 = true)
        {
            var progression = owned?.Progression ?? RecruitProgressionState.Default();
            if (progression.AscensionLevel < RecruitAscensionRules089.MaximumLevel)
            {
                var ascended = progression.Ascend089();
                return new HeroMasterDuplicateMergeForecast089(
                    HeroMasterDuplicateMergeKind089.Ascension,
                    owned?.RecruitId,
                    ownedIndex,
                    progression.AscensionLevel,
                    ascended.AscensionLevel,
                    RecruitAscensionRules089.MaximumHpBonusPerLevel,
                    RecruitAscensionRules089.MaximumMpBonusPerLevel,
                    RecruitAscensionRules089.CoreStatBonusPerLevel,
                    string.Empty,
                    string.Empty,
                    0,
                    0,
                    0,
                    "ASCENSION " + ascended.AscensionLevel + "/" +
                    RecruitAscensionRules089.MaximumLevel + "  •  +" +
                    RecruitAscensionRules089.MaximumHpBonusPerLevel +
                    " HP  •  +" + RecruitAscensionRules089.MaximumMpBonusPerLevel +
                    " MP  •  +" + RecruitAscensionRules089.CoreStatBonusPerLevel +
                    " STR / DEF / AGI / MAG / WILL",
                    ascended);
            }

            var normalized = progression;
            RecruitTreeProgressionSnapshot070 snapshot = null;
            if (allowGeneratedTreeUnlocks094 && _recruitTreeProgression089 != null)
            {
                try
                {
                    normalized = _recruitTreeProgression089.Normalize(
                        authorityId, progression);
                    snapshot = _recruitTreeProgression089.Describe(
                        authorityId, normalized);
                    var lockedTree = snapshot.Slots
                        .Where(value => value.Earnable && !value.Unlocked)
                        .OrderBy(value => value.SlotKind)
                        .ThenBy(value => value.TreeId, StringComparer.Ordinal)
                        .FirstOrDefault();
                    if (lockedTree != null)
                    {
                        var unlocked = _recruitTreeProgression089.UnlockEarnableTree(
                            authorityId, normalized, lockedTree.TreeId);
                        return new HeroMasterDuplicateMergeForecast089(
                            HeroMasterDuplicateMergeKind089.TreeUnlocked,
                            owned?.RecruitId,
                            ownedIndex,
                            progression.AscensionLevel,
                            progression.AscensionLevel,
                            0,
                            0,
                            0,
                            lockedTree.TreeId,
                            string.Empty,
                            0,
                            0,
                            0,
                            "MAX ASCENSION  •  UNLOCK " +
                            FriendlyProgressionId089(lockedTree.TreeId) +
                            " TREE + OPENING ART",
                            unlocked);
                    }
                }
                catch (KeyNotFoundException)
                {
                    // Imported Hero Master identities do not replace the legacy
                    // Signature Recruit plan authority. Their already-generated
                    // Auto-Generation 010 profile supplies canonical assigned
                    // Mystic/Secondary Role slots through the same progression
                    // root-seeding service.
                    normalized = progression;
                    snapshot = null;
                    if (_heroMasterDeepProgression089 != null &&
                        _heroMasterDeepProgression089
                            .TryUnlockNextEarnableTree089(
                                owned,
                                normalized,
                                out var generatedTreeId,
                                out var generatedUnlock))
                    {
                        return new HeroMasterDuplicateMergeForecast089(
                            HeroMasterDuplicateMergeKind089.TreeUnlocked,
                            owned?.RecruitId,
                            ownedIndex,
                            progression.AscensionLevel,
                            progression.AscensionLevel,
                            0,
                            0,
                            0,
                            generatedTreeId,
                            string.Empty,
                            0,
                            0,
                            0,
                            "MAX ASCENSION  •  UNLOCK " +
                            FriendlyProgressionId089(generatedTreeId) +
                            " TREE + OPENING ART",
                            generatedUnlock);
                    }
                }
            }

            IEnumerable<string> legalArtIds = snapshot == null
                ? normalized.LearnedArtIds
                : snapshot.Arts.Where(value => value.Usable)
                    .Select(value => value.ArtId)
                    .ToArray();
            var artChoice = legalArtIds
                .Select(artId =>
                {
                    var mastery = normalized.ArtMastery.FirstOrDefault(value =>
                        StringComparer.Ordinal.Equals(value.ArtId, artId));
                    var points = mastery?.MasteryPoints ?? 0;
                    return new
                    {
                        ArtId = artId,
                        Mastery = mastery,
                        Points = points,
                        Level = M2ArtMasteryLevelPolicy088.LevelForMasteryPoints(points)
                    };
                })
                .Where(value => value.Level < M2ArtMasteryLevelPolicy088.MaximumLevel)
                .OrderBy(value => value.Level)
                .ThenBy(value => value.Points)
                .ThenBy(value => value.ArtId, StringComparer.Ordinal)
                .FirstOrDefault();
            if (artChoice != null)
            {
                var nextLevel = artChoice.Level + 1;
                var mastery = new List<RecruitArtMasteryState>(
                    normalized.ArtMastery.Where(value =>
                        !StringComparer.Ordinal.Equals(
                            value.ArtId, artChoice.ArtId)))
                {
                    new RecruitArtMasteryState(
                        artChoice.ArtId,
                        artChoice.Mastery?.Discipline ?? string.Empty,
                        artChoice.Mastery?.MeaningfulUses ?? 0,
                        M2ArtMasteryLevelPolicy088.ThresholdForLevel(nextLevel))
                };
                var leveled = normalized.WithArts(
                    normalized.LearnedArtIds,
                    mastery.AsReadOnly());
                return new HeroMasterDuplicateMergeForecast089(
                    HeroMasterDuplicateMergeKind089.ArtLeveled,
                    owned?.RecruitId,
                    ownedIndex,
                    progression.AscensionLevel,
                    progression.AscensionLevel,
                    0,
                    0,
                    0,
                    string.Empty,
                    artChoice.ArtId,
                    artChoice.Level,
                    nextLevel,
                    0,
                    "MAX ASCENSION  •  " +
                    FriendlyProgressionId089(artChoice.ArtId) +
                    " ART  LV " + artChoice.Level + " → " + nextLevel,
                    leveled);
            }

            var trained = normalized.GainPersonalXp(
                RecruitAscensionRules089.FullyMasteredPersonalXpFallback,
                owned?.ClassTendencyId ?? string.Empty);
            return new HeroMasterDuplicateMergeForecast089(
                HeroMasterDuplicateMergeKind089.VeteranTraining,
                owned?.RecruitId,
                ownedIndex,
                progression.AscensionLevel,
                progression.AscensionLevel,
                0,
                0,
                0,
                string.Empty,
                string.Empty,
                0,
                0,
                RecruitAscensionRules089.FullyMasteredPersonalXpFallback,
                "ALL PATHS MASTERED  •  +" +
                RecruitAscensionRules089.FullyMasteredPersonalXpFallback +
                " PERSONAL XP (LEVEL CAP APPLIES)",
                trained);
        }

        private static int FindOwnedHeroMasterRecruitIndex089(
            IReadOnlyList<RecruitState> recruits,
            HeroMaster300Hero087 hero)
        {
            if (recruits == null || hero == null) return -1;
            var projectedId = HeroMaster300CreatorRecruitProjection087
                .ExpeditionApplicantRecruitIdFor089(hero);
            for (var index = 0; index < recruits.Count; index++)
            {
                var recruit = recruits[index];
                if (recruit != null &&
                    (StringComparer.Ordinal.Equals(recruit.RecruitId, projectedId) ||
                     StringComparer.Ordinal.Equals(recruit.RecruitId, hero.StableId) ||
                     StringComparer.Ordinal.Equals(recruit.RecruitId, hero.GameEntityId) ||
                     StringComparer.Ordinal.Equals(
                         recruit.AuthoredStableRecruitId, hero.StableId) ||
                     StringComparer.Ordinal.Equals(recruit.SignatureId, hero.StableId) ||
                     StringComparer.Ordinal.Equals(recruit.SignatureId, hero.GameEntityId)))
                    return index;
            }
            return -1;
        }

        private static bool HasExpeditionRecruitLead089(
            CampaignState campaign,
            string leadId)
        {
            if (string.IsNullOrWhiteSpace(leadId)) return false;
            var ids = campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019
                ?.Playable020?.WorldGate023?.ExpeditionRecruitLeadIds089;
            return ids != null && ids.Contains(leadId);
        }

        private static string FriendlyProgressionId089(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "MYSTERY";
            var words = value.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(part => !StringComparer.OrdinalIgnoreCase.Equals(part, "TREE") &&
                               !StringComparer.OrdinalIgnoreCase.Equals(part, "ART") &&
                               !part.All(char.IsDigit))
                .ToArray();
            return words.Length == 0 ? value : string.Join(" ", words);
        }

        private static int FirstAvailableSlot089(
            IReadOnlyList<ApplicantSnapshotState> applicants)
        {
            var used = new HashSet<int>(applicants.Select(value => value.Slot));
            for (var slot = 1; slot <= MaximumApplicantOffers089; slot++)
                if (!used.Contains(slot)) return slot;
            throw new InvalidOperationException("The Applicant Board has no open offer slot.");
        }

        private static string ExpeditionLeadWorldId089(CampaignState campaign)
        {
            var value = campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019
                ?.Playable020?.WorldGate023?.CurrentWorldId;
            return string.IsNullOrWhiteSpace(value) ||
                   StringComparer.Ordinal.Equals(value, "SKYHOME")
                ? HeroMaster300CreatorRecruitProjection087.DefaultWorldId
                : value;
        }

        private bool RosterContainsAuthority089(
            IReadOnlyList<RecruitState> recruits,
            string authorityId)
        {
            if (recruits == null || string.IsNullOrWhiteSpace(authorityId))
                return false;
            if (recruits.Any(value => value != null &&
                (StringComparer.Ordinal.Equals(value.RecruitId, authorityId) ||
                 StringComparer.Ordinal.Equals(value.SignatureId, authorityId) ||
                 StringComparer.Ordinal.Equals(
                     value.AuthoredStableRecruitId, authorityId))))
                return true;
            return _heroMasterCatalog089 != null &&
                   _heroMasterCatalog089.TryGetAcceptedHero(
                       authorityId, out var hero) &&
                   hero != null &&
                   HeroMaster300ApplicantLead089.RosterContains(recruits, hero);
        }

        private static CampaignState ConsumeExpeditionRecruitLead089(
            CampaignState campaign,
            string leadId)
        {
            if (campaign?.Guild?.GuildCity == null ||
                string.IsNullOrWhiteSpace(leadId)) return campaign;
            var city = ConsumeExpeditionRecruitLead089(
                campaign.Guild.GuildCity, leadId);
            return ReferenceEquals(city, campaign.Guild.GuildCity)
                ? campaign
                : campaign.With(
                    campaign.Guild.WithGuildCity(city),
                    campaign.OpeningFlow);
        }

        private static GuildCityState017D ConsumeExpeditionRecruitLead089(
            GuildCityState017D city,
            string leadId)
        {
            if (city == null || string.IsNullOrWhiteSpace(leadId)) return city;
            var strategic = city.Strategic017H;
            var progress = strategic?.Campaign019;
            var playable = progress?.Playable020;
            var runtime = playable?.WorldGate023;
            if (runtime == null ||
                !runtime.ExpeditionRecruitLeadIds089.Contains(leadId)) return city;

            var remaining = runtime.ExpeditionRecruitLeadIds089
                .Where(value => !StringComparer.Ordinal.Equals(value, leadId))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            runtime = runtime.With(expeditionRecruitLeadIds089: remaining);
            playable = playable.With(
                worldGate023: runtime,
                replaceWorldGate023: true);
            progress = progress.With(
                playable020: playable,
                replacePlayable020: true);
            strategic = strategic.With(
                campaign019: progress,
                replaceCampaign019: true);
            return city.With(
                strategic017H: strategic,
                replaceStrategic017H: true);
        }

        private static RecruitmentAccessProfile017D RecruitmentAccess017D(
            CampaignState campaign,
            RecruitmentContent recruitment)
        {
            var worlds = new HashSet<string>(OpeningWorlds, StringComparer.Ordinal);
            var races = new HashSet<string>(OpeningRaces, StringComparer.Ordinal);
            var flags = new HashSet<string>(StringComparer.Ordinal)
            {
                // Retain the previous recurring-board aliases for old saves and
                // other callers, while using the authored charter flag that the
                // Signature Recruit authority actually requires.
                "GUILD_CHARTERED",
                "WORLD_GATE_01_AVAILABLE"
            };
            if (campaign.OpeningFlow == null || campaign.OpeningFlow.CivicCharterAccepted)
                flags.Add("GUILD_CHARTER_SIGNED");

            var strategic = campaign.Guild.GuildCity?.Strategic017H;
            if (strategic?.StoryGates != null)
                AddFlags017D(flags, strategic.StoryGates);
            var runtime = strategic?.Campaign019?.Playable020?.WorldGate023;
            if (runtime != null)
            {
                AddWorldAccess017D(worlds, flags, runtime.CurrentWorldId);
                for (var index = 0; index < runtime.UnlockedWorldIds.Count; index++)
                    AddWorldAccess017D(worlds, flags, runtime.UnlockedWorldIds[index]);
                AddFlags017D(flags, runtime.CompletedDefinitionIds);
                for (var index = 0; index < runtime.UnlockedRecruitOriginIds.Count; index++)
                {
                    var originId = runtime.UnlockedRecruitOriginIds[index];
                    if (string.IsNullOrWhiteSpace(originId)) continue;
                    flags.Add(originId);
                    AddOriginRaces017D(races, originId);
                }
            }

            // Exact authored worlds also contribute their existing visual races.
            // This keeps the access projection data-driven without making the
            // recurring service depend on a presentation-layer registry.
            var records = recruitment.Signatures["signatureRecruits"];
            if (records != null)
                foreach (var record in records)
                {
                    var worldId = record?["home"]?["worldId"]?.Value<string>();
                    if (!worlds.Contains(worldId)) continue;
                    var raceId = record?["raceId"]?.Value<string>();
                    if (!string.IsNullOrWhiteSpace(raceId)) races.Add(raceId);
                }

            return new RecruitmentAccessProfile017D(
                worlds.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                races.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                flags.OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        private static void AddWorldAccess017D(
            ISet<string> worlds,
            ISet<string> flags,
            string worldId)
        {
            if (string.IsNullOrWhiteSpace(worldId)) return;
            flags.Add(worldId);
            flags.Add(worldId + "_AVAILABLE");
            if (StringComparer.Ordinal.Equals(worldId, "SKYHOME"))
            {
                flags.Add("WORLD_GATE_01_AVAILABLE");
                return;
            }

            worlds.Add(worldId);
            var separator = worldId.LastIndexOf('_');
            if (separator < 0 || separator == worldId.Length - 1) return;
            var suffix = worldId.Substring(separator + 1);
            if (suffix.Length == 2 && int.TryParse(suffix, out var packOrdinal) &&
                packOrdinal > 0)
                flags.Add("WORLD_PACK_" + suffix + "_ACCESS");
        }

        private static void AddFlags017D(ISet<string> flags, IReadOnlyList<string> values)
        {
            if (values == null) return;
            for (var index = 0; index < values.Count; index++)
                if (!string.IsNullOrWhiteSpace(values[index])) flags.Add(values[index]);
        }

        private static void AddOriginRaces017D(ISet<string> races, string originId)
        {
            if (originId.IndexOf("DARKELF", StringComparison.OrdinalIgnoreCase) >= 0 ||
                originId.IndexOf("DARK_ELF", StringComparison.OrdinalIgnoreCase) >= 0)
                races.Add("DARK_ELF");
            else if (originId.IndexOf("DEMON", StringComparison.OrdinalIgnoreCase) >= 0)
                races.Add("DEMON_HERITAGE");
            else if (originId.IndexOf("BEAST", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     originId.IndexOf("DOG", StringComparison.OrdinalIgnoreCase) >= 0)
                races.Add("DOG_TRIBE");
            else if (originId.IndexOf("ORC", StringComparison.OrdinalIgnoreCase) >= 0)
                races.Add("ORC");
            else if (originId.IndexOf("GOBLIN", StringComparison.OrdinalIgnoreCase) >= 0)
                races.Add("GOBLIN");
            else if (originId.IndexOf("HUMAN", StringComparison.OrdinalIgnoreCase) >= 0)
                races.Add("HUMAN");

            // Campaign 023 deliberately projects Bunny recruits through existing
            // Human/Goblin rigs until a native visual authority is ship-ready.
            if (originId.IndexOf("BUNNY", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                races.Add("HUMAN");
                races.Add("GOBLIN");
            }
        }

        private sealed class RecruitmentAccessProfile017D
        {
            public RecruitmentAccessProfile017D(
                IReadOnlyList<string> worldIds,
                IReadOnlyList<string> raceIds,
                IReadOnlyList<string> eligibilityFlags)
            {
                WorldIds = worldIds;
                RaceIds = raceIds;
                EligibilityFlags = eligibilityFlags;
            }

            public IReadOnlyList<string> WorldIds { get; }
            public IReadOnlyList<string> RaceIds { get; }
            public IReadOnlyList<string> EligibilityFlags { get; }
        }

        private static int FindRecruitIndex(IReadOnlyList<RecruitState> values, string id)
        {
            for (var i = 0; i < values.Count; i++)
                if (StringComparer.Ordinal.Equals(values[i].RecruitId, id)) return i;
            return -1;
        }

        private static int FindInventoryIndex(IReadOnlyList<EquipmentItemState> values, string id)
        {
            for (var i = 0; i < values.Count; i++)
                if (StringComparer.Ordinal.Equals(values[i].InstanceId, id)) return i;
            return -1;
        }

        private static bool IsItemEquipped(IReadOnlyList<RecruitState> recruits, string itemId)
        {
            for (var r = 0; r < recruits.Count; r++)
                if (LoadoutContains(recruits[r].Equipment, itemId)) return true;
            return false;
        }

        private static bool LoadoutContains(EquipmentLoadoutState loadout, string itemId)
        {
            if (loadout == null) return false;
            for (var i = 0; i < loadout.Assignments.Count; i++)
                if (StringComparer.Ordinal.Equals(loadout.Assignments[i].Item.InstanceId, itemId)) return true;
            return false;
        }
    }
}
