#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation.Release023;
using SecondDimension.Save;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FinalGameReleaseCandidate023Tests
    {
        FinalReleaseRegistry023 _registry;
        FinalReleaseContentSnapshot023 _snapshot;
        [SetUp] public void Setup() { _registry = FinalReleaseRegistry023.LoadFromResources(); _snapshot = FinalReleaseReadinessService023.BuildSnapshot(); }

        [Test] public void CurrentSaveFormatIncludesReleaseCandidateState() => Assert.GreaterOrEqual(SaveEnvelopeV1.CurrentFormatVersion, 10);
        [Test] public void CompleteContentSnapshotLoads() { Assert.IsTrue(_snapshot.IsAvailable, _snapshot.Error); Assert.AreEqual(30, _snapshot.CityBuildings); Assert.AreEqual(300, _snapshot.SignatureRecruits); Assert.AreEqual(240, _snapshot.ArtBindings); }
        [Test] public void ActiveAndFutureCampaignBranchesAreReconciled() { Assert.AreEqual(82, _snapshot.ActiveCampaignChapters); Assert.AreEqual(120, _snapshot.FutureQuests); Assert.AreEqual(24, _snapshot.FutureFortresses); Assert.AreEqual(210, _snapshot.FutureEncounters); }
        [Test] public void WorldGateRuntimeCoversAllOperations() { Assert.AreEqual(130, _snapshot.WorldGateBoards); Assert.AreEqual(1372, _snapshot.WorldGateNodes); Assert.AreEqual(8, _snapshot.WorldGateTravelRoutes); Assert.AreEqual(8, _snapshot.WorldGateStandingSystems); Assert.AreEqual(8, _snapshot.WorldGateRecruitUnlockSets); }
        [Test] public void EndgameContentIsComplete() { Assert.AreEqual(12, _snapshot.WeaponTracks); Assert.AreEqual(72, _snapshot.WeaponRecipes); Assert.AreEqual(144, _snapshot.ArmorRecipes); Assert.AreEqual(10, _snapshot.AbyssFloors); Assert.AreEqual(40, _snapshot.AbyssOperations); Assert.AreEqual(8, _snapshot.GreatCovenants); }
        [Test] public void SmokeMatrixCoversEveryMajorLoop() { Assert.AreEqual(30, _registry.SmokeCases.Count); var cats=_registry.SmokeCases.Values.Select(x=>x.category).Distinct().ToArray(); CollectionAssert.IsSubsetOf(new[]{"RECRUITMENT","CITY","BATTLE","CAMPAIGN","PROGRESSION","ABYSS","COVENANTS","PERSISTENCE","DELIVERY"},cats); }
        [Test] public void AllSmokeCasesHaveEvidenceAndSystems() => Assert.IsTrue(_registry.SmokeCases.Values.All(x=>x.requiredForOwnerReview&&!string.IsNullOrWhiteSpace(x.expectedEvidence)&&x.requiredSystems!=null&&x.requiredSystems.Length>0));
        [Test] public void TwentyUnionCapacityRemainsLocked() { Assert.AreEqual(10,_snapshot.AlliedUnionCapacity); Assert.AreEqual(10,_snapshot.EnemyUnionCapacity); }
        [Test] public void ReleaseThresholdRequiresOwnerApproval() { Assert.GreaterOrEqual(_registry.Acceptance.minimumOwnerAverage,9f); Assert.GreaterOrEqual(_registry.Acceptance.minimumCategoryScore,8.5f); Assert.IsTrue(_registry.Acceptance.requiresOwnerApproval); }
    }
}
#endif
