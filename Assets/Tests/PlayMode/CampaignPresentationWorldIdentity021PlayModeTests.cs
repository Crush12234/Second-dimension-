#if UNITY_EDITOR
using System.Collections; using System.Linq; using NUnit.Framework; using UnityEngine; using UnityEngine.TestTools; using SecondDimension.Presentation.Campaign021;
namespace SecondDimension.Tests.PlayMode
{
 public sealed class CampaignPresentationWorldIdentity021PlayModeTests
 {
  [UnityTest] public IEnumerator WorldTexturesAndAudioLoad(){yield return null;var r=CampaignPresentationRegistry021.LoadFromResources();foreach(var w in r.Worlds.Values){Assert.NotNull(Campaign021AssetResolver.LoadTexture(w.backgroundResource),w.worldId);Assert.NotNull(Campaign021AssetResolver.LoadTexture(w.frameResource),w.worldId);}foreach(var a in r.Audio.Values)Assert.NotNull(Campaign021AssetResolver.LoadAudio(a.resourcePath),a.cueId);}
  [UnityTest] public IEnumerator RepresentativeIconsLoadForEveryCategory(){yield return null;var r=CampaignPresentationRegistry021.LoadFromResources();Assert.IsTrue(r.Enemies.Values.All(x=>Campaign021AssetResolver.LoadTexture(x.emblemResource)!=null));Assert.IsTrue(r.Bosses.Values.All(x=>Campaign021AssetResolver.LoadTexture(x.emblemResource)!=null));Assert.IsTrue(r.Recruits.Values.All(x=>Campaign021AssetResolver.LoadTexture(x.emblemResource)!=null));Assert.IsTrue(r.Loot.Values.All(x=>Campaign021AssetResolver.LoadTexture(x.iconResource)!=null));Assert.IsTrue(r.Materials.Values.All(x=>Campaign021AssetResolver.LoadTexture(x.iconResource)!=null));}
 }
}
#endif
