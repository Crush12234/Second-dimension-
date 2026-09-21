using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class MissionNavigation084Tests
    {
        [TestCase(true, true, true, "EXPEDITION")]
        [TestCase(false, true, true, "WORLD GATE")]
        [TestCase(false, false, true, "CAMPAIGN")]
        [TestCase(false, false, false, "CONTRACTS")]
        public void MissionLandingKeepsTheHighestPriorityActivePlayableFlow084(
            bool hasActiveBoardQuest,
            bool hasActiveWorldGate,
            bool hasActiveCampaign,
            string expectedTab)
        {
            Assert.That(
                M1FlowPresenter.MissionLandingTabForVerification084(
                    hasActiveBoardQuest,
                    hasActiveWorldGate,
                    hasActiveCampaign),
                Is.EqualTo(expectedTab));
        }

        [TestCase(true, true, true, "EXPEDITION")]
        [TestCase(false, true, true, "WORLD GATE")]
        [TestCase(false, false, true, "CAMPAIGN")]
        [TestCase(false, false, false, "CAMPAIGN")]
        public void AvailableStoryChangesOnlyTheIdleMissionLanding122(
            bool boardQuest, bool worldGate, bool campaign, string expected)
        {
            Assert.That(M1FlowPresenter.MissionLandingTabForVerification084(
                boardQuest, worldGate, campaign, true), Is.EqualTo(expected));
        }

        [TestCase(true, true, false, "AVAILABLE", true)]
        [TestCase(false, true, false, "AVAILABLE", false)]
        [TestCase(true, false, false, "AVAILABLE", false)]
        [TestCase(true, true, true, "AVAILABLE", false)]
        [TestCase(true, true, false, "LOCKED", false)]
        [TestCase(true, true, true, "COMPLETED", false)]
        public void StoryLandingUsesExistingAvailableChapterAndOpeningCompletion122(
            bool openingComplete, bool authorityAvailable, bool completed, string status, bool expected)
        {
            var state = new SecondDimension.Presentation.Campaign019.CampaignPresentationState019
            {
                IsAvailable = authorityAvailable,
                Chapters = new[]
                {
                    new SecondDimension.Presentation.Campaign019.CampaignChapterView019
                    { ChapterId = "CH018_002", Completed = completed, Status = status }
                }
            };
            Assert.That(M1FlowPresenter.AvailableStoryMissionForVerification122(
                openingComplete, state), Is.EqualTo(expected));
            Assert.That(state.Chapters[0].Status, Is.EqualTo(status));
            Assert.That(state.Chapters[0].Completed, Is.EqualTo(completed));
        }

        [TestCase(true, true, true, "CAMPAIGN")]
        [TestCase(false, true, true, "CONTRACTS")]
        [TestCase(true, false, true, "CONTRACTS")]
        [TestCase(true, true, false, "CONTRACTS")]
        public void CompletedStoryCycleRemainsReachableOnlyWithItsExistingAuthority130(
            bool openingComplete, bool authorityAvailable, bool canStartCycle, string expected)
        {
            // Presentation routing trusts the existing authority projection;
            // these inputs do not manufacture a completed campaign save.
            var campaign = new SecondDimension.Presentation.Campaign019.CampaignPresentationState019
            {
                IsAvailable = authorityAvailable, CanStartNextCycle130 = canStartCycle,
                CycleCompleted130 = 82, CycleTotal130 = 82,
                Chapters = Enumerable.Range(1, 82).Select(index =>
                    new SecondDimension.Presentation.Campaign019.CampaignChapterView019
                    { ChapterId = "CH018_" + index.ToString("000"), Completed = true, Status = "COMPLETED" }).ToArray()
            };
            var story = M1FlowPresenter.AvailableStoryMissionForVerification122(openingComplete, campaign);
            Assert.That(M1FlowPresenter.MissionLandingTabForVerification084(false, false, false, story),
                Is.EqualTo(expected));
            Assert.That(M1FlowPresenter.MissionLandingTabForVerification084(true, true, true, story),
                Is.EqualTo("EXPEDITION"), "An available replay never takes another operation's navigation ownership.");
            Assert.That(M1FlowPresenter.MissionLandingTabForVerification084(false, true, true, story),
                Is.EqualTo("WORLD GATE"));
            Assert.That(campaign.Chapters.All(chapter => chapter.Completed && chapter.Status == "COMPLETED"), Is.True);
            Assert.That(campaign.CanStartNextCycle130, Is.EqualTo(canStartCycle));
        }

        [Test]
        public void MissingStoryAuthorityDoesNotAdvertiseAnAvailableChapter122()
        {
            Assert.That(M1FlowPresenter.AvailableStoryMissionForVerification122(true, null), Is.False);
            Assert.That(M1FlowPresenter.AvailableStoryMissionForVerification122(true,
                new SecondDimension.Presentation.Campaign019.CampaignPresentationState019
                { IsAvailable = true, Chapters = null }), Is.False);
        }

        [Test]
        public void ResolvedTowerVictoryReturnsToItsClaimBeforeOwnerOrNewMission110()
        {
            var battle = new M2BattleView
            {
                BattleId = "saved-floor-301",
                IsResolved = true,
                Outcome = "Victory",
                Reward = new M2BattleRewardView { RewardId = "earned-floor-301", Claimed = false }
            };
            Assert.That(M1FlowPresenter.ActiveAdventureRouteForVerification110(
                battle, false, true, false, true, false, false, false),
                Is.EqualTo("BATTLE RESULTS"));

            battle.Reward.Claimed = true;
            Assert.That(M1FlowPresenter.ActiveAdventureRouteForVerification110(
                battle, false, true, true, true, false, false, false),
                Is.EqualTo("ABYSS"), "A claimed result still belongs to the unfinished Tower floor.");
            Assert.That(M1FlowPresenter.ActiveAdventureRouteForVerification110(
                battle, false, false, false, false, false, false, false),
                Is.Empty, "A completed and claimed battle must not block the next mission.");
        }

        [Test]
        public void RealInProgressBattleWinsOverOtherLandingAndPreparationRoutes110()
        {
            var battle = new M2BattleView { BattleId = "active-battle", IsResolved = false };
            Assert.That(M1FlowPresenter.ActiveAdventureRouteForVerification110(
                battle, false, true, false, true, true, true, true), Is.EqualTo("BATTLE"));
        }

        [TestCase(true, false, false, false, "ABYSS")]
        [TestCase(false, true, true, false, "WORLD GATE")]
        [TestCase(false, false, true, false, "CAMPAIGN")]
        [TestCase(false, false, false, true, "EXPEDITION")]
        public void CommittedEncounterResumesItsOwningOperationWithoutNewStart110(
            bool tower, bool worldGate, bool campaign, bool expedition, string expected)
        {
            Assert.That(M1FlowPresenter.ActiveAdventureRouteForVerification110(
                null, false, true, false, tower, worldGate, campaign, expedition),
                Is.EqualTo(expected));
        }

        [Test]
        public void StandaloneCommittedEncounterAndPendingReturnStayActionable110()
        {
            Assert.That(M1FlowPresenter.ActiveAdventureRouteForVerification110(
                null, false, true, false, false, false, false, false), Is.EqualTo("ENCOUNTER"));
            Assert.That(M1FlowPresenter.ActiveAdventureRouteForVerification110(
                null, false, false, true, false, false, false, false), Is.EqualTo("BATTLE RESULTS"));
        }

        [Test]
        public void IdleTowerAllowsCampaignBrowsingWithoutChangingGenericResume158()
        {
            var tower = IdleTower158();
            var city = new SecondDimension.Presentation.GuildCity017D.GuildCityPresentationState017D();
            Assert.That(M1FlowPresenter.CanBrowseCampaignFromIdleTowerForVerification158(
                tower, city, null), Is.True);
            var route = M1FlowPresenter.ActiveAdventureRouteForVerification110(
                null, false, false, false, true, false, false, false);
            Assert.That(route, Is.EqualTo("ABYSS"), "Generic Continue still resumes the chosen Tower run.");
            Assert.That(M1FlowPresenter.MainCampaignRouteForVerification158(route, true, false), Is.Empty);
            Assert.That(tower.ActiveAbyssOperationId, Is.EqualTo("TOWERRUN094_navigation158"));
            Assert.That(tower.ActiveAbyssStatus, Is.EqualTo("Active"));
            Assert.That(tower.HighestClearedTowerFloor, Is.EqualTo(300));
        }

        [TestCase("unavailable")]
        [TestCase("legacy-owner")]
        [TestCase("awaiting-battle")]
        [TestCase("step-receipt")]
        [TestCase("battle-receipt")]
        [TestCase("encounter")]
        [TestCase("battle-return")]
        [TestCase("city-reward")]
        [TestCase("active-battle")]
        [TestCase("battle-reward")]
        [TestCase("tower-battle")]
        [TestCase("tower-reward")]
        [TestCase("legacy-recovery")]
        [TestCase("legacy-support")]
        [TestCase("authority-error")]
        public void CommittedTowerBoundaryCannotBeTreatedAsIdleForCampaign158(string boundary)
        {
            var tower = IdleTower158();
            var city = new SecondDimension.Presentation.GuildCity017D.GuildCityPresentationState017D();
            M2BattleView battle = null;
            switch (boundary)
            {
                case "unavailable": tower.IsAvailable = false; break;
                case "legacy-owner": tower.ActiveAbyssOperationId = "ABYSSOP022_legacy"; break;
                case "awaiting-battle": tower.ActiveAbyssStatus = "AwaitingBattle"; break;
                case "step-receipt": tower.HasPendingAbyssStepReceipt = true; break;
                case "battle-receipt": tower.HasPendingAbyssBattleReceipt = true; break;
                case "encounter": city.HasPendingEncounter = true; break;
                case "battle-return": city.HasPendingBattleReturn = true; break;
                case "city-reward": city.HasUnclaimedBattleReward = true; break;
                case "active-battle": battle = new M2BattleView { IsResolved = false }; break;
                case "battle-reward": battle = new M2BattleView
                    { IsResolved = true, Reward = new M2BattleRewardView { Claimed = false } }; break;
                case "tower-battle": tower.TowerBattleInProgress = true; break;
                case "tower-reward": tower.TowerBattleRewardAwaitingClaim = true; break;
                case "legacy-recovery": tower.LegacyTowerRecoveryRequired = true; break;
                case "legacy-support": tower.LegacyTowerRecoveryRequiresSupport = true; break;
                case "authority-error": tower.TowerAuthorityError094 = "Needs review"; break;
            }
            Assert.That(M1FlowPresenter.CanBrowseCampaignFromIdleTowerForVerification158(
                tower, city, battle), Is.False, boundary);
        }

        [Test]
        public void BankedBattleDoesNotStrandTheNextIdleTowerBoundary158()
        {
            Assert.That(M1FlowPresenter.CanBrowseCampaignFromIdleTowerForVerification158(
                IdleTower158(), new SecondDimension.Presentation.GuildCity017D.GuildCityPresentationState017D(),
                new M2BattleView { IsResolved = true, Reward = new M2BattleRewardView { Claimed = true } }),
                Is.True);
        }

        [TestCase("BATTLE", true, true, "BATTLE")]
        [TestCase("BATTLE RESULTS", true, true, "BATTLE RESULTS")]
        [TestCase("ABYSS", false, false, "ABYSS")]
        [TestCase("ABYSS", true, false, "")]
        [TestCase("WORLD GATE", false, true, "CAMPAIGN")]
        [TestCase("WORLD GATE", false, false, "WORLD GATE")]
        [TestCase("CAMPAIGN", false, false, "CAMPAIGN")]
        [TestCase("EXPEDITION", false, false, "EXPEDITION")]
        [TestCase("ENCOUNTER", false, false, "ENCOUNTER")]
        public void CampaignEntryKeepsRequiredOwnersAndRecognizesItsOwnCardBoard158(
            string activeRoute, bool idleTower, bool campaignBoard, string expected)
        {
            Assert.That(M1FlowPresenter.MainCampaignRouteForVerification158(
                activeRoute, idleTower, campaignBoard), Is.EqualTo(expected));
        }

        private static SecondDimension.Presentation.Campaign022.CampaignProgressionPresentationState022 IdleTower158()
            => new SecondDimension.Presentation.Campaign022.CampaignProgressionPresentationState022
            {
                IsAvailable = true,
                ActiveAbyssOperationId = "TOWERRUN094_navigation158",
                ActiveAbyssStatus = "Active",
                HighestClearedTowerFloor = 300
            };
    }
}
