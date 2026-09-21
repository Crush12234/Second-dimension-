using System;
namespace SecondDimension.Presentation.Battle.ArtProduction011 {
[Serializable] public sealed class BattleArtManifest011 { public string contentVersion; public BattleArtCanvas011 canvas; public CharacterPoseSet011[] characters; public VfxAsset011[] vfx; public IconAsset011[] icons; public MotionProfile011[] motionProfiles; public BattleArtLaws011 laws; }
[Serializable] public sealed class BattleArtCanvas011 { public int width; public int height; public float[] characterPivot; }
[Serializable] public sealed class CharacterPoseSet011 { public string stableId; public string displayName; public string side; public string role; public string weaponFamily; public string orientation; public PoseAsset011[] poses; }
[Serializable] public sealed class PoseAsset011 { public string poseId; public string resourcesPath; public string productionStatus; public float[] pivot; public int[] canvas; }
[Serializable] public sealed class VfxAsset011 { public string assetId; public string category; public string school; public string weaponFamily; public string resourcesPath; public string productionStatus; }
[Serializable] public sealed class IconAsset011 { public string assetId; public string semantic; public string resourcesPath; public string productionStatus; }
[Serializable] public sealed class MotionProfile011 { public string profileId; public float anticipation; public float approach; public float contact; public float consequence; public float recovery; }
[Serializable] public sealed class BattleArtLaws011 { public bool presentationOnly; public bool changesBattleResolution; public bool individualArtSelectableInStandard; public int supportsAlliedUnionSlots; public int supportsEnemyUnionSlots; public bool runtimeGenerativeAI; }
public enum BattlePoseId011 { IdleReady, Anticipation, PrimaryAction, RoleAction, GuardCastSupport, HitReaction, Downed, Victory }
}