#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using SecondDimension.Presentation.Campaign020;

namespace SecondDimension.Tests.PlayMode
{
 public sealed class CampaignPlayableOperations020PlayModeTests
 {
  [UnityTest] public IEnumerator ResourcesLoadAllPlayableCampaignPacks(){yield return null;var r=CampaignRegistry020.LoadFromResources();Assert.AreEqual(82,r.Blueprints.Count);Assert.AreEqual(32,r.Repeatables.Count);Assert.AreEqual(16,r.Crises.Count);}
  [UnityTest] public IEnumerator EveryBlueprintHasReadableCurrentStepSequence(){yield return null;var r=CampaignRegistry020.LoadFromResources();foreach(var b in r.Blueprints.Values){Assert.GreaterOrEqual(b.steps.Length,4,b.chapterId);Assert.AreEqual("BRIEFING",b.steps[0].kind,b.chapterId);Assert.AreEqual("RESULTS",b.steps[b.steps.Length-1].kind,b.chapterId);}}
 }
}
#endif
