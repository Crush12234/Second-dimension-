#if UNITY_EDITOR
using System; using UnityEditor; using UnityEngine;
namespace SecondDimension.Editor.Campaign021
{
 public static class Campaign021ValidationMenu
 {
  [MenuItem("Second Dimension/Campaign 021/Validate Presentation & World Identity")]
  public static void Validate(){var r=SecondDimension.Presentation.Campaign021.CampaignPresentationRegistry021.LoadFromResources();foreach(var w in r.Worlds.Values){if(SecondDimension.Presentation.Campaign021.Campaign021AssetResolver.LoadTexture(w.backgroundResource)==null)throw new InvalidOperationException("Missing world background: "+w.worldId);if(SecondDimension.Presentation.Campaign021.Campaign021AssetResolver.LoadTexture(w.frameResource)==null)throw new InvalidOperationException("Missing world frame: "+w.worldId);}foreach(var e in r.Enemies.Values)if(SecondDimension.Presentation.Campaign021.Campaign021AssetResolver.LoadTexture(e.emblemResource)==null)throw new InvalidOperationException("Missing enemy emblem: "+e.archetypeId);foreach(var a in r.Audio.Values)if(SecondDimension.Presentation.Campaign021.Campaign021AssetResolver.LoadAudio(a.resourcePath)==null)throw new InvalidOperationException("Missing audio: "+a.cueId);Debug.Log("Campaign 021 validation PASS — worlds "+r.Worlds.Count+", enemies "+r.Enemies.Count+", bosses "+r.Bosses.Count+", assets/audio bound.");}
 }
}
#endif
