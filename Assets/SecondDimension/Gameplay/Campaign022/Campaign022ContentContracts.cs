using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.Campaign022
{
    [Serializable] public sealed class CampaignManifestDto022 { public string contentVersion; public int saveFormatVersion; public int weaponFamilyTrackCount; public int weaponEvolutionRecipeCount; public int armorEvolutionRecipeCount; public int startingClassFoundationCount; public int advancedClassUniqueCount; public int advancedClassCertificationCount; public int advancedClassCertificationPathCount; public int abyssFloorCount; public int abyssOperationCount; public int abyssEventCount; public int abyssBossCount; public int invocationArtifactBaseCount; public int invocationSupportItemCount; public int invocationAffixCount; public int artifactEvolutionPathCount; public int summonEchoCount; public int greatCovenantCount; public int endgameUnlockCount; }
    [Serializable] public sealed class MaterialCostDto022 { public string materialId; public int amount; }
    [Serializable] public sealed class WeaponRecipeDto022 { public string recipeId; public string trackId; public string weaponFamilyId; public string fromTier; public string toTier; public int forgeLevelRequired; public int requiredMasteryPoints; public int requiredMeaningfulUses; public MaterialCostDto022[] materialCosts; public bool preserveInstanceId; public bool noAutoEquip; public bool exactOnce; public bool godlyRequiresStoryOrAbyssGate; }
    [Serializable] public sealed class WeaponTrackDto022 { public string trackId; public string weaponFamilyId; public string displayName; public string proficiencyTrackId; public string combatIdentity; }
    [Serializable] public sealed class WeaponTrackRoot022 { public WeaponTrackDto022[] tracks; public WeaponRecipeDto022[] recipes; }
    [Serializable] public sealed class ArmorRecipeDto022 { public string recipeId; public string armorSetId; public string displayName; public string fromTier; public string toTier; public int forgeLevelRequired; public int requiredOperations; public MaterialCostDto022[] materialCosts; public bool noAutoEquip; public bool exactOnce; }
    [Serializable] public sealed class ArmorRoot022 { public ArmorRecipeDto022[] recipes; }
    [Serializable] public sealed class ClassProfileDto022 { public string classId; public string displayName; public string primaryDiscipline; public int primaryProficiencyRequired; public string secondaryDiscipline; public int secondaryProficiencyRequired; public int primaryUnionRankRequired; public int secondaryUnionRankRequired; public int meaningfulPrimaryActionsRequired; public int meaningfulSecondaryActionsRequired; public string certificationFacility; public bool zeroEffectSpamForbidden; }
    [Serializable] public sealed class CertificationPathDto022 { public string certificationId; public string sourceClassId; public string sourceClassDisplayName; public string advancedClassId; public string advancedClassDisplayName; public string primaryDiscipline; public string secondaryDiscipline; public int trainingXpCost; public bool meaningfulUseRequired; public bool learnedArtsPreservedWhenLegal; public bool weaponLegalityRemainsAuthoritative; public bool exactOnceCertificationReceipt; }
    [Serializable] public sealed class ClassRoot022 { public ClassProfileDto022[] classes; public CertificationPathDto022[] certificationPaths; }
    [Serializable] public sealed class AbyssFloorDto022 { public int floor; public string floorId; public string displayName; public string identity; public string firstClearAboveGroundChangeId; public bool exactlyOneMarginOnEligibleFloor; public int firstClearFullRewardPermille; public int secondClearRewardPermille; public int laterClearRewardPermille; public bool antiFarmJudgment; }
    [Serializable] public sealed class FloorRoot022 { public AbyssFloorDto022[] floors; }
    [Serializable] public sealed class AbyssStepDto022 { public string stepId; public string kind; public string title; public bool requiresBattle; public string bossId; public bool exactOnce; }
    [Serializable] public sealed class AbyssOperationDto022 { public string operationId; public string floorId; public string kind; public string displayName; public AbyssStepDto022[] steps; public bool firstClearOnly; public bool requiresPreviousFloorClear; public string[] rewardMaterialIds; public int guildXp; public int hallXp; public int summonResonance; public bool exactOnceReceipts; public bool existingEquipmentRewardRemainsAuthoritative; }
    [Serializable] public sealed class AbyssOperationRoot022 { public AbyssOperationDto022[] operations; }
    [Serializable] public sealed class ArtifactBaseDto022 { public string baseId; public string displayName; public string weaponFamilyId; public string validSlotId; public string ownership; public bool sentientOwnershipAllowed; public string standardControl; public int maxActivePerEarlyUnion; public bool usesSharedAp; public bool usesIndividualMp; public string baseForecastRole; public string requiredFacility; public bool runtimeGenerativeAi; public MaterialCostDto022[] materialCosts; }
    [Serializable] public sealed class SupportItemDto022 { public string itemId; public string displayName; public string effect; }
    [Serializable] public sealed class ArtifactRoot022 { public ArtifactBaseDto022[] bases; public SupportItemDto022[] supportItems; }
    [Serializable] public sealed class AffixDto022 { public string affixId; public string displayName; public string forecastCategory; public int effectPermille; public bool meaningfulUseRequired; public bool zeroEffectSpamForbidden; public int maximumCopiesPerArtifact; }
    [Serializable] public sealed class AffixRoot022 { public AffixDto022[] affixes; }
    [Serializable] public sealed class ArtifactEvolutionStageDto022 { public int stage; public string name; public int resonanceRequired; public int abyssFloorRequired; }
    [Serializable] public sealed class ArtifactPathDto022 { public string pathId; public string weaponFamilyId; public string displayName; public ArtifactEvolutionStageDto022[] stages; public bool preserveArtifactInstance; public bool noSentientOwnership; public bool voluntaryReleaseAlwaysAvailable; }
    [Serializable] public sealed class ArtifactPathRoot022 { public ArtifactPathDto022[] paths; }
    [Serializable] public sealed class EchoDto022 { public string echoId; public string displayName; public int unlockFloor; public string forecastCategory; public string description; public int sharedApCost; public int personalMpCost; public bool directIndividualSelection; public bool realMoneyGacha; }
    [Serializable] public sealed class EchoRoot022 { public EchoDto022[] echoes; }
    [Serializable] public sealed class CovenantDto022 { public string covenantId; public string displayName; public string authorityStatus; public string role; public string identity; public string[] requirements; public int minimumAbyssFloor; public string requiredStoryGate; public bool sentientOwnershipAllowed; public bool voluntaryAcceptanceRequired; public string standardControl; public bool usesSharedAp; public bool usesIndividualMp; public bool directIndividualSelection; public bool realMoneyGacha; public int trustMaximum; }
    [Serializable] public sealed class CovenantRoot022 { public CovenantDto022[] covenants; }
    [Serializable] public sealed class UnlockDto022 { public string unlockId; public string sourceFloorId; public string displayName; public string aboveGroundChangeId; public bool exactOnce; }
    [Serializable] public sealed class UnlockRoot022 { public UnlockDto022[] unlocks; }

    public interface ICampaignRegistry022
    {
        CampaignManifestDto022 Manifest { get; }
        IReadOnlyDictionary<string,WeaponTrackDto022> WeaponTracks { get; }
        IReadOnlyDictionary<string,WeaponRecipeDto022> WeaponRecipes { get; }
        IReadOnlyDictionary<string,ArmorRecipeDto022> ArmorRecipes { get; }
        IReadOnlyDictionary<string,ClassProfileDto022> Classes { get; }
        IReadOnlyDictionary<string,CertificationPathDto022> CertificationPaths { get; }
        IReadOnlyDictionary<string,AbyssFloorDto022> Floors { get; }
        IReadOnlyDictionary<string,AbyssOperationDto022> AbyssOperations { get; }
        IReadOnlyDictionary<string,ArtifactBaseDto022> ArtifactBases { get; }
        IReadOnlyDictionary<string,AffixDto022> Affixes { get; }
        IReadOnlyDictionary<string,ArtifactPathDto022> ArtifactPaths { get; }
        IReadOnlyDictionary<string,EchoDto022> Echoes { get; }
        IReadOnlyDictionary<string,CovenantDto022> Covenants { get; }
        IReadOnlyDictionary<string,UnlockDto022> Unlocks { get; }
    }
}
