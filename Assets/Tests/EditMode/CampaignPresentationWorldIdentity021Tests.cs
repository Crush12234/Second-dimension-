#if UNITY_EDITOR
using System.Linq; using NUnit.Framework; using SecondDimension.Presentation.Campaign021; using SecondDimension.Save;
namespace SecondDimension.Tests.EditMode
{
 public sealed class CampaignPresentationWorldIdentity021Tests
 {
  CampaignPresentationRegistry021 _r; [SetUp] public void Setup()=>_r=CampaignPresentationRegistry021.LoadFromResources();
  [Test] public void ContentCountsAreComplete(){Assert.AreEqual(8,_r.Worlds.Count);Assert.AreEqual(64,_r.Enemies.Count);Assert.AreEqual(28,_r.Bosses.Count);Assert.AreEqual(56,_r.Recruits.Count);Assert.AreEqual(96,_r.Loot.Count);Assert.AreEqual(48,_r.Materials.Count);Assert.AreEqual(48,_r.Operations.Count);Assert.AreEqual(8,_r.Travel.Count);Assert.AreEqual(9,_r.Settlements.Count);Assert.AreEqual(82,_r.Chapters.Count);Assert.AreEqual(60,_r.Audio.Count);}
  [Test] public void PresentationDoesNotRequireSaveMigration()=>Assert.GreaterOrEqual(SaveEnvelopeV1.CurrentFormatVersion,8);
  [Test] public void EnemiesPreserveArtAndCivilizationLaw()=>Assert.IsTrue(_r.Enemies.Values.All(x=>x.usesExistingArtAuthority&&x.noRacewideEvilFraming&&!string.IsNullOrWhiteSpace(x.poseFamily)));
  [Test] public void BossesUseCertifiedBattleAndAllowNonExecution()=>Assert.IsTrue(_r.Bosses.Values.All(x=>x.usesCertifiedUnionBattle&&x.executionNeverRequired&&x.phases.Length>=2&&x.postBattleResolutions.Length>=4));
  [Test] public void RecruitOriginsUseAutogenAndNeverBorrowIdentity()=>Assert.IsTrue(_r.Recruits.Values.All(x=>x.usesRecruitAutogen010&&x.permanentWhenSigned&&!x.runtimeGenerativeAi&&!x.borrowsNamedCharacterIdentity));
  [Test] public void LootUsesExistingRewardAndManualEquip()=>Assert.IsTrue(_r.Loot.Values.All(x=>x.manualEquipOnly&&!x.createsDuplicateItemCatalog&&x.existingRewardAuthority.Contains("EXISTING")));
  [Test] public void ChapterBindingsCoverEveryBlueprint()=>Assert.IsTrue(_r.Chapters.Keys.All(x=>_r.Base020.Blueprints.ContainsKey(x)));
  [Test] public void AdapterResolvesAllEnemyAndBossIds(){var a=new CampaignBattlePresentationAdapter021(_r);Assert.IsTrue(_r.Enemies.Keys.All(x=>a.TryResolveEnemy(x,out _)));Assert.IsTrue(_r.Bosses.Keys.All(x=>a.TryResolveBoss(x,out _)));}
 }
}
#endif
