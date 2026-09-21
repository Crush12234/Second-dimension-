#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Save;

namespace SecondDimension.Tests.EditMode
{
 public sealed class CampaignPlayableOperations020Tests
 {
  CampaignRegistry020 _registry;
  [SetUp] public void Setup()=>_registry=CampaignRegistry020.LoadFromResources();
  [Test] public void AllEightyTwoChaptersHavePlayableBlueprints()=>Assert.AreEqual(82,_registry.Blueprints.Count);
  [Test] public void OperationalContentCountsAreComplete(){Assert.AreEqual(8,_registry.EnemyPacks.Count);Assert.AreEqual(7,_registry.RecruitPacks.Count);Assert.AreEqual(8,_registry.LootProfiles.Count);Assert.AreEqual(48,_registry.Materials.Count);Assert.AreEqual(32,_registry.Repeatables.Count);Assert.AreEqual(16,_registry.Crises.Count);}
  [Test] public void EveryBlueprintIsExactOnceAndUsesExistingReward()=>Assert.IsTrue(_registry.Blueprints.Values.All(b=>b.exactOnce&&b.existingEquipmentRewardRemainsAuthoritative&&b.chapterCompletionRequiresAllSteps&&b.steps.All(s=>s.exactOnce&&s.committedBeforeReveal)));
  [Test] public void EveryRegionHasEightEnemyArchetypes()=>Assert.IsTrue(_registry.EnemyPacks.Values.All(p=>p.archetypes.Length==8&&p.archetypes.All(a=>a.noRacewideEvilFraming)));
  [Test] public void EveryRaceWorldHasEightAutogenOrigins()=>Assert.IsTrue(_registry.RecruitPacks.Values.All(p=>p.profiles.Length==8&&p.profiles.All(x=>x.permanentWhenSigned&&x.usesRecruitAutogen010&&!x.runtimeGenerativeAi&&!x.signatureRecruitReplacement)));
  [Test] public void LootUsesExistingAuthorityAndManualEquip()=>Assert.IsTrue(_registry.LootProfiles.Values.All(p=>p.entries.Length==12&&p.exactItemCommittedBeforeResults&&p.noAutoEquip&&p.entries.All(x=>x.manualEquipOnly&&!x.createsDuplicateItemCatalog)));
  [Test] public void RepeatablesAndCrisesPreserveUnionCapacity()=>Assert.IsTrue(_registry.Crises.Values.All(c=>c.maximumAlliedUnions<=10&&c.maximumEnemyUnions<=10));
  [Test] public void CurrentSaveFormatIncludesPlayableOperations()=>Assert.GreaterOrEqual(SaveEnvelopeV1.CurrentFormatVersion,8);
  [Test] public void DefaultStateKeepsSkyhomeOpen()=>Assert.IsTrue(CampaignPlayableState020.Default().WorldGates.Any(x=>x.WorldId=="SKYHOME"&&x.Unlocked));
 }
}
#endif
