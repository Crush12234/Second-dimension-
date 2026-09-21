using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.Recruitment
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class StatTendency
    {
        [JsonProperty("baseIndex")] public int BaseIndex { get; internal set; }
        [JsonProperty("growthBias")] public int GrowthBias { get; internal set; }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class EquipmentItem
    {
        [JsonProperty("instanceId")] public string InstanceId { get; internal set; }
        [JsonProperty("itemDefinitionId")] public string ItemDefinitionId { get; internal set; }
        [JsonProperty("tags")] public IReadOnlyList<string> Tags { get; internal set; }
        [JsonProperty("locked")] public bool Locked { get; internal set; }
        [JsonProperty("condition")] public string Condition { get; internal set; }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class EquipmentLoadout
    {
        [JsonProperty("loadoutId")] public string LoadoutId { get; internal set; }
        [JsonProperty("slots")] public IReadOnlyDictionary<string, EquipmentItem> Slots { get; internal set; }
        [JsonProperty("aggregateBonuses")] public IReadOnlyDictionary<string, int> AggregateBonuses { get; internal set; }
        [JsonProperty("autoEquipAllowed")] public bool AutoEquipAllowed { get; internal set; }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class OpeningRecruitRecord
    {
        [JsonProperty("recruitId")] public string RecruitId { get; internal set; }
        [JsonProperty("sourceType")] public string SourceType { get; internal set; }
        [JsonProperty("signatureId")] public string SignatureId { get; internal set; }
        [JsonProperty("generationSeed")] public string GenerationSeed { get; internal set; }
        [JsonProperty("visualSeed")] public string VisualSeed { get; internal set; }
        [JsonProperty("displayName")] public string DisplayName { get; internal set; }
        [JsonProperty("raceId")] public string RaceId { get; internal set; }
        [JsonProperty("homeCommunityId")] public string HomeCommunityId { get; internal set; }
        [JsonProperty("ageBandId")] public string AgeBandId { get; internal set; }
        [JsonProperty("pronounId")] public string PronounId { get; internal set; }
        [JsonProperty("backgroundId")] public string BackgroundId { get; internal set; }
        [JsonProperty("startingClassId")] public string StartingClassId { get; internal set; }
        [JsonProperty("visibleTraitIds")] public IReadOnlyList<string> VisibleTraitIds { get; internal set; }
        [JsonProperty("hiddenTraitId")] public string HiddenTraitId { get; internal set; }
        [JsonProperty("hiddenTraitRevealed")] public bool HiddenTraitRevealed { get; internal set; }
        [JsonProperty("fearId")] public string FearId { get; internal set; }
        [JsonProperty("ambitionId")] public string AmbitionId { get; internal set; }
        [JsonProperty("growthPatternId")] public string GrowthPatternId { get; internal set; }
        [JsonProperty("leadershipTendencyId")] public string LeadershipTendencyId { get; internal set; }
        [JsonProperty("weaponAptitudeProfileId")] public string WeaponAptitudeProfileId { get; internal set; }
        [JsonProperty("disciplineProfileId")] public string DisciplineProfileId { get; internal set; }
        [JsonProperty("relationshipTendencyId")] public string RelationshipTendencyId { get; internal set; }
        [JsonProperty("dialogueStyleId")] public string DialogueStyleId { get; internal set; }
        [JsonProperty("personalEventHookId")] public string PersonalEventHookId { get; internal set; }
        [JsonProperty("disciplineAptitudes")] public IReadOnlyDictionary<string, int> DisciplineAptitudes { get; internal set; }
        [JsonProperty("statTendencies")] public IReadOnlyDictionary<string, StatTendency> StatTendencies { get; internal set; }
        [JsonProperty("weaponAptitudes")] public IReadOnlyDictionary<string, int> WeaponAptitudes { get; internal set; }
        [JsonProperty("leadershipScore")] public int LeadershipScore { get; internal set; }
        [JsonProperty("commandBandwidth")] public int CommandBandwidth { get; internal set; }
        [JsonProperty("startingArtIds")] public IReadOnlyList<string> StartingArtIds { get; internal set; }
        [JsonProperty("equipmentLoadout")] public EquipmentLoadout EquipmentLoadout { get; internal set; }
        [JsonProperty("developmentPotentialScore")] public int DevelopmentPotentialScore { get; internal set; }
        [JsonProperty("signingCostXp")] public int SigningCostXp { get; internal set; }
        [JsonProperty("variantFlags")] public IReadOnlyList<string> VariantFlags { get; internal set; }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class ValueRange
    {
        [JsonProperty("range")] public IReadOnlyList<int> Range { get; internal set; }
        [JsonProperty("confidence")] public int Confidence { get; internal set; }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class AptitudeEstimate
    {
        [JsonProperty("estimatedRange")] public IReadOnlyList<int> EstimatedRange { get; internal set; }
        [JsonProperty("label")] public string Label { get; internal set; }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class ScoutingReport
    {
        [JsonProperty("recruitId")] public string RecruitId { get; internal set; }
        [JsonProperty("displayName")] public string DisplayName { get; internal set; }
        [JsonProperty("raceId")] public string RaceId { get; internal set; }
        [JsonProperty("homeCommunityId")] public string HomeCommunityId { get; internal set; }
        [JsonProperty("startingClassId")] public string StartingClassId { get; internal set; }
        [JsonProperty("backgroundId")] public string BackgroundId { get; internal set; }
        [JsonProperty("visibleTraitIds")] public IReadOnlyList<string> VisibleTraitIds { get; internal set; }
        [JsonProperty("leadershipEstimate")] public ValueRange LeadershipEstimate { get; internal set; }
        [JsonProperty("disciplineEstimates")] public IReadOnlyDictionary<string, AptitudeEstimate> DisciplineEstimates { get; internal set; }
        [JsonProperty("growthAssessment")] public string GrowthAssessment { get; internal set; }
        [JsonProperty("qualityProfile")] public string QualityProfile { get; internal set; }
        [JsonProperty("exactPotentialDisplayed")] public bool ExactPotentialDisplayed { get; internal set; }
        [JsonProperty("hiddenTraitRevealed")] public bool HiddenTraitRevealed { get; internal set; }
        [JsonProperty("hiddenDataWithheld")] public bool HiddenDataWithheld { get; internal set; }
        [JsonProperty("scoutingAccuracy")] public int ScoutingAccuracy { get; internal set; }
        [JsonProperty("hiddenTraitId", NullValueHandling = NullValueHandling.Ignore)]
        public string HiddenTraitId { get; internal set; }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class RecruitmentOfficeState
    {
        [JsonProperty("tier")] public int Tier { get; internal set; }
        [JsonProperty("boardSize")] public int BoardSize { get; internal set; }
        [JsonProperty("scoutingAccuracyBase")] public int ScoutingAccuracyBase { get; internal set; }
        [JsonProperty("signatureBonusBasisPoints")] public int SignatureBonusBasisPoints { get; internal set; }
        [JsonProperty("freeRefreshesPerDay")] public int FreeRefreshesPerDay { get; internal set; }
        [JsonProperty("manualRefreshBaseXp")] public int ManualRefreshBaseXp { get; internal set; }
        [JsonProperty("targetedScouting")] public bool TargetedScouting { get; internal set; }
        [JsonProperty("dryStreak")] public int DryStreak { get; internal set; }
        [JsonProperty("refreshIndexToday")] public int RefreshIndexToday { get; internal set; }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class ApplicantSlotDetailed
    {
        [JsonProperty("slotIndex")] public int SlotIndex { get; internal set; }
        [JsonProperty("sourceType")] public string SourceType { get; internal set; }
        [JsonProperty("sourceChannel")] public string SourceChannel { get; internal set; }
        [JsonProperty("guaranteed")] public bool Guaranteed { get; internal set; }
        [JsonProperty("recruitId")] public string RecruitId { get; internal set; }
        [JsonProperty("signatureId")] public string SignatureId { get; internal set; }
        [JsonProperty("signingCostXp")] public int SigningCostXp { get; internal set; }
        [JsonProperty("applicantRecord")] public OpeningRecruitRecord ApplicantRecord { get; internal set; }
        [JsonProperty("scoutingReport")] public ScoutingReport ScoutingReport { get; internal set; }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class DetailedApplicantBoard
    {
        [JsonProperty("boardId")] public string BoardId { get; internal set; }
        [JsonProperty("generationSeed")] public string GenerationSeed { get; internal set; }
        [JsonProperty("stateHash")] public string StateHash { get; internal set; }
        [JsonProperty("campaignSeedHash")] public string CampaignSeedHash { get; internal set; }
        [JsonProperty("guildDay")] public int GuildDay { get; internal set; }
        [JsonProperty("refreshIndex")] public int RefreshIndex { get; internal set; }
        [JsonProperty("worldIds")] public IReadOnlyList<string> WorldIds { get; internal set; }
        [JsonProperty("signatureChanceBasisPoints")] public int SignatureChanceBasisPoints { get; internal set; }
        [JsonProperty("dryStreakBefore")] public int DryStreakBefore { get; internal set; }
        [JsonProperty("dryStreakAfter")] public int DryStreakAfter { get; internal set; }
        [JsonProperty("mercyCharterTriggered")] public bool MercyCharterTriggered { get; internal set; }
        [JsonProperty("officeState")] public RecruitmentOfficeState OfficeState { get; internal set; }
        [JsonProperty("applicants")] public IReadOnlyList<ApplicantSlotDetailed> Applicants { get; internal set; }
        [JsonProperty("committed")] public bool Committed { get; internal set; }
        [JsonProperty("expiresAfterGuildDay")] public int ExpiresAfterGuildDay { get; internal set; }
    }

    public sealed class ProceduralRecruitRequest
    {
        public object CampaignSeed { get; set; }
        public int GuildDay { get; set; }
        public int RefreshIndex { get; set; }
        public int SlotIndex { get; set; }
        public string SourceChannel { get; set; }
        public IReadOnlyList<string> UnlockedRaces { get; set; }
        public string ExtraSalt { get; set; } = string.Empty;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class TargetedSearchSpec
    {
        [JsonProperty("signatureId", NullValueHandling = NullValueHandling.Ignore)] public string SignatureId { get; set; }
        [JsonProperty("raceId", NullValueHandling = NullValueHandling.Ignore)] public string RaceId { get; set; }
        [JsonProperty("startingClassId", NullValueHandling = NullValueHandling.Ignore)] public string StartingClassId { get; set; }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class ForcedApplicantSpec
    {
        [JsonProperty("slotIndex", NullValueHandling = NullValueHandling.Ignore)] public int? SlotIndex { get; set; }
        [JsonProperty("sourceType")] public string SourceType { get; set; }
        [JsonProperty("signatureId", NullValueHandling = NullValueHandling.Ignore)] public string SignatureId { get; set; }
        [JsonProperty("sourceChannel", NullValueHandling = NullValueHandling.Ignore)] public string SourceChannel { get; set; }
        [JsonProperty("extraSalt", NullValueHandling = NullValueHandling.Ignore)] public string ExtraSalt { get; set; }
    }

    public sealed class ApplicantBoardRequest
    {
        public object CampaignSeed { get; set; }
        public int GuildDay { get; set; }
        public int RefreshIndex { get; set; }
        public int OfficeTier { get; set; }
        public int DryStreak { get; set; }
        public IReadOnlyList<string> UnlockedWorlds { get; set; }
        public IReadOnlyList<string> UnlockedRaces { get; set; }
        public IReadOnlyList<string> EligibilityFlags { get; set; }
        public int GuildRank { get; set; }
        public IReadOnlyList<string> RecruitedSignatureIds { get; set; }
        public IReadOnlyList<string> SeenSignatureIds { get; set; }
        public IReadOnlyDictionary<string, int> DeclinedSignatureUntilDay { get; set; }
        public int EventBonusBasisPoints { get; set; }
        public int ScoutSkill { get; set; }
        public TargetedSearchSpec TargetedSearch { get; set; }
        public IReadOnlyList<ForcedApplicantSpec> ForcedApplicants { get; set; }
        public int? BoardSizeOverride { get; set; }
    }

    public sealed class TutorialSignatureAlias
    {
        public TutorialSignatureAlias(string tutorialStableId, string signatureId, string stableRecruitId)
        {
            TutorialStableId = tutorialStableId;
            SignatureId = signatureId;
            StableRecruitId = stableRecruitId;
        }

        public string TutorialStableId { get; }
        public string SignatureId { get; }
        public string StableRecruitId { get; }
    }

    public sealed class TutorialApplicantAuthoritySlot
    {
        public TutorialApplicantAuthoritySlot(
            int authoringSlot,
            int boardSlotIndex,
            string kind,
            string sourceSeed,
            string tutorialStableId,
            string signatureId,
            string stableRecruitId,
            string roleHint)
        {
            AuthoringSlot = authoringSlot;
            BoardSlotIndex = boardSlotIndex;
            Kind = kind;
            SourceSeed = sourceSeed;
            TutorialStableId = tutorialStableId;
            SignatureId = signatureId;
            StableRecruitId = stableRecruitId;
            RoleHint = roleHint;
        }

        public int AuthoringSlot { get; }
        public int BoardSlotIndex { get; }
        public string Kind { get; }
        public string SourceSeed { get; }
        public string TutorialStableId { get; }
        public string SignatureId { get; }
        public string StableRecruitId { get; }
        public string RoleHint { get; }
    }
}
