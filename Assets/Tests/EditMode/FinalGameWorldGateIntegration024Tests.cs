#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.Release023;
using SecondDimension.Presentation.Release024;
using SecondDimension.Save;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FinalGameWorldGateIntegration024Tests
    {
        private FullGameIntegrationRegistry024 _registry;
        private FullGameIntegrationHealthSnapshot024 _health;
        private CampaignRegistry023 _worldGate;

        [SetUp]
        public void Setup()
        {
            _registry = FullGameIntegrationRegistry024.LoadFromResources();
            _health = FullGameIntegrationHealthService024.BuildSnapshot();
            _worldGate = CampaignRegistry023.LoadFromResources();
        }

        [Test]
        public void SaveFormatIncludesTheWorldGateSchema()
        {
            Assert.GreaterOrEqual(SaveEnvelopeV1.CurrentFormatVersion, 10);
            Assert.AreEqual(SaveEnvelopeV1.CurrentFormatVersion, _registry.Manifest.saveFormatVersion);
            Assert.AreEqual(SaveEnvelopeV1.CurrentFormatVersion, FinalReleaseRegistry023.LoadFromResources().Manifest.saveFormatVersion);
        }

        [Test]
        public void FinalReleaseAndWorldGateAreBothReady()
        {
            Assert.IsTrue(_health.IsReady, _health.Error);
            Assert.AreEqual(30, _health.CityBuildings);
            Assert.AreEqual(240, _health.ArtBindings);
            Assert.AreEqual(10, _health.AlliedUnionCapacity);
            Assert.AreEqual(10, _health.EnemyUnionCapacity);
        }

        [Test]
        public void EveryOperationFamilyHasACommittedBoard()
        {
            Assert.AreEqual(130, _worldGate.Boards.Count);
            Assert.AreEqual(82, _worldGate.Boards.Values.Count(value => value.operationKind == "CHAPTER"));
            Assert.AreEqual(32, _worldGate.Boards.Values.Count(value => value.operationKind == "REPEATABLE"));
            Assert.AreEqual(16, _worldGate.Boards.Values.Count(value => value.operationKind == "CRISIS"));
            Assert.AreEqual(1372, _worldGate.Boards.Values.Sum(value => value.nodes.Length));
        }

        [Test]
        public void WorldGateStateIsEmbeddedInThePlayableCampaignState()
        {
            var state = CampaignPlayableState020.Default();
            Assert.IsNotNull(state.WorldGate023);
            Assert.AreEqual("SKYHOME", state.WorldGate023.CurrentWorldId);
        }

        [Test]
        public void TravelStandingAndRecruitUnlockCoverageIsComplete()
        {
            Assert.AreEqual(8, _worldGate.Travel.Count);
            Assert.AreEqual(8, _worldGate.Standing.Count);
            Assert.AreEqual(8, _worldGate.RecruitUnlocks.Count);
            Assert.IsTrue(_worldGate.RecruitUnlocks.Values.All(value =>
                value.usesRecruitAutogen010 &&
                value.permanentWhenSigned &&
                !value.runtimeGenerativeAi));
        }

        [Test]
        public void AllBoardsPreserveTheCertifiedBattleAndRecoveryLaws()
        {
            Assert.IsTrue(_worldGate.Boards.Values.All(board =>
                board.maximumAlliedUnions <= 10 &&
                board.maximumEnemyUnions <= 10 &&
                board.nodes.All(node =>
                    node.committedBeforeReveal &&
                    node.exactOnce &&
                    node.failureCreatesRecovery)));
        }

        [Test]
        public void OwnerReviewMatrixCoversTheMergedRuntime()
        {
            Assert.AreEqual(36, _registry.SmokeCases.Count);
            CollectionAssert.IsSubsetOf(
                new[]
                {
                    "FOUNDATION", "RECRUITMENT", "CITY", "BATTLE",
                    "WORLD_GATE", "PERSISTENCE", "USABILITY", "DELIVERY"
                },
                _registry.SmokeCases.Values
                    .Select(value => value.category)
                    .Distinct()
                    .ToArray());
        }

        [Test]
        public void ExistingEquipmentRewardRemainsTheOnlyItemReward()
        {
            Assert.AreEqual(
                "EXISTING_M2_REWARD_RECEIPT",
                _worldGate.RewardPolicy.battleEquipmentRewardAuthority);
        }
    }
}
#endif
