using System;
using System.IO;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.FirstHour072;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FirstHourOpeningState072Tests
    {
        private string _directory;
        private string _savePath;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "SecondDimensionFirstHour072Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _savePath = Path.Combine(_directory, "first_hour_route.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }

        [Test]
        public void RouteMovesForwardAndFounderAcceptanceIsExplicitAndIdempotent()
        {
            var state = FirstHourOpeningState072.New()
                .AdvanceTo(FirstHourOpeningPhase072.MarketArrival, "FH072_TEST_MARKET")
                .AdvanceTo(FirstHourOpeningPhase072.CharterDialogue, "FH072_TEST_CHARTER")
                .WithDialogueBeat(3)
                .WithCampaign("CAMPAIGN_TEST", "Maren")
                .AdvanceTo(FirstHourOpeningPhase072.FounderIntroductions, "FH072_TEST_FOUNDERS")
                .WithAcceptedFounder("FOUNDER_A")
                .WithAcceptedFounder("FOUNDER_A");

            Assert.That(state.Phase, Is.EqualTo(FirstHourOpeningPhase072.FounderIntroductions));
            Assert.That(state.AcceptedFounderIds, Is.EqualTo(new[] { "FOUNDER_A" }));
            Assert.That(state.AllFoundersAccepted, Is.False);
            Assert.Throws<InvalidOperationException>(() => state.AdvanceTo(
                FirstHourOpeningPhase072.Title,
                "FH072_TEST_BACKWARDS"));
            Assert.Throws<InvalidOperationException>(() => FirstHourOpeningState072.New().AdvanceTo(
                FirstHourOpeningPhase072.FounderIntroductions,
                "FH072_TEST_SKIPPED_VISIBLE_ACTIONS"));
        }

        [Test]
        public void UnionCannotLockUntilPlayerChoosesLeaderAndFormation()
        {
            var state = StateAtUnionSetup().MarkUnionDraftCreated();

            Assert.Throws<InvalidOperationException>(() => state.ConfirmUnion(0));
            state = state.WithUnionLeader(0, "FOUNDER_A");
            Assert.Throws<InvalidOperationException>(() => state.ConfirmUnion(0));
            state = state.WithUnionFormation(0, "FORMATION_LINE").ConfirmUnion(0);
            state = state
                .WithUnionLeader(1, "FOUNDER_D")
                .WithUnionFormation(1, "FORMATION_WEDGE")
                .ConfirmUnion(1);

            Assert.That(state.Choice(0).Confirmed, Is.True);
            Assert.That(state.Choice(1).Confirmed, Is.True);
            Assert.That(state.AllUnionsConfirmed, Is.True);
        }

        [Test]
        public void VersionedRouteStateRoundTripsWithImmutableBuildIdentity()
        {
            var expected = FirstHourOpeningState072.New()
                .AdvanceTo(FirstHourOpeningPhase072.MarketArrival, "FH072_TEST_MARKET")
                .AdvanceTo(FirstHourOpeningPhase072.CharterDialogue, "FH072_TEST_HALL")
                .WithDialogueBeat(2)
                .WithDialogueBeat(3)
                .WithCampaign("CAMPAIGN_TEST", "Kiri")
                .AdvanceTo(FirstHourOpeningPhase072.FounderIntroductions, "FH072_TEST_FOUNDERS")
                .WithAcceptedFounder("FOUNDER_A");
            var store = new FirstHourOpeningStateStore072(_savePath);

            store.Save(expected);
            var loaded = store.Load();

            Assert.That(loaded.RecoveredFromBackup, Is.False);
            Assert.That(loaded.State.SaveVersion, Is.EqualTo(FirstHourOpeningState072.CurrentSaveVersion));
            Assert.That(loaded.State.BuildIdentity, Is.EqualTo("FIRST-HOUR-REBUILD-072"));
            Assert.That(loaded.State.Phase, Is.EqualTo(expected.Phase));
            Assert.That(loaded.State.LastCheckpointId, Is.EqualTo(expected.LastCheckpointId));
            Assert.That(loaded.State.CampaignGuid, Is.EqualTo("CAMPAIGN_TEST"));
            Assert.That(loaded.State.AcceptedFounderIds, Is.EqualTo(new[] { "FOUNDER_A" }));
        }

        [Test]
        public void CorruptPrimaryRecoversThePreviousVerifiedCheckpoint()
        {
            var store = new FirstHourOpeningStateStore072(_savePath);
            store.Save(FirstHourOpeningState072.New());
            store.Save(FirstHourOpeningState072.New().AdvanceTo(
                FirstHourOpeningPhase072.MarketArrival,
                "FH072_TEST_MARKET"));
            File.WriteAllText(_savePath, "{ not valid json");

            var loaded = store.Load();

            Assert.That(loaded.RecoveredFromBackup, Is.True);
            Assert.That(loaded.State.Phase, Is.EqualTo(FirstHourOpeningPhase072.Title));
            Assert.That(loaded.State.LastCheckpointId, Is.EqualTo("FH072_CP_00_TITLE"));
        }

        [Test]
        public void DedicatedBattlePresenterImplementsTheNoFakeCompletionContract()
        {
            Assert.That(
                typeof(IFirstHourBattleExperience072).IsAssignableFrom(typeof(M1FlowPresenter)),
                Is.True);
        }

        [Test]
        public void ExplicitRouteResumeDoesNotSilentlyExpandTheSixFounderRoster()
        {
            var previous = M1RuntimeCoordinator.SuppressAutomaticFirstHourRosterMigration072;
            M1RuntimeCoordinator.SuppressAutomaticFirstHourRosterMigration072 = true;
            try
            {
                var campaignPath = Path.Combine(_directory, "campaign.json");
                var contentRoot = Path.Combine(
                    Application.streamingAssetsPath,
                    "Authority",
                    "CONTENT");
                var coordinator = new M1RuntimeCoordinator(contentRoot, campaignPath);
                Assert.That(coordinator.CreateGuild(new M1NewGuildIntent
                {
                    GuildmasterName = "Maren",
                    ModeId = "Standard",
                    TutorialDepthId = "Contextual Tips",
                    TextScale = 1f
                }).Succeeded, Is.True);
                foreach (var applicant in coordinator.State.Applicants)
                    Assert.That(coordinator.SignRecruit(applicant.RecruitId).Succeeded, Is.True);
                Assert.That(coordinator.CompleteEquipmentReview().Succeeded, Is.True);
                Assert.That(coordinator.AddUnion().Succeeded, Is.True);
                Assert.That(coordinator.CompleteFirstHourFoundingCompany072().Succeeded, Is.True);
                Assert.That(coordinator.State.Recruits, Has.Count.EqualTo(6));

                var resumed = new M1RuntimeCoordinator(contentRoot, campaignPath);

                Assert.That(resumed.State.Recruits, Has.Count.EqualTo(6),
                    "Release 071 compatibility migration must not add unseen recruits to the explicit route.");
            }
            finally
            {
                M1RuntimeCoordinator.SuppressAutomaticFirstHourRosterMigration072 = previous;
            }
        }

        [Test]
        public void ClaimedDefeatCannotAdvanceIntoTheVictoryAftermath()
        {
            var defeat = new M2BattleView
            {
                BattleId = FirstHourOpeningState072.HallBreachBattleId,
                Outcome = "Defeat",
                Reward = new M2BattleRewardView { Claimed = true }
            };
            var victory = new M2BattleView
            {
                BattleId = FirstHourOpeningState072.HallBreachBattleId,
                Outcome = "Victory",
                Reward = new M2BattleRewardView { Claimed = true }
            };
            var unrelatedVictory = new M2BattleView
            {
                BattleId = "BATTLE_OTHER",
                Outcome = "Victory",
                Reward = new M2BattleRewardView { Claimed = true }
            };

            Assert.That(FirstHourExperienceRoot.IsClaimedHallBreachVictory072(defeat), Is.False);
            Assert.That(FirstHourExperienceRoot.IsClaimedHallBreachVictory072(victory), Is.True);
            Assert.That(FirstHourExperienceRoot.IsClaimedHallBreachVictory072(unrelatedVictory), Is.False);
        }

        private static FirstHourOpeningState072 StateAtUnionSetup()
        {
            var state = FirstHourOpeningState072.New()
                .AdvanceTo(FirstHourOpeningPhase072.MarketArrival, "FH072_TEST_MARKET")
                .AdvanceTo(FirstHourOpeningPhase072.CharterDialogue, "FH072_TEST_CHARTER")
                .WithDialogueBeat(FirstHourOpeningState072.CharterDialogueBeatCount)
                .WithCampaign("CAMPAIGN_TEST", "Maren")
                .AdvanceTo(FirstHourOpeningPhase072.FounderIntroductions, "FH072_TEST_FOUNDERS");
            for (var index = 0; index < FirstHourOpeningState072.RequiredFounderCount; index++)
                state = state.WithAcceptedFounder("FOUNDER_" + index);
            return state
                .AdvanceTo(FirstHourOpeningPhase072.LoadoutReview, "FH072_TEST_LOADOUTS")
                .ConfirmLoadouts()
                .AdvanceTo(FirstHourOpeningPhase072.UnionSetup, "FH072_TEST_UNIONS");
        }
    }
}
