#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Save;
namespace SecondDimension.Tests.EditMode
{
 public sealed class CampaignWorldGateOperations023Tests
 {
  CampaignRegistry023 _r; [SetUp] public void Setup()=>_r=CampaignRegistry023.LoadFromResources();
  [Test] public void ContentCountsMatchAuthority(){Assert.AreEqual(130,_r.Boards.Count);Assert.AreEqual(8,_r.Travel.Count);Assert.AreEqual(8,_r.Standing.Count);Assert.AreEqual(8,_r.RecruitUnlocks.Count);Assert.AreEqual(1372,_r.Boards.Values.Sum(x=>x.nodes.Length));}
  [Test] public void CurrentSaveFormatIncludesWorldGateState()=>Assert.GreaterOrEqual(SaveEnvelopeV1.CurrentFormatVersion,10);
  [Test] public void EveryBoardCommitsBeforeRevealAndReturns()=>Assert.IsTrue(_r.Boards.Values.All(b=>b.nodes.All(n=>n.committedBeforeReveal&&n.exactOnce&&n.failureCreatesRecovery)&&b.nodes.Any(n=>n.nodeId==b.startNodeId)&&b.nodes.Any(n=>n.nodeId==b.exitNodeId)));
  [Test] public void BattleNodesRespectUnionCapAndExistingReward()=>Assert.IsTrue(_r.Boards.Values.SelectMany(b=>b.nodes).Where(n=>n.requiresCertifiedBattle).All(n=>n.enemyUnionCount>=1&&n.enemyUnionCount<=10));
  [Test] public void AllStoryChaptersHaveBoards()=>Assert.AreEqual(82,_r.Boards.Values.Count(x=>x.operationKind=="CHAPTER"));
  [Test] public void RepeatablesAndCrisesHaveBoards(){Assert.AreEqual(32,_r.Boards.Values.Count(x=>x.operationKind=="REPEATABLE"));Assert.AreEqual(16,_r.Boards.Values.Count(x=>x.operationKind=="CRISIS"));}
  [Test] public void RecruitmentRemainsPermanentAndOffline()=>Assert.IsTrue(_r.RecruitUnlocks.Values.All(x=>x.usesRecruitAutogen010&&!x.runtimeGenerativeAi&&x.permanentWhenSigned));
  [Test] public void RewardPolicyPreventsImmediateFarmLoop(){Assert.AreEqual(new[]{10000,7000,4000,0},_r.RewardPolicy.repeatableConsecutiveRewardBasisPoints);Assert.AreEqual("EXISTING_M2_REWARD_RECEIPT",_r.RewardPolicy.battleEquipmentRewardAuthority);}
  [Test] public void DefaultRuntimeStartsAtSkyhome(){var s=WorldGateRuntimeState023.Default();Assert.AreEqual("SKYHOME",s.CurrentWorldId);Assert.AreEqual(30,s.TravelSupplies);Assert.IsNull(s.ActiveOperation);}
 }
}
#endif
