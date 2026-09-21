using System;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Presentation.Battle.ArtProduction011;
using PoseSetManifest011 = SecondDimension.Presentation.Battle.ArtProduction011.BattleArtManifest011;

namespace SecondDimension.Presentation.Battle.ArtProduction013
{
    public enum BattleVisualSourceKind013
    {
        BespokePoseSet011,
        ModularRecruitPuppet010,
        HonestMissingAssetFallback
    }

    [Serializable]
    public sealed class ResolvedArtPresentationPlan013
    {
        public string actorStableId;
        public string stableArtId;
        public string displayName;
        public BattlePoseId011 anticipationPose;
        public BattlePoseId011 actionPose;
        public BattlePoseId011 targetConsequencePose;
        public BattlePoseId011 returnPose;
        public string scheduleId;
        public string animationFamilyId;
        public string cameraProfileId;
        public string primaryVfxId;
        public string impactVfxId;
        public string semanticIconId;
        public string audioCueFamilyId;
        public int hitStopMilliseconds;
        public string screenShake;
        public string reducedMotionProfile;
        public BattleVisualSourceKind013 visualSource;
        public bool presentationOnly;
    }

    public static class BattleArtPresentationResolver013
    {
        public static bool TryResolve(
            string actorStableId,
            string stableArtId,
            GeneratedRecruitProfile010 generatedProfile,
            out ResolvedArtPresentationPlan013 plan)
        {
            plan = null;
            if (!ArtPresentationBindingLoader013.TryGet(stableArtId, out ArtPresentationBinding013 binding))
                return false;
            plan = new ResolvedArtPresentationPlan013
            {
                actorStableId = actorStableId ?? string.Empty,
                stableArtId = binding.stableArtId,
                displayName = binding.displayName,
                anticipationPose = ParsePose(binding.anticipationPoseId),
                actionPose = ParsePose(binding.actionPoseId),
                targetConsequencePose = ParsePose(binding.targetConsequencePoseId),
                returnPose = ParsePose(binding.returnPoseId),
                scheduleId = binding.scheduleId,
                animationFamilyId = binding.animationFamilyId,
                cameraProfileId = binding.cameraProfileId,
                primaryVfxId = binding.primaryVfxId,
                impactVfxId = binding.impactVfxId,
                semanticIconId = binding.semanticIconId,
                audioCueFamilyId = binding.audioCueFamilyId,
                hitStopMilliseconds = binding.hitStopMilliseconds,
                screenShake = binding.screenShake,
                reducedMotionProfile = binding.reducedMotionProfile,
                visualSource = ResolveVisualSource(actorStableId, generatedProfile),
                presentationOnly = true
            };
            return true;
        }

        public static BattleVisualSourceKind013 ResolveVisualSource(
            string actorStableId,
            GeneratedRecruitProfile010 generatedProfile)
        {
            PoseSetManifest011 manifest = BattleArtManifestLoader013.Load();
            foreach (CharacterPoseSet011 character in manifest.characters)
                if (character != null && StringComparer.Ordinal.Equals(character.stableId, actorStableId))
                    return BattleVisualSourceKind013.BespokePoseSet011;
            if (generatedProfile != null && generatedProfile.BattlePuppetRecipe != null)
                return BattleVisualSourceKind013.ModularRecruitPuppet010;
            return BattleVisualSourceKind013.HonestMissingAssetFallback;
        }

        public static BattlePoseId011 ParsePose(string value)
        {
            switch (value)
            {
                case "IDLE_READY": return BattlePoseId011.IdleReady;
                case "ANTICIPATION": return BattlePoseId011.Anticipation;
                case "PRIMARY_ACTION": return BattlePoseId011.PrimaryAction;
                case "ROLE_ACTION": return BattlePoseId011.RoleAction;
                case "GUARD_CAST_SUPPORT": return BattlePoseId011.GuardCastSupport;
                case "HIT_REACTION": return BattlePoseId011.HitReaction;
                case "DOWNED": return BattlePoseId011.Downed;
                case "VICTORY": return BattlePoseId011.Victory;
                default: return BattlePoseId011.IdleReady;
            }
        }
    }
}
