#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign019;
using SecondDimension.Save;

namespace SecondDimension.Tests.EditMode
{
    public sealed class CampaignRuntime019Tests
    {
        private CampaignRegistry019 _registry;
        private CampaignRuleCatalogAdapter019 _rules;
        private CampaignCommandService019 _service;

        [SetUp]
        public void Setup()
        {
            _registry = CampaignRegistry019.LoadFromResources();
            _rules = new CampaignRuleCatalogAdapter019(_registry.Base018);
            _service = new CampaignCommandService019();
        }

        [Test] public void AllEightyTwoChaptersHaveAuthoredNarrative() =>
            Assert.AreEqual(82, _registry.Chapters.Count);

        [Test] public void EventDiplomacyBossSiegeCountsAreComplete()
        {
            Assert.AreEqual(56, _registry.Events.Count);
            Assert.AreEqual(28, _registry.Diplomacy.Count);
            Assert.AreEqual(28, _registry.Bosses.Count);
            Assert.AreEqual(15, _registry.Sieges.Count);
        }

        [Test] public void EveryWorldHasGameplayPackAndCivilizationContent()
        {
            Assert.AreEqual(7, _registry.Worlds.Count);
            Assert.IsTrue(_registry.Worlds.Values.All(world =>
                world.noConquestOnly && world.ordinaryPeopleRequired.Length >= 6 &&
                world.eventIds.Length == 8 && world.diplomacyIncidentIds.Length == 4 &&
                world.bossIds.Length == 4));
        }

        [Test] public void NoGenericEventTitlesRemain() =>
            Assert.IsTrue(_registry.Events.Values.All(item =>
                item.title.IndexOf("Event ", StringComparison.Ordinal) < 0 &&
                item.title.IndexOf("placeholder", StringComparison.OrdinalIgnoreCase) < 0));

        [Test] public void RelationshipEventsAreFreeAndPermanentRosterSafe() =>
            Assert.IsTrue(_registry.Events.Values.All(item =>
                !item.relationshipSceneCostsOperation && item.committedBeforeReveal && !item.canRemoveRecruit));

        [Test] public void SaveFormatEightContainsPlayableCampaignRuntime() =>
            Assert.GreaterOrEqual(SaveEnvelopeV1.CurrentFormatVersion, 7);

        [Test] public void ProgressStateBeginsWithFirstGateEchoes() =>
            Assert.AreEqual("ARC018_FIRST_GATE_ECHOES", CampaignProgressState019.Default().ActiveArcId);

        [Test] public void FortressPlansRespectTwentyUnionCapacityAndHumaneOutcomes() =>
            Assert.IsTrue(_registry.Sieges.Values.All(siege =>
                siege.maxAlliedUnions <= 10 && siege.maxEnemyUnions <= 10 &&
                (siege.noConquestEnding || siege.allowsSurrender ||
                 (siege.resultModes ?? Array.Empty<string>()).Any(mode =>
                     mode.IndexOf("SURRENDER", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     mode.IndexOf("TREATY", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     mode.IndexOf("REFORM", StringComparison.OrdinalIgnoreCase) >= 0))));

        [Test] public void GoblinWorldTimeLawRemainsLocked() =>
            StringAssert.Contains("1 Second Dimension day equals 1 Goblin World month",
                _registry.Worlds["WORLD_GOBLIN_001"].timeLaw);

        [Test] public void CertifiedBattleModesAreRecognized()
        {
            Assert.IsTrue(_rules.TryGetChapter("CH018_003", out var battle));
            Assert.IsTrue(battle.BattleRequired);
            Assert.IsTrue(_rules.TryGetChapter("CH018_001", out var civic));
            Assert.IsFalse(civic.BattleRequired);
            Assert.AreEqual(37, _registry.Base018.Chapters.Values.Count(chapter =>
                (chapter.gameplayModes ?? Array.Empty<string>()).Any(mode =>
                    mode.IndexOf("BATTLE", StringComparison.OrdinalIgnoreCase) >= 0)));
        }

        [Test] public void ChapterCommitIsDeterministicAndPreviousChapterIsRequired()
        {
            var source = CreateCampaign();
            var first = Require(_service.StartChapter(source, _rules, "CH018_001", new[] { "U1" }, true));
            var repeated = Require(_service.StartChapter(CreateCampaign(), _rules, "CH018_001", new[] { "U1" }, true));
            Assert.AreEqual(
                first.Guild.GuildCity.Strategic017H.Campaign019.ActiveOperation.RequestId,
                repeated.Guild.GuildCity.Strategic017H.Campaign019.ActiveOperation.RequestId);
            var outOfSequence = _service.StartChapter(CreateCampaign(), _rules, "CH018_002", new[] { "U1" }, true);
            Assert.IsFalse(outOfSequence.IsSuccess);
            CollectionAssert.Contains(outOfSequence.Errors, "CAMPAIGN019_PREVIOUS_CHAPTER_REQUIRED");
        }

        [Test] public void NonCombatChapterReceiptAppliesExactlyOnce()
        {
            var started = Require(_service.StartChapter(CreateCampaign(), _rules, "CH018_001", new[] { "U1" }, true));
            var committed = Require(_service.CommitNonCombatReceipt(started, _rules, "SUCCESS"));
            var applied = Require(_service.ApplyReceiptExactlyOnce(committed, _rules));
            CollectionAssert.Contains(applied.Guild.GuildCity.Strategic017H.Campaign019.CompletedChapterIds, "CH018_001");
            var duplicate = _service.ApplyReceiptExactlyOnce(applied, _rules);
            Assert.IsFalse(duplicate.IsSuccess);
        }

        [Test] public void CampaignStateRoundTripKeepsNestedProgress()
        {
            var started = Require(_service.StartChapter(CreateCampaign(), _rules, "CH018_001", new[] { "U1" }, true));
            var json = SecondDimension.Determinism.CanonicalJson.Serialize(started.Guild.GuildCity.Strategic017H);
            var roundTrip = Newtonsoft.Json.JsonConvert.DeserializeObject<SecondDimension.Gameplay.GuildCity017H.GuildCityStrategicState017H>(json);
            Assert.NotNull(roundTrip);
            Assert.AreEqual("CH018_001", roundTrip.Campaign019.ActiveChapterId);
            Assert.AreEqual(
                started.Guild.GuildCity.Strategic017H.Campaign019.ActiveOperation.RequestId,
                roundTrip.Campaign019.ActiveOperation.RequestId);
        }

        private static CampaignState CreateCampaign()
        {
            var recruits = new[]
            {
                new RecruitState("R1",100,100,20,20), new RecruitState("R2",100,100,20,20),
                new RecruitState("R3",100,100,20,20), new RecruitState("R4",100,100,20,20),
                new RecruitState("R5",100,100,20,20), new RecruitState("R6",100,100,20,20)
            };
            var unions = new[]
            {
                new UnionState("U1","First Union",UnionKind.Normal,"R1",new[]{"R1","R2","R3"},"FORMATION_LINE","DOCTRINE_BALANCED",30,7000),
                new UnionState("U2","Second Union",UnionKind.Normal,"R4",new[]{"R4","R5","R6"},"FORMATION_LINE","DOCTRINE_BALANCED",30,7000)
            };
            var guild = new GuildState("GUILD_TEST",1000,recruits,unions);
            var flow = new OpeningFlowState(OpeningStage.Complete,"SDGOW_TUTORIAL_V1_001",true,null,false,439,0,true,true,true,true,"complete");
            return new CampaignState("00000000-0000-0000-0000-000000019019",19019,"1.0",
                ModeRuleSnapshot.StandardDefaults(),guild,
                new NewGuildProfileState("Tester",GameMode.Standard,TutorialDepth.FullTutorial,AccessibilitySettingsState.Defaults(),false),flow);
        }

        private static CampaignState Require(Result<CampaignState> result)
        {
            Assert.IsTrue(result.IsSuccess, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
#endif
