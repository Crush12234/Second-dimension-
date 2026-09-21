using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.Recruitment.AutoGeneration010
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class RecruitVisualLayerSelection010
    {
        [JsonProperty("layerId")] public string LayerId { get; internal set; }
        [JsonProperty("pieceId")] public string PieceId { get; internal set; }
        [JsonProperty("resourcePath")] public string ResourcePath { get; internal set; }
        [JsonProperty("required")] public bool Required { get; internal set; }
        [JsonProperty("assetStatus")] public string AssetStatus { get; internal set; }
        [JsonProperty("order")] public int Order { get; internal set; }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class RecruitVisualRecipe010
    {
        [JsonProperty("visualRecipeId")] public string VisualRecipeId { get; internal set; }
        [JsonProperty("visualSeed")] public string VisualSeed { get; internal set; }
        [JsonProperty("visualMode")] public string VisualMode { get; internal set; }
        [JsonProperty("portraitResourcePath")] public string PortraitResourcePath { get; internal set; }
        [JsonProperty("standeeResourcePath")] public string StandeeResourcePath { get; internal set; }
        [JsonProperty("actionResourcePath")] public string ActionResourcePath { get; internal set; }
        [JsonProperty("paletteIds")] public IReadOnlyList<string> PaletteIds { get; internal set; }
        [JsonProperty("layers")] public IReadOnlyList<RecruitVisualLayerSelection010> Layers { get; internal set; }
        [JsonProperty("collisionKey")] public string CollisionKey { get; internal set; }
        [JsonProperty("silhouetteKey")] public string SilhouetteKey { get; internal set; }
        [JsonProperty("runtimeGeneratedAi")] public bool RuntimeGeneratedAi { get; internal set; }
        [JsonProperty("cosmeticOnly")] public bool CosmeticOnly { get; internal set; }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class RecruitBattlePuppetRecipe010
    {
        [JsonProperty("battlePuppetId")] public string BattlePuppetId { get; internal set; }
        [JsonProperty("bodyRigId")] public string BodyRigId { get; internal set; }
        [JsonProperty("weaponFamilyId")] public string WeaponFamilyId { get; internal set; }
        [JsonProperty("weaponPropResourcePath")] public string WeaponPropResourcePath { get; internal set; }
        [JsonProperty("offhandPropResourcePath")] public string OffhandPropResourcePath { get; internal set; }
        [JsonProperty("armorOverlayResourcePath")] public string ArmorOverlayResourcePath { get; internal set; }
        [JsonProperty("animationFamilyIds")] public IReadOnlyList<string> AnimationFamilyIds { get; internal set; }
        [JsonProperty("lodModes")] public IReadOnlyList<string> LodModes { get; internal set; }
        [JsonProperty("supportsPoseSwap")] public bool SupportsPoseSwap { get; internal set; }
        [JsonProperty("supportsLayeredRig")] public bool SupportsLayeredRig { get; internal set; }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class GeneratedRecruitProfile010
    {
        [JsonProperty("contentVersion")] public string ContentVersion { get; internal set; }
        [JsonProperty("profileId")] public string ProfileId { get; internal set; }
        [JsonProperty("recruitId")] public string RecruitId { get; internal set; }
        [JsonProperty("sourceType")] public string SourceType { get; internal set; }
        [JsonProperty("signatureId")] public string SignatureId { get; internal set; }
        [JsonProperty("stableAuthoredRecruitId")] public string StableAuthoredRecruitId { get; internal set; }
        [JsonProperty("displayName")] public string DisplayName { get; internal set; }
        [JsonProperty("raceId")] public string RaceId { get; internal set; }
        [JsonProperty("worldId")] public string WorldId { get; internal set; }
        [JsonProperty("startingClassId")] public string StartingClassId { get; internal set; }
        [JsonProperty("fixedWeaponFamilyId")] public string FixedWeaponFamilyId { get; internal set; }
        [JsonProperty("weaponTreeId")] public string WeaponTreeId { get; internal set; }
        [JsonProperty("mysticTreeId")] public string MysticTreeId { get; internal set; }
        [JsonProperty("primaryRoleTreeId")] public string PrimaryRoleTreeId { get; internal set; }
        [JsonProperty("secondaryRoleTreeId")] public string SecondaryRoleTreeId { get; internal set; }
        [JsonProperty("legalTreeIds")] public IReadOnlyList<string> LegalTreeIds { get; internal set; }
        [JsonProperty("startingStableNodeIds")] public IReadOnlyList<string> StartingStableNodeIds { get; internal set; }
        [JsonProperty("preservedLegacyArtIds")] public IReadOnlyList<string> PreservedLegacyArtIds { get; internal set; }
        [JsonProperty("startingLearnedArtIds")] public IReadOnlyList<string> StartingLearnedArtIds { get; internal set; }
        [JsonProperty("artDisciplineById")] public IReadOnlyDictionary<string,string> ArtDisciplineById { get; internal set; }
        [JsonProperty("roleWeights")] public IReadOnlyDictionary<string,int> RoleWeights { get; internal set; }
        [JsonProperty("baseStats")] public IReadOnlyDictionary<string,int> BaseStats { get; internal set; }
        [JsonProperty("maximumHp")] public int MaximumHp { get; internal set; }
        [JsonProperty("maximumMp")] public int MaximumMp { get; internal set; }
        [JsonProperty("leadershipScore")] public int LeadershipScore { get; internal set; }
        [JsonProperty("commandBandwidth")] public int CommandBandwidth { get; internal set; }
        [JsonProperty("autoEquipAllowed")] public bool AutoEquipAllowed { get; internal set; }
        [JsonProperty("visualRecipe")] public RecruitVisualRecipe010 VisualRecipe { get; internal set; }
        [JsonProperty("battlePuppetRecipe")] public RecruitBattlePuppetRecipe010 BattlePuppetRecipe { get; internal set; }
        [JsonProperty("profileHash")] public string ProfileHash { get; internal set; }
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class GeneratedApplicantBoard010
    {
        [JsonProperty("boardId")] public string BoardId { get; internal set; }
        [JsonProperty("profiles")] public IReadOnlyList<GeneratedRecruitProfile010> Profiles { get; internal set; }
        [JsonProperty("visualCollisionRepairs")] public int VisualCollisionRepairs { get; internal set; }
        [JsonProperty("generationHash")] public string GenerationHash { get; internal set; }
    }
}
