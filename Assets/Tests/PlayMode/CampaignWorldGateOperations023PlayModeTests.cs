#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using SecondDimension.Presentation.Campaign023;
namespace SecondDimension.Tests.PlayMode
{
 public sealed class CampaignWorldGateOperations023PlayModeTests
 {
  [UnityTest] public IEnumerator ResourcesLoadInPlayerContext(){var r=CampaignRegistry023.LoadFromResources();Assert.AreEqual(130,r.Boards.Count);yield return null;}
  [UnityTest] public IEnumerator EveryWorldHasTravelStandingAndRecruitAuthority(){var r=CampaignRegistry023.LoadFromResources();foreach(var world in r.Standing.Keys){Assert.IsTrue(r.Travel.ContainsKey(world));Assert.IsTrue(r.RecruitUnlocks.ContainsKey(world));}yield return null;}
 }
}
#endif
