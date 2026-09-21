#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    // Actual coordinator/presenter methods and AtomicSaveStore, with explicitly
    // synthetic battle-return fixtures. This is not a rendered combat playthrough.
    public sealed class TowerCoordinator094Tests
    {
        TowerHeroRewards094Tests _authorityFixture;
        CampaignRegistry022 _registry;
        CampaignState _readyTenth;
        CampaignState _legacyTwelveClears;
        string _testDirectory;
        string _savePath;
        static string ContentRoot => Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");

        [OneTimeSetUp]
        public void PrepareCertifiedCommandFixturesWithSyntheticVictoryReturns()
        {
            _authorityFixture = new TowerHeroRewards094Tests();
            _authorityFixture.SetUp();
            _registry = CampaignRegistry022.LoadFromResources();
            var service = new CampaignProgressionCommandService022();
            var campaign = FreshCampaign();
            for (var floor = 1; floor < 10; floor++)
                campaign = Fixture<CampaignState>("CompleteNewFloor", campaign, floor);
            var begun = service.BeginTowerFloor094(campaign, _registry);
            Assert.That(begun.IsSuccess, Is.True, string.Join("\n", begun.Errors));
            _readyTenth = Fixture<CampaignState>("CommitActiveFloor", begun.Value, "COORDINATOR_TENTH_094");
            _legacyTwelveClears = FreshCampaign();
            for (var ordinal = 1; ordinal <= 12; ordinal++)
                _legacyTwelveClears = Legacy<CampaignState>("CompleteNextTowerFloor089",
                    _legacyTwelveClears, service, _registry, ordinal);
        }

        [SetUp]
        public void CreateIsolatedSaveLocation()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "SecondDimensionTower094_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDirectory);
            _savePath = Path.Combine(_testDirectory, "coordinator.json");
        }

        [TearDown]
        public void RemoveOnlyThisFixtureSaveFiles()
        {
            foreach (var suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(_savePath + suffix)) File.Delete(_savePath + suffix);
            if (Directory.Exists(_testDirectory)) Directory.Delete(_testDirectory, false);
        }

        [Test]
        public void PublicBeginShowsActualElevenAndTemplateOneAfterHistoricalRepeats094()
        {
            var coordinator = Coordinator(_legacyTwelveClears);
            var lobby = coordinator.CampaignProgression022;
            Assert.That(lobby.IsAvailable, Is.True, lobby.Error);
            Assert.That(lobby.TowerAuthorityError094, Is.Empty);
            Assert.That(lobby.TotalTowerClears, Is.EqualTo(12));
            Assert.That(lobby.HighestClearedTowerFloor, Is.EqualTo(10));
            Assert.That(lobby.TowerFloorNumber, Is.EqualTo(11));
            Assert.That(lobby.TowerContentTemplateFloor094, Is.EqualTo(1));
            Assert.That(lobby.TowerExpectedEnemyUnions, Is.EqualTo(TowerThreatRules098.EnemyUnionCount098(11)),
                "A new floor11 lobby must preview the real098 force, not template1's old count.");
            Assert.That(lobby.TowerArtResourcePath, Is.EqualTo(TowerRunRules081.ArtResourcePath(1)));
            Assert.That(lobby.TowerOperationId, Is.EqualTo("ABYSS_OP094_01_ENDLESS_BATTLE"));
            Require(coordinator.BeginTowerRun081());
            var active = coordinator.CampaignProgression022;
            Assert.That(active.TowerFloorNumber, Is.EqualTo(11));
            Assert.That(active.TowerContentTemplateFloor094, Is.EqualTo(1));
            Assert.That(active.TowerUsesNewFloorPolicy094, Is.True);
            Assert.That(active.TowerExpectedEnemyUnions, Is.EqualTo(lobby.TowerExpectedEnemyUnions));
            Assert.That(active.ActiveAbyssOperationId, Does.StartWith("TOWERRUN094_000011_"));
            var loaded = new AtomicSaveStore().ReadWithRecovery(_savePath);
            Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
            Assert.That(loaded.Value.CanonicalStateHash, Is.EqualTo(CanonicalJson.Sha256Hex(Campaign(coordinator))));
            var restarted = new M1RuntimeCoordinator(ContentRoot, _savePath).CampaignProgression022;
            Assert.That(restarted.TowerFloorNumber, Is.EqualTo(11));
            Assert.That(restarted.TowerContentTemplateFloor094, Is.EqualTo(1));
            Assert.That(restarted.TowerAuthorityError094, Is.Empty);
            Assert.That(restarted.TowerExpectedEnemyUnions, Is.EqualTo(active.TowerExpectedEnemyUnions),
                "Reload preserves the committed floor policy and its force preview.");
            for (var transition = 0; transition < 8 && !coordinator.CampaignProgression022.ActiveStepRequiresBattle; transition++)
                Require(coordinator.AdvanceTowerRun081());
            Assert.That(coordinator.CampaignProgression022.ActiveStepRequiresBattle, Is.True,
                "Actual floor11 must stop at its certified Union battle, not auto-clear nonbattle trial rooms.");
            Assert.That(coordinator.CampaignProgression022.HighestClearedTowerFloor, Is.EqualTo(10));
            Assert.That(coordinator.AdvanceTowerRun081().Succeeded, Is.False,
                "The normal advance command cannot replace the required floor11 battle with a success receipt.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BothPublicFinalizersUseRecruitmentAuthorityAndSaveExactTenthOutcome094(bool genericFinalizer)
        {
            var coordinator = Coordinator(_readyTenth);
            var before = coordinator.CampaignProgression022;
            Assert.That(before.TowerRewardSummary, Does.Contain("1% SS HERO CHANCE"));
            Assert.That(before.TowerRewardSummary, Does.Not.Contain("S-RANK RECRUIT LEAD"));
            Require(genericFinalizer ? coordinator.FinalizeAbyssOperation022() : coordinator.AdvanceTowerRun081());
            var campaign = Campaign(coordinator);
            var projected = coordinator.CampaignProgression022;
            Assert.That(projected.TowerAuthorityError094, Is.Empty);
            Assert.That(projected.HighestClearedTowerFloor, Is.EqualTo(10));
            Assert.That(projected.TowerFloorNumber, Is.EqualTo(11));
            Assert.That(projected.TowerContentTemplateFloor094, Is.EqualTo(1));
            Assert.That(projected.TowerLastHeroRewardFloor094, Is.EqualTo(10));
            Assert.That(projected.TowerLastHeroRewardSummary094, Is.Not.Empty);
            var records = campaign.Guild.Development.AppliedAdventureAuthorityIds.Where(value =>
                value.StartsWith(GuildCityRecruitmentService017D.TowerHeroResultPrefix094, StringComparison.Ordinal)).ToArray();
            Assert.That(records, Has.Length.EqualTo(1));
            Assert.That(projected.TowerLastHeroRewardWon094, Is.EqualTo(!records[0].Contains("|NO_HERO|")));
            var savedHeroId = records[0].Substring(GuildCityRecruitmentService017D.TowerHeroResultPrefix094.Length).Split('|')[8];
            if (savedHeroId == "NO_HERO")
                Assert.That(projected.TowerLastHeroRewardSummary094, Does.StartWith("No bonus hero this time."));
            else
            {
                var earnedHero = campaign.Guild.Recruits.Single(value =>
                    value.AuthoredStableRecruitId == savedHeroId || value.RecruitId == savedHeroId);
                Assert.That(projected.TowerLastHeroRewardSummary094, Does.StartWith("SS HERO • " + earnedHero.DisplayName));
            }
            var loaded = new AtomicSaveStore().ReadWithRecovery(_savePath);
            Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
            Assert.That(loaded.Value.CanonicalStateHash, Is.EqualTo(CanonicalJson.Sha256Hex(campaign)));
            var restarted = new M1RuntimeCoordinator(ContentRoot, _savePath);
            var reprojected = restarted.CampaignProgression022;
            Assert.That(reprojected.TowerLastHeroRewardSummary094, Is.EqualTo(projected.TowerLastHeroRewardSummary094));
            var beforeReplay = CanonicalJson.Sha256Hex(Campaign(restarted));
            Assert.That(restarted.AdvanceTowerRun081().Succeeded, Is.False);
            Assert.That(CanonicalJson.Sha256Hex(Campaign(restarted)), Is.EqualTo(beforeReplay));

            RenderTower(restarted, body => {
                var text = string.Join("\n", body.GetComponentsInChildren<Text>(true).Select(value => value.text));
                Assert.That(text, Does.Contain("FLOOR 10 • HERO REWARD SAVED"));
                Assert.That(text, Does.Contain(projected.TowerLastHeroRewardSummary094));
                Assert.That(text, Does.Contain("FIGHT FLOOR 11"));
                Assert.That(body.GetComponentsInChildren<Button>(true).Any(value =>
                    value.name.StartsWith("Begin authored Tower route", StringComparison.Ordinal)), Is.False);
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingDependencyDoesNotConsumeCommittedRewardThroughEitherFinalizer094(bool genericFinalizer)
        {
            var coordinator = Coordinator(_readyTenth);
            Field("_guildCityRecruitment").SetValue(coordinator, null);
            var before = CanonicalJson.Sha256Hex(Campaign(coordinator));
            var result = genericFinalizer ? coordinator.FinalizeAbyssOperation022() : coordinator.AdvanceTowerRun081();
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Is.EqualTo("Tower094 Recruitment Authority Required."));
            Assert.That(CanonicalJson.Sha256Hex(Campaign(coordinator)), Is.EqualTo(before));
            Assert.That(File.Exists(_savePath), Is.False, "No failed reward transaction may replace the save.");
        }

        [Test]
        public void InvalidBindingIsHeldByActualCoordinatorAndRenderedWithoutFightButton094()
        {
            var service = new CampaignProgressionCommandService022();
            var begun = service.BeginTowerFloor094(FreshCampaign(), _registry);
            Assert.That(begun.IsSuccess, Is.True);
            var invalid = (CampaignState)typeof(TowerHeroRewards094Tests).GetMethod("WithoutBindings",
                BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { begun.Value });
            var coordinator = Coordinator(invalid);
            Assert.That(coordinator.CampaignProgression022.TowerAuthorityError094, Is.Not.Empty);
            var before = CanonicalJson.Sha256Hex(Campaign(coordinator));
            Assert.That(coordinator.BeginTowerRun081().Succeeded, Is.False);
            Assert.That(CanonicalJson.Sha256Hex(Campaign(coordinator)), Is.EqualTo(before));
            RenderTower(coordinator, body => {
                var text = string.Join("\n", body.GetComponentsInChildren<Text>(true).Select(value => value.text));
                Assert.That(text, Does.Contain("TOWER RECORD HELD SAFELY"));
                Assert.That(body.GetComponentsInChildren<Button>(true).Any(value =>
                    value.name == "Climb next Tower floor 084" || value.name == "Enter Tower battle 081"), Is.False);
            });
        }

        M1RuntimeCoordinator Coordinator(CampaignState campaign)
        {
            var coordinator = new M1RuntimeCoordinator(ContentRoot, _savePath);
            Field("_campaign").SetValue(coordinator, campaign);
            return coordinator;
        }

        static void RenderTower(M1RuntimeCoordinator coordinator, Action<GameObject> assertions)
        {
            var presenterObject = new GameObject("Tower Coordinator Presenter 094");
            var body = new GameObject("Tower Coordinator Body 094", typeof(RectTransform));
            try
            {
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                typeof(M1FlowPresenter).GetField("_reducedMotion", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(presenter, true);
                typeof(M1FlowPresenter).GetMethod("BuildGuildCityAbyss022", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(presenter, new object[] { body.transform, coordinator, coordinator.CampaignProgression022 });
                assertions(body);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(body);
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        T Fixture<T>(string method, params object[] args) => (T)typeof(TowerHeroRewards094Tests)
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_authorityFixture, args);
        static T Legacy<T>(string method, params object[] args) => (T)typeof(TowerRun081EditModeTests)
            .GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
        static CampaignState FreshCampaign() => Legacy<CampaignState>("CreateTowerCampaign");
        static FieldInfo Field(string name) => typeof(M1RuntimeCoordinator).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        static CampaignState Campaign(M1RuntimeCoordinator coordinator) => (CampaignState)Field("_campaign").GetValue(coordinator);
        static void Require(M1CommandResult result) => Assert.That(result.Succeeded, Is.True, result.Message);
    }
}
#endif
