using UnityEngine;
namespace SecondDimension.Presentation.Campaign021
{
 public static class Campaign021AssetResolver
 {
  public static Texture2D LoadTexture(string resourcePath)=>string.IsNullOrWhiteSpace(resourcePath)?null:Resources.Load<Texture2D>(resourcePath);
  public static AudioClip LoadAudio(string resourcePath)=>string.IsNullOrWhiteSpace(resourcePath)?null:Resources.Load<AudioClip>(resourcePath);
 }
 public sealed class CampaignCombatantPresentationPlan021
 {
  public string StableId; public string WorldId; public string DisplayName; public string EmblemResource; public string PoseFamily; public string WeaponAnimationFamily; public string MysticPresentationFamily; public string VfxFamily; public string AudioCueId; public string CameraProfile; public string LodPolicy; public bool IsBoss; public string ProductionStatus;
 }
 public sealed class CampaignBattlePresentationAdapter021
 {
  readonly CampaignPresentationRegistry021 _r; public CampaignBattlePresentationAdapter021(CampaignPresentationRegistry021 registry){_r=registry;}
  public bool TryResolveEnemy(string archetypeId,out CampaignCombatantPresentationPlan021 p){p=null;if(!_r.Enemies.TryGetValue(archetypeId,out var e))return false;p=new CampaignCombatantPresentationPlan021{StableId=e.archetypeId,WorldId=e.worldId,DisplayName=e.displayName,EmblemResource=e.emblemResource,PoseFamily=e.poseFamily,WeaponAnimationFamily=e.weaponAnimationFamily,MysticPresentationFamily=e.mysticPresentationFamily,VfxFamily=e.vfxFamily,AudioCueId=e.audioCueId,CameraProfile=e.cameraProfile,LodPolicy=e.lodPolicy,IsBoss=false,ProductionStatus=e.productionStatus};return true;}
  public bool TryResolveBoss(string bossId,out CampaignCombatantPresentationPlan021 p){p=null;if(!_r.Bosses.TryGetValue(bossId,out var b))return false;p=new CampaignCombatantPresentationPlan021{StableId=b.bossId,WorldId=b.worldId,DisplayName=b.displayName,EmblemResource=b.emblemResource,PoseFamily="BOSS_AUTHORED_PHASE",WeaponAnimationFamily="RESOLVE_FROM_EXISTING_ART_OR_EVENT",MysticPresentationFamily="RESOLVE_FROM_EXISTING_ART_OR_EVENT",VfxFamily=b.worldId+"_BOSS",AudioCueId=b.introAudioCueId,CameraProfile="BOSS_PHASE_AUTHORITY",LodPolicy="HERO_ALWAYS_DURING_BOSS_PHASE",IsBoss=true,ProductionStatus=b.productionStatus};return true;}
  public WorldPresentationTheme021 ResolveWorld(string worldId)=>_r.Worlds.TryGetValue(worldId,out var x)?x:null;
  public AudioClip ResolveCue(string cueId)=>_r.Audio.TryGetValue(cueId,out var x)?Campaign021AssetResolver.LoadAudio(x.resourcePath):null;
 }
}
