using System;

namespace SecondDimension.Presentation.Battle.ArtProduction013
{
    [Serializable]
    public sealed class ArtPresentationBindingManifest013
    {
        public string contentVersion;
        public int bindingCount;
        public ArtPresentationBinding013[] bindings;
        public ArtPresentationLaws013 laws;
    }

    [Serializable]
    public sealed class ArtPresentationBinding013
    {
        public string stableArtId;
        public string displayName;
        public string treeId;
        public string artClass;
        public string nodeType;
        public int tier;
        public string weaponFamilyId;
        public string anticipationPoseId;
        public string actionPoseId;
        public string targetConsequencePoseId;
        public string returnPoseId;
        public string downedPoseId;
        public string victoryPoseId;
        public string scheduleId;
        public string animationFamilyId;
        public string cameraProfileId;
        public string primaryVfxId;
        public string impactVfxId;
        public string semanticIconId;
        public string sourceSfxProfileId;
        public string audioCueFamilyId;
        public int hitStopMilliseconds;
        public string screenShake;
        public bool requiresDedicatedActionPose;
        public bool allowsSharedWeaponFamilyRig;
        public string reducedMotionProfile;
        public bool presentationOnly;
        public bool changesBattleResolution;
        public bool memberActionClickable;
        public bool playerDirectlySelectableInStandard;
        public string productionStatus;
    }

    [Serializable]
    public sealed class ArtPresentationLaws013
    {
        public bool presentationOnly;
        public bool changesBattleResolution;
        public bool individualArtSelectableInStandard;
        public int supportsAlliedUnionSlots;
        public int supportsEnemyUnionSlots;
        public bool runtimeGenerativeAI;
        public bool unknownRecruitUsesModularPuppet010;
        public bool unknownRecruitMayNotBorrowAnotherIdentity;
    }
}
