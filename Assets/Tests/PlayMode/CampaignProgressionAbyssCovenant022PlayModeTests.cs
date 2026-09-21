#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using SecondDimension.Presentation.Campaign022;
namespace SecondDimension.Tests.PlayMode
{
 public sealed class CampaignProgressionAbyssCovenant022PlayModeTests
 {
  [UnityTest] public IEnumerator ResourcesLoadAllProgressionPacks(){yield return null;var r=CampaignRegistry022.LoadFromResources();Assert.AreEqual(10,r.Floors.Count);Assert.AreEqual(24,r.ArtifactBases.Count);Assert.AreEqual(8,r.Covenants.Count);}
  [UnityTest] public IEnumerator EveryAbyssFloorPreservesThreeLegacyOperationsAndOneEndlessBattle()
  {
   yield return null;var r=CampaignRegistry022.LoadFromResources();
   foreach(var f in r.Floors.Values)
   {
    var operations=r.AbyssOperations.Values.Where(x=>x.floorId==f.floorId).ToArray();
    Assert.AreEqual(3,operations.Count(x=>x.kind!=SecondDimension.Gameplay.Campaign022.CampaignProgressionCommandService022.EndlessBattleKind094),f.floorId);
    Assert.AreEqual(1,operations.Count(x=>x.kind==SecondDimension.Gameplay.Campaign022.CampaignProgressionCommandService022.EndlessBattleKind094),f.floorId);
   }
  }
 }
}
#endif
